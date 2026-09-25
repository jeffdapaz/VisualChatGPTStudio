using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace VisualChatGPTStudioShared.Utils.Repositories.Sqlite
{
    /// <summary>
    /// Represents a SQL statement to execute against a <see cref="SqliteConnection"/>.
    /// Supports positional ("?") and named ("@name") parameters.
    /// </summary>
    public sealed class SqliteCommand
    {
        #region Properties

        private readonly SqliteConnection connection;
        private readonly string sql;
        private readonly List<KeyValuePair<string, object>> bindings = [];

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteCommand"/> class.
        /// </summary>
        /// <param name="connection">The connection used to execute the command.</param>
        /// <param name="sql">The SQL statement.</param>
        internal SqliteCommand(SqliteConnection connection, string sql)
        {
            this.connection = connection;
            this.sql = sql;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Binds a value to a named parameter.
        /// </summary>
        /// <param name="name">The parameter name, including its prefix (for example "@ID").</param>
        /// <param name="value">The value to bind.</param>
        public void Bind(string name, object value)
        {
            bindings.Add(new KeyValuePair<string, object>(name, value));
        }

        /// <summary>
        /// Binds a value to the next positional parameter.
        /// </summary>
        /// <param name="value">The value to bind.</param>
        public void Bind(object value)
        {
            bindings.Add(new KeyValuePair<string, object>(null, value));
        }

        /// <summary>
        /// Executes the command without returning rows.
        /// </summary>
        /// <returns>Always 0; kept for compatibility with the previous API.</returns>
        public int ExecuteNonQuery()
        {
            Execute(_ => false);

            return 0;
        }

        /// <summary>
        /// Executes the command and returns the first column of the first row, converted to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The expected value type.</typeparam>
        /// <returns>The converted value, or the default of <typeparamref name="T"/> when there are no rows.</returns>
        public T ExecuteScalar<T>()
        {
            T result = default;

            Execute(stmt =>
            {
                result = (T)ConvertValue(ReadColumn(stmt, 0), typeof(T));

                return false;
            });

            return result;
        }

        /// <summary>
        /// Executes the command and returns the first column of every row, converted to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The expected value type.</typeparam>
        /// <returns>The converted values.</returns>
        public IEnumerable<T> ExecuteQueryScalars<T>()
        {
            List<T> result = [];

            Execute(stmt =>
            {
                result.Add((T)ConvertValue(ReadColumn(stmt, 0), typeof(T)));

                return true;
            });

            return result;
        }

        /// <summary>
        /// Executes the command and maps every row to a new <typeparamref name="T"/> instance,
        /// matching column names to writable property names (case-insensitive).
        /// </summary>
        /// <typeparam name="T">The type to map each row to.</typeparam>
        /// <returns>The mapped objects.</returns>
        public List<T> ExecuteQuery<T>() where T : new()
        {
            List<T> result = [];

            PropertyInfo[] columnProperties = null;

            Execute(stmt =>
            {
                if (columnProperties == null)
                {
                    columnProperties = MapColumns(stmt, typeof(T));
                }

                T item = new();

                for (int i = 0; i < columnProperties.Length; i++)
                {
                    PropertyInfo property = columnProperties[i];

                    if (property != null)
                    {
                        property.SetValue(item, ConvertValue(ReadColumn(stmt, i), property.PropertyType));
                    }
                }

                result.Add(item);

                return true;
            });

            return result;
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Prepares, binds and steps through the statement, invoking the row handler for each returned row.
        /// </summary>
        /// <param name="rowHandler">Handles a row; returns true to continue reading rows or false to stop.</param>
        private void Execute(Func<IntPtr, bool> rowHandler)
        {
            IntPtr db = connection.Handle;

            if (db == IntPtr.Zero)
            {
                throw new ObjectDisposedException(nameof(SqliteConnection));
            }

            byte[] sqlBytes = SqliteNative.ToUtf8(sql);

            int result = SqliteNative.sqlite3_prepare_v2(db, sqlBytes, sqlBytes.Length, out IntPtr stmt, IntPtr.Zero);

            if (result != SqliteNative.SQLITE_OK)
            {
                throw new SqliteException(result, SqliteNative.GetErrorMessage(db));
            }

            try
            {
                BindParameters(stmt);

                while (true)
                {
                    result = SqliteNative.sqlite3_step(stmt);

                    if (result == SqliteNative.SQLITE_ROW)
                    {
                        if (!rowHandler(stmt))
                        {
                            break;
                        }
                    }
                    else if (result == SqliteNative.SQLITE_DONE)
                    {
                        break;
                    }
                    else
                    {
                        throw new SqliteException(result, SqliteNative.GetErrorMessage(db));
                    }
                }
            }
            finally
            {
                SqliteNative.sqlite3_finalize(stmt);
            }
        }

        /// <summary>
        /// Binds all registered parameter values to the prepared statement.
        /// </summary>
        /// <param name="stmt">The prepared statement handle.</param>
        private void BindParameters(IntPtr stmt)
        {
            int nextPositionalIndex = 1;

            foreach (KeyValuePair<string, object> binding in bindings)
            {
                int index = binding.Key == null ? nextPositionalIndex++ : SqliteNative.sqlite3_bind_parameter_index(stmt, SqliteNative.ToUtf8(binding.Key));

                if (index <= 0)
                {
                    continue;
                }

                int result = BindValue(stmt, index, binding.Value);

                if (result != SqliteNative.SQLITE_OK)
                {
                    throw new SqliteException(result, SqliteNative.GetErrorMessage(connection.Handle));
                }
            }
        }

        /// <summary>
        /// Binds a single .NET value to the parameter at the specified index.
        /// </summary>
        /// <param name="stmt">The prepared statement handle.</param>
        /// <param name="index">The 1-based parameter index.</param>
        /// <param name="value">The value to bind.</param>
        /// <returns>The SQLite result code.</returns>
        private static int BindValue(IntPtr stmt, int index, object value)
        {
            switch (value)
            {
                case null:
                case DBNull:
                    return SqliteNative.sqlite3_bind_null(stmt, index);
                case string text:
                    return BindText(stmt, index, text);
                case bool boolean:
                    return SqliteNative.sqlite3_bind_int64(stmt, index, boolean ? 1 : 0);
                case byte[] blob:
                    return SqliteNative.sqlite3_bind_blob(stmt, index, blob, blob.Length, SqliteNative.SQLITE_TRANSIENT);
                case float or double or decimal:
                    return SqliteNative.sqlite3_bind_double(stmt, index, Convert.ToDouble(value, CultureInfo.InvariantCulture));
                case Enum:
                    return SqliteNative.sqlite3_bind_int64(stmt, index, Convert.ToInt64(value, CultureInfo.InvariantCulture));
                case byte or sbyte or short or ushort or int or uint or long or ulong:
                    return SqliteNative.sqlite3_bind_int64(stmt, index, Convert.ToInt64(value, CultureInfo.InvariantCulture));
                case DateTime dateTime:
                    return BindText(stmt, index, dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                default:
                    return BindText(stmt, index, Convert.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Binds a text value to the parameter at the specified index.
        /// </summary>
        /// <param name="stmt">The prepared statement handle.</param>
        /// <param name="index">The 1-based parameter index.</param>
        /// <param name="text">The text to bind.</param>
        /// <returns>The SQLite result code.</returns>
        private static int BindText(IntPtr stmt, int index, string text)
        {
            byte[] bytes = SqliteNative.ToUtf8(text);

            return SqliteNative.sqlite3_bind_text(stmt, index, bytes, bytes.Length - 1, SqliteNative.SQLITE_TRANSIENT);
        }

        /// <summary>
        /// Reads the value of a result column as a .NET object (long, double, string, byte[] or null).
        /// </summary>
        /// <param name="stmt">The statement handle positioned on a row.</param>
        /// <param name="index">The 0-based column index.</param>
        /// <returns>The column value.</returns>
        private static object ReadColumn(IntPtr stmt, int index)
        {
            switch (SqliteNative.sqlite3_column_type(stmt, index))
            {
                case SqliteNative.SQLITE_INTEGER:
                    return SqliteNative.sqlite3_column_int64(stmt, index);
                case SqliteNative.SQLITE_FLOAT:
                    return SqliteNative.sqlite3_column_double(stmt, index);
                case SqliteNative.SQLITE_TEXT:
                    return SqliteNative.FromUtf8(SqliteNative.sqlite3_column_text(stmt, index));
                case SqliteNative.SQLITE_BLOB:
                    IntPtr blobPointer = SqliteNative.sqlite3_column_blob(stmt, index);
                    byte[] blob = new byte[SqliteNative.sqlite3_column_bytes(stmt, index)];
                    if (blob.Length > 0)
                    {
                        Marshal.Copy(blobPointer, blob, 0, blob.Length);
                    }
                    return blob;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Maps each result column to the writable property of the target type with the same name (case-insensitive).
        /// </summary>
        /// <param name="stmt">The statement handle.</param>
        /// <param name="targetType">The type to map to.</param>
        /// <returns>An array with the property for each column, or null where no property matches.</returns>
        private static PropertyInfo[] MapColumns(IntPtr stmt, Type targetType)
        {
            int columnCount = SqliteNative.sqlite3_column_count(stmt);

            PropertyInfo[] result = new PropertyInfo[columnCount];

            for (int i = 0; i < columnCount; i++)
            {
                string columnName = SqliteNative.FromUtf8(SqliteNative.sqlite3_column_name(stmt, i));

                PropertyInfo property = targetType.GetProperty(columnName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                result[i] = property != null && property.CanWrite ? property : null;
            }

            return result;
        }

        /// <summary>
        /// Converts a raw SQLite value to the specified .NET type.
        /// </summary>
        /// <param name="value">The raw value (long, double, string, byte[] or null).</param>
        /// <param name="targetType">The target type.</param>
        /// <returns>The converted value.</returns>
        private static object ConvertValue(object value, Type targetType)
        {
            Type underlyingType = Nullable.GetUnderlyingType(targetType);

            if (value == null)
            {
                return targetType.IsValueType && underlyingType == null ? Activator.CreateInstance(targetType) : null;
            }

            Type type = underlyingType ?? targetType;

            if (type == typeof(object) || type.IsInstanceOfType(value))
            {
                return value;
            }

            if (type == typeof(string))
            {
                return value is byte[] bytes ? Convert.ToBase64String(bytes) : Convert.ToString(value, CultureInfo.InvariantCulture);
            }

            if (type == typeof(bool))
            {
                return value is string boolText ? boolText == "1" || bool.TryParse(boolText, out bool parsed) && parsed : Convert.ToInt64(value, CultureInfo.InvariantCulture) != 0;
            }

            if (type == typeof(DateTime))
            {
                return value is string dateText ? DateTime.Parse(dateText, CultureInfo.InvariantCulture) : new DateTime(Convert.ToInt64(value, CultureInfo.InvariantCulture));
            }

            if (type == typeof(Guid))
            {
                return Guid.Parse(Convert.ToString(value, CultureInfo.InvariantCulture));
            }

            if (type.IsEnum)
            {
                return value is string enumText ? Enum.Parse(type, enumText, true) : Enum.ToObject(type, Convert.ToInt64(value, CultureInfo.InvariantCulture));
            }

            return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        #endregion Private Methods
    }
}
