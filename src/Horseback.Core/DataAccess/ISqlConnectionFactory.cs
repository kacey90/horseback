using System.Data;

namespace Horseback.Core.DataAccess
{
    public interface ISqlConnectionFactory
    {
        string GetConnectionString();

        IDbConnection CreateConnection(string? connectionString = null);

        IDbConnection GetOpenConnection();
    }
}
