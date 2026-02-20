using CCAT.Mvp1.Api.Interfaces;
using CCAT.Mvp1.Api.Repositories;
using CCAT.Mvp1.Api.Services;
using CCAT.Mvp1.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// =====================
// Core
// =====================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =====================
// Dependency Injection
// =====================

// DB
builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();

// Usuarios
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();

// Productos
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IProductoService, ProductoService>();

//Auth
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

//Inventario Repuestos
builder.Services.AddScoped<IInventarioRepuestoRepository, InventarioRepuestoRepository>();
builder.Services.AddScoped<IInventarioRepuestoService, InventarioRepuestoService>();

//Nuevos Vehículos Nuevos
builder.Services.AddScoped<IVehiculoNuevoRepository, VehiculoNuevoRepository>();
builder.Services.AddScoped<IVehiculoNuevoService, VehiculoNuevoService>();

//Contabilidad - Facturación
builder.Services.AddScoped<IFacturacionRepository, FacturacionRepository>();
builder.Services.AddScoped<IFacturacionService, FacturacionService>();

//Contabilidad - Compras
builder.Services.AddScoped<IComprasRepository, ComprasRepository>();
builder.Services.AddScoped<IComprasService, ComprasService>();

//Contabilidad - Guias
builder.Services.AddScoped<IGuiasRepository, GuiasRepository>();
builder.Services.AddScoped<IGuiasService, GuiasService>();

//Clientes
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IClienteService, ClienteService>();

//OrdenServicios
builder.Services.AddScoped<IOrdenServicioRepository, OrdenServicioRepository>();
builder.Services.AddScoped<IOrdenServicioService, OrdenServicioService>();

// =====================
// App
// =====================
var app = builder.Build();

// =====================
// Middleware
// =====================

// Manejo global de errores
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
