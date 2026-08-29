using Markdig;

namespace RedAnts.Infrastructure.Shared;

public static class MailMarkdown
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseSoftlineBreakAsHardlineBreak()
        .DisableHtml()
        .Build();

    public static string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return "";
        var html = Markdown.ToHtml(markdown, Pipeline);
        return html.Replace("\r\n", "\n").Replace("\n", "").Trim();
    }
}
