// See https://aka.ms/new-console-template for more information

using Castle.Windsor;
using Microting.eForm.Installers;
using Rebus.Bus;

namespace eformparsed
{
    class Program
    {

        private static IWindsorContainer _container;
        private static IBus _bus;

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
        }
    }
}
