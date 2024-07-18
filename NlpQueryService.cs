using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class NlpQueryService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const int MaxRetries = 5;

    public NlpQueryService(string apiKey)
    {
        _httpClient = new HttpClient();
        _apiKey = apiKey;
    }

    public async Task<string> QueryModelAsync(string prompt)
    {
        var requestBody = new
        {
            model = "gpt-4",  // Use the latest chat model
            messages = new[]
            {
                new { role = "system", content = "You are a helpful assistant." },
                new { role = "user", content = prompt }
            },
            max_tokens = 150
        };

        var requestContent = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", requestContent);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                var responseObject = JObject.Parse(responseString);
                return responseObject["choices"][0]["message"]["content"].ToString().Trim();
            }
            catch (HttpRequestException ex) when ((int)ex.StatusCode == 429)
            {
                Console.WriteLine($"Rate limit exceeded, retrying in {Math.Pow(2, attempt)} seconds...");
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
            }
        }

        throw new Exception("Exceeded maximum retry attempts due to rate limiting.");
    }
}
