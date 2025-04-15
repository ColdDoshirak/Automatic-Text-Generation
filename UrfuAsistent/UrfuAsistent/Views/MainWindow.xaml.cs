using System;
using System.Windows;
using ConsoleApp8.ViewModels;
using MaterialDesignThemes.Wpf;

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

        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            MessageBox.Show($"An unexpected error occurred: {args.ExceptionObject}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };
    }
}