using System;
using System.Windows.Forms;
using BookHelperLib;
using System.Collections.Generic;

namespace Books
{
    public partial class SourceForm : Form
    {
        public delegate void elSource(string rootsourcename);
        public event elSource delSource;
        public SourceForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SourceAdd SA = new SourceAdd();
            SA.GetResult += SA_GetResult;
            SA.ShowDialog();
        }

        private void SA_GetResult(string sourcename, string sourceurl)
        {
            string content = BookHelper.GetRequst(sourceurl);
            if (string.IsNullOrWhiteSpace(content))
            {
                MessageBox.Show("源添加失败,请检查源地址是否正确.");
                return;
            }
            string sql = string.Format("INSERT INTO Sources(Userid,Name,Address,Content) VALUES('{0}','{1}','{2}','{3}')", Configs.UserId, MyCryptography.DESEncrypt(sourcename), MyCryptography.DESEncrypt(sourceurl), MyCryptography.DESEncrypt(content));
            if (Configs.Sql.ExecuteNonQuery(sql) < 1)
            {
                MessageBox.Show("Sqlite 错误,源添加失败.");
                return;
            }
            BookHelper.AddSoucerAdress(sourcename, sourceurl, content);
            ListViewItem lvi = new ListViewItem(new string[] { sourcename, sourceurl });
            listView1.Items.Add(lvi);       
        }

        private void SourceForm_Load(object sender, EventArgs e)
        {
            listView1.Columns.Add("源名称");
            listView1.Columns.Add("源地址");
            SourceForm_Resize(null, null);
            List<KeyValuePair<string, string>> sources = BookHelper.GetSoucerAdress();
            for(int i = 0; i < sources.Count; i++)
            {
                ListViewItem lvi = new ListViewItem(new string[] { sources[i].Key, sources[i].Value });
                listView1.Items.Add(lvi);
            }
        }

        private void SourceForm_Resize(object sender, EventArgs e)
        {
            for (int i = 0; i < listView1.Columns.Count; i++)
            {
                listView1.Columns[i].Width =(int)(listView1.Width / listView1.Columns.Count*0.9);
                listView1.Columns[i].TextAlign = HorizontalAlignment.Center;
            }
        }

        private void SourceForm_FormClosed(object sender, FormClosedEventArgs e)
        {
           
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                if(MessageBox.Show("您确定要删除选中的源吗?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    for(int i=0;i< listView1.SelectedItems.Count; i++)
                    {
                        string rootsourcename = listView1.SelectedItems[i].Text;
                        string sql = string.Format("DELETE FROM Sources WHERE Userid={0} AND Name = '{1}'", Configs.UserId, MyCryptography.DESEncrypt(rootsourcename));
                        if (Configs.Sql.ExecuteNonQuery(sql) > 0)
                        {
                            listView1.SelectedItems[i].Remove();
                            delSource(rootsourcename);
                        }       
                        else
                            MessageBox.Show("删除失败!");
                    }
                }
            }
        }
    }
}
