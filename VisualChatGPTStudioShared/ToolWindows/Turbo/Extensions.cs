using System.Data;
using System.Linq;
using System.Text;

namespace VisualChatGPTStudioShared.ToolWindows.Turbo;

public static class Extensions
{
    public static string ToMarkdown(this DataTable dataTable)
    {
        var rows = dataTable.Rows.OfType<DataRow>()
            .Select(row => dataTable.Columns.OfType<DataColumn>()
                .ToDictionary(col => col.ColumnName, col => row[col]?.ToString() ?? string.Empty))
            .ToList();

        var sb = new StringBuilder();

        if (rows.Any())
        {
            // --- header ---
            var headers = rows.First().Keys.ToList();
            sb.Append("| ");
            foreach (var h in headers) sb.Append(h).Append(" |");
            sb.AppendLine();

            // --- divider ---
            sb.Append("|");
            foreach (var h in headers) sb.Append("------|");
            sb.AppendLine();

            // --- data ---
            foreach (var row in rows)
            {
                sb.Append("| ");
                foreach (var h in headers)
                {
                    sb.Append(EscapeMd(row[h])).Append(" |");
                }
                sb.AppendLine();
            }
        }

        string EscapeMd(string text)
        {
            if (text == null) return string.Empty;
            return text.Replace("|", @"\|")
                .Replace("\r\n", " ")
                .Replace("\n", " ");
        }

        return sb.ToString();
    }
}
