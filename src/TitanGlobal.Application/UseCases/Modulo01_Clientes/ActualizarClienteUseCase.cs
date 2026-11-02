using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo01_Clientes;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo01_Clientes;

public class ActualizarClienteUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarClienteUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, GuardarClienteRequestDto solicitud)
    {
        Cliente? cliente = await _unitOfWork.Repository<Cliente>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (cliente is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El cliente {id} no existe.");
        }

        string nombre = ValidacionCliente.NormalizarNombre(solicitud.Nombre);
        string ruc = ValidacionCliente.NormalizarRuc(solicitud.Ruc);
        bool rucExiste = await _unitOfWork.Repository<Cliente>()
            .AsQueryable()
            .AnyAsync(entidad => entidad.Id != id && entidad.Ruc == ruc);
        if (rucExiste)
        {
            throw CrearConflictoRucException(ruc);
        }

        cliente.Nombre = nombre;
        cliente.Ruc = ruc;
        cliente.Descripcion = ValidacionCliente.NormalizarDescripcion(solicitud.Descripcion);
        _unitOfWork.Repository<Cliente>().Update(cliente);

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
