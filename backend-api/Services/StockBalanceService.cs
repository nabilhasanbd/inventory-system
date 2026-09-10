using backend_api.Common;
using backend_api.Data;
using backend_api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace backend_api.Services;

public class StockBalanceService : IStockBalanceService
{
    private readonly ApplicationDbContext _db;

    public StockBalanceService(ApplicationDbContext db)
    {
        _db = db;
    }

    // Advisory read (no lock) - used for fail-fast pre-checks. Not concurrency-safe on its own.
    public async Task<decimal> GetCurrentStockAsync(int storeId, int itemId, CancellationToken ct)
    {
        var balance = await _db.StockBalances
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.StoreId == storeId && b.ItemId == itemId, ct);
        return balance?.Quantity ?? 0m;
    }

    public async Task<decimal> IncreaseStockAsync(int storeId, int itemId, decimal quantity, CancellationToken ct)
    {
        if (quantity <= 0)
            throw new BadRequestException("Quantity must be greater than zero.");

        if (!await _db.Stores.AnyAsync(s => s.Id == storeId, ct))
            throw new NotFoundException($"Store with id {storeId} was not found.");
        if (!await _db.Items.AnyAsync(i => i.Id == itemId, ct))
            throw new NotFoundException($"Item with id {itemId} was not found.");

        await using var cmd = CreateCommand(
            @"INSERT INTO ""StockBalances"" (""StoreId"", ""ItemId"", ""Quantity"") VALUES (@s, @i, @q)
              ON CONFLICT (""StoreId"", ""ItemId"") DO UPDATE
              SET ""Quantity"" = ""StockBalances"".""Quantity"" + EXCLUDED.""Quantity""
              RETURNING ""Quantity""",
            new (string, object)[] { ("@s", storeId), ("@i", itemId), ("@q", quantity) });
        return Convert.ToDecimal(await cmd.ExecuteScalarAsync(ct));
    }

    public async Task<decimal> DecreaseStockAsync(int storeId, int itemId, decimal quantity, CancellationToken ct)
    {
        if (quantity <= 0)
            throw new BadRequestException("Quantity must be greater than zero.");

        var current = await LockQuantityAsync(storeId, itemId, ct)
            ?? throw new ConflictException($"No stock balance found for store {storeId}, item {itemId}.");

        if (current < quantity)
            throw new ConflictException($"Insufficient stock. Available: {current}, requested: {quantity}.");

        var newQty = current - quantity;
        await SetQuantityAsync(storeId, itemId, newQty, ct);
        return newQty;
    }

    // Locks the row with SELECT ... FOR UPDATE and returns its quantity, or null if no row exists.
    // Must run inside the caller's database transaction so the row lock is held until commit.
    private async Task<decimal?> LockQuantityAsync(int storeId, int itemId, CancellationToken ct)
    {
        await using var cmd = CreateCommand(
            @"SELECT ""Quantity"" FROM ""StockBalances"" WHERE ""StoreId"" = @s AND ""ItemId"" = @i FOR UPDATE",
            new (string, object)[] { ("@s", storeId), ("@i", itemId) });
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is null ? null : Convert.ToDecimal(result);
    }

    private async Task SetQuantityAsync(int storeId, int itemId, decimal newQty, CancellationToken ct)
    {
        await ExecAsync(
            @"UPDATE ""StockBalances"" SET ""Quantity"" = @q WHERE ""StoreId"" = @s AND ""ItemId"" = @i",
            new (string, object)[] { ("@q", newQty), ("@s", storeId), ("@i", itemId) }, ct);
    }

    private NpgsqlCommand CreateCommand(string sql, (string Name, object Value)[] args)
    {
        var conn = (NpgsqlConnection)_db.Database.GetDbConnection();
        var cmd = conn.CreateCommand();
        if (_db.Database.CurrentTransaction?.GetDbTransaction() is NpgsqlTransaction t)
            cmd.Transaction = t;
        cmd.CommandText = sql;
        foreach (var (name, value) in args)
            cmd.Parameters.Add(new NpgsqlParameter { ParameterName = name, Value = value });
        return cmd;
    }

    private async Task ExecAsync(string sql, (string Name, object Value)[] args, CancellationToken ct)
    {
        await using var cmd = CreateCommand(sql, args);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
