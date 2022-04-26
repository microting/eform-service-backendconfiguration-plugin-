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


using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Policy;
using ImageMagick;
using Microting.eForm.Helpers;

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
                .FirstOrDefaultAsync();

            if (eformIdForNewTasks == 0)
            {
                Console.WriteLine("eformIdForNewTasks is 0");
                return;
            }

            var eformIdForOngoingTasks = await sdkDbContext.CheckListTranslations
                .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                .Where(x => x.Text == "02. Ongoing task")
                .Select(x => x.CheckListId)
                .FirstOrDefaultAsync();

            if (eformIdForOngoingTasks == 0)
            {
                Console.WriteLine("eformIdForOngoingTasks is 0");
                return;
            }

            var eformIdForCompletedTasks = await sdkDbContext.CheckListTranslations
                .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                .Where(x => x.Text == "03. Completed task")
                .Select(x => x.CheckListId)
                .FirstOrDefaultAsync();

            if (eformIdForCompletedTasks == 0)
            {
                Console.WriteLine("eformIdForCompletedTasks is 0");
                return;
            }

            var workorderCase = await backendConfigurationPnDbContext.WorkorderCases
                .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                .Where(x => x.CaseId == message.MicrotingUId)
                .Include(x => x.ParentWorkorderCase)
                .Include(x => x.PropertyWorker)
                .ThenInclude(x => x.Property)
                .ThenInclude(x => x.PropertyWorkers)
                .ThenInclude(x => x.WorkorderCases)
                .FirstOrDefaultAsync();

            var dbCase = await sdkDbContext.Cases.AsNoTracking().SingleOrDefaultAsync(x => x.Id == message.CaseId) ?? await sdkDbContext.Cases.SingleOrDefaultAsync(x => x.MicrotingCheckUid == message.CheckId);

            if (eformIdForNewTasks == dbCase.CheckListId && workorderCase != null)
            {
                var property = workorderCase.PropertyWorker.Property;

                var propertyWorkers = property.PropertyWorkers
                    .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                    .ToList();

                var cls = await sdkDbContext.Cases
                    .Where(x => x.MicrotingUid == message.MicrotingUId)
                    .OrderBy(x => x.DoneAt)
                    .Include(x => x.Site)
                    .LastAsync();

                var language = await sdkDbContext.Languages.SingleOrDefaultAsync(x => x.Id == cls.Site.LanguageId) ??
                               await sdkDbContext.Languages.SingleOrDefaultAsync(x => x.LanguageCode == LocaleNames.Danish);

                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(language.LanguageCode);

                var areaField =
                    await sdkDbContext.Fields.SingleAsync(x => x.CheckListId == eformIdForNewTasks + 1 && x.DisplayIndex == 1);
                var areaFieldValue = await sdkDbContext.FieldValues.SingleAsync(x => x.FieldId == areaField.Id && x.CaseId == dbCase.Id);

                var pictureField =
                    await sdkDbContext.Fields.SingleAsync(x => x.CheckListId == eformIdForNewTasks + 1 && x.DisplayIndex == 2);
                var pictureFieldValues = await sdkDbContext.FieldValues.Where(x => x.FieldId == pictureField.Id && x.CaseId == dbCase.Id).ToListAsync();

                var commentField =
                    await sdkDbContext.Fields.SingleAsync(x => x.CheckListId == eformIdForNewTasks + 1 && x.DisplayIndex == 3);
                var commentFieldValue = await sdkDbContext.FieldValues.SingleAsync(x => x.FieldId == commentField.Id && x.CaseId == dbCase.Id);

                var assignToTexField =
                    await sdkDbContext.Fields.SingleAsync(x => x.CheckListId == eformIdForNewTasks + 1 && x.DisplayIndex == 4);
                var assignedToFieldValue = await sdkDbContext.FieldValues.SingleAsync(x => x.FieldId == assignToTexField.Id && x.CaseId == dbCase.Id);

                var assignToSelectField =
                    await sdkDbContext.Fields.SingleAsync(x => x.CheckListId == eformIdForNewTasks + 1 && x.DisplayIndex == 5);
                var assignedToSelectFieldValue = await sdkDbContext.FieldValues.SingleAsync(x => x.FieldId == assignToSelectField.Id && x.CaseId == dbCase.Id);

                var updatedByName = dbCase.Site.Name;
                // var fieldValues = await _sdkCore.Advanced_FieldValueReadList(new() { cls.Id }, language);

                var areasGroup = await sdkDbContext.EntityGroups
                    .SingleAsync(x => x.Id == property.EntitySelectListAreas);
                var deviceUsersGroup = await sdkDbContext.EntityGroups
                    .SingleAsync(x => x.Id == property.EntitySelectListDeviceUsers);

                var assignedTo = await sdkDbContext.EntityItems.SingleAsync(x => x.EntityGroupId == deviceUsersGroup.Id && x.Id == int.Parse(assignedToSelectFieldValue.Value));
                var areaName = "";
                if (!string.IsNullOrEmpty(areaFieldValue.Value) && areaFieldValue.Value != "null")
                {
                    var area = await sdkDbContext.EntityItems.SingleAsync(x => x.EntityGroupId == areasGroup.Id && x.Id == int.Parse(areaFieldValue.Value));
                    areaName = area.Name;
                    // workorderCase.EntityItemIdForArea = area.Id;
                }

                if (backendConfigurationPnDbContext.WorkorderCases.Any(x =>
                        x.ParentWorkorderCaseId == workorderCase.Id
                        && x.WorkflowState != Constants.WorkflowStates.Removed
                        && x.CaseId == dbCase.Id
                        && x.PropertyWorkerId == workorderCase.PropertyWorkerId
                        && x.SelectedAreaName == areaName
                        && x.CreatedByName == cls.Site.Name
                        && x.CreatedByText == assignedToFieldValue.Value
                        && x.CaseStatusesEnum == CaseStatusesEnum.Ongoing
                        && x.Description == commentFieldValue.Value))
                {
                    return;
                }

                var newWorkorderCase = new WorkorderCase
                {
                    ParentWorkorderCaseId = workorderCase.Id,
                    CaseId = dbCase.Id,
                    PropertyWorkerId = workorderCase.PropertyWorkerId,
                    SelectedAreaName = areaName,
                    CreatedByName = cls.Site.Name,
                    CreatedByText = assignedToFieldValue.Value,
                    CaseStatusesEnum = CaseStatusesEnum.Ongoing,
                    Description = commentFieldValue.Value,
                    CaseInitiated = DateTime.UtcNow
                };
                await newWorkorderCase.Create(backendConfigurationPnDbContext);

                var picturesOfTasks = new List<string>();
                foreach (var pictureFieldValue in pictureFieldValues)
                {
                    if (pictureFieldValue.UploadedDataId != null)
                    {
                        var uploadedData =
                            await sdkDbContext.UploadedDatas.SingleAsync(x => x.Id == pictureFieldValue.UploadedDataId);
                        var workOrderCaseImage = new WorkorderCaseImage
                        {
                            WorkorderCaseId = newWorkorderCase.Id,
                            UploadedDataId = (int) pictureFieldValue.UploadedDataId
                        };

                        picturesOfTasks.Add($"{uploadedData.Id}_700_{uploadedData.Checksum}{uploadedData.Extension}");
                        await workOrderCaseImage.Create(backendConfigurationPnDbContext);
                    }
                }

                var hash = await GeneratePdf(picturesOfTasks, (int)cls.SiteId);

                var label = $"<strong>{Translations.AssignedTo}:</strong> {assignedTo.Name}<br>" +
                            $"<strong>{Translations.Location}:</strong> {property.Name}<br>" +
                            (!string.IsNullOrEmpty(areaName)
                                ? $"<strong>{Translations.Area}:</strong> {areaName}<br>"
                                : "") +
                            $"<strong>{Translations.Description}:</strong> {commentFieldValue.Value}<br><br>" +
                            $"<strong>{Translations.CreatedBy}:</strong> {cls.Site.Name}<br>" +
                            (!string.IsNullOrEmpty(assignedToFieldValue.Value)
                                ? $"<strong>{Translations.CreatedBy}:</strong> {assignedToFieldValue.Value}<br>"
                                : "") +
                            $"<strong>{Translations.CreatedDate}:</strong> {newWorkorderCase.CaseInitiated: dd.MM.yyyy}<br><br>" +
                            $"<strong>{Translations.Status}:</strong> {Translations.Ongoing}<br><br>";

                var pushMessageTitle = !string.IsNullOrEmpty(areaName) ? $"{property.Name}; {areaName}" : $"{property.Name}";
                var pushMessageBody = $"{commentFieldValue.Value}";

                // deploy eform to ongoing status
                await DeployEform(propertyWorkers, eformIdForOngoingTasks, (int)property.FolderIdForOngoingTasks, label, CaseStatusesEnum.Ongoing, newWorkorderCase, commentFieldValue.Value, int.Parse(deviceUsersGroup.MicrotingUid), hash,
                    assignedTo.Name, pushMessageBody, pushMessageTitle, updatedByName);
            }
            else if (eformIdForOngoingTasks == dbCase.CheckListId && workorderCase != null)
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

                // var fieldValues = await _sdkCore.Advanced_FieldValueReadList(new() { cls.Id }, language);

                var deviceUsersGroup = await sdkDbContext.EntityGroups
                    .SingleAsync(x => x.Id == property.EntitySelectListDeviceUsers);

                var pictureField =
                    await sdkDbContext.Fields.SingleAsync(x => x.CheckListId == eformIdForOngoingTasks + 1 && x.DisplayIndex == 2);
                var pictureFieldValues = await sdkDbContext.FieldValues.Where(x => x.FieldId == pictureField.Id && x.CaseId == dbCase.Id).ToListAsync();

                var commentField =
                    await sdkDbContext.Fields.SingleAsync(x => x.CheckListId == eformIdForOngoingTasks + 1 && x.DisplayIndex == 3);
                var commentFieldValue = await sdkDbContext.FieldValues.SingleAsync(x => x.FieldId == commentField.Id && x.CaseId == dbCase.Id);

                var assignToSelectField =
                    await sdkDbContext.Fields.SingleAsync(x => x.CheckListId == eformIdForOngoingTasks + 1 && x.DisplayIndex == 4);
                var assignedToSelectFieldValue = await sdkDbContext.FieldValues.SingleAsync(x => x.FieldId == assignToSelectField.Id && x.CaseId == dbCase.Id);

                var statusField =
                    await sdkDbContext.Fields.SingleAsync(x => x.CheckListId == eformIdForOngoingTasks + 1 && x.DisplayIndex == 5);
                var statusFieldValue = await sdkDbContext.FieldValues.SingleAsync(x => x.FieldId == statusField.Id && x.CaseId == dbCase.Id);

                var assignedTo = await sdkDbContext.EntityItems.SingleAsync(x => x.EntityGroupId == deviceUsersGroup.Id && x.Id == int.Parse(assignedToSelectFieldValue.Value));
                // var area = await sdkDbContext.EntityItems.SingleAsync(x => x.EntityGroupId == areasGroup.Id && x.Id == int.Parse(areaId));
                var textStatus = statusFieldValue.Value == "1" ? Translations.Ongoing : Translations.Completed;

                var updatedByName = dbCase.Site.Name;

                var picturesOfTasks = new List<string>();
                foreach (var pictureFieldValue in pictureFieldValues)
                {
                    if (pictureFieldValue.UploadedDataId != null)
                    {
                        var uploadedData = await sdkDbContext.UploadedDatas.SingleOrDefaultAsync(x => x.Id == pictureFieldValue.UploadedDataId);
                        var workOrderCaseImage = new WorkorderCaseImage
                        {
                            WorkorderCaseId = workorderCase.Id,
                            UploadedDataId = (int) pictureFieldValue.UploadedDataId
                        };

                        picturesOfTasks.Add($"{uploadedData.Id}_700_{uploadedData.Checksum}{uploadedData.Extension}");
                        await workOrderCaseImage.Create(backendConfigurationPnDbContext);
                    }
                }
                var parentCaseImages = await backendConfigurationPnDbContext.WorkorderCaseImages.Where(x => x.WorkorderCaseId == workorderCase.ParentWorkorderCaseId).ToListAsync();

                foreach (var workorderCaseImage in parentCaseImages)
                {
                    var uploadedData = await sdkDbContext.UploadedDatas.SingleAsync(x => x.Id == workorderCaseImage.UploadedDataId);
                    picturesOfTasks.Add($"{uploadedData.Id}_700_{uploadedData.Checksum}{uploadedData.Extension}");
                    var workOrderCaseImage = new WorkorderCaseImage
                    {
                        WorkorderCaseId = workorderCase.Id,
                        UploadedDataId = (int)uploadedData.Id
                    };
                    await workOrderCaseImage.Create(backendConfigurationPnDbContext);
                }

                var hash = await GeneratePdf(picturesOfTasks, (int)cls.SiteId);

                var label = $"<strong>{Translations.AssignedTo}:</strong> {assignedTo.Name}<br>";

                var pushMessageTitle = !string.IsNullOrEmpty(workorderCase.SelectedAreaName) ? $"{property.Name}; {workorderCase.SelectedAreaName}" : $"{property.Name}";
                var pushMessageBody = $"{commentFieldValue.Value}";
                var deviceUsersGroupUid = await sdkDbContext.EntityGroups
                    .Where(x => x.Id == property.EntitySelectListDeviceUsers)
                    .Select(x => x.MicrotingUid)
                    .FirstAsync();
                if (textStatus == "Ongoing" || textStatus == "Igangværende" || textStatus == "1")
                {
                    label += $"<strong>{Translations.Location}:</strong> {property.Name}<br>" +
                             (!string.IsNullOrEmpty(workorderCase.SelectedAreaName)
                                 ? $"<strong>{Translations.Area}:</strong> {workorderCase.SelectedAreaName}<br>"
                                 : "") +
                             $"<strong>{Translations.Description}:</strong> {commentFieldValue.Value}<br><br>" +
                             $"<strong>{Translations.CreatedBy}:</strong> {workorderCase.CreatedByName}<br>" +
                             (!string.IsNullOrEmpty(workorderCase.CreatedByText)
                                 ? $"<strong>{Translations.CreatedBy}:</strong> {workorderCase.CreatedByText}<br>"
                                 : "") +
                             $"<strong>{Translations.CreatedDate}:</strong> {workorderCase.CaseInitiated: dd.MM.yyyy}<br><br>" +
                             $"<strong>{Translations.LastUpdatedBy}:</strong> {cls.Site.Name}<br>" +
                             $"<strong>{Translations.LastUpdatedDate}:</strong> {DateTime.UtcNow: dd.MM.yyyy}<br><br>" +
                             $"<strong>{Translations.Status}:</strong> {textStatus}<br><br>";
                    // retract eform
                    await RetractEform(workorderCase);
                    // deploy eform to ongoing status
                    await DeployEform(propertyWorkers, eformIdForOngoingTasks, folderIdForOngoingTasks, label, CaseStatusesEnum.Ongoing, workorderCase, commentFieldValue.Value, int.Parse(deviceUsersGroupUid), hash, assignedTo.Name, pushMessageBody, pushMessageTitle, updatedByName);
                }
                else
                {
                    label = $"<strong>{Translations.Location}:</strong> {property.Name}<br>" +
                            (!string.IsNullOrEmpty(workorderCase.SelectedAreaName)
                                ? $"<strong>{Translations.Area}:</strong> {workorderCase.SelectedAreaName}<br>"
                                : "") +
                            $"<strong>{Translations.Description}:</strong> {commentFieldValue.Value}<br><br>" +
                            $"<strong>{Translations.CreatedBy}:</strong> {workorderCase.CreatedByName}<br>" +
                            (!string.IsNullOrEmpty(workorderCase.CreatedByText)
                                ? $"<strong>{Translations.CreatedBy}:</strong> {workorderCase.CreatedByText}<br>"
                                : "") +
                            $"<strong>{Translations.CreatedDate}:</strong> {workorderCase.CaseInitiated: dd.MM.yyyy}<br><br>" +
                            $"<strong>{Translations.LastUpdatedBy}:</strong> {cls.Site.Name}<br>" +
                            $"<strong>{Translations.LastUpdatedDate}:</strong> {DateTime.UtcNow: dd.MM.yyyy}<br><br>" +
                            $"<strong>{Translations.Status}:</strong> {textStatus}<br><br>";
                    // retract eform
                    await RetractEform(workorderCase);
                    // deploy eform to completed status
                    await DeployEform(propertyWorkers, eformIdForCompletedTasks, folderIdForCompletedTasks, label, CaseStatusesEnum.Completed, workorderCase, commentFieldValue.Value, null, hash, assignedTo.Name, pushMessageBody, pushMessageTitle, updatedByName);
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
                    var checkListSite = await sdkDbContext.CheckListSites.AsNoTracking().SingleOrDefaultAsync(x =>
                        x.MicrotingUid == message.MicrotingUId).ConfigureAwait(false);
                    if (checkListSite == null)
                    {
                        return;
                    }
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
                                itemsPlanningPnDbContext.PlanningCaseSites.AsNoTracking().Single(x => x.Id == planningCaseSite.Id);
                        }
                    }

                    Console.WriteLine($"planningCaseSite {planningCaseSite.Id} is completed");
                    Thread.Sleep(10000);


                    var bla = ((DateTime) planning.NextExecutionTime).ToUniversalTime().AddDays(1);
                    // backendConfigurationPnDbContext.Database.Log = Console.Write;

                    var complianceList = await backendConfigurationPnDbContext.Compliances
                        .Where(x => x.Deadline == new DateTime(bla.Year, bla.Month, bla.Day, 0, 0, 0))
                        .AsNoTracking()
                        .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                        .Where(x => x.PlanningId == planningCaseSite.PlanningId).ToListAsync();

                    foreach (var compliance in complianceList)
                    {

                        if (compliance != null)
                        {
                            var dbCompliance =
                                await backendConfigurationPnDbContext.Compliances.SingleAsync(
                                    x => x.Id == compliance.Id);
                            await dbCompliance.Delete(backendConfigurationPnDbContext);
                        }

                        var backendPlanning = await backendConfigurationPnDbContext.AreaRulePlannings.AsNoTracking()
                            .Where(x => x.ItemPlanningId == planningCaseSite.PlanningId).FirstOrDefaultAsync();

                        var property =
                            await backendConfigurationPnDbContext.Properties.SingleOrDefaultAsync(x =>
                                x.Id == backendPlanning.PropertyId);

                        if (property == null)
                        {
                            return;
                        }

                        if (backendConfigurationPnDbContext.Compliances.AsNoTracking().Any(x =>
                                x.Deadline < DateTime.UtcNow && x.PropertyId == property.Id &&
                                x.WorkflowState != Constants.WorkflowStates.Removed))
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

                        // if (backendConfigurationPnDbContext.Compliances.AsNoTracking().Any(x =>
                        //         x.PropertyId == property.Id && x.WorkflowState != Constants.WorkflowStates.Removed))
                        // {
                        //     // if (property is { ComplianceStatus: 0 })
                        //     // {
                        //     //     // property.ComplianceStatus = 1;
                        //     //     // await property.Update(backendConfigurationPnDbContext);
                        //     // }
                        //     //
                        //     // if (property is { ComplianceStatusThirty: 0 })
                        //     // {
                        //     //     // if (backendConfigurationPnDbContext.Compliances.Any(x =>
                        //     //     //         x.Deadline < DateTime.UtcNow.AddDays(30) && x.PropertyId == property.Id &&
                        //     //     //         x.WorkflowState != Constants.WorkflowStates.Removed))
                        //     //     // {
                        //     //     //     property.ComplianceStatusThirty = 1;
                        //     //     //     await property.Update(backendConfigurationPnDbContext);
                        //     //     // }
                        //     // }
                        //     // else
                        //     // {
                        //
                        //     // }
                        // }
                        // else
                        // {
                        //     property.ComplianceStatus = 0;
                        //     property.ComplianceStatusThirty = 0;
                        //     await property.Update(backendConfigurationPnDbContext);
                        // }
                    }
                }
            }
        }

        private async Task DeployEform(
            List<PropertyWorker> propertyWorkers,
            int eformId,
            int folderId,
            string description,
            CaseStatusesEnum status,
            WorkorderCase workorderCase,
            string newDescription,
            int? deviceUsersGroupId,
            string pdfHash,
            string siteName,
            string pushMessageBody,
            string pushMessageTitle,
            string UpdatedByName)
        {
            await using var sdkDbContext = _sdkCore.DbContextHelper.GetDbContext();
            await using var backendConfigurationPnDbContext =
                _backendConfigurationDbContextHelper.GetDbContext();
            var i = 0;
            foreach (var propertyWorker in propertyWorkers)
            {
                var site = await sdkDbContext.Sites.SingleAsync(x => x.Id == propertyWorker.WorkerId);
                var siteLanguage = await sdkDbContext.Languages.SingleAsync(x => x.Id == site.LanguageId);
                var mainElement = await _sdkCore.ReadeForm(eformId, siteLanguage);
                mainElement.CheckListFolderName = sdkDbContext.Folders.Single(x => x.Id == folderId)
                    .MicrotingUid.ToString();
                mainElement.Label = " ";
                mainElement.ElementList[0].QuickSyncEnabled = true;
                mainElement.EnableQuickSync = true;
                mainElement.ElementList[0].Label = " ";
                mainElement.ElementList[0].Description.InderValue = description + "<center><strong>******************</strong></center>";
                if (status == CaseStatusesEnum.Completed || site.Name == siteName)
                {
                    DateTime startDate = new DateTime(2020, 1, 1);
                    mainElement.DisplayOrder = (int)(startDate - DateTime.UtcNow).TotalSeconds;
                }
                if (site.Name == siteName)
                {
                    mainElement.PushMessageTitle = pushMessageTitle;
                    mainElement.PushMessageBody = pushMessageBody;
                }
                // TODO uncomment when new app has been released.
                ((DataElement)mainElement.ElementList[0]).DataItemList[0].Description.InderValue = description;
                ((DataElement)mainElement.ElementList[0]).DataItemList[0].Label = " ";
                ((DataElement)mainElement.ElementList[0]).DataItemList[0].Color = Constants.FieldColors.Yellow;
                ((ShowPdf) ((DataElement) mainElement.ElementList[0]).DataItemList[1]).Value = pdfHash;
                if (deviceUsersGroupId != null)
                {
                    ((EntitySelect)((DataElement)mainElement.ElementList[0]).DataItemList[4]).Source = (int)deviceUsersGroupId;
                    ((EntitySelect)((DataElement)mainElement.ElementList[0]).DataItemList[4]).Mandatory = true;
                    ((Comment)((DataElement)mainElement.ElementList[0]).DataItemList[3]).Value = newDescription;
                    ((SingleSelect)((DataElement)mainElement.ElementList[0]).DataItemList[5]).Mandatory = true;
                    mainElement.EndDate = DateTime.Now.AddYears(10).ToUniversalTime();
                    mainElement.Repeated = 1;
                }
                else
                {
                    mainElement.EndDate = DateTime.Now.AddDays(30).ToUniversalTime();
                    mainElement.ElementList[0].DoneButtonEnabled = false;
                    mainElement.Repeated = 1;
                }

                mainElement.StartDate = DateTime.Now.ToUniversalTime();
                var caseId = await _sdkCore.CaseCreate(mainElement, "", (int)site.MicrotingUid, folderId);
                await new WorkorderCase
                {
                    CaseId = (int)caseId,
                    PropertyWorkerId = propertyWorker.Id,
                    CaseStatusesEnum = status,
                    ParentWorkorderCaseId = workorderCase.Id,
                    SelectedAreaName = workorderCase.SelectedAreaName,
                    CreatedByName = workorderCase.CreatedByName,
                    CreatedByText = workorderCase.CreatedByText,
                    Description = workorderCase.Description,
                    CaseInitiated = workorderCase.CaseInitiated,
                    LastAssignedToName = siteName,
                    LastUpdatedByName = UpdatedByName,
                    LeadingCase = i == 0
                }.Create(backendConfigurationPnDbContext);
                i++;
            }
        }

        private async Task RetractEform(WorkorderCase workOrderCase)
        {
            await using var sdkDbContext = _sdkCore.DbContextHelper.GetDbContext();
            await using var backendConfigurationPnDbContext =
                _backendConfigurationDbContextHelper.GetDbContext();

            var workOrdersToRetract = await backendConfigurationPnDbContext.WorkorderCases
                .Where(x => x.ParentWorkorderCaseId == workOrderCase.ParentWorkorderCaseId).ToListAsync();

            foreach (var theCase in workOrdersToRetract)
            {
                await _sdkCore.CaseDelete(theCase.CaseId);
                await theCase.Delete(backendConfigurationPnDbContext);
            }
        }

        private async Task<string> InsertImage(string imageName, string itemsHtml, int imageSize, int imageWidth, string basePicturePath)
        {
            var filePath = Path.Combine(basePicturePath, imageName);
            Stream stream;
            var storageResult = await _sdkCore.GetFileFromS3Storage(imageName);
            stream = storageResult.ResponseStream;

            using (var image = new MagickImage(stream))
            {
                var profile = image.GetExifProfile();
                // Write all values to the console
                try
                {
                    foreach (var value in profile.Values)
                    {
                        Console.WriteLine("{0}({1}): {2}", value.Tag, value.DataType, value.ToString());
                    }
                } catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                image.Rotate(90);
                var base64String = image.ToBase64();
                itemsHtml +=
                    $@"<p><img src=""data:image/png;base64,{base64String}"" width=""{imageWidth}px"" alt="""" /></p>";
            }

            await stream.DisposeAsync();

            return itemsHtml;
        }

        private async Task<string> GeneratePdf(List<string> picturesOfTasks, int sitId)
        {
            var resourceString = "ServiceBackendConfigurationPlugin.Resources.Templates.page.html";
                var assembly = Assembly.GetExecutingAssembly();
                string html;
                await using (var resourceStream = assembly.GetManifestResourceStream(resourceString))
                {
                    using var reader = new StreamReader(resourceStream ?? throw new InvalidOperationException($"{nameof(resourceStream)} is null"));
                    html = await reader.ReadToEndAsync();
                }

                // Read docx stream
                resourceString = "ServiceBackendConfigurationPlugin.Resources.Templates.file.docx";
                var docxFileResourceStream = assembly.GetManifestResourceStream(resourceString);
                if (docxFileResourceStream == null)
                {
                    throw new InvalidOperationException($"{nameof(docxFileResourceStream)} is null");
                }

                var docxFileStream = new MemoryStream();
                await docxFileResourceStream.CopyToAsync(docxFileStream);
                await docxFileResourceStream.DisposeAsync();
                string basePicturePath = Path.Combine(Path.GetTempPath(), "pictures", "workorders");
                Directory.CreateDirectory(basePicturePath);
                var word = new WordProcessor(docxFileStream);
                string imagesHtml = "";

                foreach (var imagesName in picturesOfTasks)
                {
                    Console.WriteLine($"Trying to insert image into document : {imagesName}");
                    imagesHtml = await InsertImage(imagesName, imagesHtml, 700, 650, basePicturePath);
                }

                html = html.Replace("{%Content%}", imagesHtml);

                word.AddHtml(html);
                word.Dispose();
                docxFileStream.Position = 0;

                // Build docx
                string downloadPath = Path.Combine(Path.GetTempPath(), "reports", "results");
                Directory.CreateDirectory(downloadPath);
                string timeStamp = DateTime.UtcNow.ToString("yyyyMMdd") + "_" + DateTime.UtcNow.ToString("hhmmss");
                string docxFileName = $"{timeStamp}{sitId}_temp.docx";
                string tempPDFFileName = $"{timeStamp}{sitId}_temp.pdf";
                string tempPDFFilePath = Path.Combine(downloadPath, tempPDFFileName);
                await using (var docxFile = new FileStream(Path.Combine(Path.GetTempPath(), "reports", "results", docxFileName), FileMode.Create, FileAccess.Write))
                {
                    docxFileStream.WriteTo(docxFile);
                }

                // Convert to PDF
                ReportHelper.ConvertToPdf(Path.Combine(Path.GetTempPath(), "reports", "results", docxFileName), downloadPath);
                File.Delete(docxFileName);

                // Upload PDF
                // string pdfFileName = null;
                string hash = await _sdkCore.PdfUpload(tempPDFFilePath);
                if (hash != null)
                {
                    //rename local file
                    FileInfo fileInfo = new FileInfo(tempPDFFilePath);
                    fileInfo.CopyTo(downloadPath + "/" + hash + ".pdf", true);
                    fileInfo.Delete();
                    await _sdkCore.PutFileToStorageSystem(Path.Combine(downloadPath, $"{hash}.pdf"), $"{hash}.pdf");

                    // TODO Remove from file storage?


                }

                return hash;
        }
    }
}