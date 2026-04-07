using AcmeEHRDataProcessingAPI.Models;

namespace AcmeEHRDataProcessingAPI.Services;
public class ExtractionService
{
    public FhirResource Apply(FhirResource resource, ExtractionConfig? config)
    {
        if (config == null) { return resource; }
        var resourceType = resource.ResourceType ?? string.Empty;
        
        // Remove fields common to all resource types
        if (!config.ShouldExtract(config.Id, resourceType))
        {
            resource.Id = null;
        }

        // Remove resource-specific fields
        switch (resource)
        {
            case FhirPatient patient:
                ApplyToPatient(patient, config, resourceType);
                break;
            case FhirObservation obs:
                ApplyToObservation(obs, config, resourceType);
                break;
            case FhirCondition cond:
                ApplyToCondition(cond, config, resourceType);
                break;
            case FhirEncounter enc:
                ApplyToEncounter(enc, config, resourceType);
                break;
            case FhirMedicationRequest med:
                ApplyToMedicationRequest(med, config, resourceType);
                break;
            case FhirProcedure proc:
                ApplyToProcedure(proc, config, resourceType);
                break;
        }

        return resource;
    }

#region AI-generated
    private void ApplyToPatient(FhirPatient p, ExtractionConfig c, string rt)
    {
        if (!c.ShouldExtract(c.Subject, rt)) { /* Patient has no subject */ }
        if (!c.ShouldExtract(c.Code, rt))    { /* Patient has no code */ }
        if (!c.ShouldExtract(c.Status, rt))  { /* Patient has no status */ }

        if (!c.ShouldExtract(c.Name, rt))      { p.Name = null;  }
        if (!c.ShouldExtract(c.Gender, rt))    { p.Gender = null; }
        if (!c.ShouldExtract(c.BirthDate, rt)) { p.BirthDate = null; }
    }

    private void ApplyToObservation(FhirObservation o, ExtractionConfig c, string rt)
    {
        if (!c.ShouldExtract(c.Subject, rt))          { o.Subject = null; }
        if (!c.ShouldExtract(c.Code, rt))             { o.Code = null;}
        if (!c.ShouldExtract(c.Status, rt))           { o.Status = null;}
        if (!c.ShouldExtract(c.EffectiveDateTime, rt)){ o.EffectiveDateTime = null;  }
        if (!c.ShouldExtract(c.ValueQuantity, rt))    { o.ValueQuantity = null; }
        if (!c.ShouldExtract(c.ValueCodeableConcept, rt)) { o.ValueCodeableConcept = null; }
        if (!c.ShouldExtract(c.ValueString, rt))      { o.ValueString = null; }
    }

    private void ApplyToCondition(FhirCondition cond, ExtractionConfig c, string rt)
    {
        if (!c.ShouldExtract(c.Subject, rt))       { cond.Subject = null;  }
        if (!c.ShouldExtract(c.Code, rt))          { cond.Code = null;     }
        if (!c.ShouldExtract(c.Status, rt))        { cond.ClinicalStatus = null;  }
        if (!c.ShouldExtract(c.ClinicalStatus, rt)){ cond.ClinicalStatus = null;  }
        if (!c.ShouldExtract(c.OnsetDateTime, rt)) { cond.OnsetDateTime = null; }
    }

    private void ApplyToEncounter(FhirEncounter enc, ExtractionConfig c, string rt)
    {
        if (!c.ShouldExtract(c.Subject, rt)) { enc.Subject = null;  }
        if (!c.ShouldExtract(c.Status, rt))  { enc.Status = null;   }
        if (!c.ShouldExtract(c.Period, rt))  { enc.Period = null;   }
    }

    private void ApplyToMedicationRequest(FhirMedicationRequest med, ExtractionConfig c, string rt)
    {
        if (!c.ShouldExtract(c.Subject, rt))          { med.Subject = null; }
        if (!c.ShouldExtract(c.Status, rt))           { med.Status = null; }
        if (!c.ShouldExtract(c.Intent, rt))           { med.Intent = null;  }
        if (!c.ShouldExtract(c.AuthoredOn, rt))       { med.AuthoredOn = null;  }
        if (!c.ShouldExtract(c.DosageInstruction, rt)){ med.DosageInstruction = null; }
    }

    private void ApplyToProcedure(FhirProcedure proc, ExtractionConfig c, string rt)
    {
        if (!c.ShouldExtract(c.Subject, rt))          { proc.Subject = null; }
        if (!c.ShouldExtract(c.Code, rt))             { proc.Code = null;              }
        if (!c.ShouldExtract(c.Status, rt))           { proc.Status = null;           }
        if (!c.ShouldExtract(c.PerformedDateTime, rt)){ proc.PerformedDateTime = null; }
        if (!c.ShouldExtract(c.Period, rt))           { proc.PerformedPeriod = null;   }
    }
#endregion
}