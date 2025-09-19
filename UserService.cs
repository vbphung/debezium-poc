using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace debezium_poc
{
    [Table("users", Schema = "business")]
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("email")]
        public string? Email { get; set; }

        [Column("phone")]
        public string? Phone { get; set; }

        [NotMapped]
        public DateTime CreatedAt { get; set; }
    }

    class UserService(BusinessDbContext sourceDb) : IHostedService
    {
        private BusinessDbContext SourceDb => sourceDb;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            List<User> users =
            [
                new() { Id = 1, Name = "Alice", Email = "alice@example.com", Phone = "0123456789" },
                new() { Id = 2, Name = "Bob", Email = "bob@example.com", Phone = "0987654321" },
                new() { Id = 3, Name = "Charlie", Email = "charlie@example.com", Phone = "0112233445" },
                new() { Id = 4, Name = "David", Email = "david@example.com", Phone = "0223344556" },
                new() { Id = 5, Name = "Eve", Email = "eve@example.com", Phone = "0334455667" },
                new() { Id = 6, Name = "Frank", Email = "frank@example.com", Phone = "0445566778" },
                new() { Id = 7, Name = "Grace", Email = "grace@example.com", Phone = "0556677889" }
            ];

            var query = UpsertQueryBuilder.BuildUpsertSql(users);

            var affected = await SourceDb.Database.ExecuteSqlRawAsync(query, cancellationToken);
            Console.WriteLine($"{affected} rows have been changed");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    public static class UpsertQueryBuilder
    {
        public static string BuildUpsertSql<T>(IEnumerable<T> items)
        {
            var tableAttr = typeof(T).GetCustomAttribute<TableAttribute>()
                ?? throw new Exception("Table attribute must be configured");

            var tableName = tableAttr.Schema == null
                ? CaseSensitive(tableAttr.Name)
                : $"{CaseSensitive(tableAttr.Schema)}.{CaseSensitive(tableAttr.Name)}";

            var props = typeof(T).GetProperties()
                .Where(p => p.CanRead && p.GetCustomAttribute<ColumnAttribute>() != null);

            var columnNames = props
                .Select(p => p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name)
                .Where(p => !string.IsNullOrEmpty(p))
                .Select(CaseSensitive);

            var insertedRows = items
                .Select(item => props.Select(prop =>
                {
                    var value = prop.GetValue(item);
                    return FormatValue(value);
                }))
                .Select(values => string.Join(",", values))
                .Select(row => $"({row})");

            var keyColumns = typeof(T).GetProperties()
                .Where(prop => prop.GetCustomAttribute<KeyAttribute>() != null)
                .Select(prop => prop.GetCustomAttribute<ColumnAttribute>()?.Name ?? prop.Name)
                .Select(CaseSensitive);

            var setClauses = columnNames
                .Where(c => !keyColumns.Contains(c))
                .Select(c => $"{c} = EXCLUDED.{c}");

            var whereClauses = columnNames
                .Where(c => !keyColumns.Contains(c))
                .Select(c => $"{tableName}.{c} IS DISTINCT FROM EXCLUDED.{c}");

            return @$"INSERT INTO {tableName} (
{string.Join(",\n", columnNames)}
) VALUES {string.Join(",\n", insertedRows)}
ON CONFLICT ({string.Join(",", keyColumns)}) 
DO UPDATE SET {string.Join(",\n", setClauses)}
WHERE {string.Join("\nOR ", whereClauses)};";
        }

        private static string? FormatValue(object? value)
        {
            return value switch
            {
                null => "NULL",
                string s => $"'{s.Replace("'", "''")}'",
                DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
                bool b => b ? "TRUE" : "FALSE",
                _ => value.ToString()
            };
        }

        private static string CaseSensitive(string str)
        {
            return $"\"{str}\"";
        }
    }
}
