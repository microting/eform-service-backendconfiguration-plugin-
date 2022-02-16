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


namespace ServiceBackendConfigurationPlugin.Handlers
{
    using Infrastructure.Helpers;
    using Messages;
    using Microsoft.EntityFrameworkCore;
    using Microting.eForm.Infrastructure.Constants;
    using Microting.eForm.Infrastructure.Models;
    using Microting.eFormApi.BasePn.Infrastructure.Consts;
    using Microting.EformBackendConfigurationBase.Infrastructure.Data.Entities;
    using Microting.ItemsPlanningBase.Infrastructure.Enums;
    using Rebus.Handlers;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microting.EformBackendConfigurationBase.Infrastructure.Enum;
    using Resources;

    public class EFormCompletedHandler : IHandleMessages<eFormCompleted>
    {
        private readonly eFormCore.Core _sdkCore;
        private readonly ItemsPlanningDbContextHelper _itemsPlanningDbContextHelper;
        private readonly BackendConfigurationDbContextHelper _backendConfigurationDbContextHelper;

        public EFormCompletedHandler(eFormCore.Core sdkCore, ItemsPlanningDbContextHelper itemsPlanningDbContextHelper,
            BackendConfigurationDbContextHelper backendConfigurationDbContextHelper)
        {
            _sdkCore = sdkCore;
            _itemsPlanningDbContextHelper = itemsPlanningDbContextHelper;
            _backendConfigurationDbContextHelper = backendConfigurationDbContextHelper;
        }

