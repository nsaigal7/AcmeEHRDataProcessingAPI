using MongoDB.Driver;
using AcmeEHRDataProcessingAPI.Models;
using Microsoft.Extensions.Options;

namespace AcmeEHRDataProcessingAPI.Persistence;

public class FhirResourceStore
{
    private readonly IMongoDatabase _database;

    public IMongoCollection<FhirPatient> Patients { get; }
    public IMongoCollection<FhirObservation> Observations { get; }
    public IMongoCollection<FhirCondition> Conditions { get; }
    public IMongoCollection<FhirEncounter> Encounters { get; }
    public IMongoCollection<FhirMedicationRequest> MedicationRequests { get; }
    public IMongoCollection<FhirProcedure> Procedures { get; }
    public IMongoCollection<ValidationError> ValidationErrors { get; }
    public IMongoCollection<ApiMetric> ApiMetrics { get; }

    public FhirResourceStore(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);

        Patients = _database.GetCollection<FhirPatient>("patients");
        Observations = _database.GetCollection<FhirObservation>("observations");
        Conditions = _database.GetCollection<FhirCondition>("conditions");
        Encounters = _database.GetCollection<FhirEncounter>("encounters");
        MedicationRequests = _database.GetCollection<FhirMedicationRequest>("medicationRequests");
        Procedures = _database.GetCollection<FhirProcedure>("procedures");
        ValidationErrors = _database.GetCollection<ValidationError>("validationErrors");
        ApiMetrics = _database.GetCollection<ApiMetric>("apiMetrics");
    }
}