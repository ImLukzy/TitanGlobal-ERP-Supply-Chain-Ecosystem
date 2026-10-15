namespace TitanGlobal.Api.Modules.Modulo72_RolesPermisos.Dtos;

public class PaginaRolesDto
{
    public List<RolListadoDto> Items { get; set; } = new List<RolListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class RolListadoDto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public bool EsSistema { get; set; }

    public int ModulosConLectura { get; set; }

    public int ModulosConEscritura { get; set; }
}

public class RolDetalleDto : RolListadoDto
{
    public string? Descripcion { get; set; }
}

public class PermisoModuloDto
{
    public int ModuloNumero { get; set; }

    public string ModuloNombre { get; set; } = string.Empty;

    public string Dominio { get; set; } = string.Empty;

    public bool PuedeLeer { get; set; }

    public bool PuedeEscribir { get; set; }
}

public class MatrizPermisosDto
{
    public List<RolResumenDto> Roles { get; set; } = new List<RolResumenDto>();

    public List<ModuloMatrizDto> Modulos { get; set; } = new List<ModuloMatrizDto>();
}

public class RolResumenDto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;
}

public class ModuloMatrizDto
{
    public int ModuloNumero { get; set; }

    public string ModuloNombre { get; set; } = string.Empty;

    public string Dominio { get; set; } = string.Empty;

    public List<string> Permisos { get; set; } = new List<string>();
}

public class RolIdDto
{
    public int Id { get; set; }
}
