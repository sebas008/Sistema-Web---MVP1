using Microsoft.Data.SqlClient;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new Exception("No existe DefaultConnection en appsettings.");
    }

    public SqlConnection CreateConnection()
    {
        var cn = new SqlConnection(_connectionString);
        cn.Open();
        return cn;
    }
}
