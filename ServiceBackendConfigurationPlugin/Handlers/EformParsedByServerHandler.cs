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
using Microting.ItemsPlanningBase.Infrastructure.Enums;
using Rebus.Handlers;
using ServiceBackendConfigurationPlugin.Infrastructure.Helpers;
using ServiceBackendConfigurationPlugin.Messages;

namespace ServiceBackendConfigurationPlugin.Handlers
{
    public static class DateTimeExtensions
    {
        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }
    }

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
                await itemsPlanningPnDbContext.PlanningCaseSites
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.MicrotingSdkCaseId == message.CaseId);

            if (planningCaseSite == null)
            {
                // var site = await sdkDbContext.Sites.FirstOrDefaultAsync(x => x.MicrotingUid == caseDto.SiteUId);
                var checkListSite = await sdkDbContext.CheckListSites
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                    x.MicrotingUid == message.MicrotingUId);
                if (checkListSite == null) return;
                planningCaseSite =
                    await itemsPlanningPnDbContext.PlanningCaseSites
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                        x.MicrotingCheckListSitId == checkListSite.Id);
            }

            if (planningCaseSite == null)
            {
                return;
            }

            var areaRulePlanning = await backendConfigurationPnDbContext.AreaRulePlannings
                .Where(x => x.ItemPlanningId == planningCaseSite.PlanningId)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (areaRulePlanning != null)
            {
                var property = await backendConfigurationPnDbContext.Properties.FirstAsync(x => x.Id == areaRulePlanning.PropertyId);

                var planning = await itemsPlanningPnDbContext.Plannings.AsNoTracking().FirstAsync(x => x.Id == planningCaseSite.PlanningId);

                var planningSite = await backendConfigurationPnDbContext.PlanningSites.FirstAsync(x => x.SiteId == planningCaseSite.MicrotingSdkSiteId && x.AreaRulePlanningsId == areaRulePlanning.Id);

                planningSite.Status = 70;
                await planningSite.Update(backendConfigurationPnDbContext);

                if (!areaRulePlanning.ComplianceEnabled)
                {
                    return;
                }

                if (planning.RepeatEvery == 0 && planning.RepeatType == RepeatType.Day) { }
                else
                {
                    if (planning.NextExecutionTime == null)
                    {
                        var now = DateTime.UtcNow;
                        if (planning.RepeatType == RepeatType.Day)
                        {
                            if (planning.RepeatEvery != 0)
                            {
                                var nextRun = now.AddDays(planning.RepeatEvery);
                                planning.NextExecutionTime = nextRun;
                                await planning.Update(itemsPlanningPnDbContext);
                            }
                        }
                        if (planning.RepeatType == RepeatType.Week)
                        {
                            if (planning.DayOfWeek != null)
                            {
                                var startOfWeek =
                                    new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).StartOfWeek(
                                        (DayOfWeek) planning.DayOfWeek);
                                var nextRun = startOfWeek.AddDays(planning.RepeatEvery * 7);
                                planning.NextExecutionTime = nextRun;
                                await planning.Update(itemsPlanningPnDbContext);
                            }
                        }

                        if (planning.RepeatType == RepeatType.Month)
                        {
                            if (planning.DayOfMonth != null)
                            {
                                if (planning.DayOfMonth == 0)
                                {
                                    planning.DayOfMonth = 1;
                                }
                                var startOfMonth = new DateTime(now.Year, now.Month, (int) planning.DayOfMonth, 0, 0, 0);
                                var nextRun = startOfMonth.AddMonths(planning.RepeatEvery);
                                planning.NextExecutionTime = nextRun;
                                await planning.Update(itemsPlanningPnDbContext);
                            }
                        }
                    }

                    if (!backendConfigurationPnDbContext.Compliances.AsNoTracking().Any(x =>
                            x.Deadline == (DateTime)planning.NextExecutionTime &&
                            x.PlanningId == planningCaseSite.PlanningId &&
                            // x.PlanningCaseSiteId == planningCaseSite.Id &&
                            x.WorkflowState != Constants.WorkflowStates.Removed))
                    {
                        var deadLine = (DateTime)planning.NextExecutionTime!;
                        Compliance compliance = new Compliance
                        {
                            PropertyId = property.Id,
                            PlanningId = planningCaseSite.PlanningId,
                            AreaId = areaRulePlanning.AreaId,
                            Deadline = new DateTime(deadLine.Year, deadLine.Month, deadLine.Day, 0, 0, 0),
                            StartDate = (DateTime)planning.LastExecutedTime!,
                            MicrotingSdkeFormId = planning.RelatedEFormId,
                            // PlanningCaseSiteId = planningCaseSite.Id,
                            MicrotingSdkCaseId = (int) message.CaseId!
                        };

                        await compliance.Create(backendConfigurationPnDbContext);
                    }

                    // if (property is {ComplianceStatus: 0})
                    // {
                    //     property.ComplianceStatus = 1;
                    //     await property.Update(backendConfigurationPnDbContext);
                    // }
                    //
                    // if (property is {ComplianceStatusThirty: 0})
                    // {
                    //     if (backendConfigurationPnDbContext.Compliances.Any(x => x.Deadline < DateTime.UtcNow.AddDays(30) && x.PropertyId == property.Id && x.WorkflowState != Constants.WorkflowStates.Removed))
                    //     {
                    //         property.ComplianceStatusThirty = 1;
                    //         await property.Update(backendConfigurationPnDbContext);
                    //     }
                    // }
                    var today = new DateTime(DateTime.Now.AddDays(1).Year, DateTime.Now.AddDays(1).Month, DateTime.Now.AddDays(1).Day, 0, 0, 0);

                    if (backendConfigurationPnDbContext.Compliances.AsNoTracking().Any(x => x.Deadline < today && x.PropertyId == property.Id && x.WorkflowState != Constants.WorkflowStates.Removed))
                    {
                        property.ComplianceStatus = 2;
                        property.ComplianceStatusThirty = 2;
                        await property.Update(backendConfigurationPnDbContext);
                    }
                    else
                    {
                        if (!backendConfigurationPnDbContext.Compliances.AsNoTracking().Any(x =>
                                x.Deadline < DateTime.UtcNow.AddDays(30) && x.PropertyId == property.Id &&
                                x.WorkflowState != Constants.WorkflowStates.Removed))
                        {
                            property.ComplianceStatusThirty = 0;
                            await property.Update(backendConfigurationPnDbContext);
                        }

                        if (!backendConfigurationPnDbContext.Compliances.AsNoTracking().Any(x =>
                                x.Deadline < DateTime.UtcNow && x.PropertyId == property.Id &&
                                x.WorkflowState != Constants.WorkflowStates.Removed))
                        {
                            property.ComplianceStatus = 0;
                            await property.Update(backendConfigurationPnDbContext);
                        }
                    }
                }
            }
        }
    }
}