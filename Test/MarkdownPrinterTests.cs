namespace libFormat.Tests;

public class MarkdownPrinterTests
{
    [Fact]
    public void Print_RendersPropertiesAndFieldsAsMarkdownTable()
    {
        Project[] projects =
        [
            new() { Name = "Website", Status = "Active", OpenTasks = 4 },
            new() { Name = "Migration", Status = "Planned", OpenTasks = 12 }
        ];

        string markdown = MarkdownPrinter.Print(projects);
        string expected = """
            | Name      | Status  | OpenTasks |
            | --------- | ------- | --------- |
            | Website   | Active  | 4         |
            | Migration | Planned | 12        |
            """;

        Assert.Equal(expected, markdown);
    }

    [Fact]
    public void Print_WithSelectedMembers_RendersRequestedColumnsInOrder()
    {
        Project[] projects =
        [
            new() { Name = "Website", Status = "Active", OpenTasks = 4 },
            new() { Name = "Migration", Status = "Planned", OpenTasks = 12 }
        ];

        string markdown = MarkdownPrinter.Print(projects, [nameof(Project.Status), nameof(Project.Name)]);
        string expected = """
            | Status  | Name      |
            | ------- | --------- |
            | Active  | Website   |
            | Planned | Migration |
            """;

        Assert.Equal(expected, markdown);
    }

    [Fact]
    public void Print_WithTitleAndCustomColumnNames_RendersHeadingAndCustomHeaders()
    {
        WorkItem[] items =
        [
            new() { Title = "Fix login", Owner = "Ada" }
        ];

        string markdown = MarkdownPrinter.Print(items, "Sprint work");
        string expected = """
            ### Sprint work

            | Work title | Assigned to |
            | ---------- | ----------- |
            | Fix login  | Ada         |
            """;

        Assert.Equal(expected, markdown);
    }

    [Fact]
    public void Print_EscapesPipesAndLineBreaks()
    {
        Note[] notes =
        [
            new() { Text = "alpha|beta\nnext" }
        ];

        string markdown = MarkdownPrinter.Print(notes);
        string expected = """
            | Text                 |
            | -------------------- |
            | alpha\|beta<br/>next |
            """;

        Assert.Equal(expected, markdown);
    }

    [Fact]
    public void Print_NullValuesAndNullItems_RenderEmptyCells()
    {
        Note?[] notes =
        [
            new() { Text = null },
            null
        ];

        string markdown = MarkdownPrinter.Print(notes);
        string expected = """
            | Text |
            | ---- |
            |      |
            |      |
            """;

        Assert.Equal(expected, markdown);
    }

    [Fact]
    public void Print_NoMatchingSelectedMembers_ReturnsEmptyString()
    {
        Project[] projects =
        [
            new() { Name = "Website", Status = "Active", OpenTasks = 4 }
        ];

        string markdown = MarkdownPrinter.Print(projects, ["Missing"]);

        Assert.Equal(string.Empty, markdown);
    }

    [Fact]
    public void GetWidestText_ReturnsWidestStringLength()
    {
        Assert.Equal(9, TextWidthProvider.GetWidestText(["one", "three", "migration"]));
    }

    [Fact]
    public void GetWidestText_ForProperty_ReturnsWidestStringPropertyLength()
    {
        List<Project> projects =
        [
            new() { Name = "Web", Status = "Active", OpenTasks = 4 },
            new() { Name = "Migration", Status = "Planned", OpenTasks = 12 }
        ];

        Assert.Equal(9, TextWidthProvider.GetWidestText(projects, nameof(Project.Name)));
    }

    private sealed class Project
    {
        public string Name { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public int OpenTasks;
    }

    private sealed class WorkItem
    {
        [MDCollumnName("Work title")]
        public string Title { get; init; } = string.Empty;

        [MDCollumnName("Assigned to")]
        public string Owner = string.Empty;
    }

    private sealed class Note
    {
        public string? Text { get; init; }
    }
}
