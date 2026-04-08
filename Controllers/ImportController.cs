using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using AcmeEHRDataProcessingAPI.Models;
using AcmeEHRDataProcessingAPI.Services;

namespace AcmeEHRDataProcessingAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class ImportController : ControllerBase
{
    private readonly FhirImportService _importService;
    private readonly RecordsService _recordsService;
    private readonly TransformService _transformService;
    private readonly AnalyticsService _analyticsService;
    private readonly MatchService _matchService;
    private static readonly HashSet<string> ValidResourceTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Patient", "Observation", "Condition", "Encounter", "MedicationRequest", "Procedure"
    };

    private static readonly HashSet<string> ValidActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "flatten", "extract", "rename", "remove"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ImportController(FhirImportService importService, RecordsService recordsService, TransformService transformService, 
                            AnalyticsService analyticsService, MatchService matchService)
    {
        _importService = importService;
        _recordsService = recordsService;
        _transformService = transformService;
        _analyticsService = analyticsService;
        _matchService = matchService;
    }

    [HttpPost("import")]
    public async Task<ActionResult<ImportResult>> Post(IFormFile? file, [FromForm] string? extractionConfig)
    {
        ExtractionConfig? config = null;
        if (!string.IsNullOrWhiteSpace(extractionConfig))
        {
            try
            {
                config = JsonSerializer.Deserialize<ExtractionConfig>(extractionConfig, JsonOptions);
            }
            catch (Exception ex)
            {
                return BadRequest($"Invalid extractionConfig JSON: {ex.Message}");
            }
        }

        Stream? inputStream = null;

        if (file != null)
        {
            if (file.Length == 0)
                return BadRequest("Uploaded file is empty.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".jsonl")
                return BadRequest("File must have a .jsonl extension.");

            inputStream = file.OpenReadStream();
        }
        else if (Request.ContentLength > 0 && Request.Body != null)
        {
            inputStream = Request.Body;
        }
        else
        {
            return BadRequest("No input provided. Send a JSONL file via multipart/form-data");
        }

        await using (inputStream)
        {
            var result = await _importService.ImportAsync(inputStream, config);
            return Ok(result);
        }
    }

    [HttpGet("records")]
    public async Task<ActionResult<List<FhirResource>>> Get([FromQuery] string? resourceType, [FromQuery] string? subject, [FromQuery] string? fields)
    {
 
        if (!string.IsNullOrWhiteSpace(resourceType) && !ValidResourceTypes.Contains(resourceType))
        {
            return BadRequest($"Unknown resourceType '{resourceType}'. Should be one of: {string.Join(", ", ValidResourceTypes)}");
        }
 
        var query = new RecordsQueryParameters
        {
            ResourceType = resourceType,
            Subject = subject,
            Fields = fields
        };
 
        var result = await _recordsService.QueryAsync(query);
        if (result.Count == 0) { return NoContent(); }
        return Ok(result);
    }

    [HttpGet("records/{id}")]
    public async Task<ActionResult<Dictionary<string, object?>>> GetRecordById(string id, [FromQuery] string? fields)
    {
        var fieldsQuery = new RecordsQueryParameters
        {
            Fields = fields
        };

        var record = await _recordsService.GetByIdAsync(id, fieldsQuery);
 
        if (record == null)
            return NotFound($"No record found with id '{id}'.");
 
        return Ok(record);
    }

    [HttpPost("transform")]
    public async Task<ActionResult<List<Dictionary<string, object?>>>> Post([FromBody] TransformRequest request)
    {
        var result = await _transformService.TransformAsync(request);
        return Ok(result);
    }

    [HttpGet("analytics")]
    public async Task<ActionResult<AnalyticsResult>>  Analytics()
    {
        var analytics = await _analyticsService.GetAnalytics();
        return Ok(analytics);
    }

    [HttpPost("BestMatch")]
    public async Task<ActionResult<List<MatchResult>>> BestMatches([FromQuery] string patientId, [FromBody] IdentityConfig config)
    {
        FhirPatient? patient = await _recordsService.GetPatientById(patientId);
        if (patient == null) { return NotFound($"Source patient with id {patientId} not found."); }

        var matches = await _matchService.GetMatches(patient, config);
        if (matches.Count <= 0)
        { return NoContent(); }
        
        return Ok(matches);
    }
}