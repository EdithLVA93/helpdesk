using System.Text.Json.Serialization;
using HelpDesk.Application;
using HelpDesk.Infrastructure;
using HelpDesk.Infrastructure.Data;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuración
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("HelpDeskDb")
    ?? throw new InvalidOperationException(
        "La cadena de conexión 'HelpDeskDb' no está configurada (ConnectionStrings:HelpDeskDb).");

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

// ---------------------------------------------------------------------------
// Servicios
// ---------------------------------------------------------------------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        // Los enums viajan como texto ("Open") y no como número (0).
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Respuestas de error con el formato estándar ProblemDetails (RFC 9457).
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi(options =>
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "HelpDesk API";
        document.Info.Version = "v1";
        document.Info.Description = "API REST de la mesa de ayuda: gestión de usuarios y tickets.";
        return Task.CompletedTask;
    }));

// GET /health comprueba que la API puede conectarse a SQL Server.
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<HelpDeskDbContext>("database");

builder.Services.AddCors(options =>
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

// ---------------------------------------------------------------------------
// Pipeline HTTP
// ---------------------------------------------------------------------------
var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                       // /openapi/v1.json
    app.UseSwaggerUI(options =>             // /swagger
    {
        options.SwaggerEndpoint("/openapi/v1.json", "HelpDesk API v1");
        options.DocumentTitle = "HelpDesk API";
    });
}

app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);

app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();
