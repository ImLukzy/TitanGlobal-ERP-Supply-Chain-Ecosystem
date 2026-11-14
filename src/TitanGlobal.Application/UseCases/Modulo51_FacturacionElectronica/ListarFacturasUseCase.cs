using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo51_FacturacionElectronica;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo51_FacturacionElectronica;

public class ListarFacturasUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarFacturasUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaFacturasDto> ExecuteAsync(
        string? busqueda = null,
        int? clienteId = null,
        string? tipoComprobante = null,
        string? estado = null,
        string? moneda = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int pagina = 1,
        int tamanoPagina = 20)
    {
        if (pagina <= 0)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La página debe ser mayor que cero.");
        }

        if (tamanoPagina <= 0 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El tamaño de página debe estar entre 1 y 100.");
        }

        if (clienteId.HasValue && clienteId.Value <= 0)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El cliente seleccionado no es válido.");
        }

        long desplazamiento =
            ((long)pagina - 1) * tamanoPagina;

        if (desplazamiento > int.MaxValue)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La página solicitada supera el límite permitido.");
        }

        IGenericRepository<Factura> repositorio =
            _unitOfWork.Repository<Factura>();

        IQueryable<Factura> consulta = repositorio
            .AsQueryable()
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            string texto = busqueda.Trim().ToLowerInvariant();

            consulta = consulta.Where(factura =>
                factura.Cliente.Nombre.ToLower().Contains(texto) ||
                factura.Cliente.Ruc.ToLower().Contains(texto) ||
                factura.Serie.ToLower().Contains(texto) ||
                factura.Numero.ToString().Contains(texto) ||
                (factura.Serie.ToLower() + "-" +
                    factura.Numero.ToString()).Contains(texto));

            string[] partes = texto.Split(
                '-',
                StringSplitOptions.RemoveEmptyEntries);

            if (partes.Length == 2 &&
                int.TryParse(partes[1], out int numero))
            {
                string serie = partes[0];

                consulta = repositorio
                    .AsQueryable()
                    .AsNoTracking()
                    .Where(factura =>
                        factura.Serie.ToLower() == serie &&
                        factura.Numero == numero);
            }
        }

        if (clienteId.HasValue)
        {
            consulta = consulta.Where(
                factura => factura.ClienteId == clienteId.Value);
        }

        if (!string.IsNullOrWhiteSpace(tipoComprobante))
        {
            string tipoNormalizado =
                ValidacionFactura.NormalizarTipoComprobante(
                    tipoComprobante);

            consulta = consulta.Where(
                factura =>
                    factura.TipoComprobante == tipoNormalizado);
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            string estadoNormalizado =
                NormalizarEstado(estado);

            consulta = consulta.Where(
                factura => factura.Estado == estadoNormalizado);
        }

        if (!string.IsNullOrWhiteSpace(moneda))
        {
            string monedaNormalizada =
                moneda.Trim().ToUpperInvariant();

            if (monedaNormalizada.Length != 3 ||
                !monedaNormalizada.All(
                    caracter => caracter >= 'A' && caracter <= 'Z'))
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status400BadRequest,
                    "La moneda debe tener un código de tres letras.");
            }

            consulta = consulta.Where(
                factura => factura.Moneda == monedaNormalizada);
        }

        DateTime? desde = fechaDesde.HasValue
            ? FechasUtc.Normalizar(fechaDesde.Value)
            : null;

        DateTime? hasta = fechaHasta.HasValue
            ? FechasUtc.Normalizar(fechaHasta.Value)
            : null;

        if (desde.HasValue &&
            hasta.HasValue &&
            desde.Value > hasta.Value)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La fecha inicial no puede ser posterior a la fecha final.");
        }

        if (desde.HasValue)
        {
            consulta = consulta.Where(
                factura => factura.FechaEmision >= desde.Value);
        }

        if (hasta.HasValue)
        {
            consulta = consulta.Where(
                factura => factura.FechaEmision <= hasta.Value);
        }

        int totalRegistros = await consulta.CountAsync();

        List<FacturaListadoDto> facturas = await consulta
            .OrderByDescending(factura => factura.FechaEmision)
            .ThenByDescending(factura => factura.Id)
            .Skip((int)desplazamiento)
            .Take(tamanoPagina)
            .Select(factura => new FacturaListadoDto
            {
                Id = factura.Id,
                ClienteId = factura.ClienteId,
                ClienteNombre = factura.Cliente.Nombre,
                ClienteRuc = factura.Cliente.Ruc,
                TipoComprobante = factura.TipoComprobante,
                Serie = factura.Serie,
                Numero = factura.Numero,
                FechaEmision = factura.FechaEmision,
                FechaVencimiento = factura.FechaVencimiento,
                Moneda = factura.Moneda,
                Estado = factura.Estado,
                Subtotal = factura.Subtotal,
                TotalDescuento = factura.TotalDescuento,
                TotalImpuesto = factura.TotalImpuesto,
                Total = factura.Total,
                CantidadDetalles = factura.DetallesFactura.Count
            })
            .ToListAsync();

        foreach (FacturaListadoDto factura in facturas)
        {
            factura.NumeroComprobante =
                $"{factura.Serie}-{factura.Numero:D8}";
        }

        int totalPaginas = (int)Math.Ceiling(
            totalRegistros / (decimal)tamanoPagina);

        return new PaginaFacturasDto
        {
            Items = facturas,
            Pagina = pagina,
            TamanoPagina = tamanoPagina,
            TotalRegistros = totalRegistros,
            TotalPaginas = totalPaginas
        };
    }

    private static string NormalizarEstado(string estado)
    {
        string valor = estado.Trim();

        if (string.Equals(
            valor,
            ValidacionFactura.EstadoBorrador,
            StringComparison.OrdinalIgnoreCase))
        {
            return ValidacionFactura.EstadoBorrador;
        }

        if (string.Equals(
            valor,
            ValidacionFactura.EstadoEmitida,
            StringComparison.OrdinalIgnoreCase))
        {
            return ValidacionFactura.EstadoEmitida;
        }

        if (string.Equals(
            valor,
            ValidacionFactura.EstadoAnulada,
            StringComparison.OrdinalIgnoreCase))
        {
            return ValidacionFactura.EstadoAnulada;
        }

        throw new ReglaNegocioException(
            CodigosEstado.Status400BadRequest,
            "El estado debe ser Borrador, Emitida o Anulada.");
    }
}
