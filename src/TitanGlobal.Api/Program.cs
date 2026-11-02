using TitanGlobal.Api.Configuration;
using TitanGlobal.Api.Middlewares;
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationServices(builder.Configuration);
WebApplication app = builder.Build();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseRouting();
app.UseMiddleware<ParameterValidationMiddleware>();
app.UseMiddleware<RoleBasedAccessMiddleware>();
app.UseAuthorization();
app.MapControllers();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.Run();
public partial class Program
{
}
