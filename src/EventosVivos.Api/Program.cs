using EventosVivos.Application;
using EventosVivos.Infrastructure;
using EventosVivos.Api;
using EventosVivos.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

var allowedOrigins = GetAllowedOrigins(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();

static string[] GetAllowedOrigins(IConfiguration configuration)
{
    var origins = new List<string>();

    var configured = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    if (configured is not null)
        origins.AddRange(configured.Where(origin => !string.IsNullOrWhiteSpace(origin)));

    var frontendUrl = configuration["FRONTEND_URL"]
        ?? Environment.GetEnvironmentVariable("FRONTEND_URL");
    if (!string.IsNullOrWhiteSpace(frontendUrl))
        origins.Add(frontendUrl.Trim());

    return origins.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() is { Length: > 0 } result
        ? result
        : ["http://localhost:4200"];
}

public partial class Program { }
