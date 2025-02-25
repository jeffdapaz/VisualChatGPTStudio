using Microsoft.VisualStudio.Data.Services;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;
using OpenAI_API.Functions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;

namespace JeffPires.VisualChatGPTStudio.Agents
{
    /// <summary>
    /// Allows executing SQL Server scripts as an Agent
    /// </summary>
    public static class SqlServerAgent
    {
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
            .ToList();
        }

        /// <summary>
        /// Retrieves a list of SQL function requests, each containing details about the function name, description, and required parameters.
        /// </summary>
        /// <returns>
        /// A list of <see cref="FunctionRequest"/> objects representing SQL functions for executing scripts (e.g., ExecuteReader, ExecuteNonQuery, ExecuteScalar).
        /// </returns>
        public static List<FunctionRequest> GetSqlFunctions()
        {
            List<FunctionRequest> functions = [];

            Parameter parameter = new()
            {
                Properties = new Dictionary<string, Property>
                {
                    { nameof(SqlServerConnectionInfo.DataSource), new Property { Type = "string", Description = "DataSource (server) name where the script will be executed." } },
                    { nameof(SqlServerConnectionInfo.InitialCatalog), new Property { Type = "string", Description = "Database name where the script will be executed." } },
                    { "query", new Property { Type = "string", Description = "Script to be executed." } }
                },
                Required = [nameof(SqlServerConnectionInfo.DataSource), nameof(SqlServerConnectionInfo.InitialCatalog), "query"]
            };

            FunctionRequest functionReader = new()
            {
                Function = new()
                {
                    Name = nameof(ExecuteReader),
                    Description = "Execute a SQL Server script to read and retrieve data from the database.",
                    Parameters = parameter
                }
            };

            FunctionRequest functionNonQuery = new()
            {
                Function = new()
                {
                    Name = nameof(ExecuteNonQuery),
                    Description = "Executes a SQL Server script and returns the number of rows affected.",
                    Parameters = parameter
                }
            };

            FunctionRequest functionScalar = new()
            {
                Function = new()
                {
                    Name = nameof(ExecuteScalar),
                    Description = "Executes a SQL Server script, and returns the first column of the first row in the result set returned by the query. Additional columns or rows are ignored.",
                    Parameters = parameter
                }
            };

            functions.Add(functionReader);
            functions.Add(functionNonQuery);
            functions.Add(functionScalar);

            return functions;
        }

