using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo47_ControlCombustible;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo47_ControlCombustible;

public class ListarValesUseCase
{
    private readonly IUnitOfWork _unidad;

    public ListarValesUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaValesDto> ExecuteAsync(string? busqueda, string? estado, string? tipoCombustible, int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw ValidacionVale.Error("El número de página debe ser mayor que cero.");
        }
        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw ValidacionVale.Error("El tamaño de página debe estar entre 1 y 100.");
        }
        IQueryable<ValeCombustible> consulta = _unidad.Repository<ValeCombustible>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(estado))
        {
            string? normalizado = new[] { "Borrador", "Confirmado", "Anulado" }.FirstOrDefault(e => e.Equals(estado.Trim(), StringComparison.OrdinalIgnoreCase));
            if (normalizado is null)
            {
                throw ValidacionVale.Error("El estado debe ser Borrador, Confirmado o Anulado.");
            }
            consulta = consulta.Where(v => v.Estado == normalizado);
        }
        if (!string.IsNullOrWhiteSpace(tipoCombustible))
        {
            string tipo = ValidacionVale.Tipo(tipoCombustible);
            consulta = consulta.Where(v => v.TipoCombustible == tipo);
        }
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            string termino = busqueda.Trim().ToLowerInvariant();
            consulta = consulta.Where(v => v.Codigo.ToLower().Contains(termino) || v.Placa.ToLower().Contains(termino) || v.Estacion.ToLower().Contains(termino));
        }
        int total = await consulta.CountAsync();
        long salto = (long)(pagina - 1) * tamanoPagina;
        List<ValeListadoDto> items = new List<ValeListadoDto>();
        if (salto < total)
        {
            List<ValeCombustible> vales = await consulta.OrderBy(v => v.Codigo).ThenBy(v => v.Id).Skip((int)salto).Take(tamanoPagina).ToListAsync();
            foreach (ValeCombustible vale in vales)
            {
                items.Add(CalculoVale.Listado(vale));
            }
        }
        return new PaginaValesDto { Items = items, Total = total, Pagina = pagina, TamanoPagina = tamanoPagina };
    }
}
