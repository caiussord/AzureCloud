using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using ProductApi.Models;

namespace ProductApi.Data;

public sealed class ProductRepository(IConfiguration configuration)
{
    private readonly string? _sqlServerConnection = configuration.GetConnectionString("SqlServer");
    private const string SqliteConnection = "Data Source=products.db";

    public async Task InitializeAsync()
    {
        var sql = """CREATE TABLE Products (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Description TEXT NULL, Price DECIMAL NOT NULL, Stock INTEGER NOT NULL, Category TEXT NOT NULL, CreatedAt TEXT NOT NULL)""";
        if (_sqlServerConnection is not null)
        {
            sql = """IF OBJECT_ID('Products','U') IS NULL CREATE TABLE Products (Id INT IDENTITY(1,1) PRIMARY KEY, Name NVARCHAR(120) NOT NULL, Description NVARCHAR(500) NULL, Price DECIMAL(18,2) NOT NULL, Stock INT NOT NULL, Category NVARCHAR(80) NOT NULL, CreatedAt DATETIME2 NOT NULL)""";
            await using var connection = new SqlConnection(_sqlServerConnection);
            await connection.OpenAsync(); await using var command = new SqlCommand(sql, connection); await command.ExecuteNonQueryAsync();
            return;
        }
        await using var sqlite = new SqliteConnection(SqliteConnection);
        await sqlite.OpenAsync(); await using var sqliteCommand = new SqliteCommand(sql, sqlite); await sqliteCommand.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        const string sql = "SELECT Id, Name, Description, Price, Stock, Category, CreatedAt FROM Products ORDER BY Id DESC";
        return await ReadAsync(sql, null);
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        var products = await ReadAsync("SELECT Id, Name, Description, Price, Stock, Category, CreatedAt FROM Products WHERE Id = @id", new() { ["@id"] = id });
        return products.SingleOrDefault();
    }

    public async Task<Product> CreateAsync(SaveProductRequest input)
    {
        var parameters = Parameters(input);
        const string sqliteSql = "INSERT INTO Products (Name,Description,Price,Stock,Category,CreatedAt) VALUES (@name,@description,@price,@stock,@category,@createdAt); SELECT last_insert_rowid();";
        const string sqlServerSql = "INSERT INTO Products (Name,Description,Price,Stock,Category,CreatedAt) OUTPUT INSERTED.Id VALUES (@name,@description,@price,@stock,@category,@createdAt);";
        var id = await ExecuteScalarAsync(_sqlServerConnection is null ? sqliteSql : sqlServerSql, parameters);
        return (await GetByIdAsync(Convert.ToInt32(id)))!;
    }

    public async Task<bool> UpdateAsync(int id, SaveProductRequest input)
    {
        var parameters = Parameters(input); parameters["@id"] = id;
        var rows = await ExecuteAsync("UPDATE Products SET Name=@name, Description=@description, Price=@price, Stock=@stock, Category=@category WHERE Id=@id", parameters);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id) => await ExecuteAsync("DELETE FROM Products WHERE Id=@id", new() { ["@id"] = id }) > 0;

    private static Dictionary<string, object?> Parameters(SaveProductRequest x) => new()
    {
        ["@name"] = x.Name, ["@description"] = x.Description, ["@price"] = x.Price, ["@stock"] = x.Stock, ["@category"] = x.Category, ["@createdAt"] = DateTime.UtcNow
    };

    private async Task<IEnumerable<Product>> ReadAsync(string sql, Dictionary<string, object?>? parameters)
    {
        var result = new List<Product>();
        if (_sqlServerConnection is not null)
        {
            await using var c = new SqlConnection(_sqlServerConnection); await c.OpenAsync(); await using var cmd = new SqlCommand(sql, c); AddParameters(cmd, parameters);
            await using var reader = await cmd.ExecuteReaderAsync(); while (await reader.ReadAsync()) result.Add(Map(reader));
        }
        else
        {
            await using var c = new SqliteConnection(SqliteConnection); await c.OpenAsync(); await using var cmd = new SqliteCommand(sql, c); AddParameters(cmd, parameters);
            await using var reader = await cmd.ExecuteReaderAsync(); while (await reader.ReadAsync()) result.Add(Map(reader));
        }
        return result;
    }

    private async Task<object?> ExecuteScalarAsync(string sql, Dictionary<string, object?> p)
    {
        if (_sqlServerConnection is not null) { await using var c = new SqlConnection(_sqlServerConnection); await c.OpenAsync(); await using var cmd = new SqlCommand(sql, c); AddParameters(cmd,p); return await cmd.ExecuteScalarAsync(); }
        await using var s = new SqliteConnection(SqliteConnection); await s.OpenAsync(); await using var sc = new SqliteCommand(sql,s); AddParameters(sc,p); return await sc.ExecuteScalarAsync();
    }
    private async Task<int> ExecuteAsync(string sql, Dictionary<string, object?> p)
    {
        if (_sqlServerConnection is not null) { await using var c = new SqlConnection(_sqlServerConnection); await c.OpenAsync(); await using var cmd = new SqlCommand(sql,c); AddParameters(cmd,p); return await cmd.ExecuteNonQueryAsync(); }
        await using var s = new SqliteConnection(SqliteConnection); await s.OpenAsync(); await using var sc = new SqliteCommand(sql,s); AddParameters(sc,p); return await sc.ExecuteNonQueryAsync();
    }
    private static void AddParameters(dynamic command, Dictionary<string, object?>? parameters) { if (parameters is not null) foreach (var p in parameters) command.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value); }
    private static Product Map(dynamic r) => new(r.GetInt32(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2), r.GetDecimal(3), r.GetInt32(4), r.GetString(5), r.GetDateTime(6));
}
