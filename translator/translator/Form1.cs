﻿using static System.Net.Mime.MediaTypeNames;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;  
using System.Text.Json;         
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace translator
{
    public partial class Form1 : Form
    {
        private TaskCompletionSource<bool> _buttonClickCompletion;
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
            comboBox1.Items.Add(new ItemDisplay<string>("RU", "Russian"));
            comboBox1.Items.Add(new ItemDisplay<string>("SK", "Slovak"));
            comboBox1.Items.Add(new ItemDisplay<string>("SL", "Slovenian"));
            comboBox1.Items.Add(new ItemDisplay<string>("ES", "Spanish"));
            comboBox1.Items.Add(new ItemDisplay<string>("SV", "Swedish"));
            comboBox1.SelectedItem = comboBox1.Items[0];
        }

        private void Form1_Load(object sender, EventArgs e)
        {

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

                        string countryCode = (comboBox1.SelectedItem != null) ? (comboBox1.SelectedItem as ItemDisplay<string>).GetCountryCode() : null;

                        List<string> checkedFilePaths = (checkBox2.Checked) ? textFolderPath : 
                            checkedListBox1.CheckedItems.OfType<string>().ToList().Select(title => titleToFileMap[title]).ToList();
                        foreach (var checkedFilePath in checkedFilePaths)
                        {
                            string result = await TranslateTextWithDeepL(textBox1.Text, checkedFilePath, countryCode
                                , (checkBox1.Checked) ? "https://api-free.deepl.com/v2/translate" : "https://api.deepl.com/v2/translate" );
                            await UpdateXhtmlFileWithTranslation(checkedFilePath, result);
                            CorrectHtmlFile(checkedFilePath);
                        }

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
            if(filePaths.Count() > titleToFileMap.Count())
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

                MessageBox.Show("File updated successfully with translated content.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the folder containing .xml and .html files";
                dialog.SelectedPath = directoryPath;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string directoryPath2 = dialog.SelectedPath;

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
            string epubFilePath = Path.Combine(Path.GetDirectoryName(extractPath), originalEpubFileName + comboBox1.SelectedText + ".epub");

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
    }
}
