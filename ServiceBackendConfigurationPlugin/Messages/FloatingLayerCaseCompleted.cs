namespace ServiceBackendConfigurationPlugin.Messages;

public class FloatingLayerCaseCompleted(int? caseId, int? microtingUId, int? checkId, int? siteUId)
{
    public int? CaseId { get; } = caseId;
    public int? MicrotingUId { get; } = microtingUId;
    public int? CheckId { get; } = checkId;
    public int? SiteUId { get; } = siteUId;
}