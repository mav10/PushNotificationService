﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Audit.Core;
using Duende.IdentityServer.EntityFramework.Options;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;
using Hangfire;
using Hangfire.Dashboard.BasicAuthorization;
using Hangfire.PostgreSql;
using I18Next.Net.Backends;
using I18Next.Net.Extensions;
using I18Next.Net.Plugins;
using IdentityModel;
using IdentityOAuthSpaExtensions;
using MccSoft.PushNotification.App.Features.Products;
using MccSoft.NpgSql;
using MccSoft.PushNotification.App.Middleware;
using MccSoft.PushNotification.App.Services.Authentication;
using MccSoft.PushNotification.App.Utils;
using MccSoft.PushNotification.App.Utils.Localization;
using MccSoft.PushNotification.Domain;
using MccSoft.PushNotification.Persistence;
using MccSoft.DomainHelpers.DomainEvents.Events;
using MccSoft.LowLevelPrimitives;
using MccSoft.Mailing;
using MccSoft.PersistenceHelpers.DomainEvents;
using MccSoft.PushNotification.App.Features.MobileUsers;
using MccSoft.PushNotification.App.Settings;
using MccSoft.PushNotification.Domain.Audit;
using MccSoft.WebApi;
using MccSoft.WebApi.Patching;
using MccSoft.WebApi.Sentry;
using MccSoft.WebApi.Serialization.DateTime;
using MccSoft.WebApi.SignedUrl;
using MediatR;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using NeinLinq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Npgsql;
using NSwag;
using NSwag.AspNetCore;
using NSwag.Generation.Processors.Security;
using Serilog;

[assembly: ApiController]
[assembly: InternalsVisibleTo("MccSoft.PushNotification.App.Tests")]

namespace MccSoft.PushNotification.App
{
    public class Startup
    {
        private const string _changeTrackingHubUrl = "/change-tracking-hub";
        private const string _healthCheckUrl = "/health";
        private readonly ILogger<Startup> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private IHttpContextAccessor _httpContextAccessor;

        public Startup(
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment,
            ILogger<Startup> logger
        ) {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            Configuration = configuration;
            SwaggerOptions = Configuration.GetSection("Swagger").Get<Settings.SwaggerOptions>();
        }

        public IConfiguration Configuration { get; }
        private Settings.SwaggerOptions SwaggerOptions { get; }

        public virtual void ConfigureServices(IServiceCollection services)
        {
            _logger.LogInformation($"Start {nameof(ConfigureServices)}");
            services.AddMemoryCache(
                options =>
                {
                    options.SizeLimit = null;
                }
            );
            ConfigureContainer(services);
            ConfigureDatabase(services);
            ConfigureAuth(services);
            AddI18Next(services);

            services.AddSignUrl(Configuration.GetSection("SignUrl").GetValue<string>("Secret"));
            services.AddMailing(Configuration.GetSection("Email"));

            JsonSerializerSettings SetupJson(JsonSerializerSettings settings)
            {
                settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                settings.ContractResolver = new PatchRequestContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy
                    {
                        ProcessDictionaryKeys = false,
                        OverrideSpecifiedNames = false
                    }
                };
                settings.Converters.Add(new StringEnumConverter());

                return settings;
            }

            JsonConvert.DefaultSettings = () => SetupJson(new JsonSerializerSettings());

            services.AddControllers(
                    opt =>
                    {
                        AddGlobalFilters(opt);
                    }
                )
                .AddNewtonsoftJson(setupAction => SetupJson(setupAction.SerializerSettings));

            services.AddIgnoreTimezoneAttributes();

            // ToDo this should be properly configured for Cloud scenario
            services.AddCors(
                x =>
                    x.AddPolicy(
                        "mypolicy",
                        configurePolicy =>
                            configurePolicy.AllowAnyHeader()
                                .AllowAnyMethod()
                                .WithExposedHeaders("x-miniprofiler-ids")
                                .SetIsOriginAllowed(hostName => true)
                                .AllowCredentials()
                    )
            );

            services.AddSpaStaticFiles(
                configuration =>
                {
                    configuration.RootPath = "wwwroot";
                }
            );
            //
            // services
            //     .AddSingleton<HealthCheck, PushNotificationDbHealthCheck>()
            //     .AddAppHealth(Configuration);

            AddSwagger(services);

            services.AddHealthChecks();
        }

