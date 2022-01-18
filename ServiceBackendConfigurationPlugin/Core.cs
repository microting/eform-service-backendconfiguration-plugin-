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

using Castle.MicroKernel.Registration;
using Microting.eForm.Infrastructure.Constants;
using Microting.EformBackendConfigurationBase.Infrastructure.Data;
using Microting.EformBackendConfigurationBase.Infrastructure.Data.Entities;
using Microting.EformBackendConfigurationBase.Infrastructure.Data.Factories;
using Microting.ItemsPlanningBase.Infrastructure.Data;
using Microting.ItemsPlanningBase.Infrastructure.Data.Entities;
using Microting.ItemsPlanningBase.Infrastructure.Data.Factories;
using Property = Microting.EformBackendConfigurationBase.Infrastructure.Data.Entities.Property;

namespace ServiceBackendConfigurationPlugin
{
    using System;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using Castle.Windsor;
    using Infrastructure.Helpers;
    using Installers;
    using Messages;
    using Microsoft.EntityFrameworkCore;
    using Microting.eForm.Dto;
    using Microting.WindowsService.BasePn;
    using Rebus.Bus;

    [Export(typeof(ISdkEventHandler))]
    public class Core : ISdkEventHandler
    {
        private eFormCore.Core _sdkCore;
        private IWindsorContainer _container;
        private IBus _bus;
        private bool _coreThreadRunning = false;
        private bool _coreStatChanging;
        private bool _coreAvailable;
        private string _serviceLocation;
        private static int _maxParallelism = 1;
        private static int _numberOfWorkers = 1;
        private BackendConfigurationPnDbContext _dbContext;
        private ItemsPlanningPnDbContext _itemsPlanningDbContext;
        private BackendConfigurationDbContextHelper _backendConfigurationBackendConfigurationDbContextHelper;
        private ItemsPlanningDbContextHelper _itemsPlanningDbContextHelper;

        public void CoreEventException(object sender, EventArgs args)
        {
            // Do nothing
        }

        public void UnitActivated(object sender, EventArgs args)
        {
            // Do nothing
        }

        public void eFormProcessed(object sender, EventArgs args)
        {
            CaseDto trigger = (CaseDto)sender;
            Console.WriteLine($"Received eFormProcessed event with caseId: {trigger.CaseId}");
            Console.WriteLine($"Received eFormProcessed event with MicrotingUId: {trigger.MicrotingUId}");

            if (trigger.MicrotingUId != null)
            {
                _bus.SendLocal(new EformParsedByServer(trigger.CaseId, trigger.MicrotingUId, trigger.CheckUId));
            }
        }

        public void eFormProcessingError(object sender, EventArgs args)
        {
            // CaseDto trigger = (CaseDto)sender;
            //
            // int? caseId = trigger.MicrotingUId;
            // if (caseId != null) _bus.SendLocal(new EformParsingError((int) caseId));
        }

        public void eFormRetrived(object sender, EventArgs args)
        {
            // CaseDto trigger = (CaseDto)sender;
            //
            // int? caseId = trigger.MicrotingUId;
            // if (caseId != null) _bus.SendLocal(new eFormRetrieved((int) caseId));
        }

        public void CaseCompleted(object sender, EventArgs args)
        {
            CaseDto trigger = (CaseDto)sender;

            if (trigger.MicrotingUId != null)
            {
                _bus.SendLocal(new eFormCompleted(trigger.CaseId, trigger.MicrotingUId, trigger.CheckUId));
            }
        }

        public void CaseDeleted(object sender, EventArgs args)
        {
            // Do nothing
        }

        public void NotificationNotFound(object sender, EventArgs args)
        {
            // Do nothing
        }

