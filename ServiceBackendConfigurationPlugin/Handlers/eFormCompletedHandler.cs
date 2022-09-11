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

using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using ChemicalsBase.Infrastructure.Data.Entities;
using Microting.eForm.Dto;
using Microting.eForm.Infrastructure;
using Microting.eForm.Infrastructure.Data.Entities;
using Microting.ItemsPlanningBase.Infrastructure.Data.Entities;
using ServiceBackendConfigurationPlugin.Infrastructure.Models.AreaRules;

namespace ServiceBackendConfigurationPlugin.Handlers
{
    using ImageMagick;
    using Infrastructure.Helpers;
    using Messages;
    using Microsoft.EntityFrameworkCore;
    using Microting.eForm.Helpers;
    using Microting.eForm.Infrastructure.Constants;
    using Microting.eForm.Infrastructure.Models;
    using Microting.eFormApi.BasePn.Infrastructure.Consts;
    using Microting.EformBackendConfigurationBase.Infrastructure.Data.Entities;
    using Microting.EformBackendConfigurationBase.Infrastructure.Enum;
    using Microting.ItemsPlanningBase.Infrastructure.Enums;
    using Rebus.Handlers;
    using Resources;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    public class EFormCompletedHandler : IHandleMessages<eFormCompleted>
    {
        private readonly eFormCore.Core _sdkCore;
        private readonly ItemsPlanningDbContextHelper _itemsPlanningDbContextHelper;
        private readonly BackendConfigurationDbContextHelper _backendConfigurationDbContextHelper;
        private readonly ChemicalDbContextHelper _chemicalDbContextHelper;

        public EFormCompletedHandler(eFormCore.Core sdkCore, ItemsPlanningDbContextHelper itemsPlanningDbContextHelper,
            BackendConfigurationDbContextHelper backendConfigurationDbContextHelper, ChemicalDbContextHelper chemicalDbContextHelper)
        {
            _sdkCore = sdkCore;
            _itemsPlanningDbContextHelper = itemsPlanningDbContextHelper;
            _backendConfigurationDbContextHelper = backendConfigurationDbContextHelper;
            _chemicalDbContextHelper = chemicalDbContextHelper;
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

            var eformQuery = sdkDbContext.CheckListTranslations
                .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                .AsQueryable();

            var eformIdForNewTasks = await eformQuery
                .Where(x => x.Text == "01. New task")
                .Select(x => x.CheckListId)
                .FirstOrDefaultAsync();

            if (eformIdForNewTasks == 0)
            {
                Console.WriteLine("eformIdForNewTasks is 0");
                return;
            }

            var eformIdForOngoingTasks = await eformQuery
                .Where(x => x.Text == "02. Ongoing task")
                .Select(x => x.CheckListId)
                .FirstOrDefaultAsync();

            if (eformIdForOngoingTasks == 0)
            {
                Console.WriteLine("eformIdForOngoingTasks is 0");
                return;
            }

            var eformIdForCompletedTasks = await eformQuery
                .Where(x => x.Text == "03. Completed task")
                .Select(x => x.CheckListId)
                .FirstOrDefaultAsync();

            if (eformIdForCompletedTasks == 0)
            {
                Console.WriteLine("eformIdForCompletedTasks is 0");
                return;
            }

            var eformIdForControlFloatingLayer = await eformQuery
                .Where(x => x.Text == "03. Control floating layer")
                .Select(x => x.CheckListId)
                .FirstOrDefaultAsync();

            if (eformIdForControlFloatingLayer == 0)
            {
                Console.WriteLine("eformIdForControlFloatingLayer is 0");
                return;
            }

            var dbCase = await sdkDbContext.Cases
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == message.CaseId) ??
                         await sdkDbContext.Cases
                             .FirstOrDefaultAsync(x => x.MicrotingCheckUid == message.CheckId);
            if (dbCase == null)
            {
                Console.WriteLine("dbCase is null");
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

                var language = await sdkDbContext.Languages.FirstOrDefaultAsync(x => x.Id == cls.Site.LanguageId) ??
                               await sdkDbContext.Languages.FirstOrDefaultAsync(x => x.LanguageCode == LocaleNames.Danish);

                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(language.LanguageCode);

                var areaField =
                    await sdkDbContext.Fields.FirstAsync(x => x.CheckListId == eformIdForNewTasks + 1 && x.DisplayIndex == 1);
                var areaFieldValue = await sdkDbContext.FieldValues.FirstOrDefaultAsync(x => x.FieldId == areaField.Id && x.CaseId == dbCase.Id);

                var pictureField =
                    await sdkDbContext.Fields.FirstAsync(x => x.CheckListId == eformIdForNewTasks + 1 && x.DisplayIndex == 2);
                var pictureFieldValues = await sdkDbContext.FieldValues.Where(x => x.FieldId == pictureField.Id && x.CaseId == dbCase.Id).ToListAsync();

                var commentField =
                    await sdkDbContext.Fields.FirstAsync(x => x.CheckListId == eformIdForNewTasks + 1 && x.DisplayIndex == 3);
                var commentFieldValue = await sdkDbContext.FieldValues.FirstAsync(x => x.FieldId == commentField.Id && x.CaseId == dbCase.Id);

                var assignToTexField =
                    await sdkDbContext.Fields.FirstAsync(x => x.CheckListId == eformIdForNewTasks + 1 && x.DisplayIndex == 4);
                var assignedToFieldValue = await sdkDbContext.FieldValues.FirstAsync(x => x.FieldId == assignToTexField.Id && x.CaseId == dbCase.Id);

                var assignToSelectField =
                    await sdkDbContext.Fields.FirstAsync(x => x.CheckListId == eformIdForNewTasks + 1 && x.DisplayIndex == 5);
                var assignedToSelectFieldValue = await sdkDbContext.FieldValues.FirstAsync(x => x.FieldId == assignToSelectField.Id && x.CaseId == dbCase.Id);

                var site = await sdkDbContext.Sites.FirstAsync(x => x.Id == dbCase.SiteId);
                var updatedByName = site.Name;
                // var fieldValues = await _sdkCore.Advanced_FieldValueReadList(new() { cls.Id }, language);

                var areasGroup = await sdkDbContext.EntityGroups
                    .FirstAsync(x => x.Id == property.EntitySelectListAreas);
                var deviceUsersGroup = await sdkDbContext.EntityGroups
                    .FirstAsync(x => x.Id == property.EntitySelectListDeviceUsers);

                var assignedTo = await sdkDbContext.EntityItems.FirstAsync(x => x.EntityGroupId == deviceUsersGroup.Id && x.Id == int.Parse(assignedToSelectFieldValue.Value));
                var areaName = "";
                if (areaFieldValue != null)
                {
                    if (!string.IsNullOrEmpty(areaFieldValue!.Value) && areaFieldValue!.Value != "null")
                    {
                        var area = await sdkDbContext.EntityItems.FirstAsync(x =>
                            x.EntityGroupId == areasGroup.Id && x.Id == int.Parse(areaFieldValue.Value));
                        areaName = area.Name;
                        // workorderCase.EntityItemIdForArea = area.Id;
                    }
                }
                if (backendConfigurationPnDbContext.WorkorderCases.Any(x =>
                        x.ParentWorkorderCaseId == workorderCase.Id
                        && x.WorkflowState != Constants.WorkflowStates.Removed
                        && x.CaseId == dbCase.MicrotingUid
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
                    CaseId = 0,
                    PropertyWorkerId = workorderCase.PropertyWorkerId,
                    SelectedAreaName = areaName,
                    CreatedByName = cls.Site.Name,
                    CreatedByText = assignedToFieldValue.Value,
                    CaseStatusesEnum = CaseStatusesEnum.Ongoing,
                    Description = commentFieldValue.Value,
                    CaseInitiated = DateTime.UtcNow,
                    LeadingCase = false
                };
                await newWorkorderCase.Create(backendConfigurationPnDbContext);

                var picturesOfTasks = new List<string>();
                foreach (var pictureFieldValue in pictureFieldValues.Where(pictureFieldValue => pictureFieldValue.UploadedDataId != null))
                {
                    var uploadedData =
                        await sdkDbContext.UploadedDatas.FirstAsync(x => x.Id == pictureFieldValue.UploadedDataId);
                    var workOrderCaseImage = new WorkorderCaseImage
                    {
                        WorkorderCaseId = newWorkorderCase.Id,
                        UploadedDataId = (int) pictureFieldValue.UploadedDataId!
                    };

                    picturesOfTasks.Add($"{uploadedData.Id}_700_{uploadedData.Checksum}{uploadedData.Extension}");
                    await workOrderCaseImage.Create(backendConfigurationPnDbContext);
                }

                var hash = await GeneratePdf(picturesOfTasks, (int)cls.SiteId!);

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
                await DeployWorkOrderEform(propertyWorkers, eformIdForOngoingTasks, (int)property.FolderIdForOngoingTasks, label, CaseStatusesEnum.Ongoing, newWorkorderCase, commentFieldValue.Value, int.Parse(deviceUsersGroup.MicrotingUid), hash,
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

                var language = await sdkDbContext.Languages.FirstOrDefaultAsync(x => x.Id == cls.Site.LanguageId) ??
                               await sdkDbContext.Languages.FirstOrDefaultAsync(x => x.LanguageCode == LocaleNames.Danish);

                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(language.LanguageCode);

                // var fieldValues = await _sdkCore.Advanced_FieldValueReadList(new() { cls.Id }, language);

                var deviceUsersGroup = await sdkDbContext.EntityGroups
                    .FirstAsync(x => x.Id == property.EntitySelectListDeviceUsers);

                var pictureField =
                    await sdkDbContext.Fields.FirstAsync(x => x.CheckListId == eformIdForOngoingTasks + 1 && x.DisplayIndex == 2);
                var pictureFieldValues = await sdkDbContext.FieldValues.Where(x => x.FieldId == pictureField.Id && x.CaseId == dbCase.Id).ToListAsync();

                var commentField =
                    await sdkDbContext.Fields.FirstAsync(x => x.CheckListId == eformIdForOngoingTasks + 1 && x.DisplayIndex == 3);
                var commentFieldValue = await sdkDbContext.FieldValues.FirstAsync(x => x.FieldId == commentField.Id && x.CaseId == dbCase.Id);

                var assignToSelectField =
                    await sdkDbContext.Fields.FirstAsync(x => x.CheckListId == eformIdForOngoingTasks + 1 && x.DisplayIndex == 4);
                var assignedToSelectFieldValue = await sdkDbContext.FieldValues.FirstAsync(x => x.FieldId == assignToSelectField.Id && x.CaseId == dbCase.Id);

                var statusField =
                    await sdkDbContext.Fields.FirstAsync(x => x.CheckListId == eformIdForOngoingTasks + 1 && x.DisplayIndex == 5);
                var statusFieldValue = await sdkDbContext.FieldValues.FirstAsync(x => x.FieldId == statusField.Id && x.CaseId == dbCase.Id);

                var assignedTo = await sdkDbContext.EntityItems.FirstAsync(x => x.EntityGroupId == deviceUsersGroup.Id && x.Id == int.Parse(assignedToSelectFieldValue.Value));
                // var area = await sdkDbContext.EntityItems.FirstAsync(x => x.EntityGroupId == areasGroup.Id && x.Id == int.Parse(areaId));
                var textStatus = statusFieldValue.Value == "1" ? Translations.Ongoing : Translations.Completed;

                var site = await sdkDbContext.Sites.FirstAsync(x => x.Id == dbCase.SiteId);
                var updatedByName = site.Name;

                var picturesOfTasks = new List<string>();
                foreach (var pictureFieldValue in pictureFieldValues)
                {
                    if (pictureFieldValue.UploadedDataId != null)
                    {
                        var uploadedData = await sdkDbContext.UploadedDatas.FirstAsync(x => x.Id == pictureFieldValue.UploadedDataId);
                        var workOrderCaseImage = new WorkorderCaseImage
                        {
                            WorkorderCaseId = workorderCase.Id,
                            UploadedDataId = (int) pictureFieldValue.UploadedDataId!
                        };

                        picturesOfTasks.Add($"{uploadedData.Id}_700_{uploadedData.Checksum}{uploadedData.Extension}");
                        await workOrderCaseImage.Create(backendConfigurationPnDbContext);
                    }
                }
                var parentCaseImages = await backendConfigurationPnDbContext.WorkorderCaseImages.Where(x => x.WorkorderCaseId == workorderCase.ParentWorkorderCaseId).ToListAsync();

                foreach (var workorderCaseImage in parentCaseImages)
                {
                    var uploadedData = await sdkDbContext.UploadedDatas.FirstAsync(x => x.Id == workorderCaseImage.UploadedDataId);
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
                    await DeployWorkOrderEform(propertyWorkers, eformIdForOngoingTasks, folderIdForOngoingTasks, label, CaseStatusesEnum.Ongoing, workorderCase, commentFieldValue.Value, int.Parse(deviceUsersGroupUid), hash, assignedTo.Name, pushMessageBody, pushMessageTitle, updatedByName);
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
                    await DeployWorkOrderEform(propertyWorkers, eformIdForCompletedTasks, folderIdForCompletedTasks, label, CaseStatusesEnum.Completed, workorderCase, commentFieldValue.Value, null, hash, assignedTo.Name, pushMessageBody, pushMessageTitle, updatedByName);
                }
            }
            else
            {
                var planningCaseSite =
                    await itemsPlanningPnDbContext.PlanningCaseSites.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.MicrotingSdkCaseId == dbCase.Id);

                if (planningCaseSite == null)
                {
                    // var site = await sdkDbContext.Sites.FirstOrDefaultAsync(x => x.MicrotingUid == caseDto.SiteUId);
                    var checkListSite = await sdkDbContext.CheckListSites.AsNoTracking().FirstOrDefaultAsync(x =>
                        x.MicrotingUid == message.MicrotingUId).ConfigureAwait(false);
                    if (checkListSite == null)
                    {
                        return;
                    }
                    planningCaseSite =
                        await itemsPlanningPnDbContext.PlanningCaseSites.AsNoTracking().FirstOrDefaultAsync(x =>
                            x.MicrotingCheckListSitId == checkListSite.Id).ConfigureAwait(false);
                }

                if (planningCaseSite == null)
                {
                    Console.WriteLine($"planningCaseSite is null for CheckId: {message.CheckId}");
                    return;
                }

                var planning =
                    await itemsPlanningPnDbContext.Plannings.AsNoTracking()
                        .FirstAsync(x => x.Id == planningCaseSite.PlanningId);

                if (planning.RepeatType == RepeatType.Day && planning.RepeatEvery == 0)
                {
                    var areaRulePlanning = await
                        backendConfigurationPnDbContext.AreaRulePlannings.FirstOrDefaultAsync(x =>
                            x.ItemPlanningId == planning.Id);
                    var checkListTranslation = await sdkDbContext.CheckListTranslations.FirstAsync(x =>
                        x.Text == "25.01 Registrer produkter" && x.WorkflowState != Constants.WorkflowStates.Removed);
                    var areaRule =
                        await backendConfigurationPnDbContext.AreaRules.Where(x =>
                                x.Id == areaRulePlanning.AreaRuleId)
                            .Include(x => x.Area)
                            .Include(x => x.Property)
                            .Include(x => x.AreaRuleTranslations)
                            .FirstAsync();
                    var planningSites = await itemsPlanningPnDbContext.PlanningSites
                        .Where(x => x.PlanningId == planning.Id).ToListAsync();

                    var sdkSite = await sdkDbContext.Sites.FirstAsync(x => x.Id == planningSites.First().SiteId);
                    var language = await sdkDbContext.Languages.FirstAsync(x => x.Id == sdkSite.LanguageId);
                    var caseIds = new List<int>() {dbCase.Id};
                    var fieldValues = await _sdkCore.Advanced_FieldValueReadList(caseIds, language);
                    var chemicalDbContext = _chemicalDbContextHelper.GetDbContext();
                    var folder = await sdkDbContext.Folders.FirstAsync(x => x.Id == areaRule.FolderId);
                    if (planningCaseSite.MicrotingSdkeFormId == checkListTranslation.CheckListId)
                    {
                        List<string> entityIds = fieldValues.Where(x=> x.Value != "null" && x.Value != null && x.FieldType == Constants.FieldTypes.EntitySearch).Select(x => x.Value).ToList();
                        var location =
                            fieldValues.FirstOrDefault(x => x.ValueReadable != "null" && x.ValueReadable != null && x.FieldType == Constants.FieldTypes.EntitySelect);
                            // .Select(x => x.ValueReadable).ToList();


                        // List<string> barcodes = new List<string>();
                        // List<string> regNos = new List<string>();

                        List<Chemical> chemicals = new List<Chemical>();

                        checkListTranslation = await sdkDbContext.CheckListTranslations.FirstAsync(x =>
                            x.Text == "25.02 Vis kemisk produkt");

                        foreach (string entityId in entityIds)
                        {
                            var entityItem = await sdkDbContext.EntityItems.FirstAsync(x => x.Id == int.Parse(entityId));
                            Chemical chemical = null;
                            Product product = null;
                            if (entityItem.Name.Contains("-"))
                            {
                                chemical =
                                    await chemicalDbContext.Chemicals
                                        .Include(x=> x.ClassificationAndLabeling)
                                        .Include(x=> x.ClassificationAndLabeling.CLP)
                                        .Include(x=> x.ClassificationAndLabeling.CLP.HazardStatements)
                                        .Include(x=> x.ClassificationAndLabeling.DPD)
                                        .Include(x=> x.AuthorisationHolder)
                                        .Include(x=> x.AuthorisationHolder.Address)
                                        // .Include(x=> x.ClassificationAndLabeling.DPD.RiskPhrases)
                                        .FirstAsync(x => x.RegistrationNo == entityItem.Name);
                                product = await chemicalDbContext.Products.FirstOrDefaultAsync(x =>
                                    x.ChemicalId == chemical.Id);
                                // chemicals.Add(chemical);
                                // regNos.Add(entityItem.Name);
                            }
                            else
                            {
                                product =
                                    await chemicalDbContext.Products.FirstOrDefaultAsync(x => x.Barcode == entityItem.Name && x.WorkflowState != Constants.WorkflowStates.Removed);
                                chemical = await chemicalDbContext.Chemicals
                                    .Include(x=> x.ClassificationAndLabeling)
                                    .Include(x=> x.ClassificationAndLabeling.CLP)
                                    .Include(x=> x.ClassificationAndLabeling.CLP.HazardStatements)
                                    .Include(x=> x.ClassificationAndLabeling.DPD)
                                    // .Include(x=> x.ClassificationAndLabeling.DPD.RiskPhrases)
                                    .FirstAsync(x => x.Id == product.ChemicalId);
                                // chemicals.Add(chemical);
                            }

                            string folderLookUpName = "25.07 Udløber om mere end 12 mdr.";
                            var expireDate = chemical.UseAndPossesionDeadline ?? chemical.AuthorisationExpirationDate;
                            if (expireDate <= DateTime.UtcNow)
                            {
                                folderLookUpName = "25.02 Udløber i dag eller er udløbet";
                            }
                            else if (expireDate <= DateTime.UtcNow.AddMonths(1))
                            {
                                folderLookUpName = "25.03 Udløber om senest 1 mdr.";
                            }
                            else if (expireDate <= DateTime.UtcNow.AddMonths(3))
                            {
                                folderLookUpName = "25.04 Udløber om senest 3 mdr.";
                            } else if (expireDate <= DateTime.UtcNow.AddMonths(6))
                            {
                                folderLookUpName = "25.05 Udløber om senest 6 mdr.";
                            } else if (expireDate <= DateTime.UtcNow.AddMonths(12))
                            {
                                folderLookUpName = "25.06 Udløber om senest 12 mdr.";
                            }

                            var folderTranslation = await sdkDbContext.Folders.Join(sdkDbContext.FolderTranslations,
                                f => f.Id, translation => translation.FolderId, (f, translation) => new
                                {
                                    f.Id,
                                    f.ParentId,
                                    translation.Name,
                                    f.MicrotingUid
                                }).FirstAsync(x => x.Name == folderLookUpName && x.ParentId == folder.Id);
                            var folderMicrotingId = folderTranslation.MicrotingUid.ToString();

                            if (!backendConfigurationPnDbContext.ChemicalProductProperties.Any(x =>
                            x.ChemicalId == chemical.Id
                            && x.WorkflowState != Constants.WorkflowStates.Removed
                            && x.Locations.Contains(location.ValueReadable)
                            && x.PropertyId == areaRule.PropertyId))
                            {
                                var currentDeployment = await backendConfigurationPnDbContext.ChemicalProductProperties
                                    .FirstOrDefaultAsync(x =>
                                        x.ChemicalId == chemical.Id &&
                                        x.WorkflowState != Constants.WorkflowStates.Removed
                                        && x.PropertyId == areaRule.PropertyId);
                                if (currentDeployment != null)
                                {
                                    // var theCheckListSite = await sdkDbContext.CheckListSites.AsNoTracking().FirstAsync(x => x.Id == currentDeployment.SdkCaseId);
                                    await _sdkCore.CaseDelete(currentDeployment.SdkCaseId);
                                    await currentDeployment!.Delete(backendConfigurationPnDbContext);
                                }

                                var chemicalProductPropertySites =
                                    await backendConfigurationPnDbContext.ChemicalProductPropertieSites
                                        .Where(x => x.PropertyId == areaRule.PropertyId)
                                        .Where(x => x.ChemicalId == chemical.Id)
                                        .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed).ToListAsync();
                                foreach (var chemicalProductPropertySite in chemicalProductPropertySites)
                                {
                                    // var checkListSite = await sdkDbContext.CheckListSites.AsNoTracking().FirstAsync(x => x.Id == chemicalProductPropertySite.SdkCaseId);
                                    await _sdkCore.CaseDelete(chemicalProductPropertySite.SdkCaseId);
                                    await chemicalProductPropertySite.Delete(backendConfigurationPnDbContext);
                                }

                                var productName = chemical.Name;
                                if (product != null)
                                {
                                    if (product.Name != "Emballagestørrelse ikke angivet")
                                    {
                                        productName += " - " + product.Name;
                                    }
                                }

                                List<Microting.eForm.Dto.KeyValuePair> options =
                                    new List<Microting.eForm.Dto.KeyValuePair>();
                                int j = 0;
                                var totalLocations = string.Empty;
                                foreach (var s in location!.ValueReadable.Split("|"))
                                {
                                    Microting.eForm.Dto.KeyValuePair keyValuePair =
                                        new Microting.eForm.Dto.KeyValuePair(j.ToString(), s, false, j.ToString());
                                    options.Add(keyValuePair);
                                    totalLocations = s;
                                    j++;
                                }
                                if (currentDeployment != null)
                                {
                                    foreach (var s in currentDeployment.Locations.Split("|"))
                                    {
                                        Microting.eForm.Dto.KeyValuePair keyValuePair =
                                            new Microting.eForm.Dto.KeyValuePair(j.ToString(), s, false, j.ToString());
                                        options.Add(keyValuePair);
                                        totalLocations += "|" + s;
                                        j++;
                                    }
                                }

                                var mainElement = await _sdkCore.ReadeForm(checkListTranslation.CheckListId, language);
                                mainElement = await ModifyChemicalMainElement(mainElement, chemical, product,
                                    productName, folderMicrotingId, areaRule, sdkSite, totalLocations.Replace("|", ", ")).ConfigureAwait(false);

                                MultiSelect multiSelect = new MultiSelect(0, false, false,
                                    "Vælg rum som produktet er fjernet fra", " ", Constants.FieldColors.Red, -1,
                                    false, options);
                                ((FieldContainer) ((DataElement) mainElement.ElementList[0]).DataItemGroupList[1]).DataItemList.Add(multiSelect);

                                if (string.IsNullOrEmpty(chemical.Use))
                                {
                                    ((DataElement) mainElement.ElementList[0]).DataItemGroupList
                                        .RemoveAt(0);
                                }

                                var caseId = await _sdkCore.CaseCreate(mainElement, "", (int) sdkSite.MicrotingUid,
                                    folder.Id);
                                var thisDbCase = await sdkDbContext.CheckListSites.AsNoTracking().FirstAsync(x => x.MicrotingUid == caseId);

                                var propertySites = await backendConfigurationPnDbContext.PropertyWorkers
                                    .Where(x => x.PropertyId == areaRule.PropertyId).ToListAsync();

                                // This is repeated since now we are deploying the eForm to consumers and they should not see the remove product part.
                                if (string.IsNullOrEmpty(chemical.Use))
                                {
                                    ((DataElement) mainElement.ElementList[0]).DataItemGroupList
                                        .RemoveAt(0);
                                }
                                else
                                {
                                    ((DataElement) mainElement.ElementList[0]).DataItemGroupList
                                        .RemoveAt(1);
                                }

                                int i = 0;
                                foreach (PropertyWorker propertyWorker in propertySites)
                                {
                                    if (propertyWorker.WorkerId != sdkSite.Id)
                                    {
                                        var site = await
                                            sdkDbContext.Sites.FirstOrDefaultAsync(x => x.Id == propertyWorker.WorkerId);
                                        if (i == 0)
                                        {
                                            var list = ((DataElement) mainElement.ElementList[0]).DataItemGroupList[1]
                                                .DataItemList;
                                            list.RemoveAt(0);
                                            list.RemoveAt(0);
                                        }
                                        var siteCaseId = await _sdkCore.CaseCreate(mainElement, "", (int) site!.MicrotingUid!,
                                            folder.Id);
                                        // var siteDbCaseId =
                                        //     await sdkDbContext.Cases.FirstAsync(x => x.MicrotingUid == siteCaseId);
                                        var chemicalProductPropertySite = new ChemicalProductPropertySite()
                                        {
                                            ChemicalId = chemical.Id,
                                            SdkCaseId = (int)siteCaseId!,
                                            SdkSiteId = site!.Id,
                                            PropertyId = areaRule.PropertyId
                                        };
                                        await chemicalProductPropertySite.Create(backendConfigurationPnDbContext);
                                    }

                                    i++;
                                }

                                AreaRulePlanningModel areaRulePlanningModel = new AreaRulePlanningModel
                                {
                                    Status = true,
                                    AssignedSites = new List<AreaRuleAssignedSitesModel>
                                    {
                                        new()
                                        {
                                            Checked = true,
                                            SiteId = sdkSite.Id
                                        }
                                    },
                                    SendNotifications = false,
                                    StartDate = DateTime.UtcNow,
                                    PropertyId = areaRule.PropertyId,
                                    ComplianceEnabled = false,
                                    TypeSpecificFields = null,
                                    RuleId = areaRule.Id
                                };

                                var newPlanning = await CreateItemPlanningObject(checkListTranslation.CheckListId,
                                    areaRule.EformName,
                                    areaRule.FolderId, areaRulePlanningModel, areaRule);
                                newPlanning.RepeatEvery = 0;
                                newPlanning.RepeatType = RepeatType.Day;
                                newPlanning.StartDate = DateTime.Now.ToUniversalTime();
                                var now = DateTime.UtcNow;
                                newPlanning.LastExecutedTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
                                await newPlanning.Create(itemsPlanningPnDbContext);
                                var newPlanningCase = new PlanningCase
                                {
                                    PlanningId = newPlanning.Id,
                                    Status = 66,
                                    MicrotingSdkeFormId = checkListTranslation.CheckListId
                                };
                                await newPlanningCase.Create(itemsPlanningPnDbContext);
                                var newPlanningCaseSite = new PlanningCaseSite
                                {
                                    MicrotingSdkSiteId = sdkSite.Id,
                                    MicrotingSdkeFormId = checkListTranslation.CheckListId,
                                    Status = 66,
                                    PlanningId = newPlanning.Id,
                                    PlanningCaseId = newPlanningCase.Id,
                                    MicrotingSdkCaseId = (int) caseId,
                                    MicrotingCheckListSitId = thisDbCase.Id
                                };

                                await newPlanningCaseSite.Create(itemsPlanningPnDbContext);

                                var newAreaRulePlanning = CreateAreaRulePlanningObject(areaRulePlanningModel, areaRule,
                                    newPlanning.Id,
                                    areaRule.FolderId);


                                await newAreaRulePlanning.Create(backendConfigurationPnDbContext);
                                ChemicalProductProperty chemicalProductProperty = new ChemicalProductProperty()
                                {
                                    ChemicalId = chemical.Id,
                                    PropertyId = areaRule.PropertyId,
                                    SdkCaseId = (int) caseId,
                                    Locations = totalLocations,
                                    LanguageId = language.Id,
                                    SdkSiteId = (int) sdkSite.MicrotingUid,
                                    ExpireDate = chemical.UseAndPossesionDeadline ?? chemical.AuthorisationExpirationDate
                                };

                                await chemicalProductProperty.Create(backendConfigurationPnDbContext);
                            }
                        }



                        // foreach (Chemical chemical in chemicals)
                        // {

                        // }
                    }
                    else
                    {
                        checkListTranslation = await sdkDbContext.CheckListTranslations.FirstAsync(x =>
                            x.Text == "25.02 Vis kemisk produkt");
                        if (planningCaseSite.MicrotingSdkeFormId == checkListTranslation.CheckListId)
                        {
                            if (fieldValues.Any(x =>
                                    x.FieldType == Constants.FieldTypes.CheckBox && x.Value == "checked"))
                            {
                                var indexesToRemove = fieldValues.First(x => x.FieldId == 0).Value.Split("|").ToList();
                                var cpp = await backendConfigurationPnDbContext.ChemicalProductProperties
                                    .FirstAsync(x =>
                                        x.SdkCaseId == dbCase.MicrotingUid);
                                var currentLocations = cpp.Locations.Split("|").ToList();
                                var locationsToBeRemoved = new List<string>();

                                if (currentLocations.Count == indexesToRemove.Count)
                                {
                                    cpp.Locations = "";
                                }
                                else
                                {

                                    foreach (var index in indexesToRemove)
                                    {
                                        var location = currentLocations[int.Parse(index)];
                                        locationsToBeRemoved.Add(location);
                                    }

                                    foreach (var s in locationsToBeRemoved)
                                    {
                                        cpp.Locations = cpp.Locations.Replace(s, "").Replace("||", "|");
                                    }

                                    if (cpp.Locations.StartsWith("|"))
                                    {
                                        cpp.Locations = cpp.Locations.Substring(1, cpp.Locations.Length - 1);
                                    }

                                    if (cpp.Locations.EndsWith("|"))
                                    {
                                        cpp.Locations = cpp.Locations.Substring(0, cpp.Locations.Length - 1);
                                    }

                                }

                                if (cpp.Locations == "")
                                {
                                    await _sdkCore.CaseDelete(cpp.SdkCaseId);
                                    await cpp.Delete(backendConfigurationPnDbContext);
                                }
                                else
                                {
                                    await _sdkCore.CaseDelete(cpp.SdkCaseId);
                                    await cpp.Delete(backendConfigurationPnDbContext);
                                    var chemical = await chemicalDbContext.Chemicals
                                        .Include(x=> x.ClassificationAndLabeling)
                                        .Include(x=> x.ClassificationAndLabeling.CLP)
                                        .Include(x=> x.ClassificationAndLabeling.CLP.HazardStatements)
                                        .Include(x=> x.ClassificationAndLabeling.DPD)
                                        .Include(x=> x.AuthorisationHolder)
                                        .Include(x=> x.AuthorisationHolder.Address)
                                        // .Include(x=> x.ClassificationAndLabeling.DPD.RiskPhrases)
                                        .FirstAsync(x => x.Id == cpp.ChemicalId);
                                    var product = await chemicalDbContext.Products.FirstOrDefaultAsync(x =>
                                        x.ChemicalId == chemical.Id);
                                    var productName = chemical.Name;
                                    if (product != null)
                                    {
                                        if (product!.Name != "Emballagestørrelse ikke angivet")
                                        {
                                            productName += " - " + product!.Name;
                                        }
                                    }

                                    string folderLookUpName = "25.07 Udløber om mere end 12 mdr.";
                                    var expireDate = chemical.UseAndPossesionDeadline ?? chemical.AuthorisationExpirationDate;
                                    if (expireDate <= DateTime.UtcNow)
                                    {
                                        folderLookUpName = "25.02 Udløber i dag eller er udløbet";
                                    }
                                    else if (expireDate <= DateTime.UtcNow.AddMonths(1))
                                    {
                                        folderLookUpName = "25.03 Udløber om senest 1 mdr.";
                                    }
                                    else if (expireDate <= DateTime.UtcNow.AddMonths(3))
                                    {
                                        folderLookUpName = "25.04 Udløber om senest 3 mdr.";
                                    } else if (expireDate <= DateTime.UtcNow.AddMonths(6))
                                    {
                                        folderLookUpName = "25.05 Udløber om senest 6 mdr.";
                                    } else if (expireDate <= DateTime.UtcNow.AddMonths(12))
                                    {
                                        folderLookUpName = "25.06 Udløber om senest 12 mdr.";
                                    }

                                    var folderTranslation = await sdkDbContext.Folders.Join(sdkDbContext.FolderTranslations,
                                        f => f.Id, translation => translation.FolderId, (f, translation) => new
                                        {
                                            f.Id,
                                            f.ParentId,
                                            translation.Name,
                                            f.MicrotingUid
                                        }).FirstAsync(x => x.Name == folderLookUpName && x.ParentId == folder.Id);
                                    var folderMicrotingId = folderTranslation.MicrotingUid.ToString();
                                    var mainElement = await _sdkCore.ReadeForm(checkListTranslation.CheckListId, language);
                                    mainElement = await ModifyChemicalMainElement(mainElement, chemical, product,
                                        productName, folderMicrotingId, areaRule, sdkSite, cpp.Locations);
                                    List<Microting.eForm.Dto.KeyValuePair> options =
                                        new List<Microting.eForm.Dto.KeyValuePair>();
                                    int j = 0;
                                    foreach (var s in cpp.Locations.Split("|"))
                                    {
                                        Microting.eForm.Dto.KeyValuePair keyValuePair =
                                            new Microting.eForm.Dto.KeyValuePair(j.ToString(), s, false, j.ToString());
                                        options.Add(keyValuePair);
                                        j++;
                                    }

                                    MultiSelect multiSelect = new MultiSelect(0, false, false,
                                        "Vælg rum som kemiproduktet skal fjernes fra", " ", Constants.FieldColors.Yellow, -1,
                                        false, options);
                                    ((FieldContainer) ((DataElement) mainElement.ElementList[0]).DataItemGroupList[1]).DataItemList.Add(multiSelect);

                                    if (string.IsNullOrEmpty(chemical.Use))
                                    {
                                        ((DataElement) mainElement.ElementList[0]).DataItemGroupList
                                            .RemoveAt(0);
                                    }

                                    var caseId = await _sdkCore.CaseCreate(mainElement, "", (int) sdkSite.MicrotingUid,
                                    folder.Id);
                                var thisDbCase = await sdkDbContext.CheckListSites.AsNoTracking().FirstAsync(x => x.MicrotingUid == caseId);

                                var propertySites = await backendConfigurationPnDbContext.PropertyWorkers
                                    .Where(x => x.PropertyId == areaRule.PropertyId).ToListAsync();

                                foreach (PropertyWorker propertyWorker in propertySites)
                                {
                                    if (propertyWorker.WorkerId != sdkSite.Id)
                                    {
                                        var site = await
                                            sdkDbContext.Sites.FirstOrDefaultAsync(x => x.Id == propertyWorker.WorkerId);
                                        var list = ((DataElement) mainElement.ElementList[0]).DataItemGroupList[1].DataItemList;
                                        list.RemoveAt(0);
                                        list.RemoveAt(0);
                                        ((DataElement) mainElement.ElementList[0]).DataItemGroupList
                                            .RemoveAt(1);
                                        var siteCaseId = await _sdkCore.CaseCreate(mainElement, "", (int) site!.MicrotingUid!,
                                            folder.Id);
                                        // var siteDbCaseId =
                                        //     await sdkDbContext.Cases.FirstAsync(x => x.MicrotingUid == siteCaseId);
                                        var chemicalProductPropertySite = new ChemicalProductPropertySite()
                                        {
                                            ChemicalId = chemical.Id,
                                            SdkCaseId = (int)siteCaseId!,
                                            SdkSiteId = site!.Id,
                                            PropertyId = areaRule.PropertyId,
                                            LanguageId = language.Id
                                        };
                                        await chemicalProductPropertySite.Create(backendConfigurationPnDbContext);
                                    }
                                }

                                AreaRulePlanningModel areaRulePlanningModel = new AreaRulePlanningModel
                                {
                                    Status = true,
                                    AssignedSites = new List<AreaRuleAssignedSitesModel>
                                    {
                                        new()
                                        {
                                            Checked = true,
                                            SiteId = sdkSite.Id
                                        }
                                    },
                                    SendNotifications = false,
                                    StartDate = DateTime.UtcNow,
                                    PropertyId = areaRule.PropertyId,
                                    ComplianceEnabled = false,
                                    TypeSpecificFields = null,
                                    RuleId = areaRule.Id
                                };

                                var newPlanning = await CreateItemPlanningObject(checkListTranslation.CheckListId,
                                    areaRule.EformName,
                                    areaRule.FolderId, areaRulePlanningModel, areaRule);
                                newPlanning.RepeatEvery = 0;
                                newPlanning.RepeatType = RepeatType.Day;
                                newPlanning.StartDate = DateTime.Now.ToUniversalTime();
                                var now = DateTime.UtcNow;
                                newPlanning.LastExecutedTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
                                await newPlanning.Create(itemsPlanningPnDbContext);
                                var newPlanningCase = new PlanningCase
                                {
                                    PlanningId = newPlanning.Id,
                                    Status = 66,
                                    MicrotingSdkeFormId = checkListTranslation.CheckListId
                                };
                                await newPlanningCase.Create(itemsPlanningPnDbContext);
                                var newPlanningCaseSite = new PlanningCaseSite
                                {
                                    MicrotingSdkSiteId = sdkSite.Id,
                                    MicrotingSdkeFormId = checkListTranslation.CheckListId,
                                    Status = 66,
                                    PlanningId = newPlanning.Id,
                                    PlanningCaseId = newPlanningCase.Id,
                                    MicrotingSdkCaseId = (int) caseId,
                                    MicrotingCheckListSitId = thisDbCase.Id
                                };

                                await newPlanningCaseSite.Create(itemsPlanningPnDbContext);

                                var newAreaRulePlanning = CreateAreaRulePlanningObject(areaRulePlanningModel, areaRule,
                                    newPlanning.Id,
                                    areaRule.FolderId);


                                await newAreaRulePlanning.Create(backendConfigurationPnDbContext);
                                ChemicalProductProperty chemicalProductProperty = new ChemicalProductProperty()
                                {
                                    ChemicalId = chemical.Id,
                                    PropertyId = areaRule.PropertyId,
                                    SdkCaseId = (int) caseId,
                                    Locations = cpp.Locations,
                                    LanguageId = language.Id,
                                    SdkSiteId = (int)sdkSite.MicrotingUid
                                };

                                await chemicalProductProperty.Create(backendConfigurationPnDbContext);
                                    // var caseId = await _sdkCore.CaseCreate(mainElement, "", (int) sdkSite.MicrotingUid,
                                    //     folder.Id);
                                    // var thisDbCase = await sdkDbContext.CheckListSites.AsNoTracking().FirstAsync(x => x.MicrotingUid == caseId);

                                }
                            }
                        }
                        else
                        {
                            if (areaRule.SecondaryeFormId != 0 && (areaRule.SecondaryeFormName == "Morgenrundtur" || areaRule.SecondaryeFormName == "Morning tour"))
                            {
                                Console.WriteLine("we have a morning tour");

                                var planningCaseSites = await itemsPlanningPnDbContext.PlanningCaseSites
                                    .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                                    .Where(x => x.PlanningId == planningCaseSite.PlanningId).ToListAsync();

                                foreach (var caseSite in planningCaseSites)
                                {
                                    if (caseSite.MicrotingCheckListSitId != 0)
                                    {
                                        var cls = await sdkDbContext.CheckListSites.FirstAsync(x =>
                                            x.Id == caseSite.MicrotingCheckListSitId);
                                        await _sdkCore.CaseDelete(cls.MicrotingUid);
                                    }
                                    var site = await sdkDbContext.Sites.FirstAsync(x => x.Id == caseSite.MicrotingSdkSiteId);
                                    var siteLanguage = await sdkDbContext.Languages.FirstAsync(x => x.Id == site.LanguageId);
                                    var mainElement = await _sdkCore.ReadeForm(areaRule.SecondaryeFormId, siteLanguage);

                                    foreach (var fieldValue in fieldValues)
                                    {
                                        var field = ((DataElement)mainElement.ElementList[0]).DataItemList
                                            .FirstOrDefault(x => x.Id == fieldValue.FieldId);
                                        if (field != null && !string.IsNullOrEmpty(fieldValue.ValueReadable))
                                        {
                                            if (fieldValue.ValueReadable == "unchecked")
                                            {
                                                fieldValue.ValueReadable = language.Name switch
                                                {
                                                    "Danish" => "Ikke OK",
                                                    "English" => "Not okay",
                                                    _ => "Nicht okay",
                                                };
                                            } else if (fieldValue.ValueReadable == "checked")
                                            {
                                                fieldValue.ValueReadable = language.Name switch
                                                {
                                                    "Danish" => "OK",
                                                    "English" => "Okay",
                                                    _ => "Okay",
                                                };
                                            }
                                            field!.Description.InderValue += language.Name switch
                                            {
                                                "Danish" => "<br>Sidst indsendte: " + fieldValue.ValueReadable,
                                                "English" => "<br>Last submitted: " + fieldValue.ValueReadable,
                                                _ => "<br>Zuletzt eingereicht: " + fieldValue.ValueReadable
                                            };
                                        }
                                    }

                                    await caseSite.Delete(itemsPlanningPnDbContext);
                                    var translation = itemsPlanningPnDbContext.PlanningNameTranslation
                                        .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                                        .Where(x => x.LanguageId == language.Id)
                                        .Where(x => x.PlanningId == planning.Id)
                                        .Select(x => x.Name)
                                        .FirstOrDefault();

                                    mainElement.Label = string.IsNullOrEmpty(planning.PlanningNumber)
                                        ? ""
                                        : planning.PlanningNumber;
                                    if (!string.IsNullOrEmpty(translation))
                                    {
                                        mainElement.Label +=
                                            string.IsNullOrEmpty(mainElement.Label) ? $"{translation}" : $" - {translation}";
                                    }

                                    if (!string.IsNullOrEmpty(planning.BuildYear))
                                    {
                                        mainElement.Label += string.IsNullOrEmpty(mainElement.Label)
                                            ? $"{planning.BuildYear}"
                                            : $" - {planning.BuildYear}";
                                    }

                                    if (!string.IsNullOrEmpty(planning.Type))
                                    {
                                        mainElement.Label += string.IsNullOrEmpty(mainElement.Label)
                                            ? $"{planning.Type}"
                                            : $" - {planning.Type}";
                                    }

                                    if (mainElement.ElementList.Count == 1)
                                    {
                                        mainElement.ElementList[0].Label = mainElement.Label;
                                    }
                                    var thisFolder = await sdkDbContext.Folders.SingleAsync(x => x.Id == areaRule.FolderId).ConfigureAwait(false);
                                    var folderMicrotingId = thisFolder.MicrotingUid.ToString();
                                    mainElement.CheckListFolderName = folderMicrotingId;
                                    mainElement.StartDate = DateTime.Now.ToUniversalTime();
                                    mainElement.EndDate = DateTime.Now.AddYears(10).ToUniversalTime();
                                    mainElement.Repeated = 0;
                                    var caseId = await _sdkCore.CaseCreate(mainElement, "", (int)site!.MicrotingUid!, areaRule.FolderId).ConfigureAwait(false);
                                    var planningCase = new PlanningCase
                                    {
                                        PlanningId = planning.Id,
                                        Status = 66,
                                        MicrotingSdkeFormId = (int)areaRule.EformId!
                                    };
                                    await planningCase.Create(itemsPlanningPnDbContext).ConfigureAwait(false);
                                    var checkListSite = await sdkDbContext.CheckListSites.SingleAsync(x => x.MicrotingUid == caseId).ConfigureAwait(false);
                                    var newPlanningCaseSite = new PlanningCaseSite
                                    {
                                        MicrotingSdkSiteId = site.Id,
                                        MicrotingSdkeFormId = (int)areaRule.EformId!,
                                        Status = 66,
                                        PlanningId = planning.Id,
                                        PlanningCaseId = planningCase.Id,
                                        MicrotingSdkCaseId = (int)caseId!,
                                        MicrotingCheckListSitId = checkListSite.Id
                                    };

                                    await newPlanningCaseSite.Create(itemsPlanningPnDbContext).ConfigureAwait(false);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (planning.RepeatType == RepeatType.Week && planning.RepeatEvery == 1)
                    {
                        var poolHour = await
                            backendConfigurationPnDbContext.PoolHours.FirstOrDefaultAsync(x =>
                                x.ItemsPlanningId == planning.Id);
                        if (poolHour != null)
                        {
                            var poolPlanningCaseSite =
                                await itemsPlanningPnDbContext.PlanningCaseSites.FirstOrDefaultAsync(x =>
                                    x.MicrotingSdkCaseId == dbCase.Id);
                            var theDate = new DateTime(dbCase.CreatedAt.Value.Year, dbCase.CreatedAt.Value.Month,
                                poolPlanningCaseSite.CreatedAt.Day, poolHour.Index, 0, 0);
                            var historyDate = new DateTime(dbCase.CreatedAt.Value.Year, dbCase.CreatedAt.Value.Month,
                                poolPlanningCaseSite.CreatedAt.Day, 0, 0, 0);
                            var poolHourResult =
                                await backendConfigurationPnDbContext.PoolHourResults.FirstOrDefaultAsync(x =>
                                    x.PoolHourId == poolHour.Id && x.Date == theDate);

                            var checkList = await sdkDbContext.CheckListTranslations.FirstOrDefaultAsync(x =>
                                x.Text == "00. Info boks");

                            var areaRule =
                                await backendConfigurationPnDbContext.AreaRules.Where(x =>
                                    x.Id == poolHour.AreaRuleId)
                                    .Include(x => x.AreaRuleTranslations)
                                    .FirstOrDefaultAsync();

                            if (poolHourResult == null)
                            {

                                var areaField =
                                    await sdkDbContext.Fields.FirstAsync(x =>
                                        x.CheckListId == planning.RelatedEFormId + 1 && x.DisplayIndex == 0);
                                var pulseFieldValue = await sdkDbContext.FieldValues.FirstAsync(x =>
                                    x.FieldId == areaField.Id && x.CaseId == dbCase.Id);
                                areaField =
                                    await sdkDbContext.Fields.FirstAsync(x =>
                                        x.CheckListId == planning.RelatedEFormId + 1 && x.DisplayIndex == 1);
                                var phFieldValue = await sdkDbContext.FieldValues.FirstAsync(x =>
                                    x.FieldId == areaField.Id && x.CaseId == dbCase.Id);
                                areaField =
                                    await sdkDbContext.Fields.FirstAsync(x =>
                                        x.CheckListId == planning.RelatedEFormId + 1 && x.DisplayIndex == 2);
                                var freeClorideFieldValue =
                                    await sdkDbContext.FieldValues.FirstAsync(x =>
                                        x.FieldId == areaField.Id && x.CaseId == dbCase.Id);
                                areaField =
                                    await sdkDbContext.Fields.FirstAsync(x =>
                                        x.CheckListId == planning.RelatedEFormId + 1 && x.DisplayIndex == 3);
                                var tempFieldValue = await sdkDbContext.FieldValues.FirstAsync(x =>
                                    x.FieldId == areaField.Id && x.CaseId == dbCase.Id);
                                areaField =
                                    await sdkDbContext.Fields.FirstAsync(x =>
                                        x.CheckListId == planning.RelatedEFormId + 1 && x.DisplayIndex == 4);
                                var numberOfGuestsFieldValue =
                                    await sdkDbContext.FieldValues.FirstAsync(x =>
                                        x.FieldId == areaField.Id && x.CaseId == dbCase.Id);
                                areaField =
                                    await sdkDbContext.Fields.FirstAsync(x =>
                                        x.CheckListId == planning.RelatedEFormId + 1 && x.DisplayIndex == 5);
                                var clarityFieldValue =
                                    await sdkDbContext.FieldValues.FirstAsync(x =>
                                        x.FieldId == areaField.Id && x.CaseId == dbCase.Id);
                                areaField =
                                    await sdkDbContext.Fields.FirstAsync(x =>
                                        x.CheckListId == planning.RelatedEFormId + 1 && x.DisplayIndex == 6);
                                var measuredFreeClorideFieldValue =
                                    await sdkDbContext.FieldValues.FirstAsync(x =>
                                        x.FieldId == areaField.Id && x.CaseId == dbCase.Id);
                                areaField =
                                    await sdkDbContext.Fields.FirstAsync(x =>
                                        x.CheckListId == planning.RelatedEFormId + 1 && x.DisplayIndex == 7);
                                var totalClorideFieldValue =
                                    await sdkDbContext.FieldValues.FirstAsync(x =>
                                        x.FieldId == areaField.Id && x.CaseId == dbCase.Id);
                                areaField =
                                    await sdkDbContext.Fields.FirstAsync(x =>
                                        x.CheckListId == planning.RelatedEFormId + 1 && x.DisplayIndex == 8);
                                var boundClorideFieldValue =
                                    await sdkDbContext.FieldValues.FirstAsync(x =>
                                        x.FieldId == areaField.Id && x.CaseId == dbCase.Id);
                                areaField =
                                    await sdkDbContext.Fields.FirstAsync(x =>
                                        x.CheckListId == planning.RelatedEFormId + 1 && x.DisplayIndex == 9);
                                var measuredPhFieldValue =
                                    await sdkDbContext.FieldValues.FirstAsync(x =>
                                        x.FieldId == areaField.Id && x.CaseId == dbCase.Id);
                                areaField =
                                    await sdkDbContext.Fields.FirstAsync(x =>
                                        x.CheckListId == planning.RelatedEFormId + 1 && x.DisplayIndex == 10);
                                var receiptFieldValue =
                                    await sdkDbContext.FieldValues.FirstAsync(x =>
                                        x.FieldId == areaField.Id && x.CaseId == dbCase.Id);
                                areaField =
                                    await sdkDbContext.Fields.FirstAsync(x =>
                                        x.CheckListId == planning.RelatedEFormId + 1 && x.DisplayIndex == 11);
                                var measuredTempFieldValue =
                                    await sdkDbContext.FieldValues.FirstAsync(x =>
                                        x.FieldId == areaField.Id && x.CaseId == dbCase.Id);
                                areaField =
                                    await sdkDbContext.Fields.FirstAsync(x =>
                                        x.CheckListId == planning.RelatedEFormId + 1 && x.DisplayIndex == 12);
                                var commentFieldValue =
                                    await sdkDbContext.FieldValues.FirstAsync(x =>
                                        x.FieldId == areaField.Id && x.CaseId == dbCase.Id);
                                areaField =
                                    await sdkDbContext.Fields.FirstAsync(x =>
                                        x.CheckListId == planning.RelatedEFormId + 1 && x.DisplayIndex == 13);
                                var doneByFieldValue =
                                    await sdkDbContext.FieldValues.FirstAsync(x =>
                                        x.FieldId == areaField.Id && x.CaseId == dbCase.Id);

                                poolHourResult = new PoolHourResult()
                                {
                                    PoolHourId = poolHour.Id,
                                    PlanningId = planning.Id,
                                    FolderId = (int) planning.SdkFolderId,
                                    Date = theDate,
                                    PulseRateAtOpening = double.Parse((pulseFieldValue.Value ?? "0").Replace(",",".")),
                                    ReadPhValue = double.Parse((phFieldValue.Value ?? "0").Replace(",",".")),
                                    ReadFreeChlorine = double.Parse((freeClorideFieldValue.Value ?? "0").Replace(",",".")),
                                    ReadTemperature = double.Parse((tempFieldValue.Value ?? "0").Replace(",",".")),
                                    NumberOfGuestsAtClosing = double.Parse((numberOfGuestsFieldValue.Value ?? "0").Replace(",",".")),
                                    Clarity = clarityFieldValue.Value,
                                    MeasuredFreeChlorine = double.Parse((measuredFreeClorideFieldValue.Value ?? "0").Replace(",",".")),
                                    MeasuredTotalChlorine = double.Parse((totalClorideFieldValue.Value ?? "0").Replace(",",".")),
                                    MeasuredBoundChlorine = double.Parse((boundClorideFieldValue.Value ?? "0").Replace(",",".")),
                                    MeasuredPh = double.Parse((measuredPhFieldValue.Value ?? "0").Replace(",",".")),
                                    AcknowledgmentOfPulseRateAtOpening = receiptFieldValue.Value,
                                    MeasuredTempDuringTheDay = double.Parse((measuredTempFieldValue.Value ?? "0").Replace(",",".")),
                                    Comment = commentFieldValue.Value,
                                    DoneByUserId = doneByFieldValue.Value == "null" ? 0 : int.Parse(doneByFieldValue.Value),
                                    DoneByUserName = doneByFieldValue.Value,
                                    SdkCaseId = dbCase.Id,
                                    AreaRuleId = poolHour.AreaRuleId,
                                    DoneAt = (DateTime)dbCase.DoneAt
                                };
                                await poolHourResult.Create(backendConfigurationPnDbContext);

                                var planningSites = await itemsPlanningPnDbContext.PlanningSites
                                    .Where(x => x.PlanningId == planning.Id).ToListAsync();

                                var lookupName = areaRule.AreaRuleTranslations.First().Name;

                                var subfolder = await sdkDbContext.Folders
                                    .Include(x => x.FolderTranslations)
                                    .Where(x=> x.ParentId == areaRule.FolderId)
                                    .Where(x => x.FolderTranslations.Any(y => y.Name == lookupName))
                                    .FirstOrDefaultAsync();
                                var innerLookupName = $"{(int)poolHour.DayOfWeek}. {poolHour.DayOfWeek.ToString().Substring(0, 3)}";
                                var poolDayFolder = await sdkDbContext.Folders
                                    .Include(x => x.FolderTranslations)
                                    .Where(x=> x.ParentId == subfolder.Id)
                                    .Where(x => x.FolderTranslations.Any(y => y.Name == innerLookupName))
                                    .FirstAsync();


                                Regex regex = new Regex(@"(\d\.\s)");
                                TimeZoneInfo timeZoneInfo;
                                try
                                {
                                    timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");
                                }
                                catch
                                {
                                    timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("E. Europe Standard Time");
                                }
                                foreach (var planningSite in planningSites)
                                {
                                    var poolHistorySite = await backendConfigurationPnDbContext.PoolHistorySites
                                        .Where(x => x.AreaRuleId == poolHour.AreaRuleId)
                                        .Where(x => x.SiteId == planningSite.SiteId)
                                        .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                                        .Where(x => x.Date == historyDate).FirstOrDefaultAsync();

                                    var sdkSite = await sdkDbContext.Sites.FirstAsync(x => x.Id == planningSite.SiteId);
                                    var language = await sdkDbContext.Languages.FirstAsync(x => x.Id == sdkSite.LanguageId);
                                    var mainElement = await _sdkCore.ReadeForm(checkList.CheckListId, language);
                                    mainElement.Repeated = 0;
                                    mainElement.CheckListFolderName = poolDayFolder.MicrotingUid.ToString();
                                    mainElement.StartDate = DateTime.Now.ToUniversalTime();
                                    mainElement.EndDate = DateTime.Now.AddDays(2).ToUniversalTime();
                                    mainElement.Label =
                                        $"0. Tidligere indsendt data - {areaRule.AreaRuleTranslations.First().Name}";
                                    mainElement.ElementList[0].Label = mainElement.Label;
                                    var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo);
                                    mainElement.ElementList[0].Description = new CDataValue()
                                    {
                                        InderValue =
                                            $"Sidst opdateret: {localTime:H:mm}"
                                    };
                                    ((DataElement) mainElement.ElementList[0]).DataItemList[0].Color =
                                        Constants.FieldColors.Yellow;

                                    // ((DataElement) mainElement.ElementList[0]).DataItemList[0].Label = $"{poolDayFolder.FolderTranslations.Where(x => x.LanguageId == language.Id).Select(x => x.Name).First()} - {areaRule.AreaRuleTranslations.First().Name}";
                                    // ((DataElement) mainElement.ElementList[0]).DataItemList[0].Label =
                                    //     regex.Replace(((DataElement) mainElement.ElementList[0]).DataItemList[0].Label,
                                    //         "");
                                    // ((DataElement) mainElement.ElementList[0]).DataItemList[0].Label = $"{((DataElement) mainElement.ElementList[0]).DataItemList[0].Label}";
                                    ((DataElement) mainElement.ElementList[0]).DataItemList[0].Label = " ";
                                    var allPoolHourResults = await backendConfigurationPnDbContext.PoolHourResults
                                        .Where(x => x.Date >= historyDate)
                                        .Where(x => x.Date < historyDate.AddDays(1))
                                        .Where(x=> x.FolderId == planning.SdkFolderId)
                                        .Where(x => x.AreaRuleId == poolHour.AreaRuleId)
                                        .OrderBy(x => x.Date)
                                        .ToListAsync();
                                    StringBuilder header = new StringBuilder();

                                    foreach (var hourResult in allPoolHourResults)
                                    {

                                        localTime = TimeZoneInfo.ConvertTimeFromUtc(hourResult.DoneAt, timeZoneInfo);
                                        var selectedPoolHour =
                                            await backendConfigurationPnDbContext.PoolHours.FirstAsync(x =>
                                                x.Id == hourResult.PoolHourId);
                                        header.Append($"<strong>{selectedPoolHour.Name}:00 {regex.Replace($"{poolDayFolder.FolderTranslations.Where(x => x.LanguageId == language.Id).Select(x => x.Name).First()} - {areaRule.AreaRuleTranslations.First().Name}", "")}</strong>");
                                        header.Append($"<br>Dato: {localTime:dd-MM-yyyy}<br>");
                                        header.Append($"Tid: {localTime:HH:mm:ss}<br>");
                                        header.Append($"Pulsslag ved åbning: {hourResult.PulseRateAtOpening}<br>");
                                        header.Append($"Aflæst PH: {hourResult.ReadPhValue}<br>");
                                        header.Append($"Aflæst frit klor: {hourResult.ReadFreeChlorine}<br>");
                                        header.Append($"Aflæst temp: {hourResult.ReadTemperature}<br>");
                                        header.Append($"Antal gæster ved lukning: {hourResult.NumberOfGuestsAtClosing}<br>");
                                        header.Append($"Klarhed: {(hourResult.Clarity == "1" ? "OK" : hourResult.Clarity == null ? "" : "Grumset")}<br>");
                                        header.Append($"Målt Frit klor: {hourResult.MeasuredFreeChlorine}<br>");
                                        header.Append($"Målt Total klor: {hourResult.MeasuredTotalChlorine}<br>");
                                        header.Append($"Målt bundet klor: {hourResult.MeasuredBoundChlorine}<br>");
                                        header.Append($"Målt PH: {hourResult.MeasuredPh}<br>");
                                        header.Append($"Kvittering af pulsslag v. åbning: {(hourResult.AcknowledgmentOfPulseRateAtOpening == "1" ? "Ja" : hourResult.AcknowledgmentOfPulseRateAtOpening == null ? "" : "Nej")}<br>");
                                        header.Append($"Målt temp. i løbet af dagen: {hourResult.MeasuredTempDuringTheDay}<br>");
                                        header.Append($"Kommentar: {hourResult.Comment}<br><br>");
                                    }

                                        // header = ;
                                    ((DataElement) mainElement.ElementList[0]).DataItemList[0].Description = new CDataValue
                                    {
                                        InderValue =
                                            header.ToString()
                                    };

                                    if (poolHistorySite == null)
                                    {
                                        var caseId = await _sdkCore.CaseCreate(mainElement, "", (int) sdkSite.MicrotingUid, poolDayFolder.Id);
                                        poolHistorySite = new PoolHistorySite()
                                        {
                                            AreaRuleId = poolHour.AreaRuleId,
                                            Date = historyDate,
                                            SiteId = planningSite.SiteId,
                                            SdkCaseId = (int)caseId
                                        };

                                        await poolHistorySite.Create(backendConfigurationPnDbContext);
                                    }
                                    else
                                    {
                                        await _sdkCore.CaseDelete(poolHistorySite.SdkCaseId);
                                        var caseId = await _sdkCore.CaseCreate(mainElement, "", (int) sdkSite.MicrotingUid, poolDayFolder.Id);
                                        poolHistorySite.SdkCaseId = (int)caseId;
                                        await poolHistorySite.Update(backendConfigurationPnDbContext);
                                    }

                                }
                            }
                        }
                    }
                    else
                    {
                        while (planningCaseSite.Status != 100)
                        {
                            Thread.Sleep(1000);
                            Console.WriteLine($"Waiting for case {planningCaseSite.Id} to be completed");
                            planningCaseSite = itemsPlanningPnDbContext.PlanningCaseSites.AsNoTracking()
                                .First(x => x.Id == planningCaseSite.Id);
                            if (planningCaseSite.Status == 100)
                            {
                                planningCaseSite =
                                    itemsPlanningPnDbContext.PlanningCaseSites.AsNoTracking()
                                        .First(x => x.Id == planningCaseSite.Id);
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
                                    await backendConfigurationPnDbContext.Compliances.FirstAsync(
                                        x => x.Id == compliance.Id);
                                await dbCompliance.Delete(backendConfigurationPnDbContext);
                            }

                            var backendPlanning = await backendConfigurationPnDbContext.AreaRulePlannings.AsNoTracking()
                                .Where(x => x.ItemPlanningId == planningCaseSite.PlanningId).FirstOrDefaultAsync();

                            var property =
                                await backendConfigurationPnDbContext.Properties.FirstOrDefaultAsync(x =>
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
                        }

                        if (eformIdForControlFloatingLayer == dbCase.CheckListId)
                        {
                            var fieldValues = await sdkDbContext.FieldValues
                                .Where(x => x.CaseId == dbCase.Id)
                                .Include(x => x.Field)
                                .ThenInclude(x => x.FieldType)
                                .ToListAsync();

                            var checkBoxFloatingLayerOk = fieldValues
                                .Where(x => x.Field.FieldType.Type == Constants.FieldTypes.CheckBox)
                                .Select(x =>
                                    !string.IsNullOrEmpty(x.Value) &&
                                    x.Value ==
                                    "checked") // string.IsNullOrEmpty(x.Value) ? false : x.Value == "checked" ? true : false
                                .First();

                            var statusOrActivityFieldIdAndKey = fieldValues
                                .Where(x => x.Field.FieldType.Type == Constants.FieldTypes.SingleSelect)
                                .Select(x => new {Key = x.Value, x.FieldId})
                                .First();

                            var statusOrActivity = string.IsNullOrEmpty(statusOrActivityFieldIdAndKey.Key)
                                ? ""
                                : await sdkDbContext.FieldOptions
                                    .Where(x => x.Key == statusOrActivityFieldIdAndKey.Key)
                                    .Where(x => x.FieldId == (int) statusOrActivityFieldIdAndKey.FieldId)
                                    .Include(x => x.FieldOptionTranslations)
                                    .SelectMany(x => x.FieldOptionTranslations)
                                    .Where(x => x.LanguageId == 1) // get only danish
                                    .Select(x => x.Text)
                                    .FirstOrDefaultAsync();

                            var listWithStatuses = new List<string>
                            {
                                "Beholder omrørt", // da
                                // "Slurry tank stirred", // en
                                "Gylle udbragt", // da
                                // "Slurry delivered", // en
                                "Halm tilført", // da
                                // "Straw added", // en
                                "Flyttet til anden beholder", // da
                                // "Moved to another slurry tank", // en
                                "Modtaget biogas-gylle", // da
                                // "Biogas slurry received", // en
                                "", // Blank
                            };
                            if (checkBoxFloatingLayerOk == false && listWithStatuses.Contains(statusOrActivity))
                            {
                                // retract old eform
                                // await _sdkCore.CaseDelete(dbCase.Id);
                                // deploy new eform with old data, reminder: Current data + 6 days
                                // planningCaseSite.MicrotingSdkSiteId

                                var oldComment = fieldValues
                                    .Where(x => x.Field.FieldType.Type == Constants.FieldTypes.Comment)
                                    .Select(x => x.Value)
                                    .First();

                                // get name tank from linked planning
                                var itemPlanningId = await itemsPlanningPnDbContext.PlanningCaseSites
                                    .Where(x => x.MicrotingSdkCaseId == dbCase.Id)
                                    .Select(x => x.PlanningId)
                                    .FirstOrDefaultAsync();

                                // var itemPlanning = await itemsPlanningPnDbContext.Plannings.FirstAsync(x => x.Id == itemPlanningId);
                                var itemPlanningSites = await itemsPlanningPnDbContext.PlanningSites
                                    .Where(x => x.PlanningId == itemPlanningId).ToListAsync();
                                PlanningCase planningCase = new PlanningCase()
                                {
                                    PlanningId = planning.Id,
                                    Status = 66,
                                    MicrotingSdkeFormId = (int) dbCase.CheckListId
                                };
                                foreach (var itemPlanningSite in itemPlanningSites)
                                {
                                    var site = await sdkDbContext.Sites.FirstAsync(
                                        x => x.Id == itemPlanningSite.SiteId);
                                    var siteLanguage =
                                        await sdkDbContext.Languages.FirstAsync(x => x.Id == site.LanguageId);
                                    var nameTank = await _itemsPlanningDbContextHelper
                                        .GetDbContext().PlanningNameTranslation
                                        .Where(x => x.PlanningId == itemPlanningId)
                                        .Where(x => x.LanguageId == site.LanguageId)
                                        .Select(x => x.Name)
                                        .FirstOrDefaultAsync();

                                    var mainElement =
                                        await _sdkCore.ReadeForm(eformIdForControlFloatingLayer, siteLanguage);
                                    ((DataElement) mainElement.ElementList[0]).DataItemGroupList[0].DataItemList[0]
                                        .Description.InderValue =
                                        $"<strong>{Translations.FollowUpFloatingLayerCheck}</strong><br>" +
                                        $"<strong>{Translations.SlurryTank}:</strong> {nameTank}<br>" +
                                        $"<strong>{Translations.LastUpdated}:</strong> {dbCase.DoneAt.Value:dd-MM-yyyy}<br>" +
                                        $"<strong>{Translations.StatusOrActivity}:</strong>{statusOrActivity}<br>" +
                                        $"<strong>{Translations.ControlLatest}:</strong> {dbCase.DoneAt.Value.AddDays(6):dd-MM-yyyy}";
                                    ((Comment) ((DataElement) mainElement.ElementList[0]).DataItemList[3]).Value =
                                        oldComment;

                                    mainElement.StartDate = DateTime.Now.AddDays(6).ToUniversalTime();
                                    mainElement.CheckListFolderName = await sdkDbContext.Folders
                                        .Where(x => x.Id == dbCase.FolderId)
                                        .Select(x => x.MicrotingUid.ToString())
                                        .FirstAsync();
                                    planningCaseSite = new PlanningCaseSite()
                                    {
                                        MicrotingSdkSiteId = site.Id,
                                        MicrotingSdkeFormId = (int) dbCase.CheckListId,
                                        Status = 66,
                                        PlanningId = planning.Id,
                                        PlanningCaseId = planningCase.Id
                                    };

                                    await planningCaseSite.Create(itemsPlanningPnDbContext);
                                    var folder = await getTopFolderName((int) planning.SdkFolderId, sdkDbContext);
                                    string body = "";
                                    if (folder != null)
                                    {
                                        planning.SdkFolderId = sdkDbContext.Folders
                                            .FirstOrDefault(y => y.Id == planning.SdkFolderId)
                                            ?.Id;
                                        FolderTranslation folderTranslation =
                                            await sdkDbContext.FolderTranslations.FirstOrDefaultAsync(x =>
                                                x.FolderId == folder.Id && x.LanguageId == site.LanguageId);
                                        body = $"{folderTranslation.Name} ({site.Name};{DateTime.Now:dd.MM.yyyy})";
                                    }

                                    PlanningNameTranslation planningNameTranslation =
                                        await itemsPlanningPnDbContext.PlanningNameTranslation.FirstOrDefaultAsync(x =>
                                            x.PlanningId == planning.Id
                                            && x.LanguageId == site.LanguageId);

                                    mainElement.PushMessageBody = body;
                                    mainElement.PushMessageTitle = planningNameTranslation.Name;
                                    // var _ = await _sdkCore.CaseCreate(mainElement, "", (int)site.MicrotingUid, dbCase.FolderId);
                                    var caseId = await _sdkCore.CaseCreate(mainElement, "", (int) site.MicrotingUid,
                                        dbCase.FolderId);

                                    if (caseId != null)
                                    {
                                        var caseDto = await _sdkCore.CaseLookupMUId((int) caseId);
                                        if (caseDto?.CaseId != null)
                                            planningCaseSite.MicrotingSdkCaseId = (int) caseDto.CaseId;
                                        await planningCaseSite.Update(itemsPlanningPnDbContext);
                                    }
                                }
                            }
                        }


                    }
                }
            }
        }

        private async Task<MainElement> ModifyChemicalMainElement(MainElement mainElement, Chemical chemical,
            Product product, string productName, string folderMicrotingId, AreaRule areaRule, Site sdkSite, string locations)
        {
            mainElement.Repeated = 0;
            mainElement.CheckListFolderName = folderMicrotingId;
            mainElement.StartDate = DateTime.Now.ToUniversalTime();
            mainElement.EndDate = DateTime.Now.AddYears(10).ToUniversalTime();
            mainElement.DisplayOrder = 10000000;
            mainElement.ElementList[0].DoneButtonEnabled = false;
            mainElement.Label = productName;
            mainElement.ElementList[0].Label = productName;
            mainElement.ElementList.First().Description.InderValue =
                $"{chemical.AuthorisationHolder.Name}<br>" +
                $"Reg nr.: {chemical.RegistrationNo}<br>";
            if (chemical.PesticideProductGroup.Count > 0)
            {
                mainElement.ElementList.First().Description.InderValue += "Produktgruppe: ";
                var n = 0;
                foreach (int i in chemical.PesticideProductGroup)
                {
                    if (n > 0)
                    {
                        mainElement.ElementList.First().Description.InderValue += ",";
                    }
                    mainElement.ElementList.First().Description.InderValue += Microting.EformBackendConfigurationBase
                        .Infrastructure.Const.Constants.ProductGroupPesticide.First(x => x.Key == i).Value;
                    n++;
                }
                mainElement.ElementList.First().Description.InderValue += "<br><br>";
            }

            mainElement.ElementList.First().Description.InderValue +=
                $"<br><strong>Placering</strong><br>Ejendom: {areaRule.Property.Name}<br>Rum: {locations}<br><br><strong>Udløbsdato: </strong><br>";

            if (chemical.UseAndPossesionDeadline != null)
            {
                mainElement.ElementList.First().Description.InderValue += $"Dato: {chemical.UseAndPossesionDeadline:dd-MM-yyyy}<br><br>";
            }
            else
            {
                mainElement.ElementList.First().Description.InderValue += $"Dato: {chemical.AuthorisationExpirationDate:dd-MM-yyyy}<br><br>";
            }
            ((None) ((DataElement) mainElement.ElementList[0]).DataItemList[0]).Label = productName;
            ((None) ((DataElement) mainElement.ElementList[0]).DataItemList[0]).Description
                .InderValue =
                $"{chemical.AuthorisationHolder.Name}<br>" +
                $"Reg nr.: {chemical.RegistrationNo}<br><br>";

            ((None) ((DataElement) mainElement.ElementList[0]).DataItemList[0]).Description
                .InderValue +=
                "<strong>Udløbsdato</strong><br>";

            if (chemical.UseAndPossesionDeadline != null)
            {
                ((None) ((DataElement) mainElement.ElementList[0]).DataItemList[0]).Description
                    .InderValue += $"Dato: {chemical.UseAndPossesionDeadline:dd-MM-yyyy}<br><br>";
            }
            else
            {
                ((None) ((DataElement) mainElement.ElementList[0]).DataItemList[0]).Description
                    .InderValue += $"Dato: {chemical.AuthorisationExpirationDate:dd-MM-yyyy}<br><br>";
            }

            if (chemical.PesticideProductGroup.Count > 0)
            {
                ((None) ((DataElement) mainElement.ElementList[0]).DataItemList[0]).Description
                    .InderValue += "Produktgruppe: ";
                var n = 0;
                foreach (int i in chemical.PesticideProductGroup)
                {
                    if (n > 0)
                    {
                        ((None) ((DataElement) mainElement.ElementList[0]).DataItemList[0]).Description
                            .InderValue += ",";
                    }
                    ((None) ((DataElement) mainElement.ElementList[0]).DataItemList[0]).Description
                        .InderValue += Microting.EformBackendConfigurationBase
                        .Infrastructure.Const.Constants.ProductGroupPesticide.First(x => x.Key == i).Value;
                    n++;
                }
                ((None) ((DataElement) mainElement.ElementList[0]).DataItemList[0]).Description
                    .InderValue += "<br><br>";
            }

            ((None) ((DataElement) mainElement.ElementList[0]).DataItemList[0]).Description
                .InderValue +=
                "<strong>Placering:</strong><br>" +
                $"Ejendom: {areaRule.Property.Name}<br>" +
                $"Rum: {locations}<br><br>" +
                "<strong>Klassificering og mærkening</strong><br>";
            List<string> HStatements = new List<string>();
            foreach (var hazardStatement in chemical.ClassificationAndLabeling.CLP.HazardStatements)
            {
                ((None) ((DataElement) mainElement.ElementList[0]).DataItemList[0]).Description
                    .InderValue +=
                    $"{Microting.EformBackendConfigurationBase.Infrastructure.Const.Constants.HazardStatement.First(x => x.Key == hazardStatement.Statement).Value}<br><br>";
                Regex regex = new Regex(@"\((H\d\d\d)\)");
                var res = regex.Match(Microting.EformBackendConfigurationBase.Infrastructure.Const.Constants
                    .HazardStatement.First(x => x.Key == hazardStatement.Statement).Value);
                HStatements.Add(res.Value);
            }

            ((None) ((DataElement) mainElement.ElementList[0]).DataItemList[0]).Description
                .InderValue +=
                "<br><strong>Generelle oplysninger</strong><br>" +
                "<u>Bekæmpelsesmiddelstype</u><br>";

            ((None) ((DataElement) mainElement.ElementList[0]).DataItemList[0]).Description
                .InderValue += chemical.PestControlType != null
                    ? $"{Microting.EformBackendConfigurationBase.Infrastructure.Const.Constants.PestControlType.FirstOrDefault(x => x.Key == chemical.PestControlType)!.Value}<br><br>"
                    : "";

            ((None) ((DataElement) mainElement.ElementList[0]).DataItemList[0]).Description
                .InderValue +=
                "<u>Produktstatus</u><br>" +
                $"{Microting.EformBackendConfigurationBase.Infrastructure.Const.Constants.ProductStatusType.FirstOrDefault(x => x.Key == chemical.Status).Value}<br><br>";

            if (chemical.PestControlType == 2)
            {
                ((None) ((DataElement) mainElement.ElementList[0]).DataItemList[0]).Description
                    .InderValue += $"<u>Pesticid produktgruppe</u><br>";
                foreach (var i in chemical.PesticideProductGroup)
                {
                    ((None) ((DataElement) mainElement.ElementList[0]).DataItemList[0]).Description
                        .InderValue +=
                        $"{Microting.EformBackendConfigurationBase.Infrastructure.Const.Constants.ProductGroupPesticide.FirstOrDefault(x => x.Key == i).Value}<br>";
                }
            }
            else
            {
                ((None) ((DataElement) mainElement.ElementList[0]).DataItemList[0]).Description
                    .InderValue += $"<u>Biocid produktgruppe</u><br>";
                foreach (var i in chemical.BiocideProductType)
                {
                    ((None) ((DataElement) mainElement.ElementList[0]).DataItemList[0]).Description
                        .InderValue +=
                        $"{Microting.EformBackendConfigurationBase.Infrastructure.Const.Constants.ProductGroupBiocide.FirstOrDefault(x => x.Key == i).Value}<br>";
                }
            }



            ((None) ((DataElement) mainElement.ElementList[0]).DataItemList[0]).Description
                .InderValue +=
                "<br><u>Godkendelsesdato</u><br>" +
                $"{chemical.AuthorisationDate:dd-MM-yyyy}<br><br>";

            ((FieldContainer) ((DataElement) mainElement.ElementList[0]).DataItemGroupList[0])
                .DataItemList[0].Label = " ";
            ((FieldContainer) ((DataElement) mainElement.ElementList[0]).DataItemGroupList[0])
                .DataItemList[0].Description
                .InderValue =
                $"{chemical.Use}<br>";

            ((FieldContainer) ((DataElement) mainElement.ElementList[0]).DataItemGroupList[1])
                .DataItemList[0].Label = "Kemiprodukt fjernet";
            ((DataElement) mainElement.ElementList[0]).DataItemGroupList[1].Label = "Hvordan fjerner jeg et produkt?";
            string description = $"Produkt: {productName}<br>" +
                                 $"Producent: {chemical.AuthorisationHolder.Name}<br>" +
                                 $"Reg nr.: {chemical.RegistrationNo}<br>";
            if (chemical.PesticideProductGroup.Count > 0)
            {
                description += "Produktgruppe: ";
                var n = 0;
                foreach (int i in chemical.PesticideProductGroup)
                {
                    if (n > 0)
                    {
                        description += ",";
                    }
                    description += Microting.EformBackendConfigurationBase
                            .Infrastructure.Const.Constants.ProductGroupPesticide.First(x => x.Key == i).Value;
                    n++;
                }
                description += "<br><br>";
            }

            description += $"Ejendom: {areaRule.Property.Name}<br>" +
                                 $"Rum: {locations}<br><br>" +
                                 "<strong>Gør følgende for at fjerne et produkt:</strong><br>" +
                                 "1. Vælg hvilke rum produktet skal fjernes fra og tryk Gem.<br>" +
                                 "2. Sæt flueben i <strong>Produkt fjernet</strong><br>" +
                                 "3. Tryk på Bekræft<br><br>";
            None none = new None(1, false, false, " ", description, Constants.FieldColors.Red, -2, false);
            ((FieldContainer) ((DataElement) mainElement.ElementList[0]).DataItemGroupList[1]).DataItemList.Add(none);
            ((CheckBox) ((FieldContainer) ((DataElement) mainElement.ElementList[0]).DataItemGroupList[1]).DataItemList[0]).Label =
                "Produkt fjernet";
            ((SaveButton) ((FieldContainer) ((DataElement) mainElement.ElementList[0]).DataItemGroupList[1]).DataItemList[1]).Label =
                "Bekræft produkt fjernet";
            if (product != null)
            {
                if (!string.IsNullOrEmpty(product.FileName))
                {
                    using var webClient = new HttpClient();
                    Console.WriteLine(
                        $"Trying to download https://chemicalbase.microting.com/api/chemicals-pn/get-pdf-file?fileName={product.FileName}");
                    await using (var s = await webClient.GetStreamAsync(
                                     $"https://chemicalbase.microting.com/api/chemicals-pn/get-pdf-file?fileName={product.FileName}"))
                    {
                        File.Delete(Path.Combine(Path.GetTempPath(), $"{product.FileName}.pdf"));
                        await using (var fs = new FileStream(
                                         Path.Combine(Path.GetTempPath(), $"{product.FileName}.pdf"),
                                         FileMode.CreateNew))
                        {
                            await s.CopyToAsync(fs);
                        }
                    }

                    var pdfId = await _sdkCore.PdfUpload(Path.Combine(Path.GetTempPath(), $"{product.FileName}.pdf"));

                    await _sdkCore.PutFileToStorageSystem(Path.Combine(Path.GetTempPath(), $"{product.FileName}.pdf"),
                        $"{product.FileName}.pdf");
                    File.Delete(Path.Combine(Path.GetTempPath(), $"{product.FileName}.pdf"));

                    ((ShowPdf)((DataElement)mainElement.ElementList[0]).DataItemList[1]).Value = pdfId;
                    ((DataElement) mainElement.ElementList[0]).DataItemList.RemoveAt(2);
                }
                else
                {
                    ((DataElement) mainElement.ElementList[0]).DataItemList.RemoveAt(1);
                    ((DataElement) mainElement.ElementList[0]).DataItemList.RemoveAt(1);
                }
            }
            else
            {
                ((DataElement) mainElement.ElementList[0]).DataItemList.RemoveAt(1);
                ((DataElement) mainElement.ElementList[0]).DataItemList.RemoveAt(1);
            }

            return mainElement;
        }

        private async Task DeployWorkOrderEform(
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
            string updatedByName)
        {
            await using var sdkDbContext = _sdkCore.DbContextHelper.GetDbContext();
            await using var backendConfigurationPnDbContext =
                _backendConfigurationDbContextHelper.GetDbContext();
            var i = 0;
            foreach (var propertyWorker in propertyWorkers)
            {
                var site = await sdkDbContext.Sites.FirstAsync(x => x.Id == propertyWorker.WorkerId);
                var siteLanguage = await sdkDbContext.Languages.FirstAsync(x => x.Id == site.LanguageId);
                var mainElement = await _sdkCore.ReadeForm(eformId, siteLanguage);
                mainElement.CheckListFolderName = sdkDbContext.Folders.First(x => x.Id == folderId)
                    .MicrotingUid.ToString();
                mainElement.Label = " ";
                mainElement.ElementList[0].QuickSyncEnabled = true;
                mainElement.EnableQuickSync = true;
                mainElement.ElementList[0].Label = " ";
                mainElement.ElementList[0].Description.InderValue = description.Replace("\n", "<br>") + "<center><strong>******************</strong></center>";
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
                ((DataElement)mainElement.ElementList[0]).DataItemList[0].Description.InderValue = description.Replace("\n", "<br>");
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
                    Description = newDescription,
                    CaseInitiated = workorderCase.CaseInitiated,
                    LastAssignedToName = siteName,
                    LastUpdatedByName = updatedByName,
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

            if (workOrderCase.ParentWorkorderCaseId != null)
            {
                var workOrdersToRetract = await backendConfigurationPnDbContext.WorkorderCases
                    .Where(x => x.ParentWorkorderCaseId == workOrderCase.ParentWorkorderCaseId).ToListAsync();

                foreach (var theCase in workOrdersToRetract)
                {
                    try {
                        await _sdkCore.CaseDelete(theCase.CaseId);
                    } catch (Exception e) {
                        Console.WriteLine(e);
                        Console.WriteLine($"faild to delete case {theCase.CaseId}");
                    }
                    await theCase.Delete(backendConfigurationPnDbContext);
                }

                var parentCase = await backendConfigurationPnDbContext.WorkorderCases
                    .FirstAsync(x => x.Id == workOrderCase.ParentWorkorderCaseId);

                if (parentCase.CaseId != 0 && parentCase.ParentWorkorderCaseId != null)
                {
                    try
                    {
                        await _sdkCore.CaseDelete(parentCase.CaseId);
                    } catch (Exception e) {
                        Console.WriteLine(e);
                        Console.WriteLine($"faild to delete case {parentCase.CaseId}");
                    }
                }
                await parentCase.Delete(backendConfigurationPnDbContext);
            }
        }

        private async Task<string> InsertImageToPdf(string imageName, string itemsHtml, int imageSize, int imageWidth, string basePicturePath)
        {
            if (imageName.Contains("GH"))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceString = $"ServiceBackendConfigurationPlugin.Resources.GHSHazardPictogram.{imageName}.jpg";
                // using var FileStream FileStream = new FileStream()
                await using var resourceStream = assembly.GetManifestResourceStream(resourceString);
                // using var reader = new StreamReader(resourceStream ?? throw new InvalidOperationException($"{nameof(resourceStream)} is null"));
                // html = await reader.ReadToEndAsync();
                // MemoryStream memoryStream = new MemoryStream();
                // await resourceStream.CopyToAsync(memoryStream);
                using var image = new MagickImage(resourceStream);
                var profile = image.GetExifProfile();
                // Write all values to the console
                try
                {
                    foreach (var value in profile.Values)
                    {
                        Console.WriteLine("{0}({1}): {2}", value.Tag, value.DataType, value.ToString());
                    }
                } catch (Exception)
                {
                    // Console.WriteLine(e);
                }
                // image.Rotate(90);
                var base64String = image.ToBase64();
                itemsHtml +=
                    $@"<p><img src=""data:image/png;base64,{base64String}"" width=""{imageWidth}px"" alt="""" /></p>";

                // await stream.DisposeAsync();
            }
            else
            {
                var storageResult = await _sdkCore.GetFileFromS3Storage(imageName);
                var stream = storageResult.ResponseStream;

                using var image = new MagickImage(stream);
                var profile = image.GetExifProfile();
                // Write all values to the console
                try
                {
                    foreach (var value in profile.Values)
                    {
                        Console.WriteLine("{0}({1}): {2}", value.Tag, value.DataType, value.ToString());
                    }
                } catch (Exception)
                {
                    // Console.WriteLine(e);
                }
                image.Rotate(90);
                var base64String = image.ToBase64();
                itemsHtml +=
                    $@"<p><img src=""data:image/png;base64,{base64String}"" width=""{imageWidth}px"" alt="""" /></p>";

                await stream.DisposeAsync();
            }

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
                    imagesHtml = await InsertImageToPdf(imagesName, imagesHtml, 700, 650, basePicturePath);
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

        private async Task<Folder> getTopFolderName(int folderId, MicrotingDbContext dbContext)
        {
            var result = await dbContext.Folders.FirstOrDefaultAsync(y => y.Id == folderId);
            if (result.ParentId != null)
            {
                result = await getTopFolderName((int)result.ParentId, dbContext);
            }
            return result;
        }

        private async Task<Planning> CreateItemPlanningObject(int eformId, string eformName, int folderId,
            AreaRulePlanningModel areaRulePlanningModel, AreaRule areaRule)
        {
            var _backendConfigurationPnDbContext = _backendConfigurationDbContextHelper.GetDbContext();
            var propertyItemPlanningTagId = await _backendConfigurationPnDbContext.Properties
                .Where(x => x.Id == areaRule.PropertyId)
                .Select(x => x.ItemPlanningTagId)
                .FirstAsync();
            return new Planning
            {
                CreatedByUserId = 0,
                Enabled = areaRulePlanningModel.Status,
                RelatedEFormId = eformId,
                RelatedEFormName = eformName,
                SdkFolderId = folderId,
                DaysBeforeRedeploymentPushMessageRepeat = false,
                DaysBeforeRedeploymentPushMessage = 5,
                PushMessageOnDeployment = areaRulePlanningModel.SendNotifications,
                StartDate = areaRulePlanningModel.StartDate,
                IsLocked = true,
                IsEditable = false,
                IsHidden = true,
                PlanningSites = areaRulePlanningModel.AssignedSites
                    .Select(x =>
                        new Microting.ItemsPlanningBase.Infrastructure.Data.Entities.PlanningSite
                        {
                            SiteId = x.SiteId,
                        })
                    .ToList(),
                PlanningsTags = new List<PlanningsTags>
                {
                    new() {PlanningTagId = areaRule.Area.ItemPlanningTagId,},
                    new() {PlanningTagId = propertyItemPlanningTagId,},
                }
            };
        }

                private AreaRulePlanning CreateAreaRulePlanningObject(AreaRulePlanningModel areaRulePlanningModel,
            AreaRule areaRule, int planningId, int folderId)
        {
            var areaRulePlanning = new AreaRulePlanning
            {
                AreaId = areaRule.AreaId,
                CreatedByUserId = 0,
                UpdatedByUserId = 0,
                StartDate = areaRulePlanningModel.StartDate,
                Status = areaRulePlanningModel.Status,
                SendNotifications = areaRulePlanningModel.SendNotifications,
                AreaRuleId = areaRulePlanningModel.RuleId,
                ItemPlanningId = planningId,
                FolderId = folderId,
                PropertyId = areaRulePlanningModel.PropertyId,
                PlanningSites = areaRulePlanningModel.AssignedSites.Select(x => new PlanningSite
                {
                    SiteId = x.SiteId,
                    CreatedByUserId = 0,
                    UpdatedByUserId = 0,
                    AreaId = areaRule.AreaId,
                    AreaRuleId = areaRule.Id
                }).ToList(),
                ComplianceEnabled = areaRulePlanningModel.ComplianceEnabled,
            };
            if (areaRulePlanningModel.TypeSpecificFields != null)
            {
                areaRulePlanning.DayOfMonth = areaRulePlanningModel.TypeSpecificFields.DayOfMonth == 0
                    ? 1
                    : areaRulePlanningModel.TypeSpecificFields.DayOfMonth;
                areaRulePlanning.DayOfWeek = areaRulePlanningModel.TypeSpecificFields.DayOfWeek == 0
                    ? 1
                    : areaRulePlanningModel.TypeSpecificFields.DayOfWeek;
                areaRulePlanning.HoursAndEnergyEnabled = areaRulePlanningModel.TypeSpecificFields.HoursAndEnergyEnabled;
                areaRulePlanning.EndDate = areaRulePlanningModel.TypeSpecificFields.EndDate;
                areaRulePlanning.RepeatEvery = areaRulePlanningModel.TypeSpecificFields.RepeatEvery;
                areaRulePlanning.RepeatType = areaRulePlanningModel.TypeSpecificFields.RepeatType;
            }

            if (areaRule.Type != null)
            {
                areaRulePlanning.Type = (AreaRuleT2TypesEnum)areaRule.Type;
            }

            if (areaRule.Alarm != null)
            {
                areaRulePlanning.Alarm = (AreaRuleT2AlarmsEnum)areaRule.Alarm;
            }

            return areaRulePlanning;
        }

    }
}