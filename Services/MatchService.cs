using System.Text.RegularExpressions;
using AcmeEHRDataProcessingAPI.Models;
using AcmeEHRDataProcessingAPI.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AcmeEHRDataProcessingAPI.Services;
public class MatchService
{
    private readonly FhirResourceStore _resourceStore; 
    public MatchService(FhirResourceStore resourceStore)
    {
        _resourceStore = resourceStore;
    }

    public async Task<List<MatchResult>> GetMatches(FhirPatient patient, IdentityConfig config)
    {
        var allPatients = await _resourceStore.Patients.Find(Builders<FhirPatient>.Filter.Empty).ToListAsync();
        var matches = new List<MatchResult>();
        foreach(var pt in allPatients)
        {
            var (match, score) = IsMatch(patient, pt, config); 
            if (match && pt.Id is not null)
            {
                matches.Add(new MatchResult(pt.Id, score));
            }
        }
        return matches;
    }

    private (bool, int) IsMatch(FhirPatient patient1, FhirPatient patient2, IdentityConfig config)
    {
        // not a match because it's the same patient
        if (patient1.Id.Equals(patient2.Id)) { return (false, -1); }
        int totalWeight = 0;
        foreach(var kvp in config.WeightParameters)
        {
            if (PassMatchCheck(patient1, patient2, kvp.Key, kvp.Value.MatchType))
            {
                totalWeight += kvp.Value.Weight;
            }
        }

        return (totalWeight >= config.MinWeight && totalWeight <= config.MaxWeight, totalWeight);
    }

    private bool PassMatchCheck(FhirPatient patient1, FhirPatient patient2, PatientFieldEnum fieldType, MatchTypeEnum matchType)
    {
        return 
         fieldType switch
            {
                PatientFieldEnum.Name => NameCheck(patient1, patient2, matchType),
                PatientFieldEnum.Gender => GenderCheck(patient1, patient2, matchType),
                PatientFieldEnum.BirthDate => BirthDateCheck(patient1, patient2, matchType),
                PatientFieldEnum.Telecom => TelecomCheck(patient1, patient2, matchType),
                PatientFieldEnum.Address => AddressCheck(patient1, patient2, matchType),
                _ => false
            };
    }

    #region AI-generated

    private bool NameCheck(FhirPatient patient1, FhirPatient patient2, MatchTypeEnum matchType)
    {
        // If either patient has no names, we cannot confirm a match
        if (patient1.Name == null || !patient1.Name.Any() ||
            patient2.Name == null || !patient2.Name.Any())
        {
            return false;
        }

        // Prefer "official" use names, fall back to first available
        FhirName? name1 = patient1.Name.FirstOrDefault(n => n.Use == "official") ?? patient1.Name.First();
        FhirName? name2 = patient2.Name.FirstOrDefault(n => n.Use == "official") ?? patient2.Name.First();

        return matchType switch
        {
            MatchTypeEnum.Exact => ExactNameMatch(name1, name2),
            MatchTypeEnum.Partial => PartialNameMatch(name1, name2),
            _ => false
        };
    }

    private bool ExactNameMatch(FhirName name1, FhirName name2)
    {
        // Family names must match exactly (case-insensitive)
        if (!string.Equals(name1.Family, name2.Family, StringComparison.OrdinalIgnoreCase))
            return false;

        // Both must have given names
        if (name1.Given == null || !name1.Given.Any() ||
            name2.Given == null || !name2.Given.Any())
            return false;

        // All given names must match exactly (order-insensitive, case-insensitive)
        var given1 = name1.Given.Select(g => g.ToLowerInvariant()).OrderBy(g => g).ToList();
        var given2 = name2.Given.Select(g => g.ToLowerInvariant()).OrderBy(g => g).ToList();

        return given1.SequenceEqual(given2);
    }

