namespace AcmeEHRDataProcessingAPI.Models;

public class ApiMetric
{
    public double ElapsedMilliseconds { get; set; }
    public DateTime DateOfExecution {get; set; }
    public string? Request { get; set; }
}