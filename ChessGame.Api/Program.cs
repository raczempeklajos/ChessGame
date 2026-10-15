
var builder = WebApplication.CreateBuilder(args);

// Register MVC controllers.
builder.Services.AddControllers();

// Register OpenAPI services.
builder.Services.AddOpenApi();

// Configure allowed frontend origins.
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactFrontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Expose OpenAPI in development.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Enable CORS before authorization and controllers.
app.UseCors("ReactFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();

namespace ChessGameApi
{
    public partial class Program { }
}
