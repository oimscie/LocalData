﻿using LocalData.MySql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

namespace LocalData.Data
{
    public class CountMonths
    {
        private readonly MySqlHelper mysql;
        private readonly string Company;
        private readonly string date;

        public CountMonths(string dates)
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

        #region//车辆统计

        private void Count(object source, System.Timers.ElapsedEventArgs e)
        {
            CountMonth(date);
            CountDrive("挖掘机", date);
            CountDrive("运输车", date);
            CountDrive("其他", date);
        }

        private void CountTimer()
        {
            System.Timers.Timer Timer1 = new System.Timers.Timer(1000 * 60 * 15);
            Timer1.Elapsed += new ElapsedEventHandler(Count);
            Timer1.AutoReset = true;
            Timer1.Enabled = true;
        }

        /// <summary>
        /// 全月统计
        /// </summary>
        /// <param name="date">日期（Y-M-d）</param>
        public void CountMonth(string date)
        {
            //获取全月车辆数据（车辆编号、车辆类型、运输总和、非正常运输总和、燃油消耗总和、加油总和、里程总和、车次总和、平均满载率、油耗异常总数、超速异常总数、非正常运输总数）
            string sql = "SELECT VEHICLE_ID as id,TYPE as type,SUM(WEIGHT) as weight ,SUM(UNUSUAL_WEIGHT) as unu_weight,SUM(USE_FUEL) as fule,SUM(INJECT_FUEL) as inject,SUM(MILEAGE) as mileage,SUM(LOAD_NUM) as num,SUM(UNFUEL) as unfuel,SUM(UNSPEED) as unspeed, SUM(UNTRANS) as untrans from count_sys_day where  COMPANY='" + Company + "' and DATE_FORMAT(ADD_TIME,'%Y-%m')=DATE_FORMAT('" + date + "','%Y-%m') GROUP BY VEHICLE_ID";
            List<Dictionary<string, string>> list = mysql.multiple_select_list_dic(sql, new List<string>() { "id", "type", "weight", "unu_weight", "fule", "inject", "mileage", "num", "unfuel", "unspeed", "untrans" });
            try
            {
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        sql = "select VEHICLE_LOAD as loads from list_vehicle where VEHICLE_ID='" + item["id"] + "' and COMPANY='" + Company + "'";
                        string loadResult = mysql.SingleSelectfield(sql, "loads");
                        //车辆核定载荷，默认50
                        float load = loadResult == null ? load = 50 : load = float.Parse(loadResult);
                        //满载率默认0
                        float factors = item["num"] == "0" || load == 0 ? 0 : float.Parse(item["weight"]) / float.Parse(item["num"]) / load;
                        sql = "select COUNT(ID) as Count from count_sys_month where company='" + Company + "' and VEHICLE_ID='" + item["id"] + "' and DATE_FORMAT(ADD_TIME,'%Y-%m')=DATE_FORMAT('" + date + "','%Y-%m')";
                        if (mysql.GetCount(sql) != 0)
                        {
                            sql = "UPDATE `count_sys_month` SET `VEHICLE_ID` = '" + item["id"] + "', `TYPE` = '" + item["type"] + "', `WEIGHT` = '" + item["weight"] + "', `UNUSUAL_WEIGHT` = '" + item["unu_weight"] + "', `USE_FUEL` = '" + item["fule"] + "', `INJECT_FUEL` = '" + item["inject"] + "', `MILEAGE` = '" + item["mileage"] + "', `LOAD_NUM` = '" + item["num"] + "', `LOAD_FACTOR` = '" + factors + "', `UNFUEL` = '" + item["unfuel"] + "', `UNSPEED` = '" + item["unspeed"] + "', `UNTRANS` = '" + item["untrans"] + "' WHERE company='" + Company + "' and VEHICLE_ID='" + item["id"] + "' and DATE_FORMAT(ADD_TIME,'%Y-%m')=DATE_FORMAT('" + date + "','%Y-%m')";
                            mysql.UpdOrInsOrdel(sql);
                        }
                        else
                        {
                            string[] dateStr = date.Split('-');
                            string tempdate = dateStr[0] + '-' + dateStr[1] + "-1";
                            sql = "INSERT INTO `count_sys_month`( `VEHICLE_ID`, `TYPE`, `WEIGHT`, `UNUSUAL_WEIGHT`, `USE_FUEL`, `INJECT_FUEL`, `MILEAGE`, `LOAD_NUM`, `LOAD_FACTOR`, `UNFUEL`, `UNSPEED`, `UNTRANS`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ( '" + item["id"] + "', '" + item["type"] + "', '" + item["weight"] + "', '" + item["unu_weight"] + "', '" + item["fule"] + "', '" + item["inject"] + "', '" + item["mileage"] + "', '" + item["num"] + "', '" + factors + "', '" + item["unfuel"] + "', '" + item["unspeed"] + "', '" + item["untrans"] + "', '" + Company + "', '" + tempdate + "', NULL, NULL, NULL, NULL)";
                            mysql.UpdOrInsOrdel(sql);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog("月统计错误-----" + ex);
            }
        }

        #endregion

        #region//司机统计

        public void CountDrive(string type, string date)
        {
            //获取司机
            string sql = "SELECT DRIVER as driver,SUM(WEIGHT) as weight ,SUM(UNUSUAL_WEIGHT) as unu_weight,SUM(LOAD_NUM) as num,AVG(AVGWEIGHT) as avgweight,SUM(WORKTIME) as workTime,SUM(UNFUEL) as unfuel,SUM(UNSPEED) as unspeed, SUM(UNTRANS) as untrans from count_driver_day where TYPE='" + type + "' and  COMPANY='" + Company + "' and DATE_FORMAT(ADD_TIME,'%Y-%m')=DATE_FORMAT('" + date + "','%Y-%m') GROUP BY DRIVER";
            List<Dictionary<string, string>> list = mysql.multiple_select_list_dic(sql, new List<string>() { "driver", "weight", "unu_weight", "num", "avgweight", "workTime", "unfuel", "unspeed", "untrans" });
            try
            {
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        sql = "select COUNT(ID) as Count from count_driver_month where company='" + Company + "' and DRIVER='" + item["driver"] + "' and TYPE='" + type + "' and DATE_FORMAT(ADD_TIME,'%Y-%m')=DATE_FORMAT('" + date + "','%Y-%m')";
                        if (mysql.GetCount(sql) != 0)
                        {
                            sql = "UPDATE `count_driver_month` SET   `WEIGHT` = '" + item["weight"] + "', `UNUSUAL_WEIGHT` = '" + item["unu_weight"] + "', `LOAD_NUM` = '" + item["num"] + "', `AVGWEIGHT` = '" + item["avgweight"] + "',`WORKTIME`='" + item["workTime"] + "', `UNFUEL` = '" + item["unfuel"] + "', `UNSPEED` = '" + item["unspeed"] + "', `UNTRANS` = '" + item["untrans"] + "' WHERE company='" + Company + "' and DRIVER='" + item["driver"] + "' and DATE_FORMAT(ADD_TIME,'%Y-%m')=DATE_FORMAT('" + date + "','%Y-%m')";
                            mysql.UpdOrInsOrdel(sql);
                        }
                        else
                        {
                            string[] dateStr = date.Split('-');
                            string tempdate = dateStr[0] + '-' + dateStr[1] + "-1";
                            sql = "INSERT INTO `count_driver_month`( `DRIVER`, `TYPE`, `WEIGHT`, `UNUSUAL_WEIGHT`,`LOAD_NUM`, `AVGWEIGHT`, `WORKTIME`,`UNFUEL`, `UNSPEED`, `UNTRANS`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ( '" + item["driver"] + "', '" + type + "', '" + item["weight"] + "', '" + item["unu_weight"] + "', '" + item["num"] + "', '" + item["avgweight"] + "','" + item["workTime"] + "', '" + item["unfuel"] + "', '" + item["unspeed"] + "', '" + item["untrans"] + "', '" + Company + "', '" + tempdate + "', NULL, NULL, NULL, NULL)";
                            mysql.UpdOrInsOrdel(sql);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog("月计算错误-----" + ex);
            }
        }

        #endregion
    }
}