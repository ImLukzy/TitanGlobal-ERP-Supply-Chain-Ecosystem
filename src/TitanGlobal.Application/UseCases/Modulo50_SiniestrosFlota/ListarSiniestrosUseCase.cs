using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo50_SiniestrosFlota;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo50_SiniestrosFlota;

public class ListarSiniestrosUseCase
{
    private readonly IUnitOfWork _unidad;
    public ListarSiniestrosUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task<PaginaSiniestrosDto> ExecuteAsync(string? busqueda, string? estado, string? tipo, int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw ValidacionSiniestro.Error("El número de página debe ser mayor que cero.");
        }
        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw ValidacionSiniestro.Error("El tamaño de página debe estar entre 1 y 100.");
        }
        IQueryable<SiniestroFlota> consulta = _unidad.Repository<SiniestroFlota>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(estado))
        {
            string? normalizado = new[] { "Registrado", "EnReclamo", "Cerrado", "Anulado" }.FirstOrDefault(e => e.Equals(estado.Trim(), StringComparison.OrdinalIgnoreCase));
            if (normalizado is null)
            {
                throw ValidacionSiniestro.Error("El estado debe ser Registrado, EnReclamo, Cerrado o Anulado.");
            }
            consulta = consulta.Where(r => r.Estado == normalizado);
        }
        if (!string.IsNullOrWhiteSpace(tipo))
        {
            string normalizado = ValidacionSiniestro.Tipo(tipo);
            consulta = consulta.Where(r => r.Tipo == normalizado);
        }
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            string termino = busqueda.Trim().ToLowerInvariant();
            consulta = consulta.Where(r => r.Codigo.ToLower().Contains(termino) || r.Placa.ToLower().Contains(termino) || r.Lugar.ToLower().Contains(termino));
        }
        int total = await consulta.CountAsync();
        long salto = (long)(pagina - 1) * tamanoPagina;
        List<SiniestroListadoDto> items = new List<SiniestroListadoDto>();
        if (salto < total)
        {
            List<SiniestroFlota> siniestros = await consulta.OrderBy(r => r.Codigo).ThenBy(r => r.Id).Skip((int)salto).Take(tamanoPagina).ToListAsync();
            foreach (SiniestroFlota siniestro in siniestros)
            {
                items.Add(PresentacionSiniestro.Listado(siniestro));
            }
        }
        return new PaginaSiniestrosDto { Items = items, Total = total, Pagina = pagina, TamanoPagina = tamanoPagina };
    }
}
