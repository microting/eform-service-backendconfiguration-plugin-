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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microting.eForm.Infrastructure.Constants;
using Microting.eFormApi.BasePn.Infrastructure.Helpers;
using Microting.EformBackendConfigurationBase.Infrastructure.Data;
using Microting.ItemsPlanningBase.Infrastructure.Data;
using Rebus.Bus;
using ServiceBackendConfigurationPlugin.Infrastructure.Helpers;

namespace ServiceBackendConfigurationPlugin.Scheduler.Jobs
{
    public class SearchListJob : IJob
    {
        private readonly BackendConfigurationPnDbContext _dbContext;

        public SearchListJob(
            BackendConfigurationDbContextHelper dbContextHelper)
        {
            _dbContext = dbContextHelper.GetDbContext();
        }

        public async Task Execute()
        {
            await ExecuteUpdateProperties();
        }

        private async Task ExecuteUpdateProperties()
        {

            if (DateTime.UtcNow.Hour > 3)
            {
                Log.LogEvent(
                    $"SearchListJob.Task: ExecutePush The current hour is bigger than the end time of 3, so ending processing");
                return;
            }

            if (DateTime.UtcNow.Hour < 3)
            {
                Log.LogEvent(
                    $"SearchListJob.Task: ExecutePush The current hour is smaller than the start time of 3, so ending processing");
                return;
            }

            Log.LogEvent("SearchListJob.Task: SearchListJob.Execute got called");

            var properties = await _dbContext.Properties.Where(x => x.WorkflowState != Constants.WorkflowStates.Removed).ToListAsync();

            foreach (var property in properties)
            {
                if (_dbContext.Compliances.AsNoTracking().Any(x =>
                        x.Deadline < DateTime.UtcNow && x.PropertyId == property.Id &&
                        x.WorkflowState != Constants.WorkflowStates.Removed))
                {
                    property.ComplianceStatus = 2;
                    property.ComplianceStatusThirty = 2;
                    await property.Update(_dbContext);
                }
                else
                {
                    if (!_dbContext.Compliances.AsNoTracking().Any(x =>
                            x.Deadline < DateTime.UtcNow.AddDays(30) && x.PropertyId == property.Id &&
                            x.WorkflowState != Constants.WorkflowStates.Removed))
                    {
                        property.ComplianceStatusThirty = 0;
                        await property.Update(_dbContext);
                    }

                    if (!_dbContext.Compliances.AsNoTracking().Any(x =>
                            x.Deadline < DateTime.UtcNow && x.PropertyId == property.Id &&
                            x.WorkflowState != Constants.WorkflowStates.Removed))
                    {
                        property.ComplianceStatus = 0;
                        await property.Update(_dbContext);
                    }
                }
            }

        }
    }
}