using static System.Net.Mime.MediaTypeNames;
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

                        string workingDirectory = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
                        string newFilePath = Path.Combine(workingDirectory, Path.GetFileNameWithoutExtension(filePath) + ".zip");
                        MessageBox.Show($"{workingDirectory}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                        var checkedTitles = checkedListBox1.CheckedItems.OfType<string>().ToList();
                        List<string> checkedFilePaths = checkedTitles.Select(title => titleToFileMap[title]).ToList();
                        foreach(var checkedFilePath in checkedFilePaths)
                        {
                            string result = await TranslateTextWithDeepL(textBox1.Text, checkedFilePath, countryCode); 
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
            return titleToFileMap;
        }

        private Task WaitForButtonPressAsync()
        {
            _buttonClickCompletion = new TaskCompletionSource<bool>();
            return _buttonClickCompletion.Task;
        }

        public List<string> ExtractTitlesFromHtmlFiles(List<string> filePaths)
        {
            List<string> titles = new List<string>();

            foreach (var filePath in filePaths)
            {
                if (File.Exists(filePath))
                {
                    HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
                    htmlDoc.Load(filePath);

                    var titleNode = htmlDoc.DocumentNode.SelectSingleNode("//title");
                    if (titleNode != null)
                    {
                        titles.Add(titleNode.InnerText);
                    }
                    else
                    {
                        var headerNode = htmlDoc.DocumentNode.SelectSingleNode("//h1");
                        if (headerNode != null)
                        {
                            titles.Add(headerNode.InnerText);
                        }
                        else
                        {
                            titles.Add("No title found");
                        }
                    }
                }
                else
                {
                    titles.Add("File not found: " + filePath);
                }
            }

            return titles;
        }

        public async Task UpdateXhtmlFileWithTranslation(string filePath, string jsonResult)
        {
            try
            {
                string pattern = @"<([\w/:.-]+)(?![^><]*>)(?=(?:[\r\n\""}\]]|$))";
                string correctedHtml = Regex.Replace(jsonResult, pattern, m => $"<{m.Groups[1].Value}>");
                pattern = @"(</\w+>)(\r\n)?(<\w>)";
                correctedHtml = Regex.Replace(correctedHtml, pattern, m => {return m.Groups[2].Success ? m.Value : $"{m.Groups[1].Value}\r\n{m.Groups[3].Value}";});
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

        public static async Task<string> TranslateTextWithDeepL(string apiKey, string filePath, string targetLangCode)
        {
            string apiUrl = "https://api-free.deepl.com/v2/translate";
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
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Console.WriteLine("Directory does not exist.");
                    return textFiles;
                }
                string[] files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    if (file.EndsWith(".xhtml", StringComparison.OrdinalIgnoreCase) ||
                        file.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                    {
                        textFiles.Add(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }

            return textFiles;
        }

        private void RepackToEpub(string extractPath, string originalEpubFileName)
        {
            string epubFilePath = Path.Combine(Path.GetDirectoryName(extractPath), originalEpubFileName+ comboBox1.SelectedText + ".epub");

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
            UpdateXhtmlFileWithTranslation("s", "/{\"translations\":[{\"detected_source_language\":\"EN\",\"text\":\"<html xmlns=\\\"http://www.w3.org/1999/xhtml\\\" xmlns:epub=\\\"http://www.idpf.org/2007/ops\\\" lang=\\\"en\\\" xml:lang=\\\"en\\\">\\r\\n<head>\\r\\n<title> Розділ 1. Скептицизм: Загальна картина (англійською)</title></title></head> <head> <title>Розділ 1.\\r\\n<meta content=\\\"text/html; charset=utf-8\\\" http-equiv=\\\"default-style\\\"/>\\r\\n<link href=\\\"../styles/stylesheet.css\\\" rel=\\\"stylesheet\\\" type=\\\"text/css\\\" />\\r\\n<meta content=\\\"urn:uuid:04d17cfb-3862-4fbf-9524-906f6b6682ad\\\" name=\\\"Adept.expected.resource\\\"/>\\r\\n</head> </head> </head> </head> </head> </head> </head> <body\\r\\n<body epub:type=\\\"bodymatter\\\">\\r\\n<div class=\\\"body\\\">\\r\\n<p class=\\\"sp\\\"> </p> </p> <p class=\\\"sp\\\"> </p> <p class=\\\"sp\\r\\n<section aria-labelledby=\\\"ch1\\\" epub:type=\\\"chapter\\\" role=\\\"doc-chapter\\\">\\r\\n\\r\\n<h1 class=\\\"CN\\\" id=\\\"ch1\\\"><span aria-label=\\\"1\\\" epub:type=\\\"pagebreak\\\" id=\\\"pg_1\\\" role=\\\"doc-pagebreak\\\" title=\\\"1\\\"/><samp class=\\\"SANS_Futura_Std_Book_11\\\">ГЛАВА 1</samp></h1>\\r\\n<h1 class=\\\"CT\\\"><samp class=\\\"SANS_Futura_Std_Book_11\\\">Скептицизм: Загальна картина</samp></h1></h1></h1\\r\\n\\r\\n<p class=\\\"COS2\\\"><i>Секст дає огляд скептицизму на початку першої книги</i> \\\"Нарисів пірронізму\\\"</i>: за винятком одного незначного пропуску (в розділі [<a href=\\\"12_ch1-g.xhtml#g_7\\\" id=\\\"g-7\\\">7</a>]), я включаю його повністю.</i></p>.\\r\\n<section epub:type=\\\"division\\\">\\r\\n<h2 class=\\\"H1\\\"><samp class=\\\"SANS_Futura_Std_Book_11\\\">Про найголовнішу відмінність між філософіями</samp></h2>\\r\\n<p class=\\\"TNI\\\">[<a href=\\\"12_ch1-g.xhtml#g_1\\\" id=\\\"g-1\\\">1</a>] Припустімо, що ви досліджуєте якусь тему: найімовірніше, в результаті ви або (а) зробите відкриття, або (б) заперечите, що зробили відкриття, і визнаєте, що це питання не можна <a href=\\\"23_glossary.xhtml#gla_5\\\" id=\\\"gla-5\\\">зрозуміти</a>*, або (в) продовжите дослідження. [<a href=\\\"12_ch1-g.xhtml#g_2\\\" id=\\\"g-2\\\">2</a>] Так само, коли йдеться про те, що досліджується у філософії, деякі люди стверджували, що відкрили істину, деякі заявляли, що її неможливо осягнути, а деякі <span aria-label=\\\"3\\\" epub:type=\\\"pagebreak\\\" id=\\\"pg_3\\\" role=\\\"doc-pagebreak\\\" title=\\\"3\\\"/> продовжують досліджувати. [<a href=\\\"12_ch1-g.xhtml#g_3\\\" id=\\\"g-3\\\">3</a>] Це ті, кого суворо називають догматиками*, думають, що вони відкрили її - такі люди, як Аристотель*, Епікур*, стоїки* та деякі інші; це Клітомах*, Карнеадес* та інші академіки* заявили, що вони мають справу з речами, які неможливо осягнути; і це скептики, які все ще досліджують. [<a href=\\\"12_ch1-g.xhtml#g_4\\\" id=\\\"g-4\\\">4</a>] Звідси зрозуміло, що основними філософіями вважаються три: догматична*, академічна та скептична. Про інші доречно буде поговорити іншим; зараз ми поговоримо в загальних рисах про скептичний підхід, з наступною передмовою - що на жодній з речей, які будуть обговорюватися, ми <a href=\\\"23_glossary.xhtml#gla_6\\\" id=\\\"gla-6\\\">не наполягаємо</a>* на тому, що справа однозначно є такою, як ми говоримо, але про кожну з них ми повідомляємо як про конкретний випадок, відповідно до того, як вона нам зараз видається.</p>\\r\\n</section></p> <section\\r\\n<section epub:type=\\\"division\\\">\\r\\n<h2 class=\\\"H1\\\"><span aria-label=\\\"5\\\" epub:type=\\\"pagebreak\\\" id=\\\"pg_5\\\" role=\\\"doc-pagebreak\\\" title=\\\"5\\\"/><samp class=\\\"SANS_Futura_Std_Book_11\\\">На рахунках скептицизму</samp></h2>\\r\\n<p class=\\\"TNI\\\">[<a href=\\\"12_ch1-g.xhtml#g_5\\\" id=\\\"g-5\\\">5</a>] Існує одна версія скептичної філософії, яка називається \\\"загальною\\\", а інша - \\\"конкретною\\\". У загальному викладі ми розкриваємо особливості скептицизму, розповідаємо, як він мислиться, які його вихідні положення та аргументи, його критерій та мета, які способи призупинення судження, як ми використовуємо скептичні твердження та чим скептицизм відрізняється від найближчих до нього філософій; [<a href=\\\"12_ch1-g.xhtml#g_6\\\" id=\\\"g-6\\\">6</a>] у конкретному викладі ми наводимо аргументи проти кожної частини так званої філософії. Що ж, давайте спочатку розберемося із загальним рахунком, почавши наш огляд з назв скептичного підходу.</p>\\r\\n</section></p> <section\\r\\n<section epub:type=\\\"division\\\">\\r\\n<h2 class=\\\"H1\\\"><samp class=\\\"SANS_Futura_Std_Book_11\\\">Про те, як називають скептицизм</samp></h2>\\r\\n<p class=\\\"TNI\\\">[<a href=\\\"12_ch1-g.xhtml#g_7\\\">7</a>] Скептичний підхід, таким чином, називається дослідницьким, від його активності, пов'язаної з розслідуванням і пошуком, і відстороненим від реакції <span aria-label=\\\"7\\\" epub:type=\\\"pagebreak\\\" id=\\\"pg_7\\\" role=\\\"doc-pagebreak\\\" title=\\\"7\\\"/>яка виникає у дізнавача після розслідування... і пірронічним - від того, що Піррон*, як нам здається, вдається до скептицизму більш повно і очевидно, ніж попередники.</p>\\r\\n</section></p> <section\\r\\n<section epub:type=\\\"division\\\">\\r\\n<h2 class=\\\"H1\\\"><samp class=\\\"SANS_Futura_Std_Book_11\\\">Що таке скептицизм</samp></h2>\\r\\n<p class=\\\"TNI\\\">[<a href=\\\"12_ch1-g.xhtml#g_8\\\" id=\\\"g-8\\\">8</a>] Скептична здатність - це така, що породжує протиставлення між речами, які з'являються, і речами, які мисляться будь-яким чином, від чого, через рівну силу в протилежних предметах і розповідях, ми приходимо спочатку до припинення судження, а після цього до спокою*.</p>\\r\\n<p class=\\\"TX\\\">[<a href=\\\"12_ch1-g.xhtml#g_9\\\" id=\\\"g-9\\\">9</a>] Ми називаємо це \\\"здатністю\\\" не в якомусь витонченому сенсі, а просто в сенсі <i>здатності</i>; під \\\"речами, що з'являються\\\" ми розуміємо речі, які сприймаються органами чуття, і тому протиставляємо їм речі, що мисляться. \\\"Будь-яким чином\\\" може бути пов'язане зі здатністю (це означає, що ми <span aria-label=\\\"9\\\" epub:type=\\\"pagebreak\\\" id=\\\"pg_9\\\" role=\\\"doc-pagebreak\\\" title=\\\"9\\\"/>сприймаємо слово \\\"здатність\\\" просто, як ми вже казали), або з \\\"продукуванням опозицій між речами, що з'являються, і речами, що мисляться\\\"; оскільки ми протиставляємо їх у різний спосіб - протиставляємо речі, що з'являються, речам, що з'являються, або речам, що мисляться, речам, що мисляться, або міняємо їх місцями, так, щоб включити всі опозиції, - то ми й кажемо: \\\"будь-яким іншим чином.\\\" Або \\\"будь-яким чином\\\" поєднується з \\\"речами, що з'являються, і речами, що мисляться\\\", що означає, що ми не досліджуємо <i>як</i> речі, що з'являються, з'являються, або речі, що мисляться, мисляться - ми розглядаємо їх у простий спосіб. [<a href=\\\"12_ch1-g.xhtml#g_10\\\" id=\\\"g-10\\\">10</a>] Ми говоримо про \\\"протилежні\\\" свідчення не обов'язково в сенсі твердження і заперечення, а просто замість \\\"конфліктуючі\\\". \\\"Рівна сила\\\" означає рівність з точки зору достовірності або її відсутності, так що жодне з конфліктуючих свідчень не випереджає будь-яке інше як більш достовірне. Призупинення судження - це коли думка зупиняється; через <span aria-label=\\\"11\\\" epub:type=\\\"pagebreak\\\" id=\\\"pg_11\\\" role=\\\"doc-pagebreak\\\" title=\\\"11\\\"/>це ми нічого не заперечуємо і не висуваємо. Спокій - це безтурботний стан, або умиротворення, душі. Про те, як спокій приходить разом з відстороненням від суджень, ми розповімо в наших міркуваннях про мету.<sup><a href=\\\"25_notes.xhtml#chapter1-1\\\" id=\\\"chapter1_1\\\" role=\\\"doc-noteref\\\">1</a></sup></p></p></p>\\r\\n</section></section\\r\\n<section epub:type=\\\"division\\\">\\r\\n<h2 class=\\\"H1\\\"><samp class=\\\"SANS_Futura_Std_Book_11\\\">Про скептика</samp></h2>\\r\\n<p class=\\\"TNI\\\">[<a href=\\\"12_ch1-g.xhtml#g_11\\\" id=\\\"g-11\\\">11</a>] Пірронійський філософ, по суті, вже пояснив концепцію скептичного підходу; це людина, яка має частинку цієї \\\"здатності\\\".\\r\\n</section></p> <section\\r\\n<section epub:type=\\\"division\\\">\\r\\n<h2 class=\\\"H1\\\"><samp class=\\\"SANS_Futura_Std_Book_11\\\">Про відправні точки скептицизму</samp></h2>\\r\\n<p class=\\\"TNI\\\">[<a href=\\\"12_ch1-g.xhtml#g_12\\\" id=\\\"g-12\\\">12</a>] Відправною точкою, яка викликає скептицизм, ми говоримо, є надія отримати спокій. Високообдаровані люди, занепокоєні суперечливістю речей і не знаючи, яким з них більше довіряти, почали досліджувати, що в речах є істинним, а що хибним, припускаючи, що, визначивши ці речі, вони досягнуть спокою. Але <span aria-label=\\\"13\\\" epub:type=\\\"pagebreak\\\" id=\\\"pg_13\\\" role=\\\"doc-pagebreak\\\" title=\\\"13\\\"/>відправною точкою скептичної установки є насамперед те, що кожен аргумент має рівновеликий аргумент, що лежить в опозиції до нього; бо з цього ми, здається, закінчуємо тим, що не маємо доктрин*.</p>\\r\\n</section></section\\r\\n<section epub:type=\\\"division\\\">\\r\\n<h2 class=\\\"H1\\\"><samp class=\\\"SANS_Futura_Std_Book_11\\\">Чи має скептик доктрини</samp></h2>\\r\\n<p class=\\\"TNI\\\">[<a href=\\\"12_ch1-g.xhtml#g_13\\\" id=\\\"g-13\\\">13</a>] Ми говоримо, що скептик не має доктрин не в тому більш повсякденному розумінні \\\"доктрини\\\", в якому дехто каже, що доктрина - це коли ви погоджуєтеся з чимось<sup><a href=\\\"25_notes.xhtml#chapter1-2\\\" id=\\\"chapter1_2\\\" role=\\\"doc-noteref\\\">2</a></sup> - бо скептик погоджується з реакціями, які йому нав'язує зовнішність* (наприклад, коли його зігрівають чи охолоджують, він не скаже: \\\"Я думаю, що мене не зігрівають чи охолоджують\\\"); ми говоримо, що він не має доктрин у тому сенсі, в якому дехто вважає, що доктрина - це згода з якоюсь неясною річчю, досліджуваною науками, - бо пірроніст не погоджується ні з чим неясним. [<a href=\\\"12_ch1-g.xhtml#g_14\\\" id=\\\"g-14\\\">14</a>] У нього немає доктрин навіть у виголошенні скептичних фраз <i>про</i> неясні речі - наприклад, \\\"Не більше\\\"<sup><a href=\\\"25_notes.xhtml#chapter1-3\\\" id=\\\"chapter1_3\\\" role=\\\"doc-noteref\\\">3</a></sup> або \\\"Я нічого не визначаю\\\", <span aria-label=\\\"15\\\" epub:type=\\\"pagebreak\\\" id=\\\"pg_15\\\" role=\\\"doc-pagebreak\\\" title=\\\"15\\\"/>або будь-які інші, про які ми поговоримо пізніше. Бо той, хто має доктрину, висуває як реальність те, про що, як кажуть, він має доктрину, але скептик не висуває ці фрази як певну реальність; він вважає, що подібно до того, як фраза \\\"все хибне\\\" говорить, що вона сама є хибною разом з іншими, і подібно до того, як \\\"ніщо не є істинним\\\", так і \\\"не більше [так, ніж так]\\\" говорить, що разом з іншими вона сама є \\\"не більше\\\", ніж її протилежність, і з цієї причини <br /> <br /> <br /> <br /> <br /> <br /> <br /> <br /> <br />...xhtml#gla_2\\\" id=\\\"gla-2\\\">дужки</a>* себе разом з іншими. Те саме можна сказати і про інші скептичні фрази. [<a href=\\\"12_ch1-g.xhtml#g_15\\\" id=\\\"g-15\\\">15</a>] Але якщо догматик висуває як реальність те, про що у нього є доктрина, а скептик вимовляє свої фрази так, що вони потенційно самі по собі беруться в дужки, то не можна сказати, що він, вимовляючи їх, має доктрини. Але найголовніше те, що, вимовляючи ці фрази, він говорить те, що здається йому самому, і оголошує без <span aria-label=\\\"17\\\" epub:type=\\\"pagebreak\\\" id=\\\"pg_17\\\" role=\\\"doc-pagebreak\\\" title=\\\"17\\\"/>оцінок* те, як він сам на це впливає, не роблячи ніяких твердих тверджень* про об'єкти, що існують насправді.</p>\\r\\n</section></p> <section\\r\\n<section epub:type=\\\"division\\\">\\r\\n<h2 class=\\\"H1\\\"><samp class=\\\"SANS_Futura_Std_Book_11\\\">Чи є у скептика школа мислення</samp></h2>\\r\\n<p class=\\\"TNI\\\">[<a href=\\\"12_ch1-g.xhtml#g_16\\\" id=\\\"g-16\\\">16</a>] Ми йдемо схожим шляхом у питанні, чи має скептик школу думки. Якщо хтось скаже, що школа - це прихильність до багатьох доктрин, які узгоджуються між собою і з очевидними* речами, а під \\\"доктриною\\\" розумітиме згоду з незрозумілим, то ми скажемо, що він не має школи. [<a href=\\\"12_ch1-g.xhtml#g_17\\\" id=\\\"g-17\\\">17</a>] Але якщо хтось скаже, що школа - це підхід, який слідує певному обґрунтуванню відповідно до того, що з'являється, де це обґрунтування вказує, як можна здаватися, що живеш правильно (\\\"правильно\\\" розуміється не тільки в термінах чесноти, але й більш прямолінійно), і поширюється на здатність призупинити судження, то ми скажемо, що у нього є школа, бо ми дійсно слідуємо певному обґрунтуванню, яке, відповідно до того, що з'являється, виділяємо <span aria-label=\\\"19\\\" epubtype=\\\"pagebreak\\\" id=\\\"pg_19\\\" role=\\\"doc-pagebreak\\\" title=\\\"19\\\"/>життя для нас, яке відповідає звичаям предків, законам, культурі та нашим власним реакціям.</p>\\r\\n</section>\\r\\n<section epub:type=\\\"division\\\">\\r\\n<h2 class=\\\"H1\\\"><samp class=\\\"SANS_Futura_Std_Book_11\\\">Чи займається скептик природничими науками</samp></h2>\\r\\n<p class=\\\"TNI\\\">[<a href=\\\"12_ch1-g.xhtml#g_18\\\" id=\\\"g-18\\\">18</a>] Ми говоримо схожі речі щодо питання, чи варто скептику займатися природничими науками. Якщо сенс полягає в тому, щоб робити заяви з твердою впевненістю про будь-які речі, на яких тримаються доктрини в природничих науках, то ми не займаємося природничими науками. Але якщо метою є вміння протиставити кожному аргументу рівний аргумент і досягти спокою, то ми займаємося природничими науками. Так само ми охоплюємо логічну та етичну частини так званої філософії.</p> <p>\\r\\n</section></p> <section\\r\\n<section epub:type=\\\"division\\\">\\r\\n<h2 class=\\\"H1\\\"><span aria-label=\\\"21\\\" epub:type=\\\"pagebreak\\\" id=\\\"pg_21\\\" role=\\\"doc-pagebreak\\\" title=\\\"21\\\"/><samp class=\\\"SANS_Futura_Std_Book_11\\\">Чи справді скептики не помічають очевидних речей</samp></h2>\\r\\n<p class=\\\"TNI\\\">[<a href=\\\"12_ch1-g.xhtml#g_19\\\" id=\\\"g-19\\\">19</a>] Ті, хто каже, що скептики заперечують очевидні речі, мені здається, не слухають нас. Ми не відкидаємо те, що через пасивну зовнішність, хочемо ми того чи ні, змушує нас погодитися - як ми вже говорили раніше; <sup><a href=\\\"25_notes.xhtml#chapter1-4\\\" id=\\\"chapter1_4\\\" role=\\\"doc-noteref\\\">4</a></sup> а це і є очевидні речі. Коли ми досліджуємо, чи є реальний об'єкт <i>таким</i>, яким він видається, ми допускаємо, що він видається, і наше дослідження стосується не видимої речі, а того, що <i>сказано про</i> видиму річ; а це відрізняється від дослідження самої видимої речі. [<a href=\\\"12_ch1-g.xhtml#g_20\\\" id=\\\"g-20\\\">20</a>] Наприклад, мед здається нам солодким; ми погоджуємося з цим, бо в чуттєвому сприйнятті ми відчуваємо солодкість. Але чи дійсно він <i>солодкий</i> з точки зору аргументу,<sup><a href=\\\"25_notes.xhtml#chapter1-5\\\" id=\\\"chapter1_5\\\" role=\\\"doc-noteref\\\">5</a></sup> ми досліджуємо - що є не очевидною річчю, а чимось сказаним про очевидну річ. І навіть якщо ми <span aria-label=\\\"23\\\" epub:type=\\\"pagebreak\\\" id=\\\"pg_23\\\" role=\\\"doc-pagebreak\\\" title=\\\"23\\\"/>висуваємо аргументи проти очевидних речей, то висуваємо їх не для того, щоб покінчити з очевидними речами, а для того, щоб показати необачність догматиків; бо якщо аргумент настільки хитрий, що ледь не вихоплює очевидні речі з-під наших очей, то як ми можемо не підозрювати його в неясних питаннях, а отже, не слідувати йому і не діяти необачно?</p>\\r\\n</section></p> <section\\r\\n<section epub:type=\\\"division\\\">\\r\\n<h2 class=\\\"H1\\\"><samp class=\\\"SANS_Futura_Std_Book_11\\\">Про критерій скептицизму</samp></h2>\\r\\n<p class=\\\"TNI\\\">[<a href=\\\"12_ch1-g.xhtml#g_21\\\" id=\\\"g-21\\\">21</a>] Те, що ми звертаємо увагу на очевидні речі, зрозуміло з того, що ми говоримо про критерій скептичного підходу. Про критерій говорять у двох сенсах: є критерій, який використовується з метою довіри в питанні реальності чи нереальності - і ми поговоримо про це у викладі, який передбачає контраргументи;<sup><a href=\\\"25_notes.xhtml#chapter1-6\\\" id=\\\"chapter1_6\\\" role=\\\"doc-noteref\\\">6</a></sup> і є ще одна для дії - прислухаючись до неї в житті, ми робимо одні речі і не робимо інші, і саме про неї ми зараз говоримо. [<a href=\\\"12_ch1-g.xhtml#g_22\\\" id=\\\"g-22\\\">22</a>] Отже, ми говоримо, що критерієм скептичного <span aria-label=\\\"25\\\" epub:type=\\\"pagebreak\\\" id=\\\"pg_25\\\" role=\\\"doc-pagebreak\\\" title=\\\"25\\\"/>підходу є те, що є очевидним, фактично тут мається на увазі зовнішній вигляд; адже це пов'язано з реакцією, з тим, як на нас впливають, хочемо ми того чи ні, і тому не підлягає дослідженню. Я маю на увазі, що ніхто не буде сперечатися з тим, чи дійсно об'єкт <i>виглядає</i> так чи інакше, але досліджується те, чи є він <i>таким</i>, як він виглядає.</p>\\r\\n<p class=\\\"TX\\\">[<a href=\\\"12_ch1-g.xhtml#g_23\\\" id=\\\"g-23\\\">23</a>] Отже, звертаючи увагу на те, що з'являється, ми живемо без думок згідно з рутиною життя, оскільки не можемо бути повністю бездіяльними. Ця \\\"рутина життя\\\", здається, має чотири аспекти: один пов'язаний з керівництвом природи, другий - з необхідністю того, як на нас впливають, третій - з передачею законів і звичаїв, а четвертий - з навчанням навичкам. [<a href=\\\"12_ch1-g.xhtml#g_24\\\" id=\\\"g-24\\\">24</a>] Природне керівництво - це те, як ми природно сприймаємо і мислимо; необхідність впливів на нас - це те, як голод змушує нас їсти, а спрага - пити; передача законів і звичаїв - це те, як, наскільки це стосується нашого життя,<sup><a href=\\\"25_notes.xhtml#chapter1-7\\\" id=\\\"chapter1_7\\\" role=\\\"doc-noteref\\\">7</a></sup> ми приймаємо побожність як добро і <span aria-label=\\\"27\\\" epub:type=\\\"pagebreak\\\" id=\\\"pg_27\\\" role=\\\"doc-pagebreak\\\" title=\\\"27\\\"/>нечестивість як погане; а навчання навичок - як ми не залишаємося бездіяльними в уміннях, які приймаємо. І все це ми говоримо без оцінок.</p> <p>\\r\\n</section></p> <section\\r\\n<section epub:type=\\\"division\\\">\\r\\n<h2 class=\\\"H1\\\"><samp class=\\\"SANS_Futura_Std_Book_11\\\">Яка мета скептицизму?</samp></h2>\\r\\n<p class=\\\"TNI\\\">[<a href=\\\"12_ch1-g.xhtml#g_25\\\" id=\\\"g-25\\\">25</a>] Після цього, наступним питанням, яке ми розглянемо, буде мета скептичного підходу. Тепер мета - це те, заради чого все робиться або розглядається, хоча сама по собі вона не є нічим; іншими словами, це кінцева точка бажаного. Досі ми говорили, що метою скептика є спокій у питаннях, пов'язаних з думкою, і поміркована реакція на речі, які нам нав'язують. [<a href=\\\"12_ch1-g.xhtml#g_26\\\" id=\\\"g-26\\\">26</a>] Бо хоч він і почав займатися філософією, щоб розбиратися в явищах і розуміти, які з них істинні, а які хибні, щоб досягти спокою, він потрапив у суперечку з рівними за силою сторонами. Оскільки він не міг вирішити її, він призупинив судження. Але коли він призупинив судження, просто <span aria-label=\\\"29\\\" epub:type=\\\"pagebreak\\\" id=\\\"pg_29\\\" role=\\\"doc-pagebreak\\\" title=\\\"29\\\"/>так сталося, що це супроводжувалося спокоєм у питаннях думки.</p>\\r\\n<p class=\\\"TX\\\">[<a href=\\\"12_ch1-g.xhtml#g_27\\\" id=\\\"g-27\\\">27</a>] Для людини, яка дотримується думки, що все за своєю природою добре або погане, весь час неспокійно. Коли те, що вона вважає добрим, недоступне для неї, вона вважає, що її переслідує те, що за своєю природою погане, і переслідує те, що (як вона думає) є добрим; але, отримавши це, вона впадає в ще більший неспокій, тому що збуджується понад розум і міру, і тому що, боячись змін, робить все, щоб не втратити те, що вона вважає добрим. [<a href=\\\"12_ch1-g.xhtml#g_28\\\" id=\\\"g-28\\\">28</a>] А людина, яка не визначилася з тим, що добре чи погано за своєю природою, ні від чого не тікає і ні за чим не гониться, і тому має спокій.</p>\\r\\n<p class=\\\"TX\\\">Те, що сталося зі скептиком, - це те саме, що розповідають про художника Апеллеса. Кажуть, що він малював коня і хотів зобразити на картині кінську піну; але це було <span aria-label=\\\"31\\\" epub:type=\\\"pagebreak\\\" id=\\\"pg_31\\\" role=\\\"doc-pagebreak\\\" title=\\\"31\\\"/>такою невдачею, що він здався і жбурнув у картину губку, якою витирав фарби з пензля, - яка, влучивши в неї, створила зображення кінської піни. [<a href=\\\"12_ch1-g.xhtml#g_29\\\" id=\\\"g-29\\\">29</a>] Бачиш, скептики теж сподівалися досягти спокою, винісши рішення про невідповідність між тим, що видно, і тим, що мислиться; але, не зумівши цього зробити, вони призупинили судження. Але коли вони призупинили судження, це сталося так, що спокій супроводжував це, як тінь супроводжує тіло.</p> <p class=\\\"TX\\\" />.\\r\\n<p class=\\\"TX\\\">Не можна сказати, що ми вважаємо скептика абсолютно безтурботним - ми говоримо, що його турбують речі, які йому нав'язують; адже ми згодні з тим, що іноді йому холодно, хочеться пити і він страждає від інших подібних речей. [<a href=\\\"12_ch1-g.xhtml#g_30\\\" id=\\\"g-30\\\">30</a>] Але навіть у цих випадках звичайних людей пригнічує пара умов: самі реакції і, що не менш важливо, переконання, що ці умови погані від природи. Скептик, з іншого боку, позбувається <span aria-label=\\\"33\\\" epub:type=\\\"pagebreak\\\" id=\\\"pg_33\\\" role=\\\"doc-pagebreak\\\" title=\\\"33\\\"/>додаткової думки, що кожна з цих речей погана за своєю природою, і тому виходить більш поміркованим навіть у цих випадках. Тому ми говоримо, що метою скептика є спокій у питаннях думки і поміркована реакція на речі, які нам нав'язують. Деякі поважні скептики додають до цього \\\"призупинення суджень під час розслідувань\\\".\\r\\n<p class=\\\"STNI4\\\"><i>Секст також робить деякі загальні зауваження щодо скептицизму на початку</i> \\\"Проти тих, хто займається дисциплінами\\\" (<i>M</i> I-VI)</i>, своєї праці про спеціальні науки (граматику, риторику, геометрію, арифметику, астрологію та музику). Хоча вони набагато менш докладні і присвячені його трактуванню цих \\\"дисциплін\\\", вони в цілому відповідають картині в <i>Нарисах</i>; але є деякі цікаві відмінності в тональності і акцентах.</i></p>.\\r\\n<p class=\\\"TX\\\">[<a href=\\\"12_ch1-g.xhtml#g_1-1\\\" id=\\\"g-1-1\\\">1</a>] Контраргумент проти тих, що в дисциплінах, здається, досить широко використовувався як епікурейцями, так і <span aria-label=\\\"35\\\" epub:type=\\\"pagebreak\\\" id=\\\"pg_35\\\" role=\\\"doc-pagebreak\\\" title=\\\"35\\\"/>пірроністами, хоча їхні підходи не були однаковими. Епікурейці виходили з того, що дисципліни нічого не дають для досягнення мудрості - або, як дехто припускає, вважаючи, що цим вони прикривають власну неосвіченість (адже Епікур винен у тому, що багато в чому не вчився; навіть у звичайній розмові його мова нечиста), [<a href=\\\"12_ch1-g.xhtml#g_2-1\\\" id=\\\"g-2-1\\\">2</a>] і, можливо, також через ворожість до Платона,* Аристотеля та інших, які мали широку освіченість.... [<a href=\\\"12_ch1-g.xhtml#g_5-1\\\" id=\\\"g-5-1\\\">5</a>] Ну, це більш-менш те, звідки виходив Епікур, ризикну припустити, коли вважав за потрібне воювати з дисциплінами. Але пірроністи робили це не тому, що вони нічого не додають до мудрості - бо це догматичне твердження - і не тому, що їм бракує освіти; бо крім того, що вони освічені і мають більший досвід, ніж інші філософи, вони ще й байдужі до думки натовпу. [<a href=\\\"12_ch1-g.xhtml#g_6-1\\\" id=\\\"g-6-1\\\">6</a>] Не те, щоб це було пов'язано з ворожістю до когось (вада <span aria-label=\\\"37\\\" epub:type=\\\"pagebreak\\\" id=\\\"pg_37\\\" role=\\\"doc-pagebreak\\\" title=\\\"37\\\"/>такого роду далека від їхньої м'якості); але у випадку з дисциплінами з ними сталося те саме, що й у випадку з філософією в цілому. Бо так само, як вони взялися за неї з бажанням досягти істини, але, натрапивши на суперечності однакової сили і суперечливості в предметах, припинили судження, так і у випадку дисциплін вони взялися за них, прагнучи пізнати істину, але, виявивши однакові глухі кути, не стали їх приховувати. [<a href=\\\"12_ch1-g.xhtml#g_7-1\\\" id=\\\"g-7-1\\\">7</a>] З цієї причини ми теж будемо дотримуватися того ж підходу, що й вони, і спробуємо без суперечок вибрати і викласти ефективні речі, сказані проти дисциплін.<sup><a href=\\\"25_notes.xhtml#chapter1-8\\\" id=\\\"chapter1_8\\\" role=\\\"doc-noteref\\\">8</a></sup></p>\\r\\n</section>\\r\\n</section>\\r\\n</div> </div> </div> </div> </div> </div\\r\\n</body> </div> </html\\r\\n</html> </body> </html\"}]}");
        }
    }
}
