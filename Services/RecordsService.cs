using System.Reflection;
using System.Text.Json;
using MongoDB.Driver;
using AcmeEHRDataProcessingAPI.Models;
using AcmeEHRDataProcessingAPI.Persistence;

namespace AcmeEHRDataProcessingAPI.Services;

public class RecordsService
{
    private readonly FhirResourceStore _store;
    private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> PropertyMapCache = new();
     private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RecordsService(FhirResourceStore store)
    {
        _store = store;
    }

    public async Task<List<Dictionary<string, object?>>> QueryAsync(RecordsQueryParameters query)
    {
        var records = new List<Dictionary<string, object?>>();
        var requestedFields = query.ParsedFields();
        var resourceTypeFilter = query.ResourceType?.Trim();
        var subjectFilter = query.Subject?.Trim();

        // Fetch from each collection based on resourceType filter
        var allRecords = new List<FhirResource>();

        bool includeType(string typeName) =>
            string.IsNullOrWhiteSpace(resourceTypeFilter) ||
            resourceTypeFilter.Equals(typeName, StringComparison.OrdinalIgnoreCase);

        if (includeType("Patient"))
            allRecords.AddRange(await FetchAsync(_store.Patients, subjectFilter: null));

        if (includeType("Observation"))
            allRecords.AddRange(await FetchAsync(_store.Observations, subjectFilter));

        if (includeType("Condition"))
            allRecords.AddRange(await FetchAsync(_store.Conditions, subjectFilter));

        if (includeType("Encounter"))
            allRecords.AddRange(await FetchAsync(_store.Encounters, subjectFilter));

        if (includeType("MedicationRequest"))
            allRecords.AddRange(await FetchAsync(_store.MedicationRequests, subjectFilter));

        if (includeType("Procedure"))
            allRecords.AddRange(await FetchAsync(_store.Procedures, subjectFilter));

        // Project each record to a dictionary containing only the requested fields
        records = allRecords
            .Select(r => ProjectRecord(r, requestedFields))
            .ToList();

        return records;
    }

    private static async Task<List<T>> FetchAsync<T>(IMongoCollection<T> collection, string? subjectFilter) where T : FhirResource
    {
        var docs = await collection.Find(Builders<T>.Filter.Empty).ToListAsync();

        if (string.IsNullOrWhiteSpace(subjectFilter))
            return docs;

        // Subject is stored as a nested FhirReference — filter in-process
        return docs.Where(doc => MatchesSubject(doc, subjectFilter)).ToList();
    }

    private static bool MatchesSubject(FhirResource resource, string subjectFilter)
    {
        // Patients don't have a subject field
        if (resource is FhirPatient) return false;

        var subjectRef = resource switch
        {
            FhirObservation o => o.Subject?.Reference,
            FhirCondition c => c.Subject?.Reference,
            FhirEncounter e => e.Subject?.Reference,
            FhirMedicationRequest m => m.Subject?.Reference,
            FhirProcedure p => p.Subject?.Reference,
            _ => null
        };

        return string.Equals(subjectRef, subjectFilter, StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, object?> ProjectRecord(FhirResource resource, HashSet<string>? requestedFields)
    {
        var type = resource.GetType();
        var propMap = GetPropertyMap(type);
        var output = new Dictionary<string, object?>();

        foreach (var (camelName, prop) in propMap)
        {
            // Never expose the internal MongoDB _id
            if (camelName == "mongoId") continue;

            // If a field list was specified, skip fields not in it
            if (requestedFields != null && !requestedFields.Contains(camelName)) continue;

            var value = prop.GetValue(resource);

            // Skip null values unless explicitly requested
            if (value == null && requestedFields == null) continue;

            output[camelName] = value;
        }

        return output;
    }

    private static Dictionary<string, PropertyInfo> GetPropertyMap(Type type)
    {
        if (PropertyMapCache.TryGetValue(type, out var cached))
            return cached;

        var map = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

        // Walk up the inheritance chain to include base class properties
        var current = type;
        while (current != null && current != typeof(object))
        {
            foreach (var prop in current.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var camelName = char.ToLowerInvariant(prop.Name[0]) + prop.Name[1..];
                if (!map.ContainsKey(camelName))
                    map[camelName] = prop;
            }
            current = current.BaseType;
        }

        PropertyMapCache[type] = map;
        return map;
    }

    public async Task<Dictionary<string, object?>?> GetByIdAsync(string id, RecordsQueryParameters fieldsQuery)
    {
        FhirResource? match =
            await FindFirstAsync(_store.Patients, id) ??
            await FindFirstAsync(_store.Observations, id) ??
            await FindFirstAsync(_store.Conditions, id) ??
            await FindFirstAsync(_store.Encounters, id) ??
            await FindFirstAsync(_store.MedicationRequests, id) ??
            (FhirResource?) await FindFirstAsync(_store.Procedures, id);
        
        if (match == null) { return null; }

        return ProjectRecord(match, fieldsQuery.ParsedFields());
    }

    public async Task<FhirPatient?> GetPatientById(string id)
    {
        return await FindFirstAsync(_store.Patients, id);         
    }

    private static async Task<T?> FindFirstAsync<T>(IMongoCollection<T> collection, string id) where T : FhirResource
    {
        var filter = Builders<T>.Filter.Eq(r => r.Id, id);
        return await collection.Find(filter).FirstOrDefaultAsync();
    }
}