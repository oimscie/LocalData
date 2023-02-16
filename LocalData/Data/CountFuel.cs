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
    public class CountFuel
    {
        private readonly MySqlHelper mysql;
        private int hourMIn;
        private int hourMax;
        private readonly string Company;
        private readonly float Fuel;
        private readonly string date;
        private bool isRead = false;

        /// <summary>
        /// 统计过程标记
        /// </summary>
        private int TotalCount = 0;

        public CountFuel(string dates)
        {
            Company = ConfigurationManager.AppSettings["Company"];
            Fuel = float.Parse(ConfigurationManager.AppSettings["Fuel"]);
            mysql = new MySqlHelper();
            Thread CheckThread = new Thread(Check)
            {
                IsBackground = true
            };
            CheckThread.Start();
            date = DateTime.Parse(dates).AddDays(-1).ToString("yyyy-MM-dd");
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
            CountFuelHour(date);
            CountFuelDay(date);
            CountHourUnFuelByDriver(date, "运输车");
            CountHourUnFuelByDriver(date, "挖掘机");
            CountDayUnFuelByDriver(date, "运输车");
            CountDayUnFuelByDriver(date, "挖掘机");
            TotalCount = 0;
        }

        private void CountTimer()
        {
            System.Timers.Timer Timer1 = new System.Timers.Timer(1000 * 60 * 15);
            Timer1.Elapsed += new ElapsedEventHandler(Count);
            Timer1.AutoReset = true;
            Timer1.Enabled = true;
        }

        private void Check()
        {
            System.Timers.Timer Timer1 = new System.Timers.Timer(1000 * 60 * 10);
            Timer1.Elapsed += new ElapsedEventHandler(CheckFuel);
            Timer1.AutoReset = true;
            Timer1.Enabled = true;
        }

        /// <summary>
        /// 统计指定日期的各小时油耗与加油量
        /// </summary>
        /// <param name="date"></param>
        public void CountFuelHour(string date)
        {
            while (isRead)
            {
                Thread.Sleep(2);
            }
            isRead = true;
            FormUtil.ModifyLable(DataForm.MainForm.IcoMonitor, "统计-" + (TotalCount += 1), Color.Green);
            if (DateTime.Now.ToString("yyyy-MM-dd") != date)
            {//判断时间  1.当天--2.昨天
                hourMIn = 0;
                hourMax = 23;
            }
            else
            {
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
            try
            {
                for (int i = hourMIn; i <= hourMax; i++)
                {
                    string startTime = date + "-" + i + ":0:0";
                    string endTime = date + "-" + i + ":59:59";
                    //查找当前时间段内车辆
                    string sql = "select a.vid,b.type from (select distinct VEHICLE_ID as vid from temp_fuel_fit where company='" + Company + "' and ADD_TIME between '" + startTime + "' and '" + endTime + "') a inner join (select VEHICLE_ID,VEHICLE_TYPE as type from list_vehicle where company='" + Company + "')b on a.vid=b.VEHICLE_ID";
                    List<Dictionary<string, string>> list = mysql.multiple_select_list_dic(sql, new List<string>() { "vid", "type" });
                    if (list != null)
                    {
                        foreach (var dic in list)
                        {
                            //查找当前时间段内车辆历史油位值
                            sql = "select REAL_FUEL as fuel from temp_fuel_fit where company='" + Company + "' and VEHICLE_ID='" + dic["vid"] + "' and ADD_TIME between '" + startTime + "' and '" + endTime + "' order by ADD_TIME asc";
                            List<double> fuel = mysql.multiple_select_list_double(sql, "fuel");
                            if (fuel != null)
                            {
                                //计算当前时间段内当前车辆加油与耗油
                                List<double> result = CurveOfCutting(fuel);
                                //检查当前车辆油耗异常记录
                                sql = "Select COUNT(ID) as Count from rec_unu_fuel where VEHICLE_ID='" + dic["vid"] + "' and company='" + Company + "' and ADD_TIME between '" + startTime + "' and '" + endTime + "'";
                                int count = mysql.GetCount(sql);
                                //插入数据库
                                sql = "select COUNT(ID) as Count from count_sys_hour where company='" + Company + "' and VEHICLE_ID='" + dic["vid"] + "' and ADD_TIME ='" + date + "-" + i + ":30:00" + "'";
                                if (mysql.GetCount(sql) != 0)
                                {
                                    sql = "update count_sys_hour set USE_FUEL='" + result[0] + "',INJECT_FUEL='" + result[1] + "',UNFUEL='" + count + "' where company='" + Company + "' and VEHICLE_ID='" + dic["vid"] + "' and ADD_TIME ='" + date + "-" + i + ":30:00" + "'";
                                    mysql.UpdOrInsOrdel(sql);
                                }
                                else
                                {
                                    sql = "INSERT INTO `count_sys_hour`( `VEHICLE_ID`, `TYPE`, `WEIGHT`, `UNUSUAL_WEIGHT`, `USE_FUEL`, `INJECT_FUEL`, `MILEAGE`, `LOAD_NUM`, `LOAD_FACTOR`, `UNFUEL`, `UNSPEED`, `UNTRANS`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ('" + dic["vid"] + "', '" + dic["type"] + "', 0, 0, '" + result[0] + "', '" + result[1] + "', '0', 0, 0.00, '" + count + "',0, 0, '" + Company + "', '" + date + "-" + i + ":30:00" + "', NULL, NULL, NULL, NULL)";
                                    mysql.UpdOrInsOrdel(sql);
                                }
                            }
                        }
                    }
                }
                FormUtil.ModifyLable(DataForm.MainForm.IcoMonitor, "正常", Color.Green);
            }
            catch (Exception ex)
            {
                FormUtil.ModifyLable(DataForm.MainForm.IcoMonitor, "错误", Color.Red);
                LogHelper.WriteLog("油耗计算错误-----" + ex);
            }
            finally
            {
                isRead = false;
            }
        }

        /// <summary>
        ///  统计指定日期的全天油耗与加油量
        /// </summary>
        /// <param name="date"></param>
        public void CountFuelDay(string date)
        {
            while (isRead)
            {
                Thread.Sleep(2);
            }
            isRead = true;
            FormUtil.ModifyLable(DataForm.MainForm.IcoMonitor, "统计-" + (TotalCount += 1), Color.Green);
            try
            {
                string sql = "select VEHICLE_ID as vid,TYPE as type,SUM(USE_FUEL) as useFuel,SUM(INJECT_FUEL) as inject,SUM(UNFUEL) as unfuel from count_sys_hour where company='" + Company + "' and DATE_FORMAT(ADD_TIME,'%Y-%m-%d')='" + date + "' group by VEHICLE_ID";
                List<Dictionary<string, string>> list = mysql.multiple_select_list_dic(sql, new List<string>() { "useFuel", "inject", "unfuel", "vid", "type" });
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        sql = "select COUNT(ID) as Count from count_sys_day where company='" + Company + "' and ADD_TIME='" + date + "' and VEHICLE_ID='" + item["vid"] + "'";
                        if (mysql.GetCount(sql) != 0)
                        {
                            sql = "update count_sys_day set USE_FUEL='" + item["useFuel"] + "',INJECT_FUEL='" + item["inject"] + "',UNFUEL='" + item["unfuel"] + "' where company='" + Company + "' and ADD_TIME='" + date + "' and VEHICLE_ID='" + item["vid"] + "'";
                            mysql.UpdOrInsOrdel(sql);
                        }
                        else
                        {
                            sql = "INSERT INTO `count_sys_day`(`VEHICLE_ID`, `TYPE`, `WEIGHT`, `UNUSUAL_WEIGHT`, `USE_FUEL`, `INJECT_FUEL`, `MILEAGE`, `LOAD_NUM`, `LOAD_FACTOR`, `UNFUEL`, `UNSPEED`, `UNTRANS`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ('" + item["vid"] + "', '" + item["type"] + "',0, 0, '" + item["useFuel"] + "', '" + item["inject"] + "', 0, 0, 0.00, '" + item["unfuel"] + "', 0, 0, '" + Company + "', '" + date + "', NULL, NULL, NULL, NULL)";
                            mysql.UpdOrInsOrdel(sql);
                        }
                    }
                }
                FormUtil.ModifyLable(DataForm.MainForm.IcoMonitor, "正常", Color.Green);
            }
            catch (Exception ex)
            {
                FormUtil.ModifyLable(DataForm.MainForm.IcoMonitor, "错误", Color.Red);
                LogHelper.WriteLog("油耗计算错误-----" + ex);
            }
            finally
            {
                isRead = false;
            }
        }

        /// <summary>
        /// 连续油位值数据分割计算，返回[0]：耗油,[1]:加油
        /// </summary>
        /// <param name="list"></param>
        /// <returns>List<double></returns>
        public List<double> CurveOfCutting(List<double> list)
        {
            double InRemain = 0;
            double OutRemain = 0;
            List<double> back = new List<double> { 0, 0 };
            Queue<List<double>> queue = new Queue<List<double>>();
            List<double> RealList = new List<double>();
            for (int i = 0; i < list.Count - 1; i++)
            {
                if (list[i] != list[i + 1])
                {
                    RealList.Add(list[i]);
                }
            }
            RealList.Add(list[list.Count - 1]);
            if (RealList.Count < 2) { return back; }
            int index = 0;
            if (RealList[0] - RealList[1] > 0)
            {
                while (index < RealList.Count)
                {
                    for (int i = index; i < RealList.Count - 1; i++)
                    {
                        if (RealList[i] < RealList[i + 1])
                        {
                            queue.Enqueue(RealList.Skip(index).Take(i - index + 1).ToList());
                            index = i;
                            break;
                        }
                        else if (i == RealList.Count - 2)
                        {
                            queue.Enqueue(RealList.Skip(index).Take(i - index + 2).ToList());
                            index = RealList.Count;
                            break;
                        }
                    }
                    for (int i = index; i < RealList.Count - 1; i++)
                    {
                        if (RealList[i] > RealList[i + 1])
                        {
                            queue.Enqueue(RealList.Skip(index).Take(i - index + 1).ToList());
                            index = i;
                            break;
                        }
                        else if (i == RealList.Count - 2)
                        {
                            queue.Enqueue(RealList.Skip(index).Take(i - index + 2).ToList());
                            index = RealList.Count;
                            break;
                        }
                    }
                }
            }
            else
            {
                while (index < RealList.Count)
                {
                    for (int i = index; i < RealList.Count - 1; i++)
                    {
                        if (RealList[i] > RealList[i + 1])
                        {
                            queue.Enqueue(RealList.Skip(index).Take(i - index + 1).ToList());
                            index = i;
                            break;
                        }
                        else if (i == RealList.Count - 2)
                        {
                            queue.Enqueue(RealList.Skip(index).Take(i - index + 2).ToList());
                            index = RealList.Count;
                            break;
                        }
                    }
                    for (int i = index; i < RealList.Count - 1; i++)
                    {
                        if (RealList[i] < RealList[i + 1])
                        {
                            queue.Enqueue(RealList.Skip(index).Take(i - index + 1).ToList());
                            index = i;
                            break;
                        }
                        else if (i == RealList.Count - 2)
                        {
                            queue.Enqueue(RealList.Skip(index).Take(i - index + 2).ToList());
                            index = RealList.Count;
                            break;
                        }
                    }
                }
            }
            while (queue.Count > 0)
            {
                List<double> temp = queue.Dequeue();
                if (temp[0] > temp[1])//下降段
                {
                    if (queue.Count == 0)
                    {
                        if (temp[0] > temp[1])//下降段
                        {
                            //耗油量
                            OutRemain += temp.First() - temp.Last();
                        }
                        else
                        { //上升段
                          //加油
                            if ((temp.Average() - temp.Min()) > 40)
                            {
                                InRemain += temp.Last() - temp.First();
                            }
                        }
                    }
                    while (queue.Count > 0)
                    {
                        List<double> temp2 = queue.Dequeue();
                        if ((temp2.Average() - temp2.Min()) < 40)
                        {
                            temp = temp.Concat(temp2).ToList();
                            //耗油量
                            OutRemain += temp.First() - temp.Last();
                            temp.Clear();
                            break;
                        }
                        else
                        {
                            //耗油量
                            if (temp.Count > 0)
                            {
                                OutRemain += temp.First() - temp.Last();
                                temp.Clear();
                            }
                            //加油
                            InRemain += temp2.Last() - temp2.First();
                            break;
                        }
                    }
                }
                else
                { //上升段
                    if (queue.Count == 0)
                    {
                        if (temp[0] > temp[1])//下降段
                        {
                            //耗油量
                            OutRemain += temp.First() - temp.Last();
                        }
                        else
                        { //上升段
                          //加油
                            if ((temp.Average() - temp.Min()) > 40)
                            {
                                InRemain += temp.Last() - temp.First();
                            }
                        }
                    }
                    while (queue.Count > 0)
                    {
                        if ((temp.Average() - temp.Min()) > 40)
                        {
                            //加油
                            InRemain += temp.Last() - temp.First();
                        }
                        else
                        {
                            //耗油量
                            OutRemain += temp.First() - temp.Last();
                        }
                        temp.Clear();
                        break;
                    }
                }
            }
            back[0] = OutRemain < 0 ? 0 : OutRemain;
            back[1] = InRemain < 0 ? 0 : InRemain;
            return back;
        }

        public void CheckFuel(object source, System.Timers.ElapsedEventArgs e)
        {
            while (isRead)
            {
                Thread.Sleep(2);
            }
            isRead = true;
            try
            {
                string sql = "SELECT DISTINCT VEHICLE_ID as vid from temp_fuel_fit where COMPANY='" + Company + "' and ADD_TIME>=DATE_SUB(NOW(),INTERVAL 30 MINUTE)";
                List<string> VehicleList = mysql.multiple_select_list_string(sql, "vid");
                if (VehicleList == null)
                {
                    return;
                }
                foreach (var item in VehicleList)
                {
                    sql = "SELECT REAL_FUEL ,ID from temp_fuel_fit where COMPANY='" + Company + "' and VEHICLE_ID='" + item + "'  and ADD_TIME>=DATE_SUB(NOW(),INTERVAL 30 MINUTE) ORDER BY REAL_FUEL desc LIMIT 0,1";
                    List<Dictionary<string, string>> max = mysql.multiple_select_list_dic(sql, new List<string>() { "REAL_FUEL", "ID" });

                    sql = "SELECT REAL_FUEL ,ID from temp_fuel_fit where COMPANY='" + Company + "' and VEHICLE_ID='" + item + "'  and ADD_TIME>=DATE_SUB(NOW(),INTERVAL 30 MINUTE) ORDER BY REAL_FUEL  LIMIT 0,1";
                    List<Dictionary<string, string>> min = mysql.multiple_select_list_dic(sql, new List<string>() { "REAL_FUEL", "ID" });

                    if (max != null && min != null)
                    {
                        max[0].TryGetValue("REAL_FUEL", out string maxFuel);
                        max[0].TryGetValue("ID", out string maxId);
                        min[0].TryGetValue("REAL_FUEL", out string minFuel);
                        min[0].TryGetValue("ID", out string minId);
                        float data = float.Parse(maxFuel) - float.Parse(minFuel);
                        if (data > Fuel && int.Parse(maxId) < int.Parse(minId))//判断油量最大值是否在前
                        {
                            sql = "select COUNT(ID) as Count from rec_unu_fuel where COMPANY='" + Company + "' and VEHICLE_ID='" + item + "' and USE_FUEL='" + data + "'  and ADD_TIME>=DATE_SUB(NOW(),INTERVAL 30 MINUTE)";
                            if (mysql.GetCount(sql) == 0)
                            {
                                sql = "select VEHICLE_ID as vid , VEHICLE_DRIVER as driver , VEHICLE_TYPE as type from list_vehicle  where COMPANY='" + Company + "' and VEHICLE_ID='" + item + "'";
                                List<Dictionary<string, string>> result = mysql.multiple_select_list_dic(sql, new List<string>() { "vid", "driver", "type" });
                                result[0].TryGetValue("vid", out string vid);
                                result[0].TryGetValue("driver", out string driver);
                                result[0].TryGetValue("type", out string type);
                                sql = "INSERT INTO `rec_unu_fuel`( `VEHICLE_ID`, `VEHICLE_TYPE`, `USE_FUEL`, `DRIVER`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ( '" + vid + "', '" + type + "', '" + data + "', '" + driver + "', '" + Company + "', '" + DateTime.Now + "', NULL, NULL, NULL, NULL)";
                                mysql.UpdOrInsOrdel(sql);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog("油量检测错误----" + ex);
            }
            finally
            {
                isRead = false;
            }
        }

        #region 司机统计

        /// <summary>
        /// 统计各小时司机燃油报警,按类型统计（运输/挖掘）
        /// </summary>
        /// <param name="date"></param>
        public void CountHourUnFuelByDriver(string date, string count_type)
        {
            FormUtil.ModifyLable(DataForm.MainForm.IcoMonitor, "统计-" + (TotalCount += 1), Color.Green);
            if (DateTime.Now.ToString("yyyy-MM-dd") != date)
            {//判断时间  1.当天--2.昨天
                hourMIn = 0;
                hourMax = 23;
            }
            else
            {
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
            try
            {
                for (int i = hourMIn; i <= hourMax; i++)
                {
                    string startTime = date + "-" + i + ":0:0";
                    string endTime = date + "-" + i + ":59:59";
                    //查找当前时间段内车辆
                    string sql = "select distinct DRIVER as driver from rec_unu_fuel where company='" + Company + "' and VEHICLE_TYPE='" + count_type + "' and ADD_TIME between '" + startTime + "' and '" + endTime + "'";
                    List<string> list = mysql.multiple_select_list_string(sql, "driver");
                    if (list != null)
                    {
                        foreach (var driver in list)
                        {
                            //检查当前车辆燃油报警记录
                            sql = "Select COUNT(ID) as Count from rec_unu_fuel where DRIVER='" + driver + "' and company='" + Company + "' and VEHICLE_TYPE='" + count_type + "' and ADD_TIME between '" + startTime + "' and '" + endTime + "'";
                            int count = mysql.GetCount(sql);
                            //插入数据库
                            sql = "select COUNT(ID) as Count from count_driver_hour where company='" + Company + "' and TYPE='" + count_type + "' and DRIVER='" + driver + "' and ADD_TIME ='" + date + "-" + i + ":30:00" + "'";
                            if (mysql.GetCount(sql) != 0)
                            {
                                sql = "update count_driver_hour set UNFUEL='" + count + "' where company='" + Company + "' and TYPE='" + count_type + "' and driver='" + driver + "' and ADD_TIME ='" + date + "-" + i + ":30:00" + "'";
                                mysql.UpdOrInsOrdel(sql);
                            }
                            else
                            {
                                sql = "INSERT INTO `count_driver_hour`( `DRIVER`, `WEIGHT`, `TYPE`, `UNUSUAL_WEIGHT`, `LOAD_NUM`, `AVGWEIGHT`, `UNFUEL`, `UNSPEED`, `UNTRANS`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ('" + driver + "', '0', '" + count_type + "','0', '0', '0','" + count + "','0', 0, '" + Company + "', '" + date + "-" + i + ":30:00" + "', NULL, NULL, NULL, NULL);";
                                mysql.UpdOrInsOrdel(sql);
                            }
                        }
                    }
                }
                FormUtil.ModifyLable(DataForm.MainForm.IcoMonitor, "正常", Color.Green);
            }
            catch (Exception ex)
            {
                FormUtil.ModifyLable(DataForm.MainForm.IcoMonitor, "错误", Color.Red);
                LogHelper.WriteLog("司机超速统计错误-----" + ex);
            }
        }

        //计算指定日期全天的司机燃油报警数据
        public void CountDayUnFuelByDriver(string date, string count_type)
        {
            FormUtil.ModifyLable(DataForm.MainForm.IcoMonitor, "统计-" + (TotalCount += 1), Color.Green);
            string sql = "select DRIVER as driver,TYPE as type,SUM(UNFUEL) as unfuel from count_driver_hour where company='" + Company + "'  and TYPE='" + count_type + "' and DATE_FORMAT(ADD_TIME,'%Y-%m-%d')='" + date + "' group by DRIVER";
            List<Dictionary<string, string>> list = mysql.multiple_select_list_dic(sql, new List<string>() { "unfuel", "driver", "type" });
            try
            {
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        sql = "select COUNT(ID) as Count from count_driver_day where company='" + Company + "' and TYPE='" + count_type + "' and ADD_TIME='" + date + "' and DRIVER='" + item["driver"] + "'";
                        if (mysql.GetCount(sql) != 0)
                        {
                            sql = "update count_driver_day set UNFUEL='" + item["unfuel"] + "' where company='" + Company + "' and TYPE='" + count_type + "' and ADD_TIME='" + date + "' and DRIVER='" + item["driver"] + "'";
                            mysql.UpdOrInsOrdel(sql);
                        }
                        else
                        {
                            sql = "INSERT INTO `count_driver_day`( `DRIVER`, `WEIGHT`, `TYPE`, `UNUSUAL_WEIGHT`, `LOAD_NUM`, `AVGWEIGHT`, `WORKTIME`,`UNFUEL`, `UNSPEED`, `UNTRANS`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ('" + item["driver"] + "', '0', '" + count_type + "', '0', '0', '0','0', '" + item["unfuel"] + "', '0', '0', '" + Company + "', '" + date + "', NULL, NULL, NULL, NULL);";
                            mysql.UpdOrInsOrdel(sql);
                        }
                    }
                }
                FormUtil.ModifyLable(DataForm.MainForm.IcoMonitor, "正常", Color.Green);
            }
            catch (Exception ex)
            {
                FormUtil.ModifyLable(DataForm.MainForm.IcoMonitor, "错误", Color.Red);
                LogHelper.WriteLog("里程计算错误-----" + ex);
            }
        }

        #endregion 司机统计
    }
}