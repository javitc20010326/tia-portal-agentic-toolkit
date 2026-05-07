using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace TiaPortalAgenticToolkit.Openness;

public sealed record ProjectTextSample(
    string Category,
    string ViewPath,
    IReadOnlyList<string> Texts);

public sealed record ProjectTextsSummary(
    string FilePath,
    IReadOnlyDictionary<string, int> CategoryCounts,
    IReadOnlyDictionary<string, int> HmiScreens,
    IReadOnlyDictionary<string, int> HmiObjectTypes,
    IReadOnlyDictionary<string, int> PlcBlocks,
    IReadOnlyList<ProjectTextSample> Samples,
    IReadOnlyList<string> Warnings);

public sealed record WebServerBinding(
    string FilePath,
    string Database,
    string Tag,
    string RawBinding,
    int LineNumber);

public sealed record WebServerBindingSummary(
    string RootPath,
    int FilesScanned,
    IReadOnlyList<WebServerBinding> Bindings,
    IReadOnlyDictionary<string, int> DatabaseCounts,
    IReadOnlyDictionary<string, int> TagCounts,
    IReadOnlyList<string> Warnings);

public sealed record DbSourceSummary(
    string FilePath,
    string? BlockName,
    bool OptimizedAccess,
    IReadOnlyList<string> Sections,
    IReadOnlyList<string> Variables,
    IReadOnlyList<string> Warnings);

public sealed record PdfPrintoutTextSummary(
    string FilePath,
    int Characters,
    IReadOnlyDictionary<string, int> HmiObjectTypeCounts,
    IReadOnlyList<string> EventNames,
    IReadOnlyList<string> VariableReferences,
    IReadOnlyList<string> ScreenNames,
    IReadOnlyList<string> Warnings);

public sealed class TiaArtifactParsers
{
    private static readonly Regex WebBindingRegex = new(@":=""(?<db>[^""]+)""\.(?<tag>[^:]+):", RegexOptions.Compiled);

    public ProjectTextsSummary AnalyzeProjectTextsXlsx(string filePath)
    {
        filePath = ResolveExistingFile(filePath);
        var warnings = new List<string>();
        var rows = ReadFirstWorksheetRows(filePath, warnings);

        var categories = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var hmiScreens = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var hmiObjects = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var plcBlocks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var samples = new List<ProjectTextSample>();

        foreach (var row in rows.Skip(1))
        {
            if (row.Count < 2)
            {
                continue;
            }

            var category = row[0];
            var viewPath = row[1];
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(viewPath))
            {
                continue;
            }

            Increment(categories, category);
            var parts = viewPath.Split('\\', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var hmiIndex = Array.FindIndex(parts, part => part.Equals("Imágenes", StringComparison.OrdinalIgnoreCase) || part.Equals("Images", StringComparison.OrdinalIgnoreCase));
            if (hmiIndex >= 0)
            {
                if (hmiIndex + 1 < parts.Length)
                {
                    Increment(hmiScreens, parts[hmiIndex + 1]);
                }

                if (hmiIndex + 2 < parts.Length)
                {
                    Increment(hmiObjects, NormalizeHmiObjectType(parts[hmiIndex + 2]));
                }
            }

            var blockIndex = Array.FindIndex(parts, part => part.Equals("Bloques de programa", StringComparison.OrdinalIgnoreCase) || part.Equals("Program blocks", StringComparison.OrdinalIgnoreCase));
            if (blockIndex >= 0 && blockIndex + 1 < parts.Length)
            {
                Increment(plcBlocks, parts[blockIndex + 1]);
            }

            var texts = row.Skip(4)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Take(4)
                .ToList();
            if (texts.Count > 0 && samples.Count < 80)
            {
                samples.Add(new ProjectTextSample(category, viewPath, texts));
            }
        }

        if (rows.Count == 0)
        {
            warnings.Add("No rows were extracted from the workbook.");
        }

        return new ProjectTextsSummary(
            filePath,
            Top(categories, 40),
            Top(hmiScreens, 80),
            Top(hmiObjects, 40),
            Top(plcBlocks, 40),
            samples,
            warnings);
    }

