﻿using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace Gap
{

    public class Program
    {
        internal static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static IrcBot IrcBot { get; set; }
        public static XmppBot XmppBot { get; set; }
        public static WebhookQueueProcessor WebhookQueueProcessor { get; set; }
        private static bool KeepRunning = true;
        static void Main(string[] args)
        {
            Run().Wait();
        }

        static async Task Run()
        {
            IrcBot = new IrcBot();
            IrcBot.Start();
            await TenSeconds();
            XmppBot = new XmppBot(IrcBot);
            XmppBot.Start();
            await TenSeconds();
            WebhookQueueProcessor = new WebhookQueueProcessor();
            WebhookQueueProcessor.Start();

            if (IrcBot.IrcClient.IsConnected)
            {
                MessageQueue.Get().Add(new MessageItem("SYSTEM", string.Format("OCTGN Gap v{0} Reporting for Duty", typeof(Program).Assembly.GetName().Version), Destination.IrcOctgn));
                MessageQueue.Get().Add(new MessageItem("SYSTEM", string.Format("OCTGN Gap v{0} Reporting for Duty", typeof(Program).Assembly.GetName().Version), Destination.IrcOctgnDev));
                MessageQueue.Get().Add(new MessageItem("SYSTEM", string.Format("OCTGN Gap v{0} Reporting for Duty", typeof(Program).Assembly.GetName().Version), Destination.IrcOctgnLobby));
            }

            await Task.Factory.StartNew(() =>
            {
                while (Console.KeyAvailable == false && KeepRunning)
                {
                    Thread.Sleep(1000);
                }
            });
        }

        static async Task TenSeconds()
        {
            await Task.Factory.StartNew(() => Thread.Sleep(10000));
        }

        public static void Close()
        {
            try
            {
                WebhookQueueProcessor.Dispose();
                MessageQueue.Get().Stop();
                IrcBot.Stop();
                XmppBot.Stop();
                PythonEngine.Get().Close();
            }
            catch (Exception e)
            {
                Log.Error("Close",e);
            } 
            KeepRunning = false;
        }
    }
}