        /// <summary>
        /// Generates the database schema as a DDL (Data Definition Language) script, including table creation, primary keys, and foreign keys.
        /// </summary>
        /// <param name="connectionString">The connection string to the database.</param>
        /// <returns>
        /// A string containing the database schema in DDL format.
        /// </returns>
        public static string GetDataBaseSchema(string connectionString)
        {
            const string QUERY = @"DECLARE @DDL NVARCHAR(MAX) = '';

                                    -- Generate CREATE TABLE
                                    SELECT @DDL = @DDL + 'CREATE TABLE ' + t.name + ' (' +
                                        STUFF((
                                            SELECT ', ' + c.name + ' ' + tp.name + 
                                                   CASE 
                                                       WHEN tp.name IN ('varchar', 'nvarchar', 'char', 'nchar') THEN '(' + CAST(c.max_length AS VARCHAR) + ')'
                                                       WHEN tp.name IN ('decimal', 'numeric') THEN '(' + CAST(c.precision AS VARCHAR) + ',' + CAST(c.scale AS VARCHAR) + ')'
                                                       ELSE ''
                                                   END + 
                                                   CASE WHEN c.is_identity = 1 THEN ' IDENTITY(' + CAST(IDENT_SEED(t.name) AS VARCHAR) + ',' + CAST(IDENT_INCR(t.name) AS VARCHAR) + ')' ELSE '' END + 
                                                   CASE WHEN c.is_nullable = 0 THEN ' NOT NULL' ELSE ' NULL' END
                                            FROM sys.columns c
                                            JOIN sys.types tp ON c.user_type_id = tp.user_type_id
                                            WHERE c.object_id = t.object_id
                                            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') + ');' + CHAR(13) + CHAR(10)
                                    FROM sys.tables t;

                                    -- Generate the PKs 
                                    SELECT @DDL = @DDL + 'ALTER TABLE ' + t.name + ' ADD CONSTRAINT ' + kc.name + ' PRIMARY KEY (' +
                                        STUFF((
                                            SELECT ', ' + c.name
                                            FROM sys.index_columns ic
                                            JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                                            WHERE ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
                                            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') + ');' + CHAR(13) + CHAR(10)
                                    FROM sys.key_constraints kc
                                    JOIN sys.tables t ON kc.parent_object_id = t.object_id
                                    WHERE kc.type = 'PK';

                                    -- Generate the FKs
                                    SELECT @DDL = @DDL + 'ALTER TABLE ' + t.name + ' ADD CONSTRAINT ' + fk.name + 
                                           ' FOREIGN KEY (' +
                                           STUFF((
                                               SELECT ', ' + fc.name
                                               FROM sys.foreign_key_columns fkc
                                               JOIN sys.columns fc ON fkc.parent_object_id = fc.object_id AND fkc.parent_column_id = fc.column_id
                                               WHERE fkc.constraint_object_id = fk.object_id
                                               FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') + 
                                           ') REFERENCES ' + rt.name + ' (' +
                                           STUFF((
                                               SELECT ', ' + rc.name
                                               FROM sys.foreign_key_columns fkc
                                               JOIN sys.columns rc ON fkc.referenced_object_id = rc.object_id AND fkc.referenced_column_id = rc.column_id
                                               WHERE fkc.constraint_object_id = fk.object_id
                                               FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') + ');' + CHAR(13) + CHAR(10)
                                    FROM sys.foreign_keys fk
                                    JOIN sys.tables t ON fk.parent_object_id = t.object_id
                                    JOIN sys.tables rt ON fk.referenced_object_id = rt.object_id;

                                    SELECT @DDL AS DatabaseDDL;";

            string result = ExecuteScalar(connectionString, QUERY);

            SqlConnectionStringBuilder sqlConnectionStringBuilder = GetSqlConnectionStringBuilder(connectionString);

            result = string.Format("{0}: {1}, {2}: {3}{4}{5}",
                                    nameof(sqlConnectionStringBuilder.DataSource),
                                    sqlConnectionStringBuilder.DataSource,
                                    nameof(sqlConnectionStringBuilder.InitialCatalog),
                                    sqlConnectionStringBuilder.InitialCatalog,
                                    Environment.NewLine,
                                    result);

            return result;
        }

        /// <summary>
        /// Executes a specified SQL function (ExecuteReader, ExecuteNonQuery, or ExecuteScalar) on a given database connection 
        /// and returns the result. Optionally outputs the reader result for ExecuteReader operations.
        /// </summary>
        /// <param name="function">The function to execute, including its name and arguments.</param>
        /// <param name="logQueries">If true, all queries executed by the SQL Server Agent will be logged to the Output window.</param>
        /// <param name="readerResult">An output parameter to store the result of ExecuteReader operations.</param>        
        /// <returns>
        /// The result of the executed SQL function as a string.
        /// </returns>
        public static string ExecuteFunction(FunctionResult function, bool logQueries, out DataView readerResult)
        {
            readerResult = null;
            string functionResult;

            try
            {
                JObject arguments = JObject.Parse(function.Function.Arguments);

                string dataSource = arguments[nameof(SqlServerConnectionInfo.DataSource)]?.Value<string>();
                string initialCatalog = arguments[nameof(SqlServerConnectionInfo.InitialCatalog)]?.Value<string>();
                string query = arguments[nameof(query)].Value<string>();

                string connectionString = GetConnections().FirstOrDefault(c => c.DataSource == dataSource && c.InitialCatalog == initialCatalog)?.ConnectionString;

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return $"The datasource {dataSource} and initial catalog {initialCatalog} was not found.";
                }

                if (function.Function.Name.Equals(nameof(SqlServerAgent.ExecuteReader)))
                {
                    functionResult = SqlServerAgent.ExecuteReader(connectionString, query, out readerResult);
                }
                else if (function.Function.Name.Equals(nameof(SqlServerAgent.ExecuteNonQuery)))
                {
                    functionResult = SqlServerAgent.ExecuteNonQuery(connectionString, query);
                }
                else if (function.Function.Name.Equals(nameof(SqlServerAgent.ExecuteScalar)))
                {
                    functionResult = SqlServerAgent.ExecuteScalar(connectionString, query);
                }
                else
                {
                    functionResult = $"The function {function.Function.Name} not exists.";
                }

                if (logQueries)
                {
                    Logger.Log(query);
                    Logger.Log(new string('_', 100));
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                functionResult = ex.ToString();
            }

            return functionResult;
        }

        /// <summary>
        /// Executes a SQL query using the provided connection string and retrieves the result as a list of dictionaries, 
        /// where each dictionary represents a row with column names as keys and their corresponding values.
        /// </summary>
        /// <param name="connectionString">The connection string to the database.</param>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="result">An output parameter that contains the query result.</param>
        /// <returns>
        /// A string message indicating the number of rows retrieved.
        /// </returns>
        private static string ExecuteReader(string connectionString, string query, out DataView result)
        {
            result = [];
            List<Dictionary<string, object>> rows = [];

            using (SqlConnection connection = new(connectionString))
            {
                connection.Open();

                string messageOutput = string.Empty;

                using (SqlCommand command = new(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        List<string> columnNames = [];

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            columnNames.Add(reader.GetName(i));
                        }

                        while (reader.Read())
                        {
                            Dictionary<string, object> row = [];

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[columnNames[i]] = reader.GetValue(i);
                            }

                            rows.Add(row);
                        }
                    }

                    result = ConvertReaderResultToDataTable(rows);

                    return "Rows retrieved: " + result.Count;
                }
            }
        }

        /// <summary>
        /// Executes a non-query SQL command (e.g., INSERT, UPDATE, DELETE) using the provided connection string and query.
        /// </summary>
        /// <param name="connectionString">The connection string to the database.</param>
        /// <param name="query">The SQL query to execute.</param>
        /// <returns>
        /// A string indicating the number of rows affected by the query.
        /// </returns>
        private static string ExecuteNonQuery(string connectionString, string query)
        {
            using (SqlConnection connection = new(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new(query, connection))
                {
                    int rowsAffected = command.ExecuteNonQuery();

                    return "Rows affected: " + rowsAffected;
                }
            }
        }

        /// <summary>
        /// Executes a scalar SQL query and returns the result as a string.
        /// </summary>
        /// <param name="connectionString">The connection string to the database.</param>
        /// <param name="query">The SQL query to execute.</param>
        /// <returns>
        /// The result of the scalar query as a string.
        /// </returns>
        private static string ExecuteScalar(string connectionString, string query)
        {
            using (SqlConnection connection = new(connectionString))
            {
                SqlCommand command = new(query, connection);

                connection.Open();

                object result = command.ExecuteScalar();

                return result?.ToString();
            }
        }

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
        /// Converts a list of dictionaries representing rows of data into a DataView object. 
        /// Each dictionary's keys are used as column names, and the values populate the rows of the DataTable.
        /// </summary>
        /// <param name="readerResult">A list of dictionaries where each dictionary represents a row of data with column names as keys.</param>
        /// <returns>
        /// A DataView object representing the converted data from the input list of dictionaries.
        /// </returns>
        private static DataView ConvertReaderResultToDataTable(List<Dictionary<string, object>> readerResult)
        {
            DataTable dataTable = new();

            foreach (string key in readerResult.First().Keys)
            {
                dataTable.Columns.Add(key);
            }

            foreach (Dictionary<string, object> row in readerResult)
            {
                DataRow dataRow = dataTable.NewRow();

                foreach (string key in row.Keys)
                {
                    dataRow[key] = row[key] ?? DBNull.Value;
                }

                dataTable.Rows.Add(dataRow);
            }

            return dataTable.DefaultView;
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
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the connection string used to establish a connection to a database or other data source.
        /// </summary>
        public string ConnectionString { get; set; }
    }
}