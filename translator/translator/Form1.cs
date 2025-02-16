﻿
namespace translator
{
    public partial class Form1 : Form
    {
        Screen prewScreen;

        public Form1()
        {
            InitializeComponent();
            comboBox1.Items.Add(new ItemDisplay<string>("UK", "Ukrainian"));
            comboBox1.Items.Add(new ItemDisplay<string>("BG", "Bulgarian"));
            comboBox1.Items.Add(new ItemDisplay<string>("ZH", "Chinese"));
            comboBox1.Items.Add(new ItemDisplay<string>("CS", "Czech"));
            comboBox1.Items.Add(new ItemDisplay<string>("DA", "Danish"));
            comboBox1.Items.Add(new ItemDisplay<string>("NL", "Dutch"));
            comboBox1.Items.Add(new ItemDisplay<string>("EN-US", "English (American)"));
            comboBox1.Items.Add(new ItemDisplay<string>("EN-GB", "English (British)"));
            comboBox1.Items.Add(new ItemDisplay<string>("ET", "Estonian"));
            comboBox1.Items.Add(new ItemDisplay<string>("FI", "Finnish"));
            comboBox1.Items.Add(new ItemDisplay<string>("FR", "French"));
            comboBox1.Items.Add(new ItemDisplay<string>("DE", "German"));
            comboBox1.Items.Add(new ItemDisplay<string>("EL", "Greek"));
            comboBox1.Items.Add(new ItemDisplay<string>("HU", "Hungarian"));
            comboBox1.Items.Add(new ItemDisplay<string>("ID", "Indonesian"));
            comboBox1.Items.Add(new ItemDisplay<string>("IT", "Italian"));
            comboBox1.Items.Add(new ItemDisplay<string>("JA", "Japanese"));
            comboBox1.Items.Add(new ItemDisplay<string>("KO", "Korean"));
            comboBox1.Items.Add(new ItemDisplay<string>("LV", "Latvian"));
            comboBox1.Items.Add(new ItemDisplay<string>("LT", "Lithuanian"));
            comboBox1.Items.Add(new ItemDisplay<string>("NO", "Norwegian (bokmål)"));
            comboBox1.Items.Add(new ItemDisplay<string>("PL", "Polish"));
            comboBox1.Items.Add(new ItemDisplay<string>("PT", "Portuguese"));
            comboBox1.Items.Add(new ItemDisplay<string>("PT-BR", "Portuguese (Brazilian)"));
            comboBox1.Items.Add(new ItemDisplay<string>("RO", "Romanian"));
            //comboBox1.Items.Add(new ItemDisplay<string>("RU", "Russian"));
            comboBox1.Items.Add(new ItemDisplay<string>("SK", "Slovak"));
            comboBox1.Items.Add(new ItemDisplay<string>("SL", "Slovenian"));
            comboBox1.Items.Add(new ItemDisplay<string>("ES", "Spanish"));
            comboBox1.Items.Add(new ItemDisplay<string>("SV", "Swedish"));
            comboBox1.SelectedItem = comboBox1.Items[0];

            comboBox2.Items.Add(new ItemDisplay<Screen>(new Screen(0, new List<Control>()), "Non"));
            comboBox2.Items.Add(new ItemDisplay<Screen>(new Epub(1, new List<Control>(), this), ".epub"));
            comboBox2.Items.Add(new ItemDisplay<Screen>(new Audiobook(1, new List<Control>(), this), "Audiobook"));
            comboBox2.Items.Add(new ItemDisplay<Screen>(new Text(1, new List<Control>(), this), "Text"));
            comboBox2.Items.Add(new ItemDisplay<Screen>(new Text(1, new List<Control>(), this), "Web Files (.html/.xhtml/.htm/.xhtm)"));
            comboBox2.SelectedItem = comboBox2.Items[0];

            Application.ApplicationExit += new EventHandler(OnApplicationExit);
            this.FormClosing += new FormClosingEventHandler(OnApplicationExit);
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            //CleanRepackingFolder();
        }

        private void CleanRepackingFolder()
        {
            string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "RepackingFolder");

            foreach (string file in Directory.GetFiles(folderPath))
            {
                File.Delete(file);
            }

            foreach (string dir in Directory.GetDirectories(folderPath))
            {
                Directory.Delete(dir, true);
            }
        }

        public Control GetControlByName(string name)
        {
            foreach (Control control in AllControls)
            {
                if (control.Name == name)
                    return control;
            }
            if (name == "progressBar1")
                return progressBar1;
            return null;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            Screen screen = (comboBox2.SelectedItem != null) ? (comboBox2.SelectedItem as ItemDisplay<Screen>).GetTValue() : null;
            if (prewScreen != null)
                DeactivateScreens(prewScreen);
            prewScreen = screen;

            if (screen == null)
                return;
            ActivateScreen(screen);
        }

        void DeactivateScreens(Screen screen)
        {
            screen.ClearScrean(Controls);
        }

        void ActivateScreen(Screen screen)
        {
            screen.DrawScrean(Controls);
        }
    }
}
