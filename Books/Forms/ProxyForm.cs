using System;
using System.Net;
using System.Windows.Forms;
using BookHelperLib;
using System.Net.Http;

namespace Books
{
    public partial class ProxyForm : Form
    {
        public ProxyForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool enable = false;
            string type = "HTTP";
            string host = textBox1.Text.Trim();
            int port = 8080;
            int.TryParse(textBox2.Text.Trim(), out port);
            string user = textBox3.Text.Trim();
            string passwd = textBox4.Text.Trim();
            if (radioButton1.Checked)
            {
                enable = false;
            }else
            {
                enable = true;
            }
            string sql = string.Format("UPDATE Proxys SET Enable={0},Type='{1}',Host='{2}',Port={3},User='{4}',Password='{5}' WHERE Userid={6}", enable, type, host, port, user, passwd, Configs.UserId);
            if (Configs.Sql.ExecuteNonQuery(sql) < 1)
            {
                MessageBox.Show("Sqlite 失败,数据更新失败.");
                return;
            }
            BookHelper.SetProxy(enable, type, host, port, user, passwd);
            Close();
        }

        private void ProxyForm_Load(object sender, EventArgs e)
        {
            BookHelper.netProxy proxy = BookHelper.GetProxy();
            if (proxy.Enable)
            {
                radioButton2.Checked = true;
                linkLabel1.Enabled = true;
            }
            else
            {
                radioButton1.Checked = true;
                linkLabel1.Enabled = false;
            }
            textBox1.Text = proxy.Host;
            textBox2.Text = proxy.Port.ToString();
            textBox3.Text = proxy.Username;
            textBox4.Text = proxy.Password;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            linkLabel1.Cursor = Cursors.WaitCursor;
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                WebProxy Tproxy = new WebProxy(textBox1.Text.Trim(), int.Parse(textBox2.Text.Trim()));
                NetworkCredential nc = new NetworkCredential(textBox3.Text.Trim(), textBox4.Text.Trim());
                HttpClientHandler Hch = new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip };
                Hch.Proxy = Tproxy;
                HttpClient httpClient = new HttpClient(Hch);
                if (string.IsNullOrWhiteSpace(httpClient.GetStringAsync("http://www.baidu.com/").Result))
                {
                    MessageBox.Show("网络连接失败.");
                }
                else
                {
                    MessageBox.Show("网络连接成功");
                }
            }
            catch (Exception)
            {
                MessageBox.Show("网络连接失败.");
            }    
            linkLabel1.Cursor = Cursors.Default;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            linkLabel1.Enabled = false;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            linkLabel1.Enabled = true;
        }
    }
}
