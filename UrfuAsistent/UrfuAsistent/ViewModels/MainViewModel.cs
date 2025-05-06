using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows.Input;
using ConsoleApp8.Configuration;
using ConsoleApp8.Services;
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
    private bool _isSsrLoaded;
    private string _ssrHtml = "";
    private string _selectedAudioFile = "";
    private string _transcription = "";
    private string _voiceResponse = "";
    private bool _transcribeOnly = true;
    private bool _isRecording = false;
    private bool _isRecordingComplete = false;
    private bool _showTranscription = false;
    private bool _showVoiceResponse = false;
    private string _recordedAudioPath = "";
    private bool _useStreaming = true;
    private bool _useImmediateResponse = false;
    private int _voiceMode = 0;
    private bool _isStreamingActive = false;
    private bool _isSimpleResponse = true;
    private NAudio.Wave.WaveInEvent _waveIn;
    private NAudio.Wave.WaveFileWriter _waveWriter;
    private string _tempWavFile;
    private bool _isStreamLoading;

    public bool IsStreamLoading
    {
        get => _isStreamLoading;
        set => SetField(ref _isStreamLoading, value);
    }

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

    public bool IsRecording
    {
        get => _isRecording;
        set => SetField(ref _isRecording, value);
    }

    public bool IsRecordingComplete
    {
        get => _isRecordingComplete;
        set => SetField(ref _isRecordingComplete, value);
    }

    public bool ShowTranscription
    {
        get => _showTranscription;
        set => SetField(ref _showTranscription, value);
    }

    public bool ShowVoiceResponse
    {
        get => _showVoiceResponse;
        set => SetField(ref _showVoiceResponse, value);
    }

    public string RecordedAudioPath
    {
        get => _recordedAudioPath;
        set => SetField(ref _recordedAudioPath, value);
    }

    public bool UseStreaming
    {
        get => _useStreaming;
        set
        {
            SetField(ref _useStreaming, value);
            if (value)
            {
                UseImmediateResponse = false;
            }
            OnPropertyChanged(nameof(UseImmediateResponse));
        }
    }

    public bool UseImmediateResponse
    {
        get => _useImmediateResponse;
        set
        {
            SetField(ref _useImmediateResponse, value);
            if (value)
            {
                UseStreaming = false;
            }
            OnPropertyChanged(nameof(UseStreaming));
        }
    }

    public int VoiceMode
    {
        get => _voiceMode;
        set => SetField(ref _voiceMode, value);
    }

    public bool IsStreamingActive
    {
        get => _isStreamingActive;
        set
        {
            SetField(ref _isStreamingActive, value);
            if (value)
            {
                IsSimpleResponse = false;
            }
            else
            {
                IsSimpleResponse = true;
            }
        }
    }

    public bool IsSimpleResponse
    {
        get => _isSimpleResponse;
        set => SetField(ref _isSimpleResponse, value);
    }

    public ICommand SendPromptCommand { get; }
    public ICommand SendSsrPromptCommand { get; }
    public ICommand UpdateApiUrlCommand { get; }
    public ICommand ClearPromptCommand { get; }
    public ICommand StartRecordingCommand { get; }
    public ICommand StopRecordingCommand { get; }
    public ICommand ProcessVoiceCommand { get; }

    public MainViewModel(ApiService apiService, ConfigurationService configService)
    {
        _apiService = apiService;
        _configService = configService;
        _httpClient = new HttpClient();

        ApiUrl = _configService.GetApiSettings().BaseUrl;

        SendPromptCommand = new RelayCommand(async _ => await SendPrompt(), _ => !string.IsNullOrWhiteSpace(Prompt) && !IsLoading);
        SendSsrPromptCommand = new RelayCommand(async _ => await SendSsrPrompt(), _ => !string.IsNullOrWhiteSpace(Prompt) && !IsLoading);
        UpdateApiUrlCommand = new RelayCommand(_ => UpdateApiUrl(), _ => !string.IsNullOrWhiteSpace(ApiUrl) && !IsLoading);
        ClearPromptCommand = new RelayCommand(_ => ClearPrompt(), _ => !IsLoading);
        ProcessVoiceCommand = new RelayCommand(async _ => await ProcessVoice(), _ => !string.IsNullOrEmpty(RecordedAudioPath) && !IsLoading);

        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "UrfuAssistant"));
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
            if (UseStreaming)
            {
                IsStreamLoading = true;
                
                Response = "Получаю ответ...";
                
                var requestUri = $"{ApiUrl}/api/ask/stream";
                var content = new StringContent($"{{\"question\": \"{Prompt}\"}}", Encoding.UTF8, "application/json");
                
                await GetServerSentEventsContentAsync(requestUri, content);
            }
            else
            {
                IsLoading = true;
                Response = "Waiting for response...";
                
                var result = await _apiService.AskQuestionAsync(Prompt);
                Response = result;
            }
        }
        catch (Exception ex)
        {
            Response = $"An unexpected error occurred: {ex.Message}";
        }
        finally
        {
            if (UseStreaming)
            {
                IsStreamLoading = false;
            }
            else 
            {
                IsLoading = false;
            }
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

            var requestUri = $"{ApiUrl}/api/ask/stream";
            var content = new StringContent($"{{\"question\": \"{Prompt}\"}}", Encoding.UTF8, "application/json");

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

        IsStreamLoading = true;
        Response = "Получаю ответ...";

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var fullResponse = new StringBuilder();
        string? line;
        bool firstDataReceived = false;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (line.StartsWith("data: "))
            {
                var data = line.Substring("data: ".Length);
                if (!string.IsNullOrEmpty(data))
                {
                    fullResponse.Append(data);
                    
                    if (!firstDataReceived)
                    {
                        firstDataReceived = true;
                        IsStreamLoading = false;
                    }
                    
                    Response = fullResponse.ToString();
                }
            }
            else if (line.StartsWith("event: done"))
            {
                break;
            }
            else if (line.StartsWith("event: error"))
            {
                line = await reader.ReadLineAsync();
                if (line != null && line.StartsWith("data: "))
                {
                    throw new Exception(line.Substring("data: ".Length));
                }
                throw new Exception("Unknown SSE error");
            }
        }
        
        IsStreamLoading = false;
        
        return fullResponse.ToString();
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

            Transcription = "Processing audio...";
            VoiceResponse = "Waiting for response...";

            if (TranscribeOnly)
            {
                await TranscribeAudio();
            }
            else
            {
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

    private async Task TranscribeVoiceAndGetDirectResponse()
    {
        var requestUri = $"{ApiUrl}/api/speech-to-answer";
        var fileBytes = File.ReadAllBytes(RecordedAudioPath);

        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(fileBytes);

        content.Add(fileContent, "file", Path.GetFileName(RecordedAudioPath));

        Response = "Обрабатываю голосовой запрос...";
        
        try
        {
            var httpResponse = await _httpClient.PostAsync(requestUri, content);
            httpResponse.EnsureSuccessStatusCode();

            var responseJson = await httpResponse.Content.ReadAsStringAsync();

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
                ShowTranscription = true;
                
                Response = answerJson;
                
                IsStreamingActive = false;
                IsSimpleResponse = true;
            }
            else
            {
                Transcription = "Error parsing response.";
                Response = "Не удалось получить ответ от сервера.";
                IsStreamingActive = false;
                IsSimpleResponse = true;
            }
        }
        catch (Exception ex)
        {
            Response = $"Ошибка при обработке голосового запроса: {ex.Message}";
            IsStreamingActive = false;
            IsSimpleResponse = true;
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

    public void StartRecording()
    {
        try
        {
            IsRecording = true;
            IsRecordingComplete = false;
            ShowTranscription = false;
            ShowVoiceResponse = false;
            Transcription = "";
            VoiceResponse = "";

            var tempDir = Path.Combine(Path.GetTempPath(), "UrfuAssistant");
            _tempWavFile = Path.Combine(tempDir, $"recording_{DateTime.Now:yyyyMMddHHmmss}.wav");

            _waveIn = new NAudio.Wave.WaveInEvent
            {
                DeviceNumber = 0,
                WaveFormat = new NAudio.Wave.WaveFormat(44100, 1)
            };

            _waveWriter = new NAudio.Wave.WaveFileWriter(_tempWavFile, _waveIn.WaveFormat);
            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;

            _waveIn.StartRecording();
        }
        catch (Exception ex)
        {
            IsRecording = false;
            VoiceResponse = $"Recording error: {ex.Message}";
        }
    }

    private void OnDataAvailable(object sender, NAudio.Wave.WaveInEventArgs e)
    {
        if (_waveWriter != null)
        {
            _waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
        }
    }

    private void OnRecordingStopped(object sender, NAudio.Wave.StoppedEventArgs e)
    {
        if (_waveWriter != null)
        {
            _waveWriter.Dispose();
            _waveWriter = null;
        }

        if (_waveIn != null)
        {
            _waveIn.Dispose();
            _waveIn = null;
        }

        RecordedAudioPath = _tempWavFile;
        IsRecordingComplete = true;
    }

    public void StopRecording()
    {
        if (IsRecording && _waveIn != null)
        {
            IsRecording = false;
            _waveIn.StopRecording();
        }
    }

    public async Task ProcessVoice()
    {
        if (string.IsNullOrEmpty(RecordedAudioPath) || !File.Exists(RecordedAudioPath))
        {
            Response = "Нет аудиозаписи для обработки.";
            return;
        }
        
        try
        {
            IsLoading = true;
            
            if (VoiceMode == 0)
            {
                await TranscribeVoiceToPrompt();
            }
            else
            {
                if (UseStreaming)
                {
                    await TranscribeVoiceAndStreamResponse();
                }
                else
                {
                    await TranscribeVoiceAndGetDirectResponse();
                }
            }
        }
        catch (Exception ex)
        {
            Transcription = "Ошибка при обработке голоса.";
            Response = $"Произошла ошибка: {ex.Message}";
            IsStreamingActive = false;
            IsSimpleResponse = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task TranscribeVoiceToPrompt()
    {
        var requestUri = $"{ApiUrl}/api/transcribe";
        var fileBytes = File.ReadAllBytes(RecordedAudioPath);

        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(fileBytes);

        content.Add(fileContent, "file", Path.GetFileName(RecordedAudioPath));

        var response = await _httpClient.PostAsync(requestUri, content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();

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
            Prompt = jsonValue;
            VoiceResponse = "Audio transcription complete. You can edit the text before sending.";
        }
        else
        {
            Transcription = "Error parsing transcription response.";
        }
    }

    private async Task TranscribeVoiceAndGetResponse()
    {
        var requestUri = $"{ApiUrl}/api/speech-to-answer";
        var fileBytes = File.ReadAllBytes(RecordedAudioPath);

        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(fileBytes);

        content.Add(fileContent, "file", Path.GetFileName(RecordedAudioPath));

        var response = await _httpClient.PostAsync(requestUri, content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();

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

    private async Task TranscribeVoiceAndStreamResponse()
    {
        IsLoading = true;
        
        try {
            var transcribeUri = $"{ApiUrl}/api/transcribe";
            var fileBytes = File.ReadAllBytes(RecordedAudioPath);

            using var transcribeContent = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(fileBytes);
            transcribeContent.Add(fileContent, "file", Path.GetFileName(RecordedAudioPath));

            var transcribeResponse = await _httpClient.PostAsync(transcribeUri, transcribeContent);
            transcribeResponse.EnsureSuccessStatusCode();

            var transcribeJson = await transcribeResponse.Content.ReadAsStringAsync();
            var startIndex = transcribeJson.IndexOf("\"text\":", StringComparison.Ordinal) + "\"text\":".Length;
            var endIndex = transcribeJson.LastIndexOf('}');

            if (startIndex > 0 && endIndex > startIndex)
            {
                var transcriptionText = transcribeJson.Substring(startIndex, endIndex - startIndex).Trim();
                if (transcriptionText.StartsWith("\"") && transcriptionText.EndsWith("\""))
                {
                    transcriptionText = transcriptionText.Substring(1, transcriptionText.Length - 2);
                }

                Transcription = transcriptionText;
                
                IsLoading = false;
                
                IsStreamLoading = true;
                
                var streamUri = $"{ApiUrl}/api/ask/stream";
                var content = new StringContent($"{{\"question\": \"{transcriptionText}\"}}", Encoding.UTF8, "application/json");
                
                await GetServerSentEventsContentAsync(streamUri, content);
            }
            else
            {
                Transcription = "Ошибка при анализе ответа расшифровки.";
                Response = "Не удалось получить расшифровку голоса.";
            }
        }
        catch (Exception ex) {
            Response = $"Ошибка при обработке голосового ввода: {ex.Message}";
        }
        finally {
            IsLoading = false;
            IsStreamLoading = false;
        }
    }
}