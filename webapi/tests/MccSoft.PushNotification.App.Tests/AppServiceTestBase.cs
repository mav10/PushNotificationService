using System.Globalization;
using System.Threading;
using Duende.IdentityServer.EntityFramework.Options;
using Hangfire;
using MccSoft.PushNotification.Domain;
using MccSoft.PushNotification.Persistence;
using MccSoft.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MccSoft.PushNotification.App.Tests
{
    /// <summary>
    /// The base class for application service test classes.
    /// </summary>
    /// <typeparam name="TService">The type of the service under test.</typeparam>
    public class AppServiceTestBase<TService> : AppServiceTest<TService, PushNotificationDbContext>
    {
        protected Mock<IBackgroundJobClient> _backgroundJobClient;
        private User _defaultUser;

        public AppServiceTestBase()
            : base(
                (options, userAccessor) =>
                    new PushNotificationDbContext(
                        options,
                        userAccessor,
                        Options.Create(new OperationalStoreOptions())
                    )
            ) {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            WithDbContext(
                db =>
                {
                    db.Users.Add(_defaultUser = new User("default@test.test"));
                    db.SaveChanges();
                    _userAccessorMock.Setup(x => x.GetUserId()).Returns(_defaultUser.Id);
                }
            );
        }

        protected ServiceCollection CreateServiceCollection(PushNotificationDbContext db)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(db);

            serviceCollection.AddTransient(typeof(ILogger<>), typeof(NullLogger<>));

            _backgroundJobClient = new Mock<IBackgroundJobClient>();
            serviceCollection.AddSingleton(_backgroundJobClient.Object);

            return serviceCollection;
        }
    }
}
