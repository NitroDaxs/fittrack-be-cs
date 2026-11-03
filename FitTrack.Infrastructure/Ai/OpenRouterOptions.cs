namespace FitTrack.Infrastructure.Ai;

public class OpenRouterOptions
{
    public string ApiKey { get; set; } = "";

    public string Model { get; set; } = "";
    
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1/";
    
    public double Temperature { get; set; } = 0.5;
    
    public int MaxTokens { get; set; } = 700;
    
    public string AppTitle { get; set; } = "FitTrack";

    public string AppUrl { get; set; } = "http://localhost:5173";
}