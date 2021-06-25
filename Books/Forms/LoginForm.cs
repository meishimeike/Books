using System;
using System.Windows.Forms;
using System.IO;
using System.Data;
using BookHelperLib;

namespace Books
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            IniSqlite();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string username = textBox1.Text.Trim();
            string password = textBox2.Text.Trim();
            if (username == "" || password == "") 
            {
                MessageBox.Show("用户名和密码不能为空", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string sql = string.Format("SELECT Id FROM Users WHERE Username = '{0}'", MyCryptography.DESEncrypt(username));
            DataTable DT = Configs.Sql.ExecuteQuery(sql);
            if (DT.Rows.Count==0) 
            {
                MessageBox.Show("用户名不存在", "警告",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                return;
            }
            sql = string.Format("SELECT Id FROM Users WHERE Username = '{0}' AND Password = '{1}'", MyCryptography.DESEncrypt(username),MD5Helper.EncryptString(password));
            DT = Configs.Sql.ExecuteQuery(sql);
            if (DT.Rows.Count == 0)
            {
                MessageBox.Show("密码错误", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }else
            {
                Configs.UserId = int.Parse(DT.Rows[0][0].ToString());
            }

            Hide();
            BookForm BF = new BookForm();
            BF.ShowDialog();
            Close();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            RegForm RF = new RegForm();
            RF.ShowDialog();
        }

        #region 初始化SQLite数据
        private void IniSqlite()
        {
            Configs.Sql = new SQLiteHelper(Application.CommonAppDataPath + "\\book.db");
            if (!File.Exists(Application.CommonAppDataPath + "\\book.db"))
            {
                //创建用户数据
                string sql = @"CREATE TABLE Users ( 
                        Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username VARCHAR NOT NULL,
                        Password VARCHAR NOT NULL 
                    );";
                Configs.Sql.ExecuteNonQuery(sql);

                //创建代理数据
                sql = @"CREATE TABLE Proxys ( 
                                Id    INTEGER PRIMARY KEY AUTOINCREMENT,
                                Userid INTEGER NOT NULL,
                                Enable BOOLEAN,
                                Type VARCHAR,
                                Host VARCHAR,
                                Port INTEGER,
                                User VARCHAR,
                                Password VARCHAR
                                );";
                Configs.Sql.ExecuteNonQuery(sql);

                //创建源数据
                sql = @"CREATE TABLE Sources ( 
                                Id    INTEGER PRIMARY KEY AUTOINCREMENT,
                                Userid INTEGER NOT NULL,
                                Name VARCHAR,
                                Address VARCHAR,
                                Content VARCHAR
                                );";
                Configs.Sql.ExecuteNonQuery(sql);

                //创建书籍信息数据
                sql = @"CREATE TABLE Books ( 
                        Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                        Userid INTEGER NOT NULL,
                        Rootsourcename VARCHAR NOT NULL,
                        Sourcename VARCHAR NOT NULL,
                        Name VARCHAR NOT NULL,
                        Url  VARCHAR NOT NULL,
                        Author VARCHAR,
                        Coverurl VARCHAR,
                        Coverbase64 VARCHAR,
                        Des VARCHAR,
                        Read VARCHAR
                    );";
                Configs.Sql.ExecuteNonQuery(sql);
                
                //创建书籍内容数据
                sql = @"CREATE TABLE Fictions ( 
                        Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                        Userid         INTEGER NOT NULL,
                        Rootsourcename VARCHAR NOT NULL,
                        Bookname       INTEGER NOT NULL,
                        Chapter        VARCHAR NOT NULL,
                        Section        VARCHAR NOT NULL 
                    );";
                Configs.Sql.ExecuteNonQuery(sql);

                //创建书籍章节数据
                sql = @"CREATE TABLE Chapters ( 
                        Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                        Userid         INTEGER NOT NULL,
                        Rootsourcename VARCHAR NOT NULL,
                        Bookname       VARCHAR NOT NULL,
                        Sectionurl     VARCHAR NOT NULL,
                        Chapter        VARCHAR NOT NULL 
                    );";
                Configs.Sql.ExecuteNonQuery(sql);

                //创建配置数据
                sql = @"CREATE TABLE Configs ( 
                                Id    INTEGER PRIMARY KEY AUTOINCREMENT,
                                Userid INTEGER NOT NULL,
                                Cname VARCHAR NOT NULL,
                                Para1 VARCHAR,
                                Para2 VARCHAR,
                                Para3 VARCHAR,
                                Para4 VARCHAR,
                                Para5 VARCHAR,
                                Para6 VARCHAR 
                                );";
                Configs.Sql.ExecuteNonQuery(sql);
            }
        }
        #endregion

    }
}
