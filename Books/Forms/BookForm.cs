using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Data;
using System.Threading;
using BookHelperLib;
using System.Collections.Generic;
using System.Data.SQLite;

namespace Books
{
    public partial class BookForm : Form
    {
        private bool isClose = false;
        private List<SQLiteParameter[]> UpdataParas = new List<SQLiteParameter[]>();
        private List<SQLiteParameter[]> InsertParas = new List<SQLiteParameter[]>();
        public BookForm()
        {
            InitializeComponent();
        }

        private void updataUToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            LoadConfig();
        }

        private void BookForm_Load(object sender, EventArgs e)
        {
            IniConfig();
            LoadConfig();
            BackThread();
        }

        private void BackThread()
        {
            Thread updataSource = new Thread(UpdataSourceFile);
            updataSource.Start();
        }

        private void UpdataSourceFile()
        {
            string sql = string.Format("SELECT Name,Address,Content FROM Sources WHERE Userid={0}", Configs.UserId);
            DataTable DT = Configs.Sql.ExecuteQuery(sql);
            if (DT.Rows.Count > 0)
            {
                foreach (DataRow R in DT.Rows)
                {
                    string name = MyCryptography.DESDecrypt(R[0].ToString());
                    string address = MyCryptography.DESDecrypt(R[1].ToString());
                    string content = MyCryptography.DESDecrypt(R[2].ToString());
                    string newcontent = BookHelper.GetRequst(address);
                    if (newcontent.Length > 200 && content != newcontent)
                    {
                        sql = string.Format("UPDATE Sources SET Content='{0}' WHERE Name='{1}' AND Address='{2}';", MyCryptography.DESEncrypt(newcontent), MyCryptography.DESEncrypt(name), MyCryptography.DESEncrypt(address));
                        Configs.Sql.ExecuteNonQuery(sql);
                        BookHelper.DelSourceAdress(name);
                        BookHelper.AddSoucerAdress(name, address, newcontent);
                    }
                }
            }
        }
        private void IniConfig()
        {
            BookHelper.SetCovePath(Path.GetTempPath() + "Books\\");
            string sql = string.Format("SELECT Enable,Type,Host,Port,User,Password FROM Proxys WHERE Userid={0}", Configs.UserId);
            DataTable DT = Configs.Sql.ExecuteQuery(sql);
            if (DT.Rows.Count > 0)
            {
                bool Enable = (bool)DT.Rows[0][0];
                string Type= DT.Rows[0][1].ToString();
                string Host = DT.Rows[0][2].ToString();
                int Port = 8080;
                int.TryParse(DT.Rows[0][3].ToString(),out Port);
                string User = DT.Rows[0][4].ToString();
                string Passwd = DT.Rows[0][5].ToString();

                if (Enable)
                {
                    BookHelper.SetProxy(true,Type,Host, Port, User, Passwd);
                }
                else
                {
                    BookHelper.SetProxy(false, Type, Host, Port, User, Passwd);
                }
            }
            sql = string.Format("SELECT Name,Address,Content FROM Sources WHERE Userid={0}", Configs.UserId);
            DT = Configs.Sql.ExecuteQuery(sql);
            if (DT.Rows.Count > 0)
            {             
                foreach (DataRow R in DT.Rows)
                {
                    string name = MyCryptography.DESDecrypt(R[0].ToString());
                    string address = MyCryptography.DESDecrypt(R[1].ToString());
                    string content = MyCryptography.DESDecrypt(R[2].ToString());
                    BookHelper.AddSoucerAdress(name,address,content);
                }
            }
        }
        private void LoadConfig()
        {           
            string sql = string.Format("SELECT * FROM Books Where Userid={0}", Configs.UserId);
            DataTable DT = Configs.Sql.ExecuteQuery(sql);
            if (DT.Rows.Count > 0)
            {
                for(int i = 0; i < DT.Rows.Count; i++)
                {
                    BookHelper.Book BK = new BookHelper.Book();
                    BK.RootSourcename= MyCryptography.DESDecrypt(DT.Rows[i][2].ToString());
                    BK.Sourcename= MyCryptography.DESDecrypt(DT.Rows[i][3].ToString());
                    BK.Name= MyCryptography.DESDecrypt(DT.Rows[i][4].ToString());
                    BK.Url= MyCryptography.DESDecrypt(DT.Rows[i][5].ToString());
                    BK.Author= MyCryptography.DESDecrypt(DT.Rows[i][6].ToString());
                    BK.Coverurl= MyCryptography.DESDecrypt(DT.Rows[i][7].ToString());
                    BK.Coverbase64 = DT.Rows[i][8].ToString();
                    BK.Des= MyCryptography.DESDecrypt(DT.Rows[i][9].ToString());
                    BK.Read = int.Parse(MyCryptography.DESDecrypt(DT.Rows[i][10].ToString()));

                    ListViewItem lvi = new ListViewItem(BK.Name);
                    Image img = BookHelper.Base64ToImage(BK.Coverbase64);
                    if (img == null) 
                        imageList1.Images.Add(BK.Name, Properties.Resources._null); 
                    else 
                        imageList1.Images.Add(BK.Name, img);                    
                    lvi.ImageKey = BK.Name;
                    lvi.Tag = BK;
                    lvi.ToolTipText = "作者:" + BK.Author + Environment.NewLine + "源:" + "[" + BK.RootSourcename + "]" + "『" + BK.Sourcename + "』" + Environment.NewLine + "简介:" + BK.Des;
                    listView1.Items.Add(lvi);
                }

            }
           
        }
        private void booksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SearchForm SF = new SearchForm();
            SF.Getbook += SF_Getbook;
            Hide();
            SF.ShowDialog();
            Show();
        }

        private void SF_Getbook(BookHelper.Book book)
        {
            try
            {
                foreach (ListViewItem lv in listView1.Items)
                {
                    BookHelper.Book bK = (BookHelper.Book)lv.Tag;
                    if (bK.Name == book.Name && bK.Sourcename==book.Sourcename)
                    {
                        MessageBox.Show("添加的书已存在，请不要重复添加！");
                        return;
                    }
                }
                string Coverbase64 = "";
                if (File.Exists(book.Coverpath))
                    Coverbase64 = BookHelper.ImageToBase64(book.Coverpath);
                else
                    Coverbase64 = BookHelper.ImageToBase64(Properties.Resources._null);
                string sql = string.Format("INSERT INTO Books(Userid,Rootsourcename,Sourcename,Name,Url,Author,Coverurl,Coverbase64,Des,Read) VALUES({0},\'{1}\',\'{2}\',\'{3}\',\'{4}\',\'{5}\',\'{6}\',\'{7}\',\'{8}\',\'{9}\')", Configs.UserId, MyCryptography.DESEncrypt(book.RootSourcename), MyCryptography.DESEncrypt(book.Sourcename), MyCryptography.DESEncrypt(book.Name), MyCryptography.DESEncrypt(book.Url), MyCryptography.DESEncrypt(book.Author), MyCryptography.DESEncrypt(book.Coverurl), Coverbase64, MyCryptography.DESEncrypt(book.Des), MyCryptography.DESEncrypt("0"));
                if (Configs.Sql.ExecuteNonQuery(sql) > 0)
                {
                    Image coverimage = BookHelper.Base64ToImage(Coverbase64);             
                    imageList1.Images.Add(book.Name, coverimage);
                    coverimage.Dispose();

                    ListViewItem lvi = new ListViewItem(book.Name);                 
                    lvi.ImageKey = book.Name;
                    lvi.Tag = book;
                    lvi.ToolTipText = "作者:" + book.Author + Environment.NewLine + "源:" + "[" + book.RootSourcename + "]" + "『" + book.Sourcename + "』" + Environment.NewLine + "简介:" + book.Des;
                    listView1.Items.Add(lvi);
                    MessageBox.Show("添加成功。");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("添加失败," + ex.Message);
            }
           
        }

        private void BookForm_FormClosed(object sender, FormClosedEventArgs e)
        {          
            Application.ExitThread();
        }

        private void setProxyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProxyForm PF = new ProxyForm();
            PF.ShowDialog();
        }

        private void sourceSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SourceForm SF = new SourceForm();
            SF.delSource += SF_delSource;
            SF.ShowDialog();
        }

        private void SF_delSource(string rootsourcename)
        {
            string sql = string.Format("DELETE FROM Books WHERE Userid={0} AND Rootsourcename='{1}'", Configs.UserId, MyCryptography.DESEncrypt(rootsourcename));
            if (Configs.Sql.ExecuteNonQuery(sql) > 0)
            {
                for (int i = listView1.Items.Count - 1; i > -1; i--)
                {
                    BookHelper.Book book = (BookHelper.Book)listView1.Items[i].Tag;
                    if (book.RootSourcename == rootsourcename)
                    {
                        listView1.Items[i].Remove();
                    }
                }           
            }
            BookHelper.DelSourceAdress(rootsourcename);
        }

        private void aboutAToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutForm ABF = new AboutForm();
            ABF.ShowDialog();
        }

        private void addBookToolStripMenuItem_Click(object sender, EventArgs e)
        {
            booksToolStripMenuItem_Click(null,null);
        }

        private void deleteBookToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                if (MessageBox.Show("确认要删除选中的书籍吗？", "删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    for (int i = listView1.SelectedItems.Count-1; i >-1 ; i--)
                    {
                        BookHelper.Book book = (BookHelper.Book)listView1.SelectedItems[i].Tag;
                        string sql = string.Format("DELETE FROM Books WHERE Userid={0} AND Rootsourcename='{1}' AND Name = '{2}';DELETE FROM Chapters WHERE Userid={0} AND Rootsourcename='{1}' AND Bookname = '{2}';DELETE FROM Fictions WHERE Userid={0} AND Rootsourcename='{1}' AND Bookname = '{2}';", Configs.UserId, MyCryptography.DESEncrypt(book.RootSourcename), MyCryptography.DESEncrypt(listView1.SelectedItems[i].Text));
                        if (Configs.Sql.ExecuteNonQuery(sql) > 0)
                            listView1.SelectedItems[i].Remove();
                        else
                            MessageBox.Show(listView1.SelectedItems[i].Text+",删除失败。");
                    }
                }
            }
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 1)
            {
                BookHelper.Book book = (BookHelper.Book)listView1.SelectedItems[0].Tag;
                ReadForm RF = new ReadForm();
                RF.book = book;          
                Hide();
                RF.ShowDialog();
                Show();
            }
        }

        private void listView1_MouseMove(object sender, MouseEventArgs e)
        {
            ListViewItem item = this.listView1.GetItemAt(e.X, e.Y);

            if (item == null)
            {

                this.listView1.Cursor = Cursors.Default;
            }
            else
            {
                this.listView1.Cursor = Cursors.Hand;
            }        
        }

        private void clearCacheToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show("确认要清除所有数据吗？", "警告", MessageBoxButtons.YesNo,MessageBoxIcon.Warning) == DialogResult.OK)
            {
                string sql = @"DELETE FROM Fictions;
                           DELETE FROM ;
                           UPDATE sqlite_sequence set seq=0 where name='Fictions';
                           UPDATE sqlite_sequence set seq=0 where name='Chapters';
                           ";
                if (Configs.Sql.ExecuteNonQuery(sql) > 0)
                {
                    MessageBox.Show("清除成功！");
                }
            }           
        }

        private void exitEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void importDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog Ofd = new OpenFileDialog();
            Ofd.Filter = "DB数据文件(*.db)|*.db";
            if (Ofd.ShowDialog() == DialogResult.OK)
            {
                File.Copy(Ofd.FileName, Application.CommonAppDataPath + "\\book.db", true);
                BookForm_Load(null, null);
            }
        }

        private void exportDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog Sfd = new SaveFileDialog();
            Sfd.Filter= "DB数据文件(*.db)|*.db";
            if (Sfd.ShowDialog() == DialogResult.OK)
            {
                File.Copy(Application.CommonAppDataPath + "\\book.db", Sfd.FileName);
            }
        }

        private void openOToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1_DoubleClick(null, null);
        }

        private void cacheBookToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0) {
                for (int i = 0; i < listView1.SelectedItems.Count; i++) {
                    BookHelper.Book book = (BookHelper.Book)listView1.SelectedItems[i].Tag;
                    Thread cacheBookThread = new Thread(() => { cacheBook(book); });
                    cacheBookThread.Start();
                }
            }
        }

        private void BookForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            isClose = true;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 1)
            {
                toolStripStatusLabel2.Text = listView1.SelectedItems[0].Text;
            }
            else
            {
                toolStripStatusLabel2.Text = "null";
            }
        }

        private void cacheBook(BookHelper.Book book)
        {
            List<KeyValuePair<string, string>> Contents = BookHelper.GetBookContents(book);
            if (Contents == null)
            {
                return;
            }
            //*********************更新章节************************
            string sql = string.Format("SELECT Chapter,Sectionurl FROM Chapters Where Userid='{0}' and Rootsourcename='{1}' and Bookname='{2}'",Configs.UserId, MyCryptography.DESEncrypt(book.RootSourcename), MyCryptography.DESEncrypt(book.Name));
            DataTable DT = Configs.Sql.ExecuteQuery(sql);
            if (DT.Rows.Count < Contents.Count)
            {
                sql = string.Format("DELETE FROM Chapters WHERE Userid='{0}' and Rootsourcename='{1}' and Bookname='{2}'", Configs.UserId, MyCryptography.DESEncrypt(book.RootSourcename), MyCryptography.DESEncrypt(book.Name));
                Configs.Sql.ExecuteNonQuery(sql);

                List<SQLiteParameter[]> listParas = new List<SQLiteParameter[]>();
                for (int m = 0; m < Contents.Count; m++)
                {
                    string Chapter = Contents[m].Key;
                    string Sectionurl = Contents[m].Value;
                    SQLiteParameter[] Paras = new SQLiteParameter[] {new SQLiteParameter("@Userid",Configs.UserId),new SQLiteParameter("@Rootsourcename", MyCryptography.DESEncrypt(book.RootSourcename)), new SQLiteParameter("@Bookname", MyCryptography.DESEncrypt(book.Name)), new SQLiteParameter("@Sectionurl", MyCryptography.DESEncrypt(Sectionurl)), new SQLiteParameter("@Chapter", MyCryptography.DESEncrypt(Chapter)) };
                    listParas.Add(Paras);
                }
                sql = "INSERT INTO Chapters(Userid,Rootsourcename,Bookname,Sectionurl,Chapter) VALUES(@Userid,@Rootsourcename,@Bookname,@Sectionurl,@Chapter)";
                Configs.Sql.ExecuteNonQueryBatch(sql, listParas);
            }

            //*********************更新内容************************
            List<Thread> Caches = new List<Thread>();
            int i = 0;
            while (i<Contents.Count && !isClose)
            {
                for (int m = 0; m < Caches.Count; m++)
                {
                    Thread th = Caches[m];
                    if (!th.IsAlive)
                    {
                        Caches.Remove(th);
                    }
                }

                if (Caches.Count < 20)
                {
                    string Chapter = Contents[i].Key;
                    string Sectionurl = Contents[i].Value;

                    if (statusStrip1.InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            toolStripStatusLabel2.Text = string.Format("开始缓存 《{0}》 {1}.", book.Name, Chapter);
                        }));
                    }
                    else
                    {
                        toolStripStatusLabel2.Text = string.Format("开始缓存 《{0}》 {1}.", book.Name, Chapter);
                    }

                    Thread cache = new Thread(() =>
                    {
                        cacheContent(Chapter, Sectionurl, book);
                    });
                    Caches.Add(cache);
                    cache.Start();
                    i++;                  
                }
                else
                {
                    Thread.Sleep(100);
                }

                if (UpdataParas.Count > 10)
                {
                    sql = "UPDATE Fictions SET Section=@Section WHERE Userid = @Userid AND Rootsourcename = @Rootsourcename AND Bookname = @Bookname AND Chapter=@Chapter";
                    Configs.Sql.ExecuteNonQueryBatch(sql, UpdataParas);
                    UpdataParas.Clear();
                }
                if (InsertParas.Count > 10)
                {
                    sql = "INSERT INTO Fictions(Userid,Rootsourcename,Bookname,Chapter,Section) VALUES(@Userid,@Rootsourcename,@Bookname,@Chapter,@Section)";
                    Configs.Sql.ExecuteNonQueryBatch(sql, InsertParas);
                    InsertParas.Clear();
                }        
            }

            while (true)
            {
                if (Caches.Count > 0)
                {
                    for (int m = 0; m < Caches.Count; m++)
                    {
                        Thread th = Caches[m];
                        if (!th.IsAlive)
                        {
                            Caches.Remove(th);
                        }
                    }
                }
                else
                {
                    break;
                }
                Thread.Sleep(500);
            }

            if (UpdataParas.Count > 0)
            {
                sql = "UPDATE Fictions SET Section=@Section WHERE Userid = @Userid AND Rootsourcename = @Rootsourcename AND Bookname = @Bookname AND Chapter=@Chapter";
                Configs.Sql.ExecuteNonQueryBatch(sql, UpdataParas);
                UpdataParas.Clear();
            }
            if (InsertParas.Count > 0)
            {
                sql = "INSERT INTO Fictions(Userid,Rootsourcename,Bookname,Chapter,Section) VALUES(@Userid,@Rootsourcename,@Bookname,@Chapter,@Section)";
                Configs.Sql.ExecuteNonQueryBatch(sql, InsertParas);
                InsertParas.Clear();
            }

            if (statusStrip1.InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    toolStripStatusLabel2.Text = "无工作";
                }));
            }
            else
            {
                toolStripStatusLabel2.Text = "无工作";
            }
        }

        private void cacheContent(string Chapter, string Sectionurl, BookHelper.Book book)
        {
            string Section = "";
            string sql = string.Format("SELECT Section FROM Fictions WHERE Userid ={0} AND Rootsourcename = '{1}' AND Bookname = '{2}' AND Chapter='{3}'",Configs.UserId,MyCryptography.DESEncrypt(book.RootSourcename), MyCryptography.DESEncrypt(book.Name), MyCryptography.DESEncrypt(Chapter));
            DataTable DB = Configs.Sql.ExecuteQuery(sql);
            if (DB.Rows.Count > 0)
            {
                Section = DB.Rows[0][0].ToString();
                if (Section.Length < 20)
                {
                    Section = BookHelper.GetContentTxt(Sectionurl, book);
                    UpdataParas.Add(new SQLiteParameter[] {new SQLiteParameter("@Userid",Configs.UserId),new SQLiteParameter("@Rootsourcename",MyCryptography.DESEncrypt(book.RootSourcename)), new SQLiteParameter("@Bookname", MyCryptography.DESEncrypt(book.Name)), new SQLiteParameter("@Chapter", MyCryptography.DESEncrypt(Chapter)), new SQLiteParameter("@Section", MyCryptography.DESEncrypt(Section)) });
                }
            }
            else
            {
                Section = BookHelper.GetContentTxt(Sectionurl, book);
                InsertParas.Add(new SQLiteParameter[] { new SQLiteParameter("@Userid", Configs.UserId), new SQLiteParameter("@Rootsourcename", MyCryptography.DESEncrypt(book.RootSourcename)), new SQLiteParameter("@Bookname", MyCryptography.DESEncrypt(book.Name)), new SQLiteParameter("@Chapter", MyCryptography.DESEncrypt(Chapter)), new SQLiteParameter("@Section", MyCryptography.DESEncrypt(Section)) });
            }         
        }     
    }
}
