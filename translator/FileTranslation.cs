using System;

public static class FileTranslation
{
    public static async Task<string> TranslateFileWithDeepL(string apiKey, string filePath, string targetLangCode, string apiUrl)
    {
        using (HttpClient httpClient = new HttpClient())
        {
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
                    return await WaitForFileTranslationCompletion(await response.Content.ReadAsStringAsync(), apiKey, apiUrl);
                }
                else
                {
                    return $"API Request failed: {response.StatusCode} - {response.ReasonPhrase}";
                }
            }
            catch (Exception ex)
            {
                return $"Error sending request: {ex.Message}";
            }
            finally
            {
                fileStream.Close();
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

            var content = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string, string>("document_key", documentKey)
        });

            TimeSpan pollingInterval = TimeSpan.FromSeconds(2);
            while (true)
            {
                HttpResponseMessage response = await httpClient.PostAsync(apiUrl + "/" + documentId, content);
                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    var status = JsonConvert.DeserializeObject<dynamic>(result);
                    if (status.status == "done")
                    {
                        return await GetFileTranslation(resultOfTranslateFileWithDeepL, apiKey, apiUrl);
                    }
                }
                else
                {
                    return $"API Request failed: {response.StatusCode} - {response.ReasonPhrase}";
                }
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

            var content = new FormUrlEncodedContent(new[]
            {
                    new KeyValuePair<string, string>("document_key", documentKey)
                });

            HttpResponseMessage response = await httpClient.PostAsync(apiUrl + "/" + documentId + "/result", content);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                return $"API Request failed: {response.StatusCode} - {response.ReasonPhrase}";
            }
        }
    }
}
