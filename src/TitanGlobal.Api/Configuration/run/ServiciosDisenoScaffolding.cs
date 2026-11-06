using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;

namespace TitanGlobal.Api.Configuration.Scaffolding;

// dotnet ef lo descubre en el proyecto de arranque y lo usa solo al hacer scaffold.
public class ServiciosDisenoScaffolding : IDesignTimeServices
{
    public void ConfigureDesignTimeServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IPluralizer, PluralizadorEspanol>();
#pragma warning disable EF1001 // Servicio interno de EF Core, solo en tiempo de diseño.
        serviceCollection.AddSingleton<ICandidateNamingService, NombresNavegacion>();
#pragma warning restore EF1001
    }
}
