namespace libFormat;

/// <summary>
/// Specifies the markdown column name used when rendering a property or field.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class MDCollumnNameAttribute(string name) : Attribute {

    /// <summary>
    /// Gets the column name that should be displayed in the markdown table.
    /// </summary>
    public string Name { get; } = name;
}
