using CToolkit.v1_0.Protocol;
using CToolkit.v1_0.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CToolkit.v1_0.Wcf
{


    /// <summary>
    /// //����²���T���洫 & ���� Channel
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    public class CtkWcfDuplexTcpListener<TService>
        : CtkWcfDuplexTcpListenerBasic<TService, ICTkWcfDuplexOpCallback>
        , ICtkProtocolNonStopConnect
        where TService : ICtkWcfDuplexOpService
    {

        protected int m_IntervalTimeOfConnectCheck = 5000;
        ICTkWcfDuplexOpCallback activeWorkClient;
        CtkCancelTask NonStopTask;
        public CtkWcfDuplexTcpListener(TService serviceInstance) : base(new NetTcpBinding(), serviceInstance) { }
        ~CtkWcfDuplexTcpListener() { this.Dispose(false); }



        void CleanHost()
        {
            CtkEventUtil.RemoveEventHandlersFromOwningByFilter(this, (dlgt) => true);
            CtkEventUtil.RemoveEventHandlersFromOwningByTarget(this.host, this);
        }




        #region ICtkProtocolNonStopConnect


        public event EventHandler<CtkProtocolEventArgs> evtDataReceive;

        public event EventHandler<CtkProtocolEventArgs> evtDisconnect;

        public event EventHandler<CtkProtocolEventArgs> evtErrorReceive;

        public event EventHandler<CtkProtocolEventArgs> evtFailConnect;

        public event EventHandler<CtkProtocolEventArgs> evtFirstConnect;

        public object ActiveWorkClient { get { return this.activeWorkClient; } set { this.activeWorkClient = value as ICTkWcfDuplexOpCallback; } }

        public int IntervalTimeOfConnectCheck { get { return this.m_IntervalTimeOfConnectCheck; } set { this.m_IntervalTimeOfConnectCheck = value; } }

        public bool IsLocalReadyConnect { get { return this.host != null && this.host.State <= CommunicationState.Opened; } }

        public bool IsNonStopRunning { get { return this.NonStopTask != null && this.NonStopTask.Task.Status < TaskStatus.RanToCompletion; } }

        public bool IsOpenRequesting { get { try { return Monitor.TryEnter(this, 10); } finally { Monitor.Exit(this); } } }

        public bool IsRemoteConnected { get { return this.GetAllChannels().Count > 0; } }
        public void AbortNonStopConnect()
        {
            this.NonStopTask.Cancel();
        }

        public void ConnectIfNo()
        {
            if (this.IsLocalReadyConnect) return;

            try
            {
                if (!Monitor.TryEnter(this, 1000)) return;//�i���h�����}
                this.CleanDisconnect();
                this.CleanHost();
                this.NewHost();

                this.host.Opened += (ss, ee) =>
                {
                    var ea = new CtkWcfDuplexEventArgs();
                    //ea.WcfChannel = this.GetCallback();//Listener(or call Host, Service) �}�ҫ�, �èS��Channel�s�u�i��
                    this.OnFirstConnect(ea);
                };


                this.serviceInstance.evtReceiveMsg += (ss, ee) =>
                {
                    var ea = ee;
                    ea.WcfChannel = this.GetCallback();
                    this.OnDataReceive(ea);
                };

                this.host.Closed += (ss, ee) =>
                {
                    var ea = new CtkWcfDuplexEventArgs();
                    //ea.WcfChannel = this.GetCallback();//Listerner����, �|�����Ҧ�Channel, �èS���S�w���@��
                    this.OnDisconnect(ea);
                };
                this.Open();
            }
            finally
            {
                Monitor.Exit(this);
            }
        }

        public void Disconnect()
        {
            this.AbortNonStopConnect();
            base.Close();

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
                    Thread.Sleep(this.IntervalTimeOfConnectCheck);
                }

            });


        }



        /// <summary>
        /// �u�䴩 CtkWcfMessage
        /// </summary>
        /// <param name="msg"></param>
        public void WriteMsg(CtkProtocolTrxMessage msg)
        {
            var wcfmsg = msg.As<CtkWcfMessage>();
            if (wcfmsg != null)
            {
                this.activeWorkClient.CtkSend(msg.As<CtkWcfMessage>());
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




        #region ICtkWcfDuplexOpService

        #endregion


        #region IDisposable

        public override void DisposeSelf()
        {
            this.Disconnect();
            base.DisposeSelf();
        }

        #endregion



    }



    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class CtkWcfDuplexTcpListener : ICtkWcfDuplexOpService
    {
        public event EventHandler<CtkWcfDuplexEventArgs> evtReceiveMsg;

        public static CtkWcfDuplexTcpListener<T> CreateSingle<T>(T inst = null) where T : class, ICtkWcfDuplexOpService, new()
        {
            if (inst == null)
                return new CtkWcfDuplexTcpListener<T>(new T());
            else
                return new CtkWcfDuplexTcpListener<T>(inst);
        }

        public static CtkWcfDuplexTcpListener<CtkWcfDuplexTcpListener> CreateSingle()
        {
            return new CtkWcfDuplexTcpListener<CtkWcfDuplexTcpListener>(new CtkWcfDuplexTcpListener());
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

        void OnReceiveMsg(CtkWcfDuplexEventArgs ea)
        {
            if (this.evtReceiveMsg == null) return;
            this.evtReceiveMsg(this, ea);
        }
    }


}
