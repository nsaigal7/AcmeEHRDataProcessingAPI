using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AcmeEHRDataProcessingAPI.Models;

public class ImportResult
{
    public int TotalLinesProcessed { get; set; }
    public int RecordsImportedSuccessfully { get; set; }
    public int RecordsSkipped { get; set; }
    public List<ValidationError> ValidationErrors { get; set; } = new();
    public List<DataQualityWarning> DataQualityWarnings { get; set; } = new();
    public ImportStatistics Statistics { get; set; } = new();
}

public class ValidationError
{
    public int LineNumber { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? RawContent { get; set; }
    
    [BsonRepresentation(BsonType.String)]
    public ValidationErrorTypeEnum ErrorType { get; set; }
    public bool HardStopError {get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ValidationErrorTypeEnum
{
    InvalidJson,
    MissingResourceField,
    MalformedData,
    UnsupportedResourceType,
}

public class ValidationErrorSummary
{
    public Dictionary<string, int> CountByErrorType {get; set;} = new();
    public Dictionary<string, int> CountByHardStopError {get; set;} = new();
}

public class DataQualityWarning
{
    public int LineNumber { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ImportStatistics
{
    public Dictionary<string, int> RecordsByType { get; set; } = new();
    public int UniquePatients { get; set; }
    public int TotalObservations { get; set; }
    public int TotalConditions { get; set; }
    public int TotalEncounters { get; set; }
    public int TotalMedicationRequests { get; set; }
    public int TotalProcedures { get; set; }
}