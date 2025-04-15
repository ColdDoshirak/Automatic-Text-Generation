using System.Threading.Tasks;
using System.Windows.Input;
using ConsoleApp8.Configuration;
using ConsoleApp8.Services;

namespace ConsoleApp8.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly ApiService _apiService;
    private readonly ConfigurationService _configService;

    private string _prompt = "";
    private string _response = "";
    private string _apiUrl = "";
    private bool _isLoading;

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

    public ICommand SendPromptCommand { get; }
    public ICommand UpdateApiUrlCommand { get; }
    public ICommand ClearPromptCommand { get; }

    public MainViewModel(ApiService apiService, ConfigurationService configService)
    {
        _apiService = apiService;
        _configService = configService;

        ApiUrl = _configService.GetApiSettings().BaseUrl;

        SendPromptCommand = new RelayCommand(async _ => await SendPrompt(), _ => !string.IsNullOrWhiteSpace(Prompt) && !IsLoading);
        UpdateApiUrlCommand = new RelayCommand(_ => UpdateApiUrl(), _ => !string.IsNullOrWhiteSpace(ApiUrl) && !IsLoading);
        ClearPromptCommand = new RelayCommand(_ => ClearPrompt(), _ => !IsLoading);
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