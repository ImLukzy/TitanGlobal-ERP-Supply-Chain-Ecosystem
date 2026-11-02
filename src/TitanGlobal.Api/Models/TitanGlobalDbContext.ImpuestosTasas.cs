using Microsoft.EntityFrameworkCore;
namespace TitanGlobal.Api.Models;

public partial class TitanGlobalDbContext
{
    public DbSet<Impuesto> Impuestos { get; set; } = null!;

    public DbSet<TasaImpuesto> TasasImpuestos { get; set; } = null!;
}
