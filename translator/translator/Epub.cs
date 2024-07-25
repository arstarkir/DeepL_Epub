using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace translator
{
    public partial class Epub : Screen
    {

        public Epub(int id, List<Control> toDraw, Form1 form1) : base(id, toDraw, form1) 
        {
            InitializeComponent();

            toDraw.Add(button1);
            toDraw.Add(button2);
            toDraw.Add(button3);
            toDraw.Add(checkedListBox1);
            toDraw.Add(checkBox2);
        }


        private TaskCompletionSource<bool> _buttonClickCompletion;

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
                        Directory.CreateDirectory(extractPath);
                        ZipFile.ExtractToDirectory(newFilePath, extractPath);

                        List<string> textFolderPath = FindTextFilesInDirectory(extractPath);
                        var titleToFileMap = ExtractTitlesAndMapToFiles(textFolderPath);

                        checkedListBox1.Items.Clear();
                        foreach (var title in titleToFileMap.Keys)
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
                            checkedListBox1.CheckedItems.OfType<string>().ToList().Select(title => titleToFileMap[title]).ToList();
                        GetForm1().Controls.Add(progressBar1);
                        progressBar1.Maximum = checkedFilePaths.Count;
                        foreach (var checkedFilePath in checkedFilePaths)
                        {
                            string result = await TranslateFileWithDeepL(textBox1.Text, checkedFilePath, countryCode
                                , (checkBox1.Checked) ? "https://api-free.deepl.com/v2/document" : "https://api.deepl.com/v2/document");

                            await UpdateXhtmlFileWithTranslation(checkedFilePath, result);
                            CorrectHtmlFile(checkedFilePath);


                            // This is a backup way of transleetion if File translation doesn't work
                            // Needs to be re implemented

                            //string result = await TranslateFileWithDeepL(textBox1.Text, checkedFilePath, countryCode
                            //    , (checkBox1.Checked) ? "https://api-free.deepl.com/v2/translate" : "https://api.deepl.com/v2/document");
                            //await UpdateXhtmlFileWithTranslation(checkedFilePath, result);
                            //CorrectHtmlFile(checkedFilePath);
                        }
                        Controls.Remove(progressBar1);
                        RepackToEpub(extractPath, Path.GetFileNameWithoutExtension(filePath));
                    }
                }
            }
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
                {
                    titleToFileMap.Add(title, filePath);
                }
            }
            if (filePaths.Count() > titleToFileMap.Count())
            {
                MessageBox.Show($"Title was not found! Emergency Full Book translation turned on", "HTML Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                checkBox2.Checked = true;
                checkBox2.Enabled = false;
            }
            return titleToFileMap;
        }

        private Task WaitForButtonPressAsync()
        {
            _buttonClickCompletion = new TaskCompletionSource<bool>();
            return _buttonClickCompletion.Task;
        }

        public async Task UpdateXhtmlFileWithTranslation(string filePath, string jsonResult)
        {
            try
            {
                string pattern = @"<([\w/:.-]+)(?![^><]*>)(?=(?:[\r\n\""}\]]|$))";
                string correctedHtml = Regex.Replace(jsonResult, pattern, m => $"<{m.Groups[1].Value}>");
                pattern = @"(</\w+>)(\r\n)?(<\w>)";
                correctedHtml = Regex.Replace(correctedHtml, pattern, m => { return m.Groups[2].Success ? m.Value : $"{m.Groups[1].Value}\r\n{m.Groups[3].Value}"; });
                JObject jsonResponse = JObject.Parse(correctedHtml);
                string translatedXhtml = jsonResponse["translations"][0]["text"].ToString();
                await File.WriteAllTextAsync(filePath, translatedXhtml, Encoding.UTF8);
                ((ProgressBar)GetForm1().GetControlByName("progressBar1")).PerformStep();
            }
            catch (JsonException jsonEx)
            {
                MessageBox.Show($"Failed to parse JSON response: {jsonEx.Message}", "JSON Parsing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ioEx)
            {
                MessageBox.Show($"Failed to write to file: {ioEx.Message}", "File Writing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public static async Task<string> TranslateFileWithDeepL(string apiKey, string filePath, string targetLangCode, string apiUrl)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"DeepL-Auth-Key {apiKey}");

                    var form = new MultipartFormDataContent();
                    form.Add(new StringContent(targetLangCode), "target_lang");

                    // Read the file and add it to the multipart form
                    using (var fileStream = File.OpenRead(filePath))
                    {
                        var fileContent = new StreamContent(fileStream);
                        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                        form.Add(fileContent, "file", Path.GetFileName(filePath));
                    }

                    HttpResponseMessage response = await httpClient.PostAsync(apiUrl, form);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        return jsonResponse;
                    }
                    else
                    {
                        return $"API Request failed: {response.StatusCode}";
                    }
                }
                catch (Exception ex)
                {
                    return $"Error sending request: {ex.Message}";
                }
            }
        }


        // This is a backup way of transleetion if File translation doesn't work
        // Needs to be re implemented
        public static async Task<string> TranslateTextWithDeepL(string apiKey, string filePath, string targetLangCode, string apiUrl)
        {
            string textToTranslate;

            try
            {
                textToTranslate = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            }
            catch (IOException ex)
            {
                return $"Error reading file: {ex.Message}";
            }

            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"DeepL-Auth-Key {apiKey}");

                    var content = new FormUrlEncodedContent(new[]
                    {
                new KeyValuePair<string, string>("text", textToTranslate),
                new KeyValuePair<string, string>("target_lang", targetLangCode)
            });

                    HttpResponseMessage response = await httpClient.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        return jsonResponse;
                    }
                    else
                    {
                        return $"API Request failed: {response.StatusCode}";
                    }
                }
                catch (Exception ex)
                {
                    return $"Error sending request: {ex.Message}";
                }
            }
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
                            Console.WriteLine("Directory does not exist.");
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
                        Console.WriteLine("An error occurred: " + ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("No folder selected.");
                }
            }

            return textFiles;
        }

        private void RepackToEpub(string extractPath, string originalEpubFileName)
        {
            string epubFilePath = Path.Combine(Path.GetDirectoryName(extractPath), originalEpubFileName + 
                ((ComboBox)GetForm1().GetControlByName("comboBox1")).SelectedText + ".epub");

            if (File.Exists(epubFilePath))
                File.Delete(epubFilePath);

            using (FileStream fs = new FileStream(epubFilePath, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    var mimetypeEntry = archive.CreateEntry("mimetype", CompressionLevel.NoCompression);
                    using (var writer = new StreamWriter(mimetypeEntry.Open()))
                    {
                        writer.Write("application/epub+zip");
                    }

                    DirectoryInfo dirInfo = new DirectoryInfo(extractPath);
                    foreach (FileInfo file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                    {
                        if (file.Name != "mimetype")
                        {
                            string relativePath = Path.GetRelativePath(extractPath, file.FullName).Replace('\\', '/');
                            archive.CreateEntryFromFile(file.FullName, relativePath, CompressionLevel.Optimal);
                        }
                    }
                }
            }
            MessageBox.Show($"EPUB repacked and saved as: {epubFilePath}", "Repack Successful", MessageBoxButtons.OK);
        }

        public static void CorrectHtmlFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("File does not exist: " + filePath);
                return;
            }

            try
            {
                var doc = new HtmlAgilityPack.HtmlDocument
                {
                    OptionOutputAsXml = true
                };

                //doc.Load(filePath);

                ////var nodes = doc.DocumentNode.Descendants().ToList();
                ////for (int i = 0; i < nodes.Count; i++)
                ////{
                ////    var node = nodes[i];
                ////    if (node.NodeType == HtmlNodeType.Element)
                ////    {
                ////        if (node.OuterHtml.EndsWith("<"))
                ////        {
                ////            node.InnerHtml += ">";
                ////        }
                ////    }
                ////}

                //doc.Save(filePath);
            }

            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _buttonClickCompletion?.TrySetResult(true);
        }
       
        public static string FindFirstFileWithExtension(string directoryPath, string extension)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Console.WriteLine("Directory does not exist.");
                    return null;
                }
                var files = Directory.GetFiles(directoryPath, "*" + extension, SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    return files[0];
                }
                else
                {
                    Console.WriteLine("No files with the specified extension were found.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
                return null;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            BookEditorForm bookEditorForm = new BookEditorForm();
            bookEditorForm.Show();
        }
    }
}
