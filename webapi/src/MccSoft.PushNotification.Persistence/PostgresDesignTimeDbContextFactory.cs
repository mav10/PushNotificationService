using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;

namespace MccSoft.PushNotification.Persistence
{
    /// <summary>
    /// This class is to allow running powershell EF commands from the project folder without
    /// specifying Startup class (without triggering the whole startup during EF operations
    /// like add/remove migrations).
    /// </summary>
    public class PostgresDesignTimeDbContextFactory
        : IDesignTimeDbContextFactory<PushNotificationDbContext>
    {
        public PushNotificationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PushNotificationDbContext>();

            optionsBuilder.UseNpgsql(
                "Server=localhost;Database=push_notification;Port=5432;Username=postgres;Password=postgres;Pooling=true;Keepalive=5;Command Timeout=60;"
            );

            var operationalStoreOptions = new OperationalStoreOptions();
            return new PushNotificationDbContext(
                optionsBuilder.Options,
                null,
                Options.Create(operationalStoreOptions)
            );
        }
    }
}
