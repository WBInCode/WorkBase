using System.Text.Json;

namespace WorkBase.Modules.Workflow.Application.Services;

public interface IConditionEvaluator
{
    bool Evaluate(string expression, string? contextJson);
}

public sealed class SimpleConditionEvaluator : IConditionEvaluator
{
    public bool Evaluate(string expression, string? contextJson)
    {
        if (string.IsNullOrWhiteSpace(expression)) return true;
        if (string.IsNullOrWhiteSpace(contextJson)) return false;

        try
        {
            using var doc = JsonDocument.Parse(contextJson);
            var root = doc.RootElement;

            // Support simple expressions: "field operator value"
            // e.g. "Amount > 10000", "Status == Approved", "DaysRequested <= 5"
            var parts = expression.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) return false;

            var field = parts[0];
            var op = parts[1];
            var expected = parts[2];

            if (!root.TryGetProperty(field, out var prop)) return false;

            return op switch
            {
                "==" or "=" => GetStringValue(prop) == expected,
                "!=" => GetStringValue(prop) != expected,
                ">" => TryGetDouble(prop, out var v1) && double.TryParse(expected, out var e1) && v1 > e1,
                ">=" => TryGetDouble(prop, out var v2) && double.TryParse(expected, out var e2) && v2 >= e2,
                "<" => TryGetDouble(prop, out var v3) && double.TryParse(expected, out var e3) && v3 < e3,
                "<=" => TryGetDouble(prop, out var v4) && double.TryParse(expected, out var e4) && v4 <= e4,
                "contains" => GetStringValue(prop).Contains(expected, StringComparison.OrdinalIgnoreCase),
                "in" => expected.Split(',').Contains(GetStringValue(prop)),
                _ => false,
            };
        }
        catch
        {
            return false;
        }
    }

    private static string GetStringValue(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString() ?? "",
        JsonValueKind.Number => el.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => el.GetRawText(),
    };

    private static bool TryGetDouble(JsonElement el, out double value)
    {
        if (el.ValueKind == JsonValueKind.Number) { value = el.GetDouble(); return true; }
        if (el.ValueKind == JsonValueKind.String) return double.TryParse(el.GetString(), out value);
        value = 0; return false;
    }
}
