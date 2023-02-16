using LocalData.MySql;
using LocalData.staticResouce;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
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
        /// 窜铲检测识别距离
        /// </summary>
        private readonly double ExceDistance;

        /// <summary>
        /// 禁止未知车辆过磅标识
        /// </summary>
        private readonly string ForbiddenUnknown;

        /// <summary>
        /// 装车时间
        /// </summary>
        private readonly double LoadTime;

        /// <summary>
        /// 车辆信息及采面数据库字段集合
        /// </summary>
        private readonly List<string> VehicleInfoFieldName;

        /// <summary>
        /// 车辆状态表数据库字段集合
        /// </summary>
        private readonly List<string> VehicleStateFieldName;

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

        /// <summary>
        ///  铲车状态表字典，key为车辆编号，valueTuple<x坐标，y坐标，时间,fid>
        /// </summary>
        private Dictionary<string, ValueTuple<string, string, string, string>> spade;

        /// <summary>
        ///  运输车状态表字典，key为车辆编号，valueTuple<x坐标，y坐标，时间,fid>
        /// </summary>
        private Dictionary<string, ValueTuple<string, string, string, string>> trans;

        /// <summary>
        /// 触发报警的运输车字典（车辆编号---触发时间）
        /// </summary>
        private ConcurrentDictionary<string, string> VehicleWarn;

        private bool isRead = false;

        public InsertDB()
        {
            ForbiddenUnknown = ConfigurationManager.AppSettings["ForbiddenUnknown"];
            Carid = ConfigurationManager.AppSettings["InCarid"];
            Weight = ConfigurationManager.AppSettings["InWeight"];
            Time = ConfigurationManager.AppSettings["InTime"];
            Company = ConfigurationManager.AppSettings["Company"];
            ExceDistance = double.Parse(ConfigurationManager.AppSettings["ExceDistance"]);
            LoadTime = double.Parse(ConfigurationManager.AppSettings["LoadTime"]);
            TransInfo = new Dictionary<string, (string, string, string, string, string)>();
            spadeInfo = new Dictionary<string, (string, string)>();
            panelSpadeInfo = new Dictionary<string, (string, string)>();
            panelInfo = new Dictionary<string, (string, string, string)>();
            VehicleInfoFieldName = new List<string> { "VEHICLE_ID", "VEHICLE_DRIVER", "VEHICLE_TYPE", "PANEL", "CA", "MG", "NA" };
            VehicleStateFieldName = new List<string> { "FID", "VEHICLE_ID", "VEHICLE_TYPE", "POSI_X", "POSI_Y", "ADD_TIME" };
            VehicleWarn = new ConcurrentDictionary<string, string>();
            mysql = new MySqlHelper();
            GetInfo(null, null);
            Thread getINfoThread = new Thread(getPanelInfo)
            {
                IsBackground = true
            };
            getINfoThread.Start();
            Thread CheckTransThread = new Thread(CheckTransTimers)
            {
                IsBackground = true
            };
            CheckTransThread.Start();

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
                if (Resource.insertDb.Count > 0)
                {
                    try
                    {
                        while (isRead)
                        {
                            Thread.Sleep(20);
                        }
                        isRead = true;
                        Resource.insertDb.TryDequeue(out Dictionary<string, string> val);
                        if (!TransInfo.ContainsKey(val[Carid]))
                        {
                            string vehicleDriver = "未知";
                            //未分配车辆，不记录采面分配信息
                            string tempSql = "select VEHICLE_DRIVER from list_vehicle where COMPANY='" + Company + "' and VEHICLE_ID='" + val[Carid] + "'";
                            Dictionary<string, string> tempDriver = mysql.SingleSelect(tempSql, "VEHICLE_DRIVER");
                            if (tempDriver != null)
                            {
                                vehicleDriver = tempDriver["VEHICLE_DRIVER"];
                            }
                            if (ForbiddenUnknown == "true")
                            {
                                //系统外车辆过磅，禁止计入
                                tempSql = "INSERT INTO `rec_unu_info`( `WARN_USER_ID`, `WARN_USER_TYPE`, `WARNTYPE`, `INFO`, `DRIVER`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ('" + val[Carid] + "', '车辆', '无效过磅', '系统外车辆或系统内未分配任务车辆过磅，已禁止计入', '未知司机', '" + Company + "', '" + val[Time] + "', NULL, NULL, NULL, NULL);";
                                mysql.UpdOrInsOrdel(tempSql);
                            }
                            else
                            {
                                //存入永久表
                                tempSql = "INSERT INTO `load_trans`( `VEHICLE_ID`, `DRIVER`, `WEIGHT`, `STATE`, `REAL_PANEL`, `CA`, `MG`, `NA`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ( '" + val[Carid] + "', '" + vehicleDriver + "', '" + float.Parse(val[Weight]) + "', '正常', '未分配', '0', '0', '0', '" + Company + "', '" + val[Time] + "', NULL, NULL, NULL, NULL)";
                                if (mysql.UpdOrInsOrdel(tempSql) == 0)
                                {
                                    using (StreamWriter file = new StreamWriter("LoadError.txt", true))
                                    {
                                        file.WriteLine(val[Carid] + "," + val[Weight] + "," + val[Time]);// 直接追加文件末尾，换行
                                    }
                                    continue;
                                }
                                //存入临时表
                                tempSql = "INSERT INTO `temp_load_trans`( `VEHICLE_ID`, `DRIVER`, `WEIGHT`, `STATE`, `REAL_PANEL`, `CA`, `MG`, `NA`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ( '" + val[Carid] + "', '" + vehicleDriver + "', '" + float.Parse(val[Weight]) + "', '正常', '未分配', '0', '0', '0', '" + Company + "', '" + val[Time] + "', NULL, NULL, NULL, NULL)";
                                mysql.UpdOrInsOrdel(tempSql);
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
                            using (StreamWriter file = new StreamWriter("@D:/localData/LoadError.txt", true))
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
                    catch (Exception e)
                    {
                        LogHelper.WriteLog("意外错误", e);
                    }
                    finally
                    {
                        isRead = false;
                    }
                }
                else
                {
                    Thread.Sleep(5000);
                }
            }
        }

        /// <summary>
        /// 获取车辆分配及采面品位信息定时
        /// </summary>
        private void getPanelInfo()
        {
            System.Timers.Timer getInfoTimer = new System.Timers.Timer(1000 * 15);
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
            try
            {
                string sql = "select VEHICLE_ID,VEHICLE_DRIVER,VEHICLE_TYPE,PANEL,CA,MG,NA from" +
        "(SELECT ID,VEHICLE_ID,VEHICLE_TYPE,VEHICLE_DRIVER from list_vehicle where COMPANY='" + Company + "')a " +
        "inner join" +
        "(" +
        "select VID,PANEL,CA,MG,NA from(select PID,VID from pre_dispatch where COMPANY='" + Company + "')b " +
        "inner join " +
        "(select ID,PANEL,CA,MG,NA from pre_panel where COMPANY='" + Company + "')c " +
        "on b.PID=c.ID" +
        ")d " +
        "on a.ID=d.VID";
                while (isRead)
                {
                    Thread.Sleep(2);
                }
                isRead = true;
                List<Dictionary<string, string>> result = mysql.multiple_select_list_dic(sql, VehicleInfoFieldName);
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
            catch (Exception ex)
            {
                LogHelper.WriteLog("信息获取错误----" + ex);
            }
            finally
            {
                isRead = false;
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
                if (File.Exists("@D:/localData/LoadError.txt"))
                {
                    List<string> lines = new List<string>(File.ReadAllLines("@D:/localData/LoadError.txt"));
                    File.Delete("@D:/localData/LoadError.txt");
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
            catch (Exception ex)
            {
                LogHelper.WriteLog("记录重补错误----" + ex);
            }
        }

        /// <summary>
        /// 窜铲检查定时器
        /// </summary>
        public void CheckTransTimers()
        {
            LogTimer = new System.Timers.Timer(1000 * 30);
            LogTimer.Elapsed += new ElapsedEventHandler(CheckTrans);
            LogTimer.AutoReset = true;
            LogTimer.Enabled = true;
        }

        //窜铲检查
        public void CheckTrans(object source, System.Timers.ElapsedEventArgs e)
        {
            //30S为间隔轮询车辆位置，排除已报警车辆，检测到运输车在分配外铲车位置时触发报警，单独开启线程监测此车辆位置并加入已触发警报List，当连续两分钟车辆位置都在警报铲的氛围内时触发窜铲警报写入数据库
            string sql = "select a.FID,b.VEHICLE_ID,b.VEHICLE_TYPE,POSI_X,POSI_Y,ADD_TIME from (SELECT FID,POSI_X,POSI_Y,ADD_TIME FROM `vehicle_state` where COMPANY='" + Company + "' and ADD_TIME>=DATE_SUB(NOW(),INTERVAL 30 SECOND) ) a inner join (SELECT ID,VEHICLE_ID,VEHICLE_TYPE from list_vehicle where COMPANY='" + Company + "') b on a.FID=b.ID";
            while (isRead)
            {
                Thread.Sleep(2);
            }
            isRead = true;
            List<Dictionary<string, string>> result = mysql.multiple_select_list_dic(sql, VehicleStateFieldName);
            isRead = false;
            if (result == null)
            {
                return;
            }
            //铲车与运输车字典，key为车辆编号，valueTuple<x坐标，y坐标，时间>
            spade = new Dictionary<string, (string, string, string, string)>();
            trans = new Dictionary<string, (string, string, string, string)>();
            //遍历返回结果，根据车辆类型分类
            foreach (var item in result)
            {
                item.TryGetValue("FID", out string fid);
                item.TryGetValue("VEHICLE_ID", out string vid);
                item.TryGetValue("VEHICLE_TYPE", out string type);
                item.TryGetValue("POSI_X", out string x);
                item.TryGetValue("POSI_Y", out string y);
                item.TryGetValue("ADD_TIME", out string time);
                switch (type)
                {
                    case "运输车":
                        //判断此车辆车辆是否分配任务
                        if (TransInfo.ContainsKey(vid))
                        {
                            //判别车辆是否已触发警报-触发则不入字典
                            if (!VehicleWarn.ContainsKey(vid))
                            {
                                trans.Add(vid, new ValueTuple<string, string, string, string>(x, y, time, fid));
                            }
                        }
                        break;

                    case "挖掘机":
                        if (spadeInfo.ContainsKey(vid))
                        {
                            spade.Add(vid, new ValueTuple<string, string, string, string>(x, y, time, fid));
                        }
                        break;

                    default:
                        break;
                }
            }
            //计算运输车辆与各挖掘机距离
            foreach (var transItem in trans)
            {
                foreach (var spadeItem in spade)
                {
                    //判断当前运输车所在采面是否分配了铲车
                    if (!panelSpadeInfo.ContainsKey(TransInfo[transItem.Key].Item1))
                    {
                        continue;
                    }
                    //判断即将计算的运输车与挖掘机是否与服务器采面分配一致
                    if (panelSpadeInfo[TransInfo[transItem.Key].Item1].Item2 != spadeItem.Key)
                    {
                        //计算辆车距离
                        double dis = Math.Sqrt(Math.Pow(double.Parse(spadeItem.Value.Item1) - double.Parse(transItem.Value.Item1), 2) + Math.Pow(double.Parse(spadeItem.Value.Item2) - double.Parse(transItem.Value.Item2), 2));
                        if (dis < ExceDistance)
                        {
                            //小于设定距离，触发监测警报,加入警报字典
                            VehicleWarn.TryAdd(transItem.Key, transItem.Value.Item3);
                            Dictionary<string, string> dic = new Dictionary<string, string>
                            {
                                { "tfid", transItem.Value.Item4 },
                                { "sfid", spadeItem.Value.Item4 },
                                { "Tvid", transItem.Key },
                                { "Svid", spadeItem.Key },
                                { "time", transItem.Value.Item3}
                        };
                            ThreadPool.QueueUserWorkItem(MonitorWarnTrans, dic);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 监测触发运输报警的车辆
        /// </summary>
        /// <param name="Tvid"></param>
        public void MonitorWarnTrans(object obj)
        {
            Dictionary<string, string> dic = obj as Dictionary<string, string>;
            string Tvid = dic["Tvid"];
            string Svid = dic["Svid"];
            string time = dic["time"];
            DateTime Endtime = Convert.ToDateTime(time);
            bool isReport = true;
            while ((Endtime - Convert.ToDateTime(time)).TotalMinutes < LoadTime)
            {
                Thread.Sleep(6000);
                //查询触发警报的运输车位置
                Dictionary<string, string> Tdic = getPosi(dic["tfid"], 12);
                //查询触发警报的铲车位置
                Dictionary<string, string> Sdic = getPosi(dic["sfid"], 120);
                //判断车辆是否已离线，如果离线解除报警
                if (Tdic == null || Sdic == null)
                {
                    isReport = false;
                    VehicleWarn.TryRemove(Tvid, out _);
                    break;
                }
                //计算辆车距离
                double dis = Math.Sqrt(Math.Pow(double.Parse(Sdic["POSI_X"]) - double.Parse(Tdic["POSI_X"]), 2) + Math.Pow(double.Parse(Sdic["POSI_Y"]) - double.Parse(Tdic["POSI_Y"]), 2));
                //判断是否仍在警报范围内
                if (dis > ExceDistance)
                {
                    isReport = false;
                    VehicleWarn.TryRemove(Tvid, out _);
                    break;
                }
                Endtime = Convert.ToDateTime(Tdic["ADD_TIME"]);
            }
            VehicleWarn.TryRemove(Tvid, out _);
            if (isReport)
            {
                //确认报警，插入数据库
                //检查10分钟内是否存在记录
                string sql = "SELECT COUNT(ID) as Count from rec_unu_tran where VEHICLE_ID='" + Tvid + "' and COMPANY='" + Company + "' and ADD_TIME>=DATE_SUB(NOW(),INTERVAL 10 MINUTE)";
                while (isRead)
                {
                    Thread.Sleep(2);
                }
                isRead = true;
                if (mysql.GetCount(sql) == 0)
                {
                    TransInfo.TryGetValue(Tvid, out (string, string, string, string, string) Trans);
                    spadeInfo.TryGetValue(Svid, out (string, string) spade);
                    panelSpadeInfo.TryGetValue(Trans.Item1, out (string, string) panel);
                    _ = Trans.Item2 == null ? Trans.Item2 = "未知" : null;
                    _ = spade.Item1 == null ? spade.Item1 = "未知" : null;
                    _ = panel.Item2 == null ? panel.Item2 = "未知" : null;
                    sql = "INSERT INTO `rec_unu_tran`(`VEHICLE_ID`, `DRIVER`, `WEIGHT`, `REAL_SPADE`, `REAL_PANEL`, `PRE_SPADE`, `PRE_PANEL`, `REC_STATE`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ('" + Tvid + "', '" + Trans.Item2 + "', 0, '" + Svid + "', '" + spade.Item1 + "', '" + panel.Item2 + "', '" + Trans.Item1 + "', 'NO', '" + Company + "', '" + DateTime.Now + "', NULL, NULL, NULL, NULL)";
                    mysql.UpdOrInsOrdel(sql);
                }
                isRead = false;
            }
        }

        private Dictionary<string, string> getPosi(string Fid, int second)
        {
            //查询车辆位置
            Dictionary<string, string> dic = new Dictionary<string, string>();
            string sql = "SELECT POSI_X,POSI_Y,ADD_TIME FROM `vehicle_state` where COMPANY='" + Company + "' and FID='" + Fid + "' and ADD_TIME>=DATE_SUB(NOW(),INTERVAL '" + second + "' SECOND)";
            while (isRead)
            {
                Thread.Sleep(2);
            }
            isRead = true;
            List<Dictionary<string, string>> Tresult = mysql.multiple_select_list_dic(sql, new List<string>() { "POSI_X", "POSI_Y", "ADD_TIME" });
            isRead = false;
            if (Tresult != null)
            {
                Tresult[0].TryGetValue("ADD_TIME", out string TEndTime);
                Tresult[0].TryGetValue("POSI_X", out string Tx);
                Tresult[0].TryGetValue("POSI_Y", out string Ty);
                dic.Add("POSI_X", Tx);
                dic.Add("POSI_Y", Ty);
                dic.Add("ADD_TIME", TEndTime);
                return dic;
            }
            return null;
        }
    }
}