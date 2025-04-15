using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ConsoleApp8.ApiModels;
using ConsoleApp8.Configuration;
using Newtonsoft.Json;

namespace ConsoleApp8.Services;

public class ApiService
{
    private readonly HttpClient _client;
    private readonly ConfigurationService _configService;

    public ApiService(ConfigurationService configService)
    {
        _configService = configService;
        _client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };
    }

    public async Task<string> AskQuestionAsync(string question)
    {
        try
        {
            var settings = _configService.GetApiSettings();

            if (string.IsNullOrEmpty(settings.BaseUrl))
            {
                return "Error: API URL is not configured. Please set the API URL in settings.";
            }

            var request = new QuestionRequest { Question = question };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"{settings.BaseUrl}/api/ask", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var answerResponse = JsonConvert.DeserializeObject<AnswerResponse>(responseJson);

            return answerResponse?.Answer ?? "No response received.";
        }
        catch (HttpRequestException ex)
        {
            return $"API connection error: {ex.Message}\n\nPlease verify the API URL and ensure the server is running.";
        }
        catch (TaskCanceledException)
        {
            return "Request timed out. The server took too long to respond.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}