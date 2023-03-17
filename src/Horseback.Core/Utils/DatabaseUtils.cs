using Horseback.Core.InboxPattern.Config;
using Horseback.Core.InboxPattern;
using System;

namespace Horseback.Core.Utils
{
    internal static class DatabaseUtils
    {
        public static string GenerateCreateInboxTableSqlStatement(InboxTransactionalPatternConfiguration inboxPatternConfig)
        {
            return inboxPatternConfig.DatabaseType switch
            {
                DatabaseType.Sqlite => $@"CREATE TABLE [{inboxPatternConfig.TableName}] (
                        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                        [OccurredOn] DATETIME NOT NULL,
                        [Type] [varchar](100) NOT NULL,
                        [Data] [varchar](5000) NOT NULL,
                        [ProcessedDate] DATETIME NULL
                    )",
                DatabaseType.SqlServer => $"CREATE TABLE {inboxPatternConfig.Schema}.{inboxPatternConfig.TableName} (" +
                    "[Id] UNIQUEIDENTIFIER NOT NULL, " +
                    "[OccurredOn] DATETIME NOT NULL, " +
                    "[Type] NVARCHAR(255) NOT NULL, " +
                    "[Data] NVARCHAR(MAX) NOT NULL, " +
                    "[ProcessedDate] DATETIME NULL, " +
                    $"CONSTRAINT [PK_{inboxPatternConfig.TableName}] PRIMARY KEY CLUSTERED ( [Id] ASC ) " +
                    ")",
                DatabaseType.MySql => $"CREATE TABLE `{inboxPatternConfig.Schema} `.` {inboxPatternConfig.TableName}` (" +
                    "Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, " +
                    "OccurredOn DATETIME NOT NULL, " +
                    "Type VARCHAR(255) NOT NULL, " +
                    "Data LONGTEXT NOT NULL, " +
                    "ProcessedDate DATETIME NULL)",
                DatabaseType.PostgreSql => $@"CREATE TABLE {inboxPatternConfig.Schema}.{inboxPatternConfig.TableName} (
                        Id UUID PRIMARY KEY,
                        OccurredOn TIMESTAMP NOT NULL,
                        Type VARCHAR(255) NOT NULL,
                        Data TEXT NOT NULL,
                        ProcessedDate DATETIME NULL
                    )",
                _ => throw new NotImplementedException()
            };
        }

        public static string GenerateInsertInboxMessageSqlStatement(InboxTransactionalPatternConfiguration inboxPatternConfig)
        {
            return inboxPatternConfig.DatabaseType switch
            {
                DatabaseType.SqlServer => $"INSERT INTO [{inboxPatternConfig.Schema}].[{inboxPatternConfig.TableName}] ([Id], [OccurredOn], [Type], [Data]) VALUES (@Id, @OccurredOn, @Type, @Data)",
                DatabaseType.PostgreSql => $"INSERT INTO {inboxPatternConfig.Schema}.{inboxPatternConfig.TableName} (Id, OccurredOn, Type, Data) VALUES (@Id, @OccurredOn, @Type, @Data)",
                DatabaseType.MySql => $"INSERT INTO '{inboxPatternConfig.Schema} '.' {inboxPatternConfig.TableName}' (Id, OccurredOn, Type, Data) VALUES (@Id, @OccurredOn, @Type, @Data)",
                DatabaseType.Sqlite => $"INSERT INTO {inboxPatternConfig.Schema} . {inboxPatternConfig.TableName} (Id, OccurredOn, Type, Data) VALUES (@Id, @OccurredOn, @Type, @Data)",
                _ => throw new NotImplementedException(),
            };
        }

        public static string GenerateGetExistingEventsSqlStatement<T>(InboxTransactionalPatternConfiguration inboxPatternConfig, T @event)
        {
            return inboxPatternConfig.DatabaseType switch
            {
                DatabaseType.SqlServer => $"SELECT COUNT(Id) FROM {inboxPatternConfig.Schema}.{inboxPatternConfig.TableName} WHERE Id = @Id",
                DatabaseType.PostgreSql => $"SELECT COUNT(Id) FROM {inboxPatternConfig.Schema}.{inboxPatternConfig.TableName} WHERE Id = @Id",
                DatabaseType.MySql => $"SELECT COUNT(Id) FROM '{inboxPatternConfig.Schema}'.'{inboxPatternConfig.TableName}' WHERE Id = @Id",
                DatabaseType.Sqlite => $"SELECT COUNT(Id) FROM {inboxPatternConfig.Schema}.{inboxPatternConfig.TableName} WHERE Id = @Id",
                _ => throw new NotImplementedException($"Invalid DBMS specified")
            };
        }
    }
}
