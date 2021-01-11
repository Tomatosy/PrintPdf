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
        private static string lastOrderNo;
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

                // 不能重复打印同一单号
                if (lastOrderNo == orderNo)
                {
                    MessageBox.Show("当前单号已打印，请稍后再试！");
                }

                string httpUrl = SysConfig.HttpUrl + "ExternalInterface/ClaimsInterface/PrintOrder";
                string systemID = SysConfig.SystemID;
                string secret = SysConfig.Secret;
                string token = Md5Encrypt((systemID + "|" + DateTime.Now.ToString("yyyy-MM-dd-HH") + "|" + secret));

                // 调用外部API接口
                string strData = JsonConvert.SerializeObject(new { SystemID = systemID, Token = token, OrderNo = orderNo });
                BaseResultModel<PdfResultModel> result = HttpHelper.PostRequest<PdfResultModel>(httpUrl, DataTypeEnum.json, strData);
                if (!result.IsSuccess)
                    MessageBox.Show("单号查询失败！");

                // 将服务器文件暂存本地打印
                PdfResultModel fileUrlModel = result?.Data;
                string tempFileUrl = AppDomain.CurrentDomain.BaseDirectory;
                WebDownload.DownLoad(fileUrlModel.File, tempFileUrl);

                tempFileUrl += @"TEMP.pdf";
                PdfDocument doc = new PdfDocument();
                doc.LoadFromFile(tempFileUrl);
                doc.Print();

                lastOrderNo = orderNo;
                this.txt_orderNo.Text = "";
                this.txt_orderNo.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                this.txt_orderNo.Focus();
            }
        }
    }
}