    public WebServerBindingSummary AnalyzeWebServerBindings(string path)
    {
        path = ResolveExistingPath(path);
        var warnings = new List<string>();
        var files = Directory.Exists(path)
            ? Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                .Where(file => IsWebTextFile(file))
                .ToList()
            : new List<string> { path };

        var bindings = new List<WebServerBinding>();
        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                foreach (Match match in WebBindingRegex.Matches(lines[i]))
                {
                    bindings.Add(new WebServerBinding(
                        file,
                        match.Groups["db"].Value,
                        match.Groups["tag"].Value.Trim(),
                        match.Value,
                        i + 1));
                }
            }
        }

        if (bindings.Count == 0)
        {
            warnings.Add("No TIA web-server bindings of the form :=\"DB\".Tag: were found.");
        }

        return new WebServerBindingSummary(
            path,
            files.Count,
            bindings.Take(200).ToList(),
            Top(bindings.GroupBy(binding => binding.Database).ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase), 40),
            Top(bindings.GroupBy(binding => binding.Tag).ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase), 80),
            warnings);
    }

    public DbSourceSummary AnalyzeDbSource(string filePath)
    {
        filePath = ResolveExistingFile(filePath);
        var text = File.ReadAllText(filePath);
        var warnings = new List<string>();

        var blockName = Regex.Match(text, @"(?im)^\s*DATA_BLOCK\s+[""']?(?<name>[^""'\r\n]+)[""']?").Groups["name"].Value.Trim();
        var sections = Regex.Matches(text, @"(?im)^\s*(VAR|VAR_INPUT|VAR_OUTPUT|VAR_IN_OUT|VAR_TEMP|BEGIN|END_DATA_BLOCK)\b")
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var variables = Regex.Matches(text, @"(?im)^\s*(?<name>[A-Za-z_][\w]*)\s*:\s*(?<type>[A-Za-z_][\w.]*)")
            .Select(match => $"{match.Groups["name"].Value}: {match.Groups["type"].Value}")
            .Take(120)
            .ToList();

        if (string.IsNullOrWhiteSpace(blockName))
        {
            warnings.Add("No DATA_BLOCK declaration was detected.");
        }

        return new DbSourceSummary(
            filePath,
            string.IsNullOrWhiteSpace(blockName) ? null : blockName,
            text.Contains("S7_Optimized_Access", StringComparison.OrdinalIgnoreCase),
            sections,
            variables,
            warnings);
    }

    public PdfPrintoutTextSummary AnalyzePdfPrintoutText(string filePath)
    {
        filePath = ResolveExistingFile(filePath);
        var text = File.ReadAllText(filePath);
        var warnings = new List<string>
        {
            "This parser expects text extracted from a TIA Portal printout PDF, not the binary PDF itself. Convert the PDF to text first when possible."
        };

        var objectCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in Regex.Matches(text, @"(?i)(Bot[oó]n|Campo ES|Campo de texto|Interruptor|Deslizador|Gr[aá]fica|L[ií]nea|Rect[aá]ngulo)"))
        {
            Increment(objectCounts, match.Value);
        }

        var events = Regex.Matches(text, @"(?i)Nombre de evento\s*(?<name>Creada|Borrada|Pulsar|Soltar|Conmutar ON|Conmutar OFF)")
            .Select(match => match.Groups["name"].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(80)
            .ToList();

        var variables = Regex.Matches(text, @"(?i)\b[A-Za-z0-9_ ÁÉÍÓÚáéíóúñÑ]+_DB[_ A-Za-z0-9{}\-.]*")
            .Select(match => NormalizeWhitespace(match.Value))
            .Where(value => value.Length > 3)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(120)
            .ToList();

        var screens = Regex.Matches(text, @"(?i)Copia impresa de\s*(?<name>[A-Za-z0-9_ ÁÉÍÓÚáéíóúñÑ-]+)")
            .Select(match => NormalizeWhitespace(match.Groups["name"].Value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(80)
            .ToList();

        return new PdfPrintoutTextSummary(filePath, text.Length, Top(objectCounts, 40), events, variables, screens, warnings);
    }

    private static List<List<string>> ReadFirstWorksheetRows(string filePath, List<string> warnings)
    {
        using var archive = ZipFile.OpenRead(filePath);
        var sharedStrings = ReadSharedStrings(archive);
        var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
            ?? archive.Entries.FirstOrDefault(entry => entry.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase));
        if (sheetEntry is null)
        {
            warnings.Add("No worksheet XML was found in the XLSX package.");
            return [];
        }

        using var stream = sheetEntry.Open();
        var doc = XDocument.Load(stream);
        var rows = new List<List<string>>();

        foreach (var row in doc.Descendants().Where(e => e.Name.LocalName == "row"))
        {
            var values = new List<string>();
            foreach (var cell in row.Elements().Where(e => e.Name.LocalName == "c"))
            {
                var reference = cell.Attribute("r")?.Value;
                var columnIndex = GetColumnIndex(reference);
                while (columnIndex is not null && values.Count < columnIndex.Value)
                {
                    values.Add("");
                }

                var type = cell.Attribute("t")?.Value;
                var raw = cell.Elements().FirstOrDefault(e => e.Name.LocalName == "v")?.Value
                    ?? cell.Elements().FirstOrDefault(e => e.Name.LocalName == "is")?.Value
                    ?? "";
                if (type == "s" && int.TryParse(raw, out var sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
                {
                    values.Add(sharedStrings[sharedIndex]);
                }
                else
                {
                    values.Add(raw);
                }
            }
            rows.Add(values);
        }

        return rows;
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return [];
        }

        using var stream = entry.Open();
        var doc = XDocument.Load(stream);
        return doc.Descendants()
            .Where(e => e.Name.LocalName == "si")
            .Select(si => string.Concat(si.Descendants().Where(t => t.Name.LocalName == "t").Select(t => t.Value)))
            .ToList();
    }

    private static bool IsWebTextFile(string file) =>
        Path.GetExtension(file).Equals(".html", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(file).Equals(".htm", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(file).Equals(".txt", StringComparison.OrdinalIgnoreCase);

    private static string ResolveExistingPath(string path)
    {
        var full = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
        if (!File.Exists(full) && !Directory.Exists(full))
        {
            throw new FileNotFoundException($"Path not found: {full}", full);
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

    private static void Increment(IDictionary<string, int> dictionary, string key)
    {
        key = NormalizeWhitespace(key);
        if (key.Length == 0)
        {
            return;
        }

        dictionary[key] = dictionary.TryGetValue(key, out var count) ? count + 1 : 1;
    }

    private static string NormalizeHmiObjectType(string name)
    {
        var index = name.IndexOf('_');
        return index > 0 ? name[..index] : name;
    }

    private static IReadOnlyDictionary<string, int> Top(IDictionary<string, int> source, int count) =>
        source
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Take(count)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

    private static string NormalizeWhitespace(string value) =>
        Regex.Replace(value.Trim(), @"\s+", " ");

    private static int? GetColumnIndex(string? cellReference)
    {
        if (string.IsNullOrWhiteSpace(cellReference))
        {
            return null;
        }

        var index = 0;
        foreach (var ch in cellReference)
        {
            if (!char.IsLetter(ch))
            {
                break;
            }

            index = (index * 26) + (char.ToUpperInvariant(ch) - 'A' + 1);
        }

        return index == 0 ? null : index - 1;
    }
}
