namespace TiaPortalAgenticToolkit.Openness;

public sealed class TiaPortalSession
{
    private readonly TiaEnvironmentProbe _probe = new();

    public TiaEnvironmentStatus GetEnvironmentStatus() => _probe.GetStatus();

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
