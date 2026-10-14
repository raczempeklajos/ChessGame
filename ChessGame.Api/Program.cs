var builder = WebApplication.CreateBuilder(args);

// Register MVC controllers.
builder.Services.AddControllers();

// Register OpenAPI services.
builder.Services.AddOpenApi();

// Allow the local React development server.
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173"
            )
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