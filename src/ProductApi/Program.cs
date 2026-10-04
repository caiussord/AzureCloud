using ProductApi.Data;
using ProductApi.Models;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);
var keyVaultUri = builder.Configuration["KeyVaultUri"];
if (Uri.TryCreate(keyVaultUri, UriKind.Absolute, out var vaultUri))
{
    // Na Azure, DefaultAzureCredential usa a identidade gerenciada da VM.
    // Localmente, usa a identidade autenticada pelo Azure CLI/Visual Studio.
    builder.Configuration.AddAzureKeyVault(vaultUri, new DefaultAzureCredential());
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
if (!string.IsNullOrWhiteSpace(builder.Configuration["ApplicationInsights:ConnectionString"]))
    builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddSingleton<ProductRepository>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.MapGet("/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }));

var products = app.MapGroup("/api/products");
products.MapGet("/", async (ProductRepository repository) => Results.Ok(await repository.GetAllAsync()));
products.MapGet("/{id:int}", async (int id, ProductRepository repository) => await repository.GetByIdAsync(id) is { } product ? Results.Ok(product) : Results.NotFound());
products.MapPost("/", async (SaveProductRequest input, ProductRepository repository) =>
{
    var error = Validate(input); if (error is not null) return Results.BadRequest(new { error });
    var product = await repository.CreateAsync(input); return Results.Created($"/api/products/{product.Id}", product);
});
products.MapPut("/{id:int}", async (int id, SaveProductRequest input, ProductRepository repository) =>
{
    var error = Validate(input); if (error is not null) return Results.BadRequest(new { error });
    return await repository.UpdateAsync(id, input) ? Results.NoContent() : Results.NotFound();
});
products.MapDelete("/{id:int}", async (int id, ProductRepository repository) => await repository.DeleteAsync(id) ? Results.NoContent() : Results.NotFound());

await app.Services.GetRequiredService<ProductRepository>().InitializeAsync();
app.Run();

static string? Validate(SaveProductRequest product) => string.IsNullOrWhiteSpace(product.Name) ? "Nome é obrigatório." : product.Name.Length > 120 ? "Nome deve ter até 120 caracteres." : product.Price < 0 ? "Preço não pode ser negativo." : product.Stock < 0 ? "Estoque não pode ser negativo." : null;
