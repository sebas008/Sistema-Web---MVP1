using System.Data;

namespace CCAT.Mvp1.Api.Interfaces;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
