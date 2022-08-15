/*
The MIT License (MIT)

Copyright (c) 2007 - 2021 Microting A/S

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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChemicalsBase.Infrastructure;
using ChemicalsBase.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microting.eForm.Infrastructure;
using Microting.eForm.Infrastructure.Constants;
using Microting.eForm.Infrastructure.Data.Entities;
using Microting.eForm.Infrastructure.Models;
using Microting.EformAngularFrontendBase.Infrastructure.Data;
using Microting.eFormApi.BasePn.Infrastructure.Helpers;
using Microting.EformBackendConfigurationBase.Infrastructure.Data;
using Microting.EformBackendConfigurationBase.Infrastructure.Data.Entities;
using Microting.EformBackendConfigurationBase.Infrastructure.Enum;
using Microting.ItemsPlanningBase.Infrastructure.Data;
using Microting.ItemsPlanningBase.Infrastructure.Data.Entities;
using Microting.ItemsPlanningBase.Infrastructure.Enums;
using Rebus.Bus;
using SendGrid;
using SendGrid.Helpers.Mail;
using ServiceBackendConfigurationPlugin.Infrastructure.Helpers;
using ServiceBackendConfigurationPlugin.Infrastructure.Models.AreaRules;
using PlanningSite = Microting.EformBackendConfigurationBase.Infrastructure.Data.Entities.PlanningSite;

namespace ServiceBackendConfigurationPlugin.Scheduler.Jobs
{
    public class SearchListJob : IJob
    {
        private readonly BackendConfigurationDbContextHelper _backendConfigurationDbContextHelper;
        private readonly BackendConfigurationPnDbContext _backendConfigurationDbContext;
        private readonly ChemicalDbContextHelper _chemicalDbContextHelper;
        private readonly eFormCore.Core _core;
        private readonly MicrotingDbContext _sdkDbContext;
        private readonly ItemsPlanningPnDbContext _itemsPlanningPnDbContext;
        private readonly BaseDbContext _baseDbContext;

        public SearchListJob(
            BackendConfigurationDbContextHelper dbContextHelper, ChemicalDbContextHelper chemicalDbContextHelper, eFormCore.Core core, ItemsPlanningDbContextHelper itemsPlanningDbContextHelper, BaseDbContext baseDbContext)
        {
            _core = core;
            _baseDbContext = baseDbContext;
            _chemicalDbContextHelper = chemicalDbContextHelper;
            _backendConfigurationDbContextHelper = dbContextHelper;
            _backendConfigurationDbContext = _backendConfigurationDbContextHelper.GetDbContext();
            _sdkDbContext = _core.DbContextHelper.GetDbContext();
            _itemsPlanningPnDbContext = itemsPlanningDbContextHelper.GetDbContext();
        }

        public async Task Execute()
        {
            await ExecuteUpdateProperties();
        }

        private async Task ExecuteUpdateProperties()
        {

            // if (DateTime.UtcNow.Hour > 19)
            // {
            //     Log.LogEvent(
            //         $"SearchListJob.Task: ExecutePush The current hour is bigger than the end time of 3, so ending processing");
            //     return;
            // }
            //
            // if (DateTime.UtcNow.Hour < 22)
            // {
            //     Log.LogEvent(
            //         $"SearchListJob.Task: ExecutePush The current hour is smaller than the start time of 3, so ending processing");
            //     return;
            // }
            var sendGridKey =
                _baseDbContext.ConfigurationValues.Single(x => x.Id == "EmailSettings:SendGridKey");
            if (DateTime.UtcNow.Hour is > 4 and < 6)
            {
                Log.LogEvent("SearchListJob.Task: SearchListJob.Execute got called");
                var url = "https://chemicalbase.microting.com/get-all-chemicals";
                var client = new HttpClient();
                var response = await client.GetAsync(url).ConfigureAwait(false);
                var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                };
                List<Chemical> chemicals = JsonSerializer.Deserialize<List<Chemical>>(result, options);

                if (chemicals != null)
                {
                    int count = chemicals.Count;
                    int i = 0;
                    var parallelOptions = new ParallelOptions()
                    {
                        MaxDegreeOfParallelism = 20
                    };
                    await Parallel.ForEachAsync(chemicals, parallelOptions, async (chemical, ct) =>
                    {
                        var chemicalsDbContext = _chemicalDbContextHelper.GetDbContext();
                        if (chemicalsDbContext.Chemicals.AsNoTracking().Any(x => x.RemoteId == chemical.RemoteId))
                        {
                            Console.WriteLine(
                                $"Chemical already exist, so updating : {chemical.Name} no {i} of {count}");
                            var c = await chemicalsDbContext.Chemicals
                                .FirstAsync(x => x.RemoteId == chemical.RemoteId, ct);
                            c.Use = chemical.Use;
                            c.Verified = chemical.Verified;
                            c.AuthorisationDate = chemical.AuthorisationDate;
                            c.AuthorisationExpirationDate = chemical.AuthorisationExpirationDate;
                            c.AuthorisationTerminationDate = chemical.AuthorisationTerminationDate;
                            c.UseAndPossesionDeadline = chemical.UseAndPossesionDeadline;
                            c.PossessionDeadline = chemical.PossessionDeadline;
                            c.SalesDeadline = chemical.SalesDeadline;
                            c.Status = chemical.Status;
                            c.PesticideUser = chemical.PesticideUser;
                            c.FormulationType = chemical.FormulationType;
                            c.FormulationSubType = chemical.FormulationSubType;
                            c.BiocideAuthorisationType = chemical.BiocideAuthorisationType;
                            c.PesticidePossibleUse = chemical.PesticidePossibleUse;
                            c.PesticideProductGroup = chemical.PesticideProductGroup;
                            c.BiocidePossibleUse = chemical.BiocidePossibleUse;
                            c.BiocideSpecialUse = chemical.BiocideSpecialUse;
                            c.BiocideUser = chemical.BiocideUser;
                            c.PestControlType = chemical.PestControlType;
                            // chemical.Id = c.Id;
                            if (!chemicalsDbContext.AuthorisationHolders.Any(x =>
                                    x.RemoteId == chemical.AuthorisationHolder.RemoteId))
                            {
                                var ah = new AuthorisationHolder
                                {
                                    RemoteId = chemical.AuthorisationHolder.RemoteId,
                                    Name = chemical.AuthorisationHolder.Name,
                                    Address = chemical.AuthorisationHolder.Address
                                };
                                await ah.Create(chemicalsDbContext).ConfigureAwait(false);
                                c.AuthorisationHolderId = ah.Id;
                            }
                            else
                            {
                                c.AuthorisationHolderId = chemicalsDbContext.AuthorisationHolders.First(x =>
                                    x.RemoteId == chemical.AuthorisationHolder.RemoteId).Id;
                            }
                            await c.Update(chemicalsDbContext).ConfigureAwait(false);
                        }
                        else
                        {
                            Console.WriteLine(
                                $"Chemical does not exist, so creating : {chemical.Name} no {i} of {count}");
                            await chemical.Create(chemicalsDbContext).ConfigureAwait(false);
                        }

                        i++;
                    });
                }
            }
            if (DateTime.UtcNow.Hour is > 5 and < 7)
            {
                Log.LogEvent("SearchListJob.Task: SearchListJob.Execute got called");
                var properties = await _backendConfigurationDbContext.Properties
                    .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed).ToListAsync();

                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 20
                };

                await Parallel.ForEachAsync(properties, options, async (property, ct) =>
                {
                    if (property.EntitySearchListChemicalRegNos != null)
                    {
                        var entityGroup = await _core.EntityGroupRead(property.EntitySearchListChemicals.ToString())
                            .ConfigureAwait(false);
                        var entityGroupRegNo = await _core
                            .EntityGroupRead(property.EntitySearchListChemicalRegNos.ToString()).ConfigureAwait(false);
                        var nextItemUid = entityGroup.EntityGroupItemLst.Count;
                        var _chemicalsDbContext = _chemicalDbContextHelper.GetDbContext();
                        var internalChemicals = await _chemicalsDbContext.Chemicals
                            .Where(x => x.AuthorisationExpirationDate > DateTime.Now.AddYears(-10))
                            .Include(x => x.Products).ToListAsync();
                        await Parallel.ForEachAsync(internalChemicals, options, async (chemical, ct) =>
                        {
                            var backendDbContext = _backendConfigurationDbContextHelper.GetDbContext();
                            var sdkDbContext = _core.DbContextHelper.GetDbContext();
                            foreach (Product product in chemical.Products)
                            {
                                if (product.Verified &&
                                    !sdkDbContext.EntityItems.AsNoTracking().Any(x =>
                                        x.EntityGroupId == entityGroup.Id && x.Name == product.Barcode) &&
                                    !string.IsNullOrEmpty(product.Barcode))
                                {
                                    await _core.EntitySearchItemCreate(entityGroup.Id, product.Barcode,
                                        chemical.Name,
                                        nextItemUid.ToString());
                                    nextItemUid++;
                                }
                                else
                                {
                                    Console.WriteLine($"Product already exist, so skipping : {product.Name}");
                                }
                            }

                            if (chemical.Verified && !sdkDbContext.EntityItems.AsNoTracking().Any(x =>
                                    x.EntityGroupId == entityGroupRegNo.Id && x.Name == chemical.RegistrationNo))
                            {
                                Console.WriteLine(
                                    $"Adding chemical with name : {chemical.Name} and registration no {chemical.RegistrationNo}");
                                await _core.EntitySearchItemCreate(entityGroupRegNo.Id, chemical.RegistrationNo,
                                    chemical.Name,
                                    nextItemUid.ToString());
                                nextItemUid++;
                            }
                            else
                            {
                                Console.WriteLine($"Chemical already exist, so skipping : {chemical.Name}");
                            }

                            var propertyChemical =
                                await backendDbContext.ChemicalProductProperties.FirstOrDefaultAsync(
                                    x => x.PropertyId == property.Id
                                         && x.ChemicalId == chemical.Id
                                         && x.WorkflowState != Constants.WorkflowStates.Removed, ct);
                            if (propertyChemical != null)
                            {
                                string folderLookUpName = "25.02 Mine kemiprodukter";
                                bool moveChemical = false;
                                if (chemical.UseAndPossesionDeadline != null)
                                {
                                    if (chemical.UseAndPossesionDeadline < DateTime.UtcNow)
                                    {
                                        folderLookUpName = "25.04 Udløber i dag eller er udløbet";
                                        moveChemical = int.Parse(((DateTime) chemical.UseAndPossesionDeadline - DateTime.UtcNow)
                                            .TotalDays.ToString(CultureInfo.InvariantCulture)) == 1;
                                    }
                                    else
                                    {
                                        if (chemical.UseAndPossesionDeadline < DateTime.UtcNow.AddDays(30))
                                        {
                                            moveChemical =
                                                int.Parse(((DateTime) chemical.UseAndPossesionDeadline -
                                                 DateTime.UtcNow.AddDays(14)).TotalDays.ToString(CultureInfo.InvariantCulture)) == 1;
                                            folderLookUpName = "25.03 Udløber om senest 14 dage";
                                        }
                                    }
                                }
                                else
                                {
                                    if (chemical.AuthorisationExpirationDate < DateTime.UtcNow)
                                    {
                                        moveChemical = int.Parse(((DateTime) chemical.AuthorisationExpirationDate - DateTime.UtcNow)
                                            .TotalDays.ToString(CultureInfo.InvariantCulture)) == 1;
                                        folderLookUpName = "25.04 Udløber i dag eller er udløbet";
                                    }
                                    else
                                    {
                                        if (chemical.AuthorisationExpirationDate < DateTime.UtcNow.AddDays(30))
                                        {
                                            moveChemical =
                                                int.Parse(((DateTime) chemical.AuthorisationExpirationDate -
                                                           DateTime.UtcNow.AddDays(14)).TotalDays.ToString(CultureInfo.InvariantCulture)) == 1;
                                            folderLookUpName = "25.03 Udløber om senest 14 dage";
                                        }
                                    }
                                }

                                // Chemical should be moved
                                // moveChemical = true;
                                if (moveChemical)
                                {
                                    Console.WriteLine(
                                        $"Moving chemical with name : {chemical.Name} and registration no {chemical.RegistrationNo}");

                                    var planningCaseSite =
                                        await _itemsPlanningPnDbContext.PlanningCaseSites.AsNoTracking()
                                            .FirstOrDefaultAsync(x =>
                                                x.MicrotingSdkCaseId == propertyChemical.SdkCaseId, ct);

                                    if (planningCaseSite == null)
                                    {
                                        // var site = await sdkDbContext.Sites.SingleOrDefaultAsync(x => x.MicrotingUid == caseDto.SiteUId);
                                        var checkListSite = await _sdkDbContext.CheckListSites.AsNoTracking()
                                            .FirstOrDefaultAsync(x =>
                                                x.MicrotingUid == propertyChemical.SdkCaseId, ct).ConfigureAwait(false);
                                        if (checkListSite == null)
                                        {
                                            return;
                                        }

                                        planningCaseSite =
                                            await _itemsPlanningPnDbContext.PlanningCaseSites.AsNoTracking()
                                                .FirstOrDefaultAsync(x =>
                                                    x.MicrotingCheckListSitId == checkListSite.Id, ct)
                                                .ConfigureAwait(false);
                                    }

                                    var planning =
                                        await _itemsPlanningPnDbContext.Plannings.AsNoTracking()
                                            .FirstAsync(x => x.Id == planningCaseSite.PlanningId, ct);
                                    var areaRulePlanning = await
                                        backendDbContext.AreaRulePlannings.FirstOrDefaultAsync(x =>
                                            x.ItemPlanningId == planning.Id, ct);
                                    var checkListTranslation = await _sdkDbContext.CheckListTranslations.FirstAsync(x =>
                                        x.Text == "25.01 Registrer produkter", ct);
                                    var areaRule =
                                        await backendDbContext.AreaRules.Where(x =>
                                                x.Id == areaRulePlanning.AreaRuleId)
                                            .Include(x => x.Area)
                                            .Include(x => x.Property)
                                            .Include(x => x.AreaRuleTranslations)
                                            .FirstAsync(ct);
                                    var planningSites = await _itemsPlanningPnDbContext.PlanningSites
                                        .Where(x => x.PlanningId == planning.Id).ToListAsync(ct);

                                    var folder =
                                        await _sdkDbContext.Folders.FirstAsync(x => x.Id == areaRule.FolderId, ct);
                                    var folderTranslation = await _sdkDbContext.Folders.Join(
                                        _sdkDbContext.FolderTranslations,
                                        f => f.Id, translation => translation.FolderId, (f, translation) => new
                                        {
                                            f.Id,
                                            f.ParentId,
                                            translation.Name,
                                            f.MicrotingUid
                                        }).FirstAsync(x => x.Name == folderLookUpName && x.ParentId == folder.Id);
                                    var folderMicrotingId = folderTranslation.MicrotingUid.ToString();

                                    await _core.CaseDelete(propertyChemical.SdkCaseId);
                                    await propertyChemical.Delete(backendDbContext);

                                    var chemicalProductPropertySites =
                                        await backendDbContext.ChemicalProductPropertieSites
                                            .Where(x => x.PropertyId == areaRule.PropertyId)
                                            .Where(x => x.ChemicalId == chemical.Id)
                                            .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                                            .ToListAsync();
                                    foreach (var chemicalProductPropertySite in chemicalProductPropertySites)
                                    {
                                        // var checkListSite = await sdkDbContext.CheckListSites.AsNoTracking().FirstAsync(x => x.Id == chemicalProductPropertySite.SdkCaseId);
                                        await _core.CaseDelete(chemicalProductPropertySite.SdkCaseId);
                                        await chemicalProductPropertySite.Delete(backendDbContext);
                                    }

                                    var productName = chemical.Name;
                                    // if (product != null)
                                    // {
                                    //     if (product.Name != "Emballagestørrelse ikke angivet")
                                    //     {
                                    //         productName += " - " + product.Name;
                                    //     }
                                    // }

                                    List<Microting.eForm.Dto.KeyValuePair> options =
                                        new List<Microting.eForm.Dto.KeyValuePair>();
                                    int j = 0;
                                    var totalLocations = string.Empty;
                                    // foreach (var s in location!.ValueReadable.Split("|"))
                                    // {
                                    //     Microting.eForm.Dto.KeyValuePair keyValuePair =
                                    //         new Microting.eForm.Dto.KeyValuePair(j.ToString(), s, false, j.ToString());
                                    //     options.Add(keyValuePair);
                                    //     totalLocations = s;
                                    //     j++;
                                    // }
                                    if (propertyChemical.Locations != null)
                                    {
                                        foreach (var s in propertyChemical.Locations.Split("|"))
                                        {
                                            Microting.eForm.Dto.KeyValuePair keyValuePair =
                                                new Microting.eForm.Dto.KeyValuePair(j.ToString(), s, false,
                                                    j.ToString());
                                            options.Add(keyValuePair);
                                            totalLocations += "|" + s;
                                            j++;
                                        }
                                    }

                                    var language =
                                        await _sdkDbContext.Languages.SingleAsync(x =>
                                            x.Id == propertyChemical.LanguageId);
                                    var sdkSite = await _sdkDbContext.Sites.SingleAsync(x =>
                                        x.MicrotingUid == propertyChemical.SdkSiteId);
                                    var product = await _chemicalsDbContext.Products.FirstOrDefaultAsync(x =>
                                        x.ChemicalId == chemical.Id);


                                    var mainElement = await _core.ReadeForm(checkListTranslation.CheckListId, language);
                                    mainElement = await ModifyChemicalMainElement(mainElement, chemical, product,
                                        productName, folderMicrotingId, areaRule, sdkSite,
                                        totalLocations.Replace("|", ", "));

                                    MultiSelect multiSelect = new MultiSelect(0, false, false,
                                        "Vælg rum som kemiproduktet skal fjernes fra", " ", Constants.FieldColors.Red,
                                        -1,
                                        false, options);
                                    ((FieldContainer) ((DataElement) mainElement.ElementList[0]).DataItemGroupList[1])
                                        .DataItemList.Add(multiSelect);

                                    if (string.IsNullOrEmpty(chemical.Use))
                                    {
                                        ((DataElement) mainElement.ElementList[0]).DataItemGroupList
                                            .RemoveAt(0);
                                    }

                                    var caseId = await _core.CaseCreate(mainElement, "", (int) sdkSite.MicrotingUid,
                                        folder.Id);
                                    var thisDbCase = await _sdkDbContext.CheckListSites.AsNoTracking()
                                        .FirstAsync(x => x.MicrotingUid == caseId);

                                    var propertySites = await backendDbContext.PropertyWorkers
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

                                    foreach (PropertyWorker propertyWorker in propertySites)
                                    {
                                        if (propertyWorker.WorkerId != sdkSite.Id)
                                        {
                                            var site = await
                                                _sdkDbContext.Sites.SingleOrDefaultAsync(x =>
                                                    x.Id == propertyWorker.WorkerId);
                                            var list = ((DataElement) mainElement.ElementList[0]).DataItemGroupList[1]
                                                .DataItemList;
                                            list.RemoveAt(0);
                                            list.RemoveAt(0);
                                            var siteCaseId = await _core.CaseCreate(mainElement, "",
                                                (int) site!.MicrotingUid!,
                                                folder.Id);
                                            // var siteDbCaseId =
                                            //     await sdkDbContext.Cases.SingleAsync(x => x.MicrotingUid == siteCaseId);
                                            var chemicalProductPropertySite = new ChemicalProductPropertySite()
                                            {
                                                ChemicalId = chemical.Id,
                                                SdkCaseId = (int) siteCaseId!,
                                                SdkSiteId = site!.Id,
                                                PropertyId = areaRule.PropertyId,
                                                LanguageId = language.Id
                                            };
                                            await chemicalProductPropertySite.Create(backendDbContext);
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
                                    await newPlanning.Create(_itemsPlanningPnDbContext);
                                    var newPlanningCase = new PlanningCase
                                    {
                                        PlanningId = newPlanning.Id,
                                        Status = 66,
                                        MicrotingSdkeFormId = checkListTranslation.CheckListId
                                    };
                                    await newPlanningCase.Create(_itemsPlanningPnDbContext);
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

                                    await newPlanningCaseSite.Create(_itemsPlanningPnDbContext);

                                    var newAreaRulePlanning = CreateAreaRulePlanningObject(areaRulePlanningModel,
                                        areaRule,
                                        newPlanning.Id,
                                        areaRule.FolderId);


                                    await newAreaRulePlanning.Create(backendDbContext);
                                    ChemicalProductProperty chemicalProductProperty = new ChemicalProductProperty()
                                    {
                                        ChemicalId = chemical.Id,
                                        PropertyId = areaRule.PropertyId,
                                        SdkCaseId = (int) caseId,
                                        Locations = totalLocations,
                                        LanguageId = language.Id
                                    };

                                    await chemicalProductProperty.Create(backendDbContext);
                                }
                            }
                        });

                        var sendGridClient = new SendGridClient(sendGridKey.Value);
                        var fromEmailAddress = new EmailAddress("no-reply@microting.com",
                            $"KemiKontrol for : {property.Name}");
                        var toEmailAddress = new List<EmailAddress>();
                        if (!string.IsNullOrEmpty(property.MainMailAddress))
                        {
                            toEmailAddress.AddRange(property.MainMailAddress.Split(";").Select(s => new EmailAddress(s)));
                        }
                        if (toEmailAddress.Count > 0)
                        {
                            var assembly = Assembly.GetExecutingAssembly();
                            var assemblyName = assembly.GetName().Name;

                            var stream =
                                assembly.GetManifestResourceStream($"{assemblyName}.Resources.KemiKontrol_rapport_1.0_Libre.html");
                            string html;
                            if (stream == null)
                            {
                                throw new InvalidOperationException("Resource not found");
                            }

                            using (var reader = new StreamReader(stream, Encoding.UTF8))
                            {
                                html = await reader.ReadToEndAsync();
                            }
                            var newHtml = html;
                            newHtml = newHtml.Replace("{{propertyName}}", property.Name);
                            newHtml = newHtml.Replace("{{dato}}", DateTime.Now.ToString("dd-MM-yyyy"));
                            newHtml = newHtml.Replace("{{emailaddresses}}", property.MainMailAddress);

                            var chemicals = await _backendConfigurationDbContext.ChemicalProductProperties.Where(x =>
                                x.WorkflowState != Constants.WorkflowStates.Removed && x.PropertyId == property.Id).ToListAsync(ct);
                            var expiredProducts = new List<ChemicalProductProperty>();
                            var expiringIn14Days = new List<ChemicalProductProperty>();
                            var otherProducts = new List<ChemicalProductProperty>();

                            foreach (ChemicalProductProperty chemicalProductProperty in chemicals)
                            {
                                var chemical = _chemicalsDbContext.Chemicals.Single(x => x.Id == chemicalProductProperty.ChemicalId);
                                var expireDate = chemical.UseAndPossesionDeadline ?? chemical.AuthorisationExpirationDate;
                                if (expireDate < DateTime.Now)
                                {
                                    expiredProducts.Add(chemicalProductProperty);
                                }
                                else if (expireDate < DateTime.Now.AddDays(14))
                                {
                                    expiringIn14Days.Add(chemicalProductProperty);
                                }
                                else
                                {
                                    otherProducts.Add(chemicalProductProperty);
                                }
                            }
                            if (expiringIn14Days.Count > 0 || expiredProducts.Count > 0 ||
                                DateTime.Now.DayOfWeek == DayOfWeek.Thursday)
                            {

                                newHtml = newHtml.Replace("{{expiredProducts}}",
                                    await GenerateProductList(expiredProducts, property, _chemicalsDbContext));
                                newHtml = newHtml.Replace("{{expiringIn14Days}}",
                                    await GenerateProductList(expiringIn14Days, property, _chemicalsDbContext));
                                newHtml = newHtml.Replace("{{otherProducts}}",
                                    await GenerateProductList(otherProducts, property, _chemicalsDbContext));

                                var msg = MailHelper.CreateSingleEmailToMultipleRecipients(fromEmailAddress, toEmailAddress,
                                    $"KemiKontrol for: {property.Name}", null, newHtml);

                                List<Attachment> attachments = new List<Attachment>();

                                stream =
                                    assembly.GetManifestResourceStream($"{assemblyName}.Resources.KemiKontrol_rapport_1.0_Libre_html_5d7c0d01f9da8102.png");
                                if (stream == null)
                                {
                                    throw new InvalidOperationException("Resource not found");
                                }
                                byte[] bytes;
                                using (var memoryStream = new MemoryStream())
                                {
                                    await stream.CopyToAsync(memoryStream, ct);
                                    bytes = memoryStream.ToArray();
                                }
                                var attachment1 = new Attachment
                                {
                                    Filename = "eform-logo.png",
                                    Content = Convert.ToBase64String(bytes),
                                    ContentId = "eform-logo",
                                    Disposition = "inline"
                                };
                                attachments.Add(attachment1);

                                stream =
                                    assembly.GetManifestResourceStream($"{assemblyName}.Resources.KemiKontrol_rapport_1.0_Libre_html_36e139cc671b4deb.png");

                                if (stream == null)
                                {
                                    throw new InvalidOperationException("Resource not found");
                                }

                                using (var memoryStream = new MemoryStream())
                                {
                                    await stream.CopyToAsync(memoryStream, ct);
                                    bytes = memoryStream.ToArray();
                                }
                                var attachment2 = new Attachment
                                {
                                    Filename = "back-arrow.png",
                                    Content = Convert.ToBase64String(bytes),
                                    ContentId = "back-arrow",
                                    Disposition = "inline"
                                };
                                attachments.Add(attachment2);

                                stream =
                                    assembly.GetManifestResourceStream($"{assemblyName}.Resources.KemiKontrol_rapport_1.0_Libre_html_29bc21319b8001d7.png");

                                if (stream == null)
                                {
                                    throw new InvalidOperationException("Resource not found");
                                }

                                using (var memoryStream = new MemoryStream())
                                {
                                    await stream.CopyToAsync(memoryStream, ct);
                                    bytes = memoryStream.ToArray();
                                }

                                var attachment3 = new Attachment
                                {
                                    Filename = "sync-button.png",
                                    Content = Convert.ToBase64String(bytes),
                                    ContentId = "sync-button",
                                    Disposition = "inline"
                                };
                                attachments.Add(attachment3);
                                msg.AddAttachments(attachments);

                                var responseMessage = await sendGridClient.SendEmailAsync(msg, ct);
                                if ((int) responseMessage.StatusCode < 200 ||
                                    (int) responseMessage.StatusCode >= 300)
                                {
                                    throw new Exception($"Status: {responseMessage.StatusCode}");
                                }
                            }
                        }
                    }
                });
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
                $"<strong>Placering</strong><br>Ejendom: {areaRule.Property.Name}<br>Rum: {locations}<br><br><strong>Udløbsdato: </strong><br>";

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
                $"Reg nr.: {chemical.RegistrationNo}<br>";

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
            "<br><u>Productstatus</u><br>" +
            $"{Microting.EformBackendConfigurationBase.Infrastructure.Const.Constants.ProductStatusType.FirstOrDefault(x => x.Key == chemical.Status).Value}<br><br>" +
            $"<u>Pesticid produktgruppe</u><br>";
            foreach (var i in chemical.PesticideProductGroup)
            {
                ((None) ((DataElement) mainElement.ElementList[0]).DataItemList[0]).Description
                    .InderValue +=
                    $"{Microting.EformBackendConfigurationBase.Infrastructure.Const.Constants.ProductGroupPesticide.FirstOrDefault(x => x.Key == i).Value}<br>";
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
                using var webClient = new HttpClient();

                await using (var s = await webClient.GetStreamAsync($"https://chemicalbase.microting.com/api/chemicals-pn/get-pdf-file?fileName={product.FileName}"))
                {
                    File.Delete(Path.Combine(Path.GetTempPath(), $"{product.FileName}.pdf"));
                    await using (var fs = new FileStream(Path.Combine(Path.GetTempPath(), $"{product.FileName}.pdf"), FileMode.CreateNew))
                    {
                        await s.CopyToAsync(fs);
                    }
                }

                var pdfId = await _core.PdfUpload(Path.Combine(Path.GetTempPath(), $"{product.FileName}.pdf"));

                await _core.PutFileToStorageSystem(Path.Combine(Path.GetTempPath(), $"{product.FileName}.pdf"),
                    $"{product.FileName}.pdf");
                File.Delete(Path.Combine(Path.GetTempPath(), $"{product.FileName}.pdf"));

                ((ShowPdf) ((DataElement) mainElement.ElementList[0]).DataItemList[1]).Value = pdfId;
                ((DataElement) mainElement.ElementList[0]).DataItemList.RemoveAt(2);
            }
            else
            {
                ((DataElement) mainElement.ElementList[0]).DataItemList.RemoveAt(1);
                ((DataElement) mainElement.ElementList[0]).DataItemList.RemoveAt(1);
            }

            return mainElement;
        }

        private async Task<Planning> CreateItemPlanningObject(int eformId, string eformName, int folderId,
            AreaRulePlanningModel areaRulePlanningModel, AreaRule areaRule)
        {
            // var _backendConfigurationPnDbContext = BackendConfigurationDbContextHelper.GetDbContext();
            var propertyItemPlanningTagId = await _backendConfigurationDbContext.Properties
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

        private async Task<string> GenerateProductList(List<ChemicalProductProperty> chemicalProductProperties, Property property, ChemicalsDbContext dbContext)
        {
            string result = "";
            foreach (var chemicalProductProperty in chemicalProductProperties)
            {
                var chemical = await dbContext.Chemicals
                    .Include(x => x.AuthorisationHolder)
                    .FirstAsync(x => x.Id == chemicalProductProperty.ChemicalId);

                var productGroups = "";
                int j = 0;
                foreach (var i in chemical.PesticideProductGroup)
                {
                    if (j == 0)
                    {
                        productGroups +=  $"{Microting.EformBackendConfigurationBase.Infrastructure.Const.Constants.ProductGroupPesticide.FirstOrDefault(x => x.Key == i).Value}";;
                    }
                    else
                    {
                        productGroups += ", " +  $"{Microting.EformBackendConfigurationBase.Infrastructure.Const.Constants.ProductGroupPesticide.FirstOrDefault(x => x.Key == i).Value}";;
                    }
                    j++;
                }

                var expireDate = "";
                if (chemical.UseAndPossesionDeadline != null)
                {
                    var bla = (DateTime)chemical.UseAndPossesionDeadline;
                    expireDate = bla.ToString("dd-MM-yyyy");
                } else
                {
                    var bla = (DateTime)chemical.AuthorisationExpirationDate!;
                    expireDate = bla.ToString("dd-MM-yyyy");
                }

                result += "<tr valign=\"top\">" +
                          "<td width=\"99\"" +
                          "style=\"border-left: 1px solid #000000; border-right: 1px solid #000000; border-bottom: 1px solid #000000;  padding: 0 0.08in\">" +
                          "<p align=\"left\" style=\"orphans: 2; widows: 2\">" +
                          $"<span>{chemical.Name}</span></p>" +
                          "</td>" +
                          "<td width=\"99\"" +
                          "style=\"border-left: 1px solid #000000; border-right: 1px solid #000000; border-bottom: 1px solid #000000;  padding: 0 0.08in\">" +
                          "<p align=\"left\" style=\"orphans: 2; widows: 2\">" +
                          $"<span>{chemical.AuthorisationHolder.Name}</span></p>" +
                          "</td>" +
                          "<td width=\"99\"" +
                          "style=\"border-left: 1px solid #000000; border-right: 1px solid #000000; border-bottom: 1px solid #000000;  padding: 0 0.08in\">" +
                          "<p align=\"left\" style=\"orphans: 2; widows: 2\">" +
                          $"<span>{chemical.RegistrationNo}</span></p>" +
                          "</td><td width=\"99\"" +
                          "style=\"border-left: 1px solid #000000; border-right: 1px solid #000000; border-bottom: 1px solid #000000;  padding: 0 0.08in\">" +
                          "<p align=\"left\" style=\"orphans: 2; widows: 2\">" +
                          $"<span>{productGroups}</span></p>" +
                          "</td>" +
                          "<td width=\"99\"" +
                          "style=\"border-left: 1px solid #000000; border-right: 1px solid #000000; border-bottom: 1px solid #000000;  padding: 0 0.08in\">" +
                          "<p align=\"left\" style=\"orphans: 2; widows: 2\">" +
                          $"<span>{expireDate}</span></p>" +
                          "</td>" +
                          "<td width=\"99\"" +
                          "style=\"border-left: 1px solid #000000; border-right: 1px solid #000000; border-bottom: 1px solid #000000;  padding: 0 0.08in\">" +
                          "<p align=\"left\" style=\"orphans: 2; widows: 2\">" +
                          $"<span>{property.Name}</span></p>" +
                          "</td>" +
                          "<td width=\"99\"" +
                          "style=\"border-left: 1px solid #000000; border-right: 1px solid #000000; border-bottom: 1px solid #000000;  padding: 0 0.08in\">" +
                          "<p align=\"left\" style=\"orphans: 2; widows: 2\">" +
                          $"<span>{chemicalProductProperty.Locations}</span></p>" +
                          "</td>" +
                          "</tr>";
            }

            return result;
        }

    }
}