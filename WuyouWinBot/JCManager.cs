using System.Collections.Generic;
using System.Windows;
using JCSDK.JCAccount;
using JCSDK.JCCall;
using JCSDK.JCClient;
using JCSDK.JCMediaDevice;
using JCSDK.JCMessageChannel;
using log4net;

namespace WuyouWinBot.JCWrapper
{
    class JCManager : JCClientCallback, JCMediaDeviceCallback, JCCallCallback, JCMessageChannelCallback
    {
        //private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));

        public static string appkey;
        public static Application app;

        private static JCManager _manager;
        private JCClient _client;
        private JCMediaDevice _mediaDevice;
        private JCCall _call;
        private JCMessageChannel _messagechannel;
        private JCAccount _account;

        private bool _pstnMode = false;
        public delegate void DgOnClientStateChange(JCClientState state, JCClientState oldState);
        public DgOnClientStateChange dgOnClientStateChange = delegate { };
        public delegate void DgOnLogin(bool result, JCClientReason reason);
        public DgOnLogin dgOnLogin = delegate { };
        public delegate void DgOnLogout(JCClientReason reason);
        public DgOnLogout dgOnLogout = delegate { };

        public delegate void DgOnCallItemAdd(JCCallItem item);
        public DgOnCallItemAdd dgOnCallItemAdd = delegate { };
        public delegate void DgOnCallItemRemove(JCCallItem item, JCCallReason reason);
        public DgOnCallItemRemove dgOnCallItemRemove = delegate { };
        public delegate void DgOnCallItemUpdate(JCCallItem item, JCCallItem.ChangeParam changeParam);
        public DgOnCallItemUpdate dgOnCallItemUpdate = delegate { };
        public delegate void DgOnCallMessageReceive(string type, string content, JCCallItem item);
        public DgOnCallMessageReceive dgOnCallMessageReceive = delegate { };
        public delegate void DgOnCallDtmfReceive(JCCallItem item, JCCall.DtmfValue value);
        public DgOnCallDtmfReceive dgOnCallDtmfReceive = delegate { };

        public delegate void DgOnMessageSendUpdate(JCMessageChannelItem message);
        public DgOnMessageSendUpdate dgOnMessageSendUpdate = delegate { };
        public delegate void DgOnMessageRecv(JCMessageChannelItem message);
        public DgOnMessageRecv dgOnMessageRecv = delegate { };

        public delegate void DgJCLog(string log);
        public DgJCLog dgJCLog = delegate { };


        public JCMessageChannel messageChannel
        {
            get
            {
                return _messagechannel;
            }
        }

        public JCClient client
        {
            get
            {
                return _client;
            }
        }

        public JCMediaDevice mediaDevice
        {
            get
            {
                return _mediaDevice;
            }
        }

        public JCCall call
        {
            get
            {
                return _call;
            }
        }

        public bool pstnMode
        {
            get
            {
                return _pstnMode;
            }
            set
            {
                _pstnMode = value;
            }
        }

        private JCManager()
        {

        }

        public static JCManager shared()
        {
            if (_manager != null)
            {
                return _manager;
            }
            _manager = new JCManager();
            return _manager;
        }

        public bool isInited
        {
            get
            {
                return _client != null;
            }
        }

        public bool initialize()
        {
            JCClient.CreateParam createParam = new JCClient.CreateParam();
            createParam.sdkInfoDir = "./sdk_data";
            createParam.sdkLogDir = "./sdk_data/log";
            createParam.sdkLogLevel = (JCLogLevel)100000000;
            Logger.InfoFormat("creating JCClient...");
            _client = JCClient.create(app, appkey, this, createParam);
            if (_client.state == JCClientState.NotInit)
            {
                return false;
            }
            Logger.InfoFormat("creating JCMediaDevice...");
            _mediaDevice = JCMediaDevice.create(_client, this);
            _call = JCCall.create(_client, _mediaDevice, this);
            _messagechannel = JCMessageChannel.create(_client, this);
            addLog("*initialize");
            return true;
        }

        public void destroy()
        {
            if (_client != null)
            {
                addLog("*destroy");
                JCCall.destroy();
                JCMessageChannel.destroy();
                JCMediaDevice.destroy();
                JCClient.destroy();
                _client = null;
                _mediaDevice = null;
                _call = null;
                _messagechannel = null;
            }
        }

        void addLog(string log)
        {
            if (dgJCLog != null)
            {
                dgJCLog.Invoke(log);
            }
        }

        #region JCClientCallback

        public void onLogin(bool result, JCClientReason reason)
        {
            dgOnLogin.Invoke(result, reason);
            addLog(string.Format("*onLogin result:{0} reason:{1}", result, reason));
        }

        public void onLogout(JCClientReason reason)
        {
            dgOnLogout.Invoke(reason);
            addLog(string.Format("*onLogout reason:{0}", reason));
        }

        public void onClientStateChange(JCClientState state, JCClientState oldState)
        {
            dgOnClientStateChange.Invoke(state, oldState);
            addLog(string.Format("*onClientStateChange state:{0} oldState:{1}", state, oldState));
        }

        #endregion

        #region JCCallCallback
        public void onCallItemAdd(JCCallItem item)
        {
            dgOnCallItemAdd.Invoke(item);
            addLog(string.Format("*onCallItemAdd {0}", item.userId));
        }
        public void onCallItemRemove(JCCallItem item, JCCallReason reason)
        {
            dgOnCallItemRemove.Invoke(item, reason);
            addLog(string.Format("*onCallItemRemove {0}", item.userId));
        }
        public void onCallItemUpdate(JCCallItem item, JCCallItem.ChangeParam changeParam)
        {
            dgOnCallItemUpdate.Invoke(item, changeParam);
            addLog(string.Format("*onCallItemUpdate {0}", item.userId));
        }
        public void onMessageReceive(string type, string content, JCCallItem item)
        {
            dgOnCallMessageReceive.Invoke(type, content, item);
            addLog(string.Format("*onMessageReceive {0} type:{1} content:{2}", item.userId, type, content));
        }
        public void onMissedCallItem(JCCallItem item)
        {
            addLog(string.Format("*onMissedCallItem {0}", item.userId));
        }

        public void onDtmfReceived(JCCallItem item, JCCall.DtmfValue value)
        {
            dgOnCallDtmfReceive.Invoke(item, value);
            addLog(string.Format("*onDtmfReceived {0}", item.userId));
        }


        #endregion

        #region JCMediaDeviceCallback
        public void onCameraUpdate()
        {
            ((JCMediaDeviceCallback)_manager).onCameraUpdate();
        }
        #endregion


        #region JCMessageChannelCallback

        public void onMessageSendUpdate(JCMessageChannelItem message)
        {
            dgOnMessageSendUpdate.Invoke(message);
        }

        public void onMessageRecv(JCMessageChannelItem message)
        {
            dgOnMessageRecv.Invoke(message);
            addLog(string.Format("*onMessageRecv {0}", message.text));
        }


        #endregion

        public void onQueryUserStatusResult(int operationId, bool result, List<JCAccountItem> accountItemList)
        {

        }
    }
}
