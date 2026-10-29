using System;
using System.Collections.Generic;

namespace Edelstahl.Domain.Security
{
    /// <summary>
    /// Nombres de los roles del sistema.
    /// </summary>
    public static class NombresRol
    {
        public const string Administrador = "Administrador";
        public const string Vendedor = "Vendedor";
        public const string Produccion = "Responsable de Producción";
        public const string Deposito = "Responsable de Depósito";
        public const string Compras = "Responsable de Compras";
        public const string Logistica = "Responsable de Logística";
        public const string Supervisor = "Supervisor";
        public const string Demostracion = "DEMO";

        public static readonly string[] Todos =
        {
            Administrador, Vendedor, Produccion, Deposito, Compras, Logistica, Supervisor
        };
    }

    /// <summary>
    /// Acciones que se pueden autorizar. Cada rol recibe un subconjunto.
    /// </summary>
    public enum Permiso
    {
        ClientesVer,
        ClientesEditar,
        PresupuestosGestionar,
        PedidosVer,
        PedidosConfirmar,
        FacturasEmitir,
        CobrosRegistrar,
        CuentaCorrienteVer,
        CatalogoVer,
        CatalogoEditar,
        InventarioVer,
        InventarioMover,
        ProduccionVer,
        ProduccionGestionar,
        CalidadRegistrar,
        ComprasVer,
        ComprasGestionar,
        ProveedoresGestionar,
        PagosRegistrar,
        LogisticaVer,
        LogisticaGestionar,
        DevolucionesGestionar,
        ReportesVer,
        ReportesGerenciales,
        UsuariosAdministrar,
        RolesAdministrar,
        BitacoraVer,
        RespaldoAdministrar
    }

    /// <summary>
    /// Matriz de permisos por rol (control de acceso basado en roles).
    /// </summary>
    public static class MatrizPermisos
    {
        private static readonly Dictionary<string, HashSet<Permiso>> PorRol = Construir();

        public static bool Tiene(string nombreRol, Permiso permiso)
        {
            if (string.IsNullOrWhiteSpace(nombreRol))
            {
                return false;
            }

            HashSet<Permiso> permisos;

            return PorRol.TryGetValue(nombreRol.Trim(), out permisos)
                && permisos.Contains(permiso);
        }

        public static IEnumerable<Permiso> De(string nombreRol)
        {
            HashSet<Permiso> permisos;

            if (nombreRol != null && PorRol.TryGetValue(nombreRol.Trim(), out permisos))
            {
                return permisos;
            }

            return new Permiso[0];
        }

        private static Dictionary<string, HashSet<Permiso>> Construir()
        {
            Dictionary<string, HashSet<Permiso>> mapa =
                new Dictionary<string, HashSet<Permiso>>(StringComparer.OrdinalIgnoreCase);

            HashSet<Permiso> todos = new HashSet<Permiso>((Permiso[])Enum.GetValues(typeof(Permiso)));

            mapa[NombresRol.Administrador] = new HashSet<Permiso>(todos);
            mapa[NombresRol.Demostracion] = new HashSet<Permiso>(todos);

            mapa[NombresRol.Vendedor] = new HashSet<Permiso>
            {
                Permiso.ClientesVer, Permiso.ClientesEditar, Permiso.PresupuestosGestionar,
                Permiso.PedidosVer, Permiso.PedidosConfirmar, Permiso.FacturasEmitir,
                Permiso.CuentaCorrienteVer, Permiso.CatalogoVer, Permiso.InventarioVer,
                Permiso.ProduccionVer, Permiso.LogisticaVer, Permiso.ReportesVer
            };

            mapa[NombresRol.Produccion] = new HashSet<Permiso>
            {
                Permiso.PedidosVer, Permiso.CatalogoVer, Permiso.InventarioVer,
                Permiso.ProduccionVer, Permiso.ProduccionGestionar, Permiso.CalidadRegistrar,
                Permiso.ComprasVer, Permiso.ReportesVer
            };

            mapa[NombresRol.Deposito] = new HashSet<Permiso>
            {
                Permiso.PedidosVer, Permiso.CatalogoVer, Permiso.InventarioVer,
                Permiso.InventarioMover, Permiso.ProduccionVer, Permiso.ComprasVer,
                Permiso.LogisticaVer, Permiso.ReportesVer
            };

            mapa[NombresRol.Compras] = new HashSet<Permiso>
            {
                Permiso.CatalogoVer, Permiso.InventarioVer, Permiso.ProduccionVer,
                Permiso.ComprasVer, Permiso.ComprasGestionar, Permiso.ProveedoresGestionar,
                Permiso.ReportesVer
            };

            mapa[NombresRol.Logistica] = new HashSet<Permiso>
            {
                Permiso.ClientesVer, Permiso.PedidosVer, Permiso.InventarioVer,
                Permiso.LogisticaVer, Permiso.LogisticaGestionar, Permiso.DevolucionesGestionar,
                Permiso.ReportesVer
            };

            // El supervisor mira todo (consultas, reportes gerenciales y bitacora) pero no modifica nada.
            mapa[NombresRol.Supervisor] = new HashSet<Permiso>
            {
                Permiso.ClientesVer, Permiso.PedidosVer, Permiso.CuentaCorrienteVer, Permiso.CatalogoVer,
                Permiso.InventarioVer, Permiso.ProduccionVer, Permiso.ComprasVer, Permiso.LogisticaVer,
                Permiso.ReportesVer, Permiso.ReportesGerenciales, Permiso.BitacoraVer
            };

            return mapa;
        }
    }
}
