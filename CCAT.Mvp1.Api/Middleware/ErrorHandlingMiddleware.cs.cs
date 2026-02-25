using System.Text.Json;

namespace CCAT.Mvp1.Api.Middlewares;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;

    public ErrorHandlingMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            // Importante: en C# los tipos anónimos deben coincidir en el operador condicional.
            // Usamos 'object' para evitar error de compilación.
            object payload = _env.IsDevelopment()
                ? new { error = "Error interno del servidor", detail = ex.Message, stack = ex.StackTrace }
                : new { error = "Error interno del servidor" };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
