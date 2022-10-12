using LocalData.MySql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace LocalData.CHCNETSDK
{
    public class VideoJpg
    {
        private readonly string Company;
        private readonly string ip = ConfigurationManager.AppSettings["ServerIp"];
        private readonly int port = 8092;
        private readonly byte[] mark;
        private Socket client;
        private readonly MySqlHelper mysql;
        private List<Dictionary<string, string>> dic;
        private bool IsUpdate = false;

        public VideoJpg()
        {
            Company = ConfigurationManager.AppSettings["Company"];
            mark = new byte[] { 11, 22, 33, 44 };
            mysql = new MySqlHelper();

            Thread Thread2 = new Thread(UpdateJpgTimer)
            {
                IsBackground = true
            };
            Thread2.Start();
        }

        private void UpdateJpgTimer()
        {
            System.Timers.Timer UpdateJpgTimer = new System.Timers.Timer(1000 * 30);
            UpdateJpgTimer.Elapsed += new ElapsedEventHandler(UpdateJpg);
            UpdateJpgTimer.AutoReset = true;
            UpdateJpgTimer.Enabled = true;
        }

        /// <summary>
        /// 更新监控预览图
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void UpdateJpg(object source, ElapsedEventArgs e)
        {
            if (!IsUpdate)
            {
                IsUpdate = true;
                string sql = "select NAME,IP,PORT,USERNAME,PASSWORD,BRAND from list_monitor where COMPANY='" + Company + "'";
                dic = mysql.MultipleSelect(sql, new List<string>() { "NAME", "IP", "PORT", "USERNAME", "PASSWORD", "BRAND" });
                foreach (var item in dic)
                {
                    LocalPlay Local = new LocalPlay(item["IP"], item["PORT"], item["USERNAME"], item["PASSWORD"]);
                    if (FileUtils.DirExit(@"D:/localData/", true))
                    {
                        if (Local.GetJpg(@"D:/localData/" + item["NAME"] + ".jpg"))
                        {
                            SendFile(@"D:/localData/" + item["NAME"] + ".jpg");
                        }
                    }
                }
                IsUpdate = false;
            }
        }

        /// <summary>
        /// 发送文件
        /// </summary>
        /// <param name="userName"></param>
        private void SendFile(string path)
        {
            FileStream EzoneStream = null;
            try
            {
                FileInfo EzoneFile = new FileInfo(path);
                EzoneStream = EzoneFile.OpenRead();
                //包的大小
                int packetSize = 1000;
                //包的数量
                int packetCount = (int)(EzoneFile.Length / ((long)packetSize));
                //最后一个包的大小
                int lastPacketData = (int)(EzoneFile.Length - ((long)packetSize * packetCount));
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(IPAddress.Parse(ip), Convert.ToInt32(port));
                string info = Company + "!" + EzoneFile.Name + "!" + EzoneFile.Length;
                client.Send(Encoding.UTF8.GetBytes(info).Concat(mark).ToArray());
                byte[] data = new byte[packetSize];
                for (int i = 0; i < packetCount; i++)
                {
                    EzoneStream.Read(data, 0, packetSize);
                    client.Send(data.Concat(mark).ToArray());
                }
                if (lastPacketData != 0)
                {
                    data = new byte[lastPacketData];
                    EzoneStream.Read(data, 0, lastPacketData);
                    client.Send(data.Concat(mark).ToArray());
                }
                EzoneStream.Close();
            }
            catch (Exception e)
            {
                LogHelper.WriteLog("文件传输错误", e);
            }
            finally
            {
                if (EzoneStream != null)
                {
                    EzoneStream.Close();
                }
                client.Close();
                client.Dispose();
                File.Delete(path);
            }
        }
    }
}