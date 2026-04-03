using JeffPires.VisualChatGPTStudio.Utils;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using VisualChatGPTStudioShared.Agents.McpAgent;

namespace VisualChatGPTStudioShared.Utils.Repositories
{
    /// <summary>
    /// Represents a repository for managing MCP server definitions.
    /// </summary>
    public class McpAgentRepository
    {
        #region Properties

        private static SQLiteConnection connection;

        #endregion Properties

        #region Methods

        /// <summary>
        /// Creates a database connection and initializes MCP tables.
        /// </summary>
        public static void CreateDataBase()
        {
            try
            {
                connection = Repository.CreateDataBaseAndConnection();

                CreateTableMcpServers();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);

                MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Creates the MCP_SERVERS table if it does not exist.
        /// </summary>
        private static void CreateTableMcpServers()
        {
            string query = @"CREATE TABLE IF NOT EXISTS MCP_SERVERS
                            (
                                ID                         VARCHAR(50)  PRIMARY KEY UNIQUE NOT NULL,
                                NAME                       VARCHAR(255) NOT NULL,
                                TRANSPORT_TYPE             INTEGER      NOT NULL,
                                COMMAND                    TEXT         NULL,
                                ARGUMENTS                  TEXT         NULL,
                                WORKING_DIRECTORY          TEXT         NULL,
                                ENDPOINT                   TEXT         NULL,
                                ENVIRONMENT_VARIABLES_JSON TEXT         NULL,
                                ENABLED                    INTEGER      NOT NULL
                            );";

            SQLiteCommand command = connection.CreateCommand(query);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Retrieves an MCP server by its name.
        /// </summary>
        /// <param name="name">The server name.</param>
        /// <returns>The matching <see cref="McpServerItem"/> or null.</returns>
        public static McpServerItem GetMcpServer(string name)
        {
            SQLiteCommand command = connection.CreateCommand(@"SELECT
                                                                    ID                         AS Id,
                                                                    NAME                       AS Name,
                                                                    TRANSPORT_TYPE             AS TransportTypeAsInteger,
                                                                    COMMAND                    AS Command,
                                                                    ARGUMENTS                  AS Arguments,
                                                                    WORKING_DIRECTORY          AS WorkingDirectory,
                                                                    ENDPOINT                   AS Endpoint,
                                                                    ENVIRONMENT_VARIABLES_JSON AS EnvironmentVariablesJson,
                                                                    ENABLED                    AS Enabled
                                                               FROM MCP_SERVERS
                                                               WHERE NAME = ?", name);

            return command.ExecuteQuery<McpServerItem>().FirstOrDefault();
        }

        /// <summary>
        /// Retrieves all MCP server definitions.
        /// </summary>
        /// <returns>A list of MCP servers.</returns>
        public static List<McpServerItem> GetMcpServers()
        {
            SQLiteCommand command = connection.CreateCommand(@"SELECT
                                                                    ID                         AS Id,
                                                                    NAME                       AS Name,
                                                                    TRANSPORT_TYPE             AS TransportTypeAsInteger,
                                                                    COMMAND                    AS Command,
                                                                    ARGUMENTS                  AS Arguments,
                                                                    WORKING_DIRECTORY          AS WorkingDirectory,
                                                                    ENDPOINT                   AS Endpoint,
                                                                    ENVIRONMENT_VARIABLES_JSON AS EnvironmentVariablesJson,
                                                                    ENABLED                    AS Enabled
                                                               FROM MCP_SERVERS");

            return command.ExecuteQuery<McpServerItem>();
        }

        /// <summary>
        /// Inserts a new MCP server or updates an existing one.
        /// </summary>
        /// <param name="server">The MCP server to persist.</param>
        /// <returns>The server identifier.</returns>
        public static string InsertOrUpdate(McpServerItem server)
        {
            long count = 0;

            if (!string.IsNullOrWhiteSpace(server.Id))
            {
                SQLiteCommand command = connection.CreateCommand("SELECT COUNT(1) FROM MCP_SERVERS WHERE ID = ?", server.Id);

                count = command.ExecuteScalar<long>();
            }

            if (count > 0)
            {
                UpdateMcpServer(server);

                return server.Id;
            }

            return AddMcpServer(server);
        }

        /// <summary>
        /// Adds a new MCP server definition.
        /// </summary>
        /// <param name="server">The server to add.</param>
        /// <returns>The generated server identifier.</returns>
        public static string AddMcpServer(McpServerItem server)
        {
            string serverId = Guid.NewGuid().ToString();

            SQLiteCommand command = connection.CreateCommand(@"INSERT INTO MCP_SERVERS
                                                               (
                                                                   ID,
                                                                   NAME,
                                                                   TRANSPORT_TYPE,
                                                                   COMMAND,
                                                                   ARGUMENTS,
                                                                   WORKING_DIRECTORY,
                                                                   ENDPOINT,
                                                                   ENVIRONMENT_VARIABLES_JSON,
                                                                   ENABLED
                                                               )
                                                               VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)",
                                                               serverId,
                                                               server.Name,
                                                               server.TransportTypeAsInteger,
                                                               server.Command,
                                                               server.Arguments,
                                                               server.WorkingDirectory,
                                                               server.Endpoint,
                                                               server.EnvironmentVariablesJson,
                                                               server.Enabled);

            command.ExecuteNonQuery();

            return serverId;
        }

        /// <summary>
        /// Updates an MCP server definition.
        /// </summary>
        /// <param name="server">The server to update.</param>
        public static void UpdateMcpServer(McpServerItem server)
        {
            SQLiteCommand command = connection.CreateCommand(@"UPDATE MCP_SERVERS
                                                               SET
                                                                   NAME = ?,
                                                                   TRANSPORT_TYPE = ?,
                                                                   COMMAND = ?,
                                                                   ARGUMENTS = ?,
                                                                   WORKING_DIRECTORY = ?,
                                                                   ENDPOINT = ?,
                                                                   ENVIRONMENT_VARIABLES_JSON = ?,
                                                                   ENABLED = ?
                                                               WHERE ID = ?",
                                                               server.Name,
                                                               server.TransportTypeAsInteger,
                                                               server.Command,
                                                               server.Arguments,
                                                               server.WorkingDirectory,
                                                               server.Endpoint,
                                                               server.EnvironmentVariablesJson,
                                                               server.Enabled,
                                                               server.Id);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Deletes an MCP server definition.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        public static void DeleteMcpServer(string serverId)
        {
            SQLiteCommand command = connection.CreateCommand("DELETE FROM MCP_SERVERS WHERE ID = ?", serverId);

            command.ExecuteNonQuery();
        }

        #endregion Methods
    }
}
