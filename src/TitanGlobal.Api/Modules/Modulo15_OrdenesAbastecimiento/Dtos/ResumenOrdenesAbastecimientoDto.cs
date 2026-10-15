namespace TitanGlobal.Api.Modules.Modulo15_OrdenesAbastecimiento.Dtos;

public class ResumenOrdenesAbastecimientoDto
{
    public int Total { get; set; }

    public int Emitidas { get; set; }

    public int Recibidas { get; set; }

    public int Anuladas { get; set; }

    public int Atrasadas { get; set; }

    public decimal MontoPorRecibir { get; set; }

    public decimal MontoRecibido { get; set; }

    public List<ProveedorMontoDto> TopProveedores { get; set; } = new List<ProveedorMontoDto>();
}
