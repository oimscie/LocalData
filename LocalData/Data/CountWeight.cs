using LocalData.MySql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

namespace LocalData.Data
{
    /// <summary>
    /// 过磅统计
    /// </summary>
    public class CountWeight
    {
        private readonly MySqlHelper mysql;
        private readonly string Company;
        private readonly string date;

        /// <summary>
        /// 统计过程标记
        /// </summary>
        private int TotalCount = 0;

        public CountWeight(string dates)
        {
            Company = ConfigurationManager.AppSettings["Company"];
            mysql = new MySqlHelper();
            date = DateTime.Parse(dates).AddDays(-1).ToShortDateString();
            Count(null, null);
            date = dates;
            Thread thread = new Thread(CountTimer)
            {
                IsBackground = true
            };
            thread.Start();
        }

        private void Count(object source, System.Timers.ElapsedEventArgs e)
        {
            CountTransHourAndDayByVehicle(date);
            CountTransHourAndDayByDriver(date);
            CountSpade(date);
            CountSpadeHourAndDayByDriver(date);
            TotalCount = 0;
        }

        private void CountTimer()
        {
            System.Timers.Timer Timer = new System.Timers.Timer(1000 * 60 * 15);
            Timer.Elapsed += new ElapsedEventHandler(Count);
            Timer.AutoReset = true;
            Timer.Enabled = true;
        }

        #region//车辆运输统计

