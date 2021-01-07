using System;
using System.Data;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using BookHelperLib;
using System.Collections.Generic;
using System.Data.SQLite;

namespace Books
{
    public partial class ReadForm : Form
    {
        public BookHelper.Book book { get; set; }
        int readindex;
        int woodwidth;
        int localTreeCount = 0;
        public ReadForm()
        {
            InitializeComponent();
        }

        private void ReadForm_Load(object sender, EventArgs e)
        {
            string treesql = string.Format("SELECT Para1,Para2,Para3 FROM Configs WHERE Userid = {0} AND CName='{1}'", Configs.UserId, "Tree");
            string richsql = string.Format("SELECT Para1,Para2,Para3 FROM Configs WHERE Userid = {0} AND CName='{1}'", Configs.UserId, "Rich");

            DataTable treedt = Configs.Sql.ExecuteQuery(treesql);
            DataTable richdt = Configs.Sql.ExecuteQuery(richsql);
            if (treedt.Rows.Count > 0)
            {
                string treefont = treedt.Rows[0][0].ToString();
                string treefontcolor= treedt.Rows[0][1].ToString();
                string treebackcolor = treedt.Rows[0][2].ToString();
                treeView1.Font = (Font)(new FontConverter()).ConvertFromString(treefont);
                treeView1.ForeColor = (Color)(new ColorConverter()).ConvertFromString(treefontcolor);
                treeView1.BackColor = (Color)(new ColorConverter()).ConvertFromString(treebackcolor);
            }
            if (richdt.Rows.Count > 0)
            {
                string richfont = richdt.Rows[0][0].ToString();
                string richfontcolor = richdt.Rows[0][1].ToString();
                string richbackcolor = richdt.Rows[0][2].ToString();
                richTextBox1.Font = (Font)(new FontConverter()).ConvertFromString(richfont);
                richTextBox1.ForeColor = (Color)(new ColorConverter()).ConvertFromString(richfontcolor);
                richTextBox1.BackColor = (Color)(new ColorConverter()).ConvertFromString(richbackcolor);
            }
            pictureBox1.BackgroundImage = Properties.Resources.left;
            pictureBox1.Tag = "Close";
            GetlocalBookContents();
        }

        private void GetlocalBookContents()
        {
            string sql = string.Format("SELECT Chapter,Sectionurl FROM Chapters Where Bookname='{0}'", MyCryptography.DESEncrypt(book.Name));
            DataTable DT = Configs.Sql.ExecuteQuery(sql);
            TreeNode localTN = new TreeNode(book.Name);
            if (DT.Rows.Count > 0)
            {
                for (int i = 0; i < DT.Rows.Count; i++)
                {
                    string Chapter = MyCryptography.DESDecrypt(DT.Rows[i][0].ToString());
                    string Sectionurl = MyCryptography.DESDecrypt(DT.Rows[i][1].ToString());
                    TreeNode subTN = new TreeNode(Chapter);
                    subTN.Tag = Sectionurl;
                    localTN.Nodes.Add(subTN);
                }
                localTreeCount = localTN.Nodes.Count;
                treeView1.Nodes.Add(localTN);
                int readIndex = book.Read;
                sql = string.Format("SELECT Read FROM Books Where Userid={0} AND Rootsourcename='{1}' AND Sourcename='{2}' AND Name='{3}'", Configs.UserId, MyCryptography.DESEncrypt(book.RootSourcename), MyCryptography.DESEncrypt(book.Sourcename), MyCryptography.DESEncrypt(book.Name));
                DT = Configs.Sql.ExecuteQuery(sql);
                if (DT.Rows.Count > 0)
                {
                    readIndex = int.Parse(MyCryptography.DESDecrypt(DT.Rows[0][0].ToString()));
                }
                treeView1.SelectedNode = treeView1.Nodes[0].Nodes[readIndex];
                treeView1_DoubleClick(null, null);
            }else
            {
                richTextBox1.Text= "No local data, start requesting network data, please wait";
                treeView1.Cursor = Cursors.WaitCursor;
            }

            Thread GetTxt = new Thread(GetBookContents);
            GetTxt.Start();
            //GetBookContents();
        }

