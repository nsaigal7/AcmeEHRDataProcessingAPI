using AcmeEHRDataProcessingAPI.Models;
using AcmeEHRDataProcessingAPI.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AcmeEHRDataProcessingAPI.Services;
public class AnalyticsService
{
    private readonly FhirResourceStore _resourceStore; 
    public AnalyticsService(FhirResourceStore resourceStore)
    {
        _resourceStore = resourceStore;
    }

    public async Task<AnalyticsResult> GetAnalytics()
    {
        AnalyticsResult analyticsResult = new();
        
        // Individual records
        analyticsResult.RecordsByResourceType.Add("Patients", await _resourceStore.Patients.CountDocumentsAsync(Builders<FhirPatient>.Filter.Empty));
        analyticsResult.RecordsByResourceType.Add("Observations", await _resourceStore.Observations.CountDocumentsAsync(Builders<FhirObservation>.Filter.Empty));
        analyticsResult.RecordsByResourceType.Add("Conditions", await _resourceStore.Conditions.CountDocumentsAsync(Builders<FhirCondition>.Filter.Empty));
        analyticsResult.RecordsByResourceType.Add("Encounters", await _resourceStore.Encounters.CountDocumentsAsync(Builders<FhirEncounter>.Filter.Empty));
        analyticsResult.RecordsByResourceType.Add("MedicationRequests", await _resourceStore.MedicationRequests.CountDocumentsAsync(Builders<FhirMedicationRequest>.Filter.Empty));
        analyticsResult.RecordsByResourceType.Add("Procedures", await _resourceStore.Procedures.CountDocumentsAsync(Builders<FhirProcedure>.Filter.Empty));

        // Distinct Patient count
        var count = await _resourceStore.Patients
        .Aggregate()
        .Group(p => p.Id, g => new { Mrn = g.Key })
        .Count()
        .FirstOrDefaultAsync();
        analyticsResult.UniquePatients = count?.Count ?? 0;

        // Validation Errors 
        analyticsResult.ValidationErrorsSummary.CountByHardStopError = await GetCountByHardStopError();
        analyticsResult.ValidationErrorsSummary.CountByErrorType = await GetCountByErrorType();

        // Custom stat: average response time per day for the last 7 days
        analyticsResult.AverageResponseTimeLast7Days = await GetAverageRuntimePerDay();

        return analyticsResult;
    }

    #region Partially AI-generated
    public async Task<Dictionary<string, int>> GetCountByErrorType()
    {
        var pipeline = new[]
        {
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$ErrorType" },
                { "count", new BsonDocument("$sum", 1) }
            })
        };

        var results = await _resourceStore.ValidationErrors.Aggregate<BsonDocument>(pipeline).ToListAsync();

        return results.ToDictionary(
            doc => doc["_id"].AsString,
            doc => doc["count"].AsInt32
        );
    }
    public async Task<Dictionary<string, int>> GetCountByHardStopError()
    {
        var pipeline = new[]
        {
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$HardStopError" },
                { "count", new BsonDocument("$sum", 1) }
            })
        };

        var results = await _resourceStore.ValidationErrors.Aggregate<BsonDocument>(pipeline).ToListAsync();

        var tempDict = results.ToDictionary(
            doc => doc["_id"].AsBoolean,
            doc => doc["count"].AsInt32
        );
        return tempDict.ToDictionary(kvp => kvp.Key ? "HardStopErrors" : "WarningErrors", kvp => kvp.Value);
    }

    public async Task<Dictionary<string, string>> GetAverageRuntimePerDay()
    {
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7).Date;

        var pipeline = new[]
        {
            // Filter to last 7 days
            new BsonDocument("$match", new BsonDocument(
                "DateOfExecution", new BsonDocument
                {
                    { "$gte", new BsonDateTime(sevenDaysAgo) }
                }
            )),

            // Group by date string, average the elapsed ms
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument("$dateToString", new BsonDocument
                    {
                        { "format", "%Y-%m-%d" },
                        { "date", "$DateOfExecution" }
                    })
                },
                { "avgElapsedMs", new BsonDocument("$avg", "$ElapsedMilliseconds") }
            }),

            // Sort chronologically
            new BsonDocument("$sort", new BsonDocument("_id", 1))
        };

        var results = await _resourceStore.ApiMetrics
            .Aggregate<BsonDocument>(pipeline)
            .ToListAsync();

        return results.ToDictionary(
            doc => doc["_id"].AsString,                    
            doc =>  $"{doc["avgElapsedMs"].AsDouble:F2} ms"
        );   
    }
#endregion
}