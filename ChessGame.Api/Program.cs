
var builder = WebApplication.CreateBuilder(args);

// Register MVC controllers to handle incoming HTTP requests.
builder.Services.AddControllers();

// Register OpenAPI services for API documentation.
builder.Services.AddOpenApi();

// Build the web application with the configured services.
var app = builder.Build();

// Expose the OpenAPI document only in the development environment.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// HTTPS redirection is currently disabled for local development.
// app.UseHttpsRedirection();

// Add authorization middleware to the HTTP request pipeline.
app.UseAuthorization();

// Map controller routes to their corresponding API endpoints.
app.MapControllers();

// Start the web server and begin processing incoming requests.
app.Run();

namespace ChessGameApi
{
    /// <summary>
    /// Exposes the application entry point to the integration test project.
    /// </summary>
    public partial class Program { }
}
