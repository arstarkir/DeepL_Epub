using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.DataFormats;

namespace translator
{
    public partial class Audiobook
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
            checkBox2 = new CheckBox();
            checkedListBox1 = new CheckedListBox();
            button1 = new Button();
            button2 = new Button();
            label4 = new Label();

            // 
            // button1
            // 
            button1.Location = new Point(22, 136);
            button1.Name = "button1";
            button1.Size = new Size(85, 33);
            button1.TabIndex = 4;
            button1.Text = "Add File";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // checkedListBox1
            // 
            checkedListBox1.FormattingEnabled = true;
            checkedListBox1.Location = new Point(104, 122 + 50);
            checkedListBox1.Name = "checkedListBox1";
            checkedListBox1.Size = new Size(551, 292);
            checkedListBox1.TabIndex = 5;
            checkedListBox1.ItemCheck += checkedListBox1_ItemCheck;
            // 
            // button2
            // 
            button2.Location = new Point(703, 465);
            button2.Name = "button2";
            button2.Size = new Size(85 + 20, 33);
            button2.TabIndex = 6;
            button2.Text = "I'm Done";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // checkBox2
            // 
            checkBox2.AutoSize = true;
            checkBox2.Location = new Point(568 - 10, 381 + 75);
            checkBox2.Name = "checkBox2";
            checkBox2.Size = new Size(85, 29);
            checkBox2.TabIndex = 8;
            checkBox2.Text = "Full Book";
            checkBox2.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(703, 405);
            label4.Margin = new Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new Size(54, 25);
            label4.TabIndex = 10;
            label4.Text = "";
        }

        private Button button1;
        private CheckedListBox checkedListBox1;
        private Button button2;
        private CheckBox checkBox2;
        private Label label4;
    }
}
