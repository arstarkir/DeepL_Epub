
namespace translator
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            label2 = new Label();
            textBox1 = new TextBox();
            label3 = new Label();
            comboBox1 = new ComboBox();
            comboBox2 = new ComboBox();
            checkBox1 = new CheckBox();
            progressBar1 = new ProgressBar();
            richTextBox3 = new RichTextBox();
            richTextBox1 = new RichTextBox();
            label4 = new Label();
            button1 = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(191, 49);
            label1.Name = "label1";
            label1.Size = new Size(19, 15);
            label1.TabIndex = 1;
            label1.Text = "To";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 51);
            label2.Name = "label2";
            label2.Size = new Size(35, 15);
            label2.TabIndex = 10;
            label2.Text = "What";
            // 
            // textBox1
            // 
            textBox1.Location = new Point(66, 6);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(100, 23);
            textBox1.TabIndex = 2;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 9);
            label3.Name = "label3";
            label3.Size = new Size(48, 15);
            label3.TabIndex = 3;
            label3.Text = "API KEY";
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(216, 43);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(121, 23);
            comboBox1.TabIndex = 0;
            // 
            // comboBox2
            // 
            comboBox2.FormattingEnabled = true;
            comboBox2.Location = new Point(64, 43);
            comboBox2.Name = "comboBox2";
            comboBox2.Size = new Size(121, 23);
            comboBox2.TabIndex = 9;
            comboBox2.SelectedIndexChanged += comboBox2_SelectedIndexChanged;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Location = new Point(172, 9);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(70, 19);
            checkBox1.TabIndex = 7;
            checkBox1.Text = "Free Key";
            checkBox1.UseVisualStyleBackColor = true;
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(104, 420);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(551, 23);
            progressBar1.Step = 1;
            progressBar1.TabIndex = 9;
            // 
            // richTextBox3
            // 
            richTextBox3.Location = new Point(413, 89);
            richTextBox3.Name = "richTextBox3";
            richTextBox3.Size = new Size(300, 300);
            richTextBox3.TabIndex = 14;
            richTextBox3.Text = "";
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
            button1.Location = new Point(321, 395);
            button1.Name = "button1";
            button1.Size = new Size(111, 39);
            button1.TabIndex = 16;
            button1.Text = "Translate!";
            button1.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(button1);
            Controls.Add(label4);
            Controls.Add(richTextBox3);
            Controls.Add(richTextBox1);
            Controls.Add(label2);
            Controls.Add(comboBox2);
            Controls.Add(checkBox1);
            Controls.Add(label3);
            Controls.Add(textBox1);
            Controls.Add(comboBox1);
            Controls.Add(label1);
            Name = "Form1";
            Text = "DeepL_Epub";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Label label1;
        private TextBox textBox1;
        private Label label3;
        private ComboBox comboBox1;
        private CheckBox checkBox1;
        private ProgressBar progressBar1;
        private ComboBox comboBox2;
        private Label label2;
        private RichTextBox richTextBox3;
        private RichTextBox richTextBox1;
        private Label label4;
        private Button button1;
    }

}
