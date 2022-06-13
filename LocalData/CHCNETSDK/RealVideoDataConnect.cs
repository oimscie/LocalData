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

namespace LocalData.SuperSocket
{
    public class RealVideoDataConnect
    {
        private readonly string Company;
        private readonly AsyncTcpSession client;
        private readonly string ip = ConfigurationManager.AppSettings["ServerIp"];
        private readonly int port = 8091;
        public string CameraIp { get; set; }
        public string CameraPort { get; set; }
        public string Brand { get; set; }
        public LocalPlay LocalPlay = null;
        public RealVideoDataConnect(string[] Infos)
        {
            Company = ConfigurationManager.AppSettings["Company"];
            CameraIp = Infos[2];
            CameraPort = Infos[3];
            Brand = Infos[6];
            client = new AsyncTcpSession();
            // 连接断开事件
            client.Closed += client_Closed;
            // 收到服务器数据事件
            client.DataReceived += client_DataReceived;
            // 连接到服务器事件
            client.Connected += client_Connected;
            // 发生错误的处理
            client.Error += client_Error;
        }
        public AsyncTcpSession GetClient()
        {
            return client;
        }
        void client_Error(object sender, ErrorEventArgs e)
        {
            FormUtil.ModifyLable(DataForm.MainForm.Video, "断开",Color.Green);
            LogHelper.WriteLog("视频服务传输错误", e.Exception);
            LocalPlay.StopPlay();
        }

        void client_Connected(object sender, EventArgs e)
        {
            string str = "monitorUpload!" + Company + "!" + CameraIp + "!" + CameraPort + "!0!0!" + 0 + "!";
            Send(Encoding.UTF8.GetBytes(str).Concat(new byte[] { 11, 22, 33, 44 }).ToArray());
            FormUtil.ModifyLable(DataForm.MainForm.Video, "已连接", Color.Green);
        }

        void client_DataReceived(object sender, DataEventArgs e)
        {
            string[] Info = Encoding.UTF8.GetString(e.Data).Split('!');
            switch (Info[0])
            {
                case "Control":
                    LocalPlay.PTZControl(Info[1].Split(','));
                    break;
            }          
        }

        public void client_Closed(object sender, EventArgs e)
        {
            FormUtil.ModifyLable(DataForm.MainForm.Video, "未传输", Color.Green);
            LocalPlay.StopPlay();
        }

        /// <summary>
        /// 连接到服务器
        /// </summary>
        public void Connect()
        {
            try
            {
                client.Connect(new IPEndPoint(IPAddress.Parse(ip), port));
            }
            catch (Exception e)
            {
                LogHelper.WriteLog("视频服务连接错误", e);
                FormUtil.ModifyLable(DataForm.MainForm.Video, "错误", Color.Red);
            }
        }

        /// <summary>
        /// 向服务器发命令行协议的数据
        /// </summary>
        public void Send(byte[] data)
        {
            if (client.IsConnected)
            {
                client.Send(data, 0, data.Length);
            }
        }

    }
}
