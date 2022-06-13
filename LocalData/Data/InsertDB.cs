using LocalData.MySql;
using LocalData.staticResouce;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

namespace LocalData.Data
{
    public class InsertDB
    {
        /// <summary>
        /// mysql实体
        /// </summary>
        private readonly MySqlHelper mysql;
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
        /// 公司名称
        /// </summary>
        private readonly string Company;
        /// <summary>
        /// 字段集合
        /// </summary>
        private readonly List<string> fileName;
        /// <summary>
        /// 运输车车分配及采面品位信息,车辆编号为key
        /// item1：采面
        /// item2：司机
        /// item3：ca
        /// item4：Mg
        /// item5：na
        /// </summary>
        private readonly Dictionary<string, ValueTuple<string, string, string, string, string>> TransInfo;
        /// <summary>
        /// 挖掘机分配信息，车辆编号为key
        /// item1：采面名称
        /// item2：司机姓名
        /// </summary>
        private readonly Dictionary<string, ValueTuple<string, string>> spadeInfo;
        /// <summary>
        /// 采面品位信息,采面名称为key
        /// item1：ca
        /// item2：Mg
        /// item3：na
        /// </summary>
        private readonly Dictionary<string, ValueTuple<string, string, string>> panelInfo;
        /// <summary>
        /// 采面上挖掘机信息,采面名称为key
        /// item1：司机
        /// item2：挖掘机编号
        /// </summary>
        private readonly Dictionary<string, ValueTuple<string, string>> panelSpadeInfo;
        /// <summary>
        /// 记录重补定时器
        /// </summary>
        private System.Timers.Timer LogTimer;
        private Dictionary<string, string> logdic;
        public InsertDB()
        {
            Carid = ConfigurationManager.AppSettings["InCarid"];
            Weight = ConfigurationManager.AppSettings["InWeight"];
            Time = ConfigurationManager.AppSettings["InTime"];
            Company = ConfigurationManager.AppSettings["Company"];
            TransInfo = new Dictionary<string, (string, string, string, string, string)>();
            spadeInfo = new Dictionary<string, (string, string)>();
            panelSpadeInfo = new Dictionary<string, (string, string)>();
            panelInfo = new Dictionary<string, (string, string, string)>();
            fileName = new List<string>
            {
                "VEHICLE_ID",
                "VEHICLE_DRIVER",
                "VEHICLE_TYPE",
                "PANEL",
                "CA",
                "MG",
                "NA"
            };
            mysql = new MySqlHelper();
            GetInfo(null, null);
            Thread getINfoThread = new Thread(getPanelInfo)
            {
                IsBackground = true
            };
            getINfoThread.Start();
            Thread loadErrorThread = new Thread(LogErrorTimers)
            {
                IsBackground = true
            };
            loadErrorThread.Start();
        }
        public void InsertServerDB()
        {
            Thread.Sleep(15000);
            while (Resource.IsStart)
            {
                try
                {
                    if (Resource.insertDb.Count > 0)
                    {
                        Resource.insertDb.TryDequeue(out Dictionary<string, string> val);
                        if (!TransInfo.ContainsKey(val[Carid]))
                        {
                            //未分配车辆，不记录采面分配信息
                            string tempSql = "select VEHICLE_DRIVER from list_vehicle where COMPANY='" + Company + "' and VEHICLE_ID='"+ val[Carid] + "'";
                          
                           Dictionary<string,string> tempDriver=mysql.SingleSelect(tempSql, "VEHICLE_DRIVER");
                            if (tempDriver != null)
                            {
                                //存入永久表
                                tempSql = "INSERT INTO `load_trans`( `VEHICLE_ID`, `DRIVER`, `WEIGHT`, `STATE`, `REAL_PANEL`, `CA`, `MG`, `NA`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ( '" + val[Carid] + "', '" + tempDriver["VEHICLE_DRIVER"] + "', '" + float.Parse(val[Weight]) + "', '正常', '未分配', '0', '0', '0', '" + Company + "', '" + val[Time] + "', NULL, NULL, NULL, NULL)";
                                if (mysql.UpdOrInsOrdel(tempSql) == 0)
                                {
                                    using (StreamWriter file = new StreamWriter("LoadError.txt", true))
                                    {
                                        file.WriteLine(val[Carid] + "," + val[Weight] + "," + val[Time]);// 直接追加文件末尾，换行 
                                    }
                                    continue;
                                }
                                //存入临时表
                                tempSql = "INSERT INTO `temp_load_trans`( `VEHICLE_ID`, `DRIVER`, `WEIGHT`, `STATE`, `REAL_PANEL`, `CA`, `MG`, `NA`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ( '" + val[Carid] + "', '" + tempDriver["VEHICLE_DRIVER"] + "', '" + float.Parse(val[Weight]) + "', '正常', '未分配', '0', '0', '0', '" + Company + "', '" + val[Time] + "', NULL, NULL, NULL, NULL)";
                                mysql.UpdOrInsOrdel(tempSql);
                            }
                            else {
                                LogHelper.WriteLog("检测到系统外车辆过磅--" + val[Carid]+ "---取消计入");
                            }
                            continue;
                        }
                        string vid = val[Carid];
                        float weight = float.Parse(val[Weight]);
                        string driver = TransInfo[vid].Item2;
                        string panel = TransInfo[vid].Item1;
                        string ca = TransInfo[vid].Item3;
                        string mg = TransInfo[vid].Item4;
                        string na = TransInfo[vid].Item5;
                        string state = "正常";
                        string sql = "select ID,REAL_PANEL,DRIVER from rec_unu_tran where ADD_TIME>=DATE_SUB('" + val[Time] + "',INTERVAL 30 MINUTE) and ADD_TIME <= '" + val[Time] + "' and company='" + Company + "' and REC_STATE='NO' and VEHICLE_ID='" + vid + "'";
                        Dictionary<string, string> value = mysql.SingleSelect(sql, new string[] { "ID", "REAL_PANEL", "DRIVER" });
                        if (value != null)
                        {
                            driver = value["DRIVER"];
                            panel = value["REAL_PANEL"];
                            if (panelInfo.TryGetValue(panel, out ValueTuple<string, string, string> temp))
                            {
                                ca = temp.Item1;
                                mg = temp.Item2;
                                na = temp.Item3;
                            }
                            state = "异常";
                            string id = value["ID"];
                            sql = "update rec_unu_tran set WEIGHT='" + weight + "',REC_STATE='YES' where ID='" + id + "' ";
                            mysql.UpdOrInsOrdel(sql);
                        }
                        //存入永久表
                        sql = "INSERT INTO `load_trans`( `VEHICLE_ID`, `DRIVER`, `WEIGHT`, `STATE`, `REAL_PANEL`, `CA`, `MG`, `NA`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ( '" + vid + "', '" + driver + "', '" + weight + "', '" + state + "', '" + panel + "', '" + ca + "', '" + mg + "', '" + na + "', '" + Company + "', '" + val[Time] + "', NULL, NULL, NULL, NULL)";
                        int result = mysql.UpdOrInsOrdel(sql);
                        if (result == 0)
                        {
                            using (StreamWriter file = new StreamWriter("LoadError.txt", true))
                            {
                                file.WriteLine(val[Carid] + "," + val[Weight] + "," + val[Time]);// 直接追加文件末尾，换行 
                            }
                            continue;
                        }
                        //存入临时表
                        sql = "INSERT INTO `temp_load_trans`( `VEHICLE_ID`, `DRIVER`, `WEIGHT`, `STATE`, `REAL_PANEL`, `CA`, `MG`, `NA`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ( '" + vid + "', '" + driver + "', '" + weight + "', '" + state + "', '" + panel + "', '" + ca + "', '" + mg + "', '" + na + "', '" + Company + "', '" + val[Time] + "', NULL, NULL, NULL, NULL)";
                        mysql.UpdOrInsOrdel(sql);
                        if (panelSpadeInfo.TryGetValue(panel, out ValueTuple<string, string> temp2))
                        {
                            //写入装载临时表
                            sql = "INSERT INTO `temp_load_spade`(`VEHICLE_ID`, `WEIGHT`, `DRIVER`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ( '" + temp2.Item2 + "', '" + weight + "', '" + temp2.Item1 + "', '" + Company + "', '" + val[Time] + "', NULL, NULL, NULL, NULL)";
                            mysql.UpdOrInsOrdel(sql);
                            //写入装载永久表
                            sql = "INSERT INTO `load_spade`(`VEHICLE_ID`, `WEIGHT`, `DRIVER`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ( '" + temp2.Item2 + "', '" + weight + "', '" + temp2.Item1 + "', '" + Company + "', '" + val[Time] + "', NULL, NULL, NULL, NULL)";
                            mysql.UpdOrInsOrdel(sql);
                        }
                    }
                    else
                    {
                        Thread.Sleep(5000);
                    }
                }
                catch (Exception e)
                {
                    LogHelper.WriteLog("意外错误", e);
                }
            }
        }
        /// <summary>
        /// 获取车辆分配及采面品位信息定时
        /// </summary>
        private void getPanelInfo()
        {
            System.Timers.Timer getInfoTimer = new System.Timers.Timer(1000 * 30);
            getInfoTimer.Elapsed += new ElapsedEventHandler(GetInfo);
            getInfoTimer.AutoReset = true;
            getInfoTimer.Enabled = true;
        }
        /// <summary>
        /// 获取车辆分配及采面品位信息
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void GetInfo(object source, System.Timers.ElapsedEventArgs e)
        {
            string sql = "select VEHICLE_ID,VEHICLE_DRIVER,VEHICLE_TYPE,PANEL,CA,MG,NA from" +
                "(SELECT ID,VEHICLE_ID,VEHICLE_TYPE,VEHICLE_DRIVER from list_vehicle where COMPANY='" + Company + "' and VEHICLE_TYPE='挖掘机'or VEHICLE_TYPE='运输车' )a " +
                "inner join" +
                "(" +
                "select VID,PANEL,CA,MG,NA from(select PID,VID from pre_dispatch where COMPANY='" + Company + "')b " +
                "inner join " +
                "(select ID,PANEL,CA,MG,NA from pre_panel where COMPANY='" + Company + "')c " +
                "on b.PID=c.ID" +
                ")d " +
                "on a.ID=d.VID";
            List<Dictionary<string, string>> result = mysql.MultipleSelect(sql, fileName);
            if (result != null)
            {
                foreach (var item in result)
                {
                    item.TryGetValue("VEHICLE_ID", out string vid);
                    item.TryGetValue("VEHICLE_DRIVER", out string driver);
                    item.TryGetValue("VEHICLE_TYPE", out string type);
                    item.TryGetValue("PANEL", out string panel);
                    item.TryGetValue("CA", out string ca);
                    item.TryGetValue("MG", out string mg);
                    item.TryGetValue("NA", out string na);
                    if (type == "运输车")
                    {
                        if (TransInfo.ContainsKey(vid))
                        {
                            TransInfo[vid] = new ValueTuple<string, string, string, string, string>(panel, driver, ca, mg, na);
                        }
                        else
                        {
                            TransInfo.Add(vid, new ValueTuple<string, string, string, string, string>(panel, driver, ca, mg, na));
                        }
                    }
                    else
                    {
                        if (spadeInfo.ContainsKey(vid))
                        {
                            spadeInfo[vid] = new ValueTuple<string, string>(panel, driver);

                        }
                        else
                        {
                            spadeInfo.Add(vid, new ValueTuple<string, string>(panel, driver));

                        }
                        if (panelSpadeInfo.ContainsKey(panel))
                        {
                            panelSpadeInfo[panel] = new ValueTuple<string, string>(driver, vid);
                        }
                        else
                        {
                            panelSpadeInfo.Add(panel, new ValueTuple<string, string>(driver, vid));
                        }
                    }
                    if (panelInfo.ContainsKey(panel))
                    {
                        panelInfo[panel] = new ValueTuple<string, string, string>(ca, mg, na);
                    }
                    else
                    {
                        panelInfo.Add(panel, new ValueTuple<string, string, string>(ca, mg, na));
                    }
                }
            }
            else
            {
                TransInfo.Clear();
                spadeInfo.Clear();
                panelInfo.Clear();
                panelSpadeInfo.Clear();
            }
        }

        /// <summary>
        /// 失败数据重新存储定时
        /// </summary>
        public void LogErrorTimers()
        {
            LogTimer = new System.Timers.Timer(1000 * 60 * 10);
            LogTimer.Elapsed += new ElapsedEventHandler(LoadErrorLog);
            LogTimer.AutoReset = true;
            LogTimer.Enabled = true;
        }
        /// <summary>
        /// 重补错误记录
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void LoadErrorLog(object source, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (File.Exists("LoadError.txt"))
                {
                    List<string> lines = new List<string>(File.ReadAllLines("LoadError.txt"));
                    File.Delete("LoadError.txt");
                    foreach (var item in lines)
                    {
                        string[] log = item.Split(',');
                        logdic = new Dictionary<string, string>() {
                            { Carid, log[0] },{ Weight, log[1]},{ Time, log[2]}
                        };
                        Resource.insertDb.Enqueue(logdic);
                    }                   
                }
            }
            catch(Exception ex)
            {
                LogHelper.WriteLog("记录重补错误----"+ex);
            }
        }
    }
}
