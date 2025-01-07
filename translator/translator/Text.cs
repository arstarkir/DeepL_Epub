using HtmlAgilityPack;
using NAudio.Wave;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;


namespace translator
{
    public partial class Audiobook : Screen
    {
        public Dictionary<string, string> curTitleToFileMap;
        private TaskCompletionSource<bool> _buttonClickCompletion;

        public Audiobook(int id, List<Control> toDraw, Form1 form1) : base(id, toDraw, form1)
        {
            InitializeComponent();

            toDraw.Add(button1);
            toDraw.Add(button2);
            toDraw.Add(checkedListBox1);
            toDraw.Add(checkBox2);
            toDraw.Add(label4);
        }

        public override void DrawScrean(Control.ControlCollection control)
        {
            Form1 form1 = GetForm1();
            form1.Controls.Remove(form1.GetControlByName("checkBox1"));
            form1.Controls.Remove(form1.GetControlByName("comboBox1"));
            form1.Controls.Remove(form1.GetControlByName("label1"));
            base.DrawScrean(control);
        }

        public override void ClearScrean(Control.ControlCollection control)
        {
            Form1 form1 = GetForm1();
            form1.Controls.Add(form1.GetControlByName("checkBox1"));
            form1.Controls.Add(form1.GetControlByName("comboBox1"));
            form1.Controls.Add(form1.GetControlByName("label1"));
            base.ClearScrean(control);
        }
        private async void button1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "epub files (*.epub)|*.epub|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.Multiselect = true;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (string filePath in openFileDialog.FileNames)
                    {

                        string workingDirectory = Path.GetDirectoryName(filePath);
                        string newFilePath = Path.Combine(workingDirectory, Path.GetFileNameWithoutExtension(filePath) + ".zip");
                        if (File.Exists(newFilePath))
                            File.Delete(newFilePath);
                        File.Copy(filePath, newFilePath);

                        string extractPath = Path.Combine(workingDirectory, Path.GetFileNameWithoutExtension(filePath));
                        if (!File.Exists(newFilePath))
                        {
                            Directory.CreateDirectory(extractPath);
                            ZipFile.ExtractToDirectory(newFilePath, extractPath);
                        }


                        List<string> textFolderPath = FindTextFilesInDirectory(extractPath);
                        curTitleToFileMap = ExtractTitlesAndMapToFiles(textFolderPath);

                        checkedListBox1.Items.Clear();
                        foreach (var title in curTitleToFileMap.Keys)
                        {
                            checkedListBox1.Items.Add(title);
                        }

                        await WaitForButtonPressAsync();

                        Form1 form1 = GetForm1();
                        ComboBox comboBox1 = (ComboBox)form1.GetControlByName("comboBox1");
                        ProgressBar progressBar1 = (ProgressBar)form1.GetControlByName("progressBar1");
                        TextBox textBox1 = (TextBox)form1.GetControlByName("textBox1");
                        CheckBox checkBox1 = (CheckBox)form1.GetControlByName("checkBox1");

                        string countryCode = (comboBox1.SelectedItem != null) ? (comboBox1.SelectedItem as ItemDisplay<string>).GetTValue() : null;
                        List<string> checkedFilePaths = (checkBox2.Checked) ? textFolderPath :
                            checkedListBox1.CheckedItems.OfType<string>().ToList().Select(title => curTitleToFileMap[title]).ToList();
                        GetForm1().Controls.Add(progressBar1);
                        progressBar1.Maximum = checkedFilePaths.Count * 2 + 3;

                        foreach (var checkedFilePath in checkedFilePaths)
                        {
                            ((ProgressBar)GetForm1().GetControlByName("progressBar1")).PerformStep();
                        }
                        GetForm1().Controls.Remove(progressBar1);
                    }
                }
            }
        }

        public static string ExtractTextFromHtml(string filePath)
        {
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.Load(filePath);
            var text = ExtractNodeText(htmlDoc.DocumentNode);
            
            return string.Join(". ", text.Split(new[] { ". " }, StringSplitOptions.RemoveEmptyEntries)).Trim() + ".";
        }

        private static string ExtractNodeText(HtmlNode node)
        {
            if (node.NodeType == HtmlNodeType.Text)
                return node.InnerText.Trim();

            string extractedText = string.Empty;
            foreach (var child in node.ChildNodes)
            {
                var childText = ExtractNodeText(child);
                if (!string.IsNullOrWhiteSpace(childText))
                {
                    if (!string.IsNullOrEmpty(extractedText))
                        extractedText += ". ";
                    extractedText += childText;
                }
            }

            return extractedText.Trim();
        }

        public void ForceFileCreate(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            using (File.Create(filePath)) ;
        }

        public Dictionary<string, string> ExtractTitlesAndMapToFiles(List<string> filePaths)
        {
            Dictionary<string, string> titleToFileMap = new Dictionary<string, string>();
            foreach (var filePath in filePaths)
            {
                HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.Load(filePath);

                var titleNode = htmlDoc.DocumentNode.SelectSingleNode("//title") ?? htmlDoc.DocumentNode.SelectSingleNode("//h1");
                string title = titleNode != null ? titleNode.InnerText.Trim() : "No title found";

                if (!titleToFileMap.ContainsKey(title))
                    titleToFileMap.Add(title, filePath);
                else
                    AlreadyContainsKey(titleToFileMap, title, filePath, 1);
            }

            return titleToFileMap;
        }

        public void AlreadyContainsKey(Dictionary<string, string> titleToFileMap, string title, string filePath, int num)
        {
            if (titleToFileMap.ContainsKey(title + num.ToString()))
                AlreadyContainsKey(titleToFileMap, title, filePath, num + 1);
            else
                titleToFileMap.Add(title + num.ToString(), filePath);
        }

        private Task WaitForButtonPressAsync()
        {
            _buttonClickCompletion = new TaskCompletionSource<bool>();
            return _buttonClickCompletion.Task;
        }

        public List<string> FindTextFilesInDirectory(string directoryPath)
        {
            List<string> textFiles = new List<string>();

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.FileName = "Select the folder containing .xml and .html files";
                dialog.InitialDirectory = directoryPath;
                dialog.Filter = "All files (*.*)|*.*";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string directoryPath2 = Path.GetDirectoryName(dialog.FileName);

                    try
                    {
                        if (!Directory.Exists(directoryPath2))
                        {
                            MessageBox.Show($"Directory does not exist.", "Error!", MessageBoxButtons.OK);
                            return textFiles;
                        }

                        string[] files = Directory.GetFiles(directoryPath2, "*.*", SearchOption.AllDirectories);
                        foreach (string file in files)
                        {
                            if (file.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                                file.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                                file.EndsWith(".xhtml", StringComparison.OrdinalIgnoreCase) ||
                                file.EndsWith(".htm", StringComparison.OrdinalIgnoreCase))
                            {
                                textFiles.Add(file);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred on \"FindTextFilesInDirectory\": {ex.Message}", "Error!", MessageBoxButtons.OK);
                    }
                }
                else
                {
                    MessageBox.Show($"No folder selected.", "Error!", MessageBoxButtons.OK);
                }
            }

            return textFiles;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _buttonClickCompletion?.TrySetResult(true);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            BookEditorForm bookEditorForm = new BookEditorForm();
            bookEditorForm.Show();
        }
        int curCharacterCount = 0;
        double curEstimatedCost = 0;
        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            string filePath = curTitleToFileMap[checkedListBox1.Items[e.Index].ToString()];
            if (!File.Exists(filePath))
            {
                MessageBox.Show("Source file does not exist or has been moved.", "Error!", MessageBoxButtons.OK);
                return;
            }

            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                if (e.NewValue != CheckState.Unchecked)
                {
                    checkedListBox1.SetItemCheckState(e.Index, CheckState.Unchecked);
                    using Process fileopener = new Process();
                    fileopener.StartInfo.FileName = "explorer";
                    fileopener.StartInfo.Arguments = "\"" + filePath + "\"";
                    fileopener.Start();
                }
            }

            string fileContent = File.ReadAllText(filePath);

            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(fileContent);
            int tempCharacterCount = htmlDoc.DocumentNode.InnerText.Length;
            double tempEstimatedCost = (tempCharacterCount / 40000.0) * 1.0;

            curCharacterCount += (e.NewValue == CheckState.Unchecked) ? -1 * tempCharacterCount : 1 * tempCharacterCount;
            curEstimatedCost += (e.NewValue == CheckState.Unchecked) ? -1 * tempEstimatedCost : 1 * tempEstimatedCost;
            label4.Text = $"{curCharacterCount} Characters\n ~${curEstimatedCost:F2}";
        }

        public async Task<string> GenerateSpeechAndSaveAsync(string textToConvert, string voiceId, int id)
        {
            var payload = new
            {
                text = textToConvert,
                model_id = "eleven_monolingual_v1"
            };

            string jsonPayload = JsonConvert.SerializeObject(payload);

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("xi-api-key", GetForm1().GetControlByName("textBox1").Text);

                HttpResponseMessage response = await client.PostAsync(
                    $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}/stream",
                    new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                );

                if (response.IsSuccessStatusCode)
                {
                    byte[] audioData = await response.Content.ReadAsByteArrayAsync();
                    string tempMp3Path = Path.Combine(Directory.GetCurrentDirectory(), "RepackingAudio", $"tempAudio{id}.mp3");
                    await File.WriteAllBytesAsync(tempMp3Path, audioData);

                    string wavFilePath = Path.Combine(Directory.GetCurrentDirectory(), "RepackingAudio", Path.GetFileNameWithoutExtension(tempMp3Path) + ".wav");
                    using (var mp3Reader = new Mp3FileReader(tempMp3Path))
                    using (var waveWriter = new WaveFileWriter(wavFilePath, mp3Reader.WaveFormat))
                    {
                        mp3Reader.CopyTo(waveWriter);
                    }

                    return wavFilePath;
                }
                else
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error calling ElevenLabs API: {response.StatusCode}. Details: {errorDetails}");
                }
            }
        }
    }
}
