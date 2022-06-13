using LocalData.CHCNETSDK;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LocalData.staticResouce
{
    public class Resource
    {
        /// <summary>
        /// opc连接状态
        /// </summary>
        public static bool opc_connected = true;
        /// <summary>
        /// 全局启动运行标志
        /// </summary>
        public static bool IsStart = false;
        /// <summary>
        /// 将计入服务器的过磅记录
        /// </summary>
        public static ConcurrentQueue<Dictionary<string, string>>  insertDb = new ConcurrentQueue<Dictionary<string, string>>();
        public static ConcurrentDictionary<string, LocalPlay> videoDic = new ConcurrentDictionary<string, LocalPlay>();
    }
}