        public bool Start(string sdkConnectionString, string serviceLocation)
        {
            Console.WriteLine("ServiceBackendConfigurationPlugin start called");
            try
            {
                var dbNameSection = Regex.Match(sdkConnectionString, @"(Database=\w*;)").Groups[0].Value;
                var dbPrefix = Regex.Match(sdkConnectionString, @"Database=(\d*)_").Groups[1].Value;

                var pluginDbName = $"Database={dbPrefix}_eform-backend-configuration-plugin;";
                var connectionString = sdkConnectionString.Replace(dbNameSection, pluginDbName);
                string rabbitmqHost = connectionString.Contains("frontend") ? $"frontend-{dbPrefix}-rabbitmq" :"localhost";

                if (!_coreAvailable && !_coreStatChanging)
                {
                    _serviceLocation = serviceLocation;
                    _coreStatChanging = true;

                    if (string.IsNullOrEmpty(_serviceLocation))
                        throw new ArgumentException("serviceLocation is not allowed to be null or empty");

                    if (string.IsNullOrEmpty(connectionString))
                        throw new ArgumentException("serverConnectionString is not allowed to be null or empty");

                    BackendConfigurationPnContextFactory contextFactory = new BackendConfigurationPnContextFactory();

                    _dbContext = contextFactory.CreateDbContext(new[] { connectionString });
                    _dbContext.Database.Migrate();
                    _backendConfigurationBackendConfigurationDbContextHelper = new BackendConfigurationDbContextHelper(connectionString);

                    pluginDbName = $"Database={dbPrefix}_eform-angular-items-planning-plugin;";
                    var itemsPlanningConnectionString = sdkConnectionString.Replace(dbNameSection, pluginDbName);
                    ItemsPlanningPnContextFactory itemContextFactory = new ItemsPlanningPnContextFactory();

                    _itemsPlanningDbContext = itemContextFactory.CreateDbContext(new[] { itemsPlanningConnectionString });
                    _itemsPlanningDbContextHelper = new ItemsPlanningDbContextHelper(itemsPlanningConnectionString);

                    _coreAvailable = true;
                    _coreStatChanging = false;

                    StartSdkCoreSqlOnly(sdkConnectionString);

                    string temp = _dbContext.PluginConfigurationValues
                        .SingleOrDefault(x => x.Name == "BackendConfigurationBaseSettings:MaxParallelism")?.Value;
                    _maxParallelism = string.IsNullOrEmpty(temp) ? 1 : int.Parse(temp);

                    temp = _dbContext.PluginConfigurationValues
                        .SingleOrDefault(x => x.Name == "BackendConfigurationBaseSettings:NumberOfWorkers")?.Value;
                    _numberOfWorkers = string.IsNullOrEmpty(temp) ? 1 : int.Parse(temp);

                    CheckComplianceIntegrity(connectionString, itemsPlanningConnectionString);

                    _container = new WindsorContainer();
                    _container.Register(Component.For<IWindsorContainer>().Instance(_container));
                    _container.Register(Component.For<BackendConfigurationDbContextHelper>().Instance(_backendConfigurationBackendConfigurationDbContextHelper));
                    _container.Register(Component.For<ItemsPlanningDbContextHelper>().Instance(_itemsPlanningDbContextHelper));
                    _container.Register(Component.For<eFormCore.Core>().Instance(_sdkCore));
                    _container.Install(
                        new RebusHandlerInstaller()
                        , new RebusInstaller(connectionString, _maxParallelism, _numberOfWorkers, "admin", "password", rabbitmqHost)
                    );
                    // _container.Register(Component.For<SearchListJob>());

                    _bus = _container.Resolve<IBus>();

                    // ConfigureScheduler();
                }
                Console.WriteLine("ServiceBackendConfigurationPlugin started");
                return true;
            }
            catch(Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Start failed " + ex.Message);
                throw;
            }
        }

        public bool Stop(bool shutdownReallyFast)
        {
            try
            {
                if (_coreAvailable && !_coreStatChanging)
                {
                    _coreStatChanging = true;

                    _coreAvailable = false;

                    var tries = 0;
                    while (_coreThreadRunning)
                    {
                        Thread.Sleep(100);
                        _bus.Dispose();
                        tries++;
                    }
                    _sdkCore.Close();

                    _coreStatChanging = false;
                }

            }
            catch (ThreadAbortException)
            {
                //"Even if you handle it, it will be automatically re-thrown by the CLR at the end of the try/catch/finally."
                Thread.ResetAbort(); //This ends the re-throwning
            }

            return true;
        }

