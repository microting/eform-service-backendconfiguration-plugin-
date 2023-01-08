using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microting.eForm.Infrastructure;
using Microting.EformBackendConfigurationBase.Infrastructure.Data;
using Microting.ItemsPlanningBase.Infrastructure.Data;
using Rebus.Handlers;
using ServiceBackendConfigurationPlugin.Infrastructure.Helpers;
using ServiceBackendConfigurationPlugin.Messages;

namespace ServiceBackendConfigurationPlugin.Handlers;

public class EformRetrievedHandler : IHandleMessages<eFormRetrieved>
{
    private readonly eFormCore.Core _sdkCore;
    private readonly ItemsPlanningDbContextHelper _itemsPlanningDbContextHelper;
    private readonly BackendConfigurationDbContextHelper _backendConfigurationDbContextHelper;

    public EformRetrievedHandler(eFormCore.Core sdkCore,
            ItemsPlanningDbContextHelper itemsPlanningDbContextHelper,
            BackendConfigurationDbContextHelper backendConfigurationDbContextHelper)
        {
            _sdkCore = sdkCore;
            _itemsPlanningDbContextHelper = itemsPlanningDbContextHelper;
            _backendConfigurationDbContextHelper = backendConfigurationDbContextHelper;
        }

    public async Task Handle(eFormRetrieved message)
    {
        await using MicrotingDbContext sdkDbContext = _sdkCore.DbContextHelper.GetDbContext();
        await using ItemsPlanningPnDbContext itemsPlanningPnDbContext = _itemsPlanningDbContextHelper.GetDbContext();
        await using BackendConfigurationPnDbContext backendConfigurationPnDbContext =
            _backendConfigurationDbContextHelper.GetDbContext();

        var theCase = await sdkDbContext.Cases.FirstOrDefaultAsync(x => x.Id == message.CaseId);

        if (theCase != null)
        {
            var planningCaseSite =
                await itemsPlanningPnDbContext.PlanningCaseSites
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.MicrotingSdkCaseId == message.CaseId);

            if (planningCaseSite == null)
            {
                // var site = await sdkDbContext.Sites.FirstOrDefaultAsync(x => x.MicrotingUid == caseDto.SiteUId);
                var checkListSite = await sdkDbContext.CheckListSites
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.MicrotingUid == theCase.MicrotingUid);
                if (checkListSite == null) return;
                planningCaseSite =
                    await itemsPlanningPnDbContext.PlanningCaseSites
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.MicrotingCheckListSitId == checkListSite.Id);
            }

            var areaRulePlanning = await backendConfigurationPnDbContext.AreaRulePlannings
                .Where(x => x.ItemPlanningId == planningCaseSite.PlanningId)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (areaRulePlanning != null)
            {
                var property =
                    await backendConfigurationPnDbContext.Properties.FirstAsync(
                        x => x.Id == areaRulePlanning.PropertyId);

                var planning = await itemsPlanningPnDbContext.Plannings.AsNoTracking()
                    .FirstAsync(x => x.Id == planningCaseSite.PlanningId);

                var planningSite = await backendConfigurationPnDbContext.PlanningSites.FirstAsync(x =>
                    x.SiteId == planningCaseSite.MicrotingSdkSiteId && x.AreaRulePlanningsId == areaRulePlanning.Id);

                planningSite.Status = 77;
                await planningSite.Update(backendConfigurationPnDbContext);
            }
        }
    }
}