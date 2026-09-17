using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace AiDe.Core.Watcher;

// Admission requires CanonicalCoordinationRecord.Parse, not merely this internal codec.
// python-json-v1-dotnet-r-v1 is qualified only by the pinned finite oracle.
internal static class CanonicalCoordinationCodec
{
    internal const string Version = "python-json-v1-dotnet-r-v1";
    internal const int MaxContentBytes = 65536;
    internal const int MaxRawBytes = MaxContentBytes + 2;
    internal const int MaxIntegerDigits = 4300;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static string Number(double value)
    {
        if (!double.IsFinite(value)) throw new CanonicalInputException(CanonicalErrors.Nonfinite);
        var sign = double.IsNegative(value) ? "-" : "";
        if (0 == value) return sign + "0.0";
        var parts = Math.Abs(value).ToString("R", CultureInfo.InvariantCulture).Split('E');
        var point = parts[0].IndexOf('.');
        if (point < 0) point = parts[0].Length;
        var rawDigits = parts[0].Replace(".", "");
        var leading = rawDigits.Length - rawDigits.TrimStart('0').Length;
        var exponent = point - leading - 1
            + (parts.Length == 2 ? int.Parse(parts[1], CultureInfo.InvariantCulture) : 0);
        var digits = rawDigits.TrimStart('0').TrimEnd('0');
        if (exponent is < -4 or >= 16)
        {
            var fraction = digits.Length > 1 ? "." + digits[1..] : "";
            return sign + digits[0] + fraction + "e" + (exponent < 0 ? "-" : "+")
                + Math.Abs(exponent).ToString("D2", CultureInfo.InvariantCulture);
        }
        if (exponent < 0) return sign + "0." + new string('0', -exponent - 1) + digits;
        if (exponent + 1 >= digits.Length)
            return sign + digits + new string('0', exponent + 1 - digits.Length) + ".0";
        return sign + digits.Insert(exponent + 1, ".");
    }

    public static byte[] Bytes(byte[] source)
    {
        using var document = Read(source);
        return DigestBytes(document.RootElement);
    }

    internal static byte[] DigestBytes(JsonElement value) =>
        StrictUtf8.GetBytes(Encode(value, excludeReceipt: true));

    internal static int FullSize(JsonElement value) =>
        StrictUtf8.GetByteCount(Encode(value, spaced: true));

    internal static JsonDocument Read(ReadOnlySpan<byte> source)
    {
        if (source.Length > MaxRawBytes) throw new CanonicalInputException(CanonicalErrors.TooLarge);
        if (source.EndsWith("\n"u8)) source = source[..^1];
        if (source.EndsWith("\r"u8)) source = source[..^1];
        if (source.Length > MaxContentBytes) throw new CanonicalInputException(CanonicalErrors.TooLarge);
        _ = StrictUtf8.GetCharCount(source);
        Scan(source);
        return JsonDocument.Parse(source.ToArray(), new JsonDocumentOptions { MaxDepth = 17 });
    }

    public static string Scope(string repository, string origin, string path, string epoch) =>
        string.Join("|", new[] { repository, origin, path, epoch }
            .Select(f => Convert.ToBase64String(StrictUtf8.GetBytes(f))));

