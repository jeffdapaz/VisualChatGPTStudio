using JeffPires.VisualChatGPTStudio.Utils;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using VisualChatGPTStudioShared.Agents.ApiAgent;

namespace VisualChatGPTStudioShared.Utils.Repositories
{
    /// <summary>
    /// Represents a repository for managing API agents, providing methods for data access and manipulation related to API agent entities.
    /// </summary>
    public class ApiAgentRepository
    {
        #region Properties

        private static SQLiteConnection connection;

        #endregion Properties

        #region Methods

        /// <summary>
        /// Creates a database and establishes a connection. Also initializes the necessary tables for APIs. 
        /// Logs and displays an error message if an exception occurs during the process.
        /// </summary>
        public static void CreateDataBase()
        {
            try
            {
                connection = Repository.CreateDataBaseAndConnection();

                CreateTableAPIs();
                CreateTableTags();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);

                MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Creates a table named APIS in the SQLite database.
        /// </summary>
        private static void CreateTableAPIs()
        {
            string query = @"CREATE TABLE IF NOT EXISTS APIS
                            (
                                ID         VARCHAR(50)  PRIMARY KEY UNIQUE NOT NULL,
                                NAME       VARCHAR(255) NOT NULL,
                                BASE_URL   VARCHAR(255) NOT NULL,
                                DEFINITION TEXT         NOT NULL                                       
                            );";

            SQLiteCommand command = connection.CreateCommand(query);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Creates the TAGS table in the database if it does not already exist.
        /// </summary>
        private static void CreateTableTags()
        {
            string query = @"CREATE TABLE IF NOT EXISTS TAGS
                            (
                                API_ID   VARCHAR(50)  NOT NULL,
                                KEY      VARCHAR(255) NOT NULL,
                                VALUE    TEXT         NOT NULL,
                                TYPE     INTEGER      NOT NULL,
                                PRIMARY KEY (API_ID, KEY)
                            );";

            SQLiteCommand command = connection.CreateCommand(query);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Retrieves an API item from the database by its name, including its associated tags.
        /// </summary>
        /// <param name="name">The name of the API to retrieve.</param>
        /// <returns>
        /// An <see cref="ApiItem"/> object if found; otherwise, <c>null</c>.
        /// </returns>
        public static ApiItem GetAPI(string name)
        {
            SQLiteCommand command = connection.CreateCommand("SELECT ID AS Id, NAME AS Name, BASE_URL AS BaseUrl, DEFINITION AS Definition FROM APIS WHERE Name = ?", name);

            ApiItem api = command.ExecuteQuery<ApiItem>().FirstOrDefault();

            if (api == null)
            {
                return null;
            }

            api.Tags = GetApiTags(api.Id);

            return api;
        }

        /// <summary>
        /// Retrieves a list of APIs from the database, including their associated tags.
        /// </summary>
        /// <returns>
        /// A list of <see cref="ApiItem"/> objects, each containing API details and their associated tags.
        /// </returns>
        public static List<ApiItem> GetAPIs()
        {
            SQLiteCommand command = connection.CreateCommand("SELECT ID AS Id, NAME AS Name, BASE_URL AS BaseUrl, DEFINITION AS Definition FROM APIS");

            List<ApiItem> result = command.ExecuteQuery<ApiItem>();

            foreach (ApiItem api in result)
            {
                api.Tags = GetApiTags(api.Id);
            }

            return result;
        }

        /// <summary>
        /// Retrieves a list of API tags associated with the specified API ID from the database.
        /// </summary>
        /// <param name="apiId">The unique identifier of the API for which tags are to be retrieved.</param>
        /// <returns>
        /// A list of <see cref="ApiTagItem"/> objects containing the key, value, and type information of the tags.
        /// </returns>
        public static List<ApiTagItem> GetApiTags(string apiId)
        {
            SQLiteCommand command = connection.CreateCommand("SELECT KEY AS Key, VALUE AS Value, TYPE AS TypeAsInteger FROM TAGS WHERE API_ID = ?", apiId);

            return command.ExecuteQuery<ApiTagItem>();
        }

        /// <summary>
        /// Inserts a new API or updates an existing one based on the API ID.
        /// </summary>
        /// <param name="api">The API item to insert or update.</param>
        public static string InsertOrUpdate(ApiItem api)
        {
            long count = 0;

            if (!string.IsNullOrWhiteSpace(api.Id))
            {
                SQLiteCommand command = connection.CreateCommand("SELECT COUNT(1) FROM APIS WHERE ID = ?", api.Id);

                count = command.ExecuteScalar<long>();
            }

            if (count > 0)
            {
                UpdateApi(api);

                return api.Id;
            }

            return AddApi(api);
        }

        /// <summary>
        /// Adds a new API to the database, including its associated tags, and returns the generated API ID.
        /// </summary>
        /// <param name="api">The API item containing details such as name, base URL, definition, and tags.</param>
        /// <returns>
        /// The unique identifier (ID) of the newly added API.
        /// </returns>
        public static string AddApi(ApiItem api)
        {
            string apiId = Guid.NewGuid().ToString();

            SQLiteCommand command = connection.CreateCommand("INSERT INTO APIS (ID, NAME, BASE_URL, DEFINITION) VALUES (?, ?, ?, ?)", apiId, api.Name, api.BaseUrl, api.Definition);

            command.ExecuteNonQuery();

            foreach (ApiTagItem tag in api.Tags)
            {
                AddApiTag(apiId, tag);
            }

            return apiId;
        }

        /// <summary>
        /// Adds a tag to the specified API by inserting it into the TAGS table in the database.
        /// </summary>
        public static void AddApiTag(string apiId, ApiTagItem tag)
        {
            SQLiteCommand command = connection.CreateCommand("INSERT INTO TAGS (API_ID, KEY, VALUE, TYPE) VALUES (?, ?, ?, ?)", apiId, tag.Key, tag.Value, tag.TypeAsInteger);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Deletes an API and its associated tags from the database using the provided API ID.
        /// </summary>
        public static void DeleteApi(string apiId)
        {
            SQLiteCommand command = connection.CreateCommand("DELETE FROM APIS WHERE ID = ?", apiId);

            command.ExecuteNonQuery();

            SQLiteCommand tagCommand = connection.CreateCommand("DELETE FROM TAGS WHERE API_ID = ?", apiId);

            tagCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// Updates an existing API record in the database, including its name, base URL, and definition.
        /// Deletes all associated tags and re-adds the updated tags for the API.
        /// </summary>
        public static void UpdateApi(ApiItem api)
        {
            SQLiteCommand command = connection.CreateCommand("UPDATE APIS SET NAME = ?, BASE_URL = ?, DEFINITION = ? WHERE ID = ?", api.Name, api.BaseUrl, api.Definition, api.Id);

            command.ExecuteNonQuery();

            SQLiteCommand deleteTagsCommand = connection.CreateCommand("DELETE FROM TAGS WHERE API_ID = ?", api.Id);

            deleteTagsCommand.ExecuteNonQuery();

            foreach (ApiTagItem tag in api.Tags)
            {
                AddApiTag(api.Id, tag);
            }
        }

        #endregion Methods
    }
}