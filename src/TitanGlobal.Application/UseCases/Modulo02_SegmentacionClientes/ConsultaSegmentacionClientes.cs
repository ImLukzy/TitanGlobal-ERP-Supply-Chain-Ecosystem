using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo02_SegmentacionClientes;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo02_SegmentacionClientes;

public static class ConsultaSegmentacionClientes
{
    public static async Task<ClienteClasificadoDto?> ObtenerClienteClasificadoAsync(int clienteId, IUnitOfWork unitOfWork)
    {
        List<ClienteClasificadoDto> clientes = await ObtenerClientesAsync(unitOfWork);
        return clientes.SingleOrDefault(cliente => cliente.ClienteId == clienteId);
    }

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
