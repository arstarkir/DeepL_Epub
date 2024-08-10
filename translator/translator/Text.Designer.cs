using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace translator
{
    public partial class Text
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            button1 = new Button();
            richTextBox3 = new RichTextBox();
            richTextBox1 = new RichTextBox();
            label4 = new Label();

            // 
            // richTextBox3
            // 
            richTextBox3.Location = new Point(413, 89);
            richTextBox3.Name = "richTextBox3";
            richTextBox3.Size = new Size(300, 300);
            richTextBox3.TabIndex = 14;
            richTextBox3.Text = "";
            richTextBox3.ReadOnly = true;
            // 
            // richTextBox1
            // 
            richTextBox1.Location = new Point(52, 89);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(300, 300);
            richTextBox1.TabIndex = 13;
            richTextBox1.Text = "";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 20F);
            label4.Location = new Point(361, 210);
            label4.Name = "label4";
            label4.Size = new Size(46, 37);
            label4.TabIndex = 15;
            label4.Text = "->";
            // 
            // button1
            // 
            button1.Font = new Font("Segoe UI", 13F);
            button1.Location = new Point(328, 395);
            button1.Name = "button1";
            button1.Size = new Size(111, 39);
            button1.TabIndex = 16;
            button1.Text = "Translate!";
            button1.Click += Button1_Click;
            button1.UseVisualStyleBackColor = true;
        }

        private Button button1;
        private RichTextBox richTextBox3;
        private RichTextBox richTextBox1;
        private Label label4;
    }
}
