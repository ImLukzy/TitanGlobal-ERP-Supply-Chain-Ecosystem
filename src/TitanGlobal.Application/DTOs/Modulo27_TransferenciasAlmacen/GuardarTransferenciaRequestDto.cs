namespace TitanGlobal.Application.DTOs.Modulo27_TransferenciasAlmacen;

public class GuardarTransferenciaRequestDto
{
    public int ProductoId { get; set; }

    public int AlmacenOrigenId { get; set; }

    public int AlmacenDestinoId { get; set; }

    public int Cantidad { get; set; }

    public string? Observaciones { get; set; }
}
