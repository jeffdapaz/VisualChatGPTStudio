﻿using Newtonsoft.Json;
using System;
    /// <summary>
    /// Repository class for managing the Turbo Chats.
    /// </summary>
    public static class ChatRepository
        #region Constantes
        private const string PARAMETER_ID = "@PARAMETER_ID";

        #endregion Constantes
        #region Properties
        private static SQLiteConnection connection;

        #endregion Properties
        #region Methods

        /// <summary>
        /// Creates a database file for VisualChatGptStudio if it does not already exist and opens a connection to it.
        /// </summary>
        public static void CreateDataBase()
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Constants.EXTENSION_NAME);

                string filePath = Path.Combine(folder, "VisualChatGptStudio.db");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                connection = new(filePath);

                CreateTableChats();
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

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Retrieves a list of ChatEntity objects from the database.
        /// </summary>
        /// <returns>
        /// A list of ChatEntity objects containing the ID, Name, and Date properties.
        /// </returns>
        public static List<ChatEntity> GetChats()

            return command.ExecuteQuery<ChatEntity>();

        /// <summary>
        /// Retrieves a list of messages from the database for a given chat ID.
        /// </summary>
        /// <param name="chatId">The ID of the chat.</param>
        /// <returns>A list of MessageEntity objects representing the messages.</returns>
        public static List<MessageEntity> GetMessages(string chatId)

        /// <summary>
        /// Adds a chat entity to the database.
        /// </summary>
        /// <param name="chat">The chat entity to add.</param>
        public static void AddChat(ChatEntity chat)

        /// <summary>
        /// Updates the chat entity in the database with the provided information.
        /// </summary>
        /// <param name="chat">The chat entity to be updated.</param>
        public static void UpdateChat(ChatEntity chat)

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

        #endregion Methods
    }