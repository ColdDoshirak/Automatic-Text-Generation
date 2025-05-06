using System.IO;
using System.Windows;
using ConsoleApp8.ViewModels;

namespace ConsoleApp8.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.Response = "Welcome to AI Chat Assistant!\n\nPlease enter your question in the box below and click the Send button.";

        if (string.IsNullOrEmpty(viewModel.ApiUrl))
        {
            _viewModel.Response += "\n\nWARNING: API URL is not configured. Please set the API URL in settings before sending prompts.";
        }

        // Set up property change handlers for the SSR tab
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.SsrHtml) && _viewModel.IsSsrLoaded)
            {
                UpdateSsrBrowser();
            }
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            MessageBox.Show($"An unexpected error occurred: {args.ExceptionObject}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };
    }

    private void UpdateSsrBrowser()
    {
        try
        {
            // Create temporary HTML file to display the streamed content
            var tempFile = Path.GetTempFileName() + ".html";

            // Create a simple HTML document with the streamed content
            var htmlContent = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ 
            font-family: Arial, sans-serif; 
            margin: 20px;
            line-height: 1.5;
        }}
        pre {{ 
            white-space: pre-wrap;
            background-color: #f5f5f5;
            padding: 10px;
            border-radius: 5px;
        }}
    </style>
</head>
<body>
    {_viewModel.SsrHtml}
</body>
</html>";

            File.WriteAllText(tempFile, htmlContent);

            // Navigate to the temporary file
            SsrBrowser.Navigate(new Uri(tempFile));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error displaying SSR content: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}