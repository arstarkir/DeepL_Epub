using Microsoft.VisualBasic.Devices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

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
            toDraw.Add(checkBox3);
            toDraw.Add(label4);
        }
        public Dictionary<string, string> curTitleToFileMap;
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

                        await StartRepacking(countryCode, textBox1.Text,
                            (checkBox1.Checked) ? "https://api-free.deepl.com/v2/translate" : "https://api.deepl.com/v2/translate", openFileDialog.FileName);

                        foreach (var checkedFilePath in checkedFilePaths)
                        {
                            string result = await DeepLTranslation.TranslateFileWithDeepL(textBox1.Text, checkedFilePath, countryCode
                                , (checkBox1.Checked) ? "https://api-free.deepl.com/v2/document" : "https://api.deepl.com/v2/document");
                            ((ProgressBar)GetForm1().GetControlByName("progressBar1")).PerformStep();

                            await CreateChapterAndPutTransitionIn(checkedFilePath, result, textBox1.Text
                                , (checkBox1.Checked) ? "https://api-free.deepl.com/v2/translate" : "https://api.deepl.com/v2/translate", countryCode);
                            ((ProgressBar)GetForm1().GetControlByName("progressBar1")).PerformStep();
                        }
                        Controls.Remove(progressBar1);

                        if (checkBox3.Checked)
                            ChangeCover(Path.Combine(Directory.GetCurrentDirectory(), "RepackingFolder", "Empty", "OEBPS", "Images"));
                        RepackToEpub(Path.Combine(Directory.GetCurrentDirectory(), "RepackingFolder", "Empty"), Path.GetFileNameWithoutExtension(filePath) + "_" + countryCode);
                    }
                }
            }
        }

        public void ChangeCover(string filePath)
        {
            MessageBox.Show($"Sevect new cover. It must be .jpg file.", "Sevect New Cover", MessageBoxButtons.OK);

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "jpg files (*.jpg)|*.jpg|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.Multiselect = false;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    if (File.Exists(filePath + "\\cover.jpg"))
                        File.Delete(filePath + "\\cover.jpg");
                    File.Copy(openFileDialog.FileName, filePath + "\\cover.jpg");
                }
            }
        }

        public async Task CreateChapterAndPutTransitionIn(string filePath, string textToWrite, string apiKey, string apiUrl, string countryCode)
        {
            string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "RepackingFolder", "Empty", "OEBPS", "Text");
            string newFilePath = Path.Combine(directoryPath, $"{Path.GetFileName(filePath)}");
            if (!File.Exists(newFilePath))
                ForceFileCreate(newFilePath);
            File.WriteAllText(newFilePath, textToWrite, Encoding.UTF8);

            AddChapterToOpf(Path.Combine(Directory.GetCurrentDirectory(), "RepackingFolder", "Empty", "OEBPS", "content.opf"), $"{Path.GetFileName(newFilePath)}");
        }

        public void ForceFileCreate(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            using (File.Create(filePath)) ;
        }

        public static void AddChapterToOpf(string opfFilePath, string chapterFileName)
        {
            try
            {
                XDocument opfDocument = XDocument.Load(opfFilePath);
                XNamespace opf = "http://www.idpf.org/2007/opf";
                XElement manifest = opfDocument.Root.Element(opf + "manifest");
                XElement spine = opfDocument.Root.Element(opf + "spine");

                string itemId = $"id{Guid.NewGuid().ToString("N")}";

                XElement newItem = new XElement(opf + "item",
                    new XAttribute("id", itemId),
                    new XAttribute("href", $"Text/{chapterFileName}"),
                    new XAttribute("media-type", "application/xhtml+xml"));
                manifest.Add(newItem);

                XElement itemRef = new XElement(opf + "itemref", new XAttribute("idref", itemId));
                spine.Add(itemRef);

                opfDocument.Save(opfFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating OPF file: {ex.Message}", "Error!", MessageBoxButtons.OK);
            }
        }

        public async Task StartRepacking(string countryCode, string apiKey, string apiUrl, string originalFilePath)
        {
            string sourcePath = Path.Combine(Directory.GetCurrentDirectory(), "Empty");
            string destinationPath = Path.Combine(Directory.GetCurrentDirectory(), "RepackingFolder", "Empty");

            try
            {
                if (Directory.Exists(sourcePath))
                    CopyAllFiles(sourcePath, destinationPath);
                else
                    MessageBox.Show($"Source directory does not exist or has already been moved.", "Error!", MessageBoxButtons.OK);
            }
            catch (IOException ex)
            {
                MessageBox.Show($"An error occurred on \"StartRepacking\": {ex.Message}", "Error!", MessageBoxButtons.OK);
            }

            ((ProgressBar)GetForm1().GetControlByName("progressBar1")).PerformStep();

            string result = await DeepLTranslation.TranslateTextWithDeepL(apiKey, apiUrl, countryCode, Path.GetFileNameWithoutExtension(originalFilePath));
            JObject jsonResponse = JObject.Parse(result);
            string translatedTitle = jsonResponse["translations"][0]["text"].ToString();
            translatedTitle = translatedTitle + "_" + countryCode;

            ((ProgressBar)GetForm1().GetControlByName("progressBar1")).PerformStep();

            await UpdateOpfFile(destinationPath, countryCode, translatedTitle);
        }

        public async Task UpdateOpfFile(string destinationPath, string lang, string title)
        {
            string opfFilePath = Path.Combine(destinationPath, "OEBPS", "content.opf");
            if (!File.Exists(opfFilePath))
                return;

            try
            {
                XDocument opfDocument = XDocument.Load(opfFilePath);

                XNamespace dc = "http://purl.org/dc/elements/1.1/";
                XNamespace opf = "http://www.idpf.org/2007/opf";
                XNamespace pkg = "http://www.idpf.org/2007/opf";

                XElement metadataElement = opfDocument.Root.Element(pkg + "metadata");
                if (metadataElement != null)
                {
                    metadataElement.Element(dc + "language").Value = lang;
                    metadataElement.Element(dc + "title").Value = title;

                    XElement metaElement = new XElement(pkg + "meta",
                        new XAttribute("name", "Sigil version"),
                        new XAttribute("content", "2.2.1"));
                    metadataElement.Add(metaElement);

                    XElement dateElement = new XElement(dc + "date",
                        new XAttribute(opf + "event", "modification"),
                        "2024-08-04");
                    metadataElement.Add(dateElement);

                    opfDocument.Save(opfFilePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred on \"UpdateOpfFile\": {ex.Message}", "Error!", MessageBoxButtons.OK);
            }

            ((ProgressBar)GetForm1().GetControlByName("progressBar1")).PerformStep();
        }
        private static void CopyAllFiles(string sourceDirectory, string targetDirectory)
        {
            Directory.CreateDirectory(targetDirectory);

            foreach (var file in Directory.GetFiles(sourceDirectory))
            {
                File.Copy(file, Path.Combine(targetDirectory, Path.GetFileName(file)));
            }

            foreach (var directory in Directory.GetDirectories(sourceDirectory))
            {
                CopyAllFiles(directory, Path.Combine(targetDirectory, Path.GetFileName(directory)));
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
                using Process fileopener = new Process();
                fileopener.StartInfo.FileName = "explorer";
                fileopener.StartInfo.Arguments = "\"" + filePath + "\"";
                fileopener.Start();
                checkedListBox1.SetItemCheckState(e.Index, CheckState.Unchecked);
                return;
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
    }
}
