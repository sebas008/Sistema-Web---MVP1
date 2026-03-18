using CCAT.Mvp1.Api.Interfaces;
using CCAT.Mvp1.Api.Middlewares;
using CCAT.Mvp1.Api.Repositories;
using CCAT.Mvp1.Api.Services;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CCAT MVP1 API", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontDev", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
    );
});

// ✅ DB factory (esto faltaba)
builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();

// ✅ DI: Auth
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ✅ DI: Usuarios
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();

// ✅ DI: Clientes
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IClienteService, ClienteService>();

// ✅ DI: Productos
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IProductoService, ProductoService>();

// ✅ DI: Inventario repuestos / stock
builder.Services.AddScoped<IInventarioRepuestoRepository, InventarioRepuestoRepository>();
builder.Services.AddScoped<IInventarioRepuestoService, InventarioRepuestoService>();

// ✅ DI: Vehículos nuevos
builder.Services.AddScoped<IVehiculoNuevoRepository, VehiculoNuevoRepository>();
builder.Services.AddScoped<IVehiculoNuevoService, VehiculoNuevoService>();

// ✅ DI: Compras
builder.Services.AddScoped<IComprasRepository, ComprasRepository>();
builder.Services.AddScoped<IComprasService, ComprasService>();

// ✅ DI: Facturación
builder.Services.AddScoped<IFacturacionRepository, FacturacionRepository>();
builder.Services.AddScoped<IFacturacionService, FacturacionService>();

// ✅ DI: Guías
builder.Services.AddScoped<IGuiasRepository, GuiasRepository>();
builder.Services.AddScoped<IGuiasService, GuiasService>();

// ✅ DI: Ordenes de servicio
builder.Services.AddScoped<IOrdenServicioRepository, OrdenServicioRepository>();
builder.Services.AddScoped<IOrdenServicioService, OrdenServicioService>();

// ✅ DI: Proveedores
builder.Services.AddScoped<IProveedorRepository, ProveedorRepository>();
builder.Services.AddScoped<IProveedorService, ProveedorService>();

// ✅ DI: Roles
builder.Services.AddScoped<IRolRepository, RolRepository>();
builder.Services.AddScoped<IRolService, RolService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("FrontDev");
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseHttpsRedirection();
// SPA static files (si publicas el front en wwwroot, esto evita el Cannot GET al refrescar rutas)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();
// Fallback para rutas SPA (Angular)
app.MapFallbackToFile("index.html");

app.Run();