        /// <summary>
        /// 指定日期的各小时与全天运输统计
        /// </summary>
        /// <param name="date">日期(Y-M-D)</param>
        public void CountTransHourAndDayByVehicle(string date)
        {
            //获取运输车辆
            FormUtil.ModifyLable(DataForm.MainForm.Loadometer, "统计-" + (TotalCount + 1), Color.Green);
            string sql = "SELECT DISTINCT VEHICLE_ID FROM temp_load_trans where COMPANY='" + Company + "' and DATE(ADD_TIME)='" + date + "'";
            List<string> list = mysql.multiple_select_list_string(sql, "VEHICLE_ID");

            try
            {
                if (list != null)
                {
                    //遍历各车运输情况（运输量、运输次数、满载率）
                    foreach (string vid in list)
                    {
                        float TotalWeight = 0;//当前车全天运输总量
                        int TotalNum = 0;//当前车全天运输总车次
                        int load = 0;//当前车辆核定载重
                        sql = "select ifnull(loads,50) as loads,weight,num,hours from (select  VEHICLE_ID,SUM(WEIGHT) as weight ,COUNT(ID) as num ,HOUR(ADD_TIME) as hours from temp_load_trans where VEHICLE_ID='" + vid + "' and COMPANY='" + Company + "' and DATE(ADD_TIME)='" + date + "' GROUP BY hours)a left join (select VEHICLE_ID,VEHICLE_LOAD as loads from list_vehicle where VEHICLE_ID='" + vid + "' and COMPANY='" + Company + "' )b on a.VEHICLE_ID=b.VEHICLE_ID";
                        List<Dictionary<string, string>> result = mysql.multiple_select_list_dic(sql, new List<string>() { "loads", "weight", "num", "hours" });
                        if (result != null)
                        {
                            //当前车各小时过磅数据（运输量、运输次数、满载率）
                            foreach (var dic in result)
                            {
                                string hours = dic["hours"];
                                string weight = dic["weight"];
                                TotalWeight += float.Parse(weight);
                                string num = dic["num"];
                                TotalNum += int.Parse(num);
                                load = int.Parse(dic["loads"]);
                                string factor = load * float.Parse(num) == 0 ? "0" : (float.Parse(weight) / load / float.Parse(num)).ToString();
                                string FormatDate = date + "-" + hours;
                                sql = "select COUNT(ID) as Count from count_sys_hour where VEHICLE_ID='" + vid + "' and COMPANY='" + Company + "' and date_format(ADD_TIME,'%Y-%m-%d-%h')=date_format('" + FormatDate + "','%Y-%m-%d-%h')";
                                if (mysql.GetCount(sql) != 0)
                                {
                                    sql = "update count_sys_hour set WEIGHT='" + weight + "',LOAD_NUM='" + num + "',LOAD_FACTOR='" + factor + "' where VEHICLE_ID='" + vid + "' and COMPANY='" + Company + "' and date_format(ADD_TIME,'%Y-%m-%d-%h')=date_format('" + FormatDate + "','%Y-%m-%d-%h')";
                                    mysql.UpdOrInsOrdel(sql);
                                }
                                else
                                {
                                    string time = date + " " + hours + ":30:00";
                                    sql = "INSERT INTO `count_sys_hour`( `VEHICLE_ID`, `TYPE`, `WEIGHT`, `UNUSUAL_WEIGHT`, `USE_FUEL`, `INJECT_FUEL`, `MILEAGE`, `LOAD_NUM`, `LOAD_FACTOR`, `UNFUEL`, `UNSPEED`, `UNTRANS`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ('" + vid + "', '运输车','" + weight + "', 0, 0, 0, 0, '" + num + "', '" + factor + "', 0, 0, 0, '" + Company + "', '" + time + "', NULL, NULL, NULL, NULL)";
                                    mysql.UpdOrInsOrdel(sql);
                                }
                            }
                            //计算当前日期当前车辆总运输数据
                            string factors = load * TotalNum == 0 ? "0" : (TotalWeight / TotalNum / load).ToString();
                            sql = "select COUNT(ID) as Count from count_sys_day where VEHICLE_ID='" + vid + "' and COMPANY='" + Company + "' and DATE(ADD_TIME)='" + date + "'";
                            if (mysql.GetCount(sql) != 0)
                            {
                                sql = "update count_sys_day set WEIGHT='" + TotalWeight + "',LOAD_NUM='" + TotalNum + "',LOAD_FACTOR='" + factors + "' where VEHICLE_ID='" + vid + "' and COMPANY='" + Company + "' and ADD_TIME='" + date + "'";
                                mysql.UpdOrInsOrdel(sql);
                            }
                            else
                            {
                                sql = "INSERT INTO `count_sys_day`( `VEHICLE_ID`, `TYPE`, `WEIGHT`, `UNUSUAL_WEIGHT`, `USE_FUEL`, `INJECT_FUEL`, `MILEAGE`, `LOAD_NUM`, `LOAD_FACTOR`, `UNFUEL`, `UNSPEED`, `UNTRANS`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ('" + vid + "', '运输车','" + TotalWeight + "', 0, 0, 0, 0, '" + TotalNum + "', '" + factors + "', 0, 0, 0, '" + Company + "', '" + date + "', NULL, NULL, NULL, NULL)";
                                mysql.UpdOrInsOrdel(sql);
                            }
                        }
                    }
                    //遍历各车运输警报情况
                    foreach (string vid in list)
                    {
                        float TotalWeight = 0;//当前车辆当前日期运输异常总量
                        float TotalCount = 0;//当前车辆当前日期运输运输报警总次数
                        sql = "select  Count(ID) as Count,SUM(WEIGHT)as weight,HOUR(ADD_TIME) as hours from rec_unu_tran where REC_STATE='YES' and VEHICLE_ID='" + vid + "' and COMPANY='" + Company + "' and DATE(ADD_TIME)='" + date + "' GROUP BY hours";
                        List<Dictionary<string, string>> result = mysql.multiple_select_list_dic(sql, new List<string>() { "Count", "weight", "hours" });
                        if (result != null)
                        {
                            //当前车各小时运输报警数据
                            foreach (var dic in result)
                            {
                                string hours = dic["hours"];
                                string weight = dic["weight"];
                                TotalWeight += float.Parse(weight);
                                string Count = dic["Count"];
                                TotalCount += int.Parse(Count);
                                string FormatDate = date + "-" + hours;
                                sql = "update count_sys_hour set UNUSUAL_WEIGHT='" + weight + "',UNTRANS='" + Count + "' where VEHICLE_ID='" + vid + "' and COMPANY='" + Company + "' and date_format(ADD_TIME,'%Y-%m-%d-%h')=date_format('" + FormatDate + "','%Y-%m-%d-%h')";
                                mysql.UpdOrInsOrdel(sql);
                            }
                            //全天
                            sql = "update count_sys_day set UNTRANS='" + TotalCount + "',UNUSUAL_WEIGHT='" + TotalWeight + "' where VEHICLE_ID='" + vid + "' and COMPANY='" + Company + "' and DATE(ADD_TIME)='" + date + "'";
                            mysql.UpdOrInsOrdel(sql);
                        }
                    }
                }
                FormUtil.ModifyLable(DataForm.MainForm.Loadometer, "正常", Color.Green);
            }
            catch (Exception ex)
            {
                FormUtil.ModifyLable(DataForm.MainForm.Loadometer, "错误", Color.Red);
                LogHelper.WriteLog("计算错误-----" + ex);
            }
        }

