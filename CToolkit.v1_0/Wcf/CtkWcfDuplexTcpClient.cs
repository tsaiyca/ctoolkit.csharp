using CToolkit.v1_0.Protocol;
using CToolkit.v1_0.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CToolkit.v1_0.Wcf
{


    public class CtkWcfDuplexTcpClient<TService, TCallback> : IDisposable
        , ICtkProtocolNonStopConnect
        where TService : ICtkWcfDuplexOpService//Server提供的, 必須是interface
        where TCallback : ICTkWcfDuplexOpCallback//提供給Server呼叫的
    {


        public TService Channel;
        public DuplexChannelFactory<TService> ChannelFactory;
        public TCallback Callback;
        CtkCancelTask NonStopTask;
        public string Uri;
        public string EntryAddress;
        protected int m_IntervalTimeOfConnectCheck = 5000;

        ~CtkWcfDuplexTcpClient() { this.Dispose(false); }


        public CtkWcfDuplexTcpClient(TCallback callbackInstance)
        {
            this.Callback = callbackInstance;
        }






        #region ICtkProtocolNonStopConnect


        public event EventHandler<CtkProtocolEventArgs> evtDataReceive;

        public event EventHandler<CtkProtocolEventArgs> evtDisconnect;

        public event EventHandler<CtkProtocolEventArgs> evtErrorReceive;

        public event EventHandler<CtkProtocolEventArgs> evtFailConnect;

        public event EventHandler<CtkProtocolEventArgs> evtFirstConnect;

        public object ActiveWorkClient { get { return this.Channel; } set { this.Channel = (TService)value; } }

        public bool IsLocalReadyConnect { get { return this.ChannelFactory != null && this.ChannelFactory.State <= CommunicationState.Opened; } }

        public bool IsNonStopRunning { get { return this.NonStopTask != null && this.NonStopTask.Task.Status < TaskStatus.RanToCompletion; } }

        public bool IsOpenRequesting { get { try { return Monitor.TryEnter(this, 10); } finally { Monitor.Exit(this); } } }

        public bool IsRemoteConnected { get { return this.ChannelFactory.State == CommunicationState.Opened; } }

        public int IntervalTimeOfConnectCheck { get { return this.m_IntervalTimeOfConnectCheck; } set { this.m_IntervalTimeOfConnectCheck = value; } }

        public void AbortNonStopConnect()
        {
            if (this.NonStopTask != null)
            {
                using (var obj = this.NonStopTask)
                    obj.Cancel();
            }
        }

        public void ConnectIfNo()
        {
            if (string.IsNullOrEmpty(this.Uri)) throw new ArgumentNullException("The Uri must has value");
            if (this.IsLocalReadyConnect) return;
            try
            {
                if (!Monitor.TryEnter(this, 1000)) return;//進不去先離開

                var site = new InstanceContext(this.Callback);
                var address = this.Uri;
                if (this.EntryAddress != null) address = Path.Combine(this.Uri, this.EntryAddress);
                var endpointAddress = new EndpointAddress(address);
                var myBinding = new NetTcpBinding();
                myBinding.Security.Mode = SecurityMode.None;
                myBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.None;
                this.ChannelFactory = new DuplexChannelFactory<TService>(site, myBinding, endpointAddress);
                this.ChannelFactory.Opened += (ss, ee) =>
                {
                    var ea = new CtkWcfDuplexEventArgs();
                    ea.WcfChannel = this.Channel;
                    this.OnFirstConnect(ea);
                };
                this.ChannelFactory.Closed += (ss, ee) =>
                 {
                     var ea = new CtkWcfDuplexEventArgs();
                     ea.WcfChannel = this.Channel;
                     this.OnDisconnect(ea);
                 };

                this.Channel = this.ChannelFactory.CreateChannel();
                this.Channel.CtkSend(new CtkWcfMessage());
            }
            finally
            {
                Monitor.Exit(this);
            }
        }

        public void Disconnect()
        {
            this.AbortNonStopConnect();
            if (this.ChannelFactory != null)
            {
                using (var obj = this.ChannelFactory)
                {
                    obj.Abort();
                    obj.Close();
                }
            }

            CtkEventUtil.RemoveEventHandlersFromOwningByFilter(this, (dlgt) => true);

        }

        public void NonStopConnectAsyn()
        {
            AbortNonStopConnect();

            this.NonStopTask = CtkCancelTask.Run((ct) =>
            {
                while (!this.disposed && !ct.IsCancellationRequested)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        this.ConnectIfNo();
                    }
                    catch (Exception ex) { CtkLog.Write(ex); }
                    Thread.Sleep(this.m_IntervalTimeOfConnectCheck);
                }

            });


        }



        /// <summary>
        /// 只支援 CtkWcfMessage
        /// </summary>
        /// <param name="msg"></param>
        public void WriteMsg(CtkProtocolTrxMessage msg)
        {
            var wcfmsg = msg.As<CtkWcfMessage>();
            if (wcfmsg != null)
            {
                this.Channel.CtkSend(msg.As<CtkWcfMessage>());
                return;
            }
            throw new ArgumentException("No support type");
        }

        void OnDataReceive(CtkProtocolEventArgs ea)
        {
            if (this.evtDataReceive == null) return;
            this.evtDataReceive(this, ea);
        }
        void OnDisconnect(CtkProtocolEventArgs tcpstate)
        {
            if (this.evtDisconnect == null) return;
            this.evtDisconnect(this, tcpstate);
        }
        void OnErrorReceive(CtkProtocolEventArgs ea)
        {
            if (this.evtErrorReceive == null) return;
            this.evtErrorReceive(this, ea);
        }
        void OnFailConnect(CtkProtocolEventArgs tcpstate)
        {
            if (this.evtFailConnect == null) return;
            this.evtFailConnect(this, tcpstate);
        }
        void OnFirstConnect(CtkProtocolEventArgs tcpstate)
        {
            if (this.evtFirstConnect == null) return;
            this.evtFirstConnect(this, tcpstate);
        }
        #endregion


        #region IDispose

        bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void DisposeManaged()
        {

        }

        public virtual void DisposeSelf()
        {
            this.Disconnect();
        }

        public virtual void DisposeUnManaged()
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Free any managed objects here.
                this.DisposeManaged();
            }

            // Free any unmanaged objects here.
            //
            this.DisposeUnManaged();
            this.DisposeSelf();
            disposed = true;
        }
        #endregion
    }

    public class CtkWcfDuplexTcpClient : ICTkWcfDuplexOpCallback
    {

        public static CtkWcfDuplexTcpClient<TService, TCallback> CreateSingle<TService, TCallback>(TCallback inst = null)
            where TService : ICtkWcfDuplexOpService
            where TCallback : class, ICTkWcfDuplexOpCallback, new()
        {
            if (inst == null)
                return new CtkWcfDuplexTcpClient<TService, TCallback>(new TCallback());
            else
                return new CtkWcfDuplexTcpClient<TService, TCallback>(inst);
        }

        public static CtkWcfDuplexTcpClient<TService, CtkWcfDuplexTcpClient> CreateSingle<TService>()
            where TService : ICtkWcfDuplexOpService
        {
            return new CtkWcfDuplexTcpClient<TService, CtkWcfDuplexTcpClient>(new CtkWcfDuplexTcpClient());
        }

        public static CtkWcfDuplexTcpClient<ICtkWcfDuplexOpService, CtkWcfDuplexTcpClient> CreateSingle()
        {
            return new CtkWcfDuplexTcpClient<ICtkWcfDuplexOpService, CtkWcfDuplexTcpClient>(new CtkWcfDuplexTcpClient());
        }




        public event EventHandler<CtkWcfDuplexEventArgs> evtReceiveMsg;

        void OnReceiveMsg(CtkWcfDuplexEventArgs tcpstate)
        {
            if (this.evtReceiveMsg == null) return;
            this.evtReceiveMsg(this, tcpstate);
        }


        public void CtkSend(CtkWcfMessage msg)
        {
            var ea = new CtkWcfDuplexEventArgs();
            ea.WcfMsg = msg;
            ea.IsWcfNeedReturnMsg = false;
            this.OnReceiveMsg(ea);
        }

        public CtkWcfMessage CtkSendReply(CtkWcfMessage msg)
        {
            var ea = new CtkWcfDuplexEventArgs();
            ea.WcfMsg = msg;
            ea.IsWcfNeedReturnMsg = true;
            this.OnReceiveMsg(ea);
            return ea.WcfReturnMsg;
        }


    }




}
