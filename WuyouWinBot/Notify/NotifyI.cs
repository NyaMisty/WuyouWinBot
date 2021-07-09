using System;
using System.Threading.Tasks;

namespace WuyouWinBot.Notify
{
    public abstract class NotifyI
    {
        public virtual async Task notifySMS(string user, string from, DateTime time, string message) { await Task.Delay(1); }
        public virtual async Task notifyCall(string user, string from, DateTime time) { await Task.Delay(1); }
    }
}
