using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AiDe.Core.Extraction;

/// <summary>
/// What a scope's inputs looked like the last time it was extracted successfully.
/// </summary>
/// <remarks>
/// <para><b>Every index re-extracted every scope.</b> On a real repository that is 4.5 seconds and
/// seven scopes, and it grows with the codebase — paid in full whether one file changed or none.
/// A fingerprint that has not moved means the evidence in the store is already the answer.</para>
///
/// <para><b>A skip is reported, never disguised as work.</b> <c>IndexResult</c> counts reused scopes
/// separately from indexed ones, because "7 of 7 indexed" would be a true sentence about a run that
/// read nothing, and the operator's next question after a surprising graph is always "did it
/// actually look?".</para>
///
/// <para><b>It fails towards re-extraction.</b> An unreadable directory, a missing sidecar, a
/// changed extractor version — every uncertainty produces a fingerprint that does not match, and the
/// scope is read again. The cost of an unnecessary extraction is seconds; the cost of a skipped one
/// is a graph that quietly describes code that no longer exists.</para>
/// </remarks>
public sealed class ScopeFingerprints
{
    /// <summary>
    /// Bumped whenever extraction output could change for unchanged input.
    /// </summary>
    /// <remarks>
    /// Part of every fingerprint, so upgrading the product invalidates the whole sidecar. Without it
    /// an extractor improvement would reach only the files a user happened to touch afterwards —
    /// and the graph would be a mix of two extractor generations with nothing saying so.
    /// </remarks>
    // 2026-08-30.1 — the knowledge extractor, node_class classification, comment stripping in four
    // readers, the SQL fold and uses_table. Every one of those changes extraction OUTPUT for input
    // that did not change, so a store built before them is a mix of two generations. The user saw
    // exactly that: Knowledge read 0 on a repository holding 2,343 knowledge nodes, because the
    // scopes were cached from a build that had no knowledge reader.
    // 2026-08-30.2 — SourceRevision. The .1 bump was correct and reached nothing: a second reuse
    // check inside RefreshScopeAsync matched on the unchanged artifact revision and returned an empty
    // result, so 66 scopes were visited and none re-read (DC-044). This bump is the first one that
    // can actually take effect.
    // 2026-08-31.1 — `declared_at`. Every scope now records WHERE its files are, relative to the
    // workspace root, because nothing did: an assertion's provenance path is relative to its scope
    // and no fact said where the scope was, so a node could not be resolved to a file at all. A store
    // written before this cannot answer a content query, and the reader would show "source could not
    // be located" for everything — which is the shape a stale generation always takes.
    // 2026-09-01.1 — knowledge prose links (`links_to`), TypeScript import precision and
    // declaration coverage, and the build-output/generated-file exclusions that came with them.
    // Every one changes extraction OUTPUT for input that has not changed.
    public const string ExtractorGeneration = "2026-09-01.1";

    private const string FileName = "scope-fingerprints.json";

