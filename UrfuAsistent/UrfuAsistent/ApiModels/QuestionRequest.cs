using Newtonsoft.Json;

namespace ConsoleApp8.ApiModels;

public class QuestionRequest
{
    [JsonProperty("question")]
    public string Question { get; set; } = "";
}