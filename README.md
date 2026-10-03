# libFormat

`libFormat` is a small .NET library for formatting sequences of objects as
Markdown tables. It is part of the pmtk project and can be used by command-line
or server components that need human-readable Markdown output.

The library targets .NET 10 and has no external package dependencies.

## Native AOT

The library can be used by a Native AOT application. Set `<PublishAot>true</PublishAot>`
in the consuming application's project and publish for a runtime identifier. The
library itself enables AOT compatibility analysis with `<IsAotCompatible>true</IsAotCompatible>`.

`MarkdownPrinter.Print<T>` preserves the public properties and fields of `T` for
reflection. Pass the concrete row type as `T` when its members should become
columns. The string-based `TextWidthProvider.GetWidestText<T>` overload inspects
each object's runtime type and is unsafe with trimming. In a Native AOT app, pass
a selector instead of a property name. With the `Project` type and `projects`
array from [Basic Usage](#basic-usage):

```csharp
List<Project> projectList = [.. projects];

int nameWidth = TextWidthProvider.GetWidestText(projectList, project => project.Name);     // 9
int statusWidth = TextWidthProvider.GetWidestText(projectList, project => project.Status); // 7
```

The selector can also return `null`. Null list items and null results do not
contribute to the width:

```csharp
List<Project?> maybeProjects = [projects[0], null, projects[1]];
int nameWidth = TextWidthProvider.GetWidestText(maybeProjects, project => project?.Name); // 9
```

## Features

- Renders public instance properties and fields as Markdown columns.
- Supports selecting and ordering the columns to render.
- Supports custom column headings through an attribute.
- Adds an optional Markdown heading above a table.
- Aligns columns for readable plain-text output.
- Escapes table separators and converts line breaks to `<br/>`.

## Build

From the repository root:

```powershell
dotnet build libFormat.slnx
```

To reference the library from another project in the source tree:

```powershell
dotnet add <project> reference libFormat/libFormat.csproj
```

Then import its namespace:

```csharp
using libFormat;
```

## Basic Usage

Given a type and some values:

```csharp
public sealed class Project
{
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int OpenTasks;
}

Project[] projects =
[
    new() { Name = "Website", Status = "Active", OpenTasks = 4 },
    new() { Name = "Migration", Status = "Planned", OpenTasks = 12 }
];

string markdown = MarkdownPrinter.Print(projects);
Console.WriteLine(markdown);
```

Output:

```markdown
| Name      | Status  | OpenTasks |
| --------- | ------- | --------- |
| Website   | Active  | 4         |
| Migration | Planned | 12        |
```

`MarkdownPrinter` calls `ToString()` on each non-null member value. Null member
values are rendered as empty cells.

## Adding a Title

Pass a title to emit a level-three Markdown heading above the table:

```csharp
string markdown = MarkdownPrinter.Print(projects, "Current projects");
```

Output:

```markdown
### Current projects

| Name      | Status  | OpenTasks |
| --------- | ------- | --------- |
| Website   | Active  | 4         |
| Migration | Planned | 12        |
```

A null, empty, or whitespace-only title is omitted. Line breaks in a title are
replaced with spaces.

## Selecting Columns

Use the overload that accepts member names to control which columns are
included and their order:

```csharp
string markdown = MarkdownPrinter.Print(
    projects,
    [nameof(Project.Status), nameof(Project.Name)]);
```

Output:

```markdown
| Status  | Name      |
| ------- | --------- |
| Active  | Website   |
| Planned | Migration |
```

Member matching is case-sensitive. Unknown names are ignored. If no requested
member can be rendered, the result is an empty string.

A title can also be supplied when selecting columns:

```csharp
string markdown = MarkdownPrinter.Print(
    projects,
    [nameof(Project.Name), nameof(Project.OpenTasks)],
    "Project workload");
```

## Custom Column Names

Apply `MDCollumnNameAttribute` to a public property or field to replace its
heading in generated tables:

```csharp
public sealed class Project
{
    [MDCollumnName("Project name")]
    public string Name { get; init; } = string.Empty;

    [MDCollumnName("Open tasks")]
    public int OpenTasks;
}
```

Column selection still uses the C# member name (`Name` or `OpenTasks`), not the
custom heading.

> Note: `MDCollumnNameAttribute` retains the existing public API spelling of
> "Collumn".

## Markdown Escaping

Cell content is transformed to preserve the table structure:

| Input | Text written into the table cell |
| --- | --- |
| <code>one&#124;two</code> | <code>one\&#124;two</code> |
| A value containing a line break | <code>first&lt;br/&gt;second</code> |
| `null` | An empty cell |

Other Markdown syntax in values is preserved.

## Printable Members

The default overload includes:

- Public readable instance properties, excluding indexers.
- Public instance fields.

Properties are emitted before fields. Static and non-public members are not
included. The order within each group follows the order returned by .NET
reflection and should not be treated as a stable presentation contract; use
the column-selection overload when ordering matters.

If the item type has no printable members, `Print` returns an empty string. An
empty sequence still produces the header and separator rows because its item
type defines the columns.

Passing a null sequence or null member-name list throws
`ArgumentNullException`. A null item in a sequence produces a row of empty
cells.

## API Reference

### `MarkdownPrinter`

```csharp
public static string Print<T>(IEnumerable<T> values);
public static string Print<T>(IEnumerable<T> values, string? title);
public static string Print<T>(IEnumerable<T> values, List<string> memberNames);
public static string Print<T>(
    IEnumerable<T> values,
    List<string> memberNames,
    string? title);
```

All overloads return the complete Markdown text without an extra trailing line
break.

### `MDCollumnNameAttribute`

```csharp
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field,
    AllowMultiple = false,
    Inherited = true)]
public sealed class MDCollumnNameAttribute : Attribute
{
    public string Name { get; }
}
```

Sets the displayed column heading for a property or field.

### `TextWidthProvider`

```csharp
public static int GetWidestText(List<string> values);
public static int GetWidestText<T>(List<T>? objects, string propertyName);
public static int GetWidestText<T>(List<T>? objects, Func<T, string?> selector);
```

The first overload returns the length of the longest string, or zero for an
empty list. The generic overload finds a named public string property on each
non-null object and returns the longest value length. It returns zero when the
input is null or empty, the property is absent, or all matching values are
null. The selector overload reads values without reflection and is suitable for
Native AOT. It returns zero for a null or empty list, or when every selected
value is null, and throws `ArgumentNullException` for a null selector.

`MarkdownPrinter` uses `TextWidthProvider` internally to align generated table
cells.