        #endregion
        #region//司机运输统计

        public void CountTransHourAndDayByDriver(string date)
        {
            FormUtil.ModifyLable(DataForm.MainForm.Loadometer, "统计-" + (TotalCount + 1), Color.Green);
            //获取运输司机
            string sql = "SELECT DISTINCT DRIVER FROM temp_load_trans where COMPANY='" + Company + "' and DATE(ADD_TIME)='" + date + "'";
            List<string> list = mysql.multiple_select_list_string(sql, "DRIVER");
            try
            {
                if (list != null)
                {
                    //遍历各司机运输情况，同遍历各车情况相同
                    foreach (string driver in list)
                    {
                        float TotalWeight = 0;//当前司机全天运输总量
                        int TotalNum = 0;//当前司机全天运输总车次
                        sql = "select SUM(WEIGHT) as weight ,COUNT(ID) as num ,HOUR(ADD_TIME) as hours from temp_load_trans where DRIVER='" + driver + "' and COMPANY='" + Company + "' and DATE(ADD_TIME)='" + date + "' GROUP BY hours";
                        List<Dictionary<string, string>> result = mysql.multiple_select_list_dic(sql, new List<string>() { "weight", "num", "hours" });
                        if (result != null)
                        {
                            //当前司机各小时过磅数据（运输量、运输次数、吨/车）
                            foreach (var dic in result)
                            {
                                string hours = dic["hours"];
                                string weight = dic["weight"];
                                TotalWeight += float.Parse(weight);
                                string num = dic["num"];
                                TotalNum += int.Parse(num);
                                string avgWeight = int.Parse(num) == 0 ? "0" : (float.Parse(weight) / int.Parse(num)).ToString();
                                string FormatDate = date + "-" + hours;
                                sql = "select COUNT(ID) as Count from count_driver_hour where DRIVER='" + driver + "' and COMPANY='" + Company + "' and date_format(ADD_TIME,'%Y-%m-%d-%h')=date_format('" + FormatDate + "','%Y-%m-%d-%h')";
                                if (mysql.GetCount(sql) != 0)
                                {
                                    sql = "update count_driver_hour set WEIGHT='" + weight + "',LOAD_NUM='" + num + "',AVGWEIGHT='" + avgWeight + "' where DRIVER='" + driver + "' and COMPANY='" + Company + "' and date_format(ADD_TIME,'%Y-%m-%d-%h')=date_format('" + FormatDate + "','%Y-%m-%d-%h')";
                                    mysql.UpdOrInsOrdel(sql);
                                }
                                else
                                {
                                    string time = date + " " + hours + ":30:00";
                                    sql = "INSERT INTO `count_driver_hour`( `DRIVER`, `WEIGHT`, `TYPE`, `UNUSUAL_WEIGHT`, `LOAD_NUM`, `AVGWEIGHT`, `UNFUEL`, `UNSPEED`, `UNTRANS`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ( '" + driver + "', '" + weight + "', '运输车', 0, '" + num + "', '" + avgWeight + "', 0, 0, 0, '" + Company + "', '" + time + "', NULL, NULL, NULL, NULL);";

                                    mysql.UpdOrInsOrdel(sql);
                                }
                            }

                            //计算当前日期当前司机总过磅数
                            string AvgWeight = TotalNum == 0 ? "0" : (TotalWeight / TotalNum).ToString();
                            sql = "select COUNT(ID) as Count from count_driver_day where DRIVER='" + driver + "' and COMPANY='" + Company + "' and DATE(ADD_TIME)='" + date + "'";
                            if (mysql.GetCount(sql) != 0)
                            {
                                sql = "update count_driver_day set WEIGHT='" + TotalWeight + "',LOAD_NUM='" + TotalNum + "',AVGWEIGHT='" + AvgWeight + "' where DRIVER='" + driver + "' and COMPANY='" + Company + "' and ADD_TIME='" + date + "'";
                                mysql.UpdOrInsOrdel(sql);
                            }
                            else
                            {
                                sql = "INSERT INTO `count_driver_day`( `DRIVER`, `WEIGHT`, `TYPE`, `UNUSUAL_WEIGHT`, `LOAD_NUM`, `AVGWEIGHT`, `WORKTIME`,`UNFUEL`, `UNSPEED`, `UNTRANS`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ('" + driver + "', '" + TotalWeight + "', '运输车', 0, '" + TotalNum + "', '" + AvgWeight + "',0, 0, 0, 0, '" + Company + "', '" + date + "', NULL, NULL, NULL, NULL)";
                                mysql.UpdOrInsOrdel(sql);
                            }
                        }
                    }

                    //遍历各司机运输警报情况
                    foreach (string driver in list)
                    {
                        float TotalWeight = 0;//当前司机当前日期运输异常总量
                        float TotalCount = 0;//当前司机当前日期运输运输报警总次数
                        sql = "select  Count(ID) as Count,SUM(WEIGHT)as weight,HOUR(ADD_TIME) as hours from rec_unu_tran where REC_STATE='YES' and DRIVER='" + driver + "' and COMPANY='" + Company + "' and DATE(ADD_TIME)='" + date + "' GROUP BY hours";
                        List<Dictionary<string, string>> result = mysql.multiple_select_list_dic(sql, new List<string>() { "Count", "weight", "hours" });
                        if (result != null)
                        {
                            //当前司机各小时报警数据
                            foreach (var dic in result)
                            {
                                string hours = dic["hours"];
                                string weight = dic["weight"];
                                TotalWeight += float.Parse(weight);
                                string Count = dic["Count"];
                                TotalCount += int.Parse(Count);
                                string FormatDate = date + "-" + hours;
                                sql = "update count_driver_hour set UNUSUAL_WEIGHT='" + weight + "',UNTRANS='" + Count + "' where DRIVER='" + driver + "' and COMPANY='" + Company + "' and date_format(ADD_TIME,'%Y-%m-%d-%h')=date_format('" + FormatDate + "','%Y-%m-%d-%h')";
                                mysql.UpdOrInsOrdel(sql);
                            }
                            //全天
                            sql = "update count_driver_day set UNTRANS='" + TotalCount + "',UNUSUAL_WEIGHT='" + TotalWeight + "' where DRIVER='" + driver + "' and COMPANY='" + Company + "' and DATE(ADD_TIME)='" + date + "'";
                            mysql.UpdOrInsOrdel(sql);
                        }
                    }
                }
                FormUtil.ModifyLable(DataForm.MainForm.Loadometer, "正常", Color.Green);
            }
            catch (Exception ex)
            {
                FormUtil.ModifyLable(DataForm.MainForm.Loadometer, "错误", Color.Red);
                LogHelper.WriteLog("计算错误-----" + ex);
            }
        }

