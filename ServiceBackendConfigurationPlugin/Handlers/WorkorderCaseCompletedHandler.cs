using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.EntityFrameworkCore;
using Microting.eForm.Helpers;
using Microting.eForm.Infrastructure.Constants;
using Microting.eForm.Infrastructure.Models;
using Microting.eFormApi.BasePn.Infrastructure.Consts;
using Microting.EformBackendConfigurationBase.Infrastructure.Data.Entities;
using Microting.EformBackendConfigurationBase.Infrastructure.Enum;
using Rebus.Handlers;
using ServiceBackendConfigurationPlugin.Infrastructure.Helpers;
using ServiceBackendConfigurationPlugin.Messages;
using ServiceBackendConfigurationPlugin.Resources;
using KeyValuePair = Microting.eForm.Dto.KeyValuePair;

namespace ServiceBackendConfigurationPlugin.Handlers;

public class WorkOrderCaseCompletedHandler : IHandleMessages<WorkOrderCaseCompleted>
{
    private readonly eFormCore.Core _sdkCore;
    private readonly ItemsPlanningDbContextHelper _itemsPlanningDbContextHelper;
    private readonly BackendConfigurationDbContextHelper _backendConfigurationDbContextHelper;

    public WorkOrderCaseCompletedHandler(BackendConfigurationDbContextHelper backendConfigurationDbContextHelper, ItemsPlanningDbContextHelper itemsPlanningDbContextHelper, eFormCore.Core sdkCore)
    {
        _backendConfigurationDbContextHelper = backendConfigurationDbContextHelper;
        _itemsPlanningDbContextHelper = itemsPlanningDbContextHelper;
        _sdkCore = sdkCore;
    }

    public async Task Handle(WorkOrderCaseCompleted message)
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

        var eformIdForNewTasks = await sdkDbContext.CheckLists
            .Where(x => x.OriginalId == "142663new2")
            .Select(x => x.Id)
            .FirstAsync();

        var eformIdForOngoingTasks = await sdkDbContext.CheckLists
            .Where(x => x.OriginalId == "142664new2")
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        // var eformIdForCompletedTasks = await eformQuery
        //     .Where(x => x.Text == "03. Completed task")
        //     .Select(x => x.CheckListId)
        //     .FirstAsync();

        var dbCase = await sdkDbContext.Cases
                         .AsNoTracking()
                         .FirstOrDefaultAsync(x => x.Id == message.CaseId) ??
                     await sdkDbContext.Cases
                         .FirstOrDefaultAsync(x => x.MicrotingCheckUid == message.CheckId);

        var workOrderCase = await backendConfigurationPnDbContext.WorkorderCases
            .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
            .Where(x => x.CaseId == message.MicrotingUId)
            .Include(x => x.ParentWorkorderCase)
            .Include(x => x.PropertyWorker)
            .ThenInclude(x => x.Property)
            .ThenInclude(x => x.PropertyWorkers)
            .ThenInclude(x => x.WorkorderCases)
            .FirstOrDefaultAsync();