        #region 获取目录
        private void GetBookContents()
        {
            List<KeyValuePair<string, string>> Contents = BookHelper.GetBookContents(book);
            if(Contents==null || localTreeCount >= Contents.Count)
            {
                return;
            }

            int readIndex = book.Read;
            string sql = string.Format("SELECT Read FROM Books Where Userid={0} AND Rootsourcename='{1}' AND Sourcename='{2}' AND Name='{3}'", Configs.UserId, MyCryptography.DESEncrypt(book.RootSourcename), MyCryptography.DESEncrypt(book.Sourcename), MyCryptography.DESEncrypt(book.Name));
            DataTable DT = Configs.Sql.ExecuteQuery(sql);
            if (DT.Rows.Count > 0)
            {
                readIndex = int.Parse(MyCryptography.DESDecrypt(DT.Rows[0][0].ToString()));
            }

            sql = string.Format("DELETE FROM Chapters WHERE Bookname='{0}';", MyCryptography.DESEncrypt(book.Name));
            Configs.Sql.ExecuteNonQuery(sql);

            sql = "";
            TreeNode TN = new TreeNode(book.Name);

            List<SQLiteParameter[]> listParas = new List<SQLiteParameter[]>();
            for (int m = 0; m < Contents.Count; m++)
            {
                string Chapter = Contents[m].Key;
                string Sectionurl = Contents[m].Value;
                SQLiteParameter[] Paras = new SQLiteParameter[] { new SQLiteParameter("@Bookname", MyCryptography.DESEncrypt(book.Name)), new SQLiteParameter("@Chapter", MyCryptography.DESEncrypt(Chapter)), new SQLiteParameter("@Sectionurl", MyCryptography.DESEncrypt(Sectionurl)) };
                listParas.Add(Paras);
                TreeNode subTN = new TreeNode(Chapter);
                subTN.Tag = Sectionurl;
                TN.Nodes.Add(subTN);
            }

            sql = "INSERT INTO Chapters(Bookname,Chapter,Sectionurl) VALUES(@Bookname,@Chapter,@Sectionurl)";
            Configs.Sql.ExecuteNonQueryBatch(sql, listParas);

            if (treeView1.InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    treeView1.Cursor = Cursors.Default;
                    treeView1.Nodes.Clear();
                    treeView1.Nodes.Add(TN);
                    treeView1.SelectedNode = treeView1.Nodes[0].Nodes[readIndex];
                    treeView1_DoubleClick(null, null);
                }));
            }
            else
            {
                treeView1.Cursor = Cursors.Default;
                treeView1.Nodes.Clear();
                treeView1.Nodes.Add(TN);
                treeView1.SelectedNode = treeView1.Nodes[0].Nodes[readIndex];
                treeView1_DoubleClick(null, null);
            }
        }
        #endregion

        private void treeView1_DoubleClick(object sender, EventArgs e)
        {         
            if (treeView1.SelectedNode != null && treeView1.SelectedNode.Level==1)
            {
                readindex = treeView1.SelectedNode.Index;
                treeView1.Cursor = Cursors.WaitCursor;
                string Chapter = treeView1.SelectedNode.Text;
                string Chapterurl = treeView1.SelectedNode.Tag.ToString();
                string Section = null;
                string sql = string.Format("SELECT Section FROM Fictions WHERE Bookname = '{0}' AND Chapter='{1}'", MyCryptography.DESEncrypt(book.Name), MyCryptography.DESEncrypt(Chapter));
                DataTable DT = Configs.Sql.ExecuteQuery(sql);
                if (DT.Rows.Count > 0)
                {
                    Section = MyCryptography.DESDecrypt(DT.Rows[0][0].ToString());
                }
                if (!string.IsNullOrWhiteSpace(Section))
                {
                    richTextBox1.Text = Section;
                }else{
                    richTextBox1.Text = Getchapter(Chapter,Chapterurl);
                }
                
                toolStripStatusLabel2.Text = treeView1.SelectedNode.Text;
                double rate = (double)(treeView1.SelectedNode.Index + 1) / treeView1.Nodes[0].Nodes.Count * 100;
                toolStripProgressBar1.Value =(int)(rate);
                toolStripStatusLabel4.Text = Math.Round(rate,2) + "%";
                treeView1.Cursor = Cursors.Default;

                #region**********缓存线程**********
                int count = treeView1.Nodes[0].Nodes.Count;
                Thread ThreadSection = new Thread(() => { CachSection(readindex, count); });
                ThreadSection.Start();
                #endregion
            }
        }
        private string Getchapter(string Chapter,string Chapterurl)
        {
            string Section = BookHelper.GetContentTxt(Chapterurl, book);
            string sql = string.Format("SELECT Section FROM Fictions WHERE Bookname = '{0}' AND Chapter='{1}'", MyCryptography.DESEncrypt(book.Name), MyCryptography.DESEncrypt(Chapter));
            if (Configs.Sql.ExecuteNonQuery(sql) > 0)
            {
                sql = string.Format("UPDATE Fictions SET Section='{0}' WHERE Bookname = '{1}' AND Chapter='{2}'",MyCryptography.DESEncrypt(Section), MyCryptography.DESEncrypt(book.Name), MyCryptography.DESEncrypt(Chapter));
                Configs.Sql.ExecuteNonQuery(sql);
            }else
            {
                sql = string.Format("INSERT INTO Fictions(Bookname,Chapter,Section) VALUES('{0}','{1}','{2}')", MyCryptography.DESEncrypt(book.Name), MyCryptography.DESEncrypt(Chapter), MyCryptography.DESEncrypt(Section));
                Configs.Sql.ExecuteNonQuery(sql);
            }        
            return Section;
        }

        private void CachSection(int index,int Count)
        {
            for (int i = 1; i < 6; i++)
            {
                index += i;              
                if (index < Count)
                {
                    string Chapter = null;
                    string Chapterurl = null;
                    if (treeView1.InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            Chapter = treeView1.Nodes[0].Nodes[index].Text;
                            Chapterurl = treeView1.Nodes[0].Nodes[index].Tag.ToString();
                        }));
                    }
                    else
                    {
                        Chapter = treeView1.Nodes[0].Nodes[index].Text;
                        Chapterurl = treeView1.Nodes[0].Nodes[index].Tag.ToString();
                    }
                    string cach = null;
                    string cachsql = string.Format("SELECT Section FROM Fictions WHERE Bookname = '{0}' AND Chapter='{1}'", MyCryptography.DESEncrypt(book.Name), MyCryptography.DESEncrypt(Chapter));
                    DataTable cachDT = Configs.Sql.ExecuteQuery(cachsql);
                    if (cachDT.Rows.Count > 0)
                    {
                        cach = cachDT.Rows[0][0].ToString();
                    }
                    if (string.IsNullOrWhiteSpace(cach))
                    {
                        Getchapter(Chapter, Chapterurl);
                    }
                }
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (readindex == 0)
            {
                MessageBox.Show("Already the first chapter");
                return;
            }else if (readindex > 0)
            {
                readindex--;
                treeView1.Cursor = Cursors.WaitCursor;
                treeView1.SelectedNode = treeView1.Nodes[0].Nodes[readindex];
                treeView1_DoubleClick(null, null);
                richTextBox1.Select(0, 1);
                richTextBox1.ScrollToCaret();
                treeView1.Cursor = Cursors.Default;
            }
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (readindex > 0)
            {
                if (readindex == treeView1.Nodes.Count - 1)
                {
                    MessageBox.Show("Already the last chapter");
                    return;
                }
                readindex++;
                treeView1.Cursor = Cursors.WaitCursor;
                treeView1.SelectedNode = treeView1.Nodes[0].Nodes[readindex];
                treeView1_DoubleClick(null, null);
                richTextBox1.Select(0, 1);
                richTextBox1.ScrollToCaret();
                treeView1.Cursor = Cursors.Default;
            }
        }

        private void fontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FontDialog FD = new FontDialog();
            FD.Font = contextMenuStrip1.SourceControl.Font;
            if (FD.ShowDialog() == DialogResult.OK)
            { 
                contextMenuStrip1.SourceControl.Font = FD.Font;
            }
        }

        private void fontColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ColorDialog CD = new ColorDialog();
            CD.Color = contextMenuStrip1.SourceControl.ForeColor;
            if (CD.ShowDialog() == DialogResult.OK)
            {
                contextMenuStrip1.SourceControl.ForeColor= CD.Color;
            }
        }

        private void backColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ColorDialog CD = new ColorDialog();
            CD.Color = contextMenuStrip1.SourceControl.BackColor;
            if (CD.ShowDialog() == DialogResult.OK)
            {
                contextMenuStrip1.SourceControl.BackColor = CD.Color;
            }
        }

        private void ReadForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            string treefont = (new FontConverter()).ConvertToString(treeView1.Font);
            string treefontcolor = (new ColorConverter()).ConvertToString(treeView1.ForeColor);
            string treebackcolor = (new ColorConverter()).ConvertToString(treeView1.BackColor);
            string richfont = (new FontConverter()).ConvertToString(richTextBox1.Font);
            string richfontcolor = (new ColorConverter()).ConvertToString(richTextBox1.ForeColor);
            string richbackcolor = (new ColorConverter()).ConvertToString(richTextBox1.BackColor);
            string treesql = string.Format("UPDATE Configs SET Para1='{0}',Para2='{1}',Para3='{2}' WHERE Userid={3} AND CName='{4}'", treefont,treefontcolor,treebackcolor, Configs.UserId, "Tree");
            string richsql = string.Format("UPDATE Configs SET Para1='{0}',Para2='{1}',Para3='{2}' WHERE Userid={3} AND CName='{4}'", richfont,richfontcolor,richbackcolor, Configs.UserId, "Rich");
            Configs.Sql.ExecuteNonQuery(treesql);
            Configs.Sql.ExecuteNonQuery(richsql);

            if (treeView1.Nodes.Count > 0 && treeView1.SelectedNode!= null && treeView1.SelectedNode.Level==1)
            {
                string sql = string.Format("UPDATE Books SET Read='{0}' WHERE Name='{1}'", MyCryptography.DESEncrypt(treeView1.SelectedNode.Index.ToString()), MyCryptography.DESEncrypt(book.Name));
                Configs.Sql.ExecuteNonQuery(sql);
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if(pictureBox1.Tag.ToString() == "Close")
            {
                pictureBox1.BackgroundImage = Properties.Resources.right;
                woodwidth = splitContainer1.Panel1.Width;
                splitContainer1.SplitterDistance = 0;
                pictureBox1.Left = splitContainer1.Left + splitContainer1.Panel1.Width - pictureBox1.Width-2;
                pictureBox1.Tag = "Open";
            }
            else
            {
                pictureBox1.BackgroundImage = Properties.Resources.left;
                splitContainer1.SplitterDistance = woodwidth;
                pictureBox1.Left = splitContainer1.Left + splitContainer1.Panel1.Width - pictureBox1.Width-2;
                pictureBox1.Tag = "Close";
            }
        }

    }
}