        public async Task Handle(eFormCompleted message)
        {
            Console.WriteLine("EFormCompletedHandler .Handle called");
            Console.WriteLine($"message.CaseId: {message.CaseId}");
            Console.WriteLine($"message.MicrotingUId: {message.MicrotingUId}");
            Console.WriteLine($"message.CheckId: {message.CheckId}");
            Console.WriteLine($"message.SiteUId: {message.SiteUId}");
            await using var sdkDbContext = _sdkCore.DbContextHelper.GetDbContext();
            await using var
                itemsPlanningPnDbContext = _itemsPlanningDbContextHelper.GetDbContext();
            await using var backendConfigurationPnDbContext =
                _backendConfigurationDbContextHelper.GetDbContext();

            var eformIdForNewTasks = await sdkDbContext.CheckListTranslations
                .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                .Where(x => x.Text == "01. New task")
                .Select(x => x.CheckListId)
                .FirstAsync();

            var eformIdForOngoingTasks = await sdkDbContext.CheckListTranslations
                .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                .Where(x => x.Text == "02. Ongoing task")
                .Select(x => x.CheckListId)
                .FirstAsync();

            var eformIdForCompletedTasks = await sdkDbContext.CheckListTranslations
                .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                .Where(x => x.Text == "03. Completed task")
                .Select(x => x.CheckListId)
                .FirstAsync();

            var workorderCase = await backendConfigurationPnDbContext.WorkorderCases
                .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                .Where(x => x.CaseId == message.CaseId)
                .Include(x => x.ParentWorkorderCase)
                .Include(x => x.PropertyWorker)
                .ThenInclude(x => x.Property)
                .ThenInclude(x => x.PropertyWorkers)
                .ThenInclude(x => x.WorkorderCases)
                .FirstOrDefaultAsync();

            if (eformIdForNewTasks == message.CheckId && workorderCase != null)
            {
                var property = workorderCase.PropertyWorker.Property;

                var propertyWorkers = property.PropertyWorkers
                    .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                    .ToList();

                var folderIdForOngoingTasks = await sdkDbContext.Folders
                    .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                    .Where(x => x.ParentId == property.FolderIdForTasks)
                    .Where(x => x.FolderTranslations.Any(y => y.Name == "02. Ongoing tasks"))
                    .Select(x => x.Id)
                    .FirstAsync();

                var cls = await sdkDbContext.Cases
                    .Where(x => x.MicrotingUid == message.MicrotingUId)
                    .OrderBy(x => x.DoneAt)
                    .Include(x => x.Site)
                    .LastAsync();

                var language = await sdkDbContext.Languages.SingleOrDefaultAsync(x => x.Id == cls.Site.LanguageId) ??
                               await sdkDbContext.Languages.SingleOrDefaultAsync(x => x.LanguageCode == LocaleNames.Danish);

                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(language.LanguageCode);

                var fieldValues = await _sdkCore.Advanced_FieldValueReadList(new() { cls.Id }, language);

                var area = fieldValues.First().Value;
                var descriptionFromCase = fieldValues[2].Value;
                var createdBy = fieldValues[3].Value;
                var assignedTo = fieldValues[4].Value;

                var label = $"<strong>{Translations.Location}:</strong>{property.Name}<br>" +
                                  $"<strong>{Translations.AssignedTo}:</strong> {assignedTo}<br>" +
                                  (string.IsNullOrEmpty(area)
                                      ? $"<strong>{Translations.Area}:</strong> {area}<br>"
                                      : "") +
                                  $"<strong>{Translations.Description}:</strong> {descriptionFromCase}<br><br>" +
                                  $"<strong>{Translations.CreatedBy}:</strong> {assignedTo}<br>" +
                                  (string.IsNullOrEmpty(createdBy)
                                      ? $"<strong>{Translations.CreatedBy}:</strong> {createdBy}<br>"
                                      : "") +
                                  $"<strong>{Translations.CreatedDate}:</strong> {DateTime.UtcNow: dd.MM.yyyy}<br><br>" +
                                  $"<strong>{Translations.Status}:</strong> Ongoing;";

                var deviceUsersGroupUid = await sdkDbContext.EntityGroups
                    .Where(x => x.Id == property.EntitySelectListDeviceUsers)
                    .Select(x => x.MicrotingUid)
                    .FirstAsync();
                // deploy eform to ongoing status
                await DeployEform(propertyWorkers, eformIdForOngoingTasks, folderIdForOngoingTasks, label, CaseStatusesEnum.Ongoing, workorderCase.Id, int.Parse(deviceUsersGroupUid));
            }
            else if (eformIdForOngoingTasks == message.CheckId && workorderCase != null)
            {
                var property = workorderCase.PropertyWorker.Property;

                var propertyWorkers = property.PropertyWorkers
                    .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                    .ToList();

                var folderIdForOngoingTasks = await sdkDbContext.Folders
                    .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                    .Where(x => x.ParentId == property.FolderIdForTasks)
                    .Where(x => x.FolderTranslations.Any(y => y.Name == "02. Ongoing tasks"))
                    .Select(x => x.Id)
                    .FirstAsync();

                var folderIdForCompletedTasks = await sdkDbContext.Folders
                    .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                    .Where(x => x.ParentId == property.FolderIdForTasks)
                    .Where(x => x.FolderTranslations.Any(y => y.Name == "03. Completed tasks"))
                    .Select(x => x.Id)
                    .FirstAsync();

                var cls = await sdkDbContext.Cases
                    .Where(x => x.MicrotingUid == message.MicrotingUId)
                    .OrderBy(x => x.DoneAt)
                    .Include(x => x.Site)
                    .LastAsync();

                var language = await sdkDbContext.Languages.SingleOrDefaultAsync(x => x.Id == cls.Site.LanguageId) ??
                               await sdkDbContext.Languages.SingleOrDefaultAsync(x => x.LanguageCode == LocaleNames.Danish);

                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(language.LanguageCode);

                var fieldValues = await _sdkCore.Advanced_FieldValueReadList(new() { cls.Id }, language);

                var caseWithCreatedBy = await sdkDbContext.Cases
                    .Where(x => x.Id == workorderCase.ParentWorkorderCase.CaseId)
                    .OrderBy(x => x.DoneAt)
                    .Include(x => x.Site)
                    .FirstAsync();
                
                var fieldValuesWithCreatedBy = await _sdkCore.Advanced_FieldValueReadList(new() { caseWithCreatedBy.Id },
                    await sdkDbContext.Languages.SingleOrDefaultAsync(x => x.Id == caseWithCreatedBy.Site.LanguageId) ??
                    await sdkDbContext.Languages.SingleOrDefaultAsync(x => x.LanguageCode == LocaleNames.Danish));

                var area = fieldValues.First().Value;
                var descriptionFromCase = fieldValues[2].Value;
                var assignedTo = fieldValues[3].Value;
                var status = fieldValues[4].Value;
                var createdBy = fieldValuesWithCreatedBy[4].Value;

                var label = $"<strong>{Translations.Location}:</strong>{property.Name}<br>" +
                                  $"<strong>{Translations.AssignedTo}:</strong> {assignedTo}<br>" +
                                  (string.IsNullOrEmpty(area)
                                      ? $"<strong>{Translations.Area}:</strong> {area}<br>"
                                      : "") +
                                  $"<strong>{Translations.Description}:</strong> {descriptionFromCase}<br><br>" +
                                  $"<strong>{Translations.CreatedBy}:</strong> {assignedTo}<br>" +
                                  (string.IsNullOrEmpty(createdBy)
                                      ? $"<strong>{Translations.CreatedBy}:</strong> {createdBy}<br>"
                                      : "") +
                                  $"<strong>{Translations.CreatedDate}:</strong> {caseWithCreatedBy.DoneAt: dd.MM.yyyy}<br><br>" +
                                  $"<strong>{Translations.LastUpdatedBy}:</strong>{cls.Site.Name}<br>" +
                                  $"<strong>{Translations.LastUpdatedDate}:</strong>{DateTime.UtcNow: dd.MM.yyyy}<br><br>" +
                                  $"<strong>{Translations.Status}:</strong> {status};";
                var deviceUsersGroupUid = await sdkDbContext.EntityGroups
                    .Where(x => x.Id == property.EntitySelectListDeviceUsers)
                    .Select(x => x.MicrotingUid)
                    .FirstAsync();
                if (status == "Ongoing")
                {
                    // retract eform
                    await RetractEform(propertyWorkers, eformIdForOngoingTasks, (int)message.CaseId);
                    // deploy eform to ongoing status
                    await DeployEform(propertyWorkers, eformIdForOngoingTasks, folderIdForOngoingTasks, label, CaseStatusesEnum.Ongoing, (int)workorderCase.ParentWorkorderCaseId, int.Parse(deviceUsersGroupUid));
                }
                else
                {
                    // retract eform
                    await RetractEform(propertyWorkers, eformIdForOngoingTasks, (int)message.CaseId);
                    // deploy eform to completed status
                    await DeployEform(propertyWorkers, eformIdForCompletedTasks, folderIdForCompletedTasks, label, CaseStatusesEnum.Completed, (int)workorderCase.ParentWorkorderCaseId, null);
                }
            }
            else
            {
                var planningCaseSite =
                    await itemsPlanningPnDbContext.PlanningCaseSites.AsNoTracking()
                        .SingleOrDefaultAsync(x => x.MicrotingSdkCaseId == message.CaseId);

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

                var planning =
                    await itemsPlanningPnDbContext.Plannings.AsNoTracking()
                        .SingleAsync(x => x.Id == planningCaseSite.PlanningId);

                if (planning.RepeatType == RepeatType.Day && planning.RepeatEvery == 0)
                {
                }
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


                    var compliance = await backendConfigurationPnDbContext.Compliances
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
                        if (property is { ComplianceStatus: 0 })
                        {
                            property.ComplianceStatus = 1;
                            await property.Update(backendConfigurationPnDbContext);
                        }

                        if (property is { ComplianceStatusThirty: 0 })
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

        private async Task DeployEform(List<PropertyWorker> propertyWorkers, int eformId, int folderId, string description, CaseStatusesEnum status, int parentCaseId, int? deviceUsersGroupId)
        {
            await using var sdkDbContext = _sdkCore.DbContextHelper.GetDbContext();
            await using var backendConfigurationPnDbContext =
                _backendConfigurationDbContextHelper.GetDbContext();
            foreach (var propertyWorker in propertyWorkers)
            {
                var site = await sdkDbContext.Sites.SingleAsync(x => x.Id == propertyWorker.WorkerId);
                var siteLanguage = await sdkDbContext.Languages.SingleAsync(x => x.Id == site.LanguageId);
                var mainElement = await _sdkCore.ReadeForm(eformId, siteLanguage);
                mainElement.Repeated = 0;
                mainElement.CheckListFolderName = sdkDbContext.Folders.Single(x => x.Id == folderId)
                    .MicrotingUid.ToString();
                ((DataElement)mainElement.ElementList[0]).DataItemList[0].Description.InderValue = description;
                if (deviceUsersGroupId != null)
                {
                    ((EntitySelect)((DataElement)mainElement.ElementList[0]).DataItemList[4]).Source = (int)deviceUsersGroupId;
                }

                mainElement.EndDate = DateTime.Now.AddYears(10).ToUniversalTime();
                mainElement.StartDate = DateTime.Now.ToUniversalTime();
                var caseId = await _sdkCore.CaseCreate(mainElement, "", (int)site.MicrotingUid, folderId);
                await new WorkorderCase
                {
                    CaseId = (int)caseId,
                    PropertyWorkerId = propertyWorker.Id,
                    CaseStatusesEnum = status,
                    ParentWorkorderCaseId = parentCaseId,
                }.Create(backendConfigurationPnDbContext);
            }
        }

        private async Task RetractEform(List<PropertyWorker> propertyWorkers, int eformId, int caseId)
        {
            await using var sdkDbContext = _sdkCore.DbContextHelper.GetDbContext();
            await using var backendConfigurationPnDbContext =
                _backendConfigurationDbContextHelper.GetDbContext();
            foreach (var propertyWorker in propertyWorkers)
            {
                var site = await sdkDbContext.Sites.SingleAsync(x => x.Id == propertyWorker.WorkerId);
                await _sdkCore.CaseDelete(eformId, (int)site.MicrotingUid);
                var workorderCase = await backendConfigurationPnDbContext.WorkorderCases
                    .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                    .Where(x => x.PropertyWorkerId == propertyWorker.Id)
                    .Where(x => x.CaseId != caseId)
                    .FirstOrDefaultAsync();
                if(workorderCase != null)
                {
                    await workorderCase.Delete(backendConfigurationPnDbContext);
                }
            }
        }
    }
}