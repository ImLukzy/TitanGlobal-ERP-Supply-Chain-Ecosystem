using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo55_TesoreriaBancos;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo55_TesoreriaBancos;

public class ActualizarCuentaBancariaUseCase
{
    private readonly IUnitOfWorkTesoreria _unitOfWork;

    public ActualizarCuentaBancariaUseCase(IUnitOfWorkTesoreria unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<CuentaBancariaDetalleDto> ExecuteAsync(int id, GuardarCuentaBancariaRequestDto solicitud)
    {
        CuentaBancaria datos = ValidacionTesoreriaBancos.DatosCuenta(solicitud);
        return _unitOfWork.ConCuentasBloqueadasAsync([id], async () =>
        {
            CuentaBancaria actual = await ConsultasTesoreriaBancos.CuentaAsync(_unitOfWork, id);
            if (actual.Moneda != datos.Moneda && await ConsultasTesoreriaBancos.Movimientos(_unitOfWork, id).AnyAsync())
            {
                throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "No se puede cambiar la moneda de una cuenta con movimientos.");
            }

            datos.Id = actual.Id;
            datos.FechaRegistro = actual.FechaRegistro;
            IGenericRepository<CuentaBancaria> repositorio = _unitOfWork.Repository<CuentaBancaria>();
            await ValidacionTesoreriaBancos.ValidarUnicidadAsync(repositorio, datos);

            repositorio.Update(datos);
            await ValidacionTesoreriaBancos.GuardarAsync(_unitOfWork);
            return await ConsultasTesoreriaBancos.DetalleCuentaAsync(_unitOfWork, id);
        });
    }
}
