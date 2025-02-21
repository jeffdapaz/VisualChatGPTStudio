using JeffPires.VisualChatGPTStudio.Utils.API;
using Microsoft.VisualStudio.Data.Services;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace JeffPires.VisualChatGPTStudio.Agents
{
    public static class SqlServerAgent
    {
        public static List<SqlServerConnectionInfo> GetConnections()
        {
            const string SQL_SERVER_PROVIDER = "91510608-8809-4020-8897-fba057e22d54";

            IVsDataExplorerConnectionManager connectionManager = Package.GetGlobalService(typeof(IVsDataExplorerConnectionManager)) as IVsDataExplorerConnectionManager;

            return connectionManager.Connections
            .Where(kvp => kvp.Value.Provider == new Guid(SQL_SERVER_PROVIDER))
            .Select(kvp => kvp.Value.Connection.DisplayConnectionString)
            .Where(connectionString =>
            {
                return !string.IsNullOrWhiteSpace(GetInitialCatalogValue(connectionString));
            })
            .Select(connectionString =>
            {
                SqlConnectionStringBuilder builder = new(connectionString);

                return new SqlServerConnectionInfo
                {
                    Description = $"{builder.DataSource}: {builder.InitialCatalog}",
                    ConnectionString = connectionString
                };
            })
            .ToList();
        }

        public static List<FunctionRequest> GetSqlFunctions()
        {
            List<FunctionRequest> functions = [];

            Parameter parameter = new ()
            {
                Properties = new Dictionary<string, Property>
                {
                    { "database", new Property { Type = "string", Description = "Database name where the script will be executed." } },
                    { "query", new Property { Type = "string", Description = "Script to be executed." } }
                },
                Required = ["database", "query"]
            };

            FunctionRequest functionReader = new()
            {
                Function = new()
                {
                    Name = nameof(ExecuteQueryReader),
                    Description = "Execute a SQL Server script to read and retrieve data from the database.",
                    Parameters = parameter
                }
            };

            FunctionRequest functionScalar = new()
            {
                Function = new()
                {
                    Name = nameof(ExecuteQueryScalar),
                    Description = "Execute a SQL Server scalar script to insert, update, delete, etc.",
                    Parameters = parameter
                }
            };

            functions.Add(functionReader);
            functions.Add(functionScalar);

            return functions;
        }

        public static string GetDataBaseSchema(string connectionString)
        {
            const string QUERY = @"DECLARE @DDL NVARCHAR(MAX) = '';

                                    SELECT @DDL = @DDL + 'CREATE TABLE ' + t.name + ' (' +
                                        STUFF((
                                            SELECT ', ' + c.name + ' ' + tp.name + 
                                                   CASE 
                                                       WHEN tp.name IN ('varchar', 'nvarchar', 'char', 'nchar') THEN '(' + CAST(c.max_length AS VARCHAR) + ')'
                                                       WHEN tp.name IN ('decimal', 'numeric') THEN '(' + CAST(c.precision AS VARCHAR) + ',' + CAST(c.scale AS VARCHAR) + ')'
                                                       ELSE ''
                                                   END + 
                                                   CASE WHEN c.is_nullable = 0 THEN ' NOT NULL' ELSE ' NULL' END
                                            FROM sys.columns c
                                            JOIN sys.types tp ON c.user_type_id = tp.user_type_id
                                            WHERE c.object_id = t.object_id
                                            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') + ');' + CHAR(13) + CHAR(10)
                                    FROM sys.tables t;

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

            _ = ExecuteQueryReader(connectionString, QUERY, out List<object> queryResult);

            string result = GetInitialCatalogValue(connectionString) + ": ";

            if (queryResult[0] is object[] firstRow && firstRow.Length > 0)
            {
                return result += firstRow[0].ToString();
            }

            return result += queryResult[0].ToString();
        }

        public static string ExecuteQueryReader(string connectionString, string query, out List<object> result)
        {
            result = [];

            using (SqlConnection connection = new(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new(query, connection))
                {
                    SqlParameter outputParam = new("@MessageOutput", SqlDbType.NVarChar, 4000) { Direction = ParameterDirection.Output };

                    command.Parameters.Add(outputParam);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            object[] row = new object[reader.FieldCount];
                            reader.GetValues(row);
                            result.Add(row);
                        }
                    }

                    return $"Query executed successfully with {result.Count} rows.";
                }
            }
        }

        public static string ExecuteQueryScalar(string connectionString, string query)
        {
            try
            {
                using (SqlConnection connection = new(connectionString))
                {
                    SqlCommand command = new(query, connection);

                    connection.Open();

                    object result = command.ExecuteScalar();

                    return result != null ? result.ToString() : string.Empty;
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static string GetInitialCatalogValue(string connectionString)
        {
            return new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        }
    }

    public class SqlServerConnectionInfo
    {
        public string Description { get; set; }

        public string ConnectionString { get; set; }
    }
}
