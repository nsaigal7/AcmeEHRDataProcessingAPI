using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AcmeEHRDataProcessingAPI.Models;

#region AI-generated
public class FhirResource
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? MongoId { get; set; }  // MongoDB internal _id (auto-generated)

    public string? ResourceType { get; set; }
    public string? Id { get; set; }        // FHIR resource id
}

public class FhirPatient : FhirResource
{
    public List<FhirName>? Name { get; set; }
    public string? Gender { get; set; }
    public string? BirthDate { get; set; }
    public List<FhirIdentifier>? Identifier { get; set; }
    public List<FhirContactPoint>? Telecom { get; set; }
    public List<FhirAddress>? Address { get; set; }
}

public class FhirObservation : FhirResource
{
    public string? Status { get; set; }
    public FhirCodeableConcept? Code { get; set; }
    public FhirReference? Subject { get; set; }
    public string? EffectiveDateTime { get; set; }
    public FhirQuantity? ValueQuantity { get; set; }
    public FhirCodeableConcept? ValueCodeableConcept { get; set; }
    public string? ValueString { get; set; }
}

public class FhirCondition : FhirResource
{
    public FhirReference? Subject { get; set; }
    public FhirCodeableConcept? Code { get; set; }
    public FhirCodeableConcept? ClinicalStatus { get; set; }
    public FhirCodeableConcept? VerificationStatus { get; set; }
    public string? OnsetDateTime { get; set; }
    public string? RecordedDate { get; set; }
}

public class FhirEncounter : FhirResource
{
    public string? Status { get; set; }
    public FhirReference? Subject { get; set; }
    public FhirCodeableConcept? Class { get; set; }
    public FhirPeriod? Period { get; set; }
    public List<FhirCodeableConcept>? Type { get; set; }
}

public class FhirMedicationRequest : FhirResource
{
    public string? Status { get; set; }
    public string? Intent { get; set; }
    public FhirReference? Subject { get; set; }
    public FhirCodeableConcept? MedicationCodeableConcept { get; set; }
    public string? AuthoredOn { get; set; }
    public FhirReference? Requester { get; set; }
    public List<FhirDosage>? DosageInstruction { get; set; }
}

public class FhirDosage
{
    public string? Text { get; set; }
    public FhirQuantity? DoseQuantity { get; set; }
    public string? Timing { get; set; }
    public string? Route { get; set; }
}

public class FhirProcedure : FhirResource
{
    public string? Status { get; set; }
    public FhirReference? Subject { get; set; }
    public FhirCodeableConcept? Code { get; set; }
    public string? PerformedDateTime { get; set; }
    public FhirPeriod? PerformedPeriod { get; set; }
}

// --- Shared sub-types ---

public class FhirName
{
    public string? Family { get; set; }
    public List<string>? Given { get; set; }
    public string? Use { get; set; }
}

public class FhirIdentifier
{
    public string? System { get; set; }
    public string? Value { get; set; }
}

public class FhirContactPoint
{
    public string? System { get; set; }
    public string? Value { get; set; }
}

public class FhirAddress
{
    public List<string>? Line { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
}

public class FhirCodeableConcept
{
    public List<FhirCoding>? Coding { get; set; }
    public string? Text { get; set; }
}

public class FhirCoding
{
    public string? System { get; set; }
    public string? Code { get; set; }
    public string? Display { get; set; }
}

public class FhirReference
{
    public string? Reference { get; set; }
    public string? Display { get; set; }
}

public class FhirQuantity
{
    public decimal? Value { get; set; }
    public string? Unit { get; set; }
    public string? System { get; set; }
    public string? Code { get; set; }
}

public class FhirPeriod
{
    public string? Start { get; set; }
    public string? End { get; set; }
}

#endregion