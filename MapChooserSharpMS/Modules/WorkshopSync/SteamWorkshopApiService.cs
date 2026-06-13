using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MapChooserSharpMS.Modules.WorkshopSync;

internal enum WorkshopItemStatus
{
    Public,
    FriendsOnly,
    Private,
    Unlisted,
    NotFound,
    Error,
}

internal sealed record WorkshopItemInfo(long PublishedFileId, WorkshopItemStatus Status, string? Title, int ResultCode, int Visibility);

internal sealed class SteamWorkshopApiService : IDisposable
{
    private const int BatchSize = 100;
    private const string CollectionEndpoint = "https://api.steampowered.com/ISteamRemoteStorage/GetCollectionDetails/v1/";
    private const string FileDetailsEndpoint = "https://api.steampowered.com/IPublishedFileService/GetDetails/v1/";

    private readonly HttpClient _http = new();
    internal string ApiKey { get; private set; } = "";

    internal void SetApiKey(string apiKey) => ApiKey = apiKey ?? "";

    public void Dispose() => _http.Dispose();

    internal async Task<List<long>> GetCollectionItemIds(string collectionId, CancellationToken ct = default)
    {
        var form = new List<KeyValuePair<string, string>>
        {
            new("collectioncount", "1"),
            new("publishedfileids[0]", collectionId),
        };

        using var content = new FormUrlEncodedContent(form);
        using var resp = await _http.PostAsync(CollectionEndpoint, content, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);

        var parsed = JsonSerializer.Deserialize<CollectionEnvelope>(json);
        var details = parsed?.Response?.CollectionDetails;
        if (details is null || details.Count == 0)
            return [];

        return details[0].Children?
            .Where(c => long.TryParse(c.PublishedFileId, out _))
            .Select(c => long.Parse(c.PublishedFileId!))
            .ToList() ?? [];
    }

    internal async Task<List<WorkshopItemInfo>> GetPublishedFileDetails(
        IReadOnlyList<long> ids, CancellationToken ct = default)
    {
        var results = new List<WorkshopItemInfo>(ids.Count);

        for (int offset = 0; offset < ids.Count; offset += BatchSize)
        {
            var batch = ids.Skip(offset).Take(BatchSize).ToList();
            var batchResults = await GetFileDetailsBatch(batch, ct);
            results.AddRange(batchResults);
        }

        return results;
    }

    private async Task<List<WorkshopItemInfo>> GetFileDetailsBatch(
        List<long> ids, CancellationToken ct)
    {
        var queryParts = new List<string>();
        if (!string.IsNullOrEmpty(ApiKey))
            queryParts.Add($"key={ApiKey}");
        for (int i = 0; i < ids.Count; i++)
            queryParts.Add($"publishedfileids[{i}]={ids[i]}");

        string url = $"{FileDetailsEndpoint}?{string.Join("&", queryParts)}";

        using var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);

        var parsed = JsonSerializer.Deserialize<FileEnvelope>(json);
        var details = parsed?.Response?.PublishedFileDetails ?? [];

        var byId = new Dictionary<long, FileDetail>();
        foreach (var d in details)
        {
            if (long.TryParse(d.PublishedFileId, out long fid))
                byId[fid] = d;
        }

        var results = new List<WorkshopItemInfo>(ids.Count);
        foreach (long id in ids)
        {
            if (!byId.TryGetValue(id, out var d))
            {
                results.Add(new WorkshopItemInfo(id, WorkshopItemStatus.NotFound, null, -1, -1));
                continue;
            }

            var status = d.Result switch
            {
                1 => d.Visibility switch
                {
                    0 => WorkshopItemStatus.Public,
                    1 => WorkshopItemStatus.FriendsOnly,
                    2 => WorkshopItemStatus.Private,
                    3 => WorkshopItemStatus.Unlisted,
                    _ => WorkshopItemStatus.Public,
                },
                9 => WorkshopItemStatus.NotFound,
                _ => WorkshopItemStatus.Error,
            };
            results.Add(new WorkshopItemInfo(id, status, d.Title, d.Result, d.Visibility));
        }
        return results;
    }

    // JSON DTOs

    private sealed class CollectionEnvelope
    {
        [JsonPropertyName("response")] public CollectionResponse? Response { get; set; }
    }

    private sealed class CollectionResponse
    {
        [JsonPropertyName("collectiondetails")] public List<CollectionDetail>? CollectionDetails { get; set; }
    }

    private sealed class CollectionDetail
    {
        [JsonPropertyName("children")] public List<CollectionChild>? Children { get; set; }
    }

    private sealed class CollectionChild
    {
        [JsonPropertyName("publishedfileid")] public string? PublishedFileId { get; set; }
    }

    private sealed class FileEnvelope
    {
        [JsonPropertyName("response")] public FileResponse? Response { get; set; }
    }

    private sealed class FileResponse
    {
        [JsonPropertyName("publishedfiledetails")] public List<FileDetail>? PublishedFileDetails { get; set; }
    }

    private sealed class FileDetail
    {
        [JsonPropertyName("publishedfileid")] public string? PublishedFileId { get; set; }
        [JsonPropertyName("result")] public int Result { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("visibility")] public int Visibility { get; set; }
    }
}
