using Microsoft.EntityFrameworkCore;
using Npgsql;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo01_Clientes.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo01_Clientes.UseCases;

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
            throw new ReglaNegocioException(StatusCodes.Status404NotFound, $"El cliente {id} no existe.");
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
        catch (DbUpdateException excepcion) when (excepcion.InnerException is PostgresException excepcionPostgres
            && excepcionPostgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw CrearConflictoRucException(ruc);
        }

        return cliente.Id;
    }

    private static ReglaNegocioException CrearConflictoRucException(string ruc)
    {
        return new ReglaNegocioException(StatusCodes.Status409Conflict, $"Ya existe un cliente con el RUC {ruc}.");
    }
}
