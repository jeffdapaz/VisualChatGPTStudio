using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JeffPires.VisualChatGPTStudio.Utils;
using Microsoft.VisualStudio.Data.Services;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;
using OpenAI_API.Functions;

namespace JeffPires.VisualChatGPTStudio.Agents
{
    /// <summary>
    /// Allows executing SQL Server scripts as an Agent
    /// </summary>
    public static class SqlServerAgent
    {
        public static readonly IReadOnlyList<Tool> Tools =
        [
            new(ExecuteReaderAsync)
            {
                Name = "sql_reader",
                Description = "Use for SELECT queries when you need to retrieve multiple rows of data. Returns a full result set.",
                ExampleToSystemMessage = """
                                         Use this when user asks: "show me all products", "list customers", "get order details"

                                         Example usage:
                                         <|tool_call_begin|> functions.sql_reader:1 <|tool_call_argument_begin|> {"query": "SELECT ProductName, Price FROM Products WHERE Category = 'Electronics'"} <|tool_call_end|>
                                         """,
                RiskLevel = RiskLevel.Low,
                Category = "SQL",
                Approval = ApprovalKind.AutoApprove,
                Parameters = new Dictionary<string, Property> { { "query", new Property { Types = ["string"], Description = "Script to be executed." } } },
            },
            new(ExecuteNonQueryAsync)
            {
                Name = "sql_non_query",
                Description = "Use for INSERT, UPDATE, DELETE operations. Modifies data and returns number of affected rows. HIGH RISK - use cautiously.",
                ExampleToSystemMessage = """
                                         Use this when user asks: "add new user", "update price", "delete order"

                                         Example usage:
                                         <|tool_call_begin|> functions.sql_non_query:1 <|tool_call_argument_begin|> {"query": "INSERT INTO Users (Name, Email) VALUES ('John Doe', 'john@example.com')"} <|tool_call_end|>
                                         """,
                RiskLevel = RiskLevel.High,
                Category = "SQL",
                Parameters = new Dictionary<string, Property> { { "query", new Property { Types = ["string"], Description = "Script to be executed." } } },
            },
            new(ExecuteScalarAsync)
            {
                Name = "sql_scalar",
                Description = "Use when you need a single value - counts, sums, averages, or checking existence. Returns one value from first row/column.",
                ExampleToSystemMessage = """
                                         Use this when user asks: "how many users", "what's the total sales", "check if user exists"
                                         Example usage:
                                         <|tool_call_begin|> functions.sql_scalar:1 <|tool_call_argument_begin|> {"query": "SELECT COUNT(*) FROM Orders WHERE OrderDate >= '2024-01-01'"} <|tool_call_end|>
                                         """,
                RiskLevel = RiskLevel.Medium,
                Category = "SQL",
                Parameters = new Dictionary<string, Property> { { "query", new Property { Types = ["string"], Description = "Script to be executed." } } },
            }
        ];

        private static async Task<ToolResult> ExecuteReaderAsync(Tool tool, IReadOnlyDictionary<string, object> args)
        {
            var rows = new List<Dictionary<string, string>>();
            using SqlConnection connection = new(CurrentConnectionString);
            await connection.OpenAsync();
            var query = args.GetString("query");
            if (tool.LogResponseAndRequest)
            {
                Logger.Log($"SQL query: {query}");
            }

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            var columnNames = new List<string>();

            for (var i = 0; i < reader.FieldCount; i++)
            {
                columnNames.Add(reader.GetName(i));
            }

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, string>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[columnNames[i]] = reader.GetValue(i).ToString();
                }
                rows.Add(row);
            }

            var dataToUser = rows.Count > 0
                ? "The data is displayed to the user."
                : "No data is returned.";

