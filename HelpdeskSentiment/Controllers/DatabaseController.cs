using System.Data.Common;
using HelpdeskSentiment.Authentication;
using HelpdeskSentiment.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpdeskSentiment.Controllers
{
    // Live, read-only view of the SQL Server database for admins.
    // The page polls Snapshot() every few seconds and highlights rows that were added or changed.
    [Authorize(Roles = Roles.Admin)]
    public class DatabaseController : Controller
    {
        private const int MaxRows = 200;

        // Only these objects can be shown. Names never come from the request, so the SQL below is safe.
        private static readonly (string Name, string Kind, string OrderBy)[] Objects =
        {
            ("LoginActivity", "Table", "ORDER BY Id DESC"),
            ("CallRecords", "Table", "ORDER BY Id DESC"),
            ("Agents", "Table", "ORDER BY Id DESC"),
            ("vw_SentimentSummary", "View", "ORDER BY Sentiment"),
            ("vw_AgentSentiment", "View", "ORDER BY AgentId"),
            ("__EFMigrationsHistory", "Table", "ORDER BY MigrationId")
        };

        private readonly HelpdeskDbContext _db;

        public DatabaseController(HelpdeskDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Live database";
            ViewBag.DatabaseFile = Path.Combine(
                AppDomain.CurrentDomain.GetData("DataDirectory") as string ?? "", "HelpdeskSentimentDb.mdf");
            return View();
        }

        // GET /Database/Snapshot  → JSON with the columns and rows of every object above
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Snapshot()
        {
            DbConnection connection = _db.Database.GetDbConnection();
            await _db.Database.OpenConnectionAsync();
            try
            {
                var objects = new List<object>();
                foreach (var (name, kind, orderBy) in Objects)
                {
                    int total = await CountAsync(connection, name);
                    var (columns, rows) = await ReadAsync(connection, $"SELECT TOP ({MaxRows}) * FROM [dbo].[{name}] {orderBy}");
                    objects.Add(new { name, kind, total, columns, rows });
                }

                return Json(new { serverTime = DateTime.Now, objects });
            }
            finally
            {
                await _db.Database.CloseConnectionAsync();
            }
        }

        private static async Task<int> CountAsync(DbConnection connection, string name)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM [dbo].[{name}]";
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        private static async Task<(List<string> Columns, List<object?[]> Rows)> ReadAsync(DbConnection connection, string sql)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = sql;
            using DbDataReader reader = await command.ExecuteReaderAsync();

            var columns = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columns.Add(reader.GetName(i));
            }

            var rows = new List<object?[]>();
            while (await reader.ReadAsync())
            {
                var row = new object?[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                rows.Add(row);
            }

            return (columns, rows);
        }
    }
}
