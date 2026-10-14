using Microsoft.EntityFrameworkCore;
using Npgsql;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo01_Clientes.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo01_Clientes.UseCases;

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
