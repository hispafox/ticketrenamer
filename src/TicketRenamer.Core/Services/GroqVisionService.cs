using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TicketRenamer.Core.Models;

namespace TicketRenamer.Core.Services;

public sealed class GroqVisionService : IOcrService
{
    private readonly HttpClient _httpClient;
    private const string GroqApiUrl = "https://api.groq.com/openai/v1/chat/completions";
    private const string Model = "llama-3.2-90b-vision-preview";

    private const string SystemPrompt = """
        Eres un asistente que analiza fotos de tickets de compra de supermercados españoles.
        Debes extraer exactamente dos datos:
        1. El nombre del proveedor/supermercado (por ejemplo: Mercadona, Carrefour, Lidl, Dia, Aldi, Ahorramas, Eroski, Alcampo, Consum, BonArea)
        2. La fecha de la compra en formato YYYY-MM-DD

        Responde SOLO con un JSON valido con este formato exacto, sin texto adicional:
        {"provider": "NombreProveedor", "date": "YYYY-MM-DD"}

        Si no puedes detectar el proveedor, usa null para provider.
        Si no puedes detectar la fecha, usa null para date.
        """;

    public GroqVisionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GroqVisionResponse> ExtractReceiptDataAsync(string imagePath, CancellationToken ct = default)
    {
        var imageBytes = await File.ReadAllBytesAsync(imagePath, ct);
        var base64Image = Convert.ToBase64String(imageBytes);
        var mimeType = GetMimeType(imagePath);

        var requestBody = new
        {
            model = Model,
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = "Analiza este ticket de compra y extrae el proveedor y la fecha." },
                        new
                        {
                            type = "image_url",
                            image_url = new { url = $"data:{mimeType};base64,{base64Image}" }
                        }
                    }
                }
            },
            temperature = 0.1,
            max_tokens = 256
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(GroqApiUrl, content, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var groqResponse = JsonDocument.Parse(responseJson);

        var messageContent = groqResponse.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(messageContent))
            return new GroqVisionResponse();

        return ParseGroqResponse(messageContent);
    }

    private static GroqVisionResponse ParseGroqResponse(string content)
    {
        // The LLM might wrap JSON in markdown code blocks
        var jsonStr = content.Trim();
        if (jsonStr.StartsWith("```"))
        {
            var lines = jsonStr.Split('\n');
            jsonStr = string.Join('\n', lines.Skip(1).TakeWhile(l => !l.StartsWith("```")));
        }

        try
        {
            var doc = JsonDocument.Parse(jsonStr);
            var root = doc.RootElement;

            return new GroqVisionResponse
            {
                Provider = root.TryGetProperty("provider", out var p) && p.ValueKind != JsonValueKind.Null
                    ? p.GetString()
                    : null,
                Date = root.TryGetProperty("date", out var d) && d.ValueKind != JsonValueKind.Null
                    ? d.GetString()
                    : null
            };
        }
        catch (JsonException)
        {
            return new GroqVisionResponse();
        }
    }

    private static string GetMimeType(string filePath) =>
        Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "image/jpeg"
        };
}
