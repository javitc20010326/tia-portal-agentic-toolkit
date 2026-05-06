using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace TiaPortalAgenticToolkit.Openness;

public sealed record ExportFolderSummary(
    string FolderPath,
    int TotalFiles,
    IReadOnlyList<FileSummary> Files,
    IReadOnlyDictionary<string, int> ExtensionCounts,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> SuggestedNextSteps);

public sealed record FileSummary(
    string Path,
    string Extension,
    long Bytes,
    string Kind,
    string? Name,
    string? Detail);

public sealed record XmlArtifactSummary(
    string FilePath,
    string RootName,
    string? ArtifactName,
    IReadOnlyDictionary<string, int> ElementCounts,
    IReadOnlyDictionary<string, string> InterestingAttributes,
    IReadOnlyList<string> CandidateTexts,
    IReadOnlyList<string> Warnings);

public sealed record SclSummary(
    string FilePath,
    int LineCount,
    IReadOnlyList<string> Declarations,
    IReadOnlyList<string> Variables,
    IReadOnlyList<string> Calls,
    IReadOnlyList<string> Comments,
    IReadOnlyList<string> Warnings);

public sealed record DocumentationDraft(string Format, string Markdown);

public sealed class OfflineExportAnalyzer
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".xml", ".scl", ".awl", ".db", ".csv", ".xlsx", ".txt"
    };

    public ExportFolderSummary AnalyzeFolder(string folderPath, int maxFiles = 200)
    {
        folderPath = ResolveExistingFolder(folderPath);
        var warnings = new List<string>();
        var files = Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories)
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path)))
            .Take(Math.Max(1, maxFiles))
            .Select(SummarizeFile)
            .ToList();

        var totalFiles = Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories).Count();
        if (totalFiles > maxFiles)
        {
            warnings.Add($"Folder contains {totalFiles} files; only the first {maxFiles} supported files were summarized.");
        }

        if (files.Count == 0)
        {
            warnings.Add("No supported export files were found. Export TIA Portal blocks/tags/UDTs as XML/SCL/CSV and retry.");
        }

        var extensionCounts = files
            .GroupBy(f => f.Extension, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var nextSteps = new List<string>
        {
            "Review the file list and choose the most relevant XML/SCL artifacts.",
            "Use tia_parse_block_xml for specific XML exports.",
            "Use tia_summarize_scl for SCL sources.",
            "Use tia_generate_export_documentation to create a Markdown summary for a practice report."
        };

        return new ExportFolderSummary(folderPath, totalFiles, files, extensionCounts, warnings, nextSteps);
    }

    public XmlArtifactSummary ParseXml(string filePath)
    {
        filePath = ResolveExistingFile(filePath);
        var warnings = new List<string>();
        var doc = XDocument.Load(filePath, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        var root = doc.Root ?? throw new InvalidOperationException("XML file has no root element.");

        var elementCounts = doc.Descendants()
            .GroupBy(e => e.Name.LocalName)
            .OrderByDescending(g => g.Count())
            .Take(40)
            .ToDictionary(g => g.Key, g => g.Count());

        var interestingAttributes = doc.Descendants()
            .SelectMany(e => e.Attributes())
            .Where(a => IsInterestingName(a.Name.LocalName) && !string.IsNullOrWhiteSpace(a.Value))
            .GroupBy(a => a.Name.LocalName)
            .Select(g => g.First())
            .Take(40)
            .ToDictionary(a => a.Name.LocalName, a => Trim(a.Value, 160));

        var candidateTexts = doc.Descendants()
            .Where(e => IsInterestingName(e.Name.LocalName))
            .Select(e => NormalizeWhitespace(e.Value))
            .Where(value => value.Length is > 0 and < 240)
            .Distinct()
            .Take(60)
            .ToList();

        var artifactName = TryFindArtifactName(root);
        if (artifactName is null)
        {
            warnings.Add("Could not infer a TIA artifact name from common XML attributes/elements.");
        }

        return new XmlArtifactSummary(
            FilePath: filePath,
            RootName: root.Name.LocalName,
            ArtifactName: artifactName,
            ElementCounts: elementCounts,
            InterestingAttributes: interestingAttributes,
            CandidateTexts: candidateTexts,
            Warnings: warnings);
    }

    public SclSummary SummarizeScl(string filePath)
    {
        filePath = ResolveExistingFile(filePath);
        var lines = File.ReadAllLines(filePath);
        var text = string.Join("\n", lines);
        var warnings = new List<string>();

        var declarations = Regex.Matches(text, @"(?im)^\s*(ORGANIZATION_BLOCK|FUNCTION_BLOCK|FUNCTION|DATA_BLOCK|TYPE)\s+[""']?([A-Za-z_][\w]*)[""']?")
            .Select(m => NormalizeWhitespace(m.Value))
            .Distinct()
            .Take(80)
            .ToList();

        var variables = Regex.Matches(text, @"(?im)^\s*([A-Za-z_][\w]*)\s*:\s*([A-Za-z_][\w.]*)(?:\s*:=\s*([^;]+))?;")
            .Select(m => NormalizeWhitespace(m.Value))
            .Distinct()
            .Take(120)
            .ToList();

        var calls = Regex.Matches(text, @"(?im)\b([A-Za-z_][\w]*)\s*\(")
            .Select(m => m.Groups[1].Value)
            .Where(name => !IsLanguageKeyword(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(80)
            .ToList();

        var comments = lines
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith("(*", StringComparison.Ordinal))
            .Take(80)
            .ToList();

        if (declarations.Count == 0)
        {
            warnings.Add("No SCL block declaration was detected. Confirm this is an exported SCL source.");
        }

        return new SclSummary(filePath, lines.Length, declarations, variables, calls, comments, warnings);
    }

    public DocumentationDraft GenerateDocumentation(string folderPath)
    {
        var summary = AnalyzeFolder(folderPath);
        var sb = new StringBuilder();
        sb.AppendLine("# TIA Portal Export Documentation Draft");
        sb.AppendLine();
        sb.AppendLine($"Source folder: `{summary.FolderPath}`");
        sb.AppendLine($"Total files: {summary.TotalFiles}");
        sb.AppendLine();

        sb.AppendLine("## File Types");
        foreach (var pair in summary.ExtensionCounts)
        {
            sb.AppendLine($"- `{pair.Key}`: {pair.Value}");
        }

        sb.AppendLine();
        sb.AppendLine("## Artifacts");
        foreach (var file in summary.Files)
        {
            sb.AppendLine($"- `{file.Path}`");
            sb.AppendLine($"  - Kind: {file.Kind}");
            if (!string.IsNullOrWhiteSpace(file.Name))
            {
                sb.AppendLine($"  - Name: {file.Name}");
            }
            if (!string.IsNullOrWhiteSpace(file.Detail))
            {
                sb.AppendLine($"  - Detail: {file.Detail}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Manual Review Checklist");
        sb.AppendLine("- Confirm exported artifacts match the intended TIA Portal project version.");
        sb.AppendLine("- Review generated SCL/XML changes before manual import.");
        sb.AppendLine("- Compile in TIA Portal after import.");
        sb.AppendLine("- Test offline or in simulation before any hardware download.");
        sb.AppendLine("- Keep a backup/archive of the original project.");

        if (summary.Warnings.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Warnings");
            foreach (var warning in summary.Warnings)
            {
                sb.AppendLine($"- {warning}");
            }
        }

        return new DocumentationDraft("markdown", sb.ToString());
    }

    public DocumentationDraft PrepareManualImportChecklist(string folderPath)
    {
        var summary = AnalyzeFolder(folderPath);
        var sb = new StringBuilder();
        sb.AppendLine("# Manual TIA Portal Import Checklist");
        sb.AppendLine();
        sb.AppendLine("Use this checklist when Openness permissions are unavailable and artifacts must be imported manually.");
        sb.AppendLine();
        sb.AppendLine("## Before Import");
        sb.AppendLine("- Archive or copy the original TIA Portal project.");
        sb.AppendLine("- Confirm the export files come from the same TIA Portal major version, or review migration warnings.");
        sb.AppendLine("- Close online connections to real PLC hardware.");
        sb.AppendLine("- Prefer a test project copy.");
        sb.AppendLine();
        sb.AppendLine("## Candidate Files");
        foreach (var file in summary.Files)
        {
            sb.AppendLine($"- `{file.Path}` ({file.Kind})");
        }
        sb.AppendLine();
        sb.AppendLine("## Import Steps");
        sb.AppendLine("1. Open the copied project in TIA Portal.");
        sb.AppendLine("2. Import one artifact group at a time: UDTs, DBs, FBs/FCs, OBs, tag tables.");
        sb.AppendLine("3. Compile after each group.");
        sb.AppendLine("4. Capture compiler diagnostics and feed them back to Codex for review.");
        sb.AppendLine("5. Run offline/simulation tests before using any real device.");

        return new DocumentationDraft("markdown", sb.ToString());
    }

    private FileSummary SummarizeFile(string path)
    {
        var info = new FileInfo(path);
        var extension = info.Extension.ToLowerInvariant();
        try
        {
            return extension switch
            {
                ".xml" => SummarizeXmlFile(info),
                ".scl" or ".awl" => SummarizeCodeFile(info),
                ".csv" => new FileSummary(info.FullName, extension, info.Length, "tag-table-or-tabular-export", null, "CSV/tabular export"),
                ".xlsx" => new FileSummary(info.FullName, extension, info.Length, "excel-export", null, "Excel export"),
                _ => new FileSummary(info.FullName, extension, info.Length, "text-or-unknown", null, null)
            };
        }
        catch (Exception ex)
        {
            return new FileSummary(info.FullName, extension, info.Length, "unreadable", null, ex.Message);
        }
    }

    private FileSummary SummarizeXmlFile(FileInfo info)
    {
        var xml = ParseXml(info.FullName);
        var kind = xml.RootName.Contains("Document", StringComparison.OrdinalIgnoreCase)
            ? "tia-xml-document"
            : "xml-export";
        return new FileSummary(info.FullName, info.Extension.ToLowerInvariant(), info.Length, kind, xml.ArtifactName, $"Root={xml.RootName}");
    }

    private FileSummary SummarizeCodeFile(FileInfo info)
    {
        var scl = SummarizeScl(info.FullName);
        var name = ExtractDeclarationName(scl.Declarations.FirstOrDefault());
        return new FileSummary(info.FullName, info.Extension.ToLowerInvariant(), info.Length, "scl-or-awl-source", name, $"Lines={scl.LineCount}");
    }

    private static string ResolveExistingFolder(string folderPath)
    {
        var full = Path.GetFullPath(Environment.ExpandEnvironmentVariables(folderPath));
        if (!Directory.Exists(full))
        {
            throw new DirectoryNotFoundException($"Folder not found: {full}");
        }
        return full;
    }

    private static string ResolveExistingFile(string filePath)
    {
        var full = Path.GetFullPath(Environment.ExpandEnvironmentVariables(filePath));
        if (!File.Exists(full))
        {
            throw new FileNotFoundException($"File not found: {full}", full);
        }
        return full;
    }

    private static string? TryFindArtifactName(XElement root)
    {
        var attr = root.DescendantsAndSelf()
            .SelectMany(e => e.Attributes())
            .FirstOrDefault(a => IsNameLike(a.Name.LocalName) && !string.IsNullOrWhiteSpace(a.Value));
        if (attr is not null)
        {
            return Trim(attr.Value, 160);
        }

        var element = root.Descendants()
            .FirstOrDefault(e => IsNameLike(e.Name.LocalName) && !string.IsNullOrWhiteSpace(e.Value));
        return element is null ? null : Trim(NormalizeWhitespace(element.Value), 160);
    }

    private static bool IsNameLike(string name) =>
        name.Equals("Name", StringComparison.OrdinalIgnoreCase)
        || name.Equals("ObjectName", StringComparison.OrdinalIgnoreCase)
        || name.Equals("BlockName", StringComparison.OrdinalIgnoreCase);

    private static bool IsInterestingName(string name)
    {
        return IsNameLike(name)
            || name.Contains("Block", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Type", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Number", StringComparison.OrdinalIgnoreCase)
            || name.Contains("ProgrammingLanguage", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Comment", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Title", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLanguageKeyword(string name)
    {
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "IF", "ELSIF", "FOR", "WHILE", "CASE", "RETURN", "AND", "OR", "NOT", "REAL", "INT", "BOOL", "TIME"
        }.Contains(name);
    }

    private static string NormalizeWhitespace(string value) =>
        Regex.Replace(value.Trim(), @"\s+", " ");

    private static string Trim(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";

    private static string? ExtractDeclarationName(string? declaration)
    {
        if (string.IsNullOrWhiteSpace(declaration))
        {
            return null;
        }

        var match = Regex.Match(declaration, @"(?i)^\s*(ORGANIZATION_BLOCK|FUNCTION_BLOCK|FUNCTION|DATA_BLOCK|TYPE)\s+[""']?([^""'\s]+)");
        return match.Success ? match.Groups[2].Value : declaration;
    }
}
