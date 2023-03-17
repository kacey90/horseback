namespace Horseback.Core.InboxPattern.Config
{
    public class InboxTransactionalPatternConfiguration
    {
        public InboxTransactionalPatternConfiguration(
            string tableName,
            string dbConnectionString,
            string schema = "dbo",
            DatabaseType databaseType = DatabaseType.SqlServer)
        {
            TableName = tableName;
            DbConnectionString = dbConnectionString;
            Schema = schema;
            DatabaseType = databaseType;
        }

        public string TableName { get; private set; }
        public string DbConnectionString { get; private set; }
        public string Schema { get; private set; }
        public DatabaseType DatabaseType { get; private set; }
    }
}
