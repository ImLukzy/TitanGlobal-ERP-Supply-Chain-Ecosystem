namespace TitanGlobal.Api.Modules.Modulo71_Usuarios.Dtos;

public class PaginaUsuariosDto
{
    public List<UsuarioListadoDto> Items { get; set; } = new List<UsuarioListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class UsuarioListadoDto
{
    public int Id { get; set; }

    public string NombreUsuario { get; set; } = string.Empty;

    public string NombreCompleto { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool Activo { get; set; }

    public List<string> Roles { get; set; } = new List<string>();
}

public class UsuarioDetalleDto : UsuarioListadoDto
{
    public DateTime FechaAlta { get; set; }

    public List<int> RolIds { get; set; } = new List<int>();

    public int ModulosConLectura { get; set; }

    public int ModulosConEscritura { get; set; }
}

public class PermisoEfectivoDto
{
    public int ModuloNumero { get; set; }

    public string ModuloNombre { get; set; } = string.Empty;

    public string Dominio { get; set; } = string.Empty;

    public string Permiso { get; set; } = "Ninguno";

    public List<string> OtorgadoPor { get; set; } = new List<string>();
}

public class ResumenUsuariosDto
{
    public int Total { get; set; }

    public int Activos { get; set; }

    public int Inactivos { get; set; }

    public int SinRol { get; set; }

    public List<RolUsuariosDto> PorRol { get; set; } = new List<RolUsuariosDto>();
}

public class RolUsuariosDto
{
    public int RolId { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public int Usuarios { get; set; }
}

public class UsuarioIdDto
{
    public int Id { get; set; }
}
