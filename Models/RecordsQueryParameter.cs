namespace AcmeEHRDataProcessingAPI.Models;

public class RecordsQueryParameters
{
    public string? ResourceType { get; set; }
    public string? Subject { get; set; }
    public string? Fields { get; set; }

    public HashSet<string>? ParsedFields()
    {
        if (string.IsNullOrWhiteSpace(Fields))
            return null;

        return Fields.Split(',').Select(f => f.ToLowerInvariant()).ToHashSet();;

    }
}