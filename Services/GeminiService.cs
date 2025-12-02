using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TruekAppAPI.Services;

public interface IAiService
{
    Task<string> EnhanceDescriptionAsync(string originalText);
}

public class GeminiService(IConfiguration config, HttpClient httpClient) : IAiService
{
    private readonly string _apiKey = config["Gemini:ApiKey"] ?? throw new Exception("Gemini API Key no encontrada");
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

    public async Task<string> EnhanceDescriptionAsync(string originalText)
    {
        var url = $"{BaseUrl}?key={_apiKey}";

        // Instrucción para la IA (Prompt Engineering básico)
        var prompt = $"Actúa como un experto en marketing digital y ventas. " +
                     $"Reescribe la siguiente descripción de un producto para que sea atractiva, " +
                     $"persuasiva y profesional, utilizando emojis adecuados. " +
                     $"Mantén el texto conciso (máximo 300 caracteres). " +
                     $"Solo devuelve el texto mejorado, sin introducciones ni comillas. " +
                     $"Texto original: {originalText}";

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(url, content);
        
        if (!response.IsSuccessStatusCode)
        {
            // Loguear error real en consola del servidor
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error Gemini: {error}");
            return originalText; // Si falla, devolvemos el original para no romper la app
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GeminiResponse>(jsonResponse);

        // Extraer el texto de la respuesta compleja de Google
        return result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? originalText;
    }
}

// Clases auxiliares para mapear la respuesta de Google
public class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public List<Candidate>? Candidates { get; set; }
}
public class Candidate
{
    [JsonPropertyName("content")]
    public Content? Content { get; set; }
}
public class Content
{
    [JsonPropertyName("parts")]
    public List<Part>? Parts { get; set; }
}
public class Part
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}