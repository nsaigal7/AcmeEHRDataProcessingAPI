using System.Text.Json;
using System.Text.RegularExpressions;
using MongoDB.Driver;
using AcmeEHRDataProcessingAPI.Models;
using AcmeEHRDataProcessingAPI.Persistence;

namespace AcmeEHRDataProcessingAPI.Services;

public class TransformService
{
    private readonly FhirResourceStore _store;

    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    // Matches array indexing: "coding[0]" to field="coding", index=0
    private static readonly Regex ArrayIndexPattern = new(@"^(.+?)\[(\d+)\]$", RegexOptions.Compiled);

    public TransformService(FhirResourceStore store)
    {
        _store = store;
    }

    public async Task<TransformResult> TransformAsync(TransformRequest request)
    {
        var result = new TransformResult();
        var resourceTypes = request.ResourceTypes.Count > 0
            ? request.ResourceTypes
            : new List<string> { "Patient", "Observation", "Condition", "Encounter", "MedicationRequest", "Procedure" };


        // Fetch matching records from MongoDB
        var records = await FetchRecordsAsync(resourceTypes, request.Filters);

        // Serialize each to a JsonElement so we can traverse the record
        foreach (var resource in records)
        {
            var json = JsonSerializer.Serialize(resource, resource.GetType(), SerializeOptions);
            var doc = JsonDocument.Parse(json).RootElement;

            var output = new Dictionary<string, object?>();
            foreach (var prop in doc.EnumerateObject())
            {
                if (prop.Name == "mongoId") continue;
                output[prop.Name] = JsonElementToObject(prop.Value);
            }

            // Apply transformations
            var errors = new List<string>();
            foreach (var rule in request.Transformations)
            {
                ApplyRule(rule, doc, output, resource.Id ?? "unknown", errors);
            }

            result.Warnings.AddRange(errors);
            result.Data.Add(output);
        }

        return result;
    }

    private async Task<List<FhirResource>> FetchRecordsAsync(
        List<string> resourceTypes,
        TransformFilters? filters)
    {
        var records = new List<FhirResource>();

        foreach (var type in resourceTypes)
        {
            switch (type)
            {
                case "Patient":           await Collect(_store.Patients, filters, records);           break;
                case "Observation":       await Collect(_store.Observations, filters, records);       break;
                case "Condition":         await Collect(_store.Conditions, filters, records);         break;
                case "Encounter":         await Collect(_store.Encounters, filters, records);         break;
                case "MedicationRequest": await Collect(_store.MedicationRequests, filters, records); break;
                case "Procedure":         await Collect(_store.Procedures, filters, records);         break;
            }
        }

        return records;
    }

    private async Task Collect<T>(IMongoCollection<T> collection, TransformFilters? filters, List<FhirResource> records) where T : FhirResource
    {
        var mongoFilter = BuildMongoFilter<T>(filters);
        var docs = await collection.Find(mongoFilter).ToListAsync();
        records.AddRange(docs);
    }

    private static FilterDefinition<T> BuildMongoFilter<T>(TransformFilters? filters) where T : FhirResource
    {
        var combined = Builders<T>.Filter.Empty;

        if (filters is not { Count: > 0 })
            return combined;

        foreach (var (path, expectedValue) in filters)
        {
            // Convert bracket notation to MongoDB dot notation: e.g., "coding[0].code" becomes "coding.0.code"
            var mongoPath = Regex.Replace(path, @"\[(\d+)\]", ".$1");
            combined &= Builders<T>.Filter.Eq(mongoPath, expectedValue);
        }

        return combined;
    }

    private void ApplyRule(TransformationRule rule, JsonElement doc, Dictionary<string, object?> output, string recordId, List<string> errors)
    {
        switch (rule.Action.ToLowerInvariant())
        {
            case "flatten":
                ApplyFlatten(rule, doc, output, recordId, errors);
                break;

            case "extract":
                ApplyExtract(rule, doc, output, recordId, errors);
                break;

            default:
                errors.Add($"[{recordId}] Unknown action '{rule.Action}'. Supported: flatten and extract.");
                break;
        }
    }

