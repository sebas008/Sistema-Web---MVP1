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
        // No abrir aquí. Cada repositorio maneja OpenAsync/Dispose.
        // Si abrimos aquí y luego el repositorio llama OpenAsync(), SQLClient lanza:
        // "La conexión no se cerró. El estado actual de la conexión es abierta."
        return new SqlConnection(_connectionString);
    }
}