        #endregion

        #region//车辆铲装统计

        public void CountSpade(string date)
        {
            //获取铲装车辆
            string sql = "SELECT DISTINCT VEHICLE_ID FROM temp_load_spade where COMPANY='" + Company + "' and DATE(ADD_TIME)='" + date + "'";
            List<string> list = mysql.multiple_select_list_string(sql, "VEHICLE_ID");
            try
            {
                if (list != null)
                {
                    //遍历各车铲装情况（铲装量、铲装次数）
                    foreach (string vid in list)
                    {
                        float TotalWeight = 0;//当前车全天铲装总量
                        int TotalNum = 0;//当前车全天铲装总车次
                        sql = "select SUM(WEIGHT) as weight ,COUNT(ID) as num ,HOUR(ADD_TIME) as hours from temp_load_spade where VEHICLE_ID='" + vid + "' and COMPANY='" + Company + "' and DATE(ADD_TIME)='" + date + "' GROUP BY hours";
                        List<Dictionary<string, string>> result = mysql.multiple_select_list_dic(sql, new List<string>() { "weight", "num", "hours" });
                        if (result != null)
                        {
                            //当前车各小时铲装数据（铲装量、铲装次数）
                            foreach (var dic in result)
                            {
                                string hours = dic["hours"];
                                string weight = dic["weight"];
                                TotalWeight += float.Parse(weight);
                                string num = dic["num"];
                                TotalNum += int.Parse(num);
                                string FormatDate = date + "-" + hours;
                                sql = "select COUNT(ID) as Count from count_sys_hour where VEHICLE_ID='" + vid + "' and COMPANY='" + Company + "' and date_format(ADD_TIME,'%Y-%m-%d-%h')=date_format('" + FormatDate + "','%Y-%m-%d-%h')";
                                if (mysql.GetCount(sql) != 0)
                                {
                                    sql = "update count_sys_hour set WEIGHT='" + weight + "',LOAD_NUM='" + num + "' where VEHICLE_ID='" + vid + "' and COMPANY='" + Company + "' and date_format(ADD_TIME,'%Y-%m-%d-%h')=date_format('" + FormatDate + "','%Y-%m-%d-%h')";
                                    mysql.UpdOrInsOrdel(sql);
                                }
                                else
                                {
                                    string time = date + " " + hours + ":30:00";
                                    sql = "INSERT INTO `count_sys_hour`( `VEHICLE_ID`, `TYPE`, `WEIGHT`, `UNUSUAL_WEIGHT`, `USE_FUEL`, `INJECT_FUEL`, `MILEAGE`, `LOAD_NUM`, `LOAD_FACTOR`, `UNFUEL`, `UNSPEED`, `UNTRANS`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ('" + vid + "', '挖掘机','" + weight + "', 0, 0, 0, 0, '" + num + "', '0', 0, 0, 0, '" + Company + "', '" + time + "', NULL, NULL, NULL, NULL)";
                                    mysql.UpdOrInsOrdel(sql);
                                }
                            }
                            //计算当前日期当前车辆总铲装数据
                            sql = "select COUNT(ID) as Count from count_sys_day where VEHICLE_ID='" + vid + "' and COMPANY='" + Company + "' and DATE(ADD_TIME)='" + date + "'";
                            if (mysql.GetCount(sql) != 0)
                            {
                                sql = "update count_sys_day set WEIGHT='" + TotalWeight + "',LOAD_NUM='" + TotalNum + "' where VEHICLE_ID='" + vid + "' and COMPANY='" + Company + "' and ADD_TIME='" + date + "'";
                                mysql.UpdOrInsOrdel(sql);
                            }
                            else
                            {
                                sql = "INSERT INTO `count_sys_day`( `VEHICLE_ID`, `TYPE`, `WEIGHT`, `UNUSUAL_WEIGHT`, `USE_FUEL`, `INJECT_FUEL`, `MILEAGE`, `LOAD_NUM`, `LOAD_FACTOR`, `UNFUEL`, `UNSPEED`, `UNTRANS`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ('" + vid + "', '挖掘机','" + TotalWeight + "', 0, 0, 0, 0, '" + TotalNum + "', '0', 0, 0, 0, '" + Company + "', '" + date + "', NULL, NULL, NULL, NULL)";
                                mysql.UpdOrInsOrdel(sql);
                            }
                        }
                    }
                }
                FormUtil.ModifyLable(DataForm.MainForm.Loadometer, "正常", Color.Green);
            }
            catch (Exception ex)
            {
                FormUtil.ModifyLable(DataForm.MainForm.Loadometer, "错误", Color.Red);
                LogHelper.WriteLog("计算错误-----" + ex);
            }
        }

