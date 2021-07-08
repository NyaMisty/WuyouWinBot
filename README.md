# 无忧行 Windows 客户端

## 使用方法

在WuyouWinBot.exe.config中填入正确的各种参数，然后用命令：
```
WuyouWinBot.exe +8613800138000 123456
```
登录即可通过微信和Telegram接受提醒。

## 原理

无忧行部分魔改了菊风SDK，官方的sdk接入无忧行服务器只能接打电话，不能收到短信。

通过patch官方提供的C# SDK，实现了收短信的回调，这里共享idb供其他人分析。

此程序还可通过wine运行。