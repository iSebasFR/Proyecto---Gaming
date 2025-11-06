using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Proyecto_Gaming.ViewModels;

namespace Proyecto_Gaming.Services
{
    public class OllamaChatService : IChatbotService
    {
        private readonly HttpClient _client;
        private readonly IFallbackService _fallback;
        private readonly ILogger<OllamaChatService> _logger;
        private readonly string _model;
        private readonly bool _blendWithFallback;
    private readonly string? _systemPrompt;
    private readonly int _maxTokens;
    private readonly double _temperature;

        public OllamaChatService(HttpClient client, IConfiguration config, IFallbackService fallback, ILogger<OllamaChatService> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var baseUrl = config["Chatbot:Ollama:BaseUrl"] ?? "http://localhost:11434";
            if (!baseUrl.EndsWith('/'))
            {
                baseUrl += "/";
            }

            if (_client.BaseAddress == null)
            {
                _client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            }

            _client.Timeout = TimeSpan.FromSeconds(60);
            if (!_client.DefaultRequestHeaders.Accept.Any(h => h.MediaType == "application/json"))
            {
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            _model = config["Chatbot:Ollama:Model"] ?? "mistral";
            _systemPrompt = config["Chatbot:Ollama:SystemPrompt"];
            _blendWithFallback = config.GetValue<bool?>("Chatbot:Ollama:BlendFallback") ?? false;
            _maxTokens = config.GetValue<int?>("Chatbot:Ollama:MaxTokens") ?? 160;
            _temperature = config.GetValue<double?>("Chatbot:Ollama:Temperature") ?? 0.7;
        }

        public async Task<ChatResponse> GetReplyAsync(ChatRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Message))
            {
                var emptyReply = _fallback.GetFallbackReply(string.Empty);
                return new ChatResponse { IsSuccess = true, Reply = emptyReply, Source = "local" };
            }

            try
            {
                var chatResult = await TryChatEndpointAsync(request.Message);

                if (!chatResult.Success && chatResult.RetryWithGenerate)
                {
                    _logger.LogInformation("[Ollama] Endpoint /api/chat no disponible. Probando /api/generate...");
                    var generateResult = await TryGenerateEndpointAsync(request.Message);
                    chatResult = generateResult;
                }

                if (chatResult.Success && !string.IsNullOrWhiteSpace(chatResult.Reply))
                {
                    var combined = CombineWithFallback(chatResult.Reply, request.Message);
                    return new ChatResponse
                    {
                        IsSuccess = true,
                        Reply = combined,
                        Source = _blendWithFallback ? "hybrid" : "ollama",
                        Error = chatResult.Error
                    };
                }

                var fallbackReply = _fallback.GetFallbackReply(request.Message);
                if (!string.IsNullOrWhiteSpace(chatResult.Error))
                {
                    fallbackReply = "No pude conectarme con el modelo local (Ollama). " +
                                     "Detalle: " + chatResult.Error + Environment.NewLine + Environment.NewLine +
                                     fallbackReply;
                }
                return new ChatResponse
                {
                    IsSuccess = true,
                    Reply = fallbackReply,
                    Source = "local",
                    Error = chatResult.Error
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Ollama] Error al generar respuesta");
                var fallbackReply = _fallback.GetFallbackReply(request.Message);
                return new ChatResponse
                {
                    IsSuccess = true,
                    Reply = fallbackReply,
                    Source = "local",
                    Error = ex.Message
                };
            }
        }

