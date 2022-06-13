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
    public class CountGama
    {
        private readonly MySqlHelper mysql;
        private readonly string Company;
        private bool isRead = false;
        public CountGama()
        {
            mysql = new MySqlHelper();
            CountMin(null, null);
            CountHour(null, null);
            Company = ConfigurationManager.AppSettings["Company"];
            Thread thread = new Thread(CountMinGamaTimer)
            {
                IsBackground = true
            };
            thread.Start();
            Thread thread2 = new Thread(CountHourGamaTimer)
            {
                IsBackground = true
            };
            thread2.Start();
        }
        /// <summary>
        ///定时更新gama统计
        /// </summary>
        private void CountMinGamaTimer()
        {
            System.Timers.Timer CountMinTimer = new System.Timers.Timer(1000 * 60 * 2);
            CountMinTimer.Elapsed += new ElapsedEventHandler(CountMin);
            CountMinTimer.AutoReset = true;
            CountMinTimer.Enabled = true;
        }
        /// <summary>
        ///定时更新gama统计
        /// </summary>
        private void CountHourGamaTimer()
        {
            System.Timers.Timer CountHourTimer = new System.Timers.Timer(1000 * 60 * 10);
            CountHourTimer.Elapsed += new ElapsedEventHandler(CountHour);
            CountHourTimer.AutoReset = true;
            CountHourTimer.Enabled = true;
        }
        /// <summary>
        /// 指定日期本小时内各分钟gama计算
        /// </summary>
        /// <param name="date"></param>
        public void CountMin(object source, System.Timers.ElapsedEventArgs e)
        {
            //小时内最后一分钟数据遗漏部分，可忽略
            string date = DateTime.Now.ToShortDateString();
            int hour = DateTime.Now.Hour;
            string sql = "select MINUTE(ADD_TIME) as min,AVG(GAMA_FLUX) as flux ,AVG(GAMA_LOAD) as loads ,AVG(GAMA_SI) as si ,AVG(GAMA_AL) as al ,AVG(GAMA_FE) as fe ,AVG(GAMA_CA) as ca ,AVG(GAMA_MG) as mg ,AVG(GAMA_K) as k ,AVG(GAMA_NA) as na ,AVG(GAMA_S) as s ,AVG(GAMA_CL) as cl  from  gama_orig where company='" + Company + "' and DATE_FORMAT(ADD_TIME,'%Y-%m-%d-%h')=DATE_FORMAT('" + date + "-" + hour + ":00:00" + "','%Y-%m-%d-%h') GROUP BY min";
            while (isRead) {
                Thread.Sleep(2);
            }
            isRead = true;
            try
            {
                List<Dictionary<string, string>> list = mysql.MultipleSelect(sql, new List<string>() { "min", "flux", "loads", "si", "al", "fe", "ca", "mg", "k", "na", "s", "cl", });
                if (list != null)
                {
                    foreach (var dic in list)
                    {
                        sql = "select Count(ID) as Count from gama_min where company='" + Company + "' and ADD_TIME='" + date + "-" + hour + ":" + dic["min"] + ":30" + "'";
                        int count = mysql.GetCount(sql);
                        //如果存在记录且时间大于5分钟，则此数据不需要更新
                        if (count == 0)
                        {
                            sql = "INSERT INTO `gama_min`( `GAMA_FLUX`, `GAMA_LOAD`, `GAMA_SI`, `GAMA_AL`, `GAMA_FE`, `GAMA_CA`, `GAMA_MG`, `GAMA_K`, `GAMA_NA`, `GAMA_S`, `GAMA_CL`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ( '" + dic["flux"] + "', '" + dic["loads"] + "', '" + dic["si"] + "', '" + dic["al"] + "', '" + dic["fe"] + "', '" + dic["ca"] + "', '" + dic["mg"] + "', '" + dic["k"] + "', '" + dic["na"] + "', '" + dic["s"] + "', '" + dic["cl"] + "', '" + Company + "', '" + date + "-" + hour + ":" + dic["min"] + ":30" + "', NULL, NULL, NULL, NULL)";
                            mysql.UpdOrInsOrdel(sql);
                        }
                        else if (count != 0 && DateTime.Now.Minute - int.Parse(dic["min"]) < 5)
                        {
                            sql = "UPDATE `gama_min` SET `GAMA_FLUX` = '" + dic["flux"] + "', `GAMA_LOAD` = '" + dic["loads"] + "', `GAMA_SI` = '" + dic["si"] + "', `GAMA_AL` ='" + dic["al"] + "', `GAMA_FE` = '" + dic["fe"] + "', `GAMA_CA` = '" + dic["ca"] + "', `GAMA_MG` = '" + dic["mg"] + "', `GAMA_K` = '" + dic["k"] + "', `GAMA_NA` = '" + dic["na"] + "', `GAMA_S` = '" + dic["s"] + "', `GAMA_CL` = '" + dic["cl"] + "' where company='" + Company + "' and ADD_TIME='" + date + "-" + hour + ":" + dic["min"] + ":30" + "'";
                            mysql.UpdOrInsOrdel(sql);
                        }
                    }
                }
                FormUtil.ModifyLable(DataForm.MainForm.Gama, "正常", Color.Green);
            }
            catch (Exception ex)
            {
                FormUtil.ModifyLable(DataForm.MainForm.Gama, "错误", Color.Red);
                LogHelper.WriteLog("gama计算错误-----" + ex);
            }
            finally {
                isRead = false;
            }
        }

        public void CountHour(object source, System.Timers.ElapsedEventArgs e)
        {
            //小时内最后一钟数据遗漏部分，可忽略
            string date = DateTime.Now.ToShortDateString();
            string sql = "select hour(ADD_TIME) as hours,AVG(GAMA_FLUX) as flux ,AVG(GAMA_LOAD) as loads ,AVG(GAMA_SI) as si ,AVG(GAMA_AL) as al ,AVG(GAMA_FE) as fe ,AVG(GAMA_CA) as ca ,AVG(GAMA_MG) as mg ,AVG(GAMA_K) as k ,AVG(GAMA_NA) as na ,AVG(GAMA_S) as s ,AVG(GAMA_CL) as cl  from  gama_min where company='" + Company + "' and DATE_FORMAT(ADD_TIME,'%Y-%m-%d')=DATE_FORMAT('" + date + "-00:00:00" + "','%Y-%m-%d') GROUP BY hours";
            while (isRead)
            {
                Thread.Sleep(2);
            }
            isRead = true;
            List<Dictionary<string, string>> list = mysql.MultipleSelect(sql, new List<string>() { "hours", "flux", "loads", "si", "al", "fe", "ca", "mg", "k", "na", "s", "cl", });
            try
            {
                if (list != null)
                {
                    foreach (var dic in list)
                    {
                        sql = "select Count(ID) as Count from gama_hour where company='" + Company + "' and ADD_TIME='" + date + "-" + dic["hours"] + ":30:00" + "'";
                        int count = mysql.GetCount(sql);
                        //如果存在记录且时间大于5分钟，则此数据不需要更新
                        if (count == 0)
                        {
                            sql = "INSERT INTO `gama_hour`( `GAMA_FLUX`, `GAMA_LOAD`, `GAMA_SI`, `GAMA_AL`, `GAMA_FE`, `GAMA_CA`, `GAMA_MG`, `GAMA_K`, `GAMA_NA`, `GAMA_S`, `GAMA_CL`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ( '" + dic["flux"] + "', '" + dic["loads"] + "', '" + dic["si"] + "', '" + dic["al"] + "', '" + dic["fe"] + "', '" + dic["ca"] + "', '" + dic["mg"] + "', '" + dic["k"] + "', '" + dic["na"] + "', '" + dic["s"] + "', '" + dic["cl"] + "', '" + Company + "', '" + date + "-" + dic["hours"] + ":30:00" + "', NULL, NULL, NULL, NULL)";
                            mysql.UpdOrInsOrdel(sql);
                        }
                        else if (count != 0 && DateTime.Now.Hour - int.Parse(dic["hours"]) < 2)
                        {
                            sql = "UPDATE `gama_hour` SET `GAMA_FLUX` = '" + dic["flux"] + "', `GAMA_LOAD` = '" + dic["loads"] + "', `GAMA_SI` = '" + dic["si"] + "', `GAMA_AL` ='" + dic["al"] + "', `GAMA_FE` = '" + dic["fe"] + "', `GAMA_CA` = '" + dic["ca"] + "', `GAMA_MG` = '" + dic["mg"] + "', `GAMA_K` = '" + dic["k"] + "', `GAMA_NA` = '" + dic["na"] + "', `GAMA_S` = '" + dic["s"] + "', `GAMA_CL` = '" + dic["cl"] + "' where company='" + Company + "' and ADD_TIME='" + date + "-" + dic["hours"] + ":30:00" + "'";
                            mysql.UpdOrInsOrdel(sql);
                        }
                    }
                }
                FormUtil.ModifyLable(DataForm.MainForm.Gama, "正常", Color.Green);
            }
            catch (Exception ex)
            {
                FormUtil.ModifyLable(DataForm.MainForm.Gama, "错误", Color.Red);
                LogHelper.WriteLog("gama计算错误-----" + ex);
            }
            finally {
                isRead = false;
            }
        }
    }
}
