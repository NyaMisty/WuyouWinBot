using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;
using JCSDK.JCCall;
using JCSDK.JCClient;
using JCSDK.JCMessageChannel;
using Juphoon.Mtc;
using log4net;
using log4net.Config;
using WuyouWinBot.JCWrapper;
using WuyouWinBot.Notify;

namespace WuyouWinBot
{

    class WuyouManager
    {
        //private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));

        public string appKey = "50f3e61050214e8e05a44095";
        public string serverAddr = "http:aws.jegotrip.com.cn:10801";
        public string deviceId = Guid.NewGuid().ToString();
        public string sessId = "<pc>";
        public string user;
        public string password;
        public List<NotifyI> notifys = new List<NotifyI> { };

        public WuyouManager(string user, string password)
        {
            this.user = user;
            this.password = password;
        }

        public int initialize()
        {
            Logger.WarnFormat("WuyouManager initializing!");
            JCManager.appkey = appKey;
            JCManager.app = null;
            JCManager.shared().dgJCLog += new JCManager.DgJCLog(onJCLog);
            JCManager.shared().dgOnClientStateChange += new JCManager.DgOnClientStateChange(onClientStateChange);
            JCManager.shared().dgOnLogin += new JCManager.DgOnLogin(onLogin);
            JCManager.shared().dgOnLogout += new JCManager.DgOnLogout(onLogout);
            JCManager.shared().dgOnCallItemAdd += new JCManager.DgOnCallItemAdd(onCallItemAdd);
            JCManager.shared().dgOnCallItemRemove += new JCManager.DgOnCallItemRemove(onCallItemRemove);
            JCManager.shared().dgOnCallItemUpdate += new JCManager.DgOnCallItemUpdate(onCallItemUpdate);
            JCManager.shared().dgOnMessageRecv += new JCManager.DgOnMessageRecv(onMessageRecv);

            if (!JCManager.shared().initialize())
            {
                Logger.InfoFormat("WuyouManager JCSDK init failed!");
                return 1;
            }
            int ret = MtcCli.Mtc_CliApplySessId(sessId);
            return ret;
        }

        public bool login()
        {
            Logger.WarnFormat("WuyouManager Logging in!");
            JCClientState state = JCManager.shared().client.state;
            if (state == JCClientState.Idle)
            {
                JCClient.LoginParam loginParam = new JCClient.LoginParam();
                loginParam.deviceId = deviceId;

                if (!JCManager.shared().client.login(user, password, serverAddr, loginParam))
                {
                    return false;
                }
                return true;
            }
            else
            {
                Logger.WarnFormat("Already loggined, current state {0}", state);
                return false;
            }
        }

        private void onJCLog(string log)
        {
            Logger.DebugFormat("JCLog: {0}", log);
        }

        private void onClientStateChange(JCClientState state, JCClientState oldState)
        {
            // Engine will automatically reconnect when connection lost
        }

        private void onLogin(bool result, JCClientReason reason)
        {
            if (result)
            {
                Logger.InfoFormat("Successfully logged in!");
            }
            else
            {
                Logger.ErrorFormat("Failed to logged in! reason: {0}, re-login in 15 seconds!", reason);
                Task.Delay(15000).ContinueWith(t => login());
            }
        }
        private void onLogout(JCClientReason reason)
        {
            Logger.ErrorFormat("Got logged out due to {0}, re-login in 10 seconds!", reason);
            Task.Delay(10000).ContinueWith(t => login());
        }

        private void onMessageRecv(JCMessageChannelItem message)
        {
            Logger.InfoFormat("Got SMS message {0}", message);
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(message.sentTime / 1000).ToLocalTime();
            foreach (var notify in notifys)
            {
                try
                {
                    notify.notifySMS(user, message.displayName, dtDateTime, message.text);
                }
                catch (Exception e)
                {
                    Logger.WarnFormat("Error in onMessageRecv notify {0}: {1}", notify, e);
                }
            }
        }

        private void onCallItemAdd(JCCallItem item)
        {
            Logger.InfoFormat("Got Call Income {0}", item);
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(item.beginTime).ToLocalTime();
            foreach (var notify in notifys)
            {
                try
                {
                    notify.notifyCall(user, item.displayName, dtDateTime);
                }
                catch (Exception e)
                {
                    Logger.WarnFormat("Error in onCallItemAdd notify {0}: {1}", notify, e);
                }
            }
        }

        private void onCallItemUpdate(JCCallItem item, JCCallItem.ChangeParam changeParam)
        {
            Logger.InfoFormat("Got Call Update {0}", item);
        }

        private void onCallItemRemove(JCCallItem item, JCCallReason reason)
        {
            Logger.InfoFormat("Got Call Finish {0}", item);
        }
    }
    class Program
    {
        //private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));
        public static Dispatcher maindisp;

        static void WuyouThread(string[] args)
        {
            Logger.DebugFormat("Creating WuyouManager...");
            var manager = new WuyouManager(args[0], args[1]);
            manager.notifys.Add(new NotifyWeixin());
            manager.notifys.Add(new NotifyTelegram());

            Logger.DebugFormat("Initializing WuyouManager...");
            manager.initialize();
            Logger.DebugFormat("Logining WuyouManager...");
            manager.login();
        }

        [STAThread]
        static void Main(string[] args)
        {
            BasicConfigurator.Configure();
            if (args.Length < 2)
            {
                Logger.Fatal("At least supply username and password!");
                System.Environment.Exit(1);
            }

            Task.Delay(1000).ContinueWith(delegate
            {
                maindisp.Invoke(() =>
               {
                   try
                   {
                       WuyouThread(args);
                   } catch (Exception e)
                   {
                       Logger.Fatal("Error in WuyouThread: ", e);
                   }
               });
            });

            //Application.EnableVisualStyles();
            //Application.Run(new Form()); // or whatever
            maindisp = Dispatcher.CurrentDispatcher;
            Dispatcher.Run();
        }
    }
}
