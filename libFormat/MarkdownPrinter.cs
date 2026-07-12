using System.Globalization;
using System.Reflection;

namespace libFormat;

/// <summary>
/// Builds markdown tables from sequences of objects.
/// </summary>
public static class MarkdownPrinter {

    /// <summary>
    ///     Builds a markdown table containing all public readable properties and
    ///     public fields from the specified values. Column headers use <see cref="MDCollumnNameAttribute"/>
    ///     when it is applied to a member.
    /// </summary>
    /// <typeparam name="T">The item type to render.</typeparam>
    /// <param name="values">The sequence of values that will be rendered as table rows.</param>
    /// <returns>A markdown table string, or an empty string when the type exposes no printable members.</returns>
    public static string Print<T>(IEnumerable<T> values) {
        return Print(values, title: null);
    }

    /// <summary>
    /// Builds a markdown table containing all public readable properties and public fields from the specified values.
    /// Column headers use <see cref="MDCollumnNameAttribute"/> when it is applied to a member.
    /// When a title is provided it is emitted as a markdown heading immediately above the table.
    /// </summary>
    /// <typeparam name="T">The item type to render.</typeparam>
    /// <param name="values">The sequence of values that will be rendered as table rows.</param>
    /// <param name="title">The title displayed above the generated table.</param>
    /// <returns>A markdown table string, or an empty string when the type exposes no printable members.</returns>
    public static string Print<T>(IEnumerable<T> values, string? title, MarkdownFormatOptions? formatOptions = null) {
        ArgumentNullException.ThrowIfNull(values);

        List<IMemberAccessor> members = GetAllMembers(typeof(T));
        return BuildTable(values, members, title, formatOptions);
    }

    /// <summary>
    /// Builds a markdown table containing only the selected public readable properties and public fields from the specified values.
    /// Column headers use <see cref="MDCollumnNameAttribute"/> when it is applied to a member.
    /// </summary>
    /// <typeparam name="T">The item type to render.</typeparam>
    /// <param name="values">The sequence of values that will be rendered as table rows.</param>
    /// <param name="memberNames">The member names to include as table columns, in the order they should appear.</param>
    /// <returns>A markdown table string, or an empty string when none of the requested members can be printed.</returns>
    public static string Print<T>(IEnumerable<T> values, List<string> memberNames) {
        return Print(values, memberNames, title: null);
    }

    /// <summary>
    /// Builds a markdown table containing only the selected public readable properties and public fields from the specified values.
    /// Column headers use <see cref="MDCollumnNameAttribute"/> when it is applied to a member.
    /// When a title is provided it is emitted as a markdown heading immediately above the table.
    /// </summary>
    /// <typeparam name="T">The item type to render.</typeparam>
    /// <param name="values">The sequence of values that will be rendered as table rows.</param>
    /// <param name="memberNames">The member names to include as table columns, in the order they should appear.</param>
    /// <param name="title">The title displayed above the generated table.</param>
    /// <returns>A markdown table string, or an empty string when none of the requested members can be printed.</returns>
    public static string Print<T>(IEnumerable<T> values, List<string> memberNames, string? title) {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(memberNames);

        List<IMemberAccessor> members = GetSelectedMembers(typeof(T), memberNames);
        return BuildTable(values, members, title, formatOptions: null);
    }

