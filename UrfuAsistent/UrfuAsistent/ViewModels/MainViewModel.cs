using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Navigation;
using ConsoleApp8.Configuration;
using ConsoleApp8.Services;
using Microsoft.Win32;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace ConsoleApp8.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly ApiService _apiService;
    private readonly ConfigurationService _configService;
    private readonly HttpClient _httpClient;

    private string _prompt = "";
    private string _response = "";
    private string _apiUrl = "";
    private bool _isLoading;

    // SSR Related properties
    private bool _isSsrLoaded;
    private string _ssrHtml = "";

    // Voice transcription related properties
    private string _selectedAudioFile = "";
    private string _transcription = "";
    private string _voiceResponse = "";
    private bool _transcribeOnly = true;

    public string Prompt
    {
        get => _prompt;
        set => SetField(ref _prompt, value);
    }

    public string Response
    {
        get => _response;
        set => SetField(ref _response, value);
    }

    public string ApiUrl
    {
        get => _apiUrl;
        set => SetField(ref _apiUrl, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetField(ref _isLoading, value);
    }

    // SSR Properties
    public bool IsSsrLoaded
    {
        get => _isSsrLoaded;
        set => SetField(ref _isSsrLoaded, value);
    }

    public string SsrHtml
    {
        get => _ssrHtml;
        set => SetField(ref _ssrHtml, value);
    }

    // Voice properties
    public string SelectedAudioFile
    {
        get => _selectedAudioFile;
        set => SetField(ref _selectedAudioFile, value);
    }

    public string Transcription
    {
        get => _transcription;
        set => SetField(ref _transcription, value);
    }

    public string VoiceResponse
    {
        get => _voiceResponse;
        set => SetField(ref _voiceResponse, value);
    }

    public bool TranscribeOnly
    {
        get => _transcribeOnly;
        set
        {
            SetField(ref _transcribeOnly, value);
            OnPropertyChanged(nameof(TranscribeAndRespond));
        }
    }

    public bool TranscribeAndRespond
    {
        get => !_transcribeOnly;
        set
        {
            SetField(ref _transcribeOnly, !value);
            OnPropertyChanged(nameof(TranscribeOnly));
        }
    }

    // Commands
    public ICommand SendPromptCommand { get; }
    public ICommand SendSsrPromptCommand { get; }
    public ICommand UpdateApiUrlCommand { get; }
    public ICommand ClearPromptCommand { get; }
    public ICommand BrowseAudioFileCommand { get; }
    public ICommand ProcessAudioCommand { get; }

    public MainViewModel(ApiService apiService, ConfigurationService configService)
    {
        _apiService = apiService;
        _configService = configService;
        _httpClient = new HttpClient();

        ApiUrl = _configService.GetApiSettings().BaseUrl;

        // Initialize commands
        SendPromptCommand = new RelayCommand(async _ => await SendPrompt(), _ => !string.IsNullOrWhiteSpace(Prompt) && !IsLoading);
        SendSsrPromptCommand = new RelayCommand(async _ => await SendSsrPrompt(), _ => !string.IsNullOrWhiteSpace(Prompt) && !IsLoading);
        UpdateApiUrlCommand = new RelayCommand(_ => UpdateApiUrl(), _ => !string.IsNullOrWhiteSpace(ApiUrl) && !IsLoading);
        ClearPromptCommand = new RelayCommand(_ => ClearPrompt(), _ => !IsLoading);
        BrowseAudioFileCommand = new RelayCommand(_ => BrowseAudioFile());
        ProcessAudioCommand = new RelayCommand(async _ => await ProcessAudio(), _ => !string.IsNullOrEmpty(SelectedAudioFile) && !IsLoading);
    }

    private async Task SendPrompt()
    {
        if (string.IsNullOrWhiteSpace(Prompt))
        {
            Response = "Please enter a question first.";
            return;
        }

        try
        {
            IsLoading = true;
            Response = "Waiting for response...";

            var result = await _apiService.AskQuestionAsync(Prompt);
            Response = result;
        }
        catch (Exception ex)
        {
            Response = $"An unexpected error occurred: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SendSsrPrompt()
    {
        if (string.IsNullOrWhiteSpace(Prompt))
        {
            Response = "Please enter a question first.";
            return;
        }

        try
        {
            IsLoading = true;
            IsSsrLoaded = false;

            // Call the streaming API endpoint
            var requestUri = $"{ApiUrl}/api/ask/stream";
            var content = new StringContent($"{{\"question\": \"{Prompt}\"}}", Encoding.UTF8, "application/json");

            // Get an event from the server sent events (SSE) endpoint
            SsrHtml = await GetServerSentEventsContentAsync(requestUri, content);
            IsSsrLoaded = true;
        }
        catch (Exception ex)
        {
            Response = $"An SSR error occurred: {ex.Message}";
            IsSsrLoaded = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<string> GetServerSentEventsContentAsync(string requestUri, StringContent content)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = content
        };

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var stringBuilder = new StringBuilder();
        string? line;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            // Parse SSE format
            if (line.StartsWith("data: "))
            {
                var data = line.Substring("data: ".Length);
                if (!string.IsNullOrEmpty(data))
                {
                    stringBuilder.Append(data);
                }
            }
            else if (line.StartsWith("event: done"))
            {
                // Stream completed
                break;
            }
            else if (line.StartsWith("event: error"))
            {
                // Get the error message from the next line
                line = await reader.ReadLineAsync();
                if (line != null && line.StartsWith("data: "))
                {
                    throw new Exception(line.Substring("data: ".Length));
                }
                throw new Exception("Unknown SSE error");
            }
        }

        return stringBuilder.ToString();
    }

    private void BrowseAudioFile()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Audio Files (*.wav;*.mp3)|*.wav;*.mp3|All Files (*.*)|*.*",
            Title = "Select an Audio File"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            SelectedAudioFile = openFileDialog.FileName;
        }
    }

    private async Task ProcessAudio()
    {
        if (string.IsNullOrEmpty(SelectedAudioFile))
        {
            VoiceResponse = "Please select an audio file first.";
            return;
        }

        try
        {
            IsLoading = true;

            // Clear previous results
            Transcription = "Processing audio...";
            VoiceResponse = "Waiting for response...";

            if (TranscribeOnly)
            {
                // Send to transcription endpoint
                await TranscribeAudio();
            }
            else
            {
                // Send to transcribe and respond endpoint
                await TranscribeAndGetResponse();
            }
        }
        catch (Exception ex)
        {
            Transcription = "Error during transcription.";
            VoiceResponse = $"An error occurred: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task TranscribeAudio()
    {
        var requestUri = $"{ApiUrl}/api/transcribe";
        var fileBytes = File.ReadAllBytes(SelectedAudioFile);

        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(fileBytes);

        content.Add(fileContent, "file", Path.GetFileName(SelectedAudioFile));

        var response = await _httpClient.PostAsync(requestUri, content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        // Response format: {"text": "transcribed text"}

        // This is a simple implementation - for production, use a JSON library to parse
        var startIndex = responseJson.IndexOf("\"text\":", StringComparison.Ordinal) + "\"text\":".Length;
        var endIndex = responseJson.LastIndexOf('}');

        if (startIndex > 0 && endIndex > startIndex)
        {
            var jsonValue = responseJson.Substring(startIndex, endIndex - startIndex).Trim();
            if (jsonValue.StartsWith("\"") && jsonValue.EndsWith("\""))
            {
                jsonValue = jsonValue.Substring(1, jsonValue.Length - 2);
            }
            Transcription = jsonValue;
            VoiceResponse = "Audio transcription complete.";
        }
        else
        {
            Transcription = "Error parsing transcription response.";
        }
    }

    private async Task TranscribeAndGetResponse()
    {
        var requestUri = $"{ApiUrl}/api/speech-to-answer";
        var fileBytes = File.ReadAllBytes(SelectedAudioFile);

        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(fileBytes);

        content.Add(fileContent, "file", Path.GetFileName(SelectedAudioFile));

        var response = await _httpClient.PostAsync(requestUri, content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        // Response format: {"transcription": "...", "answer": "..."}

        // This is a simple implementation - for production, use a JSON library to parse
        var transcriptionStart = responseJson.IndexOf("\"transcription\":", StringComparison.Ordinal) + "\"transcription\":".Length;
        var transcriptionEnd = responseJson.IndexOf(",\"answer\":", StringComparison.Ordinal);

        var answerStart = responseJson.IndexOf("\"answer\":", StringComparison.Ordinal) + "\"answer\":".Length;
        var answerEnd = responseJson.LastIndexOf('}');

        if (transcriptionStart > 0 && transcriptionEnd > transcriptionStart &&
            answerStart > 0 && answerEnd > answerStart)
        {
            var transcriptionJson = responseJson.Substring(transcriptionStart, transcriptionEnd - transcriptionStart).Trim();
            var answerJson = responseJson.Substring(answerStart, answerEnd - answerStart).Trim();

            if (transcriptionJson.StartsWith("\"") && transcriptionJson.EndsWith("\""))
            {
                transcriptionJson = transcriptionJson.Substring(1, transcriptionJson.Length - 2);
            }

            if (answerJson.StartsWith("\"") && answerJson.EndsWith("\""))
            {
                answerJson = answerJson.Substring(1, answerJson.Length - 2);
            }

            Transcription = transcriptionJson;
            VoiceResponse = answerJson;
        }
        else
        {
            Transcription = "Error parsing response.";
            VoiceResponse = "Could not retrieve answer from server.";
        }
    }

    private void UpdateApiUrl()
    {
        _configService.UpdateApiUrl(ApiUrl);
        Response = $"API URL updated to: {ApiUrl}";
    }

    private void ClearPrompt()
    {
        Prompt = "";
        Response = "";
    }
}