using TitanGlobal.Application.DTOs.Modulo55_TesoreriaBancos;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo55_TesoreriaBancos;

public class CrearCuentaBancariaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearCuentaBancariaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CuentaBancariaDetalleDto> ExecuteAsync(GuardarCuentaBancariaRequestDto solicitud)
    {
        CuentaBancaria cuenta = ValidacionTesoreriaBancos.DatosCuenta(solicitud);
        IGenericRepository<CuentaBancaria> repositorio = _unitOfWork.Repository<CuentaBancaria>();
        await ValidacionTesoreriaBancos.ValidarUnicidadAsync(repositorio, cuenta);

        cuenta.FechaRegistro = FechasUtc.Normalizar(DateTime.UtcNow);
        await repositorio.AddAsync(cuenta);
        await ValidacionTesoreriaBancos.GuardarAsync(_unitOfWork);
        return await ConsultasTesoreriaBancos.DetalleCuentaAsync(_unitOfWork, cuenta.Id);
    }
}
