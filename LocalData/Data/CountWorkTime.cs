using LocalData.MySql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace LocalData.Data
{
    public class CountWorkTime
    {
        private readonly MySqlHelper mysql;
        private readonly string company;
        private readonly string date;

        public CountWorkTime(string dates)
        {
            mysql = new MySqlHelper();
            company = ConfigurationManager.AppSettings["Company"];
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

        private void CountTimer()
        {
            System.Timers.Timer Timer = new System.Timers.Timer(1000 * 60 * 15);
            Timer.Elapsed += new ElapsedEventHandler(Count);
            Timer.AutoReset = true;
            Timer.Enabled = true;
        }

        private void Count(object source, System.Timers.ElapsedEventArgs e)
        {
            CountWorkTimeDay(date);
        }

        public void CountWorkTimeDay(string date)
        {
            string sql = "select distinct USERNAME as name from rec_clockin_temp where company='" + company + "' and add_time='" + date + "'";

            List<string> list = mysql.multiple_select_list_string(sql, "name");
            if (list == null)
            {
                return;
            }
            foreach (var item in list)
            {
                try
                {
                    sql = "select FIRSTCLOCK as start,LASTCLOCK as end from rec_clockin_temp where company='" + company + "' and USERNAME='" + item + "' and add_time='" + date + "'";
                    Dictionary<string, string> dic = mysql.SingleSelect(sql, new string[] { "start", "end" });
                    double result = DiffHours(Convert.ToDateTime(dic.First().Value), Convert.ToDateTime(dic.Last().Value));
                    sql = "select COUNT(ID) as Count from count_driver_day where DRIVER='" + item + "' and COMPANY='" + company + "' and ADD_TIME='" + date + "'";
                    if (mysql.GetCount(sql) != 0)
                    {
                        sql = "update count_driver_day set WORKTIME='" + result + "' where DRIVER='" + item + "' and COMPANY='" + company + "' and ADD_TIME='" + date + "'";
                        mysql.UpdOrInsOrdel(sql);
                    }
                    else
                    {
                        sql = "INSERT INTO `count_driver_day`( `DRIVER`, `WEIGHT`, `TYPE`, `UNUSUAL_WEIGHT`, `LOAD_NUM`, `AVGWEIGHT`, `WORKTIME`,`UNFUEL`, `UNSPEED`, `UNTRANS`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ('" + item + "', '0', '其他', 0, '0', '0','" + result + "', 0, 0, 0, '" + company + "', '" + date + "', NULL, NULL, NULL, NULL)";
                        mysql.UpdOrInsOrdel(sql);
                    }
                }
                catch (Exception e)
                {
                    LogHelper.WriteLog("工作时长统计错误-------", e);
                }
            }
        }

        public double DiffHours(DateTime startTime, DateTime endTime)
        {
            TimeSpan hoursSpan = new TimeSpan(endTime.Ticks - startTime.Ticks);
            return hoursSpan.TotalHours;
        }
    }
}