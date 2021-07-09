using System;
using System.Net;
using System.Threading.Tasks;
using log4net;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace WuyouWinBot.Notify
{
    class NotifyTelegram : NotifyI
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));

        ITelegramBotClient botClient;

        public NotifyTelegram()
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            botClient = new TelegramBotClient(Properties.Settings.Default.tgBotToken);
        }

        public async override Task notifyCall(string user, string from, DateTime time)
        {
            Logger.InfoFormat("Sending telegram notifyCall! user: {0}, from: {1}, time: {2}", user, from, time);
            var title = "无忧行 " + user + " 接到电话：" + from;
            var content = "呼叫时间：" + time;
            var m = await botClient.SendTextMessageAsync(
              chatId: Properties.Settings.Default.tgChatId,
              text: String.Format("* {0} *\r\n{1}", title, content),
              parseMode: ParseMode.Markdown
            );
            Logger.InfoFormat("NotifyTelegram notifyCall result: {0}", m);
        }

        public async override Task notifySMS(string user, string from, DateTime time, string message)
        {
            Logger.InfoFormat("Sending telegram notifySMS! user: {0}, from: {1}, time: {2}, message: {3}", user, from, time, message);
            var title = "无忧行 " + user + " 收到短信：" + from;
            var content = "" + message;
            var m = await botClient.SendTextMessageAsync(
              chatId: Properties.Settings.Default.tgChatId,
              text: String.Format("* {0} *\r\n{1}", title, content),
              parseMode: ParseMode.Markdown
            );
            Logger.InfoFormat("NotifyTelegram notifySMS result: {0}", m);
        }
    }
}