    /// <summary>
    /// Creates the markdown table content for the provided values and member accessors.
    /// </summary>
    /// <typeparam name="T">The item type to render.</typeparam>
    /// <param name="values">The sequence of values that will be rendered as rows.</param>
    /// <param name="members">The members that define the table columns.</param>
    /// <param name="title">The optional title displayed above the generated table.</param>
    /// <returns>A markdown table string, or an empty string when there are no columns to render.</returns>
    private static string BuildTable<T>(IEnumerable<T> values, List<IMemberAccessor> members, string? title, MarkdownFormatOptions? formatOptions) {
        if (members.Count == 0) {
            return string.Empty;
        }

        if(formatOptions is null) {
            formatOptions = new MarkdownFormatOptions() {
                DateFormatString = "dd-MMM-yyyy"
            };
        }

        List<T> materializedValues = [.. values];
        List<string> lines = [];
        List<int> columnWidths = GetColumnWidths(materializedValues, members, formatOptions);

        if (!string.IsNullOrWhiteSpace(title)) {
            lines.Add($"### {EscapeTitle(title)}");
            lines.Add(string.Empty);
        }

        lines.Add(BuildRow(members.Select((member, index) => Pad(Escape(member.ColumnName), columnWidths[index]))));
        lines.Add(BuildRow(columnWidths.Select(width => new string('-', Math.Max(width, 3)))));

        foreach (T value in materializedValues) {
            if (value is null) {
                lines.Add(BuildRow(columnWidths.Select(width => Pad(string.Empty, width))));
                continue;
            }

            List<string> row = [];
            for (int index = 0; index < members.Count; index++) {
                IMemberAccessor member = members[index];
                object? memberValue = member.GetValue(value);
                row.Add(Pad(FormatCellValue(member.GetValue(value), formatOptions), columnWidths[index]));
            }
            lines.Add(BuildRow(row));
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Calculates the display width for each column using the header text and all rendered cell values.
    /// </summary>
    /// <typeparam name="T">The item type to inspect.</typeparam>
    /// <param name="values">The materialized values that will be rendered as rows.</param>
    /// <param name="members">The members that define the table columns.</param>
    /// <returns>A list containing the width of each column.</returns>
    private static List<int> GetColumnWidths<T>(List<T> values, List<IMemberAccessor> members, MarkdownFormatOptions formatOptions) {
        List<int> widths = members
            .Select(member => member.ColumnName.Length)
            .ToList();

        foreach (T value in values) {
            if (value is null) {
                continue;
            }

            for (int index = 0; index < members.Count; index++) {
                string text = FormatCellValue(members[index].GetValue(value), formatOptions);
                int width = TextWidthProvider.GetWidestText([text]);
                if (width > widths[index]) {
                    widths[index] = width;
                }
            }
        }

        return widths;
    }

    /// <summary>
    /// Formats a member value as it will appear in a markdown table cell.
    /// </summary>
    /// <param name="value">The raw member value.</param>
    /// <param name="formatOptions">The formatting options used for special value types.</param>
    /// <returns>The formatted cell text.</returns>
    private static string FormatCellValue(object? value, MarkdownFormatOptions formatOptions) {
        if (value is null) {
            return string.Empty;
        }

        if (value is DateTime dateTime) {
            return dateTime.ToString(formatOptions.DateFormatString, CultureInfo.InvariantCulture);
        }

        return Escape(value.ToString() ?? string.Empty);
    }

    /// <summary>
    /// Wraps a sequence of cell values into a markdown table row.
    /// </summary>
    /// <param name="cells">The already formatted cell values.</param>
    /// <returns>A single markdown table row.</returns>
    private static string BuildRow(IEnumerable<string> cells) {
        return $"| {string.Join(" | ", cells)} |";
    }

    /// <summary>
    /// Pads the specified text to the requested width so columns align in plain text output.
    /// </summary>
    /// <param name="value">The text to pad.</param>
    /// <param name="width">The target width of the padded text.</param>
    /// <returns>The padded text.</returns>
    private static string Pad(string value, int width) {
        return value.PadRight(width);
    }

    /// <summary>
    /// Retrieves all public readable properties and public fields that can be printed for the specified type.
    /// </summary>
    /// <param name="type">The type whose printable members will be collected.</param>
    /// <returns>A list of member accessors in the order they were discovered.</returns>
    private static List<IMemberAccessor> GetAllMembers(Type type) {
        List<IMemberAccessor> members = [];

        members.AddRange(
            type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
                .Select(property => new PropertyAccessor(property)));

        members.AddRange(
            type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Select(field => new FieldAccessor(field)));

        return members;
    }

    /// <summary>
    /// Retrieves only the requested printable members from the specified type.
    /// </summary>
    /// <param name="type">The type whose printable members will be searched.</param>
    /// <param name="memberNames">The member names to include, in the order they should be returned.</param>
    /// <returns>A list of member accessors matching the requested names.</returns>
    private static List<IMemberAccessor> GetSelectedMembers(Type type, IEnumerable<string> memberNames) {
        Dictionary<string, IMemberAccessor> availableMembers = GetAllMembers(type)
            .ToDictionary(member => member.Name, StringComparer.Ordinal);

        List<IMemberAccessor> selectedMembers = [];
        foreach (string memberName in memberNames) {
            if (availableMembers.TryGetValue(memberName, out IMemberAccessor? member)) {
                selectedMembers.Add(member);
            }
        }

        return selectedMembers;
    }

    /// <summary>
    /// Escapes markdown-sensitive characters and replaces line breaks so cell content remains valid inside a markdown table.
    /// </summary>
    /// <param name="value">The raw cell value.</param>
    /// <returns>The escaped value.</returns>
    private static string Escape(string value) {
        return value
            .Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r\n", "<br/>", StringComparison.Ordinal)
            .Replace("\n", "<br/>", StringComparison.Ordinal)
            .Replace("\r", "<br/>", StringComparison.Ordinal);
    }

    /// <summary>
    /// Escapes line breaks in table titles so they remain valid markdown headings.
    /// </summary>
    /// <param name="value">The raw title value.</param>
    /// <returns>The escaped title.</returns>
    private static string EscapeTitle(string value) {
        return value
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal);
    }

    /// <summary>
    /// Defines the minimal metadata and value access required to render a type
    /// member in a markdown table.
    /// </summary>
    private interface IMemberAccessor {
        /// <summary>
        /// Gets the actual reflected member name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the column header that should be displayed for the member.
        /// </summary>
        string ColumnName { get; }

        /// <summary>
        /// Retrieves the member value from the specified object instance.
        /// </summary>
        /// <param name="instance">The object instance that owns the member.</param>
        /// <returns>The current member value.</returns>
        object? GetValue(object instance);
    }

    /// <summary>
    /// Provides markdown rendering metadata and value access for a reflected property.
    /// </summary>
    private sealed class PropertyAccessor(PropertyInfo propertyInfo) : IMemberAccessor {
        /// <summary>
        /// Gets the actual reflected property name.
        /// </summary>
        public string Name => propertyInfo.Name;

        /// <summary>
        /// Gets the markdown column header for the property.
        /// </summary>
        public string ColumnName => propertyInfo.GetCustomAttribute<MDCollumnNameAttribute>()?.Name ?? propertyInfo.Name;

        /// <summary>
        /// Retrieves the property value from the specified object instance.
        /// </summary>
        /// <param name="instance">The object instance that owns the property.</param>
        /// <returns>The current property value.</returns>
        public object? GetValue(object instance) {
            return propertyInfo.GetValue(instance);
        }
    }

    /// <summary>
    /// Provides markdown rendering metadata and value access for a reflected field.
    /// </summary>
    private sealed class FieldAccessor(FieldInfo fieldInfo) : IMemberAccessor {
        /// <summary>
        /// Gets the actual reflected field name.
        /// </summary>
        public string Name => fieldInfo.Name;

        /// <summary>
        /// Gets the markdown column header for the field.
        /// </summary>
        public string ColumnName => fieldInfo.GetCustomAttribute<MDCollumnNameAttribute>()?.Name ?? fieldInfo.Name;

        /// <summary>
        /// Retrieves the field value from the specified object instance.
        /// </summary>
        /// <param name="instance">The object instance that owns the field.</param>
        /// <returns>The current field value.</returns>
        public object? GetValue(object instance) {
            return fieldInfo.GetValue(instance);
        }
    }
}
