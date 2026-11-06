namespace TitanGlobal.Domain.Models;

// Valores con los que nace cada entidad al crearla en código. El scaffold no los genera, por eso viven
// aquí y no en los archivos generados. Una entidad nueva que necesite valores iniciales se añade aquí.

public partial class Almacen
{
    public Almacen()
    {
        Activo = true;
    }
}

public partial class CategoriaProducto
{
    public CategoriaProducto()
    {
        Activa = true;
    }
}

public partial class CentroTrabajo
{
    public CentroTrabajo()
    {
        Activo = true;
    }
}

public partial class Conductor
{
    public Conductor()
    {
        Habilitado = true;
        Activo = true;
    }
}

public partial class ContratoComercial
{
    public ContratoComercial()
    {
        Estado = "Borrador";
    }
}

public partial class Cotizacion
{
    public Cotizacion()
    {
        Estado = "Emitida";
    }
}

public partial class DevolucionComercial
{
    public DevolucionComercial()
    {
        Estado = "Solicitada";
    }
}

public partial class Impuesto
{
    public Impuesto()
    {
        Pais = "PER";
        Activo = true;
        FechaCreacion = DateTime.UtcNow;
    }
}

public partial class ListaPrecios
{
    public ListaPrecios()
    {
        Activa = true;
    }
}

public partial class OrdenAbastecimiento
{
    public OrdenAbastecimiento()
    {
        Estado = "Emitida";
    }
}

public partial class Promocion
{
    public Promocion()
    {
        Activa = true;
    }
}

public partial class Proveedor
{
    public Proveedor()
    {
        Activo = true;
    }
}

public partial class ReclamoPostventa
{
    public ReclamoPostventa()
    {
        Estado = "Abierto";
    }
}

public partial class SegmentoCliente
{
    public SegmentoCliente()
    {
        Activo = true;
    }
}

public partial class SolicitudCompra
{
    public SolicitudCompra()
    {
        Estado = "Pendiente";
    }
}

public partial class TasaImpuesto
{
    public TasaImpuesto()
    {
        Activo = true;
    }
}

public partial class VehiculoFlota
{
    public VehiculoFlota()
    {
        Activo = true;
    }
}

public partial class ZonaAlmacenaje
{
    public ZonaAlmacenaje()
    {
        Activa = true;
    }
}
