using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using BookHelperLib;
using System.Threading;

namespace Books
{
    public partial class SearchForm : Form
    {
        public delegate void Addbook(BookHelper.Book book);
        public event Addbook Getbook;

        struct pageinfo
        {
            public string pageurl;
            public string pagename;
            public string rootname;
            public string sourcename;
        }
        public SearchForm()
        {
            InitializeComponent();
        }

        private void SearchForm_Load(object sender, EventArgs e)
        {
            GetTree();
        }

        private void GetTree()
        {
            List<string> trees = BookHelper.GetRootSourceName();
            if(trees!=null && trees.Count > 0)
            {
                foreach (string tree in trees)
                {
                    TreeNode TN = new TreeNode(tree);
                    List<string> subnodes = BookHelper.GetSourceName(tree);
                    foreach (string node in subnodes)
                    {
                        TreeNode subTN = new TreeNode(node);
                        TN.Nodes.Add(subTN);
                    }
                    treeView1.Nodes.Add(TN);
                }
            }else
            {
                MessageBox.Show("Please add source.");
            }
            
        }
      
        private void AddControl(string rootname, string sourcename, List<KeyValuePair<string, string>> Pages)
        {
            listView1.Controls.Clear();
            for (int i = 0; i < Pages.Count; i++)
            {

                ToolTip TT = new ToolTip();
                pageinfo PI = new pageinfo();
                PI.pagename = Pages[i].Key;
                PI.pageurl = Pages[i].Value;
                PI.rootname = rootname;
                PI.sourcename = sourcename;

                PictureBox pb = new PictureBox();
                pb.Size = new Size(20, 20);
                pb.BackColor = Color.Transparent;
                pb.BackgroundImage = Properties.Resources.circleround;
                Graphics g1 = Graphics.FromImage(pb.BackgroundImage);
                Font myFont = new Font("宋体", 12, GraphicsUnit.Pixel);
                SolidBrush myBrush = new SolidBrush(Color.White);
                int xPos = (int)myFont.Size / 3;
                int yPos = (int)myFont.Size / 3;
                g1.DrawString(PI.pagename, myFont, myBrush, xPos, yPos);
                pb.BackColor = Color.Transparent;
                pb.Parent = listView1;
                pb.Tag = PI;
                pb.Left = listView1.Left + listView1.Width - pb.Width - 20;
                pb.Top = listView1.Top + (listView1.Height - Pages.Count * 20) / 2 + i * 20;
                pb.Anchor = AnchorStyles.Right;
                pb.Cursor = Cursors.Hand;
                pb.Click += Pb_Click;
                TT.SetToolTip(pb, PI.pagename);
                listView1.Controls.Add(pb);

            }
        }
        private void Pb_Click(object sender, EventArgs e)
        {
            PictureBox pb = (PictureBox)sender;
            pageinfo pi = (pageinfo)pb.Tag;
            GetBooksList(pi.rootname, pi.sourcename, pi.pageurl);
        }

        private void treeView1_DoubleClick(object sender, EventArgs e)
        {
            treeView1.Cursor = Cursors.WaitCursor;
            if (treeView1.SelectedNode == null)
            {
                return;
            }           
            if (treeView1.SelectedNode.Level == 1 && treeView1.SelectedNode.Nodes.Count == 0)
            {
                string rootname = treeView1.SelectedNode.Parent.Text;
                string sourcename = treeView1.SelectedNode.Text;
                List<KeyValuePair<string, string>> SortsList = BookHelper.GetBookSorts(rootname, sourcename);
                if (SortsList != null)
                {
                    for (int i = 0; i < SortsList.Count; i++)
                    {
                        TreeNode tn = new TreeNode(SortsList[i].Key);
                        tn.Tag = SortsList[i].Value;
                        treeView1.SelectedNode.Nodes.Add(tn);
                    }
                }
            }
            if (treeView1.SelectedNode.Level == 2)
            {
                string rootname = treeView1.SelectedNode.Parent.Parent.Text;
                string sourcename = treeView1.SelectedNode.Parent.Text;
                string listurl = treeView1.SelectedNode.Tag.ToString();
                GetBooksList(rootname, sourcename, listurl);
            }
            treeView1.Cursor = Cursors.Default;
            UpdateCoveimages();
        }