        #endregion

        #region//司机铲装统计

        public void CountSpadeHourAndDayByDriver(string date)
        {
            FormUtil.ModifyLable(DataForm.MainForm.Loadometer, "统计-" + (TotalCount + 1), Color.Green);
            //获取铲装司机
            string sql = "SELECT DISTINCT DRIVER FROM temp_load_spade where COMPANY='" + Company + "' and DATE(ADD_TIME)='" + date + "'";
            try
            {
                List<string> list = mysql.multiple_select_list_string(sql, "DRIVER");
                if (list != null)
                {
                    //遍历各司机铲装情况，同遍历各车情况相同
                    foreach (string driver in list)
                    {
                        float TotalWeight = 0;//当前司机全天铲装总量
                        int TotalNum = 0;//当前司机全天铲装总车次
                        sql = "select SUM(WEIGHT) as weight ,COUNT(ID) as num ,HOUR(ADD_TIME) as hours from temp_load_trans where DRIVER='" + driver + "' and COMPANY='" + Company + "' and DATE(ADD_TIME)='" + date + "' GROUP BY hours";
                        List<Dictionary<string, string>> result = mysql.multiple_select_list_dic(sql, new List<string>() { "weight", "num", "hours" });
                        if (result != null)
                        {
                            //当前司机各小时铲装数据（铲装量、运输次数）
                            foreach (var dic in result)
                            {
                                string hours = dic["hours"];
                                string weight = dic["weight"];
                                TotalWeight += float.Parse(weight);
                                string num = dic["num"];
                                TotalNum += int.Parse(num);
                                string avgWeight = int.Parse(num) == 0 ? "0" : (float.Parse(weight) / int.Parse(num)).ToString();
                                string FormatDate = date + "-" + hours;
                                sql = "select COUNT(ID) as Count from count_driver_hour where DRIVER='" + driver + "' and COMPANY='" + Company + "' and date_format(ADD_TIME,'%Y-%m-%d-%h')=date_format('" + FormatDate + "','%Y-%m-%d-%h')";
                                if (mysql.GetCount(sql) != 0)
                                {
                                    sql = "update count_driver_hour set WEIGHT='" + weight + "',LOAD_NUM='" + num + "',AVGWEIGHT='" + avgWeight + "' where DRIVER='" + driver + "' and COMPANY='" + Company + "' and date_format(ADD_TIME,'%Y-%m-%d-%h')=date_format('" + FormatDate + "','%Y-%m-%d-%h')";
                                    mysql.UpdOrInsOrdel(sql);
                                }
                                else
                                {
                                    string time = date + " " + hours + ":30:00";
                                    sql = "INSERT INTO `count_driver_hour`( `DRIVER`, `WEIGHT`, `TYPE`, `UNUSUAL_WEIGHT`, `LOAD_NUM`, `AVGWEIGHT`, `UNFUEL`, `UNSPEED`, `UNTRANS`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ( '" + driver + "', '" + weight + "', '挖掘机', 0, '" + num + "', '" + avgWeight + "', 0, 0, 0, '" + Company + "', '" + time + "', NULL, NULL, NULL, NULL);";

                                    mysql.UpdOrInsOrdel(sql);
                                }
                            }
                            //计算当前日期当前司机总铲装
                            string AvgWeight = TotalNum == 0 ? "0" : (TotalWeight / TotalNum).ToString();
                            sql = "select COUNT(ID) as Count from count_driver_day where DRIVER='" + driver + "' and COMPANY='" + Company + "' and ADD_TIME='" + date + "'";
                            if (mysql.GetCount(sql) != 0)
                            {
                                sql = "update count_driver_day set WEIGHT='" + TotalWeight + "',LOAD_NUM='" + TotalNum + "',AVGWEIGHT='" + AvgWeight + "' where DRIVER='" + driver + "' and COMPANY='" + Company + "' and ADD_TIME='" + date + "'";
                                mysql.UpdOrInsOrdel(sql);
                            }
                            else
                            {
                                sql = "INSERT INTO `count_driver_day`( `DRIVER`, `WEIGHT`, `TYPE`, `UNUSUAL_WEIGHT`, `LOAD_NUM`, `AVGWEIGHT`, `WORKTIME`,`UNFUEL`, `UNSPEED`, `UNTRANS`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ('" + driver + "', '" + TotalWeight + "', '挖掘机', 0, '" + TotalNum + "', '" + AvgWeight + "',0, 0, 0, 0, '" + Company + "', '" + date + "', NULL, NULL, NULL, NULL)";
                                mysql.UpdOrInsOrdel(sql);
                            }
                        }
                    }
                }
                FormUtil.ModifyLable(DataForm.MainForm.Loadometer, "正常", Color.Green);
            }
            catch (Exception ex)
            {
                FormUtil.ModifyLable(DataForm.MainForm.Loadometer, "错误", Color.Red);
                LogHelper.WriteLog("计算错误-----" + ex);
            }
        }

        #endregion
    }
}