namespace TitanGlobal.Domain.Models;

public class PermisoRol
{
    public int Id { get; set; }

    public int RolId { get; set; }

    public int ModuloNumero { get; set; }

    public bool PuedeLeer { get; set; }

    public bool PuedeEscribir { get; set; }
}
