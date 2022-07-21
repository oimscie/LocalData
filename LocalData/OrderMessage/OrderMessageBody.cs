using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalData.OrderMessage
{
    /// <summary>
    /// 实时音视频请求
    /// </summary>
    public struct AudioAndVideo
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public string messageType;
        /// <summary>
        /// 逻辑通道号
        /// </summary>
        public string id;
        /// <summary>
        /// 数据类型（音视频类型）0：音视频，1：视频，2：双向对讲，3：监听，4中心广播，5：透传
        /// </summary>
        public string datatype;
        /// <summary>
        /// 码流类型
        /// </summary>
        public string datatypes;
        /// <summary>
        /// 终端SIM
        /// </summary>
        public string sim;
        /// <summary>
        /// 终端1078版本
        /// </summary>
        public string version1078;
    }
    /// <summary>
    /// 实时音视频控制请求
    /// </summary>
    public struct AudioAndVideoControl
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public string messageType;
        /// <summary>
        /// 逻辑通道号
        /// </summary>
        public string id;
        /// <summary>
        /// 控制指令
        /// </summary>
        public string order;
        /// <summary>
        /// 操作类型
        /// </summary>
        public string type;
        /// <summary>
        /// 终端SIM
        /// </summary>
        public string sim;
        /// <summary>
        /// 码流类型
        /// </summary>
        public string datatypes;
    }
    /// <summary>
    /// 车载历史音视频请求请求
    /// </summary>
    public struct HisVideoAndAudio
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public string messageType;
        /// <summary>
        /// 逻辑通道号
        /// </summary>
        public string id;
        /// <summary>
        /// 音视频类型 0：音视频，1：音频，2：视频，3：视频或音频
        /// </summary>
        public string datatype;
        /// <summary>
        /// 码流类型(0：主码流或子码流 1:主码流 2:子码流，如果传输音频，置0)
        /// </summary>
        public string datatypes;
        /// <summary>
        /// 回放方式
        /// </summary>
        public string ReviewType;
        /// <summary>
        /// 快进或快退倍数
        /// </summary>
        public string FastOrSlow;
        /// <summary>
        /// 开始时间
        /// </summary>
        public string StartTime;
        /// <summary>
        /// 结束时间
        /// </summary>
        public string OverTime;
        /// <summary>
        /// 终端SIM
        /// </summary>
        public string sim;
        /// <summary>
        /// 终端1078版本
        /// </summary>
        public string version1078;
    }
    /// <summary>
    /// 车载历史音视频控制请求
    /// </summary>
    public struct HisVideoAndAudioControl
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public string messageType;
        /// <summary>
        /// 逻辑通道号
        /// </summary>
        public string id;
        /// <summary>
        ///回放控制
        /// </summary>
        public string type;
        /// <summary>
        /// 快进快退倍数
        /// </summary>
        public string order;
        /// <summary>
        /// 拖动回放时间
        /// </summary>
        public string time;
        /// <summary>
        /// 终端SIM
        /// </summary>
        public string sim;
    }
    /// <summary>
    /// 客户端登录
    /// </summary>
    public struct ClientLogin
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public string messageType;
        /// <summary>
        /// uuid
        /// </summary>
        public string uuid;
        /// <summary>
        /// 类型
        /// </summary>
        public string type;
    }
    /// <summary>
    /// 客户端心跳
    /// </summary>
    public struct ClientHeart
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public string messageType;
    }
    /// <summary>
    /// 用户本地数据终端心跳
    /// </summary>
    public struct LocalHeart
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public string messageType;
    }
    /// <summary>
    /// 本地数据终端上报所属公司
    /// </summary>
    public struct LocalLogin
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public string messageType;
        /// <summary>
        /// 所属公司
        /// </summary>
        public string Company;
    }
    /// <summary>
    /// 客户端打开监控请求
    /// </summary>
    public struct MonitorOpen
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public string messageType;
        /// <summary>
        /// 所属公司
        /// </summary>
        public string Company;
        /// <summary>
        /// ip
        /// </summary>
        public string CameraIP;
        /// <summary>
        /// port
        /// </summary>
        public string CameraPort;
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName;
        /// <summary>
        /// 登录密码
        /// </summary>
        public string Password;
        /// <summary>
        /// 品牌
        /// </summary>
        public string Brand;
    }
    /// <summary>
    /// 用户本地端上传监控视频流请求
    /// </summary>
    public struct MonitorUpload
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public string messageType;
        /// <summary>
        /// 所属公司
        /// </summary>
        public string Company;
        /// <summary>
        /// ip
        /// </summary>
        public string CameraIP;
        /// <summary>
        /// port
        /// </summary>
        public string CameraPort;
        /// <summary>
        /// 品牌
        /// </summary>
        public string Brand;
    }
    /// <summary>
    /// 客户端控制监控请求
    /// </summary>
    public struct MonitorControl
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public string messageType;
        /// <summary>
        /// 操作类型
        /// </summary>
        public string OperationType;
        /// <summary>
        /// 启停（0-1）
        /// </summary>
        public string StartOrStop;

    }
}
