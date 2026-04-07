using System.Text.Json;
using AcmeEHRDataProcessingAPI.Models;
using AcmeEHRDataProcessingAPI.Persistence;

namespace AcmeEHRDataProcessingAPI.Services;

public class FhirImportService
{
    private readonly FhirResourceStore _store;
    private readonly FhirValidationService _validator;
    private readonly ExtractionService _extractionService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public FhirImportService(FhirResourceStore store, FhirValidationService validator, ExtractionService extractionService)
    {
        _store = store;
        _validator = validator;
        _extractionService = extractionService;
    }

    public async Task<ImportResult> ImportAsync(Stream stream, ExtractionConfig? extractionConfig = null)
    {
        var result = new ImportResult();
        var patientIds = new HashSet<string>();

        using var reader = new StreamReader(stream);
        int lineNumber = 0;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            result.TotalLinesProcessed++;

            // Parse as generic JSON to determine resourceType
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(line);
            }
            catch (JsonException ex)
            {
                result.ValidationErrors.Add(new ValidationError
                {
                    ErrorType =  ValidationErrorTypeEnum.InvalidJson,
                    LineNumber = lineNumber,
                    Reason = $"Invalid JSON: {ex.Message}",
                    RawContent = line.Length > 200 ? line[..200] + "..." : line,
                    HardStopError = true
                });
                result.RecordsSkipped++;
                continue;
            }

            if (!doc.RootElement.TryGetProperty("resourceType", out var resourceTypeProp))
            {
                result.ValidationErrors.Add(new ValidationError
                {
                    ErrorType =  ValidationErrorTypeEnum.MissingResourceField,
                    LineNumber = lineNumber,
                    Reason = "Missing required field: resourceType",
                    RawContent = line.Length > 200 ? line[..200] + "..." : line,
                    HardStopError = true
                });
                result.RecordsSkipped++;
                continue;
            }

            var resourceType = resourceTypeProp.GetString();

            // Deserialize into the correct typed model
            FhirResource? resource = resourceType switch
            {
                "Patient" => JsonSerializer.Deserialize<FhirPatient>(line, JsonOptions),
                "Observation" => JsonSerializer.Deserialize<FhirObservation>(line, JsonOptions),
                "Condition" => JsonSerializer.Deserialize<FhirCondition>(line, JsonOptions),
                "Encounter" => JsonSerializer.Deserialize<FhirEncounter>(line, JsonOptions),
                "MedicationRequest" => JsonSerializer.Deserialize<FhirMedicationRequest>(line, JsonOptions),
                "Procedure" => JsonSerializer.Deserialize<FhirProcedure>(line, JsonOptions),
                _ => null
            };

            if (resource == null)
            {
                result.ValidationErrors.Add(new ValidationError
                {
                    ErrorType =  ValidationErrorTypeEnum.UnsupportedResourceType,
                    LineNumber = lineNumber,
                    Reason = $"Unsupported resourceType: '{resourceType}'",
                    HardStopError = true
                });
                result.RecordsSkipped++;
                continue;
            }

            // Validate
            var errors = _validator.Validate(resource);
            foreach (var error in errors)
            {
                result.ValidationErrors.Add(new ValidationError
                {
                    ErrorType =  ValidationErrorTypeEnum.MalformedData,
                    LineNumber = lineNumber,
                    Reason = error,
                    RawContent = null,
                    HardStopError = false
                });
            }

            //  quality warnings
            var warnings = _validator.Warn(resource);
            foreach (var warning in warnings)
            {
                result.DataQualityWarnings.Add(new DataQualityWarning
                {
                    LineNumber = lineNumber,
                    ResourceType = resourceType ?? "Unknown",
                    ResourceId = resource.Id,
                    Message = warning
                });
            }

            // Apply extraction config
            resource = _extractionService.Apply(resource, extractionConfig);

            // Store the record and update statistics
            await StoreAndTrackAsync(resource, result.Statistics, patientIds);
            result.RecordsImportedSuccessfully++;
        }

        // Store validation errors for analytics
        await StoreValidationErrors(result.ValidationErrors);

        result.Statistics.UniquePatients = patientIds.Count;
        return result;
    }

    private async Task StoreValidationErrors(List<ValidationError> validationErrors)
    {
        if (validationErrors.Count > 0)
        {
            await _store.ValidationErrors.InsertManyAsync(validationErrors);
        }
    }

    private async Task StoreAndTrackAsync(FhirResource resource, ImportStatistics stats, HashSet<string> patientIds)
    {
        var type = resource.ResourceType ?? "Unknown";
        stats.RecordsByType.TryGetValue(type, out var count);
        stats.RecordsByType[type] = count + 1;

        switch (resource)
        {
            case FhirPatient patient:
                await _store.Patients.InsertOneAsync(patient);
                if (!string.IsNullOrWhiteSpace(patient.Id))
                    patientIds.Add(patient.Id);
                break;

            case FhirObservation observation:
                await _store.Observations.InsertOneAsync(observation);
                stats.TotalObservations++;
                var obsPatientRef = observation.Subject?.Reference;
                if (!string.IsNullOrWhiteSpace(obsPatientRef))
                    patientIds.Add(NormalizePatientRef(obsPatientRef));
                break;

            case FhirCondition condition:
                await _store.Conditions.InsertOneAsync(condition);
                stats.TotalConditions++;
                var condPatientRef = condition.Subject?.Reference;
                if (!string.IsNullOrWhiteSpace(condPatientRef))
                    patientIds.Add(NormalizePatientRef(condPatientRef));
                break;

            case FhirEncounter encounter:
                await _store.Encounters.InsertOneAsync(encounter);
                stats.TotalEncounters++;
                var encPatientRef = encounter.Subject?.Reference;
                if (!string.IsNullOrWhiteSpace(encPatientRef))
                    patientIds.Add(NormalizePatientRef(encPatientRef));
                break;

            case FhirMedicationRequest med:
                await _store.MedicationRequests.InsertOneAsync(med);
                stats.TotalMedicationRequests++;
                var medPatientRef = med.Subject?.Reference;
                if (!string.IsNullOrWhiteSpace(medPatientRef))
                    patientIds.Add(NormalizePatientRef(medPatientRef));
                break;

            case FhirProcedure procedure:
                await _store.Procedures.InsertOneAsync(procedure);
                stats.TotalProcedures++;
                var procPatientRef = procedure.Subject?.Reference;
                if (!string.IsNullOrWhiteSpace(procPatientRef))
                    patientIds.Add(NormalizePatientRef(procPatientRef));
                break;
        }
    }

    // Normalize "Patient/abc123" to "abc123" for deduplication
    private static string NormalizePatientRef(string reference)
    {
        var parts = reference.Split('/');
        return parts.Length >= 2 ? parts[^1] : reference;
    }
}