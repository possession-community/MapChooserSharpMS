using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Wuling.Abstract.Tianshi.Surreal;

namespace MapChooserSharpMS.Tests.Nomination;

internal sealed class StubWulingSurreal : IWulingSurreal
{
    public string ServerId => "test-server";

    public Task<IReadOnlyList<JsonObject>> QueryAsync(string surql, IReadOnlyDictionary<string, object?>? vars = null, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<JsonObject>>([]);

    public Task<int> ExecuteAsync(string surql, IReadOnlyDictionary<string, object?>? vars = null, CancellationToken ct = default)
        => Task.FromResult(0);

    public Task<SurrealEnvelope<T>> FunctionAsync<T>(string surql, IReadOnlyDictionary<string, object?>? vars = null, CancellationToken ct = default)
        => Task.FromResult(default(SurrealEnvelope<T>)!);

    public Task<IReadOnlyList<T>> QueryAsync<T>(string surql, IReadOnlyDictionary<string, object?>? vars = null, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<T>>([]);

    public Task<bool> WriteAsync(string surql, IReadOnlyDictionary<string, object?>? vars = null, CancellationToken ct = default)
        => Task.FromResult(true);

    public Task EnsureSchemasAsync(string schemeDirectory, CancellationToken ct = default)
        => Task.CompletedTask;
}