        private StringContent BuildPayload(string userMessage)
        {
            var messages = new List<object>();
            if (!string.IsNullOrWhiteSpace(_systemPrompt))
            {
                messages.Add(new { role = "system", content = _systemPrompt });
            }

            messages.Add(new { role = "user", content = userMessage });

            var body = new
            {
                model = _model,
                messages,
                stream = false,
                options = BuildOptions()
            };

            var json = JsonSerializer.Serialize(body);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private async Task<OllamaResult> TryChatEndpointAsync(string userMessage)
        {
            try
            {
                using var payload = BuildPayload(userMessage);
                var response = await _client.PostAsync("api/chat", payload);
                var raw = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return new OllamaResult(false, null, "El endpoint /api/chat no está disponible en esta versión de Ollama.", true);
                    }

                    var error = $"Ollama devolvió {(int)response.StatusCode}: {response.ReasonPhrase}. Cuerpo: {raw}";
                    _logger.LogWarning("[Ollama] Chat falló ({StatusCode}): {Body}", (int)response.StatusCode, raw);
                    return new OllamaResult(false, null, error, false);
                }

                var replyText = ParseAssistantMessage(raw, out var modelError);
                if (!string.IsNullOrWhiteSpace(modelError))
                {
                    _logger.LogWarning("[Ollama] Error reportado por el modelo: {Error}", modelError);
                }

                if (string.IsNullOrWhiteSpace(replyText))
                {
                    _logger.LogWarning("[Ollama] Respuesta sin contenido en /api/chat. Cuerpo: {Body}", raw);
                    return new OllamaResult(false, null, "Ollama devolvió una respuesta vacía.", false);
                }

                return new OllamaResult(true, replyText, modelError, false);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "[Ollama] No se pudo conectar al endpoint /api/chat");
                return new OllamaResult(false, null, ex.Message, false);
            }
        }

        private async Task<OllamaResult> TryGenerateEndpointAsync(string userMessage)
        {
            try
            {
                using var payload = BuildGeneratePayload(userMessage);
                var response = await _client.PostAsync("api/generate", payload);
                var raw = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var error = $"Ollama (/api/generate) devolvió {(int)response.StatusCode}: {response.ReasonPhrase}. Cuerpo: {raw}";
                    _logger.LogWarning("[Ollama] Generate falló ({StatusCode}): {Body}", (int)response.StatusCode, raw);
                    return new OllamaResult(false, null, error, false);
                }

                var replyText = ParseAssistantMessage(raw, out var modelError);
                if (string.IsNullOrWhiteSpace(replyText))
                {
                    _logger.LogWarning("[Ollama] Respuesta sin contenido en /api/generate. Cuerpo: {Body}", raw);
                    return new OllamaResult(false, null, "Ollama devolvió una respuesta vacía en generate.", false);
                }

                return new OllamaResult(true, replyText, modelError, false);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "[Ollama] No se pudo conectar al endpoint /api/generate");
                return new OllamaResult(false, null, ex.Message, false);
            }
        }

        private StringContent BuildGeneratePayload(string userMessage)
        {
            var promptBuilder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(_systemPrompt))
            {
                promptBuilder.AppendLine(_systemPrompt);
                promptBuilder.AppendLine();
            }

            promptBuilder.Append(userMessage);

            var body = new
            {
                model = _model,
                prompt = promptBuilder.ToString(),
                stream = false,
                options = BuildOptions()
            };

            var json = JsonSerializer.Serialize(body);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private object BuildOptions() => new
        {
            num_predict = _maxTokens,
            temperature = _temperature
        };

        private string ParseAssistantMessage(string raw, out string? error)
        {
            error = null;
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("message", out var messageElement))
                {
                    if (messageElement.TryGetProperty("content", out var content))
                    {
                        var extracted = ExtractContent(content);
                        if (!string.IsNullOrEmpty(extracted))
                        {
                            return extracted;
                        }
                    }
                }

                if (doc.RootElement.TryGetProperty("response", out var responseElement))
                {
                    return responseElement.GetString() ?? string.Empty;
                }

                if (doc.RootElement.TryGetProperty("messages", out var messagesElement) && messagesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var msg in messagesElement.EnumerateArray())
                    {
                        if (msg.TryGetProperty("role", out var role) && role.GetString() == "assistant" && msg.TryGetProperty("content", out var content))
                        {
                            var extracted = ExtractContent(content);
                            if (!string.IsNullOrEmpty(extracted))
                            {
                                return extracted;
                            }
                        }
                    }
                }

                if (doc.RootElement.TryGetProperty("error", out var errorElement))
                {
                    error = errorElement.GetString();
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "[Ollama] No se pudo parsear la respuesta JSON: {Raw}", raw);
            }

            return string.Empty;
        }

        private static string ExtractContent(JsonElement content)
        {
            if (content.ValueKind == JsonValueKind.String)
            {
                return content.GetString() ?? string.Empty;
            }

            if (content.ValueKind == JsonValueKind.Array)
            {
                var builder = new StringBuilder();
                foreach (var item in content.EnumerateArray())
                {
                    switch (item.ValueKind)
                    {
                        case JsonValueKind.String:
                            builder.Append(item.GetString());
                            break;
                        case JsonValueKind.Object:
                            if (item.TryGetProperty("text", out var textNode))
                            {
                                builder.Append(textNode.GetString());
                            }
                            break;
                    }
                }

                return builder.ToString();
            }

            return string.Empty;
        }

        private string CombineWithFallback(string primary, string userMessage)
        {
            if (!_blendWithFallback)
                return primary;

            var local = _fallback.GetFallbackReply(userMessage);
            if (string.IsNullOrWhiteSpace(local))
                return primary;

            var trimmedPrimary = primary.Trim();
            var trimmedLocal = local.Trim();

            if (string.IsNullOrEmpty(trimmedPrimary))
                return trimmedLocal;

            if (string.Equals(trimmedPrimary, trimmedLocal, StringComparison.OrdinalIgnoreCase))
                return trimmedPrimary;

            return trimmedPrimary + Environment.NewLine + Environment.NewLine + "Sugerencias adicionales: " + trimmedLocal;
        }
    }

    internal readonly record struct OllamaResult(bool Success, string? Reply, string? Error, bool RetryWithGenerate);
}
