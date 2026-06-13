using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsToml;
using CsToml.Extensions;
using Microsoft.Extensions.Logging;

namespace MapChooserSharpMS.Modules.WorkshopSync;

internal sealed class DiscordWebhookService : IDisposable
{
    private readonly HttpClient _http = new();
    private readonly ILogger _logger;

    public DiscordWebhookService(ILogger logger)
    {
        _logger = logger;
    }

    public void Dispose() => _http.Dispose();

    public async Task<bool> TrySendAsync(
        string configFilePath,
        Dictionary<string, string> placeholders,
        CancellationToken ct = default)
    {
        if (!File.Exists(configFilePath))
        {
            _logger.LogDebug("Webhook config not found: {Path}", configFilePath);
            return false;
        }

        string webhookUrl;
        string jsonTemplate;

        try
        {
            var doc = CsTomlFileSerializer.Deserialize<TomlDocument>(configFilePath);
            var root = doc.RootNode;

            if (!root["WebhookUrl"u8].TryGetString(out var urlValue))
            {
                _logger.LogWarning("Webhook config missing WebhookUrl: {Path}", configFilePath);
                return false;
            }
            webhookUrl = urlValue.ToString();

            if (string.IsNullOrWhiteSpace(webhookUrl))
            {
                _logger.LogDebug("Webhook URL is empty, skipping: {Path}", configFilePath);
                return false;
            }

            if (!root["JsonTemplate"u8].TryGetString(out var templateValue))
            {
                _logger.LogWarning("Webhook config missing JsonTemplate: {Path}", configFilePath);
                return false;
            }
            jsonTemplate = templateValue.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse webhook config: {Path}", configFilePath);
            return false;
        }

        string body = ReplacePlaceholders(jsonTemplate, placeholders);

        try
        {
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            using var resp = await _http.PostAsync(webhookUrl, content, ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Webhook POST failed ({Status}): {Path}", resp.StatusCode, configFilePath);
                return false;
            }

            _logger.LogDebug("Webhook sent successfully: {Path}", configFilePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Webhook POST exception: {Path}", configFilePath);
            return false;
        }
    }

    private static string ReplacePlaceholders(string template, Dictionary<string, string> placeholders)
    {
        var result = template;
        foreach (var (key, value) in placeholders)
        {
            result = result.Replace($"%{key}%", EscapeJson(value));
        }
        return result;
    }

    private static string EscapeJson(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}
