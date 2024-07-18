using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class NlpQueryService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public NlpQueryService(string apiKey)
    {
        _httpClient = new HttpClient();
        _apiKey = apiKey;
    }

    public async Task<string> QueryModelAsync(string prompt)
    {
        var requestBody = new
        {
            model = "text-davinci-003",  // Specify the model you want to use
            prompt = prompt,
            max_tokens = 150
        };

        var requestContent = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/completions", requestContent);
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();
        var responseObject = JObject.Parse(responseString);
        return responseObject["choices"][0]["text"].ToString().Trim();
    }
}
