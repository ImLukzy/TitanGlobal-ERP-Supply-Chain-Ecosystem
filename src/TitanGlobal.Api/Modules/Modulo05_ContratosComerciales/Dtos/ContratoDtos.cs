namespace TitanGlobal.Api.Modules.Modulo05_ContratosComerciales.Dtos;

public class GuardarContratoRequestDto
{
    public int ClienteId { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public decimal MontoComprometido { get; set; }

    public string CondicionPago { get; set; } = string.Empty;

    public string? Observaciones { get; set; }
}

public class CancelarContratoRequestDto
{
    public string Motivo { get; set; } = string.Empty;
}

public class ContratoCreadoDto
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;
}

public class PaginaContratosDto
{
    public List<ContratoListadoDto> Items { get; set; } = new List<ContratoListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ContratoListadoDto
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int ClienteId { get; set; }

    public string ClienteNombre { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public string Estado { get; set; } = string.Empty;

    public bool Vencido { get; set; }

    public decimal MontoComprometido { get; set; }

    public decimal MontoConsumido { get; set; }

    public decimal PorcentajeConsumido { get; set; }
}

public class OrdenPeriodoDto
{
    public int OrdenId { get; set; }

    public string NumeroOrden { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public decimal Total { get; set; }
}

public class ContratoDetalleDto : ContratoListadoDto
{
    public string CondicionPago { get; set; } = string.Empty;

    public string? Observaciones { get; set; }

    public string? MotivoCancelacion { get; set; }

    public DateTime? FechaActivacion { get; set; }

    public DateTime? FechaCierre { get; set; }

    public decimal Saldo { get; set; }

    public List<OrdenPeriodoDto> OrdenesPeriodo { get; set; } = new List<OrdenPeriodoDto>();
}

public class ResumenContratosDto
{
    public int Total { get; set; }

    public int Borradores { get; set; }

    public int Vigentes { get; set; }

    public int Vencidos { get; set; }

    public int Finalizados { get; set; }

    public int Cancelados { get; set; }

    public decimal MontoComprometidoVigente { get; set; }

    public decimal MontoConsumidoVigente { get; set; }

    public decimal PorcentajeConsumoVigente { get; set; }

    public List<CondicionContratosDto> PorCondicion { get; set; } = new List<CondicionContratosDto>();
}

public class CondicionContratosDto
{
    public string CondicionPago { get; set; } = string.Empty;

    public int Contratos { get; set; }
}
