using System.Text.Json.Serialization;
using AcmeEHRDataProcessingAPI.Converters;

namespace AcmeEHRDataProcessingAPI.Models;

#region AI-generated
public class ExtractionConfig
{
    // Universal fields
    [JsonPropertyName("id")]
    public ExtractionRule? Id { get; set; }

    [JsonPropertyName("resourceType")]
    public ExtractionRule? ResourceType { get; set; }

    [JsonPropertyName("subject")]
    public ExtractionRule? Subject { get; set; }

    [JsonPropertyName("code")]
    public ExtractionRule? Code { get; set; }

    [JsonPropertyName("status")]
    public ExtractionRule? Status { get; set; }

    // Resource-specific fields
    [JsonPropertyName("effectiveDateTime")]
    public ExtractionRule? EffectiveDateTime { get; set; }

    [JsonPropertyName("performedDateTime")]
    public ExtractionRule? PerformedDateTime { get; set; }

    [JsonPropertyName("dosageInstruction")]
    public ExtractionRule? DosageInstruction { get; set; }

    [JsonPropertyName("valueQuantity")]
    public ExtractionRule? ValueQuantity { get; set; }

    [JsonPropertyName("valueCodeableConcept")]
    public ExtractionRule? ValueCodeableConcept { get; set; }

    [JsonPropertyName("valueString")]
    public ExtractionRule? ValueString { get; set; }

    [JsonPropertyName("birthDate")]
    public ExtractionRule? BirthDate { get; set; }

    [JsonPropertyName("gender")]
    public ExtractionRule? Gender { get; set; }

    [JsonPropertyName("name")]
    public ExtractionRule? Name { get; set; }

    [JsonPropertyName("onsetDateTime")]
    public ExtractionRule? OnsetDateTime { get; set; }

    [JsonPropertyName("clinicalStatus")]
    public ExtractionRule? ClinicalStatus { get; set; }

    [JsonPropertyName("period")]
    public ExtractionRule? Period { get; set; }

    [JsonPropertyName("authoredOn")]
    public ExtractionRule? AuthoredOn { get; set; }

    [JsonPropertyName("intent")]
    public ExtractionRule? Intent { get; set; }
    #endregion AI-generated

    public bool ShouldExtract(ExtractionRule? rule, string resourceType)
    {
        if (rule == null) return false;
        if (rule.IsAll) return true;
        return rule.ResourceTypes?.Contains(resourceType, StringComparer.OrdinalIgnoreCase) ?? false;
    }
}

[JsonConverter(typeof(ExtractionRuleConverter))]
public class ExtractionRule
{
    public bool IsAll { get; set; }
    public List<string>? ResourceTypes { get; set; }

    public static ExtractionRule All() 
    { 
        return new ExtractionRule { IsAll = true };
    }
    public static ExtractionRule Some(List<string> types)
    {
        return new ExtractionRule { IsAll = false,  
            ResourceTypes = types 
        };
    }
    public static ExtractionRule Some(string type)
    {
        return new ExtractionRule { IsAll = false, 
            ResourceTypes = new List<string> { type }
        };
    }
}