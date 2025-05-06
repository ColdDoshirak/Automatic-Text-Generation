using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ConsoleApp8.ViewModels;
using MaterialDesignThemes.Wpf;

namespace ConsoleApp8.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private bool _isRecording = false;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.Response = "Добро пожаловать в ассистент УрФУ!\n\nВведите ваш вопрос в поле ниже и нажмите кнопку отправки.";

        if (string.IsNullOrEmpty(viewModel.ApiUrl))
        {
            _viewModel.Response += "\n\nВНИМАНИЕ: URL API не настроен. Пожалуйста, укажите URL API в настройках перед отправкой вопросов.";
        }

        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.SsrHtml) && _viewModel.IsSsrLoaded)
            {
                UpdateStreamBrowser();
            }
            
            if (args.PropertyName == nameof(MainViewModel.IsStreamingActive) || 
                args.PropertyName == nameof(MainViewModel.IsSimpleResponse))
            {
                if (_viewModel.IsStreamingActive)
                {
                    UpdateStreamBrowser();
                }
            }
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            MessageBox.Show($"Произошла непредвиденная ошибка: {args.ExceptionObject}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        };
    }

    private void UpdateStreamBrowser()
    {
        try
        {
            if (_viewModel.SsrHtml != null)
            {
                _viewModel.Response = _viewModel.SsrHtml;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при отображении стримингового контента: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isRecording)
        {
            StartRecording();
        }
        else
        {
            StopRecording();
        }
    }

    private void StartRecording()
    {
        _isRecording = true;
        RecordIcon.Kind = PackIconKind.Stop;
        RecordButtonText.Text = "Stop Recording";
        RecordingStatus.Text = "Recording...";
        RecordingStatus.Foreground = new SolidColorBrush(Colors.Red);
        
        _viewModel.StartRecording();
    }

    private void StopRecording()
    {
        _isRecording = false;
        RecordIcon.Kind = PackIconKind.Microphone;
        RecordButtonText.Text = "Голос";
        RecordingStatus.Text = "Обработка...";
        RecordingStatus.Foreground = new SolidColorBrush(Colors.Orange);
        
        _viewModel.StopRecording();
        
        _viewModel.PropertyChanged += OnRecordingCompletePropertyChanged;
    }

    private void OnRecordingCompletePropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(MainViewModel.IsRecordingComplete) && _viewModel.IsRecordingComplete)
        {
            RecordingStatus.Text = "Запись готова";
            RecordingStatus.Foreground = new SolidColorBrush(Colors.Green);
            
            _viewModel.PropertyChanged -= OnRecordingCompletePropertyChanged;
        }
    }
}