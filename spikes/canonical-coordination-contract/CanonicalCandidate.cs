using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace CanonicalCoordinationSpike;

// Byte codec experiment, NOT an envelope validator or shipping reader.
// python-json-v1-dotnet-r-v1: qualified only by the retained finite oracle corpus.
public static class CanonicalCandidate
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static string Number(double value)
    {
        if (!double.IsFinite(value)) throw new CandidateInputException("XH.NONFINITE_JSON");
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
        var text = StrictUtf8.GetString(source);
        using var document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 128 });
        Check(document.RootElement, 1);
        return StrictUtf8.GetBytes(Encode(document.RootElement, excludeReceipt: true));
    }

    public static string Scope(string repository, string origin, string path, string epoch) =>
        string.Join("|", new[] { repository, origin, path, epoch }
            .Select(f => Convert.ToBase64String(StrictUtf8.GetBytes(f))));

    private static void Check(JsonElement value, int depth)
    {
        if (value.ValueKind is JsonValueKind.Object or JsonValueKind.Array && depth > 16)
            throw new CandidateInputException("XH.DEPTH_EXCEEDED");
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new CandidateInputException("XH.DUPLICATE_KEY");
                StrictUtf8.GetByteCount(property.Name);
                Check(property.Value, depth + 1);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray()) Check(item, depth + 1);
        }
        else if (value.ValueKind == JsonValueKind.String)
            StrictUtf8.GetByteCount(value.GetString()!);
        else if (value.ValueKind == JsonValueKind.Number)
            ScalarNumber(value);
    }

    private static string ScalarNumber(JsonElement value)
    {
        var raw = value.GetRawText();
        if (raw.IndexOfAny(['.', 'e', 'E']) >= 0)
            return Number(double.Parse(raw, CultureInfo.InvariantCulture));
        return BigInteger.Parse(raw, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
    }

    private static string Encode(JsonElement value, bool excludeReceipt = false) => value.ValueKind switch
    {
        JsonValueKind.Object => "{" + string.Join(",", value.EnumerateObject()
            .Where(p => !excludeReceipt || p.Name is not ("payloadDigest" or "recordedAt"))
            .OrderBy(p => p.Name, Comparer<string>.Create(CompareCodePoints))
            .Select(p => Quote(p.Name) + ":" + Encode(p.Value))) + "}",
        JsonValueKind.Array => "[" + string.Join(",", value.EnumerateArray().Select(v => Encode(v))) + "]",
        JsonValueKind.String => Quote(value.GetString()!),
        JsonValueKind.Number => ScalarNumber(value),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => "null",
        _ => throw new CandidateInputException("XH.SCHEMA_INVALID"),
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

public sealed class CandidateInputException(string code) : FormatException(code)
{
    public string Code { get; } = code;
}