    private static readonly HashSet<string> Skip = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", ".vs", "node_modules", "artifacts", "packages", "TestResults",
    };

    private readonly string _path;
    private readonly Dictionary<string, string> _byScope;

    private ScopeFingerprints(string path, Dictionary<string, string> byScope)
    {
        _path = path;
        _byScope = byScope;
    }

    public static ScopeFingerprints Load(string dataDirectory)
    {
        var path = Path.Combine(dataDirectory, FileName);

        try
        {
            if (File.Exists(path))
            {
                var stored = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
                if (stored is not null)
                {
                    return new ScopeFingerprints(path, new Dictionary<string, string>(stored, StringComparer.Ordinal));
                }
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // An unreadable sidecar means every scope is re-extracted. That is the slow answer, and
            // it is the only safe one: a corrupt cache that is trusted is worse than no cache.
        }

        return new ScopeFingerprints(path, new Dictionary<string, string>(StringComparer.Ordinal));
    }

    /// <summary>True when this scope's inputs are byte-for-byte what they were when it last ran.</summary>
    public bool IsUnchanged(string scopeId, string fingerprint) =>
        fingerprint.Length > 0
        && _byScope.TryGetValue(scopeId, out var previous)
        && string.Equals(previous, fingerprint, StringComparison.Ordinal);

    public void Record(string scopeId, string fingerprint)
    {
        if (fingerprint.Length == 0)
        {
            // A fingerprint that could not be computed is not recorded. Recording an empty one would
            // make the next run believe an unreadable scope was up to date.
            _byScope.Remove(scopeId);
            return;
        }

        _byScope[scopeId] = fingerprint;
    }

    /// <summary>Forgets a scope, so the next run reads it whatever the filesystem says.</summary>
    public void Invalidate(string scopeId) => _byScope.Remove(scopeId);

    /// <summary>
    /// Forgets every scope this run did not see, and reports whether the SET of scopes changed.
    /// </summary>
    /// <remarks>
    /// <para><b>A project appearing is not a change to any existing scope.</b> Every per-scope
    /// fingerprint can be identical while the workspace has gained a project, lost one, or had one
    /// renamed — and a cache keyed only per scope would report "all reused" for a workspace whose
    /// shape had changed underneath it.</para>
    ///
    /// <para>Discovery runs on every index regardless, so a NEW scope is always extracted — it has
    /// no fingerprint to match. The case this closes is the opposite one: a scope that has gone.
    /// Its evidence would otherwise sit in the store forever, describing code that no longer exists,
    /// with nothing to remove it and nothing to say so.</para>
    /// </remarks>
    public bool Reconcile(IEnumerable<string> discoveredScopeIds)
    {
        var present = new HashSet<string>(discoveredScopeIds, StringComparer.Ordinal);
        var departed = _byScope.Keys.Where(id => !present.Contains(id)).ToList();

        foreach (var id in departed) _byScope.Remove(id);

        var arrived = present.Count(id => !_byScope.ContainsKey(id));
        return departed.Count > 0 || arrived > 0;
    }

    /// <summary>Scope ids this sidecar still remembers. For reporting what a run left behind.</summary>
    public IReadOnlyCollection<string> Known => _byScope.Keys;

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            File.WriteAllText(_path, JsonSerializer.Serialize(
                _byScope, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A cache that cannot be written is a slow next run, never a failed this one.
        }
    }

    /// <summary>
    /// A stable digest of a scope's input files: relative path, size and modification time.
    /// </summary>
    /// <remarks>
    /// <para>Not content hashes. Reading every byte of every file to decide whether to read every
    /// byte of every file is a cache that costs what it saves. Path, length and mtime miss only an
    /// edit that preserves both size and timestamp, which a tool does not do by accident.</para>
    ///
    /// <para>Returns empty when the scope's inputs cannot be enumerated, and an empty fingerprint
    /// never matches — so an unreadable scope is always re-read.</para>
    /// </remarks>
    public static string Compute(string rootPath, ScopeDescriptor scope)
    {
        // What the scope actually READS, which is not the same for every kind.
        //
        // A C# scope is a project directory: its extraction depends on every source file under it.
        // A Bicep scope is ONE TEMPLATE, and a schema scope is one Migrations directory. Treating a
        // single-file scope as its containing folder made two templates in one `infra/` directory
        // share a fingerprint basis, so deleting either invalidated both — over-invalidation, which
        // is safe but wrong, and it made "one scope departed" look like "everything changed".
        var single = scope.ScopeId.StartsWith("bicep:", StringComparison.Ordinal)
            && File.Exists(scope.ProjectPath);

        var target = single
            ? scope.ProjectPath
            : File.Exists(scope.ProjectPath)
                ? Path.GetDirectoryName(scope.ProjectPath) ?? rootPath
                : Directory.Exists(scope.ProjectPath) ? scope.ProjectPath : rootPath;

        var entries = new List<string>();

        try
        {
            foreach (var file in single ? [target] : Enumerate(target))
            {
                var info = new FileInfo(file);
                var name = single ? Path.GetFileName(file) : Path.GetRelativePath(target, file);
                entries.Add(string.Create(CultureInfo.InvariantCulture,
                    $"{name}|{info.Length}|{info.LastWriteTimeUtc.Ticks}"));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return string.Empty;
        }

        if (entries.Count == 0) return string.Empty;

        entries.Sort(StringComparer.Ordinal);

        var payload = Encoding.UTF8.GetBytes(
            ExtractorGeneration + "\n" + scope.TargetFramework + "\n" + string.Join("\n", entries));

        return Convert.ToHexStringLower(SHA256.HashData(payload));
    }

    private static IEnumerable<string> Enumerate(string directory)
    {
        var pending = new Stack<string>();
        pending.Push(directory);

        while (pending.Count > 0)
        {
            var current = pending.Pop();

            string[] files;
            try { files = Directory.GetFiles(current); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }

            foreach (var file in files) yield return file;

            IEnumerable<string> children;
            try { children = Directory.EnumerateDirectories(current); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }

            foreach (var child in children)
            {
                if (!Skip.Contains(Path.GetFileName(child))) pending.Push(child);
            }
        }
    }
}
