using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace ConsoleApp8.Configuration;

public class ConfigurationService
{
    private readonly string _configPath;
    private IConfiguration _configuration;

    public ConfigurationService(string configPath = "appsettings.json")
    {
        _configPath = configPath;
        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        try
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(_configPath, optional: true, reloadOnChange: true);

            _configuration = builder.Build();

            if (string.IsNullOrEmpty(GetApiSettings().BaseUrl))
            {
                EnsureDefaultConfiguration();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading configuration: {ex.Message}");
            EnsureDefaultConfiguration();
        }
    }

    private void EnsureDefaultConfiguration()
    {
        try
        {
            var defaultConfig = new
            {
                ApiSettings = new ApiSettings
                {
                    BaseUrl = "http://localhost:7860"
                }
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(defaultConfig, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(_configPath, json);

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(_configPath, optional: false, reloadOnChange: true);

            _configuration = builder.Build();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating default configuration: {ex.Message}");
            _configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        }
    }

    public ApiSettings GetApiSettings()
    {
        var settings = new ApiSettings();
        _configuration.GetSection("ApiSettings")?.Bind(settings);
        return settings;
    }

    public void UpdateApiUrl(string newUrl)
    {
        try
        {
            var directory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(_configPath))
            {
                EnsureDefaultConfiguration();
            }

            var json = File.ReadAllText(_configPath);
            dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

            if (jsonObj["ApiSettings"] == null)
                jsonObj["ApiSettings"] = new Newtonsoft.Json.Linq.JObject();

            jsonObj["ApiSettings"]["BaseUrl"] = newUrl;

            var output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(_configPath, output);

            LoadConfiguration();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating API URL: {ex.Message}");
        }
    }
}