        if (eformIdForNewTasks == dbCase.CheckListId && workOrderCase != null)
        {
            var property = workOrderCase.PropertyWorker.Property;

            var propertyWorkers = property.PropertyWorkers
                .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                .ToList();

            var cls = await sdkDbContext.Cases
                .Where(x => x.MicrotingUid == message.MicrotingUId)
                .OrderBy(x => x.DoneAt)
                .Include(x => x.Site)
                .LastAsync();

            var language = await sdkDbContext.Languages.FirstOrDefaultAsync(x => x.Id == cls.Site.LanguageId) ??
                           await sdkDbContext.Languages.FirstOrDefaultAsync(x =>
                               x.LanguageCode == LocaleNames.Danish);

            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(language.LanguageCode);

            var priorityFiled =
                await sdkDbContext.Fields.FirstAsync(x =>
                    x.CheckListId == eformIdForNewTasks + 1 && x.DisplayIndex == 1);
            var priorityFieldValue =
                await sdkDbContext.FieldValues.FirstOrDefaultAsync(x =>
                    x.FieldId == priorityFiled.Id && x.CaseId == dbCase.Id);

            var areaField =
                await sdkDbContext.Fields.FirstAsync(x =>
                    x.CheckListId == eformIdForNewTasks + 1 && x.DisplayIndex == 2);
            var areaFieldValue =
                await sdkDbContext.FieldValues.FirstOrDefaultAsync(x =>
                    x.FieldId == areaField.Id && x.CaseId == dbCase.Id);

            var pictureField =
                await sdkDbContext.Fields.FirstAsync(x =>
                    x.CheckListId == eformIdForNewTasks + 1 && x.DisplayIndex == 3);
            var pictureFieldValues = await sdkDbContext.FieldValues
                .Where(x => x.FieldId == pictureField.Id && x.CaseId == dbCase.Id).ToListAsync();

            var commentField =
                await sdkDbContext.Fields.FirstAsync(x =>
                    x.CheckListId == eformIdForNewTasks + 1 && x.DisplayIndex == 4);
            var commentFieldValue =
                await sdkDbContext.FieldValues.FirstAsync(
                    x => x.FieldId == commentField.Id && x.CaseId == dbCase.Id);

            var assignToTexField =
                await sdkDbContext.Fields.FirstAsync(x =>
                    x.CheckListId == eformIdForNewTasks + 1 && x.DisplayIndex == 5);
            var assignedToFieldValue =
                await sdkDbContext.FieldValues.FirstAsync(x =>
                    x.FieldId == assignToTexField.Id && x.CaseId == dbCase.Id);

            var assignToSelectField =
                await sdkDbContext.Fields.FirstAsync(x =>
                    x.CheckListId == eformIdForNewTasks + 1 && x.DisplayIndex == 6);
            var assignedToSelectFieldValue =
                await sdkDbContext.FieldValues.FirstAsync(x =>
                    x.FieldId == assignToSelectField.Id && x.CaseId == dbCase.Id);

            var site = await sdkDbContext.Sites.FirstAsync(x => x.Id == dbCase.SiteId);
            var updatedByName = site.Name;
            // var fieldValues = await _sdkCore.Advanced_FieldValueReadList(new() { cls.Id }, language);

            var areasGroup = await sdkDbContext.EntityGroups
                .FirstAsync(x => x.Id == property.EntitySelectListAreas);
            var deviceUsersGroup = await sdkDbContext.EntityGroups
                .FirstAsync(x => x.Id == property.EntitySelectListDeviceUsers);

            var assignedTo = await sdkDbContext.EntityItems.FirstAsync(x =>
                x.EntityGroupId == deviceUsersGroup.Id && x.Id == int.Parse(assignedToSelectFieldValue.Value));
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
                    x.ParentWorkorderCaseId == workOrderCase.Id
                    && x.WorkflowState != Constants.WorkflowStates.Removed
                    && x.CaseId == dbCase.MicrotingUid
                    && x.PropertyWorkerId == workOrderCase.PropertyWorkerId
                    && x.SelectedAreaName == areaName
                    && x.CreatedByName == cls.Site.Name
                    && x.CreatedByText == assignedToFieldValue.Value
                    && x.CaseStatusesEnum == CaseStatusesEnum.Ongoing
                    && x.Description == commentFieldValue.Value))
            {
                return;
            }

            var newWorkOrderCase = new WorkorderCase
            {
                ParentWorkorderCaseId = workOrderCase.Id,
                CaseId = 0,
                PropertyWorkerId = workOrderCase.PropertyWorkerId,
                SelectedAreaName = areaName,
                CreatedByName = cls.Site.Name,
                CreatedByText = assignedToFieldValue.Value,
                CaseStatusesEnum = CaseStatusesEnum.Ongoing,
                Description = commentFieldValue.Value,
                CaseInitiated = DateTime.UtcNow,
                LeadingCase = false,
                Priority = priorityFieldValue != null ? priorityFieldValue.Value : "4",
            };
            await newWorkOrderCase.Create(backendConfigurationPnDbContext);

            var picturesOfTasks = new List<string>();
            foreach (var pictureFieldValue in pictureFieldValues.Where(pictureFieldValue =>
                         pictureFieldValue.UploadedDataId != null))
            {
                var uploadedData =
                    await sdkDbContext.UploadedDatas.FirstAsync(x => x.Id == pictureFieldValue.UploadedDataId);
                var workOrderCaseImage = new WorkorderCaseImage
                {
                    WorkorderCaseId = newWorkOrderCase.Id,
                    UploadedDataId = (int) pictureFieldValue.UploadedDataId!
                };

                picturesOfTasks.Add($"{uploadedData.Id}_700_{uploadedData.Checksum}{uploadedData.Extension}");
                await workOrderCaseImage.Create(backendConfigurationPnDbContext);
            }

            var hash = await GeneratePdf(picturesOfTasks, (int) cls.SiteId!);

            var priorityText = "";

            switch (workOrderCase.Priority)
            {
                case "1":
                    priorityText = $"<strong>{Translations.Priority}:</strong> {Translations.Urgent}<br><br>";
                    break;
                case "2":
                    priorityText = $"<strong>{Translations.Priority}:</strong> {Translations.High}<br><br>";
                    break;
                case "3":
                    priorityText = $"<strong>{Translations.Priority}:</strong> {Translations.Medium}<br><br>";
                    break;
                case "4":
                    priorityText = $"<strong>{Translations.Priority}:</strong> {Translations.Low}<br><br>";
                    break;
            }

            var label = $"<strong>{Translations.AssignedTo}:</strong> {assignedTo.Name}<br>" +
                        $"<strong>{Translations.Location}:</strong> {property.Name}<br>" +
                        (!string.IsNullOrEmpty(areaName)
                            ? $"<strong>{Translations.Area}:</strong> {areaName}<br>"
                            : "") +
                        $"<strong>{Translations.Description}:</strong> {commentFieldValue.Value}<br>" +
                        priorityText +
                        $"<strong>{Translations.CreatedBy}:</strong> {cls.Site.Name}<br>" +
                        (!string.IsNullOrEmpty(assignedToFieldValue.Value)
                            ? $"<strong>{Translations.CreatedBy}:</strong> {assignedToFieldValue.Value}<br>"
                            : "") +
                        $"<strong>{Translations.CreatedDate}:</strong> {newWorkOrderCase.CaseInitiated: dd.MM.yyyy}<br><br>" +
                        $"<strong>{Translations.Status}:</strong> {Translations.Ongoing}<br><br>";

            var pushMessageTitle = !string.IsNullOrEmpty(areaName)
                ? $"{property.Name}; {areaName}"
                : $"{property.Name}";
            var pushMessageBody = $"{commentFieldValue.Value}";


            // deploy eform to ongoing status
            await DeployWorkOrderEform(propertyWorkers, eformIdForOngoingTasks,
                property, label, CaseStatusesEnum.Ongoing, newWorkOrderCase,
                commentFieldValue.Value, int.Parse(deviceUsersGroup.MicrotingUid), hash,
                assignedTo.Name, pushMessageBody, pushMessageTitle, updatedByName);
        }else if (eformIdForOngoingTasks == dbCase.CheckListId && workOrderCase != null)
        {
            var property = workOrderCase.PropertyWorker.Property;

            var propertyWorkers = property.PropertyWorkers
                .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                .ToList();

            // var folderIdForOngoingTasks = await sdkDbContext.Folders
            //     .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
            //     .Where(x => x.ParentId == property.FolderIdForTasks)
            //     .Where(x => x.FolderTranslations.Any(y => y.Name == "02. Ongoing tasks"))
            //     .Select(x => x.Id)
            //     .FirstAsync();
            //
            // var folderIdForCompletedTasks = await sdkDbContext.Folders
            //     .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
            //     .Where(x => x.ParentId == property.FolderIdForTasks)
            //     .Where(x => x.FolderTranslations.Any(y => y.Name == "03. Completed tasks"))
            //     .Select(x => x.Id)
            //     .FirstAsync();

            var cls = await sdkDbContext.Cases
                .Where(x => x.MicrotingUid == message.MicrotingUId)
                .OrderBy(x => x.DoneAt)
                .Include(x => x.Site)
                .LastAsync();

            var language = await sdkDbContext.Languages.FirstOrDefaultAsync(x => x.Id == cls.Site.LanguageId) ??
                           await sdkDbContext.Languages.FirstAsync(x => x.LanguageCode == LocaleNames.Danish);

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

            var priorityFiled =
                await sdkDbContext.Fields.FirstAsync(x =>
                    x.CheckListId == eformIdForNewTasks + 1 && x.DisplayIndex == 4);
            var priorityFieldValue =
                await sdkDbContext.FieldValues.FirstAsync(x =>
                    x.FieldId == priorityFiled.Id && x.CaseId == dbCase.Id);

            var assignToSelectField =
                await sdkDbContext.Fields.FirstAsync(x => x.CheckListId == eformIdForOngoingTasks + 1 && x.DisplayIndex == 5);
            var assignedToSelectFieldValue = await sdkDbContext.FieldValues.FirstAsync(x => x.FieldId == assignToSelectField.Id && x.CaseId == dbCase.Id);

            var statusField =
                await sdkDbContext.Fields.FirstAsync(x => x.CheckListId == eformIdForOngoingTasks + 1 && x.DisplayIndex == 6);
            var statusFieldValue = await sdkDbContext.FieldValues.FirstAsync(x => x.FieldId == statusField.Id && x.CaseId == dbCase.Id);

            var assignedTo = await sdkDbContext.EntityItems.FirstAsync(x => x.EntityGroupId == deviceUsersGroup.Id && x.Id == int.Parse(assignedToSelectFieldValue.Value));
            // var area = await sdkDbContext.EntityItems.FirstAsync(x => x.EntityGroupId == areasGroup.Id && x.Id == int.Parse(areaId));
            // var textStatus = statusFieldValue.Value == "1" ? Translations.Ongoing : Translations.Completed;
            var textStatus = "";

            workOrderCase.Priority = priorityFieldValue.Value;

            switch (statusFieldValue.Value)
            {
                case "1":
                    textStatus = Translations.Ongoing;
                    workOrderCase.CaseStatusesEnum = CaseStatusesEnum.Ongoing;
                    break;
                case "2":
                    textStatus = Translations.Completed;
                    workOrderCase.CaseStatusesEnum = CaseStatusesEnum.Completed;
                    break;
                case "3":
                    textStatus = Translations.Ordered;
                    workOrderCase.CaseStatusesEnum = CaseStatusesEnum.Ordered;
                    break;
                case "4":
                    textStatus = Translations.Awaiting;
                    workOrderCase.CaseStatusesEnum = CaseStatusesEnum.Awaiting;
                    break;
            }
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
                        WorkorderCaseId = workOrderCase.Id,
                        UploadedDataId = (int) pictureFieldValue.UploadedDataId!
                    };

                    picturesOfTasks.Add($"{uploadedData.Id}_700_{uploadedData.Checksum}{uploadedData.Extension}");
                    await workOrderCaseImage.Create(backendConfigurationPnDbContext);
                }
            }
            var parentCaseImages = await backendConfigurationPnDbContext.WorkorderCaseImages.Where(x => x.WorkorderCaseId == workOrderCase.ParentWorkorderCaseId).ToListAsync();

            foreach (var workorderCaseImage in parentCaseImages)
            {
                var uploadedData = await sdkDbContext.UploadedDatas.FirstAsync(x => x.Id == workorderCaseImage.UploadedDataId);
                picturesOfTasks.Add($"{uploadedData.Id}_700_{uploadedData.Checksum}{uploadedData.Extension}");
                var workOrderCaseImage = new WorkorderCaseImage
                {
                    WorkorderCaseId = workOrderCase.Id,
                    UploadedDataId = (int)uploadedData.Id
                };
                await workOrderCaseImage.Create(backendConfigurationPnDbContext);
            }

            var hash = await GeneratePdf(picturesOfTasks, (int)cls.SiteId);

            var label = $"<strong>{Translations.AssignedTo}:</strong> {assignedTo.Name}<br>";

            var pushMessageTitle = !string.IsNullOrEmpty(workOrderCase.SelectedAreaName) ? $"{property.Name}; {workOrderCase.SelectedAreaName}" : $"{property.Name}";
            var pushMessageBody = $"{commentFieldValue.Value}";
            var deviceUsersGroupUid = await sdkDbContext.EntityGroups
                .Where(x => x.Id == property.EntitySelectListDeviceUsers)
                .Select(x => x.MicrotingUid)
                .FirstAsync();
            if (textStatus != "Afsluttet")
            {
                var priorityText = "";

                switch (workOrderCase.Priority)
                {
                    case "1":
                        priorityText = $"<strong>{Translations.Priority}:</strong> {Translations.Urgent}<br><br>";
                        break;
                    case "2":
                        priorityText = $"<strong>{Translations.Priority}:</strong> {Translations.High}<br><br>";
                        break;
                    case "3":
                        priorityText = $"<strong>{Translations.Priority}:</strong> {Translations.Medium}<br><br>";
                        break;
                    case "4":
                        priorityText = $"<strong>{Translations.Priority}:</strong> {Translations.Low}<br><br>";
                        break;
                }
                label += $"<strong>{Translations.Location}:</strong> {property.Name}<br>" +
                         (!string.IsNullOrEmpty(workOrderCase.SelectedAreaName)
                             ? $"<strong>{Translations.Area}:</strong> {workOrderCase.SelectedAreaName}<br>"
                             : "") +
                         $"<strong>{Translations.Description}:</strong> {commentFieldValue.Value}<br>" +
                         priorityText +
                         $"<strong>{Translations.CreatedBy}:</strong> {workOrderCase.CreatedByName}<br>" +
                         (!string.IsNullOrEmpty(workOrderCase.CreatedByText)
                             ? $"<strong>{Translations.CreatedBy}:</strong> {workOrderCase.CreatedByText}<br>"
                             : "") +
                         $"<strong>{Translations.CreatedDate}:</strong> {workOrderCase.CaseInitiated: dd.MM.yyyy}<br><br>" +
                         $"<strong>{Translations.LastUpdatedBy}:</strong> {cls.Site.Name}<br>" +
                         $"<strong>{Translations.LastUpdatedDate}:</strong> {DateTime.UtcNow: dd.MM.yyyy}<br><br>" +
                         $"<strong>{Translations.Status}:</strong> {textStatus}<br><br>";
                // retract eform
                await RetractEform(workOrderCase);
                // deploy eform to ongoing status
                await DeployWorkOrderEform(propertyWorkers, eformIdForOngoingTasks, property, label,  CaseStatusesEnum.Ongoing, workOrderCase, commentFieldValue.Value, int.Parse(deviceUsersGroupUid), hash, assignedTo.Name, pushMessageBody, pushMessageTitle, updatedByName);
            }
            else
            {
                // label = $"<strong>{Translations.Location}:</strong> {property.Name}<br>" +
                //         (!string.IsNullOrEmpty(workOrderCase.SelectedAreaName)
                //             ? $"<strong>{Translations.Area}:</strong> {workOrderCase.SelectedAreaName}<br>"
                //             : "") +
                //         $"<strong>{Translations.Description}:</strong> {commentFieldValue.Value}<br><br>" +
                //         $"<strong>{Translations.CreatedBy}:</strong> {workOrderCase.CreatedByName}<br>" +
                //         (!string.IsNullOrEmpty(workOrderCase.CreatedByText)
                //             ? $"<strong>{Translations.CreatedBy}:</strong> {workOrderCase.CreatedByText}<br>"
                //             : "") +
                //         $"<strong>{Translations.CreatedDate}:</strong> {workOrderCase.CaseInitiated: dd.MM.yyyy}<br><br>" +
                //         $"<strong>{Translations.LastUpdatedBy}:</strong> {cls.Site.Name}<br>" +
                //         $"<strong>{Translations.LastUpdatedDate}:</strong> {DateTime.UtcNow: dd.MM.yyyy}<br><br>" +
                //         $"<strong>{Translations.Status}:</strong> {textStatus}<br><br>";
                // retract eform
                await RetractEform(workOrderCase);
                // deploy eform to completed status
                // await DeployWorkOrderEform(propertyWorkers, eformIdForCompletedTasks, property, label, CaseStatusesEnum.Completed, workOrderCase, commentFieldValue.Value, null, hash, assignedTo.Name, pushMessageBody, pushMessageTitle, updatedByName);
            }
        }
    }

    private async Task DeployWorkOrderEform(
        List<PropertyWorker> propertyWorkers,
        int eformId,
        Property property,
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
        int? folderId = null;
        await using var sdkDbContext = _sdkCore.DbContextHelper.GetDbContext();
        await using var backendConfigurationPnDbContext =
            _backendConfigurationDbContextHelper.GetDbContext();
        var i = 0;
        foreach (var propertyWorker in propertyWorkers)
        {
            var priorityText = "";

            var site = await sdkDbContext.Sites.FirstAsync(x => x.Id == propertyWorker.WorkerId);
            switch (workorderCase.Priority)
            {
                case "1":
                    priorityText = $"<strong>{Translations.Priority}:</strong> {Translations.Urgent}<br>";
                    break;
                case "2":
                    priorityText = $"<strong>{Translations.Priority}:</strong> {Translations.High}<br>";
                    break;
                case "3":
                    priorityText = $"<strong>{Translations.Priority}:</strong> {Translations.Medium}<br>";
                    break;
                case "4":
                    priorityText = $"<strong>{Translations.Priority}:</strong> {Translations.Low}<br>";
                    break;
            }

            var textStatus = "";

            switch (workorderCase.CaseStatusesEnum)
            {
                case CaseStatusesEnum.Ongoing:
                    textStatus = Translations.Ongoing;
                    break;
                case CaseStatusesEnum.Completed:
                    textStatus = Translations.Completed;
                    break;
                case CaseStatusesEnum.Awaiting:
                    textStatus = Translations.Awaiting;
                    break;
                case CaseStatusesEnum.Ordered:
                    textStatus = Translations.Ordered;
                    break;
            }

            var assignedTo = site.Name == siteName ? "" : $"<strong>{Translations.AssignedTo}:</strong> {siteName}<br>";

            var areaName = !string.IsNullOrEmpty(workorderCase.SelectedAreaName)
                ? $"<strong>{Translations.Area}:</strong> {workorderCase.SelectedAreaName}<br>"
                : "";

            var outerDescription = $"<strong>{Translations.Location}:</strong> {property.Name}<br>" +
                                   areaName +
                                   $"<strong>{Translations.Description}:</strong> {newDescription}<br>" +
                                   priorityText +
                                   assignedTo +
                                   $"<strong>{Translations.Status}:</strong> {textStatus}<br><br>";
            var siteLanguage = await sdkDbContext.Languages.FirstAsync(x => x.Id == site.LanguageId);
            var mainElement = await _sdkCore.ReadeForm(eformId, siteLanguage);
            mainElement.Label = " ";
            mainElement.ElementList[0].QuickSyncEnabled = true;
            mainElement.EnableQuickSync = true;
            mainElement.ElementList[0].Label = " ";
            mainElement.ElementList[0].Description.InderValue = outerDescription.Replace("\n", "<br>");
            if (status == CaseStatusesEnum.Completed || site.Name == siteName)
            {
                DateTime startDate = new DateTime(2020, 1, 1);
                mainElement.DisplayOrder = (int)(startDate - DateTime.UtcNow).TotalSeconds;
            }
            if (site.Name == siteName)
            {
                mainElement.CheckListFolderName = sdkDbContext.Folders.First(x => x.Id == (workorderCase.Priority != "1" ? property.FolderIdForOngoingTasks : property.FolderIdForTasks))
                    .MicrotingUid.ToString();
                folderId = property.FolderIdForOngoingTasks;
                mainElement.PushMessageTitle = pushMessageTitle;
                mainElement.PushMessageBody = pushMessageBody;
            }
            else
            {
                folderId = property.FolderIdForCompletedTasks;
                mainElement.CheckListFolderName = sdkDbContext.Folders.First(x => x.Id == property.FolderIdForCompletedTasks)
                    .MicrotingUid.ToString();
            }
            // TODO uncomment when new app has been released.
            ((DataElement)mainElement.ElementList[0]).DataItemList[0].Description.InderValue = description.Replace("\n", "<br>");
            ((DataElement)mainElement.ElementList[0]).DataItemList[0].Label = " ";
            ((DataElement)mainElement.ElementList[0]).DataItemList[0].Color = Constants.FieldColors.Yellow;
            ((ShowPdf) ((DataElement) mainElement.ElementList[0]).DataItemList[1]).Value = pdfHash;

            List<KeyValuePair> kvpList = ((SingleSelect) ((DataElement) mainElement.ElementList[0]).DataItemList[4]).KeyValuePairList;
            var newKvpList = new List<KeyValuePair>();
            foreach (var keyValuePair in kvpList)
            {
                if (keyValuePair.Key == workorderCase.Priority)
                {
                    keyValuePair.Selected = true;
                }
                newKvpList.Add(keyValuePair);
            }
            ((SingleSelect) ((DataElement) mainElement.ElementList[0]).DataItemList[4]).KeyValuePairList = newKvpList;

            if (deviceUsersGroupId != null)
            {
                ((EntitySelect)((DataElement)mainElement.ElementList[0]).DataItemList[5]).Source = (int)deviceUsersGroupId;
                ((EntitySelect)((DataElement)mainElement.ElementList[0]).DataItemList[5]).Mandatory = true;
                ((Comment)((DataElement)mainElement.ElementList[0]).DataItemList[3]).Value = newDescription;
                ((SingleSelect)((DataElement)mainElement.ElementList[0]).DataItemList[6]).Mandatory = true;
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
                LeadingCase = i == 0,
                Priority = workorderCase.Priority
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
}