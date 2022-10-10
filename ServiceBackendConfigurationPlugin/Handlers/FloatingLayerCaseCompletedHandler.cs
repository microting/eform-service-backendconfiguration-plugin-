using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microting.eForm.Infrastructure;
using Microting.eForm.Infrastructure.Constants;
using Microting.eForm.Infrastructure.Data.Entities;
using Microting.eForm.Infrastructure.Models;
using Microting.ItemsPlanningBase.Infrastructure.Data.Entities;
using Rebus.Handlers;
using ServiceBackendConfigurationPlugin.Infrastructure.Helpers;
using ServiceBackendConfigurationPlugin.Messages;
using ServiceBackendConfigurationPlugin.Resources;

namespace ServiceBackendConfigurationPlugin.Handlers;

public class FloatingLayerCaseCompletedHandler : IHandleMessages<FloatingLayerCaseCompleted>
{
    private readonly eFormCore.Core _sdkCore;
    private readonly ItemsPlanningDbContextHelper _itemsPlanningDbContextHelper;
    private readonly BackendConfigurationDbContextHelper _backendConfigurationDbContextHelper;

    public FloatingLayerCaseCompletedHandler(eFormCore.Core sdkCore, ItemsPlanningDbContextHelper itemsPlanningDbContextHelper, BackendConfigurationDbContextHelper backendConfigurationDbContextHelper)
    {
        _sdkCore = sdkCore;
        _itemsPlanningDbContextHelper = itemsPlanningDbContextHelper;
        _backendConfigurationDbContextHelper = backendConfigurationDbContextHelper;
    }

    public async Task Handle(FloatingLayerCaseCompleted message)
    {
        await using var sdkDbContext = _sdkCore.DbContextHelper.GetDbContext();
        await using var
            itemsPlanningPnDbContext = _itemsPlanningDbContextHelper.GetDbContext();
        await using var backendConfigurationPnDbContext =
            _backendConfigurationDbContextHelper.GetDbContext();

        var eformQuery = sdkDbContext.CheckListTranslations
            .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
            .AsQueryable();

        var eformIdForControlFloatingLayer = await eformQuery
            .Where(x => x.Text == "03. Control floating layer")
            .Select(x => x.CheckListId)
            .FirstOrDefaultAsync();

        var dbCase = await sdkDbContext.Cases
                         .AsNoTracking()
                         .FirstOrDefaultAsync(x => x.Id == message.CaseId) ??
                     await sdkDbContext.Cases
                         .FirstOrDefaultAsync(x => x.MicrotingCheckUid == message.CheckId);

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
                var folder = await GetTopFolderName((int) planning.SdkFolderId, sdkDbContext);
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


    private async Task<Folder> GetTopFolderName(int folderId, MicrotingDbContext dbContext)
    {
        var result = await dbContext.Folders.FirstOrDefaultAsync(y => y.Id == folderId);
        if (result.ParentId != null)
        {
            result = await GetTopFolderName((int)result.ParentId, dbContext);
        }
        return result;
    }
}