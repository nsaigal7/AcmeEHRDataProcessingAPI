using System.Text.Json.Serialization;

namespace AcmeEHRDataProcessingAPI.Models;

public class TransformRequest
{
    public List<string> ResourceTypes { get; set; } = new();

    public List<TransformationRule> Transformations { get; set; } = new();

    public TransformFilters? Filters { get; set; }
}

public class TransformationRule
{
    public string Action { get; set; } = string.Empty;

    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("as")]
    public string? As { get; set; }
}

public class TransformFilters : Dictionary<string, string> { }

public class TransformResult
{
    public List<Dictionary<string, object?>> Data { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}