using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Infrastructure.Persistence;

public partial class TitanGlobalDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // El índice único filtrado se aplica a la versión activa. Un producto puede tener varias versiones.
        modelBuilder.Entity<Producto>().Ignore(producto => producto.ListaMateriales);
        modelBuilder.Entity<ListaMateriales>()
            .HasOne(lista => lista.Producto)
            .WithMany()
            .HasForeignKey(lista => lista.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
