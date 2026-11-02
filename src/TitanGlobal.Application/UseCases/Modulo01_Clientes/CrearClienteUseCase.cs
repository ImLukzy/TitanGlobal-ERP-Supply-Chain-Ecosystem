using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo01_Clientes;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo01_Clientes;

public class CrearClienteUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearClienteUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(GuardarClienteRequestDto solicitud)
    {
        string nombre = ValidacionCliente.NormalizarNombre(solicitud.Nombre);
        string ruc = ValidacionCliente.NormalizarRuc(solicitud.Ruc);
        bool rucExiste = await _unitOfWork.Repository<Cliente>()
            .AsQueryable()
            .AnyAsync(cliente => cliente.Ruc == ruc);
        if (rucExiste)
        {
            throw CrearConflictoRucException(ruc);
        }

        Cliente cliente = new Cliente
        {
            Nombre = nombre,
            Ruc = ruc,
            Descripcion = ValidacionCliente.NormalizarDescripcion(solicitud.Descripcion)
        };
        await _unitOfWork.Repository<Cliente>().AddAsync(cliente);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw CrearConflictoRucException(ruc);
        }

        return cliente.Id;
    }

    private static ReglaNegocioException CrearConflictoRucException(string ruc)
    {
        return new ReglaNegocioException(CodigosEstado.Status409Conflict, $"Ya existe un cliente con el RUC {ruc}.");
    }
}