    private static void Scan(ReadOnlySpan<byte> source)
    {
        var reader = new Utf8JsonReader(source, new JsonReaderOptions { MaxDepth = 17 });
        var objects = new Stack<HashSet<string>>();
        var nodes = 0;
        try
        {
            while (reader.Read())
            {
                // Each token needs a source byte; this is not a narrower schema limit.
                if (++nodes > MaxContentBytes) throw new CanonicalInputException(CanonicalErrors.TooLarge);
                if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray
                    && reader.CurrentDepth >= 16)
                    throw new CanonicalInputException(CanonicalErrors.Depth);
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartObject: objects.Push(new(StringComparer.Ordinal)); break;
                    case JsonTokenType.EndObject: objects.Pop(); break;
                    case JsonTokenType.PropertyName:
                        var name = reader.GetString()!;
                        _ = StrictUtf8.GetByteCount(name);
                        if (!objects.Peek().Add(name))
                            throw new CanonicalInputException(CanonicalErrors.Duplicate);
                        break;
                    case JsonTokenType.String:
                        _ = StrictUtf8.GetByteCount(reader.GetString()!);
                        break;
                    case JsonTokenType.Number:
                        var raw = reader.ValueSpan;
                        if (raw.IndexOfAny((byte)'.', (byte)'e', (byte)'E') >= 0)
                        {
                            if (!reader.TryGetDouble(out var number) || !double.IsFinite(number))
                                throw new CanonicalInputException(CanonicalErrors.Nonfinite);
                        }
                        else if (raw.Length - (raw[0] == '-' ? 1 : 0) > MaxIntegerDigits)
                            throw new CanonicalInputException(CanonicalErrors.IntegerUnsupported);
                        break;
                }
            }
        }
        catch (JsonException)
        {
            var tail = source[(int)reader.BytesConsumed..].TrimStart(" \r\n\t"u8);
            if (tail.StartsWith("NaN"u8) || tail.StartsWith("Infinity"u8) || tail.StartsWith("-Infinity"u8))
                throw new CanonicalInputException(CanonicalErrors.Nonfinite);
            throw;
        }
    }

    private static string ScalarNumber(JsonElement value)
    {
        var raw = value.GetRawText();
        if (raw.IndexOfAny(['.', 'e', 'E']) >= 0)
            return Number(double.Parse(raw, CultureInfo.InvariantCulture));
        return BigInteger.Parse(raw, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
    }

    private static string Encode(JsonElement value, bool excludeReceipt = false, bool spaced = false) => value.ValueKind switch
    {
        JsonValueKind.Object => "{" + string.Join(spaced ? ", " : ",", value.EnumerateObject()
            .Where(p => !excludeReceipt || p.Name is not ("payloadDigest" or "recordedAt"))
            .OrderBy(p => p.Name, Comparer<string>.Create(CompareCodePoints))
            .Select(p => Quote(p.Name) + (spaced ? ": " : ":") + Encode(p.Value, spaced: spaced))) + "}",
        JsonValueKind.Array => "[" + string.Join(spaced ? ", " : ",",
            value.EnumerateArray().Select(v => Encode(v, spaced: spaced))) + "]",
        JsonValueKind.String => Quote(value.GetString()!),
        JsonValueKind.Number => ScalarNumber(value),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => "null",
        _ => throw new CanonicalInputException(CanonicalErrors.Schema),
    };

    private static int CompareCodePoints(string a, string b)
    {
        var left = a.EnumerateRunes().GetEnumerator();
        var right = b.EnumerateRunes().GetEnumerator();
        while (left.MoveNext())
        {
            if (!right.MoveNext()) return 1;
            var difference = left.Current.Value.CompareTo(right.Current.Value);
            if (0 != difference) return difference;
        }
        return right.MoveNext() ? -1 : 0;
    }

    private static string Quote(string value)
    {
        var output = new StringBuilder("\"");
        foreach (var character in value)
        {
            output.Append(character switch
            {
                '"' => "\\\"", '\\' => "\\\\", '\b' => "\\b", '\f' => "\\f",
                '\n' => "\\n", '\r' => "\\r", '\t' => "\\t",
                < ' ' => "\\u" + ((int)character).ToString("x4", CultureInfo.InvariantCulture),
                _ => character.ToString(),
            });
        }
        return output.Append('"').ToString();
    }
}

internal sealed class CanonicalInputException(string code) : FormatException(code)
{
    public string Code { get; } = code;
}

internal static class CanonicalErrors
{
    internal const string Schema = "XH.SCHEMA_INVALID";
    internal const string TooLarge = "XH.RECORD_TOO_LARGE";
    internal const string Duplicate = "XH.DUPLICATE_KEY";
    internal const string Nonfinite = "XH.NONFINITE_JSON";
    internal const string Depth = "XH.DEPTH_EXCEEDED";
    internal const string Digest = "XH.DIGEST_MISMATCH";
    internal const string Field = "XH.FIELD_INVALID";
    internal const string Unsupported = "XH.UNSUPPORTED_VERSION";
    internal const string IntegerUnsupported = "XH.INTEGER_LIMIT_UNSUPPORTED";
}
