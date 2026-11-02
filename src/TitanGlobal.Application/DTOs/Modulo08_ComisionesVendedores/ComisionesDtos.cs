namespace TitanGlobal.Application.DTOs.Modulo08_ComisionesVendedores;

public class GuardarVendedorRequestDto
{
    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string? Email { get; set; }

    public decimal PorcentajeComision { get; set; }

    public bool Activo { get; set; }
}

public class AsignarVendedorRequestDto
{
    public int VendedorId { get; set; }
}

public class LiquidarMesRequestDto
{
    public int Anio { get; set; }

    public int Mes { get; set; }
}

public class IdDto
{
    public int Id { get; set; }
}

public class PaginaVendedoresDto
{
    public List<VendedorListadoDto> Items { get; set; } = new List<VendedorListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class VendedorListadoDto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public decimal PorcentajeComision { get; set; }

    public bool Activo { get; set; }

    public int OrdenesAsignadas { get; set; }
}

public class VendedorDetalleDto : VendedorListadoDto
{
    public string? Email { get; set; }

    public decimal VentasTotales { get; set; }
}

public class PaginaOrdenesVendedorDto
{
    public List<OrdenVendedorDto> Items { get; set; } = new List<OrdenVendedorDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class OrdenVendedorDto
{
    public int OrdenCompraId { get; set; }

    public string NumeroOrden { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public string ClienteNombre { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public int? VendedorId { get; set; }

    public string? VendedorCodigo { get; set; }

    public string? VendedorNombre { get; set; }
}

public class ComisionesMesDto
{
    public int Anio { get; set; }

    public int Mes { get; set; }

    public bool Liquidado { get; set; }

    public List<ComisionVendedorDto> Filas { get; set; } = new List<ComisionVendedorDto>();

    public decimal TotalVentas { get; set; }

    public decimal TotalComision { get; set; }
}

public class ComisionVendedorDto
{
    public int VendedorId { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public int Ordenes { get; set; }

    public decimal Ventas { get; set; }

    public decimal Porcentaje { get; set; }

    public decimal Comision { get; set; }

    public string Estado { get; set; } = string.Empty;

    public int? LiquidacionId { get; set; }
}

public class LiquidacionDto
{
    public int Id { get; set; }

    public int VendedorId { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public int Anio { get; set; }

    public int Mes { get; set; }

    public int Ordenes { get; set; }

    public decimal Ventas { get; set; }

    public decimal Porcentaje { get; set; }

    public decimal Comision { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateTime FechaCalculo { get; set; }

    public DateTime? FechaPago { get; set; }
}
