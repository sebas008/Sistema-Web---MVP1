using Microsoft.Data.SqlClient;

namespace CCAT.Mvp1.Api.Interfaces;

public interface IDbConnectionFactory
{
    SqlConnection CreateConnection();
}
