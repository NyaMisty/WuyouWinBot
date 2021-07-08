using System;
using log4net;
using Senparc.CO2NET;
using Senparc.CO2NET.RegisterServices;
using Senparc.Weixin;
using Senparc.Weixin.Entities;
using Senparc.Weixin.Entities.TemplateMessage;
using Senparc.Weixin.MP.AdvancedAPIs.TemplateMessage;
using Senparc.Weixin.MP.Containers;

namespace WuyouWinBot.Notify
{
    public class WeixinTemplate : TemplateMessageBase
    {

        public TemplateDataItem title { get; set; }
        /// <summary>
        /// Time
        /// </summary>
        public TemplateDataItem content { get; set; }


        public WeixinTemplate(string url, string title, string content)
            : base(Properties.Settings.Default.wxTemplateId, url, "通知")
        {
            this.title = new TemplateDataItem(title);
            this.content = new TemplateDataItem(content);
        }
    }
    class NotifyWeixin : NotifyI
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));
        public NotifyWeixin()
        {

            var isGLobalDebug = false;//设置全局 Debug 状态
            var senparcSetting = SenparcSetting.BuildFromWebConfig(isGLobalDebug);
            var register = RegisterService.Start(senparcSetting).UseSenparcGlobal();//CO2NET全局注册，必须！

            var isWeixinDebug = false;//设置微信 Debug 状态
            var senparcWeixinSetting = SenparcWeixinSetting.BuildFromWebConfig(isWeixinDebug);
            register.UseSenparcWeixin(senparcWeixinSetting, senparcSetting);////微信全局注册，必须！

            AccessTokenContainer.Register(Properties.Settings.Default.wxAppId, Properties.Settings.Default.wxAppSecret);
        }
        public async void notifyCall(string user, string from, DateTime time)
        {
            Logger.InfoFormat("Sending weixin notifyCall! user: {0}, from: {1}, time: {2}", user, from, time);
            var templateData = new WeixinTemplate("https://misty.moe", "无忧行 " + user + " 接到电话：" + from, "呼叫时间：" + time);
            var result = await Senparc.Weixin.MP.AdvancedAPIs.TemplateApi.SendTemplateMessageAsync(Properties.Settings.Default.wxAppId, Properties.Settings.Default.wxOpenId, templateData);
            Logger.InfoFormat("NotifyWeixin notifyCall result: {0}", result);
        }

        public async void notifySMS(string user, string from, DateTime time, string message)
        {
            Logger.InfoFormat("Sending weixin notifySMS! user: {0}, from: {1}, time: {2}, message: {3}", user, from, time, message);
            var templateData = new WeixinTemplate("https://misty.moe", "无忧行 " + user + " 收到短信：" + from, "短信内容：" + message);
            var result = await Senparc.Weixin.MP.AdvancedAPIs.TemplateApi.SendTemplateMessageAsync(Properties.Settings.Default.wxAppId, Properties.Settings.Default.wxOpenId, templateData);
            Logger.InfoFormat("NotifyWeixin notifySMS result: {0}", result);
        }
    }
}