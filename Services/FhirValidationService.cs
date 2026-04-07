using AcmeEHRDataProcessingAPI.Models;

namespace AcmeEHRDataProcessingAPI.Services;

#region AI-generated
public class FhirValidationService
{
    private static readonly HashSet<string> ValidObservationStatuses =
        new() { "registered", "preliminary", "final", "amended", "corrected", "cancelled", "entered-in-error", "unknown" };

    private static readonly HashSet<string> ValidEncounterStatuses =
        new() { "planned", "arrived", "triaged", "in-progress", "onleave", "finished", "cancelled", "entered-in-error", "unknown" };

    private static readonly HashSet<string> ValidMedicationRequestStatuses =
        new() { "active", "on-hold", "cancelled", "completed", "entered-in-error", "stopped", "draft", "unknown" };

    private static readonly HashSet<string> ValidMedicationRequestIntents =
        new() { "proposal", "plan", "order", "original-order", "reflex-order", "filler-order", "instance-order", "option" };

    private static readonly HashSet<string> ValidProcedureStatuses =
        new() { "preparation", "in-progress", "not-done", "on-hold", "stopped", "completed", "entered-in-error", "unknown" };

    private static readonly HashSet<string> ValidGenders =
        new() { "male", "female", "other", "unknown" };

    /// <summary>
    /// Returns a list of hard validation errors. Records with these should be flagged but still imported (lenient mode).
    /// </summary>
    public List<string> Validate(FhirResource resource)
    {
        return resource switch
        {
            FhirPatient p => ValidatePatient(p),
            FhirObservation o => ValidateObservation(o),
            FhirCondition c => ValidateCondition(c),
            FhirEncounter e => ValidateEncounter(e),
            FhirMedicationRequest m => ValidateMedicationRequest(m),
            FhirProcedure p => ValidateProcedure(p),
            _ => new List<string> { $"Unsupported resource type: {resource.ResourceType}" }
        };
    }

    /// <summary>
    /// Returns data quality warnings for missing-but-expected fields.
    /// </summary>
    public List<string> Warn(FhirResource resource)
    {
        return resource switch
        {
            FhirPatient p => WarnPatient(p),
            FhirObservation o => WarnObservation(o),
            FhirCondition c => WarnCondition(c),
            FhirEncounter e => WarnEncounter(e),
            FhirMedicationRequest m => WarnMedicationRequest(m),
            FhirProcedure p => WarnProcedure(p),
            _ => new()
        };
    }

    // ── Validation rules ────────────────────────────────────────────────────

