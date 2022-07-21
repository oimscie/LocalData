using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalData.OrderMessage
{
    /// <summary>
    /// 消息类型
    /// </summary>
    public struct OrderMessageType
    {
        /// <summary>
        /// 音视频请求
        /// </summary>
        public const string AudioAndVideo = "1000";
        /// <summary>
        /// 音视频控制
        /// </summary>
        public const string AudioAndVideoControl = "1001";
        /// <summary>
        /// 车载历史音视频请求
        /// </summary>
        public const string HisVideoAndAudio = "2000";
        /// <summary>
        /// 车载历史音视频控制请求
        /// </summary>
        public const string HisVideoAndAudioControl = "2001";
        /*  /// <summary>
          /// 车载实时视频请求
          /// </summary>
          public const int video = 2100;
          /// <summary>
          /// 车载实时视频控制请求
          /// </summary>
          public const int videoControl = 2101;*/
        /// <summary>
        /// 客户端登录
        /// </summary>
        public const string ClientLogin = "3000";
        /// <summary>
        /// 客户端心跳
        /// </summary>
        public const string ClientHeart = "3001";
        /// <summary>
        /// 用户本地数据终端心跳
        /// </summary>
        public const string LocalHeart = "4000";
        /// <summary>
        /// 本地数据终端登录
        /// </summary>
        public const string LocalLogin = "4001";
        /// <summary>
        /// 客户端打开监控请求
        /// </summary>
        public const string MonitorOpen = "5000";
        /// <summary>
        /// 客户端监控视频控制指令
        /// </summary>
        public const string MonitorControl = "5001";
        /// <summary>
        /// 本地数据终端监控视频上传请求
        /// </summary>
        public const string MonitorUpload = "5100";
    }
    /// <summary>
    /// 监控操作类型
    /// </summary>
    public struct MonitorOperationType
    {
        /// <summary>
        /// 上
        /// </summary>
        public const string up = "1";
        /// <summary>
        /// 下
        /// </summary>
        public const string down = "2";
        /// <summary>
        /// 左
        /// </summary>
        public const string left = "3";
        /// <summary>
        /// 右
        /// </summary>
        public const string right = "4";
        /// <summary>
        /// 放大
        /// </summary>
        public const string amplification = "5";
        /// <summary>
        /// 缩小
        /// </summary>
        public const string narrow = "6";
        /// <summary>
        /// 前移
        /// </summary>
        public const string forward = "7";
        /// <summary>
        /// 后移
        /// </summary>
        public const string back = "8";
    }
    /// <summary>
    /// 操作类型
    /// </summary>
    public struct StartOrStop
    {
        /// <summary>
        /// 开始
        /// </summary>
        public const string start = "1";
        /// <summary>
        /// 停止
        /// </summary>
        public const string Stop = "0";
    }
}
