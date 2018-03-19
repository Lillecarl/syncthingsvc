using Topshelf;

namespace SyncthingSvc
{
    public class Program
    {
        public const string ServiceName = "SyncthingSvc";

        private static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<Service>(s =>
                {
                    s.ConstructUsing(name => new Service());
                    s.WhenStarted(sc => sc.Start());
                    s.WhenStopped(sc => sc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription(ServiceName);
                x.SetDisplayName(ServiceName);
                x.SetServiceName(ServiceName);
                x.BeforeInstall(callback: sc => Service.Install(ServiceName));
                x.AfterUninstall(() => Service.Uninstall(ServiceName));

                x.EnableServiceRecovery(r =>
                {
                    r.RestartService(1);
                    r.RestartService(1);
                    r.RestartService(1);
                });
            });
        }
    }
}