        public bool Restart(int sameExceptionCount, int sameExceptionCountMax, bool shutdownReallyFast)
        {
            return true;
        }

        public void StartSdkCoreSqlOnly(string sdkConnectionString)
        {
            _sdkCore = new eFormCore.Core();

            _sdkCore.StartSqlOnly(sdkConnectionString);
        }

        private void CheckComplianceIntegrity(string connectionStringBackend, string connectionStringItemsPlanning)
        {
            var contextFactory = new BackendConfigurationPnContextFactory();
            var backendConfigurationPnDbContext = contextFactory.CreateDbContext(new[] { connectionStringBackend });

            var contextFactoryItemsPlanning = new ItemsPlanningPnContextFactory();
            var itemsPlanningPnDbContext = contextFactoryItemsPlanning.CreateDbContext(new[] { connectionStringItemsPlanning });

            var properties = backendConfigurationPnDbContext.Properties
                .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                .ToList();

            foreach (Property property in properties)
            {
                int i = 0;
                var backendPlannings = backendConfigurationPnDbContext.AreaRulePlannings
                    .Where(x => x.PropertyId == property.Id)
                    .Where(x => x.ItemPlanningId != 0)
                    .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                    .ToList();

                foreach (AreaRulePlanning areaRulePlanning in backendPlannings)
                {
                    if (backendConfigurationPnDbContext.Compliances.Any(x => x.PlanningId == areaRulePlanning.ItemPlanningId && x.WorkflowState != Constants.WorkflowStates.Removed))
                    {
                        continue;
                    }

                    var planningCases =
                        itemsPlanningPnDbContext.PlanningCases
                            .Where(x => x.PlanningId == areaRulePlanning.ItemPlanningId)
                            .Where(x => x.Status != 100)
                            .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                            .Where(x => x.WorkflowState != Constants.WorkflowStates.Retracted)
                            .ToList();

                    foreach (PlanningCase planningCase in planningCases)
                    {
                        var planning =
                            itemsPlanningPnDbContext.Plannings
                                .Where(x => x.Id == planningCase.PlanningId)
                                .Where(x => x.RepeatEvery != 0)
                                .SingleOrDefault(x => x.StartDate < DateTime.UtcNow);

                        if (planning == null)
                        {
                            continue;
                        }

                        try
                        {
                            Compliance compliance = new Compliance()
                            {
                                PlanningId = planning.Id,
                                AreaId = areaRulePlanning.AreaId,
                                Deadline = (DateTime) planning.NextExecutionTime,
                                StartDate = (DateTime) planning.LastExecutedTime,
                                MicrotingSdkCaseId = planningCase.MicrotingSdkCaseId,
                                MicrotingSdkeFormId = planning.RelatedEFormId,
                                PropertyId = property.Id
                            };

                            compliance.Create(backendConfigurationPnDbContext).GetAwaiter().GetResult();
                            i++;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            // throw;
                        }
                    }
                }

                if (backendConfigurationPnDbContext.Compliances.Any(x => x.PropertyId == property.Id && x.Deadline < DateTime.UtcNow && x.WorkflowState != Constants.WorkflowStates.Removed))
                {
                    property.ComplianceStatusThirty = 2;
                    property.ComplianceStatus = 2;
                } else
                {
                    if (backendConfigurationPnDbContext.Compliances.Any(x => x.PropertyId == property.Id && x.WorkflowState != Constants.WorkflowStates.Removed))
                    {
                        property.ComplianceStatusThirty = backendConfigurationPnDbContext.Compliances.Any(x =>
                            x.Deadline < DateTime.UtcNow.AddDays(30) && x.PropertyId == property.Id &&
                            x.WorkflowState != Constants.WorkflowStates.Removed) ? 1 : 0;
                        property.ComplianceStatus = 1;
                    }
                    else
                    {
                        property.ComplianceStatusThirty = 0;
                        property.ComplianceStatus = 0;
                    }
                }
                property.Update(backendConfigurationPnDbContext).GetAwaiter().GetResult();
            }

        }
    }
}