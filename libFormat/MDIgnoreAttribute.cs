namespace libFormat;

/// <summary>
/// Excludes a property or field from markdown table output.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class MDIgnoreAttribute : Attribute;
