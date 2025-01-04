using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace translator
{
    public partial class Text : Screen
    {
        public Text(int id, List<Control> toDraw, Form1 form1) : base(id, toDraw, form1)
        {
            InitializeComponent();

            toDraw.Add(button1);
            toDraw.Add(label4);
            toDraw.Add(richTextBox3);
            toDraw.Add(richTextBox1);
        }

        private async void Button1_Click(object sender, EventArgs e)
        {
            Form1 form1 = GetForm1();
            ComboBox comboBox1 = (ComboBox)form1.GetControlByName("comboBox1");
            ProgressBar progressBar1 = (ProgressBar)form1.GetControlByName("progressBar1");
            TextBox textBox1 = (TextBox)form1.GetControlByName("textBox1");
            CheckBox checkBox1 = (CheckBox)form1.GetControlByName("checkBox1");

            string countryCode = (comboBox1.SelectedItem != null) ? (comboBox1.SelectedItem as ItemDisplay<string>).GetTValue() : null;

            string result = await DeepLTranslation.TranslateTextWithDeepL(textBox1.Text, 
                (checkBox1.Checked) ? "https://api-free.deepl.com/v2/translate" : "https://api.deepl.com/v2/translate",
                countryCode, richTextBox1.Text);

            JObject jsonResponse = JObject.Parse(result);
            richTextBox3.Text = jsonResponse["translations"][0]["text"].ToString();
            
        }
    }
}