    private void ApplyFlatten(TransformationRule rule, JsonElement doc, Dictionary<string, object?> output, 
        string recordId, List<string> errors)
    {
        var node = ResolvePath(doc, rule.Field);
        if (node == null)
        {
            errors.Add($"[{recordId}] flatten: path '{rule.Field}' not found or is null.");
            return;
        }

        if (node.Value.ValueKind != JsonValueKind.Object)
        {
            errors.Add($"[{recordId}] flatten: path '{rule.Field}' resolved to a {node.Value.ValueKind}, expected an Object.");
            return;
        }

        // Derive a prefix from the field path: "code.coding[0]" should become "code"
        var prefix = rule.Field.Split('.')[0].Split('[')[0];
        
        // remove the original prefix from the output so it can be flattened into the output.
        output.Remove(prefix);

        // Spread the object's properties into the root
        FlattenObject(node.Value, prefix, output);
    }

    private static void FlattenObject(JsonElement element, string prefix, Dictionary<string, object?> output)
    {
        foreach (var prop in element.EnumerateObject())
        {
            var key = $"{prefix}_{prop.Name}";
            if (prop.Value.ValueKind == JsonValueKind.Object)
                FlattenObject(prop.Value, key, output);  // recurse into nested objects
            else
                output[key] = JsonElementToObject(prop.Value);
        }
    }

    private void ApplyExtract(TransformationRule rule, JsonElement doc, Dictionary<string, object?> output,
        string recordId, List<string> errors)
    {
        var node = ResolvePath(doc, rule.Field);
        if (node == null)
        {
            errors.Add($"[{recordId}] extract: path '{rule.Field}' not found or is null.");
            return;
        }

        // Determine output key: use "as" alias, or fall back to last path segment
        var outputKey = !string.IsNullOrWhiteSpace(rule.As)
            ? rule.As
            : rule.Field.Split('.').Last().Split('[')[0];

        // Remove the top-level key for the first segment
        var topKey = rule.Field.Split('.')[0].Split('[')[0];
        output.Remove(topKey);

        output[outputKey] = JsonElementToObject(node.Value);
    }

    #region AI-generated
    private static JsonElement? ResolvePath(JsonElement root, string path)
    {
        var current = root;
        var segments = path.Split('.');

        foreach (var segment in segments)
        {
            var arrayMatch = ArrayIndexPattern.Match(segment);

            if (arrayMatch.Success)
            {
                // e.g. "coding[0]"
                var fieldName = arrayMatch.Groups[1].Value;
                var index = int.Parse(arrayMatch.Groups[2].Value);

                if (!current.TryGetProperty(fieldName, out var arrayProp)) return null;
                if (arrayProp.ValueKind != JsonValueKind.Array) return null;

                var items = arrayProp.EnumerateArray().ToList();
                if (index >= items.Count) return null;

                current = items[index];
            }
            else
            {
                // Plain field name
                if (!current.TryGetProperty(segment, out var next)) return null;
                current = next;
            }
        }

        return current.ValueKind == JsonValueKind.Null ? null : current;
    }

    // ── JsonElement → CLR object ──────────────────────────────────────────────

    private static object? JsonElementToObject(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String  => el.GetString(),
        JsonValueKind.Number  => el.TryGetInt64(out var l) ? l : el.GetDouble(),
        JsonValueKind.True    => true,
        JsonValueKind.False   => false,
        JsonValueKind.Null    => null,
        JsonValueKind.Array   => el.EnumerateArray().Select(JsonElementToObject).ToList(),
        JsonValueKind.Object  => el.EnumerateObject().ToDictionary(p => p.Name, p => JsonElementToObject(p.Value)),
        _ => el.GetRawText()
    };
    #endregion
}