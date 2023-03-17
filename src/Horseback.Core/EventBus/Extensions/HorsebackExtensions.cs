using Horseback.Core.Abstractions;
using Horseback.Core.DataAccess;
using Horseback.Core.EventBus.Mappers;
using Horseback.Core.InboxPattern;
using Horseback.Core.InboxPattern.Config;
using Microsoft.Extensions.DependencyInjection;

namespace Horseback.Core.EventBus.Extensions
{
    public static class HorsebackExtensions
    {
        /// <summary>
        /// Add Horseback to the service collection
        /// </summary>
        /// <param name="services">Service Collection</param>
        /// <returns></returns>
        public static IHorsebackBuilder AddHorseback(this IServiceCollection services)
        {
            services.AddLogging();
            services.AddSingleton<IntegrationEventMappingService>();

            return new HorsebackBuilder(services);
        }

        /// <summary>
        /// Add inbox messaging pattern to horseback. This will handle all Message events and store them in a database table for later processing by the application or other services
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="databaseConnection"></param>
        /// <param name="tableName"></param>
        /// <param name="schema"></param>
        /// <param name="databaseType"></param>
        /// <returns></returns>
        public static IHorsebackBuilder AddInboxMessagePattern(
            this IHorsebackBuilder builder,
            string databaseConnection,
            string tableName,
            string schema = "dbo",
            DatabaseType databaseType = DatabaseType.SqlServer)
        {
            var inboxPatternConfig = new InboxTransactionalPatternConfiguration(tableName, databaseConnection, schema, databaseType);
            builder.Services.AddSingleton(inboxPatternConfig);

            builder.Services.AddScoped<ISqlConnectionFactory>(provider =>
                new SqlConnectionFactory(databaseConnection));
            

            return builder;
        }
    }
}
