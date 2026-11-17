using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.Services.DomainModel.Security;
using Edelstahl.Services.DataAccess.Implementations;
using Edelstahl.Services.DomainModel.Composite;

namespace Edelstahl.Services.Logic
{
    /// <summary>
    /// Los roles del sistema como Familias y sus permisos como Patentes.
    /// Define el árbol de cada rol, lo guarda en la base y comprueba que coincida con la
    /// matriz de permisos que usa hoy el control de acceso.
    /// </summary>
    internal static class RolesPermisosLogic
    {
        /// <summary>
        /// Árbol de cada rol: una familia por rol, hecha de familias reutilizables y permisos sueltos.
        /// Los permisos de cada rol son los de MatrizPermisos.
        /// </summary>
        public static Dictionary<string, Familia> DefinirRoles()
        {
            Dictionary<string, Familia> roles = new Dictionary<string, Familia>(StringComparer.OrdinalIgnoreCase);

            // ---- Familias reutilizables ----
            Familia clientes = new Familia("Clientes")
                .Agregar(Permiso.ClientesVer, Permiso.ClientesEditar);

            Familia ventas = new Familia("Ventas").Agregar(
                Permiso.PresupuestosGestionar, Permiso.PedidosVer, Permiso.PedidosConfirmar,
                Permiso.FacturasEmitir, Permiso.CuentaCorrienteVer);

            Familia consultaBasica = new Familia("Consulta de catálogo e inventario")
                .Agregar(Permiso.CatalogoVer, Permiso.InventarioVer);

            Familia produccion = new Familia("Producción").Agregar(
                Permiso.ProduccionVer, Permiso.ProduccionGestionar, Permiso.CalidadRegistrar);

            Familia compras = new Familia("Compras").Agregar(
                Permiso.ComprasVer, Permiso.ComprasGestionar, Permiso.ProveedoresGestionar);

            Familia logistica = new Familia("Logística").Agregar(
                Permiso.LogisticaVer, Permiso.LogisticaGestionar, Permiso.DevolucionesGestionar);

            Familia movimientosDeposito = new Familia("Movimientos de depósito")
                .Agregar(Permiso.InventarioMover);

            Familia reportes = new Familia("Reportes").Agregar(Permiso.ReportesVer);

            Familia gerencial = new Familia("Control gerencial")
                .Agregar(Permiso.ReportesGerenciales, Permiso.BitacoraVer);

            Familia soloLectura = new Familia("Solo lectura").Agregar(
                Permiso.ClientesVer, Permiso.PedidosVer, Permiso.CuentaCorrienteVer,
                Permiso.CatalogoVer, Permiso.InventarioVer, Permiso.ProduccionVer,
                Permiso.ComprasVer, Permiso.LogisticaVer, Permiso.ReportesVer);

            Familia todos = new Familia("Acceso total")
                .Agregar((Permiso[])Enum.GetValues(typeof(Permiso)));

            // ---- Un rol es una familia hecha de otras familias y de permisos sueltos ----
            roles[NombresRol.Administrador] = new Familia(NombresRol.Administrador).Agregar(todos);
            roles[NombresRol.Demostracion] = new Familia(NombresRol.Demostracion).Agregar(todos);

            roles[NombresRol.Vendedor] = new Familia(NombresRol.Vendedor)
                .Agregar(clientes).Agregar(ventas).Agregar(consultaBasica).Agregar(reportes)
                .Agregar(Permiso.ProduccionVer, Permiso.LogisticaVer);

            roles[NombresRol.Produccion] = new Familia(NombresRol.Produccion)
                .Agregar(produccion).Agregar(consultaBasica).Agregar(reportes)
                .Agregar(Permiso.PedidosVer, Permiso.ComprasVer);

            roles[NombresRol.Deposito] = new Familia(NombresRol.Deposito)
                .Agregar(movimientosDeposito).Agregar(consultaBasica).Agregar(reportes)
                .Agregar(Permiso.PedidosVer, Permiso.ProduccionVer, Permiso.ComprasVer, Permiso.LogisticaVer);

            roles[NombresRol.Compras] = new Familia(NombresRol.Compras)
                .Agregar(compras).Agregar(consultaBasica).Agregar(reportes)
                .Agregar(Permiso.ProduccionVer);

            roles[NombresRol.Logistica] = new Familia(NombresRol.Logistica)
                .Agregar(logistica).Agregar(reportes)
                .Agregar(Permiso.ClientesVer, Permiso.PedidosVer, Permiso.InventarioVer);

            // El supervisor mira todo (consultas, reportes gerenciales y bitacora) pero no modifica nada.
            roles[NombresRol.Supervisor] = new Familia(NombresRol.Supervisor)
                .Agregar(soloLectura).Agregar(gerencial);

            return roles;
        }