        public virtual void Configure(
            IApplicationBuilder app,
            IHostApplicationLifetime appLifetime,
            IHostEnvironment hostEnvironment
        ) {
            UseForwardedHeaders(app);

            app.UseCors("mypolicy");

            RunMigration(app.ApplicationServices);
            UseHangfire(app);

            app.UseExternalAuth();

            app.UseRouting();

            app.UseRequestLocalization(
                options =>
                    options.SetDefaultCulture("en").AddSupportedCultures(new[] { "en", "fr", "de" })
            );
            app.UseErrorHandling();
            app.UseRethrowErrorsFromPersistence();

            // Should be placed before places that potentially throws a lot of exceptions,
            // to not end up in their breadcrumbs.
            _logger.LogSentryTestError(nameof(PushNotification));

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseAuthentication();
            UseSignOutLockedUser(app);
            app.UseIdentityServer();
            if (hostEnvironment.IsDevelopment())
            {
                IdentityModelEventSource.ShowPII = true;
            }

            UseSwagger(app);

            app.UseAuthorization();

            if (!hostEnvironment.IsEnvironment("Test"))
            {
                app.UseSerilogRequestLogging(
                    options =>
                    {
                        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                        {
                            diagnosticContext.Set(
                                "UserId",
                                httpContext.User?.Identity?.GetClaimValueOrNull(JwtClaimTypes.Id)
                            );
                        };
                    }
                );
            }

            app.UseEndpoints(
                endpoints =>
                {
                    endpoints.MapControllers();
                    endpoints.MapHealthChecks(_healthCheckUrl);
                }
            );
            app.UseSpa(
                spa =>
                {
                    spa.Options.SourcePath = "wwwroot";

                    if (hostEnvironment.IsDevelopment())
                    {
                        spa.UseProxyToSpaDevelopmentServer("http://localhost:3993/");
                    }
                }
            );
            _httpContextAccessor =
                app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
            _logger.LogInformation("Service started.");
        }

