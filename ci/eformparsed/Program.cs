// See https://aka.ms/new-console-template for more information

using Castle.Windsor;
using Microsoft.EntityFrameworkCore;
using Microting.eForm.Installers;
using Microting.eForm.Messages;
using Microting.EformBackendConfigurationBase.Infrastructure.Data;
using Microting.EformBackendConfigurationBase.Infrastructure.Data.Factories;
using Microting.ItemsPlanningBase.Infrastructure.Data;
using Microting.ItemsPlanningBase.Infrastructure.Data.Factories;
using RabbitMQ.Client;
using Rebus.Bus;

namespace eformparsed;

class Program
{

    private static IWindsorContainer _container;
    private static IBus _bus;
    private static BackendConfigurationPnDbContext _dbContext;
    //private static ItemsPlanningPnDbContext _dbContextItemsPlanning;

    static void Main(string[] args)
    {
        var hostname = "localhost";

        Console.WriteLine($"trying to connect to {hostname}");

        _container = new WindsorContainer();
        _container.Install(
            new RebusHandlerInstaller()
            , new RebusInstaller("420", "empty-string", 1, 1, "admin", "password", hostname)
        );
        _bus = _container.Resolve<IBus>();

        var contextFactory = new BackendConfigurationPnContextFactory();
        //var contextFactoryItemsPlanning = new ItemsPlanningPnContextFactory();

        _dbContext = contextFactory.CreateDbContext(new[] {"host=localhost;Database=420_eform-backend-configuration-plugin;user=root;password=secretpassword;port=3306;Convert Zero Datetime = true;SslMode=none;"});
        //_dbContextItemsPlanning = contextFactoryItemsPlanning.CreateDbContext(new[] {"host=localhost;Database=420_eform-angular-items-planning-plugin;user=root;password=secretpassword;port=3306;Convert Zero Datetime = true;SslMode=none;"});

        var noOfCompliances = _dbContext.Compliances.AsNoTracking().Count(x => x.WorkflowState != "removed");
        var planningSiteStatus = _dbContext.PlanningSites.AsNoTracking().Single(x => x.AreaRulePlanningsId == 36 && x.SiteId == 27);

        if (noOfCompliances != 0) throw new Exception("noOfCompliances is not 0");
        if (planningSiteStatus.Status != 33) throw new Exception("planningSiteStatus status is not 33");

        Console.WriteLine($"noOfCompliances: {noOfCompliances}");
        Console.WriteLine($"planningSiteStatus status is 33: {planningSiteStatus.Status}");

        _bus.SendLocal(new EformParsedByServer("1675761448395", 25396));

        Thread.Sleep(10000);
        noOfCompliances = _dbContext.Compliances.AsNoTracking().Count(x => x.WorkflowState != "removed");
        planningSiteStatus = _dbContext.PlanningSites.AsNoTracking().Single(x => x.AreaRulePlanningsId == 36 && x.SiteId == 27);

        Console.WriteLine($"noOfCompliances: {noOfCompliances}");
        Console.WriteLine($"planningSiteStatus status is 70: {planningSiteStatus.Status}");

        if (noOfCompliances != 1) throw new Exception("noOfCompliances is not 1");
        if (planningSiteStatus.Status != 70) throw new Exception("planningSiteStatus status is not 70");
    }
}