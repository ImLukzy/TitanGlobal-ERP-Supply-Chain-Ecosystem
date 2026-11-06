using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.Services;

namespace TitanGlobal.Infrastructure.Persistence;

// Escrito a mano: el scaffold no genera constructores (ver CodeTemplates/EFCore/DbContext.t4).
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
