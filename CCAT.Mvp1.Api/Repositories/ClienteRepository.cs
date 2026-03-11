using System.Data;
using Microsoft.Data.SqlClient;
using CCAT.Mvp1.Api.DTOs.Clientes;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly IDbConnectionFactory _factory;
    public ClienteRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<List<ClienteResponse>> ListarAsync(string? q, bool? soloActivos)
    {
        using var cn = _factory.CreateConnection();
        if (cn.State != ConnectionState.Open) await cn.OpenAsync();
        using var cmd = new SqlCommand("contabilidad.usp_Cliente_List", (SqlConnection)cn)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@Q", (object?)q ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SoloActivos", (object?)soloActivos ?? DBNull.Value);

        var list = new List<ClienteResponse>();
        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            list.Add(MapCliente(rd));
        }
        return list;
    }

    public async Task<ClienteResponse?> ObtenerAsync(int idCliente)
    {
        using var cn = _factory.CreateConnection();
        if (cn.State != ConnectionState.Open) await cn.OpenAsync();
        using var cmd = new SqlCommand("contabilidad.usp_Cliente_Get", (SqlConnection)cn)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@IdCliente", idCliente);

        using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync()) return null;

        return MapCliente(rd);
    }

    public async Task<int> CrearAsync(ClienteCrearRequest req)
    {
        using var cn = _factory.CreateConnection();
        if (cn.State != ConnectionState.Open) await cn.OpenAsync();
        using var cmd = new SqlCommand("contabilidad.usp_Cliente_Upsert", (SqlConnection)cn)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@IdCliente", DBNull.Value);
        cmd.Parameters.AddWithValue("@TipoDocumento", (object?)req.TipoDocumento ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@NumeroDocumento", (object?)req.NumeroDocumento ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RazonSocial", req.RazonSocial);
        cmd.Parameters.AddWithValue("@Direccion", (object?)req.Direccion ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Telefono", (object?)req.Telefono ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Email", (object?)req.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Activo", 1);
        cmd.Parameters.AddWithValue("@Usuario", req.Usuario);

        var idObj = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(idObj);
    }

    public async Task<int> ActualizarAsync(int idCliente, ClienteActualizarRequest req)
    {
        using var cn = _factory.CreateConnection();
        if (cn.State != ConnectionState.Open) await cn.OpenAsync();
        using var cmd = new SqlCommand("contabilidad.usp_Cliente_Upsert", (SqlConnection)cn)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@IdCliente", idCliente);
        cmd.Parameters.AddWithValue("@TipoDocumento", (object?)req.TipoDocumento ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@NumeroDocumento", (object?)req.NumeroDocumento ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RazonSocial", req.RazonSocial);
        cmd.Parameters.AddWithValue("@Direccion", (object?)req.Direccion ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Telefono", (object?)req.Telefono ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Email", (object?)req.Email ?? DBNull.Value);

        // OJO: el Upsert requiere @Activo y @Usuario, se los mandamos siempre:
        cmd.Parameters.AddWithValue("@Activo", 1);
        cmd.Parameters.AddWithValue("@Usuario", req.Usuario);

        var idObj = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(idObj);
    }

    public async Task CambiarEstadoAsync(int idCliente, bool activo)
    {
        using var cn = _factory.CreateConnection();
        if (cn.State != ConnectionState.Open) await cn.OpenAsync();
        using var cmd = new SqlCommand("contabilidad.usp_Cliente_CambiarEstado", (SqlConnection)cn)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@IdCliente", idCliente);
        cmd.Parameters.AddWithValue("@Activo", activo);

        await cmd.ExecuteNonQueryAsync();
    }

    private static ClienteResponse MapCliente(SqlDataReader rd)
    {
        return new ClienteResponse
        {
            IdCliente = rd.GetInt32(rd.GetOrdinal("IdCliente")),
            TipoDocumento = rd.IsDBNull(rd.GetOrdinal("TipoDocumento")) ? null : rd.GetString(rd.GetOrdinal("TipoDocumento")),
            NumeroDocumento = rd.IsDBNull(rd.GetOrdinal("NumeroDocumento")) ? null : rd.GetString(rd.GetOrdinal("NumeroDocumento")),
            RazonSocial = rd.GetString(rd.GetOrdinal("RazonSocial")),
            Direccion = rd.IsDBNull(rd.GetOrdinal("Direccion")) ? null : rd.GetString(rd.GetOrdinal("Direccion")),
            Telefono = rd.IsDBNull(rd.GetOrdinal("Telefono")) ? null : rd.GetString(rd.GetOrdinal("Telefono")),
            Email = rd.IsDBNull(rd.GetOrdinal("Email")) ? null : rd.GetString(rd.GetOrdinal("Email")),
            Activo = rd.GetBoolean(rd.GetOrdinal("Activo")),
            FechaCreacion = rd.GetDateTime(rd.GetOrdinal("FechaCreacion")),
            UsuarioCreacion = rd.IsDBNull(rd.GetOrdinal("UsuarioCreacion")) ? null : rd.GetString(rd.GetOrdinal("UsuarioCreacion")),
        };
    }
}