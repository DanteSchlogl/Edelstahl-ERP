using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.Domain.Catalogo;
using Edelstahl.Domain.Comercial;
using Edelstahl.Domain.Compras;
using Edelstahl.Domain.Logistica;
using Edelstahl.Domain.Security;

namespace Edelstahl.WinApp.Ui
{
    // Filas planas que se muestran en las grillas. Evitan exponer las entidades
    // completas (con colecciones y datos internos) a la interfaz y a la exportacion.

    public class ClienteFila
    {
        public Guid Id { get; set; }
        public string Tipo { get; set; }
        public string CUIT { get; set; }
        public string RazonSocial { get; set; }
        public string Localidad { get; set; }
        public string Provincia { get; set; }
        public string Email { get; set; }
        public string Telefono { get; set; }
        public decimal LimiteCredito { get; set; }
        public decimal DeudaActual { get; set; }
        public bool Activo { get; set; }
        public bool Autorizado { get; set; }

        public static ClienteFila De(Cliente c)
        {
            return new ClienteFila
            {
                Id = c.Id, Tipo = c.Tipo == TipoPersona.Particular ? Tr.T("Individual") : Tr.T("Company"), CUIT = c.CUIT, RazonSocial = c.RazonSocial, Localidad = c.Localidad,
                Provincia = c.Provincia, Email = c.Email, Telefono = c.Telefono,
                LimiteCredito = c.LimiteCredito, DeudaActual = c.DeudaActual, Activo = c.Activo, Autorizado = c.AutorizadoACredito
            };
        }
    }

    public class ProveedorFila
    {
        public Guid Id { get; set; }
        public string Cuit { get; set; }
        public string RazonSocial { get; set; }
        public string Localidad { get; set; }
        public string Email { get; set; }
        public string Telefono { get; set; }
        public int PlazoEntregaDias { get; set; }
        public decimal SaldoCuenta { get; set; }
        public bool Activo { get; set; }

        public static ProveedorFila De(Proveedor p)
        {
            return new ProveedorFila
            {
                Id = p.Id, Cuit = p.Cuit, RazonSocial = p.RazonSocial, Localidad = p.Localidad,
                Email = p.Email, Telefono = p.Telefono, PlazoEntregaDias = p.PlazoEntregaDias,
                SaldoCuenta = p.SaldoCuenta, Activo = p.Activo
            };
        }
    }

    public class VehiculoFila
    {
        public Guid Id { get; set; }
        public string Patente { get; set; }
        public string Marca { get; set; }
        public string Modelo { get; set; }
        public int Anio { get; set; }
        public decimal CapacidadKg { get; set; }
        public DateTime? VencimientoSeguro { get; set; }
        public bool Activo { get; set; }

        public static VehiculoFila De(Vehiculo v)
        {
            return new VehiculoFila
            {
                Id = v.Id, Patente = v.Patente, Marca = v.Marca, Modelo = v.Modelo, Anio = v.Anio,
                CapacidadKg = v.CapacidadKg, VencimientoSeguro = v.VencimientoSeguro, Activo = v.Activo
            };
        }
    }

    public class ChoferFila
    {
        public Guid Id { get; set; }
        public string Documento { get; set; }
        public string Nombre { get; set; }
        public string Licencia { get; set; }
        public string Categoria { get; set; }
        public DateTime VencimientoLicencia { get; set; }
        public string Telefono { get; set; }
        public bool Activo { get; set; }

        public static ChoferFila De(Chofer c)
        {
            return new ChoferFila
            {
                Id = c.Id, Documento = c.Documento, Nombre = c.NombreCompleto, Licencia = c.LicenciaConducir,
                Categoria = c.CategoriaLicencia, VencimientoLicencia = c.VencimientoLicencia,
                Telefono = c.Telefono, Activo = c.Activo
            };
        }
    }

    public class UsuarioFila
    {
        public Guid Id { get; set; }
        public string Usuario { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Rol { get; set; }
        public bool Activo { get; set; }
        public bool Bloqueado { get; set; }
        public int IntentosFallidos { get; set; }
        public DateTime? UltimoAcceso { get; set; }

        public static UsuarioFila De(Usuario u, Dictionary<Guid, string> roles)
        {
            string rol;
            roles.TryGetValue(u.RolId, out rol);

            return new UsuarioFila
            {
                Id = u.Id, Usuario = u.NombreUsuario, Nombre = u.NombreCompleto, Email = u.Email,
                Rol = rol ?? string.Empty, Activo = u.Activo,
                Bloqueado = u.BloqueadoHasta.HasValue && u.BloqueadoHasta.Value > DateTime.Now,
                IntentosFallidos = u.IntentosFallidos, UltimoAcceso = u.UltimoAcceso
            };
        }
    }

    public class RolFila
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public bool Activo { get; set; }
        public int Permisos { get; set; }
    }

    public class PermisoFila
    {
        public string Permiso { get; set; }
    }

    public class BitacoraFila
    {
        public Guid Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; }
        public string Modulo { get; set; }
        public string Accion { get; set; }
        public string Entidad { get; set; }
        public string Descripcion { get; set; }
        public string Resultado { get; set; }
        public string TipoEvento { get; set; }
        public string Equipo { get; set; }

        public static BitacoraFila De(RegistroBitacora r)
        {
            return new BitacoraFila
            {
                Id = r.Id, Fecha = r.FechaHora, Usuario = r.NombreUsuario, Modulo = r.Modulo,
                Accion = r.Accion, Entidad = r.Entidad, Descripcion = r.Descripcion,
                Resultado = r.Resultado, TipoEvento = r.TipoEvento, Equipo = r.NombreEquipo
            };
        }
    }

    public class MedioPagoFila
    {
        public Guid Id { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public bool RequiereReferencia { get; set; }
        public bool Activo { get; set; }
    }

    public class CategoriaFila
    {
        public Guid Id { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Ruta { get; set; }
        public bool Activo { get; set; }
    }

    public class DepositoFila
    {
        public Guid Id { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Localidad { get; set; }
        public string Responsable { get; set; }
        public bool Activo { get; set; }
    }

    public class ItemLinea
    {
        public Guid? ProductoId { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Descuento { get; set; }
        public int Tipo { get; set; }
        public decimal Subtotal { get { return Math.Round(Cantidad * Precio * (1m - Descuento / 100m), 2); } }
    }
}
