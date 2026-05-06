namespace TiaPortalAgenticToolkit.Openness;

public sealed class TiaPortalSession
{
    private readonly TiaEnvironmentProbe _probe = new();
    private readonly OfflineExportAnalyzer _offline = new();

    public TiaEnvironmentStatus GetEnvironmentStatus() => _probe.GetStatus();

    public TiaCapabilities GetCapabilities() => _probe.GetCapabilities();

    public ExportFolderSummary AnalyzeExportFolder(string folderPath, int maxFiles = 200) =>
        _offline.AnalyzeFolder(folderPath, maxFiles);

    public XmlArtifactSummary ParseBlockXml(string filePath) =>
        _offline.ParseXml(filePath);

    public SclSummary SummarizeScl(string filePath) =>
        _offline.SummarizeScl(filePath);

    public DocumentationDraft GenerateExportDocumentation(string folderPath) =>
        _offline.GenerateDocumentation(folderPath);

    public DocumentationDraft PrepareManualImportChecklist(string folderPath) =>
        _offline.PrepareManualImportChecklist(folderPath);

    public object AttachToRunningPortal(int? processId)
    {
        var status = _probe.GetStatus();

        if (status.EngineeringAssemblyCandidates.Count == 0)
        {
            return new
            {
                attached = false,
                reason = "Siemens.Engineering.dll was not found. Install TIA Portal Openness on this machine.",
                status
            };
        }

        if (status.RunningPortalProcesses.Count == 0)
        {
            return new
            {
                attached = false,
                reason = "No running TIA Portal process was detected. Open TIA Portal and retry.",
                status
            };
        }

        return new
        {
            attached = false,
            reason = "Attach is intentionally stubbed in v0.1.0. The environment is ready for the next implementation step.",
            requestedProcessId = processId,
            status
        };
    }
}
