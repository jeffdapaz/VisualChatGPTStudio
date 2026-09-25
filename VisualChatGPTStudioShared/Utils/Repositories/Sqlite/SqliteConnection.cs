using System;

namespace VisualChatGPTStudioShared.Utils.Repositories.Sqlite
{
    /// <summary>
    /// Represents a connection to a SQLite database file using the extension's private native SQLite library.
    /// </summary>
    public sealed class SqliteConnection : IDisposable
    {
        #region Constants

        private const int BUSY_TIMEOUT_MILLISECONDS = 5000;

        #endregion Constants

        #region Properties

        /// <summary>
        /// Gets the native database handle.
        /// </summary>
        internal IntPtr Handle { get; private set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Opens (or creates) the SQLite database file at the specified path.
        /// </summary>
        /// <param name="databasePath">The full path of the database file.</param>
        public SqliteConnection(string databasePath)
        {
            int flags = SqliteNative.SQLITE_OPEN_READWRITE | SqliteNative.SQLITE_OPEN_CREATE | SqliteNative.SQLITE_OPEN_FULLMUTEX;

            int result = SqliteNative.sqlite3_open_v2(SqliteNative.ToUtf8(databasePath), out IntPtr handle, flags, IntPtr.Zero);

            if (result != SqliteNative.SQLITE_OK)
            {
                string message = handle != IntPtr.Zero ? SqliteNative.GetErrorMessage(handle) : $"SQLite error code {result}";

                if (handle != IntPtr.Zero)
                {
                    SqliteNative.sqlite3_close_v2(handle);
                }

                throw new SqliteException(result, $"Unable to open the database '{databasePath}': {message}");
            }

            Handle = handle;

            SqliteNative.sqlite3_busy_timeout(Handle, BUSY_TIMEOUT_MILLISECONDS);
        }

        /// <summary>
        /// Releases the native database handle if the connection was not disposed.
        /// </summary>
        ~SqliteConnection()
        {
            Close();
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Creates a command for the specified SQL, binding the optional arguments to the positional ("?") parameters in order.
        /// </summary>
        /// <param name="sql">The SQL statement.</param>
        /// <param name="args">The positional parameter values.</param>
        /// <returns>A new <see cref="SqliteCommand"/>.</returns>
        public SqliteCommand CreateCommand(string sql, params object[] args)
        {
            SqliteCommand command = new(this, sql);

            if (args != null)
            {
                foreach (object arg in args)
                {
                    command.Bind(arg);
                }
            }

            return command;
        }

        /// <summary>
        /// Closes the connection and releases the native database handle.
        /// </summary>
        public void Dispose()
        {
            Close();

            GC.SuppressFinalize(this);
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Closes the native database handle if it is open.
        /// </summary>
        private void Close()
        {
            if (Handle != IntPtr.Zero)
            {
                SqliteNative.sqlite3_close_v2(Handle);

                Handle = IntPtr.Zero;
            }
        }

        #endregion Private Methods
    }

    /// <summary>
    /// Represents an error returned by the native SQLite engine.
    /// </summary>
    public sealed class SqliteException : Exception
    {
        #region Properties

        /// <summary>
        /// Gets the SQLite result code.
        /// </summary>
        public int ResultCode { get; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteException"/> class.
        /// </summary>
        /// <param name="resultCode">The SQLite result code.</param>
        /// <param name="message">The error message.</param>
        public SqliteException(int resultCode, string message) : base(message)
        {
            ResultCode = resultCode;
        }

        #endregion Constructors
    }
}
