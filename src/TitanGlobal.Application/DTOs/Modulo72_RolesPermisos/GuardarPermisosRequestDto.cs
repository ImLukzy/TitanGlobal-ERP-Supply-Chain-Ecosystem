namespace TitanGlobal.Application.DTOs.Modulo72_RolesPermisos;

public class GuardarPermisosRequestDto
{
    public List<PermisoSolicitadoDto> Permisos { get; set; } = new List<PermisoSolicitadoDto>();
}

public class PermisoSolicitadoDto
{
    public int ModuloNumero { get; set; }

    public bool PuedeLeer { get; set; }

    public bool PuedeEscribir { get; set; }
}
