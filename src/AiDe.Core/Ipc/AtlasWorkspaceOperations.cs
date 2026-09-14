using System.Text;
using System.Text.Json;
using AiDe.Core.Understanding;

namespace AiDe.Core.Ipc;

/// <summary>The only production composition facade; native authority and scope state remain inside Core.</summary>
public static class AtlasWorkspaceOperations
{
    public const string Capabilities = "atlas.capabilities.v1";
    public const string Admit = "atlas.admit.v1";
    public const string Inventory = "atlas.inventory.v1";
    public const string Select = "atlas.select.v1";
    public const string Restore = "atlas.restore.v1";
    public const string Release = "atlas.release.v1";
    private static readonly string[] Names = [Capabilities, Admit, Inventory, Select, Restore, Release];

    public static IAsyncDisposable Register(
        DaemonEndpoint endpoint, WorkspaceCore workspace, string workspaceId, string sourceRootPath,
        string dataDirectory, string approvedGitExecutablePath, string approvedGitSha256, string approvedGitVersion)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(workspace);
        if (!string.Equals(endpoint.WorkspaceId, workspaceId, StringComparison.Ordinal)
            || !Path.IsPathFullyQualified(approvedGitExecutablePath)
            || approvedGitSha256.Length != 64 || !approvedGitSha256.All(Uri.IsHexDigit)
            || string.IsNullOrWhiteSpace(approvedGitVersion))
            throw new ArgumentException("Invalid Atlas production configuration.");
        endpoint.RequireUnregistered(Names);
        AtlasWorkspaceReadPolicy policy;
        try { policy = new AtlasWorkspaceReadPolicy(workspace, workspaceId, sourceRootPath, dataDirectory); }
        catch (AtlasReadException exception) { throw new UnauthorizedAccessException(exception.Message); }
        var issuer = new AtlasReadScopeIssuer(policy,
            new AtlasGitMembership(approvedGitExecutablePath, approvedGitSha256, approvedGitVersion),
            AtlasReadBudget.ProcessWide);
        endpoint.RegisterOpening(issuer.Open);
        endpoint.RegisterConnectionEnded(issuer.EndConnectionAsync);
        endpoint.RegisterAsync(Capabilities, (request, peer, token) => Execute(() =>
        {
            token.ThrowIfCancellationRequested();
            issuer.RequireNegotiation(peer, request.Capability);
            var offered = AtlasReaderProjection.DeserializeCapabilitiesRequest(Payload(request));
            if (!offered.SupportedVersions.Contains(1))
                throw new AtlasReadException("Atlas.UnsupportedVersion", "No supported Atlas version was offered.");
            return ValueTask.FromResult(IpcResponse.Success(new AtlasCapabilitiesDto([1],
                ["inventory", "source", "outline", "receipts"], AtlasReaderProjection.MaxFrameBodyBytes,
                AtlasReaderProjection.MaxPageTextUtf8Bytes, PageRequest.MaxLimit), WorkspaceOperations.Wire));
        }));
        endpoint.RegisterAsync(Admit, (request, peer, token) => Execute(async () =>
        {
            var dto = AtlasReaderProjection.DeserializeAdmitRequest(Payload(request));
            MatchEpoch(dto.ExpectedCoreEpoch, request);
            var prepared = await issuer.AdmitAsync(dto, peer, request.Capability, token).ConfigureAwait(false);
            return await Hold(endpoint, peer, prepared,
                () => JsonSerializer.SerializeToUtf8Bytes(prepared.Value, WorkspaceOperations.Wire)).ConfigureAwait(false);
        }));
        endpoint.RegisterAsync(Inventory, (request, peer, token) => Execute(async () =>
        {
            var dto = AtlasReaderProjection.DeserializeInventoryRequest(Payload(request));
            MatchEpoch(dto.ExpectedCoreEpoch, request);
            var prepared = await issuer.InventoryAsync(dto, peer, request.Capability, token).ConfigureAwait(false);
            return await Hold(endpoint, peer, prepared,
                () => AtlasReaderProjection.SerializeInventory(prepared.Value, dto)).ConfigureAwait(false);
        }));
        endpoint.RegisterAsync(Select, (request, peer, token) => Execute(async () =>
        {
            var dto = AtlasReaderProjection.DeserializeSelect(Payload(request));
            MatchEpoch(dto.ExpectedCoreEpoch, request);
            var prepared = await issuer.SelectAsync(dto, peer, request.Capability, token).ConfigureAwait(false);
            return await Hold(endpoint, peer, prepared, () => AtlasReaderProjection.SerializeSelection(
                prepared.Value, dto with { ManifestToken = prepared.Value.ManifestToken })).ConfigureAwait(false);
        }));
        endpoint.RegisterAsync(Restore, (request, peer, token) => Execute(async () =>
        {
            var dto = AtlasReaderProjection.DeserializeRestoreRequest(Payload(request));
            MatchEpoch(dto.ExpectedCoreEpoch, request);
            var prepared = await issuer.RestoreAsync(dto, peer, request.Capability, token).ConfigureAwait(false);
            return await Hold(endpoint, peer, prepared,
                () => JsonSerializer.SerializeToUtf8Bytes(prepared.Value, ReaderWire)).ConfigureAwait(false);
        }));
        endpoint.RegisterAsync(Release, (request, peer, token) => Execute(async () =>
        {
            var dto = AtlasReaderProjection.DeserializeReleaseRequest(Payload(request));
            MatchEpoch(dto.ExpectedCoreEpoch, request);
            return IpcResponse.Success(await issuer.ReleaseAsync(dto, peer, request.Capability, token).ConfigureAwait(false),
                WorkspaceOperations.Wire);
        }));
        return issuer;
    }

    // Restore has no source-window fields on its request; its retained original request was validated in the issuer.
    private static readonly JsonSerializerOptions ReaderWire = CreateReaderWire();
    private static JsonSerializerOptions CreateReaderWire()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(allowIntegerValues: false));
        return options;
    }

    private static void MatchEpoch(long epoch, IpcRequest request)
    {
        if (epoch != request.WorkspaceEpoch)
            throw new AtlasReadException("Atlas.ScopeInvalid", "The Atlas request does not match its envelope.");
    }

    private static ReadOnlyMemory<byte> Payload(IpcRequest request)
    {
        if (request.Payload is not { ValueKind: JsonValueKind.Object } payload)
            throw new JsonException("Atlas requires an object payload.");
        var bytes = Encoding.UTF8.GetBytes(payload.GetRawText());
        if (bytes.Length > AtlasReaderProjection.MaxFrameBodyBytes)
            throw new AtlasReadException("Atlas.PayloadTooLarge", "The Atlas request exceeds its frame budget.");
        return bytes;
    }

    private static async ValueTask<IpcResponse> Hold<T>(
        DaemonEndpoint endpoint, IpcPeer peer, AtlasPrepared<T> prepared, Func<byte[]> serialize)
    {
        try
        {
            var bytes = serialize();
            using var document = JsonDocument.Parse(bytes);
            var response = IpcResponse.Success(document.RootElement.Clone());
            if (JsonSerializer.SerializeToUtf8Bytes(response, WorkspaceOperations.Wire).Length > AtlasReaderProjection.MaxFrameBodyBytes)
                throw new AtlasReadException("Atlas.PayloadTooLarge", "The Atlas response exceeds its frame budget.");
            endpoint.HoldPublication(response, peer, prepared.Operation);
            return response;
        }
        catch
        {
            await prepared.Operation.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static async ValueTask<IpcResponse> Execute(Func<ValueTask<IpcResponse>> action)
    {
        try { return await action().ConfigureAwait(false); }
        catch (AtlasReadException exception) { return IpcResponse.Error(exception.Code, exception.Message); }
        catch (JsonException) { return IpcResponse.Error("Atlas.Malformed", "The Atlas payload is invalid."); }
        catch (ArgumentException) { return IpcResponse.Error("Atlas.Malformed", "The Atlas request is invalid."); }
        catch (IOException) { return IpcResponse.Error("Atlas.MembershipUnavailable", "Current Atlas resources are unavailable."); }
    }
}
