using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;

namespace LocalData.MySql
{
    public class SqlHelper
    {
        /// <summary>
        /// 数据库连接头
        /// </summary>
        private SqlConnection Conn;
        /// <summary>
        /// 数据库连接句柄
        /// </summary>
        private SqlCommand Command;
        /// <summary>
        /// 读取头
        /// </summary>
        private SqlDataReader Reader;
        /// <summary>
        /// 连接字符串
        /// </summary>
        private readonly string ConnStr = "Server=" + ConfigurationManager.AppSettings["LocalIp"] + ";Database=" + ConfigurationManager.AppSettings["Database"] + ";uid=" + ConfigurationManager.AppSettings["UserName"] + ";pwd=" + ConfigurationManager.AppSettings["PassWord"] + "";
        public SqlHelper()
        {
            Conn = new SqlConnection(ConnStr);
            Command = new SqlCommand
            {
                Connection = Conn
            };
        }
        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Close()
        {
            try {
                Conn.Close();
            }
            catch { }
        }
        /// <summary>
        /// 打开连接
        /// </summary>
        /// <returns></returns>
        public bool Open()
        {
            try
            {
                Conn.Open();
                if (Conn.State == ConnectionState.Open)
                {
                    return true;
                }
                FormUtil.ModifyLable(DataForm.MainForm.local, "断开", Color.Red);
                return false;
            }
            catch (Exception e)
            {
                FormUtil.ModifyLable(DataForm.MainForm.local, "断开", Color.Red);
                LogHelper.WriteLog("sql连接错误", e);
                return false;
            }
        }
        public bool CheckConn()
        {
            if (Conn.State == ConnectionState.Closed)
            {
                return Open();
            }
            if (Conn.State == ConnectionState.Broken || Conn.State == ConnectionState.Connecting)
            {
                Close();
                return Open();
            }
            if (Conn.State == ConnectionState.Executing || Conn.State == ConnectionState.Fetching)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// select（单条返回）
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="fieldName">要查询的数据字段名称集合</param>
        /// <returns>List<Dictionary<string, string>>，以输入的字段名称为key值</returns>
        public Dictionary<string, string> SingleSelect(string sql, List<string> fieldName)
        {
            if (!CheckConn()) { return null;}
            try
            {
               
                Command.CommandText = sql;
                Reader = Command.ExecuteReader();
                Dictionary<string, string> back = new Dictionary<string, string>();
                if(Reader.Read())
                {
                    for (int i = 0; i < Reader.FieldCount; i++)
                    {
                        back.Add(fieldName[i], Reader[fieldName[i]].ToString());
                    }
                }
                Reader.Close();
                FormUtil.ModifyLable(DataForm.MainForm.local, "已连接", Color.Green);
                if (back.Count == 0) { return null; };
                return back;
            }
            catch (Exception e)
            {
                FormUtil.ModifyLable(DataForm.MainForm.local, "错误", Color.Red);
                LogHelper.WriteLog("本地库查询错误------"+sql+"------", e);
                return null;
            }
            finally
            {
                Conn.Close();
            }
        }

    }
}
