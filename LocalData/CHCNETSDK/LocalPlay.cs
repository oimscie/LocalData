using LocalData.staticResouce;
using LocalData.SuperSocket;
using SuperSocket.ClientEngine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace LocalData.CHCNETSDK
{
    public class LocalPlay
    {
        public string CameraIp { get; set; }
        public string CameraPort { get; set; }
        public string UserName { get; set; }
        public string PassWord { get; set; }
        public string Brand { get; set; }
        public AsyncTcpSession Sockets { get; set; }


        public Socket Socket { get; set; }


        public int M_lUserID { get; set; }
        public int M_lRealHandle { get; set; }
        public int ChannelId { get; set; }
        public string Userinfo { get; set; }
        private ConcurrentQueue<byte[]> RealData;
        private bool IsSend = true;
        private readonly byte[] mark = new byte[] { 11, 22, 33, 44 };
        private CHCNetSDK.REALDATACALLBACK RealDataCallBack = null;
        public RealVideoDataConnect Connect;
        public readonly string key;
        public bool NET_DVR_Init()
        {
            return CHCNetSDK.NET_DVR_Init();
        }
        public LocalPlay(string[] info, RealVideoDataConnect Connects, string keys)
        {
            key = keys;
            Connect = Connects;
            Sockets = Connect.GetClient();
            Userinfo = info[1];
            CameraIp = info[2];
            CameraPort = info[3];
            UserName = info[4];
            PassWord = info[5];
            Brand = info[6];
            M_lUserID = -1;
            M_lRealHandle = -1;
            ChannelId = 1;
        }
        public LocalPlay(string ip, string port, string username, string password) {
            CameraIp = ip;
            CameraPort = port;
            UserName = username;
            PassWord = password;
            M_lUserID = -1;
            M_lRealHandle = -1;
            ChannelId = 1;
        }
        private bool Login()
        {
            CHCNetSDK.NET_DVR_DEVICEINFO_V30 DeviceInfo = new CHCNetSDK.NET_DVR_DEVICEINFO_V30();
            M_lUserID = CHCNetSDK.NET_DVR_Login_V30(CameraIp, int.Parse(CameraPort), UserName, PassWord, ref DeviceInfo);
            if (M_lUserID < 0)
            {
                LogHelper.WriteLog("登录错误"+CHCNetSDK.NET_DVR_GetLastError().ToString());
                return false;
            }
            else
            {
                for (int i = 0; i < DeviceInfo.byChanNum; i++)
                {
                    ChannelId = DeviceInfo.byStartChan;
                }
                return true;
            }
        }
        public void LoginOut()
        {
            CHCNetSDK.NET_DVR_Logout(M_lUserID);
        }
        public void LivePlay()
        {
            try
            {
                if (!NET_DVR_Init())
                {
                    LogHelper.WriteLog(CHCNetSDK.NET_DVR_GetLastError().ToString());
                    return;
                }
                NET_DVR_Init();
                RealData = new ConcurrentQueue<byte[]>();
                if (!Login()){return;} 
                Thread thread = new Thread(SendRealData)
                {
                    IsBackground = true
                };
                thread.Start();
                CHCNetSDK.NET_DVR_PREVIEWINFO lpPreviewInfo = new CHCNetSDK.NET_DVR_PREVIEWINFO
                {
                    hPlayWnd = (IntPtr)null,
                    lChannel = ChannelId,
                    dwStreamType = 0,
                    dwLinkMode = 0,
                    bBlocked = false,
                    dwDisplayBufNum = 60
                };
                if (RealDataCallBack == null)
                {
                    RealDataCallBack = new CHCNetSDK.REALDATACALLBACK(RealDataCallBackS);//预览实时流回调函数
                }
                IntPtr pUser = Marshal.StringToHGlobalAnsi(Userinfo);
                M_lRealHandle = CHCNetSDK.NET_DVR_RealPlay_V40(M_lUserID, ref lpPreviewInfo, RealDataCallBack, pUser);
                if (M_lRealHandle < 0)
                {
                    LogHelper.WriteLog(CHCNetSDK.NET_DVR_GetLastError().ToString());
                    return;
                }
            }
            catch (Exception e)
            {
                LogHelper.WriteLog("监控错误---", e);
            }
        }
        /// <summary>
        /// 截图
        /// </summary>
        /// <param name="FileName">图片名称</param>
        /// <returns></returns>
        public bool GetJpg(string FileName)
        {
            if (!NET_DVR_Init())
            {
                LogHelper.WriteLog(CHCNetSDK.NET_DVR_GetLastError().ToString());
                return false;
            }
            if (!Login()) {
                return false;
            }
            CHCNetSDK.NET_DVR_JPEGPARA lpJpegPara = new CHCNetSDK.NET_DVR_JPEGPARA
            {
                wPicQuality = 0, //图像质量 Image quality
                wPicSize = 0xff //抓图分辨率 Picture size: 0xff-Auto(使用当前码流分辨率) 
            };
            //JPEG抓图保存成文件 Capture a JPEG picture
            if (!CHCNetSDK.NET_DVR_CaptureJPEGPicture(M_lUserID, ChannelId, ref lpJpegPara, FileName))
            {
                LogHelper.WriteLog("截取错误"+CHCNetSDK.NET_DVR_GetLastError().ToString());
                LoginOut();
                return false;
            }
            LoginOut();
            return true;
        }
        public void RealDataCallBackS(Int32 lRealHandle, UInt32 dwDataType, IntPtr pBuffer, UInt32 dwBufSize, IntPtr pUser)
        {
            if (IsSend && dwBufSize > 0)
            {
                byte[] sData = new byte[dwBufSize];
                Marshal.Copy(pBuffer, sData, 0, (int)dwBufSize);
                RealData.Enqueue(sData);
            }
        }
        public void StopPlay()
        {
            IsSend = false;
            Resource.videoDic.TryRemove(key, out _);
            if (M_lRealHandle >= 0)
            {
                CHCNetSDK.NET_DVR_StopRealPlay(M_lRealHandle);
                LoginOut();
            }
        }
        private void SendRealData()
        {
            while (IsSend)
            {
                try
                {
                    if (RealData.Count > 0)
                    {
                        if (RealData.TryDequeue(out byte[] temp))
                        {
                            byte[] Realdata = temp.Concat(mark).ToArray();
                            Connect.Send(Realdata);
                        };
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
                catch (Exception e)
                {
                    Connect.client_Closed(null, null);
                    LogHelper.WriteLog("视频发送错误", e);
                }
            }
        }
        public bool PTZControl(string[] info)
        {
            switch (info[0])
            {
                case "stop":
                    StopPlay();
                    return false;
                case "up":
                    switch (info[1])
                    {
                        case "0":
                            CHCNetSDK.NET_DVR_PTZControlWithSpeed_Other(M_lUserID, ChannelId, CHCNetSDK.TILT_UP, 0, 7);
                            return false;
                        case "1":
                            CHCNetSDK.NET_DVR_PTZControlWithSpeed_Other(M_lUserID, ChannelId, CHCNetSDK.TILT_UP, 1, 7);
                            return false;
                    }
                    return false;
                case "down":
                    switch (info[1])
                    {
                        case "0":
                            CHCNetSDK.NET_DVR_PTZControlWithSpeed_Other(M_lUserID, ChannelId, CHCNetSDK.TILT_DOWN, 0, 7);
                            return false;
                        case "1":
                            CHCNetSDK.NET_DVR_PTZControlWithSpeed_Other(M_lUserID, ChannelId, CHCNetSDK.TILT_DOWN, 1, 7);
                            return false;
                    }
                    return false;
                case "left":
                    switch (info[1])
                    {
                        case "0":
                            CHCNetSDK.NET_DVR_PTZControlWithSpeed_Other(M_lUserID, ChannelId, CHCNetSDK.PAN_LEFT, 0, 7);
                            return false;
                        case "1":
                            CHCNetSDK.NET_DVR_PTZControlWithSpeed_Other(M_lUserID, ChannelId, CHCNetSDK.PAN_LEFT, 1, 7);
                            return false;
                    }
                    return false;
                case "right":
                    switch (info[1])
                    {
                        case "0":
                            CHCNetSDK.NET_DVR_PTZControlWithSpeed_Other(M_lUserID, ChannelId, CHCNetSDK.PAN_RIGHT, 0, 7);
                            return false;
                        case "1":
                            CHCNetSDK.NET_DVR_PTZControlWithSpeed_Other(M_lUserID, ChannelId, CHCNetSDK.PAN_RIGHT, 1, 7);
                            return false;
                    }
                    return false;
                case "amplification":
                    switch (info[1])
                    {
                        case "0":
                            CHCNetSDK.NET_DVR_PTZControlWithSpeed_Other(M_lUserID, ChannelId, CHCNetSDK.FOCUS_NEAR, 0, 7);
                            return false;
                        case "1":
                            CHCNetSDK.NET_DVR_PTZControlWithSpeed_Other(M_lUserID, ChannelId, CHCNetSDK.FOCUS_NEAR, 1, 7);
                            return false;
                    }
                    return false;
                case "narrow":
                    switch (info[1])
                    {
                        case "0":
                            CHCNetSDK.NET_DVR_PTZControlWithSpeed_Other(M_lUserID, ChannelId, CHCNetSDK.FOCUS_FAR, 0, 7);
                            return false;
                        case "1":
                            CHCNetSDK.NET_DVR_PTZControlWithSpeed_Other(M_lUserID, ChannelId, CHCNetSDK.FOCUS_FAR, 1, 7);
                            return false;
                    }
                    return false;
                case "forward":
                    switch (info[1])
                    {
                        case "0":
                            CHCNetSDK.NET_DVR_PTZControlWithSpeed_Other(M_lUserID, ChannelId, CHCNetSDK.ZOOM_IN, 0, 7);
                            return false;
                        case "1":
                            CHCNetSDK.NET_DVR_PTZControlWithSpeed_Other(M_lUserID, ChannelId, CHCNetSDK.ZOOM_IN, 1, 7);
                            return false;
                    }
                    return false;
                case "back":
                    switch (info[1])
                    {
                        case "0":
                            CHCNetSDK.NET_DVR_PTZControlWithSpeed_Other(M_lUserID, ChannelId, CHCNetSDK.ZOOM_OUT, 0, 7);
                            return false;
                        case "1":
                            CHCNetSDK.NET_DVR_PTZControlWithSpeed_Other(M_lUserID, ChannelId, CHCNetSDK.ZOOM_OUT, 1, 7);
                            return false;
                    }
                    return false;
                default:
                    return true;
            }
        }
    }
}
