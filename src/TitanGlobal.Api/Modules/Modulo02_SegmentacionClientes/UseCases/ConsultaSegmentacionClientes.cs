using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.UseCases;

internal static class ConsultaSegmentacionClientes
{
    public static async Task<List<ClienteClasificadoDto>> ObtenerClientesAsync(IUnitOfWork unitOfWork)
    {
        List<SegmentoCliente> segmentos = await unitOfWork.Repository<SegmentoCliente>()
            .AsQueryable()
            .Where(segmento => segmento.Activo)
            .OrderBy(segmento => segmento.Prioridad)
            .ToListAsync();

        List<ClienteBaseConsulta> clientes = await unitOfWork.Repository<Cliente>()
            .AsQueryable()
            .OrderBy(cliente => cliente.Id)
            .Select(cliente => new ClienteBaseConsulta
            {
                Id = cliente.Id,
                Nombre = cliente.Nombre,
                Ruc = cliente.Ruc
            })
            .ToListAsync();

        // PATRÓN LINQ 9: una agregación agrupada obtiene todos los totales sin consultar por cliente.
        List<OrdenesClienteConsulta> totales = await unitOfWork.Repository<OrdenCompra>()
            .AsQueryable()
            .GroupBy(orden => orden.ClienteId)
            .Select(grupo => new OrdenesClienteConsulta
            {
                ClienteId = grupo.Key,
                TotalOrdenes = grupo.Count(),
                MontoTotal = grupo.Sum(orden => orden.Total)
            })
            .ToListAsync();

        Dictionary<int, OrdenesClienteConsulta> totalesPorCliente = totales.ToDictionary(total => total.ClienteId);
        List<ClienteClasificadoDto> resultado = new List<ClienteClasificadoDto>();

        foreach (ClienteBaseConsulta cliente in clientes)
        {
            OrdenesClienteConsulta? total = totalesPorCliente.GetValueOrDefault(cliente.Id);
            int totalOrdenes = total?.TotalOrdenes ?? 0;
            decimal montoTotal = total?.MontoTotal ?? 0m;
            SegmentoCliente? segmentoAsignado = segmentos.FirstOrDefault(segmento =>
                montoTotal >= segmento.MontoMinimo && totalOrdenes >= segmento.OrdenesMinimas);

            resultado.Add(new ClienteClasificadoDto
            {
                ClienteId = cliente.Id,
                Nombre = cliente.Nombre,
                Ruc = cliente.Ruc,
                TotalOrdenes = totalOrdenes,
                MontoTotal = montoTotal,
                SegmentoId = segmentoAsignado?.Id,
                SegmentoCodigo = segmentoAsignado?.Codigo,
                SegmentoNombre = segmentoAsignado?.Nombre
            });
        }

        return resultado;
    }

    private sealed class ClienteBaseConsulta
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Ruc { get; set; } = string.Empty;
    }

    private sealed class OrdenesClienteConsulta
    {
        public int ClienteId { get; set; }
        public int TotalOrdenes { get; set; }
        public decimal MontoTotal { get; set; }
    }
}
