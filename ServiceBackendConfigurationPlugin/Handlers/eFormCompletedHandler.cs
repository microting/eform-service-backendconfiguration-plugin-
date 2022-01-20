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

            Planning planning =
                await itemsPlanningPnDbContext.Plannings.AsNoTracking().SingleAsync(x => x.Id == planningCaseSite.PlanningId);

            if (planning.RepeatType == RepeatType.Day && planning.RepeatEvery == 0) {}
            else
            {
                while (planningCaseSite.Status != 100)
                {
                    Thread.Sleep(1000);
                    Console.WriteLine($"Waiting for case {planningCaseSite.Id} to be completed");
                    planningCaseSite = itemsPlanningPnDbContext.PlanningCaseSites.AsNoTracking()
                        .Single(x => x.Id == planningCaseSite.Id);
                    if (planningCaseSite.Status == 100)
                    {
                        planningCaseSite =
                            itemsPlanningPnDbContext.PlanningCaseSites.Single(x => x.Id == planningCaseSite.Id);
                    }
                }

                Console.WriteLine($"planningCaseSite {planningCaseSite.Id} is completed");
                Thread.Sleep(10000);


                Compliance compliance = await backendConfigurationPnDbContext.Compliances
                    .Where(x => x.Deadline == planning.NextExecutionTime)
                    .SingleOrDefaultAsync(x => x.PlanningId == planningCaseSite.PlanningId);

                if (compliance != null)
                {
                    await compliance.Delete(backendConfigurationPnDbContext);
                }

                var backendPlanning = await backendConfigurationPnDbContext.AreaRulePlannings
                    .Where(x => x.ItemPlanningId == planningCaseSite.PlanningId).FirstOrDefaultAsync();

                var property =
                    await backendConfigurationPnDbContext.Properties.SingleOrDefaultAsync(x =>
                        x.Id == backendPlanning.PropertyId);

                if (property == null)
                {
                    return;
                }

                if (backendConfigurationPnDbContext.Compliances.Any(x =>
                        x.Deadline < DateTime.UtcNow && x.PropertyId == property.Id &&
                        x.WorkflowState != Constants.WorkflowStates.Removed))
                {
                    property.ComplianceStatus = 2;
                    property.ComplianceStatusThirty = 2;
                    await property.Update(backendConfigurationPnDbContext);
                }

                if (backendConfigurationPnDbContext.Compliances.Any(x =>
                        x.PropertyId == property.Id && x.WorkflowState != Constants.WorkflowStates.Removed))
                {
                    if (property is {ComplianceStatus: 0})
                    {
                        property.ComplianceStatus = 1;
                        await property.Update(backendConfigurationPnDbContext);
                    }

                    if (property is {ComplianceStatusThirty: 0})
                    {
                        if (backendConfigurationPnDbContext.Compliances.Any(x =>
                                x.Deadline < DateTime.UtcNow.AddDays(30) && x.PropertyId == property.Id &&
                                x.WorkflowState != Constants.WorkflowStates.Removed))
                        {
                            property.ComplianceStatusThirty = 1;
                            await property.Update(backendConfigurationPnDbContext);
                        }
                    }
                    else
                    {
                        if (!backendConfigurationPnDbContext.Compliances.Any(x =>
                                x.Deadline < DateTime.UtcNow.AddDays(30) && x.PropertyId == property.Id &&
                                x.WorkflowState != Constants.WorkflowStates.Removed))
                        {
                            property.ComplianceStatusThirty = 0;
                            await property.Update(backendConfigurationPnDbContext);
                        }
                    }
                }
                else
                {
                    property.ComplianceStatus = 0;
                    property.ComplianceStatusThirty = 0;
                    await property.Update(backendConfigurationPnDbContext);
                }
            }
        }
    }
}