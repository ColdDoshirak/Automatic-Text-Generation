using Newtonsoft.Json;

namespace ConsoleApp8.ApiModels;

public class AnswerResponse
{
    [JsonProperty("answer")]
    public string Answer { get; set; } = "";
}