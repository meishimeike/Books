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
                MessageBox.Show("AAdd source fails, please check the address is correct");
                return;
            }
            string sql = string.Format("INSERT INTO Sources(Userid,Name,Address,Content) VALUES('{0}','{1}','{2}','{3}')", Configs.UserId, MyCryptography.DESEncrypt(sourcename), MyCryptography.DESEncrypt(sourceurl), MyCryptography.DESEncrypt(content));
            if (Configs.Sql.ExecuteNonQuery(sql) < 1)
            {
                MessageBox.Show("Sqlite failure,Add source failure");
                BookHelper.Logsadd("Sqlite failure,Add source failure");
                return;
            }
            BookHelper.AddSoucerAdress(sourcename, sourceurl, content);
            ListViewItem lvi = new ListViewItem(new string[] { sourcename, sourceurl });
            listView1.Items.Add(lvi);       
        }

        private void SourceForm_Load(object sender, EventArgs e)
        {
            listView1.Columns.Add("Source Name");
            listView1.Columns.Add("Source URL");
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
                if(MessageBox.Show("Are you sure you want to delete the selected items", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
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
                            MessageBox.Show("Delete Faile");
                    }
                }
            }
        }
    }
}
