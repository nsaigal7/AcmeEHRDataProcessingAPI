namespace AcmeEHRDataProcessingAPI.Models;
public class AnalyticsResult
{
    public Dictionary<string, long> RecordsByResourceType { get; set; } = new();
    public long UniquePatients { get; set; }
    public ValidationErrorSummary ValidationErrorsSummary { get; set; } = new();    
    public Dictionary<string, string> AverageResponseTimeLast7Days { get; set; } = new();

}