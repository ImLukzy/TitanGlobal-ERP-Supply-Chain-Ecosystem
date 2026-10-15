using System.Transactions;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo15_OrdenesAbastecimiento.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo15_OrdenesAbastecimiento.UseCases;

public static class PersistenciaOrdenAbastecimiento
{
    public static async Task<OrdenAbastecimientoCreadaDto> CrearAsync(
        OrdenAbastecimiento orden,
        IUnitOfWork unitOfWork)
    {
        using (TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await unitOfWork.Repository<OrdenAbastecimiento>().AddAsync(orden);
            await unitOfWork.CompleteAsync();
            orden.Numero = $"OA-{orden.Id:D6}";
            unitOfWork.Repository<OrdenAbastecimiento>().Update(orden);
            await unitOfWork.CompleteAsync();
            transaccion.Complete();
        }

        return new OrdenAbastecimientoCreadaDto
        {
            Id = orden.Id,
            Numero = orden.Numero
        };
    }
}