    private List<string> ValidatePatient(FhirPatient p)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(p.Id))
            errors.Add("Patient is missing required field: id");
        if (p.Gender != null && !ValidGenders.Contains(p.Gender.ToLower()))
            errors.Add($"Patient has invalid gender value: '{p.Gender}'. Must be one of: {string.Join(", ", ValidGenders)}");
        if (p.BirthDate != null && !IsValidDate(p.BirthDate))
            errors.Add($"Patient has invalid birthDate format: '{p.BirthDate}'. Expected YYYY-MM-DD");
        return errors;
    }

    private List<string> ValidateObservation(FhirObservation o)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(o.Id))
            errors.Add("Observation is missing required field: id");
        if (string.IsNullOrWhiteSpace(o.Status))
            errors.Add("Observation is missing required field: status");
        else if (!ValidObservationStatuses.Contains(o.Status.ToLower()))
            errors.Add($"Observation has invalid status: '{o.Status}'");
        if (o.Code == null)
            errors.Add("Observation is missing required field: code");
        if (o.Subject == null || string.IsNullOrWhiteSpace(o.Subject.Reference))
            errors.Add("Observation is missing required field: subject.reference");
        return errors;
    }

    private List<string> ValidateCondition(FhirCondition c)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(c.Id))
            errors.Add("Condition is missing required field: id");
        if (c.Subject == null || string.IsNullOrWhiteSpace(c.Subject.Reference))
            errors.Add("Condition is missing required field: subject.reference");
        if (c.Code == null)
            errors.Add("Condition is missing required field: code");
        return errors;
    }

    private List<string> ValidateEncounter(FhirEncounter e)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(e.Id))
            errors.Add("Encounter is missing required field: id");
        if (string.IsNullOrWhiteSpace(e.Status))
            errors.Add("Encounter is missing required field: status");
        else if (!ValidEncounterStatuses.Contains(e.Status.ToLower()))
            errors.Add($"Encounter has invalid status: '{e.Status}'");
        if (e.Subject == null || string.IsNullOrWhiteSpace(e.Subject.Reference))
            errors.Add("Encounter is missing required field: subject.reference");
        return errors;
    }

    private List<string> ValidateMedicationRequest(FhirMedicationRequest m)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(m.Id))
            errors.Add("MedicationRequest is missing required field: id");
        if (string.IsNullOrWhiteSpace(m.Status))
            errors.Add("MedicationRequest is missing required field: status");
        else if (!ValidMedicationRequestStatuses.Contains(m.Status.ToLower()))
            errors.Add($"MedicationRequest has invalid status: '{m.Status}'");
        if (string.IsNullOrWhiteSpace(m.Intent))
            errors.Add("MedicationRequest is missing required field: intent");
        else if (!ValidMedicationRequestIntents.Contains(m.Intent.ToLower()))
            errors.Add($"MedicationRequest has invalid intent: '{m.Intent}'");
        if (m.Subject == null || string.IsNullOrWhiteSpace(m.Subject.Reference))
            errors.Add("MedicationRequest is missing required field: subject.reference");
        if (m.MedicationCodeableConcept == null)
            errors.Add("MedicationRequest is missing required field: medicationCodeableConcept");
        return errors;
    }

    private List<string> ValidateProcedure(FhirProcedure p)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(p.Id))
            errors.Add("Procedure is missing required field: id");
        if (string.IsNullOrWhiteSpace(p.Status))
            errors.Add("Procedure is missing required field: status");
        else if (!ValidProcedureStatuses.Contains(p.Status.ToLower()))
            errors.Add($"Procedure has invalid status: '{p.Status}'");
        if (p.Subject == null || string.IsNullOrWhiteSpace(p.Subject.Reference))
            errors.Add("Procedure is missing required field: subject.reference");
        if (p.Code == null)
            errors.Add("Procedure is missing required field: code");
        return errors;
    }

    // ── Data quality warnings ────────────────────────────────────────────────

    private List<string> WarnPatient(FhirPatient p)
    {
        var warnings = new List<string>();
        if (p.Name == null || p.Name.Count == 0)
            warnings.Add("Patient is missing expected field: name");
        if (string.IsNullOrWhiteSpace(p.BirthDate))
            warnings.Add("Patient is missing expected field: birthDate");
        if (string.IsNullOrWhiteSpace(p.Gender))
            warnings.Add("Patient is missing expected field: gender");
        if (p.Identifier == null || p.Identifier.Count == 0)
            warnings.Add("Patient is missing expected field: identifier");
        return warnings;
    }

    private List<string> WarnObservation(FhirObservation o)
    {
        var warnings = new List<string>();
        if (string.IsNullOrWhiteSpace(o.EffectiveDateTime))
            warnings.Add("Observation is missing expected field: effectiveDateTime");
        if (o.ValueQuantity == null && o.ValueCodeableConcept == null && string.IsNullOrWhiteSpace(o.ValueString))
            warnings.Add("Observation has no value (valueQuantity, valueCodeableConcept, or valueString)");
        return warnings;
    }

    private List<string> WarnCondition(FhirCondition c)
    {
        var warnings = new List<string>();
        if (c.ClinicalStatus == null)
            warnings.Add("Condition is missing expected field: clinicalStatus");
        if (c.VerificationStatus == null)
            warnings.Add("Condition is missing expected field: verificationStatus");
        if (string.IsNullOrWhiteSpace(c.OnsetDateTime) && string.IsNullOrWhiteSpace(c.RecordedDate))
            warnings.Add("Condition is missing expected field: onsetDateTime or recordedDate");
        return warnings;
    }

    private List<string> WarnEncounter(FhirEncounter e)
    {
        var warnings = new List<string>();
        if (e.Period == null)
            warnings.Add("Encounter is missing expected field: period");
        if (e.Type == null || e.Type.Count == 0)
            warnings.Add("Encounter is missing expected field: type");
        return warnings;
    }

    private List<string> WarnMedicationRequest(FhirMedicationRequest m)
    {
        var warnings = new List<string>();
        if (string.IsNullOrWhiteSpace(m.AuthoredOn))
            warnings.Add("MedicationRequest is missing expected field: authoredOn");
        if (m.Requester == null)
            warnings.Add("MedicationRequest is missing expected field: requester");
        return warnings;
    }

    private List<string> WarnProcedure(FhirProcedure p)
    {
        var warnings = new List<string>();
        if (string.IsNullOrWhiteSpace(p.PerformedDateTime) && p.PerformedPeriod == null)
            warnings.Add("Procedure is missing expected field: performedDateTime or performedPeriod");
        return warnings;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static bool IsValidDate(string date) =>
        DateTime.TryParseExact(date, new[] { "yyyy-MM-dd", "yyyy-MM", "yyyy" },
            null, System.Globalization.DateTimeStyles.None, out _);
}
#endregion