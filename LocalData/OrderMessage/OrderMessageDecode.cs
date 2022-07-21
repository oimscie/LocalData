using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalData.OrderMessage
{
    /// <summary>
    /// 消息解包
    /// </summary>
    public class OrderMessageDecode
    {
        private readonly Encoding encoding;
        public OrderMessageDecode()
        {
            encoding = Encoding.UTF8;
        }
        /// <summary>
        /// 获取消息头
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public string GetMessageHead(byte[] buffer)
        {
            return encoding.GetString(buffer).Trim('$').Split('!')[0];
        }
        /// <summary>
        /// 音视频请求解包
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public AudioAndVideo AudioAndVideo(byte[] buffer)
        {
            string[] array = encoding.GetString(buffer).Trim('$').Split('!');
            return new AudioAndVideo()
            {
                messageType = OrderMessageType.AudioAndVideo,
                sim = array[1],
                datatype = array[2],
                id = array[3],
                datatypes = array[4],
                version1078 = array[5]
            };
        }
        /// <summary>
        /// 车载历史音视频请求解包
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public HisVideoAndAudio HisVideoAndAudio(byte[] buffer)
        {
            string[] array = encoding.GetString(buffer).Trim('$').Split('!');
            return new HisVideoAndAudio()
            {
                messageType = OrderMessageType.HisVideoAndAudio,
                sim = array[1],
                datatype = array[2],
                StartTime = array[3],
                OverTime = array[4],
                id = array[5],
                datatypes = array[6],
                version1078 = array[7],
                ReviewType = array[8],
                FastOrSlow = array[9]
            };
        }
        /// <summary>
        /// 客户端登录解包
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public ClientLogin ClientLogin(byte[] buffer)
        {
            string[] array = encoding.GetString(buffer).Trim('$').Split('!');
            return new ClientLogin()
            {
                messageType = OrderMessageType.ClientLogin,
                uuid = array[1],
                type = array[2]
            };
        }
        /// <summary>
        /// 客户端心跳解包
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public ClientHeart ClientHeart(byte[] buffer)
        {
            return new ClientHeart()
            {
                messageType = OrderMessageType.ClientHeart,
            };
        }
        /// <summary>
        /// 用户本地数据终端心跳解包
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public LocalHeart LocalHeart(byte[] buffer)
        {
            return new LocalHeart()
            {
                messageType = OrderMessageType.LocalHeart,
            };
        }
        /// <summary>
        /// 本地数据终端上报所属公司解包
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public LocalLogin LocalLogin(byte[] buffer)
        {
            string[] array = encoding.GetString(buffer).Split('!');
            return new LocalLogin()
            {
                messageType = OrderMessageType.LocalLogin,
                Company = array[1]
            };
        }
        /// <summary>
        /// 客户端打开监控请求解包
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public MonitorOpen MonitorOpen(byte[] buffer)
        {
            string[] array = encoding.GetString(buffer).Trim('$').Split('!');
            return new MonitorOpen()
            {
                messageType = OrderMessageType.MonitorOpen,
                Company = array[1],
                CameraIP = array[2],
                CameraPort = array[3],
                UserName = array[4],
                Password = array[5],
                Brand = array[6]
            };
        }
        /// <summary>
        /// 客户端监控视频控制指令解包
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public MonitorControl MonitorControl(byte[] buffer)
        {
            string[] array = encoding.GetString(buffer).Trim('$').Split('!');
            return new MonitorControl()
            {
                messageType = OrderMessageType.MonitorControl,
                OperationType = array[1],
                StartOrStop = array[2]
            };
        }
        /// <summary>
        /// 本地数据终端监控视频上传请求解包
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public MonitorUpload MonitorUpload(byte[] buffer)
        {
            string[] array = encoding.GetString(buffer).Trim('$').Split('!');
            return new MonitorUpload()
            {
                messageType = OrderMessageType.MonitorUpload,
                Company = array[1],
                CameraIP = array[2],
                CameraPort = array[3],
                Brand = array[4]
            };
        }
    }
}
