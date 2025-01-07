using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Json;

public static class DeepLTranslation
{
    public static async Task<string> TranslateTextWithDeepL(string apiKey, string apiUrl, string targetLangCode, string textToTranslate)
    {
        using (HttpClient httpClient = new HttpClient())
        {
            try
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"DeepL-Auth-Key {apiKey}");

                var content = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("text", textToTranslate),
                new KeyValuePair<string, string>("target_lang", targetLangCode)});

                HttpResponseMessage response = await httpClient.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    return jsonResponse;
                }
                else
                    return $"API Request failed: {response.StatusCode}";
            }
            catch (Exception ex)
            {
                return $"Error sending request: {ex.Message}";
            }
        }
    }

    public static async Task<string> TranslateFileWithDeepL(string apiKey, string filePath, string targetLangCode, string apiUrl, bool advancedTranslation = false, string apiKeyA = null)
    {
        using (HttpClient httpClient = new HttpClient())
        {
            string originalExtension = Path.GetExtension(filePath).ToLower();
            string newFilePath = filePath;

            if (originalExtension == ".xhtml" || originalExtension == ".xml")
            {
                string directory = Path.GetDirectoryName(filePath);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
                newFilePath = Path.Combine(directory, fileNameWithoutExtension + ".html");
                File.Copy(filePath, newFilePath, true);
                filePath = newFilePath;
            }

            httpClient.DefaultRequestHeaders.Add("Authorization", $"DeepL-Auth-Key {apiKey}");

            var content = new MultipartFormDataContent();
            content.Add(new StringContent(targetLangCode), "target_lang");

            var fileStream = File.OpenRead(filePath);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "file", Path.GetFileName(filePath));

            try
            {
                HttpResponseMessage response = await httpClient.PostAsync(apiUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    if (filePath != newFilePath)
                        File.Delete(newFilePath);
                    string originalText = await response.Content.ReadAsStringAsync();
                    string translation = await WaitForFileTranslationCompletion(originalText, apiKey, apiUrl);

                    if (advancedTranslation)
                        translation = await AdvancedTranslation(originalText, translation, apiKeyA);

                    return translation;
                }
                else
                    return $"API Request failed: {response.StatusCode} - {response.ReasonPhrase}";

            }
            catch (Exception ex)
            {
                return $"Error sending request: {ex.Message}";
            }
            finally
            {
                fileStream.Close();
                if (filePath != newFilePath)
                    File.Delete(newFilePath);
            }
        }
    }

    public static async Task<string> WaitForFileTranslationCompletion(string resultOfTranslateFileWithDeepL, string apiKey, string apiUrl)
    {
        var responseObj = JObject.Parse(resultOfTranslateFileWithDeepL);

        string documentId = (string)responseObj["document_id"];
        string documentKey = (string)responseObj["document_key"];


        using (HttpClient httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Add("Authorization", $"DeepL-Auth-Key {apiKey}");

            var content = new FormUrlEncodedContent(new[] {new KeyValuePair<string, string>("document_key", documentKey)});

            TimeSpan pollingInterval = TimeSpan.FromSeconds(2);
            while (true)
            {
                HttpResponseMessage response = await httpClient.PostAsync(apiUrl + "/" + documentId, content);
                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    var status = JsonConvert.DeserializeObject<dynamic>(result);
                    if (status.status == "done")
                        return await GetFileTranslation(resultOfTranslateFileWithDeepL, apiKey, apiUrl);
                }
                else
                    return $"API Request failed: {response.StatusCode} - {response.ReasonPhrase}";
                await Task.Delay(pollingInterval);
            }
        }
    }

    public static async Task<string> GetFileTranslation(string resultOfTranslateFileWithDeepL, string apiKey, string apiUrl)
    {
        var responseObj = JObject.Parse(resultOfTranslateFileWithDeepL);

        string documentId = (string)responseObj["document_id"];
        string documentKey = (string)responseObj["document_key"];


        using (HttpClient httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Add("Authorization", $"DeepL-Auth-Key {apiKey}");

            var content = new FormUrlEncodedContent(new[]{new KeyValuePair<string, string>("document_key", documentKey)});

            HttpResponseMessage response = await httpClient.PostAsync(apiUrl + "/" + documentId + "/result", content);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsStringAsync();
            else
                return $"API Request failed: {response.StatusCode} - {response.ReasonPhrase}";
        }
    }

    public static async Task<string> AdvancedTranslation(string originalText, string translatedText, string apiKey)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var requestBody = new
            {
                model = "gpt-4-turbo",
                messages = new[]
                {
                    new { role = "system", content = "You are a bilingual expert in English and Ukrainian." },
                    new { role = "user", content = $"Here is an original English text and its translation in Ukrainian. Identify and fix any errors in the translation while maintaining the meaning and tone of the original.\n\nOriginal:" +
                    $"\n{originalText}\n\nTranslation:\n{translatedText}\n\nPlease provide the corrected translation." }
                }
            };

            string json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (response.IsSuccessStatusCode)
            {
                string responseJson = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(responseJson);
                return jsonDoc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            }
            else
            {
                string ex = await response.Content.ReadAsStringAsync();
                MessageBox.Show($"An error occurred on \"AdvancedTranslation\": {ex}", "Error!", MessageBoxButtons.OK);
                return null;
            }
        }
    }
}
