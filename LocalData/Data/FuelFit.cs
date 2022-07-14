using LocalData.MySql;
using LocalData.staticResouce;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Timers;

namespace LocalData.Data
{
    public class FuelFit
    {
        private readonly MySqlHelper mysql;
        private readonly string Company;
        private bool isRead = false;
        /// <summary>
        /// 车辆编号--标定方式-品牌-司机-id
        /// </summary>
        private readonly Dictionary<string, ValueTuple<string, string, string, string>> demarcateInfo;
        /// <summary>
        /// 品牌标定参数
        /// </summary>
        private readonly Dictionary<string, ValueTuple<double, double, double, double, double>> paramDic;
        public FuelFit()
        {
            Company = ConfigurationManager.AppSettings["Company"];
            demarcateInfo = new Dictionary<string, ValueTuple<string, string, string, string>>();
            paramDic = new Dictionary<string, (double, double, double, double, double)>();
            mysql = new MySqlHelper();
            GetVehicleINfo(null, null);
            Thread thread = new Thread(GetPanelInfo)
            {
                IsBackground = true
            };
            thread.Start();
            Thread thread2 = new Thread(MarcateFuel)
            {
                IsBackground = true
            };
            thread2.Start();
        }
        /// <summary>
        /// 标定油量
        /// </summary>
        public void MarcateFuel()
        {
            while (Resource.IsStart)
            {
                while (isRead)
                {//等待mysql释放
                    Thread.Sleep(1000);
                }
                isRead = true;
                bool HasRead = false;
                string sql = "select distinct VEHICLE_ID as vid from fuel_orig where company='" + Company + "' and  REC_STATE='NO' and ADD_TIME > DATE_SUB(NOW(),INTERVAL  2 HOUR)";
                try
                {
                    List<string> list = mysql.MultipleSelect(sql, "vid", "");
                    if (list != null)
                    {
                        foreach (var vid in list)
                        {
                            sql = "select ID as id,VEHICLE_ID as vid,ORIG_FUEL as oriFuel,ADD_TIME as time from fuel_orig where company='" + Company + "' and VEHICLE_ID='" + vid + "' and   REC_STATE='NO' and ADD_TIME > DATE_SUB(NOW(),INTERVAL  2 HOUR) order by ADD_TIME asc limit 0,10 ";
                            List<Dictionary<string, string>> result = mysql.MultipleSelect(sql, new List<string>() { "id", "vid", "oriFuel", "time" });
                            if (result != null && result.Count == 10)
                            {
                                List<double> fuel = new List<double>();
                                List<string> index = new List<string>();
                                string times = result[5]["time"];
                                foreach (var dic in result)
                                {
                                    fuel.Add(double.Parse(dic["oriFuel"]));
                                    index.Add(dic["id"]);
                                }
                                //方差和
                                double sumVariance = 0;
                                //平均值
                                double average = fuel.Average();
                                fuel = fuel.Where(x => x != 0).ToList();
                                if (fuel.Count == 0)
                                {
                                    sql = "INSERT INTO `fuel_fit`( `VEHICLE_ID`, `REAL_FUEL`, `DRIVE_NAME`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ( '" + vid + "', 0, '" + demarcateInfo[vid].Item3 + "', '" + Company + "', '" + times + "', NULL, NULL, NULL, NULL)";
                                    mysql.UpdOrInsOrdel(sql);
                                    sql = "INSERT INTO `temp_fuel_fit`( `VEHICLE_ID`, `REAL_FUEL`, `DRIVE_NAME`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ( '" + vid + "', 0, '" + demarcateInfo[vid].Item3 + "', '" + Company + "', '" + times + "', NULL, NULL, NULL, NULL)";
                                    mysql.UpdOrInsOrdel(sql);
                                    sql = "update vehicle_state set REAl_FUEL =0 where FID='" + demarcateInfo[vid].Item4 + "'";
                                    mysql.UpdOrInsOrdel(sql);
                                    continue;
                                }
                                for (int k = 0; k < fuel.Count; k++)
                                {
                                    sumVariance += Math.Pow(fuel[k] - average, 2);
                                }
                                //方差
                                double variance = Math.Sqrt(sumVariance / fuel.Count);
                                double real = 0;
                                if (variance <= 5)
                                {
                                    real = Calculate(vid, average);
                                }
                                else
                                {
                                    fuel.Remove(fuel.Max());
                                    fuel.Remove(fuel.Min());
                                    average = fuel.Average();
                                    real = Calculate(vid, average);
                                }
                                sql = "INSERT INTO `fuel_fit`( `VEHICLE_ID`, `REAL_FUEL`, `DRIVE_NAME`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ( '" + vid + "','" + real + "', '" + demarcateInfo[vid].Item3 + "', '" + Company + "', '" + times + "', NULL, NULL, NULL, NULL)";
                                mysql.UpdOrInsOrdel(sql);
                                sql = "INSERT INTO `temp_fuel_fit`( `VEHICLE_ID`, `REAL_FUEL`, `DRIVE_NAME`, `COMPANY`, `ADD_TIME`, `TEMP1`, `TEMP2`, `TEMP3`, `TEMP4`) VALUES ( '" + vid + "','" + real + "', '" + demarcateInfo[vid].Item3 + "', '" + Company + "', '" + times + "', NULL, NULL, NULL, NULL)";
                                mysql.UpdOrInsOrdel(sql);
                                sql = "update vehicle_state set REAl_FUEL ='" + real + "'where FID='" + demarcateInfo[vid].Item4 + "'";
                                mysql.UpdOrInsOrdel(sql);
                                foreach (var id in index)
                                {
                                    sql = "update fuel_orig set REC_STATE ='YES'where ID='" + id + "'";
                                    mysql.UpdOrInsOrdel(sql);
                                }
                            }
                            else
                            {
                                //判断最后一条数据时间，如果过长则代表车辆已下线很久，删掉最后几条无用数据 
                                try
                                {
                                    if (result != null && new TimeSpan(DateTime.Now.Ticks - DateTime.Parse(result.Last()["time"]).Ticks).TotalMinutes > 5)
                                    {
                                        List<string> index = new List<string>();
                                        foreach (var dic in result)
                                        {
                                            sql = "delete from fuel_orig where ID='" + dic["id"] + "'";
                                            mysql.UpdOrInsOrdel(sql);
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    LogHelper.WriteLog("时间错误", e);
                                }
                            }
                        }
                        HasRead = true;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLog("标定错误", ex);
                }
                finally
                {
                    isRead = false;
                    if (HasRead)
                    {
                        Thread.Sleep(1000 * 6);//所有车辆标定一轮后睡眠6S
                    }
                    else
                    {
                        Thread.Sleep(1000 * 60);//无车辆标定睡眠60S
                    }
                }
            }
        }
        /// <summary>
        /// 油量拟合计算
        /// </summary>
        /// <param name="vid">车辆编号</param>
        /// <param name="fitFuel">油量值</param>
        /// <returns></returns>
        public double Calculate(string vid, double fitFuel)
        {
            double Result;
            if (demarcateInfo[vid].Item1 == "手动")
            {
                string brand = demarcateInfo[vid].Item2;
                if (paramDic.ContainsKey(brand))
                {
                    ValueTuple<double, double, double, double, double> param = paramDic[brand];
                    Result = (float)(param.Item1 + param.Item2 * fitFuel + param.Item3 * Math.Pow(fitFuel, 2) + param.Item4 * Math.Pow(fitFuel, 3) + param.Item5 * Math.Pow(fitFuel, 4));
                }
                else
                {
                    Result = 0;
                }
            }
            else
            {
                Result = fitFuel;
            }
            return Result < 0 ? 0 : Result;
        }
        /// <summary>
        /// 获取车辆分标定信息信息定时
        /// </summary>
        private void GetPanelInfo()
        {
            System.Timers.Timer getInfoTimer = new System.Timers.Timer(1000 * 60 * 5);
            getInfoTimer.Elapsed += new ElapsedEventHandler(GetVehicleINfo);
            getInfoTimer.AutoReset = true;
            getInfoTimer.Enabled = true;
        }
        /// <summary>
        /// 获取标定信息
        /// </summary>
        public void GetVehicleINfo(object source, System.Timers.ElapsedEventArgs e)
        {
            while (isRead)
            {//等待mysql释放
                Thread.Sleep(5000);
            }
            isRead = true;
            try
            {
                string sql = "select ID as id,vehicle_id as vid,VEHICLE_DRIVER as driver, DEMARCATE as type,VEHICLE_BRAND as brand from list_vehicle where company='" + Company + "'";
                List<Dictionary<string, string>> list = mysql.MultipleSelect(sql, new List<string>() { "vid", "type", "brand", "driver", "id" });
                if (list != null)
                {
                    foreach (var dic in list)
                    {
                        if (demarcateInfo.ContainsKey(dic["vid"]))
                        {
                            demarcateInfo["vid"] = new ValueTuple<string, string, string, string>(dic["type"], dic["brand"], dic["driver"], dic["id"]);
                        }
                        else
                        {
                            demarcateInfo.Add(dic["vid"], new ValueTuple<string, string, string, string>(dic["type"], dic["brand"], dic["driver"], dic["id"]));
                        }
                    }
                }
                sql = "select brand,param1,param2,param3,param4,param5 from fuelparam ";
                List<Dictionary<string, string>> param = mysql.MultipleSelect(sql, new List<string>() { "brand", "param1", "param2", "param3", "param4", "param5" });
                if (param != null)
                {
                    foreach (var dic in param)
                    {
                        if (paramDic.ContainsKey(dic["brand"]))
                        {
                            paramDic[dic["brand"]] = new ValueTuple<double, double, double, double, double>(double.Parse(dic["param1"]), double.Parse(dic["param2"]), double.Parse(dic["param3"]), double.Parse(dic["param4"]), double.Parse(dic["param5"]));
                        }
                        else
                        {
                            paramDic.Add(dic["brand"], new ValueTuple<double, double, double, double, double>(double.Parse(dic["param1"]), double.Parse(dic["param2"]), double.Parse(dic["param3"]), double.Parse(dic["param4"]), double.Parse(dic["param5"])));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog("标定信息更新错误---", ex);
            }
            finally
            {
                isRead = false;
            }
        }
    }
}
