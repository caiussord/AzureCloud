namespace ProductApi.Models;

public sealed record Product(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    string Category,
    DateTime CreatedAt);

public sealed record SaveProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    string Category);
