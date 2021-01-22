using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Spire.Pdf;
using System.Security.Cryptography;
using Newtonsoft.Json;
using SeaSky.StandardLibNew.MyModel;
using System.Runtime.InteropServices;
//using iTextSharp.text.pdf;
//using iTextSharp.text;

namespace PrintPDFByCode
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }
        private static Dictionary<string, DateTime> lastOrderNoDic = new Dictionary<string, DateTime>();

        private string Md5Encrypt(string strPwd)
        {
            MD5 md5 = MD5.Create();
            byte[] b = md5.ComputeHash(Encoding.UTF8.GetBytes(strPwd));
            StringBuilder sb = new StringBuilder();
            foreach (byte t in b)
            {
                sb.Append(t.ToString("x2").ToUpper());
            }
            return sb.ToString();

        }
        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string IpClassName, string IpWindowName);
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int SetWindowText(IntPtr hWnd, string text);

        [DllImport("user32.dll", EntryPoint = "FindWindowEx")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        /// <summary>
        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);

        private const int WM_CLOSE = 0x10;
        private const int BM_CLICK = 0xF5;
        private int FunCord;
        private IntPtr hwnd;
        private int t;
        private int r;
        private int y;
        private void txt_orderNo_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode != Keys.Enter)
                {
                    return;
                }
                this.txt_orderNo.Focus();
                string orderNo = this.txt_orderNo.Text;

                // 一分钟内不能重复打赢
                if (lastOrderNoDic.Keys.Contains(orderNo))
                {
                    lastOrderNoDic.TryGetValue(orderNo, out DateTime orderTime);
                    if (orderTime.AddMinutes(1) > DateTime.Now)
                    {
                        t = 3;
                        timer1.Enabled = true;
                        MessageBox.Show("         当前单号已打印\n       提示将于" + t + "秒后关闭", "提示");

                        this.txt_orderNo.Text = "";
                        this.txt_orderNo.Focus();
                        return;
                    }
                    lastOrderNoDic.Remove(orderNo);
                }

                string httpUrl = SysConfig.HttpUrl + "ExternalInterface/ClaimsInterface/PrintOrder";
                string systemID = SysConfig.SystemID;
                string secret = SysConfig.Secret;
                string token = Md5Encrypt((systemID + "|" + DateTime.Now.ToString("yyyy-MM-dd-HH") + "|" + secret));

                // 调用外部API接口
                string strData = JsonConvert.SerializeObject(new { SystemID = systemID, Token = token, OrderNo = orderNo });
                BaseResultModel<PdfResultModel> result = HttpHelper.PostRequest<PdfResultModel>(httpUrl, DataTypeEnum.json, strData);
                if (!result.IsSuccess)
                {
                    r = 3;
                    timer2.Enabled = true;
                    MessageBox.Show("    单号查询失败\n提示将于" + r + "秒后关闭", "提示");
                    this.txt_orderNo.Text = "";
                    this.txt_orderNo.Focus();
                    return;
                }

                // 将服务器文件暂存本地打印
                PdfResultModel fileUrlModel = result?.Data;
                string tempFileUrl = AppDomain.CurrentDomain.BaseDirectory;
                WebDownload.DownLoad(fileUrlModel.File, tempFileUrl);

                tempFileUrl += @"TEMP.pdf";
                PdfDocument doc = new PdfDocument();
                doc.LoadFromFile(tempFileUrl);
                doc.Print();

                lastOrderNoDic.Add(orderNo, DateTime.Now);
                this.txt_orderNo.Text = "";
                this.txt_orderNo.Focus();
            }
            catch (Exception ex)
            {
                y = 3;
                timer3.Enabled = true;
                MessageBox.Show("系统异常，请稍后再试\n  提示将于" + y + "秒后关闭", "提示");
                this.txt_orderNo.Focus();
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
        }


        private void timer2_Tick(object sender, EventArgs e)
        {
            hwnd = FindWindow(null, "提示");
            IntPtr a = FindWindowEx(hwnd, (IntPtr)null, null, "    单号查询失败\n提示将于" + r.ToString() + "秒后关闭");
            r = r - 1;
            SetWindowText(a, "    单号查询失败\n提示将于" + r.ToString() + "秒后关闭");
            if (r == 0)
            {
                timer2.Enabled = false;
                SendMessage(hwnd, WM_CLOSE, 0, 0);
            }
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            hwnd = FindWindow(null, "提示");
            IntPtr a = FindWindowEx(hwnd, (IntPtr)null, null, "系统异常，请稍后再试\n  提示将于" + y.ToString() + "秒后关闭");
            y = y - 1;
            SetWindowText(a, "系统异常，请稍后再试\n  提示将于" + y.ToString() + "秒后关闭");
            if (y == 0)
            {
                timer3.Enabled = false;
                SendMessage(hwnd, WM_CLOSE, 0, 0);
            }
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            hwnd = FindWindow(null, "提示");
            IntPtr a = FindWindowEx(hwnd, (IntPtr)null, null, "         当前单号已打印\n       提示将于" + t.ToString() + "秒后关闭");
            t = t - 1;
            SetWindowText(a, "         当前单号已打印\n       提示将于" + t.ToString() + "秒后关闭");
            if (t == 0)
            {
                timer1.Enabled = false;
                SendMessage(hwnd, WM_CLOSE, 0, 0);
            }

            //if (FunCord == 1)
            //{
            //    hwnd = FindWindow(null, "系统将于" + t.ToString() + "秒后关机");
            //    t = t - 1;
            //    SetWindowText(hwnd, "系统将于" + t.ToString() + "秒后关机");
            //    if (t == 0)
            //    {
            //        timer1.Enabled = false;
            //        SendMessage(hwnd, WM_CLOSE, 0, 0);
            //    }
            //}
            //else if (FunCord == 2)
            //{
            //    hwnd = FindWindow(null, "关机提示");
            //    IntPtr a = FindWindowEx(hwnd, (IntPtr)null, null, "系统将于" + t.ToString() + "秒后关机");
            //    t = t - 1;
            //    SetWindowText(a, "系统将于" + t.ToString() + "秒后关机");
            //    if (t == 0)
            //    {
            //        timer1.Enabled = false;
            //        SendMessage(hwnd, WM_CLOSE, 0, 0);
            //    }
            //}
            //else if (FunCord == 3)
            //{
            //    hwnd = FindWindow(null, "系统将于" + t.ToString() + "秒后关机");
            //    t = t - 1;
            //    SetWindowText(hwnd, "系统将于" + t.ToString() + "秒后关机");
            //    if (t == 0)
            //    {
            //        IntPtr OKHwnd = FindWindowEx(hwnd, IntPtr.Zero, null, "确定");
            //        SendMessage(OKHwnd, BM_CLICK, 0, 0);
            //        timer1.Enabled = false;
            //    }
            //}
            //else if (FunCord == 4)
            //{
            //    hwnd = FindWindow(null, "系统将于" + t.ToString() + "秒后关机");
            //    t = t - 1;
            //    SetWindowText(hwnd, "系统将于" + t.ToString() + "秒后关机");
            //    if (t == 0)
            //    {
            //        IntPtr OKHwnd = FindWindowEx(hwnd, IntPtr.Zero, null, "取消");
            //        SendMessage(OKHwnd, BM_CLICK, 0, 0);
            //        timer1.Enabled = false;
            //    }
            //}
        }
        
    }
}
