﻿using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Audit.EntityFramework;
using Duende.IdentityServer.EntityFramework.Options;
using MccSoft.LowLevelPrimitives;
using MccSoft.NpgSql;
using MccSoft.PushNotification.Domain;
using MccSoft.PushNotification.Domain.Audit;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Npgsql;

namespace MccSoft.PushNotification.Persistence
{
    public class PushNotificationDbContext
        :
          // DbContext
          ApiAuthorizationDbContext<User>,
          ITransactionFactory
    {
        public IUserAccessor UserAccessor { get; }
        public IOptions<OperationalStoreOptions> OperationalStoreOptions { get; }

        public DbSet<Product> Products { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        public PushNotificationDbContext(
            DbContextOptions<PushNotificationDbContext> options,
            IUserAccessor userAccessor,
            IOptions<OperationalStoreOptions> operationalStoreOptions
        ) : base(options, operationalStoreOptions)
        // : base(options)
        {
            UserAccessor = userAccessor;
            OperationalStoreOptions = operationalStoreOptions;
        }
        static PushNotificationDbContext() => NpgsqlConnection.GlobalTypeMapper.MapEnum<ProductType>();
        // when adding enum here don't forget to OnModelCreating as well (see below)

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.HasPostgresEnum<ProductType>();
            // when adding enum here don't forget to add it to static constructor as well (see above)

            // if you already have some data in the table, which column you'd like to convert to enum
            // you'd need to adjust migration SQL to something like the following
            // migrationBuilder.Sql(
            // @"ALTER TABLE ""Patients"" ALTER COLUMN ""NumberSource"" TYPE number_source using (enum_range(null::number_source))[""NumberSource""::int + 1];"
            //     );
            // For details see https://github.com/mcctomsk/backend-frontend-template/wiki/_new#migration-of-existing-data
            
            
            if (Database.IsNpgsql())
            {
                builder.Entity<User>()
                    .Property<int>(p => p.UniqueId)
                    .ValueGeneratedOnAdd()
                    .IsUnicode();
            }
            else
            {
                builder.Entity<User>().Property(d => d.UniqueId)
                    .HasDefaultValueSql("last_insert_rowid()");
            }
        }

        public IDbContextTransaction BeginTransaction()
        {
            return Database.BeginTransaction(IsolationLevel.Serializable);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return Database.BeginTransactionAsync(IsolationLevel.Serializable);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.AddInterceptors(new AuditSaveChangesInterceptor());
        }
    }
}
