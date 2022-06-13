using LocalData.CHCNETSDK;
using LocalData.staticResouce;
using SuperSocket.ClientEngine;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Timers;

namespace LocalData.SuperSocket
{
    public class OrderSocketClient
    {
        private bool IsConnecting = false;
        private readonly AsyncTcpSession client;
        private readonly string ip = ConfigurationManager.AppSettings["ServerIp"];
        private readonly int port = 8090;
        private readonly string info;
        public OrderSocketClient(string infos)
        {
            info = infos;
            client = new AsyncTcpSession();
            // 连接断开事件
            client.Closed += client_Closed;
            // 收到服务器数据事件
            client.DataReceived += client_DataReceived;
            // 连接到服务器事件
            client.Connected += client_Connected;
            // 发生错误的处理
            client.Error += client_Error;
            Thread ConnectThread = new Thread(Connect)
            {
                IsBackground = true
            };
            ConnectThread.Start();
            Thread heartThread = new Thread(heartTimers)
            {
                IsBackground = true
            };
            heartThread.Start();
        }
        public void heartTimers()
        {
            System.Timers.Timer dataSouth = new System.Timers.Timer(1000 * 90 );
            dataSouth.Elapsed += new ElapsedEventHandler(heart);
            dataSouth.AutoReset = true;
            dataSouth.Enabled = true;
        }
        void heart(object source, ElapsedEventArgs e)
        {
            Send(Encoding.UTF8.GetBytes("Heart!").Concat(new byte[] { 11, 22, 33, 44 }).ToArray()); ;
        }

        void client_Error(object sender, ErrorEventArgs e)
        {
            FormUtil.ModifyLable(DataForm.MainForm.Order, "断开", Color.Red);
            Connect();
        }

        void client_Connected(object sender, EventArgs e)
        {
            Send(Encoding.UTF8.GetBytes("Company!"+info).Concat(new byte[] { 11, 22, 33, 44 }).ToArray());
            FormUtil.ModifyLable(DataForm.MainForm.Order, "已连接", Color.Green);
        }

        void client_DataReceived(object sender, DataEventArgs e)
        {
            try
            {
                string[] Info = Encoding.UTF8.GetString(e.Data).Split('!');
                switch (Info[0])
                {
                    case "monitorOpen":
                        string key = DateTime.Now.ToString();
                        LocalPlay LocalPlay = new LocalPlay(Info, new RealVideoDataConnect(Info), key);
                        Resource.videoDic.TryAdd(key, LocalPlay);
                        LocalPlay.Connect.LocalPlay = LocalPlay;
                        LocalPlay.Connect.Connect();
                        LocalPlay.LivePlay();
                        break;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog("消息错误", ex);
            }
        }

        void client_Closed(object sender, EventArgs e)
        {
            FormUtil.ModifyLable(DataForm.MainForm.Order, "断开", Color.Red);
            Connect();
        }

        /// <summary>
        /// 连接到服务器
        /// </summary>
        public void Connect()
        {
            if (IsConnecting) {
                return;
            }
            IsConnecting = true;
            try
            {
                client.Connect(new IPEndPoint(IPAddress.Parse(ip), port));
                while (!client.IsConnected)
                {
                    FormUtil.ModifyLable(DataForm.MainForm.Order, "重连中", Color.Red);
                    Thread.Sleep(5000);
                    if (!client.IsConnected)
                    {
                        client.Connect(new IPEndPoint(IPAddress.Parse(ip), port));
                    }
                }

            }
            catch (Exception e)
            {
                LogHelper.WriteLog("指令服务连接错误", e);
            }
            finally {
                IsConnecting = false;
            }
        }

        /// <summary>
        /// 向服务器发命令行协议的数据
        /// </summary>
        public void Send(byte[] data)
        {
            try
            {
                if (client.IsConnected)
                {
                    client.Send(data, 0, data.Length);
                }
                else {
                    Connect();
                }
            }
            catch (Exception e)
            {
                LogHelper.WriteLog("发送错误", e);
            }
        }

    }
}
