using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using VisualChatGPTStudioShared.ToolWindows.Turbo;
using VisualChatGPTStudioShared.Utils.Repositories;

namespace JeffPires.VisualChatGPTStudio.Utils.Repositories
{
    /// <summary>
    /// Repository class for managing the Turbo Chats.
    /// </summary>
    public static class ChatRepository
    {
        #region Constantes

        private const string PARAMETER_ID = "@PARAMETER_ID";
        private const string PARAMETER_NAME = "@PARAMETER_NAME";
        private const string PARAMETER_DATE = "@PARAMETER_DATE";
        private const string PARAMETER_MESSAGES = "@PARAMETER_MESSAGES";

        #endregion Constantes

        #region Properties

        private static SQLiteConnection connection;



        #endregion Properties

        #region Chats

        /// <summary>
        /// Creates a database and establishes a connection. Initializes required tables for chats and SQL Server connections.
        /// Logs and displays any exceptions that occur during the process.
        /// </summary>
        public static void CreateDataBase()
        {
            try
            {
                connection = Repository.CreateDataBaseAndConnection();

                CreateTableChats();
                CreateTableSqlServerConnections();
                CreateTableApiDefinitions();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);

                MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Creates a table named CHATS in the SQLite database.
        /// </summary>
        private static void CreateTableChats()
        {
            string query = @"CREATE TABLE IF NOT EXISTS CHATS
                            (
                                ID       VARCHAR(50)  PRIMARY KEY UNIQUE NOT NULL,
                                NAME     TEXT         NOT NULL,
                                DATE     DATETIME     NOT NULL,
                                MESSAGES TEXT         NOT NULL
                            );";

            SQLiteCommand command = connection.CreateCommand(query);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Creates the SQL_SERVER_CONNECTIONS table in the database if it does not already exist.
        /// The table includes columns for ID, CHAT_ID, and CONNECTION, with ID serving as the primary key.
        /// </summary>
        private static void CreateTableSqlServerConnections()
        {
            string query = @"CREATE TABLE IF NOT EXISTS SQL_SERVER_CONNECTIONS
                            (
                                ID         VARCHAR(50)  PRIMARY KEY UNIQUE NOT NULL,
                                CHAT_ID    VARCHAR(50)  NOT NULL,
                                CONNECTION VARCHAR(255) NOT NULL
                            );";

            SQLiteCommand command = connection.CreateCommand(query);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Creates the API_DEFINITIONS table in the database if it does not already exist.
        /// </summary>
        private static void CreateTableApiDefinitions()
        {
            string query = @"CREATE TABLE IF NOT EXISTS API_DEFINITIONS
                            (
                                ID       VARCHAR(50)  PRIMARY KEY UNIQUE NOT NULL,
                                CHAT_ID  VARCHAR(50)  NOT NULL,
                                API_NAME VARCHAR(255) NOT NULL
                            );";

            SQLiteCommand command = connection.CreateCommand(query);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Retrieves a list of ChatEntity objects from the database.
        /// </summary>
        /// <returns>
        /// A list of ChatEntity objects containing the ID, Name, and Date properties.
        /// </returns>
        public static List<ChatEntity> GetChats()
        {
            if (connection == null)
            {
                return [];
            }

            SQLiteCommand command = connection.CreateCommand("SELECT ID AS Id, NAME AS name, DATE AS dateRaw FROM CHATS ORDER BY DATE DESC");

            return command.ExecuteQuery<ChatEntity>();
        }

        /// <summary>
        /// Retrieves a list of messages from the database for a given chat ID.
        /// </summary>
        /// <param name="chatId">The ID of the chat.</param>
        /// <returns>A list of MessageEntity objects representing the messages.</returns>
        public static List<MessageEntity> GetMessages(string chatId)
        {
            SQLiteCommand command = connection.CreateCommand("SELECT MESSAGES FROM CHATS WHERE ID = ?", chatId);

            string messages = command.ExecuteScalar<string>();

            return messages == null ? [] : JsonConvert.DeserializeObject<List<MessageEntity>>(messages);
        }

        public static void UpdateMessages(string chatId, List<MessageEntity> messages)
        {
            string query = $@"UPDATE
                                CHATS
                            SET
                                MESSAGES = {PARAMETER_MESSAGES}
                            WHERE
                                ID = {PARAMETER_ID}";

            SQLiteCommand command = connection.CreateCommand(query);

            command.Bind(PARAMETER_ID, chatId);
            command.Bind(PARAMETER_MESSAGES, JsonConvert.SerializeObject(messages));

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Adds a chat entity to the database.
        /// </summary>
        /// <param name="chat">The chat entity to add.</param>
        public static void AddChat(ChatEntity chat)
        {
            string query = $@"INSERT INTO CHATS
                            (
                                ID,
                                NAME,
                                DATE,
                                MESSAGES
                            )
                            VALUES
                            (
                                {PARAMETER_ID},
                                {PARAMETER_NAME},
                                {PARAMETER_DATE},
                                {PARAMETER_MESSAGES}
                            )";

            SQLiteCommand command = connection.CreateCommand(query);

            command.Bind(PARAMETER_ID, chat.Id);
            command.Bind(PARAMETER_NAME, chat.Name);
            command.Bind(PARAMETER_DATE, chat.Date.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Bind(PARAMETER_MESSAGES, JsonConvert.SerializeObject(chat.Messages));

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Updates the chat entity in the database with the provided information.
        /// </summary>
        /// <param name="chat">The chat entity to be updated.</param>
        public static void UpdateChat(ChatEntity chat)
        {
            string query = $@"UPDATE
                                CHATS
                            SET
                                NAME     = {PARAMETER_NAME},
                                DATE     = {PARAMETER_DATE},
                                MESSAGES = {PARAMETER_MESSAGES}
                            WHERE
                                ID = {PARAMETER_ID}";

            SQLiteCommand command = connection.CreateCommand(query);

            command.Bind(PARAMETER_ID, chat.Id);
            command.Bind(PARAMETER_NAME, chat.Name);
            command.Bind(PARAMETER_DATE, chat.Date.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Bind(PARAMETER_MESSAGES, JsonConvert.SerializeObject(chat.Messages));

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Updates the name of the chat entity in the database based on the provided ID.
        /// </summary>
        /// <param name="chatId">The ID of the chat to be updated.</param>
        /// <param name="newName">The new name for the chat.</param>
        public static void UpdateChatName(string chatId, string newName)
        {
            SQLiteCommand command = connection.CreateCommand($"UPDATE CHATS SET NAME = {PARAMETER_NAME} WHERE ID = {PARAMETER_ID}");

            command.Bind(PARAMETER_NAME, newName);
            command.Bind(PARAMETER_ID, chatId);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Deletes the chat entity from the database based on the provided ID.
        /// </summary>
        /// <param name="chatId">The ID of the chat to be deleted.</param>
        public static void DeleteChat(string chatId)
        {
            SQLiteCommand command = connection.CreateCommand("DELETE FROM CHATS WHERE ID = ?", chatId);
            command.ExecuteNonQuery();
        }

        #endregion Chats
    }
}