        /// <summary>
        /// Guarda en la base una patente por cada permiso, una familia por cada rol y por cada grupo,
        /// y las relaciones entre ellos. Se puede repetir: lo que ya existe no se duplica.
        /// </summary>
        public static ResultadoSiembra Sembrar()
        {
            ResultadoSiembra resultado = new ResultadoSiembra();

            foreach (Permiso permiso in (Permiso[])Enum.GetValues(typeof(Permiso)))
            {
                ObtenerOCrearPatente(new Patente(permiso), resultado);
            }

            foreach (Familia rol in DefinirRoles().Values)
            {
                Persistir(rol, resultado);
            }

            return resultado;
        }

        /// <summary>Asigna a cada usuario existente la familia que lleva el nombre de su rol.</summary>
        public static int AsignarRolesAUsuarios()
        {
            UsuarioFamiliaRepository usuarios = new UsuarioFamiliaRepository();
            int asignados = 0;

            foreach (KeyValuePair<Guid, string> usuario in usuarios.RolesDeLosUsuarios())
            {
                Familia familia = FamiliaLogic.GetByNombre(usuario.Value);

                if (familia == null)
                {
                    continue;
                }

                UsuarioPrivilegios privilegios = new UsuarioPrivilegios { IdUsuario = usuario.Key };

                if (!usuarios.Existe(familia, privilegios))
                {
                    usuarios.Add(familia, privilegios);
                    asignados++;
                }
            }

            return asignados;
        }

        /// <summary>
        /// Compara, rol por rol, los permisos guardados en el Composite con MatrizPermisos.
        /// Devuelve las diferencias encontradas (lista vacía si coinciden).
        /// </summary>
        public static List<string> Verificar()
        {
            List<string> diferencias = new List<string>();

            List<string> roles = NombresRol.Todos.ToList();
            roles.Add(NombresRol.Demostracion);

            foreach (string rol in roles)
            {
                Familia guardada = FamiliaLogic.GetByNombre(rol);

                if (guardada == null)
                {
                    diferencias.Add("Role '" + rol + "' is not saved as a family.");
                    continue;
                }

                HashSet<Permiso> delArbol = new HashSet<Permiso>(guardada.Permisos());
                HashSet<Permiso> deLaMatriz = new HashSet<Permiso>(MatrizPermisos.De(rol));

                foreach (Permiso falta in deLaMatriz.Except(delArbol))
                {
                    diferencias.Add("Role '" + rol + "': permission " + falta + " is missing from the tree.");
                }

                foreach (Permiso sobra in delArbol.Except(deLaMatriz))
                {
                    diferencias.Add("Role '" + rol + "': permission " + sobra + " is in the tree but not in the matrix.");
                }
            }

            return diferencias;
        }

        private static Patente ObtenerOCrearPatente(Patente modelo, ResultadoSiembra resultado)
        {
            Patente existente = PatenteLogic.GetByDataKey(modelo.DataKey);

            if (existente != null)
            {
                return existente;
            }

            PatenteLogic.Add(modelo);
            resultado.PatentesCreadas++;
            return modelo;
        }

        private static Familia Persistir(Familia modelo, ResultadoSiembra resultado)
        {
            Familia guardada = FamiliaLogic.GetByNombre(modelo.Nombre);

            if (guardada == null)
            {
                guardada = new Familia(modelo.Nombre);
                FamiliaLogic.Add(guardada);
                resultado.FamiliasCreadas++;
            }

            foreach (Component hijo in modelo.GetHijos())
            {
                Patente patente = hijo as Patente;

                if (patente != null)
                {
                    Patente guardadaPatente = ObtenerOCrearPatente(new Patente { DataKey = patente.DataKey, Nombre = patente.Nombre, TipoAcceso = patente.TipoAcceso }, resultado);
                    FamiliaLogic.AgregarPatente(guardadaPatente, guardada);
                }
                else
                {
                    Familia guardadaHija = Persistir((Familia)hijo, resultado);
                    FamiliaLogic.AgregarFamilia(guardadaHija, guardada);
                }
            }

            return guardada;
        }
    }

    /// <summary>Cuántos elementos se crearon al sembrar.</summary>
    public sealed class ResultadoSiembra
    {
        public int PatentesCreadas { get; set; }

        public int FamiliasCreadas { get; set; }

        public override string ToString()
        {
            return PatentesCreadas + " patente(s) y " + FamiliasCreadas + " familia(s) creadas.";
        }
    }
}
