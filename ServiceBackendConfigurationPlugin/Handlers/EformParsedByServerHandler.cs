/*
The MIT License (MIT)

Copyright (c) 2007 - 2020 Microting A/S

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microting.eForm.Infrastructure;
using Microting.eForm.Infrastructure.Constants;
using Microting.EformBackendConfigurationBase.Infrastructure.Data;
using Microting.EformBackendConfigurationBase.Infrastructure.Data.Entities;
using Microting.ItemsPlanningBase.Infrastructure.Data;
using Rebus.Handlers;
using ServiceBackendConfigurationPlugin.Infrastructure.Helpers;
using ServiceBackendConfigurationPlugin.Messages;

namespace ServiceBackendConfigurationPlugin.Handlers
{
    public class EformParsedByServerHandler : IHandleMessages<EformParsedByServer>
    {
        private readonly eFormCore.Core _sdkCore;
        private readonly ItemsPlanningDbContextHelper _itemsPlanningDbContextHelper;
        private readonly BackendConfigurationDbContextHelper _backendConfigurationDbContextHelper;

        public EformParsedByServerHandler(eFormCore.Core sdkCore,
            ItemsPlanningDbContextHelper itemsPlanningDbContextHelper,
            BackendConfigurationDbContextHelper backendConfigurationDbContextHelper)
        {
            _sdkCore = sdkCore;
            _itemsPlanningDbContextHelper = itemsPlanningDbContextHelper;
            _backendConfigurationDbContextHelper = backendConfigurationDbContextHelper;
        }

        public async Task Handle(EformParsedByServer message)
        {
            await using MicrotingDbContext sdkDbContext = _sdkCore.DbContextHelper.GetDbContext();
            await using ItemsPlanningPnDbContext itemsPlanningPnDbContext = _itemsPlanningDbContextHelper.GetDbContext();
            await using BackendConfigurationPnDbContext backendConfigurationPnDbContext = _backendConfigurationDbContextHelper.GetDbContext();
            var planningCaseSite =
                await itemsPlanningPnDbContext.PlanningCaseSites.SingleOrDefaultAsync(x => x.MicrotingSdkCaseId == message.CaseId);

            if (planningCaseSite == null)
            {
                // var site = await sdkDbContext.Sites.SingleOrDefaultAsync(x => x.MicrotingUid == caseDto.SiteUId);
                var checkListSite = await sdkDbContext.CheckListSites.SingleOrDefaultAsync(x =>
                    x.MicrotingUid == message.MicrotingUId);
                planningCaseSite =
                    await itemsPlanningPnDbContext.PlanningCaseSites.SingleOrDefaultAsync(x =>
                        x.MicrotingCheckListSitId == checkListSite.Id);
            }

            var backendPlannings = await backendConfigurationPnDbContext.AreaRulePlannings.Where(x => x.ItemPlanningId == planningCaseSite.PlanningId).FirstOrDefaultAsync();

            if (backendPlannings != null)
            {
                var property = await backendConfigurationPnDbContext.Properties.SingleAsync(x => x.Id == backendPlannings.PropertyId);

                var planning = await itemsPlanningPnDbContext.Plannings.SingleAsync(x => x.Id == planningCaseSite.PlanningId);

                Compliance compliance = new Compliance()
                {
                    PropertyId = property.Id,
                    PlanningId = planningCaseSite.PlanningId,
                    AreaId = backendPlannings.AreaId.ToString(),
                    Deadline = (DateTime)planning.NextExecutionTime,
                    StartDate = (DateTime)planning.LastExecutedTime
                };

                await compliance.Create(backendConfigurationPnDbContext);

                if (property is {ComplianceStatus: 0})
                {
                    property.ComplianceStatus = 1;
                    await property.Update(backendConfigurationPnDbContext);
                }

                if (backendConfigurationPnDbContext.Compliances.Any(x => x.Deadline < DateTime.UtcNow && x.WorkflowState != Constants.WorkflowStates.Removed))
                {
                    property.ComplianceStatus = 2;
                    await property.Update(backendConfigurationPnDbContext);
                }
            }



        }
    }
}