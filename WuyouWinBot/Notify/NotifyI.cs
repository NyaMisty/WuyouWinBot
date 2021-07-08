using System;

namespace WuyouWinBot.Notify
{
    interface NotifyI
    {
        void notifySMS(string user, string from, DateTime time, string message);
        void notifyCall(string user, string from, DateTime time);
    }
}
