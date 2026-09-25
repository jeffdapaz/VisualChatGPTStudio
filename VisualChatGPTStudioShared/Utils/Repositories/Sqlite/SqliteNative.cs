using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace VisualChatGPTStudioShared.Utils.Repositories.Sqlite
{
    /// <summary>
    /// Provides direct P/Invoke access to a private copy of the native SQLite engine.
    /// The native library is shipped with a unique file name and loaded from the extension folder,
    /// so it is never affected by the SQLite assemblies or binding redirects used by the Visual Studio host.
    /// </summary>
    internal static class SqliteNative
    {
        #region Constants

        /// <summary>
        /// The unique name of the native SQLite library shipped with the extension.
        /// </summary>
        public const string LIBRARY_NAME = "VisualChatGPTStudio_sqlite3";

        public const int SQLITE_OK = 0;
        public const int SQLITE_ROW = 100;
        public const int SQLITE_DONE = 101;

        public const int SQLITE_INTEGER = 1;
        public const int SQLITE_FLOAT = 2;
        public const int SQLITE_TEXT = 3;
        public const int SQLITE_BLOB = 4;
        public const int SQLITE_NULL = 5;

        public const int SQLITE_OPEN_READWRITE = 0x00000002;
        public const int SQLITE_OPEN_CREATE = 0x00000004;
        public const int SQLITE_OPEN_FULLMUTEX = 0x00010000;

        /// <summary>
        /// Tells SQLite to make its own copy of bound data (SQLITE_TRANSIENT).
        /// </summary>
        public static readonly IntPtr SQLITE_TRANSIENT = new(-1);

        #endregion Constants

        #region Constructors

        /// <summary>
        /// Loads the native SQLite library for the current process architecture from the extension folder.
        /// Runs before any native method of this class is called.
        /// </summary>
        static SqliteNative()
        {
            string architecture = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => "win-x86",
                Architecture.Arm64 => "win-arm64",
                _ => "win-x64"
            };

            string libraryPath = Path.Combine(Path.GetDirectoryName(typeof(SqliteNative).Assembly.Location), "runtimes", architecture, "native", LIBRARY_NAME + ".dll");

            if (LoadLibrary(libraryPath) == IntPtr.Zero)
            {
                throw new DllNotFoundException($"Unable to load the native SQLite library '{libraryPath}': {new Win32Exception(Marshal.GetLastWin32Error()).Message}");
            }
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Converts a .NET string to a null-terminated UTF-8 byte array.
        /// </summary>
        /// <param name="value">The string to convert.</param>
        /// <returns>The null-terminated UTF-8 bytes.</returns>
        public static byte[] ToUtf8(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);

            byte[] result = new byte[bytes.Length + 1];

            Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);

            return result;
        }

        /// <summary>
        /// Converts a pointer to a null-terminated UTF-8 string into a .NET string.
        /// </summary>
        /// <param name="pointer">The pointer to the UTF-8 string.</param>
        /// <returns>The converted string, or null when the pointer is zero.</returns>
        public static string FromUtf8(IntPtr pointer)
        {
            if (pointer == IntPtr.Zero)
            {
                return null;
            }

            int length = 0;

            while (Marshal.ReadByte(pointer, length) != 0)
            {
                length++;
            }

            byte[] bytes = new byte[length];

            Marshal.Copy(pointer, bytes, 0, length);

            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Gets the last error message of the specified database connection.
        /// </summary>
        /// <param name="db">The native database handle.</param>
        /// <returns>The error message.</returns>
        public static string GetErrorMessage(IntPtr db)
        {
            return FromUtf8(sqlite3_errmsg(db));
        }

        #endregion Public Methods

        #region Native Methods

        /// <summary>
        /// Opens a SQLite database file.
        /// </summary>
        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_open_v2(byte[] filename, out IntPtr db, int flags, IntPtr vfs);

        /// <summary>
        /// Closes a SQLite database connection.
        /// </summary>
        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_close_v2(IntPtr db);

        /// <summary>
        /// Sets a busy handler that sleeps when a table is locked.
        /// </summary>
        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_busy_timeout(IntPtr db, int milliseconds);

        /// <summary>
        /// Gets the English-language text that describes the most recent error.
        /// </summary>
        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr sqlite3_errmsg(IntPtr db);

        /// <summary>
        /// Compiles a SQL statement.
        /// </summary>
        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_prepare_v2(IntPtr db, byte[] sql, int numBytes, out IntPtr stmt, IntPtr tail);

        /// <summary>
        /// Evaluates a prepared statement.
        /// </summary>
        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_step(IntPtr stmt);

        /// <summary>
        /// Destroys a prepared statement.
        /// </summary>
        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_finalize(IntPtr stmt);

        /// <summary>
        /// Gets the index of a named parameter.
        /// </summary>
        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_bind_parameter_index(IntPtr stmt, byte[] name);

        /// <summary>
        /// Binds a NULL value to a parameter.
        /// </summary>
        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_bind_null(IntPtr stmt, int index);

        /// <summary>
        /// Binds a 64-bit integer value to a parameter.
        /// </summary>
        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_bind_int64(IntPtr stmt, int index, long value);

        /// <summary>
        /// Binds a double value to a parameter.
        /// </summary>
        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_bind_double(IntPtr stmt, int index, double value);

        /// <summary>
        /// Binds a UTF-8 text value to a parameter.
        /// </summary>
        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_bind_text(IntPtr stmt, int index, byte[] value, int numBytes, IntPtr destructor);

        /// <summary>
        /// Binds a BLOB value to a parameter.
        /// </summary>
        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_bind_blob(IntPtr stmt, int index, byte[] value, int numBytes, IntPtr destructor);

        /// <summary>
        /// Gets the number of columns in the result set.
        /// </summary>
        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_column_count(IntPtr stmt);

        /// <summary>
        /// Gets the name of a result column.
        /// </summary>
        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr sqlite3_column_name(IntPtr stmt, int index);

        /// <summary>
        /// Gets the data type of a result column value.
        /// </summary>
        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_column_type(IntPtr stmt, int index);

        /// <summary>
        /// Gets a result column value as a 64-bit integer.
        /// </summary>
        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern long sqlite3_column_int64(IntPtr stmt, int index);

        /// <summary>
        /// Gets a result column value as a double.
        /// </summary>
        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern double sqlite3_column_double(IntPtr stmt, int index);

        /// <summary>
        /// Gets a result column value as UTF-8 text.
        /// </summary>
        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr sqlite3_column_text(IntPtr stmt, int index);

        /// <summary>
        /// Gets a result column value as a BLOB.
        /// </summary>
        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr sqlite3_column_blob(IntPtr stmt, int index);

        /// <summary>
        /// Gets the size in bytes of a result column value.
        /// </summary>
        [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_column_bytes(IntPtr stmt, int index);

        #endregion Native Methods

        #region Private Methods

        /// <summary>
        /// Loads the specified native library into the current process.
        /// </summary>
        /// <param name="lpFileName">The full path of the native library.</param>
        /// <returns>The handle of the loaded module.</returns>
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        #endregion Private Methods
    }
}
