namespace ServiceBackendConfigurationPlugin.Messages;

public class MorningTourCaseCompleted
{
    public int? CaseId { get; }
    public int? MicrotingUId { get; }
    public int? CheckId { get; }
    public int? SiteUId { get; }

    public MorningTourCaseCompleted(int? caseId, int? microtingUId, int? checkId, int? siteUId)
    {
        CaseId = caseId;
        MicrotingUId = microtingUId;
        CheckId = checkId;
        SiteUId = siteUId;
    }
}