namespace AcmeEHRDataProcessingAPI.Models;
 
public class IdentityConfig
{
    public Dictionary<PatientFieldEnum, MatchTypeAndWeight> WeightParameters {get; set; } = new();
    public int MinWeight { get; set; }
    public int MaxWeight {get; set; }
}

public class MatchTypeAndWeight
{
    public MatchTypeEnum MatchType { get; set; }
    public int Weight { get; set; }
}

public enum MatchTypeEnum
{
    Partial,
    Exact
}

public enum PatientFieldEnum
{
    Name,
    Gender,
    BirthDate,
    Telecom,
    Address  
}