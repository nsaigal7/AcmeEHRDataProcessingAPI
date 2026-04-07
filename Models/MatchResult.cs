namespace AcmeEHRDataProcessingAPI.Models;
public class MatchResult
{
    public string PatientId { get; set; }
    public int Score { get; set; }

    public MatchResult(string ptid, int sc)
    {
        PatientId = ptid;
        Score = sc;
    }
}