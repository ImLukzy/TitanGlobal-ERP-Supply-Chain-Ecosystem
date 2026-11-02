namespace TitanGlobal.Domain.Models;

public class Promocion
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public int? ProductoId { get; set; }

    public decimal? MontoMinimo { get; set; }

    public decimal PorcentajeDescuento { get; set; }

    public int? SegmentoClienteId { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public bool Activa { get; set; } = true;

    public virtual SegmentoCliente? SegmentoCliente { get; set; }
}
