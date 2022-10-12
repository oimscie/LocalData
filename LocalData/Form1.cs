using LocalData.CHCNETSDK;
using LocalData.Data;
using LocalData.MySql;
using LocalData.staticResouce;
using LocalData.SuperSocket;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace LocalData
{
    public partial class DataForm : Form
    {
        public static DataForm MainForm;
        private readonly string Company;
        private readonly MySqlHelper MySqlHelper;

        public DataForm()
        {
            InitializeComponent();
            MainForm = this;
            this.InCarid.Text = ConfigurationManager.AppSettings["InCarid"];
            this.InWeight.Text = ConfigurationManager.AppSettings["InWeight"];
            this.InTime.Text = ConfigurationManager.AppSettings["InTime"];
            Company = ConfigurationManager.AppSettings["Company"];
            this.Text = Company;
            Resource.IsStart = true;
            MySqlHelper = new MySqlHelper();
        }

        private void DataForm_Load(object sender, EventArgs e)
        {
            //自启动
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey
                 ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            registryKey.SetValue("LocalData", Application.ExecutablePath);
            ProgramStart("C:\\Program Files (x86)\\分析仪数据检测终端\\分析仪数据检测终端.exe");
            Thread load = new Thread(ThreadStart)
            {
                IsBackground = true
            };
            load.Start();
        }

        public struct CopyDataStruct
        {
            public IntPtr dwData;
            public int cbData;

            [MarshalAs(UnmanagedType.LPStr)]
            public string lpData;
        }

        public const int WM_COPYDATA = 0x004A;

        //通过窗口标题来查找窗口句柄
        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        private static extern int FindWindow(string lpClassName, string lpWindowName);

        //在DLL库中的发送消息函数
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage
            (
            int hWnd,                        // 目标窗口的句柄
            int Msg,                         // 在这里是WM_COPYDATA
            int wParam,                      // 第一个消息参数
            ref CopyDataStruct lParam        // 第二个消息参数
           );

        //发送消息
        public static void SendMessage(string str)
        {
            try
            {
                string strURL = str;
                CopyDataStruct cds;
                cds.dwData = (IntPtr)1; //这里可以传入一些自定义的数据，但只能是4字节整数
                cds.lpData = strURL;    //消息字符串
                cds.cbData = System.Text.Encoding.Default.GetBytes(strURL).Length + 1; //注意，这里的长度是按字节来算的
                SendMessage(FindWindow(null, "Gama"), WM_COPYDATA, 0, ref cds);       // 窗口标题
            }
            catch
            {
            }
        }

        //接收消息方法
        protected override void WndProc(ref System.Windows.Forms.Message e)
        {
            if (e.Msg == WM_COPYDATA)
            {
                CopyDataStruct cds = (CopyDataStruct)e.GetLParam(typeof(CopyDataStruct));
                if (cds.lpData.ToString() == "运行中")
                {
                    FormUtil.ModifyLable(tsslServerState, cds.lpData.ToString(), Color.Green);
                }
                else
                {
                    FormUtil.ModifyLable(tsslServerState, cds.lpData.ToString(), Color.Red);
                }
            }
            base.WndProc(ref e);
        }

        /// <summary>
        /// 启动外部程序，无限等待其退出
        /// </summary>
        public bool ProgramStart(string appName)
        {
            try
            {
                Process.Start(appName);
                return true;
            }
            catch
            {
            }
            return false;
        }

        /// <summary>
        /// 强制关闭外部程序
        /// </summary>
        public bool CloseProc(string procName)
        {
            //关闭进程
            Process[] a = Process.GetProcessesByName(procName); //获取指定进程名的进程
            if (a.Length > 0)
            {
                foreach (Process p1 in a)
                {
                    p1.Kill();
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 后台线程初始化
        /// </summary>
        private void ThreadStart()
        {
            new OrderSocketClient();
            UpdateState(null, null);
            //禁止列排序
            for (int i = 0; i < this.dataGridView1.Columns.Count; i++)
            {
                this.dataGridView1.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            Thread dataSource = new Thread(dataSouthTimers)
            {
                IsBackground = true
            };
            dataSource.Start();
            ///重启线程
            Thread Restart = new Thread(RestartTimer)
            {
                IsBackground = true
            };
            Restart.Start();
            //检索本地磅数据库线程
            Thread Local = new Thread(new LocalWeigh().getWeighRec)
            {
                IsBackground = true
            };
            Local.Start();
            //过磅记录存储线程
            Thread InserDb = new Thread(new InsertDB().InsertServerDB)
            {
                IsBackground = true
            };
            InserDb.Start();
            new CountMonths(DateTime.Now.ToString("yyyy-MM-dd"));
            new CountWeight(DateTime.Now.ToString("yyyy-MM-dd"));
            new CountFuel(DateTime.Now.ToString("yyyy-MM-dd"));
            new CountMileage(DateTime.Now.ToString("yyyy-MM-dd"));
            new CountMonths(DateTime.Now.ToString("yyyy-MM-dd"));
            new CountGama();
            new FuelFit();
            new VideoJpg();
            FormUtil.ModifyLable(MainForm.State, "正在运行", Color.Green);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                DialogResult result = MessageBox.Show("是否进入托盘运行?", "操作提示", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Asterisk);
                if (result == DialogResult.Yes)
                {
                    e.Cancel = true;
                    WindowState = FormWindowState.Minimized;
                    Visible = false;
                }
                else if (result == DialogResult.No)
                {
                    Resource.opc_connected = false;
                    SendMessage("close");
                    Resource.IsStart = false;
                    if (Resource.videoDic.Count > 0)
                    {
                        foreach (var i in Resource.videoDic)
                        {
                            i.Value.StopPlay();
                        }
                    }
                    Dispose();
                    Application.Exit();
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }

        /// <summary>
        ///  数据源更新
        /// </summary>
        public void dataSouthTimers()
        {
            System.Timers.Timer dataSouth = new System.Timers.Timer(1000 * 60);
            dataSouth.Elapsed += new ElapsedEventHandler(UpdateState);
            dataSouth.AutoReset = true;
            dataSouth.Enabled = true;
        }

        private void UpdateState(object source, ElapsedEventArgs e)
        {
            string sql = "select VEHICLE_ID ,DRIVER,WEIGHT,STATE,REAL_PANEL,ADD_TIME from temp_load_trans where company='" + Company + "' and to_days(ADD_TIME)=to_days(now()) order by ADD_TIME desc limit 0,20";
            List<RecTrans> data = MySqlHelper.MultipleSelect(sql);
            FormUtil.UpdataSource(dataGridView1, data);
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            Visible = true;
            WindowState = FormWindowState.Normal;
        }

        public void RestartTimer()
        {
            System.Timers.Timer RestartTimer = new System.Timers.Timer(1000 * 60 * 30);
            RestartTimer.Elapsed += new ElapsedEventHandler(Restart);
            RestartTimer.AutoReset = true;
            RestartTimer.Enabled = true;
            /// <summary>
            /// 定时重启任务
            /// </summary>
            /// <param name="source"></param>
            /// <param name="e"></param>
            void Restart(object source, System.Timers.ElapsedEventArgs e)
            {
                if (DateTime.Now.Hour.ToString() == "0")
                {
                    Resource.opc_connected = false;
                    SendMessage("close");
                    //开启新的实例
                    Process.Start(Application.ExecutablePath);
                    //关闭当前实例
                    Application.Exit();
                }
            }
        }

        /// <summary>
        /// 单元格内容居中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }
    }
}