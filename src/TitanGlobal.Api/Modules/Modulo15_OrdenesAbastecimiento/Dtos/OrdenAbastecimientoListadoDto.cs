namespace TitanGlobal.Api.Modules.Modulo15_OrdenesAbastecimiento.Dtos;

public class OrdenAbastecimientoListadoDto
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public int ProveedorId { get; set; }

    public string ProveedorNombre { get; set; } = string.Empty;

    public string? SolicitudNumero { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateTime FechaEntregaEstimada { get; set; }

    public bool Atrasada { get; set; }

    public decimal MontoTotal { get; set; }
}
