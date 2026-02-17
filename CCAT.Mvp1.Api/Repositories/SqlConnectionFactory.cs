using System.Data;
using Microsoft.Data.SqlClient;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration _config;

    public SqlConnectionFactory(IConfiguration config)
    {
        _config = config;
    }

    public IDbConnection CreateConnection()
    {
        return new SqlConnection(
            _config.GetConnectionString("DefaultConnection")
        );
    }
}
