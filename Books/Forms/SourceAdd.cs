using System;
using System.Windows.Forms;

namespace Books
{
    public partial class SourceAdd : Form
    {
        public delegate void result(string sourcename, string sourceurl);
        public event result GetResult;
        public SourceAdd()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string sourcename = textBox1.Text.Trim();
            string sourceurl = textBox2.Text.Trim();
            if (sourcename == "")
            {
                MessageBox.Show("Source name is Empty");
                return;
            }
            GetResult(sourcename, sourceurl);
            Close();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
                linkLabel1.Visible = false;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
                linkLabel1.Visible = true;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenFileDialog Ofd = new OpenFileDialog();
            if (Ofd.ShowDialog() == DialogResult.OK)
            {
                textBox2.Text = Ofd.FileName;
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text.Trim() == "")
            {
                textBox1.Text = System.IO.Path.GetFileNameWithoutExtension(textBox2.Text);
            }
        }
    }
}
