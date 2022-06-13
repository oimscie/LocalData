using LocalData.MySql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

namespace LocalData.Data
{
    public class CountMileage
    {
        private readonly MySqlHelper mysql;
        private int hourMIn;
        private int hourMax;
        private readonly string Company;
        private readonly string date;
        public CountMileage(string dates)
        {
            Company = ConfigurationManager.AppSettings["Company"];
            mysql = new MySqlHelper();
            date = DateTime.Parse(dates).AddDays(-1).ToString("yyyy-MM-dd");
            Count(null,null);
            date = dates;
            Thread thread = new Thread(CountTimer)
            {
                IsBackground = true
            };
            thread.Start();
        }
        private void Count(object source, System.Timers.ElapsedEventArgs e)
        {
            CountMileagrHour(date);
            CountMileageDay(date);
        }
        private void CountTimer()
        {
            System.Timers.Timer Timer1 = new System.Timers.Timer(1000 * 60 * 15);
            Timer1.Elapsed += new ElapsedEventHandler(Count);
            Timer1.AutoReset = true;
            Timer1.Enabled = true;
        }
        /// <summary>
        /// 各车每小时里程计算
        /// </summary>
        /// <param name="date"></param>
        public void CountMileagrHour(string date)
        {
            if (DateTime.Now.ToString("yyyy-MM-dd") != date)
            {//判断时间  1.当天--2.昨天
                hourMIn = 0;
                hourMax = 23;
            }
            else {
                if (DateTime.Now.Hour > 12)
                {
                    hourMIn = 12;
                }
                else
                {
                    hourMIn = 0;
                }
                hourMax = DateTime.Now.Hour;
            }
            try {
                for (int i = hourMIn; i <= hourMax; i++)
                {
                    string startTime = date + "-" + i + ":0:0";
                    string endTime = date + "-" + i + ":59:59";
                    //查找当前时间段内车辆
                    string sql = "select distinct VEHICLE_ID as vid from temp_posi where company='" + Company + "' and ADD_TIME between '" + startTime + "' and '" + endTime + "'";
                    List<string> list = mysql.MultipleSelect(sql, "vid", "");
                    if (list != null)
                    {
                        foreach (var vid in list)
                        {
                            //查找当前时间段内车辆历史坐标值
                            sql = "select POSI_X as x,POSI_Y as y,VEHICLE_TYPE as type from temp_posi where company='" + Company + "' and VEHICLE_ID='" + vid + "' and ADD_TIME between '" + startTime + "' and '" + endTime + "' order by ADD_TIME asc";
                            List<Dictionary<string, string>> posi_list = mysql.MultipleSelect(sql, new List<string>() { "x", "y", "type" });
                            if (posi_list != null)
                            {
                                //计算当前时间段内当前车辆移动距离
                                double distance = 0;
                                for (int index = 0; index < posi_list.Count - 1; index++)
                                {
                                    double x2 = Math.Pow((double.Parse(posi_list[index + 1]["x"]) - double.Parse(posi_list[index]["x"])), 2);
                                    double y2 = Math.Pow((double.Parse(posi_list[index + 1]["y"]) - double.Parse(posi_list[index]["y"])), 2);
                                    distance += Math.Sqrt(x2 + y2)/1000;
                                }
                                //检查当前车辆超速记录
                                sql = "Select COUNT(ID) as Count from rec_unu_speed where VEHICLE_ID='" + vid + "' and company='" + Company + "' and ADD_TIME between '" + startTime + "' and '" + endTime + "'";
                                int count = mysql.GetCount(sql);
                                //插入数据库
                                sql = "select COUNT(ID) as Count from count_sys_hour where company='" + Company + "' and VEHICLE_ID='" + vid + "' and ADD_TIME ='" + date + "-" + i + ":30:00" + "'";
                                if (mysql.GetCount(sql) != 0)
                                {
                                    sql = "update count_sys_hour set MILEAGE='" + distance + "',UNSPEED='" + count + "' where company='" + Company + "' and VEHICLE_ID='" + vid + "' and ADD_TIME ='" + date + "-" + i + ":30:00" + "'";
                                    mysql.UpdOrInsOrdel(sql);
                                }
                                else
                                {
                                    sql = "INSERT INTO `count_sys_hour`( `VEHICLE_ID`, `TYPE`, `WEIGHT`, `UNUSUAL_WEIGHT`, `USE_FUEL`, `INJECT_FUEL`, `MILEAGE`, `LOAD_NUM`, `LOAD_FACTOR`, `UNFUEL`, `UNSPEED`, `UNTRANS`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ('" + vid + "', '" + posi_list[0]["type"] + "', 0, 0, 0, 0, '" + distance + "', 0, 0.00, 0, '" + count + "', 0, '" + Company + "', '" + date + "-" + i + ":30:00" + "', NULL, NULL, NULL, NULL)";
                                    mysql.UpdOrInsOrdel(sql);
                                }

                            }
                        }
                    }
                }
                FormUtil.ModifyLable(DataForm.MainForm.MileMonitor, "正常", Color.Green);
            }
            catch (Exception ex)
            {
                FormUtil.ModifyLable(DataForm.MainForm.MileMonitor, "错误", Color.Red);
                LogHelper.WriteLog("里程计算错误-----" + ex);
            }
        }

        //计算指定日期全天的里程及超速报警数据
        public void CountMileageDay(string date)
        {
            string sql = "select VEHICLE_ID as vid,TYPE as type,SUM(MILEAGE) as mileage,SUM(UNSPEED) as unspeed from count_sys_hour where company='" + Company + "' and DATE_FORMAT(ADD_TIME,'%Y-%m-%d')='" + date + "' group by VEHICLE_ID";
            List<Dictionary<string, string>> list = mysql.MultipleSelect(sql, new List<string>() { "mileage", "unspeed", "vid", "type" });
            try
            {
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        sql = "select COUNT(ID) as Count from count_sys_day where company='" + Company + "' and ADD_TIME='" + date + "' and VEHICLE_ID='" + item["vid"] + "'";
                        if (mysql.GetCount(sql) != 0)
                        {
                            sql = "update count_sys_day set MILEAGE='" + item["mileage"] + "',UNSPEED='" + item["unspeed"] + "' where company='" + Company + "' and ADD_TIME='" + date + "' and VEHICLE_ID='" + item["vid"] + "'";
                            mysql.UpdOrInsOrdel(sql);
                        }
                        else
                        {
                            sql = "INSERT INTO `count_sys_day`(`VEHICLE_ID`, `TYPE`, `WEIGHT`, `UNUSUAL_WEIGHT`, `USE_FUEL`, `INJECT_FUEL`, `MILEAGE`, `LOAD_NUM`, `LOAD_FACTOR`, `UNFUEL`, `UNSPEED`, `UNTRANS`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ('" + item["vid"] + "', '" + item["type"] + "',0, 0, 0, 0, '" + item["mileage"] + "', 0, 0.00, 0, '" + item["unspeed"] + "', 0, '" + Company + "', '" + date + "', NULL, NULL, NULL, NULL)";
                            mysql.UpdOrInsOrdel(sql);
                        }
                    }
                }
                FormUtil.ModifyLable(DataForm.MainForm.MileMonitor, "正常", Color.Green);
            }
            catch (Exception ex)
            {
                FormUtil.ModifyLable(DataForm.MainForm.MileMonitor, "错误", Color.Red);
                LogHelper.WriteLog("里程计算错误-----" + ex);
            }
        }
    }
}
