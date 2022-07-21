using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalData.OrderMessage
{
    /// <summary>
    /// 封包
    /// </summary>
    public class PacketForm
    {
        private readonly Encoding encoding;
        private readonly string Separator;
        private readonly string Mark;
        private readonly byte[] Mark2;
        public PacketForm()
        {
            encoding = Encoding.UTF8;
            Separator = "!";
            Mark = "$";
            Mark2 = new byte[] { 11, 22, 33, 44 };
        }
        /// <summary>
        /// 视频请求封包
        /// </summary>
        /// <param name="AudioAndVideo"></param>
        /// <returns></returns>
        public byte[] Video(AudioAndVideo AudioAndVideo)
        {
            return encoding.GetBytes(Mark + AudioAndVideo.messageType + Separator + AudioAndVideo.sim + Separator + AudioAndVideo.datatype + Separator + AudioAndVideo.id + Separator + AudioAndVideo.datatypes + Separator + AudioAndVideo.version1078 + Mark);
        }

        /// <summary>
        /// 音频请求封包
        /// </summary>
        /// <param name="AudioAndVideo"></param>
        /// <returns></returns>
        public byte[] Audio(AudioAndVideo AudioAndVideo)
        {
            return encoding.GetBytes(AudioAndVideo.messageType + Separator + AudioAndVideo.sim + Separator + AudioAndVideo.datatype + Separator + AudioAndVideo.id + Separator + AudioAndVideo.datatypes + Separator + AudioAndVideo.version1078).Concat(Mark2).ToArray();
        }
        /// <summary>
        /// 车载历史视频请求封包
        /// </summary>
        /// <param name="HisVideo"></param>
        /// <returns></returns>
        public byte[] HisVideo(HisVideoAndAudio HisVideo)
        {
            return encoding.GetBytes(Mark + HisVideo.messageType + Separator + HisVideo.sim + Separator + HisVideo.datatype + Separator + HisVideo.StartTime + Separator + HisVideo.OverTime + Separator + HisVideo.id + Separator + HisVideo.datatypes + Separator + HisVideo.version1078 + Separator + HisVideo.ReviewType + Separator + HisVideo.FastOrSlow + Mark);
        }
        /// <summary>
        /// 车载历史音频请求封包
        /// </summary>
        /// <param name="HisVideo"></param>
        /// <returns></returns>
        public byte[] HisAudio(HisVideoAndAudio HisAudio)
        {
            return encoding.GetBytes(HisAudio.messageType + Separator + HisAudio.sim + Separator + HisAudio.datatype + Separator + HisAudio.StartTime + Separator + HisAudio.OverTime + Separator + HisAudio.id + Separator + HisAudio.datatypes + Separator + HisAudio.version1078 + Separator + HisAudio.ReviewType + Separator + HisAudio.FastOrSlow).Concat(Mark2).ToArray();
        }


        /// <summary>
        /// 客户端登录封包
        /// </summary>
        /// <param name="Login"></param>
        /// <returns></returns>
        public byte[] ClientLogin(ClientLogin Login)
        {
            return encoding.GetBytes(Mark + Login.messageType + Separator + Login.uuid + Separator + Login.type + Mark);
        }
        /// <summary>
        /// 客户端心跳封包
        /// </summary>
        /// <param name="ClientHeart"></param>
        /// <returns></returns>
        public byte[] ClientHeart(ClientHeart ClientHeart)
        {
            return encoding.GetBytes(Mark + ClientHeart.messageType + Mark);
        }
        /// <summary>
        /// 用户本地数据终端心跳封包
        /// </summary>
        /// <param name="LocalHeart"></param>
        /// <returns></returns>
        public byte[] LocalHeart(LocalHeart LocalHeart)
        {
            return encoding.GetBytes(LocalHeart.messageType).Concat(Mark2).ToArray();
        }
        /// <summary>
        /// 本地数据终端上报所属公司封包
        /// </summary>
        /// <param name="LocalLogin"></param>
        /// <returns></returns>
        public byte[] LocalLogin(LocalLogin LocalLogin)
        {
            return encoding.GetBytes(LocalLogin.messageType + Separator + LocalLogin.Company).Concat(Mark2).ToArray();
        }
        /// <summary>
        /// 客户端打开监控请求封包
        /// </summary>
        /// <param name="MonitorOpen"></param>
        /// <returns></returns>
        public byte[] MonitorOpen(MonitorOpen MonitorOpen)
        {
            return encoding.GetBytes(MonitorOpen.messageType + Separator + MonitorOpen.Company + Separator + MonitorOpen.CameraIP + Separator + MonitorOpen.CameraPort + Separator + MonitorOpen.UserName + Separator + MonitorOpen.Password + Separator + MonitorOpen.Brand).Concat(Mark2).ToArray();
        }
        /// <summary>
        /// 客户端监控视频控制指令封包
        /// </summary>
        /// <param name="MonitorControl"></param>
        /// <returns></returns>
        public byte[] MonitorControl(MonitorControl MonitorControl)
        {
            return encoding.GetBytes(MonitorControl.messageType + Separator + MonitorControl.OperationType + Separator + MonitorControl.StartOrStop).Concat(Mark2).ToArray();
        }
        /// <summary>
        /// 本地数据终端监控视频上传请求封包
        /// </summary>
        /// <param name="MonitorUpload"></param>
        /// <returns></returns>
        public byte[] MonitorUpload(MonitorUpload MonitorUpload)
        {
            return encoding.GetBytes(MonitorUpload.messageType + Separator + MonitorUpload.Company + Separator + MonitorUpload.CameraIP + Separator + MonitorUpload.CameraPort + Separator + MonitorUpload.Brand).Concat(Mark2).ToArray();
        }
    }
}
