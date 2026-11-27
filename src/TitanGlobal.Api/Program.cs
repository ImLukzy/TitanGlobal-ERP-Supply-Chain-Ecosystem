using TitanGlobal.Api.Configuration;
using TitanGlobal.Api.Middlewares;
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationServices(builder.Configuration, builder.Environment);
WebApplication app = builder.Build();
app.UseMiddleware<ErrorHandlingMiddleware>();
if (app.Environment.IsDevelopment())
{
    // Swagger va antes de la autenticación: el navegador abre la página sin token y luego
    // se pega el token en Authorize. Los endpoints de la API siguen exigiendo token.
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseRouting();
app.UseMiddleware<ParameterValidationMiddleware>();
app.UseMiddleware<RoleBasedAccessMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}
app.Run();
public partial class Program
{
}
