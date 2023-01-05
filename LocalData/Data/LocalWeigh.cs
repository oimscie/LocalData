using LocalData.MySql;
using LocalData.staticResouce;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;

namespace LocalData.Data
{
    public class LocalWeigh
    {
        /// <summary>
        /// sql实体
        /// </summary>
        private readonly SqlHelper sql;

        /// <summary>
        /// 车辆编号字段
        /// </summary>
        private readonly string Carid;

        /// <summary>
        /// 净重字段
        /// </summary>
        private readonly string Weight;

        /// <summary>
        /// 时间字段
        /// </summary>
        private readonly string Time;

        /// <summary>
        /// 表名
        /// </summary>
        private readonly string Table;

        /// <summary>
        /// 上一次获取的过磅记录
        /// </summary>
        private Dictionary<string, string> previous;

        public LocalWeigh()
        {
            Carid = ConfigurationManager.AppSettings["InCarid"];
            Weight = ConfigurationManager.AppSettings["InWeight"];
            Time = ConfigurationManager.AppSettings["InTime"];
            Table = ConfigurationManager.AppSettings["DataTable"];
            previous = new Dictionary<string, string>
            {
                { Carid, "" },
                { Weight, "" },
                { Time, "" },
            };
            sql = new SqlHelper();
        }

        /// <summary>
        /// 获取过磅记录
        /// </summary>
        public void getWeighRec()
        {
            while (Resource.IsStart)
            {
                try
                {
                    Dictionary<string, string> dic = sql.SingleSelect("select " + Carid + "," + Weight + "," + Time + " from " + Table + " where DateDiff(SECOND," + Time + ",getdate())<=15", new List<string> { Carid, Weight, Time });
                    if (dic == null)
                    {
                        Thread.Sleep(10000);
                        continue;
                    }
                    if (dic[Carid] != previous[Carid] || dic[Weight] != previous[Weight] || dic[Time] != previous[Time])
                    {
                        previous = dic;
                        Resource.insertDb.Enqueue(dic);
                    }
                    Thread.Sleep(10000);
                }
                catch
                {
                    FormUtil.ModifyLable(DataForm.MainForm.local, "意外", Color.Red);
                }
            }
        }
    }
}