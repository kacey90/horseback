namespace MessageBroker.Wrapper.Core.EventBus.Config
{
    public class InboxTransactionalPatternConfiguration
    {
        public InboxTransactionalPatternConfiguration(string tableName, string dbConnectionString)
        {
            TableName = tableName;
            DbConnectionString = dbConnectionString;
        }

        public string TableName { get; private set; }
        public string DbConnectionString { get; private set; }
    }
}
