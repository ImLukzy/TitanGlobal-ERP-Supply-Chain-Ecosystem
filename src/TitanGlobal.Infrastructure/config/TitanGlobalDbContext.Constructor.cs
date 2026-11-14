using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.Services;

// Parte escrita a mano de TitanGlobalDbContext (clase partial). Vive fuera de Persistence/ para que esa carpeta
// solo tenga lo que genera scripts/scaffold.sh; conserva el namespace Persistence para unirse a la misma clase.
namespace TitanGlobal.Infrastructure.Persistence;

public partial class TitanGlobalDbContext : DbContext
{
    private readonly IContextoSolicitud? _contextoSolicitud;

    public TitanGlobalDbContext(
        DbContextOptions<TitanGlobalDbContext> options,
        IContextoSolicitud? contextoSolicitud = null) : base(options)
    {
        _contextoSolicitud = contextoSolicitud;
    }
}