            return new ToolResult
            {
                Result = $"Rows retrieved: {rows.Count}. {dataToUser}",
                PrivateResult = JsonUtils.Serialize(rows)
            };
        }

        private static async Task<ToolResult> ExecuteNonQueryAsync(Tool tool, IReadOnlyDictionary<string, object> args)
        {
            using SqlConnection connection = new(CurrentConnectionString);
            await connection.OpenAsync();
            var query = args.GetString("query");
            if (tool.LogResponseAndRequest)
            {
                Logger.Log($"SQL query: {query}");
            }

            using SqlCommand command = new(query, connection);
            var rowsAffected = command.ExecuteNonQuery();

            return new ToolResult
            {
                Result = rowsAffected == -1 && query.ToUpper().Contains("CREATE TABLE")
                    ? "Table created."
                    : "Rows affected: " + rowsAffected
            };
        }

        private static async Task<ToolResult> ExecuteScalarAsync(Tool tool, IReadOnlyDictionary<string, object> args)
        {
            using SqlConnection connection = new(CurrentConnectionString);
            await connection.OpenAsync();
            var query = args.GetString("query");
            if (tool.LogResponseAndRequest)
            {
                Logger.Log($"SQL query: {query}");
            }

            using SqlCommand command = new(query, connection);
            var result = command.ExecuteScalar();

            return new ToolResult
            {
                Result = result?.ToString()
            };
        }

        /// <summary>
        /// Retrieves a list of SQL Server connection information by filtering and processing connections
        /// from the Visual Studio Data Explorer Connection Manager that match a specific SQL Server provider.
        /// </summary>
        /// <returns>
        /// A list of <see cref="SqlServerConnectionInfo"/> objects containing details such as Initial Catalog,
        /// Description, and Connection String for each valid SQL Server connection.
        /// </returns>
        public static List<SqlServerConnectionInfo> GetConnections()
        {
            const string SQL_SERVER_PROVIDER = "91510608-8809-4020-8897-fba057e22d54";

            IVsDataExplorerConnectionManager connectionManager = Package.GetGlobalService(typeof(IVsDataExplorerConnectionManager)) as IVsDataExplorerConnectionManager;

            if (connectionManager == null)
                return [];

            return connectionManager.Connections
                .Where(kvp => kvp.Value.Provider == new Guid(SQL_SERVER_PROVIDER))
                .Select(kvp => kvp.Value.Connection.DisplayConnectionString)
                .Where(connectionString =>
                {
                    return !string.IsNullOrWhiteSpace(GetSqlConnectionStringBuilder(connectionString)?.InitialCatalog);
                })
                .Select(connectionString =>
                {
                    SqlConnectionStringBuilder builder = GetSqlConnectionStringBuilder(connectionString);

                    return new SqlServerConnectionInfo
                    {
                        DataSource = builder.DataSource,
                        InitialCatalog = builder.InitialCatalog,
                        Description = $"{builder.DataSource}: {builder.InitialCatalog}",
                        ConnectionString = builder.ConnectionString
                    };
                })
                .OrderBy(c => c.Description)
                .ToList();
        }

        public static string CurrentConnectionString { get; set; }

        /// <summary>
        /// Creates and returns a SqlConnectionStringBuilder object initialized with the provided connection string after replacing the trusted certificate setting.
        /// </summary>
        /// <param name="connectionString">The database connection string to be processed.</param>
        /// <returns>
        /// A SqlConnectionStringBuilder object with the modified connection string.
        /// </returns>
        private static SqlConnectionStringBuilder GetSqlConnectionStringBuilder(string connectionString)
        {
            return new SqlConnectionStringBuilder(ReplaceTrustedCertificate(connectionString));
        }

        /// <summary>
        /// Replaces occurrences of "trust server certificate" (case-insensitive) in the given connection string with "TrustServerCertificate".
        /// </summary>
        /// <param name="connectionString">The connection string to process.</param>
        /// <returns>
        /// The updated connection string with the replacement applied.
        /// </returns>
        private static string ReplaceTrustedCertificate(string connectionString)
        {
            return Regex.Replace(connectionString, "(?i)trust server certificate", "TrustServerCertificate");
        }
    }

    /// <summary>
    /// Represents the connection information required to connect to a SQL Server database.
    /// </summary>
    public class SqlServerConnectionInfo
    {
        /// <summary>
        /// Gets or sets the data source (server) to be used in the connection string.
        /// </summary>
        public string DataSource { get; set; }

        /// <summary>
        /// Gets or sets the name of the initial catalog (database) to be used in the connection string.
        /// </summary>
        public string InitialCatalog { get; set; }

        /// <summary>
        /// Gets or sets the description text.
        /// </summary>
        public string Description { get; init; }

        /// <summary>
        /// Gets or sets the connection string used to establish a connection to a database or other data source.
        /// </summary>
        public string ConnectionString { get; init; }
    }
}
