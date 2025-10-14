using JeffPires.VisualChatGPTStudio.Utils;
using SQLite;
using System;
using System.IO;

namespace VisualChatGPTStudioShared.Utils.Repositories
{
    /// <summary>
    /// Provides methods and functionality for data access and management within the application.
    /// </summary>
    internal static class Repository
    {
        #region Methods

        /// <summary>
        /// Creates and initializes a SQLite database connection. Ensures the database file is located in a specific folder within the local application data directory.
        /// If the folder does not exist, it is created.
        /// </summary>
        /// <returns>
        /// A new instance of SQLiteConnection pointing to the database file.
        /// </returns>
        public static SQLiteConnection CreateDataBaseAndConnection()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Constants.EXTENSION_NAME);
            string filePath = Path.Combine(folder, "VisualChatGptStudio.db");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            return new(filePath);
        }

        #endregion Methods
    }
}
