using JodVyapar.Api.Features.Reference;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace JodVyapar.Api.Tests;

/// <summary>The SQL scripts in /Database and the EF Core mapping must describe the same schema.</summary>
[Collection(ApiCollection.Name)]
public sealed class DatabaseSchemaTests(ApiFactory factory)
{
    private sealed record Column(string Table, string Name, string Type, bool Nullable);

    [Fact]
    public async Task ScriptedSchema_MatchesEfMapping()
    {
        var expected = new List<Column>();
        var actual = new List<Column>();

        await factory.WithDbAsync(async db =>
        {
            foreach (var entity in db.Model.GetEntityTypes())
            {
                var table = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
                expected.AddRange(entity.GetProperties().Select(p =>
                    new Column(table.Name, p.GetColumnName(table)!, p.GetColumnType(table).ToLowerInvariant(), p.IsColumnNullable(table))));
            }

            actual.AddRange(await db.Database.SqlQueryRaw<Column>("""
                SELECT TABLE_NAME AS [Table], COLUMN_NAME AS Name,
                       LOWER(DATA_TYPE + CASE
                           WHEN DATA_TYPE = 'decimal' THEN '(' + CAST(NUMERIC_PRECISION AS varchar(10)) + ',' + CAST(NUMERIC_SCALE AS varchar(10)) + ')'
                           WHEN CHARACTER_MAXIMUM_LENGTH = -1 THEN '(max)'
                           WHEN CHARACTER_MAXIMUM_LENGTH IS NOT NULL THEN '(' + CAST(CHARACTER_MAXIMUM_LENGTH AS varchar(10)) + ')'
                           ELSE '' END) AS Type,
                       CAST(CASE IS_NULLABLE WHEN 'YES' THEN 1 ELSE 0 END AS bit) AS Nullable
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = 'dbo'
                """).ToListAsync());
        });

        // Every mapped table exists with exactly the mapped columns, and no other tables exist.
        Assert.Equal(expected.OrderBy(c => c.Table).ThenBy(c => c.Name), actual.OrderBy(c => c.Table).ThenBy(c => c.Name));
    }

    [Fact]
    public async Task Scripts_CanBeRerunWithoutDuplicates()
    {
        var before = await CountReferenceRowsAsync();

        await using (var connection = new SqlConnection(factory.ConnectionString))
        {
            await connection.OpenAsync();
            foreach (var batch in DatabaseScripts.SchemaAndSeed().SelectMany(DatabaseScripts.Batches))
            {
                await using var command = new SqlCommand(batch, connection);
                await command.ExecuteNonQueryAsync();
            }
        }

        Assert.Equal(before, await CountReferenceRowsAsync());
        Assert.Equal((3, 36, 161, 47, 14), before);
    }

    private async Task<(int Roles, int States, int Cities, int Categories, int Units)> CountReferenceRowsAsync()
    {
        var counts = (0, 0, 0, 0, 0);
        await factory.WithDbAsync(async db => counts = (
            await db.Roles.CountAsync(),
            await db.Set<State>().CountAsync(),
            await db.Set<City>().CountAsync(),
            await db.Set<Category>().CountAsync(),
            await db.Set<Unit>().CountAsync()));
        return counts;
    }
}
