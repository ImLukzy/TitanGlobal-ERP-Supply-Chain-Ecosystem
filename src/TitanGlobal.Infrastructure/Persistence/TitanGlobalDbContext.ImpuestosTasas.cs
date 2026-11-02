using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
namespace TitanGlobal.Infrastructure.Persistence;

public partial class TitanGlobalDbContext
{
    public DbSet<Impuesto> Impuestos { get; set; } = null!;

    public DbSet<TasaImpuesto> TasasImpuestos { get; set; } = null!;
}