        private void GetBooksList(string rootname, string sourcename, string listurl)
        {
            listView1.Items.Clear();
            listView1.Controls.Clear();

            List<KeyValuePair<string, string>> Pages = new List<KeyValuePair<string, string>>();
            List<BookHelper.Book> books = BookHelper.GetBooksList(rootname, sourcename, listurl, out Pages);
            for (int i = 0; i < books.Count; i++)
            {
                BookHelper.Book book = books[i];

                Image coverimage = null;           
                if (File.Exists(book.Coverpath))
                {
                    coverimage = BookHelper.ReadImageFile(book.Coverpath);
                }
                else
                {
                    coverimage = new Bitmap(Properties.Resources._null);
                }
                imageList1.Images.Add(book.Name, coverimage);
                coverimage.Dispose();
                ListViewItem lvi = new ListViewItem(book.Name);
                lvi.Tag = book;
                lvi.ImageKey = book.Name;
                lvi.ToolTipText = "作者:" + book.Author + Environment.NewLine + "源:" + book.RootSourcename + "[" + book.Sourcename + "]" + Environment.NewLine + "简介:" + book.Des;
                listView1.Items.Add(lvi);
            }
            AddControl(rootname, sourcename, Pages);          
        }

        private void UpdateCoveimages()
        {
            for (int i = 0; i < listView1.Items.Count; i++)
            {
                BookHelper.Book book = (BookHelper.Book)listView1.Items[i].Tag;
                Thread UpdateImageThread = new Thread(() => UpdateImage(book));
                UpdateImageThread.Start();
            }
         }
         private void UpdateImage(BookHelper.Book book)
         {
            if (!File.Exists(book.Coverpath))
            {
                BookHelper.DownloadFile(book.Coverurl, book.Coverpath);
                if (File.Exists(book.Coverpath))
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => {
                            try
                            {
                                imageList1.Images.RemoveByKey(book.Name);
                                imageList1.Images.Add(book.Name, BookHelper.ReadImageFile(book.Coverpath));
                            }
                            catch (Exception)
                            {
                                //File.Delete(book.Coverpath);
                                //throw;
                            }
                            
                        }));
                    }
                    else
                    {
                        try
                        {
                            imageList1.Images.RemoveByKey(book.Name);
                            imageList1.Images.Add(book.Name, BookHelper.ReadImageFile(book.Coverpath));
                        }
                        catch (Exception)
                        {
                            //File.Delete(book.Coverpath);
                            //throw;
                        }
                    }
                    
                }
            }
         }      
        private void addToMybooksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                foreach(ListViewItem lv in listView1.SelectedItems)
                {
                    BookHelper.Book book = (BookHelper.Book)lv.Tag;
                    Getbook(book);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            listView1.Controls.Clear();

            textBox1.Cursor = Cursors.WaitCursor;
            button1.Cursor = Cursors.WaitCursor;

            List<BookHelper.Book> books = BookHelper.GetSearchBooksList(textBox1.Text.Trim());

            if (books == null) { return; }
            for (int i = 0; i < books.Count; i++)
            {
                BookHelper.Book book = books[i];

                Image coverimage = null;
                if (File.Exists(book.Coverpath))
                {
                    coverimage = BookHelper.ReadImageFile(book.Coverpath);
                }
                else
                {
                    coverimage = new Bitmap(Properties.Resources._null);
                }
                imageList1.Images.Add(book.Name, coverimage);
                coverimage.Dispose();
                ListViewItem lvi = new ListViewItem(book.Name);
                lvi.Tag = book;
                lvi.ImageKey = book.Name;
                lvi.ToolTipText = "作者:" + book.Author + Environment.NewLine + "源:" + book.RootSourcename + "[" + book.Sourcename + "]" + Environment.NewLine + "简介:" + book.Des;
                listView1.Items.Add(lvi);         
            }

            UpdateCoveimages();
            textBox1.Cursor = Cursors.IBeam;
            button1.Cursor = Cursors.Default;
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
    }
}