        private static void UseForwardedHeaders(IApplicationBuilder app)
        {
            var forwardedHeadersOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor
                    | ForwardedHeaders.XForwardedProto
                    | ForwardedHeaders.XForwardedHost
            };
            // These three subnets encapsulate the applicable Azure subnets. At the moment, it's not possible to narrow it down further.
            // from https://docs.microsoft.com/en-us/azure/app-service/configure-language-dotnetcore?pivots=platform-linux
            forwardedHeadersOptions.KnownNetworks.Add(
                new IPNetwork(IPAddress.Parse("::ffff:10.0.0.0"), 104)
            );
            forwardedHeadersOptions.KnownNetworks.Add(
                new IPNetwork(IPAddress.Parse("::ffff:192.168.0.0"), 112)
            );
            forwardedHeadersOptions.KnownNetworks.Add(
                new IPNetwork(IPAddress.Parse("::ffff:172.16.0.0"), 108)
            );
            app.UseForwardedHeaders(forwardedHeadersOptions);
        }

        /// <summary>
        /// Register application services here.
        /// Don't move registration outside of this function not to break dependent startups.
        /// </summary>
        private void ConfigureContainer(IServiceCollection services)
        {
            services.Configure<DefaultUserOptions>(Configuration.GetSection("DefaultUser"));

            ConfigureAudit(services);

            services.AddScoped<IDateTimeProvider, DateTimeProvider>()
                .AddTransient<IUserAccessor, UserAccessor>()
                .AddScoped<DefaultUserSeeder>()
                .AddScoped<ProductService>();

            services.AddTransient<UserService>();
            
            services.AddSingleton<Func<PushNotificationDbContext>>(
                    provider =>
                        () =>
                            new PushNotificationDbContext(
                                provider.GetRequiredService<
                                    DbContextOptions<PushNotificationDbContext>
                                >(),
                                provider.GetRequiredService<IUserAccessor>(),
                                provider.GetRequiredService<IOptions<OperationalStoreOptions>>()
                            )
                )
                .RegisterRetryHelper();
            // HangFire jobs
            // services.AddScoped<TherapyDataSyncJob>();
        }

        private void ConfigureAudit(IServiceCollection services)
        {
            var settings = Configuration.GetSection("Audit");
            services.Configure<AuditSettings>(settings);
            var typedSettings = settings.Get<AuditSettings>();

            Audit.Core.Configuration.AuditDisabled = !typedSettings.Enabled;

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(
                    config =>
                        config.UseDbContext(
                                ev =>
                                {
                                    // https://github.com/thepirat000/Audit.NET/issues/451
                                    // To support transactions rollback (e.g. PostgresRetryHelper) we need to use separate context for Audit entries
                                    // (otherwise there's an infinite cycle inside Audit.Net when transaction is rolled back and next transaction is committed).
                                    // But we also need to use the same transaction for audit logs as in the main context, to not save Audit logs from rolled back transactions.
                                    // So we create DBContext manually
                                    PushNotificationDbContext dbContext =
                                        (PushNotificationDbContext)ev.EntityFrameworkEvent.GetDbContext();
                                    DatabaseFacade db = dbContext.Database;
                                    DbConnection conn = db.GetDbConnection();
                                    IDbContextTransaction tran = db.CurrentTransaction;
                                    PushNotificationDbContext auditContext = new PushNotificationDbContext(
                                        new DbContextOptionsBuilder<PushNotificationDbContext>().UseNpgsql(
                                            conn
                                        ).Options,
                                        dbContext.UserAccessor,
                                        dbContext.OperationalStoreOptions
                                    );
                                    if (tran != null)
                                    {
                                        auditContext.Database.UseTransaction(
                                            tran.GetDbTransaction()
                                        );
                                    }

                                    return auditContext;
                                }
                            )
                            .AuditTypeMapper(x => x == typeof(AuditLog) ? null : typeof(AuditLog))
                            .AuditEntityAction<AuditLog>(
                                (ev, entry, auditLog) =>
                                {
                                    auditLog.UserId =
                                        _httpContextAccessor?.HttpContext?.User?.Identity?.GetUserIdOrNull();
                                    auditLog.ChangeDate = DateTime.UtcNow;
                                    auditLog.EntityType = entry.Name;
                                    auditLog.Action = entry.Action;
                                    auditLog.FullKey = entry.PrimaryKey;
                                    auditLog.Key =
                                        entry.PrimaryKey.Values.FirstOrDefault()?.ToString();
                                    auditLog.Change = entry.Changes?.ToDictionary(
                                        x => x.ColumnName,
                                        x => x.NewValue
                                    );
                                    auditLog.Actual = entry.ColumnValues;
                                    Dictionary<string, object> old =
                                        entry.ColumnValues.ToDictionary(x => x.Key, x => x.Value);
                                    entry.Changes?.ForEach(
                                        x => old[x.ColumnName] = x.OriginalValue
                                    );
                                    auditLog.Old = entry.Changes == null ? null : old;
                                }
                            )
                            .IgnoreMatchedProperties(true)
                );

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<PushNotificationDbContext>(
                    config => config.ForEntity<User>(_ => _.Ignore(user => user.PasswordHash))
                )
                .UseOptOut();
        }

        protected virtual void ConfigureDatabase(IServiceCollection services)
        {
            services.AddDomainEventsWithMediatR(typeof(Startup), typeof(LogDomainEventHandler));

            services.AddEntityFrameworkNpgsql()
                .AddDbContext<PushNotificationDbContext>(
                    (provider, opt) =>
                        opt.UseNpgsql(
                                Configuration.GetConnectionString("DefaultConnection"),
                                builder => builder.EnableRetryOnFailure()
                            )
                            .WithLambdaInjection()
                            .AddDomainEventsInterceptors(provider),
                    contextLifetime: ServiceLifetime.Scoped,
                    optionsLifetime: ServiceLifetime.Singleton
                );
            AddHangfire(services, Configuration.GetConnectionString("DefaultConnection"));
        }

        protected virtual void AddSwagger(IServiceCollection services)
        {
            services.AddOpenApiDocument(
                options =>
                {
                    options.DocumentProcessors.Add(
                        new SecurityDefinitionAppender(
                            "JWT Token",
                            new OpenApiSecurityScheme
                            {
                                Type = OpenApiSecuritySchemeType.ApiKey,
                                Name = "Authorization",
                                Description = "Copy 'Bearer ' + valid JWT token into field",
                                In = OpenApiSecurityApiKeyLocation.Header
                            }
                        )
                    );

                    options.PostProcess = document =>
                    {
                        document.Info = new NSwag.OpenApiInfo
                        {
                            Version = SwaggerOptions.Version,
                            Title = SwaggerOptions.Title,
                            Description = SwaggerOptions.Description,
                            Contact = new NSwag.OpenApiContact
                            {
                                Email = SwaggerOptions.Contact.Email
                            },
                            License = new NSwag.OpenApiLicense
                            {
                                Name = SwaggerOptions.License.Name
                            },
                        };
                    };

                    options.AddSecurity(
                        "Bearer",
                        new OpenApiSecurityScheme()
                        {
                            Type = OpenApiSecuritySchemeType.OAuth2,
                            Description = "PushNotification Authentication",
                            Flow = OpenApiOAuth2Flow.Password,
                            Flows = new NSwag.OpenApiOAuthFlows()
                            {
                                Password = new NSwag.OpenApiOAuthFlow()
                                {
                                    TokenUrl = "/connect/token",
                                    RefreshUrl = "/connect/token",
                                    AuthorizationUrl = "/connect/token",
                                    Scopes = new Dictionary<string, string>()
                                    {
                                        { "profile", "profile" },
                                        { "offline_access", "offline_access" },
                                        {
                                            "MccSoft.PushNotification.AppAPI",
                                            "MccSoft.PushNotification.AppAPI"
                                        },
                                    }
                                }
                            }
                        }
                    );
                    options.OperationProcessors.Add(
                        new AspNetCoreOperationSecurityScopeProcessor("Bearer")
                    );
                    options.SchemaProcessors.Add(new RequireValueTypesSchemaProcessor());
                    //options.FlattenInheritanceHierarchy = true;
                    options.GenerateEnumMappingDescription = true;
                }
            );
        }

        protected virtual void UseSignOutLockedUser(IApplicationBuilder app)
        {
            app.UseSignOutLockedUser();
        }

        protected virtual void AddHangfire(IServiceCollection services, string connectionString)
        {
            services.AddHangfire(
                config =>
                    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                        .UseSimpleAssemblyNameTypeSerializer()
                        .UseRecommendedSerializerSettings()
                        .UsePostgreSqlStorage(
                            connectionString,
                            new PostgreSqlStorageOptions()
                            {
                                DistributedLockTimeout = TimeSpan.FromSeconds(20),
                            }
                        )
            );
        }

        protected virtual void UseHangfire(IApplicationBuilder app)
        {
            app.UseHangfireServer(new BackgroundJobServerOptions { WorkerCount = 2 });

            // var therapySyncJobSettings = Configuration.GetSection(nameof(TherapyDataSyncJobSettings))
            //     .Get<HangFireJobSettings>();
            // RecurringJob.AddOrUpdate<TherapyDataSyncJob>(
            //     nameof(TherapyDataSyncJob),
            //     job => job.Execute(),
            //     therapySyncJobSettings.CronExpression);

            IConfigurationSection configurationSection = Configuration.GetSection("Hangfire");
            // In case you will need to debug/monitor tasks you can use Dashboard.
            if (configurationSection.GetValue<bool>("EnableDashboard"))
            {
                app.UseHangfireDashboard(
                    options: new DashboardOptions()
                    {
                        Authorization = new[]
                        {
                            new BasicAuthAuthorizationFilter(
                                new BasicAuthAuthorizationFilterOptions()
                                {
                                    Users = new[]
                                    {
                                        new BasicAuthAuthorizationUser()
                                        {
                                            Login = configurationSection.GetValue<string>(
                                                "DashboardUser"
                                            ),
                                            PasswordClear = configurationSection.GetValue<string>(
                                                "DashboardPassword"
                                            ),
                                        }
                                    }
                                }
                            )
                        }
                    }
                );
            }
        }

        protected virtual void UseSwagger(IApplicationBuilder app)
        {
            if (!SwaggerOptions.Enabled)
            {
                return;
            }

            var clientConfig = Configuration.GetSection("IdentityServer:Clients")
                .Get<List<Client>>()
                .First();

            app.UseOpenApi(
                options =>
                {
                    options.Path = "/swagger/v1/swagger.json";
                }
            );
            app.UseSwaggerUi3(
                options =>
                {
                    options.Path = "/swagger";
                    options.DocumentPath = "/swagger/v1/swagger.json";
                    options.OAuth2Client = new OAuth2ClientSettings()
                    {
                        ClientId = clientConfig.ClientId,
                        ClientSecret = SwaggerOptions.ClientPublicKey,
                        AppName = "swagger",
                        Realm = "swagger",
                    };
                }
            );
        }

        protected virtual void ConfigureAuth(IServiceCollection services)
        {
            // services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services.AddDefaultIdentity<User>(
                    options =>
                    {
                        options.SignIn.RequireConfirmedAccount = false;
                        options.Lockout.AllowedForNewUsers = false;
                        Configuration.GetSection("IdentityServer:Password").Bind(options.Password);
                    }
                )
                .AddErrorDescriber<LocalizableIdentityErrorDescriber>()
                .AddRoles<IdentityRole>()
                .AddRoleManager<RoleManager<IdentityRole>>()
                .AddEntityFrameworkStores<PushNotificationDbContext>()
                .AddDefaultTokenProviders();

            var identityServerBuilder = services.AddIdentityServer(
                options =>
                {
                    options.Events.RaiseSuccessEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseErrorEvents = true;
                }
            );
            AddIdentityServerCertificate(identityServerBuilder);
            identityServerBuilder.AddApiAuthorization<User, PushNotificationDbContext>(options => { })
                .AddInMemoryClients(Configuration.GetSection("IdentityServer:Clients"))
                .AddExtensionGrantValidator<PushNotificationExternalAuthenticationGrantValidator>();

            services.AddAuthentication().AddIdentityServerJwt();
            services.Configure<JwtBearerOptions>(
                IdentityServerJwtConstants.IdentityServerJwtBearerScheme,
                options =>
                {
                    var validIssuers =
                        Configuration.GetSection("IdentityServer:ValidIssuers").Get<List<string>>()
                        ?? new List<string>();
                    options.TokenValidationParameters.ValidIssuers = validIssuers;
                    options.TokenValidationParameters.ValidateIssuer = Configuration.GetSection(
                            "IdentityServer:ValidateIssuer"
                        )
                        .Get<bool>();
                    var defaultHandler = options.Events.OnMessageReceived;
                    options.Events.OnMessageReceived = context =>
                        HandleFirstMessage(context, defaultHandler);
                }
            );

            services.ConfigureExternalAuth();
            services.AddAuthentication()
                .AddOpenIdConnect(options => Configuration.Bind("AzureAd", options));

            services.AddTransient<IProfileService, AppProfileService>();
            services.AddTransient<
                IResourceOwnerPasswordValidator,
                AppResourceOwnerPasswordValidator<User>
            >();

            services.AddScoped<DbContext, PushNotificationDbContext>();
        }

        private void AddIdentityServerCertificate(IIdentityServerBuilder identityServerBuilder)
        {
            var section = Configuration.GetSection("IdentityServer:Key");
            var base64Certificate = section.GetValue<string>("Base64Certificate");
            if (!string.IsNullOrEmpty(base64Certificate))
            {
                var tempFile = Guid.NewGuid().ToString();
                section["FilePath"] = tempFile;
                File.WriteAllBytes(
                    Path.Combine(Directory.GetCurrentDirectory(), tempFile),
                    Convert.FromBase64String(base64Certificate)
                );
            }
        }

        private static Task HandleFirstMessage(
            MessageReceivedContext context,
            Func<MessageReceivedContext, Task> defaultHandler
        ) {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            var isWebsocketRequest =
                !string.IsNullOrEmpty(accessToken)
                && path.StartsWithSegments(_changeTrackingHubUrl);
            if (isWebsocketRequest)
            {
                context.Token = accessToken;
                return Task.CompletedTask;
            }
            else
            {
                return defaultHandler(context);
            }
        }

        protected virtual void RunMigration(IServiceProvider container)
        {
            using var scope = container.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<PushNotificationDbContext>();
            context.Database.Migrate();

            var conn = (NpgsqlConnection)context.Database.GetDbConnection();
            conn.Open();
            conn.ReloadTypes();

            DefaultUserSeeder seeder =
                scope.ServiceProvider.GetRequiredService<DefaultUserSeeder>();
            DefaultUserOptions defaultUser =
                scope.ServiceProvider.GetRequiredService<IOptions<DefaultUserOptions>>().Value;
            if (
                !string.IsNullOrEmpty(defaultUser.UserName)
                && !string.IsNullOrEmpty(defaultUser.Password)
            ) {
                seeder.SeedUser(defaultUser.UserName, defaultUser.Password)
                    .GetAwaiter()
                    .GetResult();
            }
        }

        protected virtual void AddGlobalFilters(MvcOptions options)
        {
            // This has the effect of applying the [Authorize] attribute to all
            // controllers, so developers don't need to remember to add it manually.
            // Use the [AllowAnonymous] attribute on a specific controller to override.
            options.Filters.Add(new AuthorizeFilter());

            // This is needed for NSwag to produce correct client code
            options.Filters.Add(
                new ProducesResponseTypeAttribute(typeof(ValidationProblemDetails), 400)
            );
            options.Filters.Add(new ProducesResponseTypeAttribute(200));
        }

        private void AddI18Next(IServiceCollection services)
        {
            services.AddI18NextLocalization(
                i18N =>
                    i18N.AddLanguageDetector<ThreadLanguageDetector>()
                        .Configure(o => o.DetectLanguageOnEachTranslation = true)
                        .AddBackend(new JsonFileBackend("Dictionaries"))
            );
            services.AddSingleton<
                IConfigureOptions<MvcOptions>,
                ConfigureModelBindingLocalization
            >();
        }
    }
}
