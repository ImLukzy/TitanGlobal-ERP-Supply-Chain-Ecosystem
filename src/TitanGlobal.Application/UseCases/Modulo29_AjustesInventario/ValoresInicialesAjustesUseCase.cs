using TitanGlobal.Application.DTOs.Modulo29_AjustesInventario;

namespace TitanGlobal.Application.UseCases.Modulo29_AjustesInventario;

public class ValoresInicialesAjustesUseCase
{
    public ValoresInicialesAjustesDto Execute()
    {
        return new ValoresInicialesAjustesDto
        {
            Tipos = ValidacionAjustesInventario.TiposValidos.ToList(),
            Motivos = ValidacionAjustesInventario.MotivosValidos.ToList()
        };
    }
}
