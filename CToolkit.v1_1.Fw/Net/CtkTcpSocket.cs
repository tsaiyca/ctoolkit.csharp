using CToolkit.v1_1.Protocol;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace CToolkit.v1_1.Net
{
    public class CtkTcpSocket : ICtkProtocolNonStopConnect, IDisposable
    {
        public bool IsActively = false;
        public bool IsAutoReceive = true;
        public Uri LocalUri;
        public Uri RemoteUri;
        protected Socket m_connSocket;
        protected Socket m_workSocket;
        bool m_isReceiveLoop = false;
        ManualResetEvent mreIsConnecting = new ManualResetEvent(true);
        ManualResetEvent mreIsReceiving = new ManualResetEvent(true);
        ~CtkTcpSocket() { this.Dispose(false); }

        public Socket ConnSocket { get { return m_connSocket; } }
        public bool IsReceiveLoop { get { return m_isReceiveLoop; } private set { lock (this) m_isReceiveLoop = value; } }
        public bool IsWaitReceive { get { return this.mreIsReceiving.WaitOne(10); } }
        public Socket WorkSocket { get { return m_workSocket; } set { lock (this) { m_workSocket = value; } } }



        /// <summary>
        /// 開始讀取Socket資料, Begin 代表非同步.
        /// 用於 1. IsAutoRead被關閉, 每次讀取需自行執行;
        ///     2. 若連線還在, 但讀取異常中姒, 可以再度開始;
        /// </summary>
        public void BeginReceive()
        {
            var myea = new CtkNonStopTcpStateEventArgs();
            var client = this.ActiveWorkClient as Socket;
            myea.Sender = this;
            myea.WorkSocket = client;
            var trxBuffer = myea.TrxMessageBuffer;
            client.BeginReceive(trxBuffer.Buffer, 0, trxBuffer.Buffer.Length, SocketFlags.None, new AsyncCallback(EndReceiveCallback), myea);
        }

        public bool CheckConnectStatus()
        {
            var socket = this.m_connSocket;
            if (socket == null) return false;
            if (!socket.Connected) return false;
            return !(socket.Poll(1000, SelectMode.SelectRead) && (socket.Available == 0));
        }
        public int ConnectIfNo(bool isAct)
        {
            this.IsActively = isAct;
            if (this.IsOpenRequesting || this.IsRemoteConnected) return 0;
            //if (this.IsLocalReadyConnect) return; //同步連線是等到連線才離開method, 不需判斷 IsLocalReadyConnect

            try
            {
                if (!Monitor.TryEnter(this, 3000)) return -1; // throw new CtkException("Cannot enter lock");
                if (!this.mreIsConnecting.WaitOne(10)) return 0;//連線中先離開
                this.mreIsConnecting.Reset();//先卡住, 不讓後面的再次進行


                //若連線不曾建立, 或聆聽/連線被關閉
                if (this.m_connSocket == null || !this.m_connSocket.Connected)
                {
                    CtkNetUtil.DisposeSocket(this.m_connSocket);//Dispose舊的
                    this.m_connSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//建立新的
                }


                if (isAct)
                {
                    if (this.LocalUri != null && !this.ConnSocket.IsBound)
                        this.ConnSocket.Bind(CtkNetUtil.ToIPEndPoint(this.LocalUri));
                    if (this.RemoteUri == null)
                        throw new CtkException("remote field can not be null");
                    this.ConnSocket.Connect(CtkNetUtil.ToIPEndPoint(this.RemoteUri));
                    this.OnFirstConnect(new CtkProtocolEventArgs() { Message = "Connect Success" });
                    this.WorkSocket = this.ConnSocket;
                }
                else
                {
                    if (this.LocalUri == null)
                        throw new Exception("local field can not be null");
                    if (!this.ConnSocket.IsBound)
                        this.ConnSocket.Bind(CtkNetUtil.ToIPEndPoint(this.LocalUri));
                    this.ConnSocket.Listen(100);
                    this.WorkSocket = this.ConnSocket.Accept();
                    this.OnFirstConnect(new CtkProtocolEventArgs() { Message = "Connect Success" });
                }

                return 0;
            }
            catch (Exception ex)
            {
                //一旦聆聽/連線失敗, 直接關閉所有Socket, 重新來過
                this.Disconnect();
                this.OnFailConnect(new CtkProtocolEventArgs() { Message = "Connect Fail" });
                throw ex;//同步型作業, 直接拋出例外, 不用寫Log
            }
            finally
            {
                this.mreIsConnecting.Set();
                if (Monitor.IsEntered(this)) Monitor.Exit(this);
            }
        }
        public int ReceiveLoop()
        {
            try
            {
                this.IsReceiveLoop = true;
                while (this.IsReceiveLoop && !this.disposed)
                {
                    this.ReceiveOnce();
                }
            }
            catch (Exception ex)
            {
                this.IsReceiveLoop = false;
                //當 this.ConnSocket == this.WorkSocket 時, 代表這是 client 端
                if (this.ConnSocket != this.WorkSocket)
                    CtkNetUtil.DisposeSocket(this.WorkSocket);//Repeat/Loop執行, 一旦結束就釋放Socket
                throw ex;//同步型作業, 直接拋出例外, 不用寫Log
            }
            return 0;
        }
        public void ReceiveLoopCancel()
        {
            this.IsReceiveLoop = false;
        }
        public int ReceiveOnce()
        {
            try
            {
                if (!Monitor.TryEnter(this, 1000)) return -1;//進不去先離開
                if (!this.mreIsReceiving.WaitOne(10)) return 0;//接收中先離開
                this.mreIsReceiving.Reset();//先卡住, 不讓後面的再次進行

                var ea = new CtkProtocolEventArgs()
                {
                    Sender = this,
                };

                ea.TrxMessage = new CtkProtocolBufferMessage(1518);
                var trxBuffer = ea.TrxMessage.ToBuffer();


                trxBuffer.Length = this.WorkSocket.Receive(trxBuffer.Buffer, 0, trxBuffer.Buffer.Length, SocketFlags.None);
                if (trxBuffer.Length == 0) return -1;
                this.OnDataReceive(ea);
            }
            catch (Exception ex)
            {
                this.OnErrorReceive(new CtkProtocolEventArgs() { Message = "Read Fail" });
                //當 this.ConnSocket == this.WorkSocket 時, 代表這是 client 端
                if (this.ConnSocket != this.WorkSocket)
                    CtkNetUtil.DisposeSocket(this.WorkSocket);//執行出現例外, 先釋放Socket
                throw ex;//同步型作業, 直接拋出例外, 不用寫Log
            }
            finally
            {
                this.mreIsReceiving.Set();//同步型的, 結束就可以Set
                if (Monitor.IsEntered(this)) Monitor.Exit(this);
            }
            return 0;
        }
        void EndReceiveCallback(IAsyncResult ar)
        {
            //var stateea = (CtkNonStopTcpStateEventArgs)ar.AsyncState;
            var myea = (CtkNonStopTcpStateEventArgs)ar.AsyncState;
            try
            {
                var client = myea.WorkSocket;
                if (!ar.IsCompleted || client == null || !client.Connected)
                {
                    throw new CtkException("Read Fail");
                }

                var ctkBuffer = myea.TrxMessageBuffer;
                var bytesRead = client.EndReceive(ar);
                ctkBuffer.Length = bytesRead;
                //呼叫他人不應影響自己運作, catch起來
                try { this.OnDataReceive(myea); }
                catch (Exception ex) { CtkLog.Write(ex); }

                if (this.IsAutoReceive)
                    client.BeginReceive(ctkBuffer.Buffer, 0, ctkBuffer.Buffer.Length, SocketFlags.None, new AsyncCallback(EndReceiveCallback), myea);

            }
            //catch (IOException ex) { CtkLog.Write(ex); }
            catch (Exception ex)
            {
                //讀取失敗, 中斷連線(會呼叫 OnDisconnect), 不需要呼叫 OnFailConnect
                this.Disconnect();
                myea.Message = ex.Message;
                myea.Exception = ex;
                this.OnErrorReceive(myea);//但要呼叫 OnErrorReceive
                CtkLog.WarnNs(this, ex);
            }
        }





        #region ICtkProtocolConnect

        public event EventHandler<CtkProtocolEventArgs> EhDataReceive;
        public event EventHandler<CtkProtocolEventArgs> EhDisconnect;
        public event EventHandler<CtkProtocolEventArgs> EhErrorReceive;
        public event EventHandler<CtkProtocolEventArgs> EhFailConnect;
        public event EventHandler<CtkProtocolEventArgs> EhFirstConnect;

        public object ActiveWorkClient { get { return this.WorkSocket; } set { this.WorkSocket = value as Socket; } }
        public bool IsLocalReadyConnect { get { return this.m_connSocket != null && this.m_connSocket.IsBound; } }
        public bool IsOpenRequesting { get { return this.mreIsConnecting.WaitOne(10); } }
        public bool IsRemoteConnected { get { return this.WorkSocket != null && this.WorkSocket.Connected; } }

        public int ConnectIfNo() { return this.ConnectIfNo(this.IsActively); }
        public void Disconnect()
        {
            this.mreIsReceiving.Set();

            try { CtkNetUtil.DisposeSocket(this.m_workSocket); }
            catch (Exception ex) { CtkLog.WarnNs(this, ex); }
            try { CtkNetUtil.DisposeSocket(this.m_connSocket); }
            catch (Exception ex) { CtkLog.WarnNs(this, ex); }

            this.OnDisconnect(new CtkProtocolEventArgs() { Message = "Disconnect method is executed" });

        }
        public void WriteMsg(CtkProtocolTrxMessage msg)
        {
            try
            {
                //寫入不卡Monitor, 並不會造成impact
                //但如果卡了Monitor, 你無法同時 等待Receive 和 要求Send

                //其它作業可以卡 Monitor.TryEnter
                //此物件會同時進行的只有 Receive 和 Send
                //所以其它作業卡同一個沒問題: Monitor.TryEnter(this, 1000)

                var buffer = msg.ToBuffer();
                this.WorkSocket.Send(buffer.Buffer, buffer.Offset, buffer.Length, SocketFlags.None);
            }
            catch (Exception ex)
            {
                this.Disconnect();//寫入失敗就斷線
                CtkLog.WarnNs(this, ex);
                throw ex;//就例外就拋出, 不吃掉
            }
        }

        #endregion


        #region ICtkProtocolNonStopConnect

        public int IntervalTimeOfConnectCheck { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsNonStopRunning => throw new NotImplementedException();
        public void AbortNonStopConnect()
        {
            throw new NotImplementedException();
        }
        public void NonStopConnectAsyn()
        {
            throw new NotImplementedException();
        }

        #endregion



        #region Event

        protected void OnDataReceive(CtkProtocolEventArgs ea)
        {
            if (this.EhDataReceive == null) return;
            this.EhDataReceive(this, ea);
        }
        protected void OnDisconnect(CtkProtocolEventArgs ea)
        {
            if (this.EhDisconnect == null) return;
            this.EhDisconnect(this, ea);
        }
        protected void OnErrorReceive(CtkProtocolEventArgs ea)
        {
            if (this.EhErrorReceive == null) return;
            this.EhErrorReceive(this, ea);
        }
        protected void OnFailConnect(CtkProtocolEventArgs ea)
        {
            if (this.EhFailConnect == null) return;
            this.EhFailConnect(this, ea);
        }
        protected void OnFirstConnect(CtkProtocolEventArgs ea)
        {
            if (this.EhFirstConnect == null) return;
            this.EhFirstConnect(this, ea);
        }

        #endregion





        #region Dispose

        bool disposed = false;
        public void Dispose()
        {
            Dispose(false);
            GC.SuppressFinalize(this);
        }
        public void DisposeSelf()
        {
            this.Disconnect();
            CtkUtilFw.DisposeObjTry(this.mreIsConnecting);
            CtkUtilFw.DisposeObjTry(this.mreIsReceiving);
            CtkEventUtil.RemoveEventHandlersOfOwnerByFilter(this, (dlgt) => true);

        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            if (disposing)
            {
                // Free any managed objects here.
            }
            // Free any unmanaged objects here.
            //
            this.DisposeSelf();
            disposed = true;
        }


        #endregion











    }

}





