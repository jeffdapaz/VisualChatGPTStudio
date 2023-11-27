using Newtonsoft.Json;using System;using System.Collections.Generic;using System.Data.SQLite;
using System.IO;using System.Text;using System.Windows;using VisualChatGPTStudioShared.ToolWindows.Turbo;namespace JeffPires.VisualChatGPTStudio.Utils{
    /// <summary>
    /// Repository class for managing the Turbo Chats.
    /// </summary>
    public static class ChatRepository    {
        #region Constantes
        private const string PARAMETER_ID = "@PARAMETER_ID";        private const string PARAMETER_NAME = "@PARAMETER_NAME";        private const string PARAMETER_DATE = "@PARAMETER_DATE";        private const string PARAMETER_MESSAGES = "@PARAMETER_MESSAGES";

        #endregion Constantes
        #region Properties
        private static SQLiteConnection connection;

        #endregion Properties
        #region Methods

        /// <summary>
        /// Creates a database file for VisualChatGptStudio if it does not already exist and opens a connection to it.
        /// </summary>
        public static void CreateDataBase()        {            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Constants.EXTENSION_NAME);

                string filePath = Path.Combine(folder, "VisualChatGptStudio.db");

                if (!File.Exists(filePath))
                {
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }

                    StreamWriter file = new(filePath, true, Encoding.Default);

                    file.Dispose();
                }

                connection = new SQLiteConnection($"Data Source={filePath}");

                connection.Open();

                CreateTableChats();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Creates a table named CHATS in the SQLite database.
        /// </summary>
        private static void CreateTableChats()        {            using (SQLiteCommand command = connection.CreateCommand())            {                command.CommandText = @"CREATE TABLE IF NOT EXISTS CHATS                                        (                                            ID       VARCHAR(50)  PRIMARY KEY UNIQUE NOT NULL,                                            NAME     TEXT         NOT NULL,                                            DATE     DATETIME     NOT NULL,                                            MESSAGES TEXT         NOT NULL                                                                                   );";                command.ExecuteNonQuery();            }        }

        /// <summary>
        /// Retrieves a list of ChatEntity objects from the database.
        /// </summary>
        /// <returns>
        /// A list of ChatEntity objects containing the ID, Name, and Date properties.
        /// </returns>
        public static List<ChatEntity> GetChats()        {            SQLiteDataReader reader;            using (SQLiteCommand command = connection.CreateCommand())            {                command.CommandText = @"SELECT ID, NAME, DATE FROM CHATS";                reader = command.ExecuteReader();            }            List<ChatEntity> chats = new();            while (reader.Read())            {                ChatEntity chat = new()                {                    Id = reader.GetString(0),                    Name = reader.GetString(1),                    Date = reader.GetDateTime(2)                };                chats.Add(chat);            }            return chats;        }

        /// <summary>
        /// Retrieves a list of messages from the database for a given chat ID.
        /// </summary>
        /// <param name="chatId">The ID of the chat.</param>
        /// <returns>A list of MessageEntity objects representing the messages.</returns>
        public static List<MessageEntity> GetMessages(string chatId)        {            const string PARAMETER_CHAT_ID = "@PARAMETER_CHAT_ID";            SQLiteDataReader reader;            using (SQLiteCommand command = connection.CreateCommand())            {                command.CommandText = $"SELECT MESSAGES FROM CHATS WHERE ID = {PARAMETER_CHAT_ID}";                command.Parameters.AddWithValue(PARAMETER_CHAT_ID, chatId);                reader = command.ExecuteReader();            }            StringBuilder result = new();            while (reader.Read())            {                result.Append(reader.GetString(0));            }            return JsonConvert.DeserializeObject<List<MessageEntity>>(result.ToString());        }

        /// <summary>
        /// Adds a chat entity to the database.
        /// </summary>
        /// <param name="chat">The chat entity to add.</param>
        public static void AddChat(ChatEntity chat)        {            using (SQLiteCommand command = connection.CreateCommand())            {                command.CommandText = $@"INSERT INTO CHATS                                         (                                            ID,                                            NAME,                                            DATE,                                            MESSAGES                                         )                                         VALUES                                         (                                            {PARAMETER_ID},                                            {PARAMETER_NAME},                                            {PARAMETER_DATE},                                            {PARAMETER_MESSAGES}                                         )";                command.Parameters.AddWithValue(PARAMETER_ID, chat.Id);                command.Parameters.AddWithValue(PARAMETER_NAME, chat.Name);                command.Parameters.AddWithValue(PARAMETER_DATE, chat.Date.ToString("yyyy-MM-dd HH:mm:ss"));                command.Parameters.AddWithValue(PARAMETER_MESSAGES, JsonConvert.SerializeObject(chat.Messages));                command.ExecuteNonQuery();            }        }

        /// <summary>
        /// Updates the chat entity in the database with the provided information.
        /// </summary>
        /// <param name="chat">The chat entity to be updated.</param>
        public static void UpdateChat(ChatEntity chat)        {            using (SQLiteCommand command = connection.CreateCommand())            {                command.CommandText = $@"UPDATE                                             CHATS                                         SET                                            NAME     = {PARAMETER_NAME},                                            DATE     = {PARAMETER_DATE},                                            MESSAGES = {PARAMETER_MESSAGES}                                         WHERE                                            ID = {PARAMETER_ID}";                command.Parameters.AddWithValue(PARAMETER_ID, chat.Id);                command.Parameters.AddWithValue(PARAMETER_NAME, chat.Name);                command.Parameters.AddWithValue(PARAMETER_DATE, chat.Date.ToString("yyyy-MM-dd HH:mm:ss"));                command.Parameters.AddWithValue(PARAMETER_MESSAGES, JsonConvert.SerializeObject(chat.Messages));                command.ExecuteNonQuery();            }        }

        /// <summary>
        /// Updates the name of the chat entity in the database based on the provided ID.
        /// </summary>
        /// <param name="chatId">The ID of the chat to be updated.</param>
        /// <param name="newName">The new name for the chat.</param>
        public static void UpdateChatName(string chatId, string newName)
        {
            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText = $"UPDATE CHATS SET NAME = {PARAMETER_NAME} WHERE ID = {PARAMETER_ID}";

                command.Parameters.AddWithValue(PARAMETER_NAME, newName);
                command.Parameters.AddWithValue(PARAMETER_ID, chatId);

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Deletes the chat entity from the database based on the provided ID.
        /// </summary>
        /// <param name="chatId">The ID of the chat to be deleted.</param>
        public static void DeleteChat(string chatId)
        {
            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText = $"DELETE FROM CHATS WHERE ID = {PARAMETER_ID}";

                command.Parameters.AddWithValue(PARAMETER_ID, chatId);

                command.ExecuteNonQuery();
            }
        }

        #endregion Methods
    }}