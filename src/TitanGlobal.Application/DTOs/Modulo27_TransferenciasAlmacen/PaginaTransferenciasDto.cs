namespace TitanGlobal.Application.DTOs.Modulo27_TransferenciasAlmacen;

public class PaginaTransferenciasDto
{
    public List<TransferenciaListadoDto> Items { get; set; } = new List<TransferenciaListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