    private bool PartialNameMatch(FhirName name1, FhirName name2)
    {
        // Family name is required and must match (case-insensitive)
        if (!string.Equals(name1.Family, name2.Family, StringComparison.OrdinalIgnoreCase))
            return false;

        // If either patient has no given names, family name match alone is sufficient
        if (name1.Given == null || !name1.Given.Any() ||
            name2.Given == null || !name2.Given.Any())
            return true;

        // At least one given name must match between the two patients
        var given1 = name1.Given.Select(g => g.ToLowerInvariant()).ToHashSet();
        var given2 = name2.Given.Select(g => g.ToLowerInvariant()).ToHashSet();

        return given1.Overlaps(given2);
    }
    private bool GenderCheck(FhirPatient patient1, FhirPatient patient2, MatchTypeEnum matchType)
    {
        // If either patient has no gender recorded, we cannot confirm a match
        if (string.IsNullOrWhiteSpace(patient1.Gender) || string.IsNullOrWhiteSpace(patient2.Gender))
            return false;

        // FHIR gender is a simple code (male/female/other/unknown) — 
        // exact and partial match both use case-insensitive equality,
        // but partial match treats "unknown" as a pass-through
        if (matchType == MatchTypeEnum.Partial)
        {
            if (patient1.Gender.Equals("unknown", StringComparison.OrdinalIgnoreCase) ||
                patient2.Gender.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return string.Equals(patient1.Gender, patient2.Gender, StringComparison.OrdinalIgnoreCase);
    }

    private bool BirthDateCheck(FhirPatient patient1, FhirPatient patient2, MatchTypeEnum matchType)
    {
        // If either patient has no birthdate recorded, we cannot confirm a match
        if (string.IsNullOrWhiteSpace(patient1.BirthDate) || string.IsNullOrWhiteSpace(patient2.BirthDate))
            return false;

        // FHIR dates can be partial: "YYYY", "YYYY-MM", or "YYYY-MM-DD"
        // Try to parse each into their components
        if (!TryParseFhirDate(patient1.BirthDate, out int? year1, out int? month1, out int? day1) ||
            !TryParseFhirDate(patient2.BirthDate, out int? year2, out int? month2, out int? day2))
            return false;

        return matchType switch
        {
            MatchTypeEnum.Exact => ExactDateMatch(year1, month1, day1, year2, month2, day2),
            MatchTypeEnum.Partial => PartialDateMatch(year1, month1, day1, year2, month2, day2),
            _ => false
        };
    }

    private bool ExactDateMatch(int? year1, int? month1, int? day1, int? year2, int? month2, int? day2)
    {
        // All components present in both dates must match
        if (year1 != year2) return false;

        // If either date supplied a month, both must agree
        if (month1.HasValue && month2.HasValue && month1 != month2) return false;
        if (month1.HasValue != month2.HasValue) return false;

        // Same logic for day
        if (day1.HasValue && day2.HasValue && day1 != day2) return false;
        if (day1.HasValue != day2.HasValue) return false;

        return true;
    }

    private bool PartialDateMatch(int? year1, int? month1, int? day1, int? year2, int? month2, int? day2)
    {
        // Year must always agree
        if (year1 != year2) return false;

        // Month: only compare if both dates include it
        if (month1.HasValue && month2.HasValue && month1 != month2) return false;

        // Day: only compare if both dates include it
        if (day1.HasValue && day2.HasValue && day1 != day2) return false;

        return true;
    }

    private bool TryParseFhirDate(string fhirDate, out int? year, out int? month, out int? day)
    {
        year = month = day = null;

        if (string.IsNullOrWhiteSpace(fhirDate))
            return false;

        // FHIR date format: YYYY | YYYY-MM | YYYY-MM-DD
        var parts = fhirDate.Trim().Split('-');

        if (parts.Length < 1 || !int.TryParse(parts[0], out int y))
            return false;

        year = y;

        if (parts.Length >= 2)
        {
            if (!int.TryParse(parts[1], out int m) || m < 1 || m > 12)
                return false;
            month = m;
        }

        if (parts.Length >= 3)
        {
            if (!int.TryParse(parts[2], out int d) || d < 1 || d > 31)
                return false;
            day = d;
        }

        return true;
    }
    private bool TelecomCheck(FhirPatient patient1, FhirPatient patient2, MatchTypeEnum matchType)
{
    // If either patient has no telecom records, we cannot confirm a match
    if (patient1.Telecom == null || !patient1.Telecom.Any() ||
        patient2.Telecom == null || !patient2.Telecom.Any())
        return false;

    // Only compare contacts that have a value
    var contacts1 = patient1.Telecom.Where(t => !string.IsNullOrWhiteSpace(t.Value)).ToList();
    var contacts2 = patient2.Telecom.Where(t => !string.IsNullOrWhiteSpace(t.Value)).ToList();

    if (!contacts1.Any() || !contacts2.Any())
        return false;

    return matchType switch
    {
        MatchTypeEnum.Exact => ExactTelecomMatch(contacts1, contacts2),
        MatchTypeEnum.Partial => PartialTelecomMatch(contacts1, contacts2),
        _ => false
    };
}

    private bool ExactTelecomMatch(List<FhirContactPoint> contacts1, List<FhirContactPoint> contacts2)
    {
        // Every contact point in patient1 must have a corresponding match in patient2
        // and both lists must be the same size
        if (contacts1.Count != contacts2.Count)
            return false;

        return contacts1.All(c1 => contacts2.Any(c2 => ContactPointsMatch(c1, c2, exact: true)));
    }

    private bool PartialTelecomMatch(List<FhirContactPoint> contacts1, List<FhirContactPoint> contacts2)
    {
        // At least one contact point must match between the two patients
        return contacts1.Any(c1 => contacts2.Any(c2 => ContactPointsMatch(c1, c2, exact: false)));
    }

    private bool ContactPointsMatch(FhirContactPoint c1, FhirContactPoint c2, bool exact)
    {
        // Normalise values for comparison
        var value1 = NormaliseTelecomValue(c1.System, c1.Value!);
        var value2 = NormaliseTelecomValue(c2.System, c2.Value!);

        // If both have a system, they must agree — a phone number should not match an email
        if (!string.IsNullOrWhiteSpace(c1.System) && !string.IsNullOrWhiteSpace(c2.System))
        {
            if (!string.Equals(c1.System, c2.System, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (exact)
            return string.Equals(value1, value2, StringComparison.OrdinalIgnoreCase);

        // Partial: for phone numbers, check if either normalised value ends with the other
        // to handle local vs. international format differences e.g. 07911123456 vs +447911123456
        if (IsPhoneSystem(c1.System) || IsPhoneSystem(c2.System))
            return value1.EndsWith(value2, StringComparison.OrdinalIgnoreCase) ||
                value2.EndsWith(value1, StringComparison.OrdinalIgnoreCase);

        // For email and other systems, fall back to case-insensitive equality
        return string.Equals(value1, value2, StringComparison.OrdinalIgnoreCase);
    }

    private string NormaliseTelecomValue(string? system, string value)
    {
        if (IsPhoneSystem(system))
        {
            // Strip all non-digit characters for phone comparison
            return Regex.Replace(value, @"\D", "");
        }

        // For email and other systems, just trim whitespace
        return value.Trim();
    }

    private bool IsPhoneSystem(string? system)
    {
        return string.Equals(system, "phone", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(system, "fax", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(system, "sms", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(system, "pager", StringComparison.OrdinalIgnoreCase);
    }
    private bool AddressCheck(FhirPatient patient1, FhirPatient patient2, MatchTypeEnum matchType)
    {
        // If either patient has no addresses, we cannot confirm a match
        if (patient1.Address == null || !patient1.Address.Any() ||
            patient2.Address == null || !patient2.Address.Any())
            return false;

        // Only work with addresses that have at least one meaningful field
        var addresses1 = patient1.Address.Where(a => HasMeaningfulData(a)).ToList();
        var addresses2 = patient2.Address.Where(a => HasMeaningfulData(a)).ToList();

        if (!addresses1.Any() || !addresses2.Any())
            return false;

        return matchType switch
        {
            MatchTypeEnum.Exact => ExactAddressMatch(addresses1, addresses2),
            MatchTypeEnum.Partial => PartialAddressMatch(addresses1, addresses2),
            _ => false
        };
    }

    private bool ExactAddressMatch(List<FhirAddress> addresses1, List<FhirAddress> addresses2)
    {
        // Every address in patient1 must have a corresponding exact match in patient2
        // and both lists must be the same size
        if (addresses1.Count != addresses2.Count)
            return false;

        return addresses1.All(a1 => addresses2.Any(a2 => CompareAddresses(a1, a2, exact: true)));
    }

    private bool PartialAddressMatch(List<FhirAddress> addresses1, List<FhirAddress> addresses2)
    {
        // At least one address must match between the two patients
        return addresses1.Any(a1 => addresses2.Any(a2 => CompareAddresses(a1, a2, exact: false)));
    }

    private bool CompareAddresses(FhirAddress a1, FhirAddress a2, bool exact)
    {
        // PostalCode is the strongest single identifier — if both supply it, it must agree
        if (!string.IsNullOrWhiteSpace(a1.PostalCode) && !string.IsNullOrWhiteSpace(a2.PostalCode))
        {
            if (!PostalCodesMatch(a1.PostalCode, a2.PostalCode, exact))
                return false;
        }

        // City must agree if both supply it
        if (!string.IsNullOrWhiteSpace(a1.City) && !string.IsNullOrWhiteSpace(a2.City))
        {
            if (!string.Equals(NormaliseAddressField(a1.City), NormaliseAddressField(a2.City),
                    StringComparison.OrdinalIgnoreCase))
                return false;
        }

        // State must agree if both supply it
        if (!string.IsNullOrWhiteSpace(a1.State) && !string.IsNullOrWhiteSpace(a2.State))
        {
            if (!string.Equals(NormaliseAddressField(a1.State), NormaliseAddressField(a2.State),
                    StringComparison.OrdinalIgnoreCase))
                return false;
        }

        // Country must agree if both supply it
        if (!string.IsNullOrWhiteSpace(a1.Country) && !string.IsNullOrWhiteSpace(a2.Country))
        {
            if (!string.Equals(NormaliseAddressField(a1.Country), NormaliseAddressField(a2.Country),
                    StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (exact)
        {
            // For exact match, line-level detail must also agree if either address supplies it
            var lines1 = NormaliseLines(a1.Line);
            var lines2 = NormaliseLines(a2.Line);

            if (lines1.Any() && lines2.Any())
            {
                if (!lines1.SequenceEqual(lines2, StringComparer.OrdinalIgnoreCase))
                    return false;
            }
            else if (lines1.Any() != lines2.Any())
            {
                // One has line detail, the other doesn't — not an exact match
                return false;
            }

            // For exact match, every supplied field must be present in both
            if (!SuppliedFieldsAlign(a1, a2))
                return false;
        }

        return true;
    }

    private bool PostalCodesMatch(string code1, string code2, bool exact)
    {
        var normalised1 = NormalisePostalCode(code1);
        var normalised2 = NormalisePostalCode(code2);

        if (exact)
            return string.Equals(normalised1, normalised2, StringComparison.OrdinalIgnoreCase);

        // Partial: match on the outward code / district portion only
        // e.g. "SW1A 1AA" -> "SW1A", "90210-1234" -> "90210"
        var sector1 = GetPostalSector(normalised1);
        var sector2 = GetPostalSector(normalised2);

        return string.Equals(sector1, sector2, StringComparison.OrdinalIgnoreCase);
    }

    private string NormalisePostalCode(string code)
    {
        // Collapse internal whitespace and trim
        return Regex.Replace(code.Trim(), @"\s+", " ").ToUpperInvariant();
    }

    private string GetPostalSector(string normalisedCode)
    {
        // Split on space (UK: "SW1A 1AA" -> "SW1A") or hyphen (US ZIP+4: "90210-1234" -> "90210")
        var separatorIndex = normalisedCode.IndexOfAny(new[] { ' ', '-' });
        return separatorIndex > 0 ? normalisedCode[..separatorIndex] : normalisedCode;
    }

    private string NormaliseAddressField(string value)
    {
        // Collapse whitespace and remove common punctuation noise
        return Regex.Replace(value.Trim(), @"\s+", " ");
    }

    private List<string> NormaliseLines(List<string>? lines)
    {
        if (lines == null) return new List<string>();

        return lines
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => NormaliseAddressField(l))
            .ToList();
    }

    private bool SuppliedFieldsAlign(FhirAddress a1, FhirAddress a2)
    {
        // For exact matching, any field present in one address should be present in the other
        if (!string.IsNullOrWhiteSpace(a1.PostalCode) != !string.IsNullOrWhiteSpace(a2.PostalCode))
            return false;
        if (!string.IsNullOrWhiteSpace(a1.City) != !string.IsNullOrWhiteSpace(a2.City))
            return false;
        if (!string.IsNullOrWhiteSpace(a1.State) != !string.IsNullOrWhiteSpace(a2.State))
            return false;
        if (!string.IsNullOrWhiteSpace(a1.Country) != !string.IsNullOrWhiteSpace(a2.Country))
            return false;

        return true;
    }

    private bool HasMeaningfulData(FhirAddress address)
    {
        return !string.IsNullOrWhiteSpace(address.PostalCode) ||
            !string.IsNullOrWhiteSpace(address.City) ||
            !string.IsNullOrWhiteSpace(address.State) ||
            !string.IsNullOrWhiteSpace(address.Country) ||
            (address.Line != null && address.Line.Any(l => !string.IsNullOrWhiteSpace(l)));
    }
    #endregion AI-generated
}