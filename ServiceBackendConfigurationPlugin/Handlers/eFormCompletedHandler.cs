/*
The MIT License (MIT)

Copyright (c) 2007 - 2022 Microting A/S

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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microting.eForm.Infrastructure;
using Microting.eForm.Infrastructure.Constants;
using Microting.EformBackendConfigurationBase.Infrastructure.Data;
using Microting.EformBackendConfigurationBase.Infrastructure.Data.Entities;
using Microting.ItemsPlanningBase.Infrastructure.Data;
using Microting.ItemsPlanningBase.Infrastructure.Data.Entities;
using Microting.ItemsPlanningBase.Infrastructure.Enums;

namespace ServiceBackendConfigurationPlugin.Handlers
{
    using System.Threading.Tasks;
    using Infrastructure.Helpers;
    using Messages;
    using Rebus.Handlers;

    public class EFormCompletedHandler : IHandleMessages<eFormCompleted>
    {
        private readonly eFormCore.Core _sdkCore;
        private readonly ItemsPlanningDbContextHelper _itemsPlanningDbContextHelper;
        private readonly BackendConfigurationDbContextHelper _backendConfigurationDbContextHelper;

        public EFormCompletedHandler(eFormCore.Core sdkCore, ItemsPlanningDbContextHelper itemsPlanningDbContextHelper, BackendConfigurationDbContextHelper backendConfigurationDbContextHelper)
        {
            _sdkCore = sdkCore;
            _itemsPlanningDbContextHelper = itemsPlanningDbContextHelper;
            _backendConfigurationDbContextHelper = backendConfigurationDbContextHelper;
        }

        public async Task Handle(eFormCompleted message)
        {
            Console.WriteLine($"EFormCompletedHandler .Handle called");
            Console.WriteLine($"message.CaseId: {message.CaseId}");
            Console.WriteLine($"message.MicrotingUId: {message.MicrotingUId}");
            Console.WriteLine($"message.CheckId: {message.CheckId}");
            await using MicrotingDbContext sdkDbContext = _sdkCore.DbContextHelper.GetDbContext();
            await using ItemsPlanningPnDbContext itemsPlanningPnDbContext = _itemsPlanningDbContextHelper.GetDbContext();
            await using BackendConfigurationPnDbContext backendConfigurationPnDbContext = _backendConfigurationDbContextHelper.GetDbContext();
            var planningCaseSite =
                await itemsPlanningPnDbContext.PlanningCaseSites.AsNoTracking().SingleOrDefaultAsync(x => x.MicrotingSdkCaseId == message.CaseId);

            if (planningCaseSite == null)
            {
                // var site = await sdkDbContext.Sites.SingleOrDefaultAsync(x => x.MicrotingUid == caseDto.SiteUId);
                var checkListSite = await sdkDbContext.CheckListSites.SingleOrDefaultAsync(x =>
                    x.MicrotingUid == message.MicrotingUId).ConfigureAwait(false);
                planningCaseSite =
                    await itemsPlanningPnDbContext.PlanningCaseSites.AsNoTracking().SingleOrDefaultAsync(x =>
                        x.MicrotingCheckListSitId == checkListSite.Id).ConfigureAwait(false);
            }

            if (planningCaseSite == null)
            {
                Console.WriteLine($"planningCaseSite is null for CheckId: {message.CheckId}");
                return;
            }

            while (planningCaseSite.Status != 100)
            {
                Thread.Sleep(1000);
                Console.WriteLine($"Waiting for case {planningCaseSite.MicrotingSdkCaseId} to be completed");
                planningCaseSite = itemsPlanningPnDbContext.PlanningCaseSites.AsNoTracking().Single(x => x.Id == planningCaseSite.Id);
                if (planningCaseSite.Status == 100)
                {
                    planningCaseSite = itemsPlanningPnDbContext.PlanningCaseSites.Single(x => x.Id == planningCaseSite.Id);
                }
            }
            Console.WriteLine($"planningCaseSite {planningCaseSite.MicrotingSdkCaseId} is completed");
            Thread.Sleep(10000);

            var backendPlanning = await backendConfigurationPnDbContext.AreaRulePlannings.Where(x => x.ItemPlanningId == planningCaseSite.PlanningId).FirstOrDefaultAsync();

            var property = await backendConfigurationPnDbContext.Properties.SingleOrDefaultAsync(x => x.Id == backendPlanning.PropertyId);

            if (property == null)
            {
                return;
            }

            if (property.ComplianceStatus == 1)
            {
                var preList = new List<int>();
                var backendPlannings = await backendConfigurationPnDbContext.AreaRulePlannings.Where(x => x.PropertyId == property.Id).ToListAsync();
                foreach (AreaRulePlanning areaRulePlanning in backendPlannings)
                {
                    var planningCases =
                        await itemsPlanningPnDbContext.PlanningCases
                            .AsNoTracking()
                            .Where(x => x.PlanningId == areaRulePlanning.ItemPlanningId)
                            .Where(x => x.Status != 100)
                            .Where(x => x.WorkflowState != Constants.WorkflowStates.Retracted)
                            .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                            .ToListAsync();

                    foreach (PlanningCase planningCase in planningCases)
                    {
                        var planning =
                            await itemsPlanningPnDbContext.Plannings
                                .AsNoTracking()
                                .Where(x => x.Id == planningCase.PlanningId)
                                .Where(x => x.RepeatEvery != 0)
                                .Where(x => x.StartDate < DateTime.UtcNow)
                                .SingleOrDefaultAsync();

                        if (planning == null)
                        {
                            continue;
                        }

                        if (planning.RepeatEvery == 1 && planning.RepeatType == RepeatType.Day)
                        {
                            continue;
                        }

                        preList.Add(planning.Id);
                    }
                }

                if (preList.Count == 0)
                {
                    Console.WriteLine($"All cases has been completed for property {property.Name}, so setting compliance status to 0");
                    property.ComplianceStatus = 0;
                    await property.Update(backendConfigurationPnDbContext);
                }

            }

        }
    }
}