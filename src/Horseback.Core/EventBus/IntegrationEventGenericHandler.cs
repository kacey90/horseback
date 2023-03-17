using Dapper;
using Horseback.Core.DataAccess;
using Horseback.Core.InboxPattern;
using Horseback.Core.InboxPattern.Config;
using Horseback.Core.Utils;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Horseback.Core.EventBus
{
    public class IntegrationEventGenericHandler<T> : IIntegrationEventHandler<T>
        where T : IntegrationEvent
    {
        private readonly ISqlConnectionFactory _sqlConnectionFactory;
        private readonly InboxTransactionalPatternConfiguration _inboxPatternConfig;

        public IntegrationEventGenericHandler(
            ISqlConnectionFactory sqlConnectionFactory,
            InboxTransactionalPatternConfiguration inboxPatternConfig)
        {
            _sqlConnectionFactory = sqlConnectionFactory;
            _inboxPatternConfig = inboxPatternConfig;
        }

        public async Task Handle(T @event)
        {
            using var connection = _sqlConnectionFactory.GetOpenConnection();

            // Check if the table exists and create it if it doesn't
            var tableExists = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = @schema AND table_name = @table", 
                new { schema = _inboxPatternConfig.Schema, table = _inboxPatternConfig.TableName });

            if (tableExists == 0)
            {
                string sqlCreateTable = DatabaseUtils.GenerateCreateInboxTableSqlStatement(_inboxPatternConfig);
                await connection.ExecuteAsync(sqlCreateTable);
            }

            var sqlCmd = DatabaseUtils.GenerateGetExistingEventsSqlStatement(_inboxPatternConfig, @event);
            var eventCount = await connection.ExecuteScalarAsync<int>(sqlCmd, new { @event.Id });
            if (eventCount > 0) return;

            string type = @event.GetType().FullName;
            string data = JsonSerializer.Serialize(@event, @event.GetType());

            string sqlInsertMessage = DatabaseUtils.GenerateInsertInboxMessageSqlStatement(_inboxPatternConfig);
            await connection.ExecuteAsync(sqlInsertMessage, new 
            { @event.Id, OccurredOn = @event.DateOccurred, Type = type, Data = data });
        }
    }
}
