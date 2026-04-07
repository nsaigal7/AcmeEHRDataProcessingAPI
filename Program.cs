using AcmeEHRDataProcessingAPI.Services;
using AcmeEHRDataProcessingAPI.Persistence;
using AcmeEHRDataProcessingAPI.Middlewares;
using AcmeEHRDataProcessingAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDB"));

builder.Services.AddSingleton<FhirResourceStore>();

builder.Services.AddScoped<FhirValidationService>();
builder.Services.AddScoped<ExtractionService>();
builder.Services.AddScoped<FhirImportService>();
builder.Services.AddScoped<RecordsService>();
builder.Services.AddScoped<TransformService>();
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddScoped<MatchService>();
builder.Services.AddScoped<ApiMetric>();

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104_857_600;
});


var app = builder.Build();
app.UseMiddleware<MetricsMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

app.Run();
