using System;
using System.Windows.Forms;
using System.Data;

namespace Books
{
    public partial class RegForm : Form
    {
        //Resources.language.En Lang = new Resources.language.En();
        
        public RegForm()
        {
            InitializeComponent();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string username = textBox1.Text.Trim();
            string password = textBox2.Text.Trim();
            string repassword = textBox3.Text.Trim();
            if (username == "") 
            {
                MessageBox.Show("用户名不能为空", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string sql = string.Format("SELECT ID FROM Users WHERE Username = '{0}'", MyCryptography.DESEncrypt(username));
            DataTable DT = Configs.Sql.ExecuteQuery(sql);
            if (DT.Rows.Count>0)
            {
                MessageBox.Show("用户名已存在", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (password == "")
            {
                MessageBox.Show("密码不能为空", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (password != repassword) 
            {
                MessageBox.Show("两次密码不一致", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try {
                sql = string.Format("INSERT INTO Users(Username,Password) VALUES('{0}','{1}')", MyCryptography.DESEncrypt(username), MD5Helper.EncryptString(password));
                if (Configs.Sql.ExecuteNonQuery(sql) > 0)
                {
                    sql = string.Format("SELECT Id FROM Users WHERE Username = '{0}'", MyCryptography.DESEncrypt(username));
                    DT = Configs.Sql.ExecuteQuery(sql);
                    Configs.UserId = int.Parse(DT.Rows[0][0].ToString());
                    //写入默认配置
                    string Proxysql = string.Format("INSERT INTO Proxys(Userid,Enable,Type,Host,Port,User,Password) VALUES({0},false,'HTTP','127.0.0.1',8080,'','')", Configs.UserId);
                    string Treesql = string.Format("INSERT INTO Configs(Userid,Cname,Para1,Para2,Para3) VALUES({0},'Tree','','','')", Configs.UserId);
                    string Richsql = string.Format("INSERT INTO Configs(Userid,Cname,Para1,Para2,Para3) VALUES({0},'Rich','','','')", Configs.UserId);
                    Configs.Sql.ExecuteNonQuery(Proxysql);
                    Configs.Sql.ExecuteNonQuery(Treesql);
                    Configs.Sql.ExecuteNonQuery(Richsql);
                    MessageBox.Show("注册成功!");
                    Close();
                }else
                {
                    MessageBox.Show("注册失败!");
                }
            }
            catch (Exception ex){
                MessageBox.Show("注册失败," + ex.Message);
            }
        }
    }
}
