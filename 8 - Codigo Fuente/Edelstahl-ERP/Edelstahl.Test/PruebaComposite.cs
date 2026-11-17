using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Edelstahl.Services.DomainModel.Security;
using Edelstahl.Services.DataAccess.Tools;
using Edelstahl.Services.DomainModel.Composite;
using Edelstahl.Services.Facade;

namespace Edelstahl.Test
{
    /// <summary>
    /// Demostración del Composite de roles y permisos contra la base de servicios
    /// (Edelstahl.Test.exe composite). Crea patentes y familias de prueba, las anida, se las asigna
    /// a un usuario existente, recupera sus privilegios y, al final, borra lo que creó.
    /// </summary>
    internal static class PruebaComposite
    {
        private static int _fallas;

        public static void Ejecutar()
        {
            _fallas = 0;
            string marca = Guid.NewGuid().ToString("N").Substring(0, 6);

            Console.WriteLine("== Composite: roles (familias) y permisos (patentes) ==");

            // 0. Tablas y roles del sistema como familias
            EsquemaComposite.Instalar();
            RolesPermisosService.Sembrar();
            RolesPermisosService.AsignarRolesAUsuarios();

            List<string> diferencias = RolesPermisosService.Verificar();
            Comprobar("los roles guardados como familias coinciden con la matriz de permisos", diferencias.Count == 0);

            foreach (string diferencia in diferencias)
            {
                Console.WriteLine("    " + diferencia);
            }

            // 1. Patentes (hojas)
            Patente ventas = Nueva("frmVentas_" + marca);
            Patente visualizacion = Nueva("frmVisualizacionVentas_" + marca);
            Patente perfil = Nueva("frmPerfil_" + marca);

            PatenteService.Add(ventas);
            PatenteService.Add(visualizacion);
            PatenteService.Add(perfil);
            Comprobar("las patentes se guardan y se leen por su DataKey",
                PatenteService.GetByDataKey(ventas.DataKey) != null && PatenteService.GetByDataKey(perfil.DataKey) != null);

            // 2. Familias (nodos)
            Familia familiaVentas = new Familia("Familia de ventas " + marca);
            Familia familiaAdmin = new Familia("Administrador " + marca);
            FamiliaService.Add(familiaVentas);
            FamiliaService.Add(familiaAdmin);

            // 3. Patentes dentro de la familia, y la familia de ventas dentro de la de administrador
            FamiliaService.AgregarPatente(ventas, familiaVentas);
            FamiliaService.AgregarPatente(visualizacion, familiaVentas);
            FamiliaService.AgregarFamilia(familiaVentas, familiaAdmin);

            Familia admin = FamiliaService.GetByNombre(familiaAdmin.Nombre);
            Comprobar("la familia anidada se reconstruye con sus patentes",
                admin != null && admin.Patentes().Count == 2 && admin.Familias().Count == 1);

            bool ciclo = false;

            try
            {
                FamiliaService.AgregarFamilia(familiaAdmin, familiaVentas);
            }
            catch (Exception)
            {
                ciclo = true;
            }

            Comprobar("una familia no se puede meter dentro de su propia descendencia (ciclo)", ciclo);

            // 4. Usuario: una familia y una patente suelta
            Guid idUsuario;
            string nombreUsuario;
            UsuarioDePrueba(out idUsuario, out nombreUsuario);

            PrivilegiosService.AgregarFamilia(familiaAdmin, idUsuario);
            PrivilegiosService.AgregarPatente(perfil, idUsuario);

            UsuarioPrivilegios usuario = PrivilegiosService.Obtener(idUsuario, nombreUsuario);

            Console.WriteLine("Patentes accesibles para '" + usuario.NombreUsuario + "' (de las pruebas):");

            foreach (Patente patente in usuario.Patentes.Where(p => p.DataKey.EndsWith(marca)))
            {
                Console.WriteLine("  - " + patente.DataKey + " (" + patente.TipoAcceso + ")");
            }

            Comprobar("el usuario accede a las patentes de la familia anidada y a la patente suelta",
                usuario.Patentes.Count(p => p.DataKey.EndsWith(marca)) == 3);
            Comprobar("el usuario ve las dos familias del árbol",
                usuario.Familias.Count(f => f.Nombre.EndsWith(marca)) == 2);

            // 4b. Herencia con tres niveles y ciclo A -> B -> C -> A
            Familia a = new Familia("Prueba composite A " + marca);
            Familia b = new Familia("Prueba composite B " + marca);
            Familia c = new Familia("Prueba composite C " + marca);
            Patente p1 = Nueva("Patente_1_" + marca);
            Patente p2 = Nueva("Patente_2_" + marca);
            Patente p3 = Nueva("Patente_3_" + marca);

            foreach (Familia f in new[] { a, b, c })
            {
                FamiliaService.Add(f);
            }

            foreach (Patente p in new[] { p1, p2, p3 })
            {
                PatenteService.Add(p);
            }

            FamiliaService.AgregarPatente(p1, a);
            FamiliaService.AgregarPatente(p2, b);
            FamiliaService.AgregarPatente(p3, c);
            FamiliaService.AgregarFamilia(b, a);   // A contiene B
            FamiliaService.AgregarFamilia(c, b);   // B contiene C

            string mensajeCiclo = null;

            try
            {
                FamiliaService.AgregarFamilia(a, c);   // C contiene A: produciría A -> B -> C -> A ...
            }
            catch (Exception ex)
            {
                mensajeCiclo = ex.Message;
            }

            Comprobar("A -> B -> C -> A se rechaza por ciclo: " + mensajeCiclo, mensajeCiclo != null);
            Comprobar("la relación rechazada no quedó guardada",
                FamiliaService.GetByNombre(c.Nombre).Familias().Count == 0);

            PrivilegiosService.AgregarFamilia(a, idUsuario);   // solo se le asigna directamente la familia A
            UsuarioPrivilegios heredero = PrivilegiosService.Obtener(idUsuario, nombreUsuario);
            Comprobar("el usuario hereda las patentes 1, 2 y 3 aunque solo se le asignó la familia A",
                new[] { p1, p2, p3 }.All(p => heredero.Patentes.Any(x => x.DataKey == p.DataKey)));

            PrivilegiosService.AgregarPatente(p1, idUsuario);   // patente directa que además llega por herencia
            heredero = PrivilegiosService.Obtener(idUsuario, nombreUsuario);
            Comprobar("patentes directas + heredadas, sin duplicados",
                heredero.Patentes.Count(x => x.DataKey == p1.DataKey) == 1);

            PrivilegiosService.QuitarPatente(p1, idUsuario);
            PrivilegiosService.QuitarFamilia(a, idUsuario);
            FamiliaService.Delete(a.Id);
            FamiliaService.Delete(b.Id);
            FamiliaService.Delete(c.Id);
            PatenteService.Delete(p1.Id);
            PatenteService.Delete(p2.Id);
            PatenteService.Delete(p3.Id);

            // 5. Arbol de un rol del sistema, leído de la base
            Familia vendedor = FamiliaService.GetByNombre(NombresRol.Vendedor);
            Console.WriteLine("Árbol del rol " + NombresRol.Vendedor + ":");
            Console.Write(vendedor == null ? "  (no está guardado)" + Environment.NewLine : vendedor.Arbol());

            // 6. Limpieza de lo creado por la prueba
            PrivilegiosService.QuitarFamilia(familiaAdmin, idUsuario);
            PrivilegiosService.QuitarPatente(perfil, idUsuario);
            FamiliaService.Delete(familiaAdmin.Id);
            FamiliaService.Delete(familiaVentas.Id);
            PatenteService.Delete(ventas.Id);
            PatenteService.Delete(visualizacion.Id);
            PatenteService.Delete(perfil.Id);

            Comprobar("la prueba no deja datos suyos en la base",
                FamiliaService.GetByNombre(familiaAdmin.Nombre) == null && PatenteService.GetByDataKey(ventas.DataKey) == null);

            Console.WriteLine(_fallas == 0
                ? "Composite: todas las comprobaciones correctas."
                : "Composite: " + _fallas + " comprobación(es) con falla.");

            if (_fallas > 0)
            {
                throw new InvalidOperationException("La prueba del Composite tuvo fallas.");
            }
        }

        private static Patente Nueva(string dataKey)
        {
            return new Patente { DataKey = dataKey, Nombre = dataKey, TipoAcceso = TipoAcceso.Pantalla };
        }

        private static void UsuarioDePrueba(out Guid id, out string nombre)
        {
            using (SqlConnection conexion = new SqlConnection(SqlServerConnection.CadenaServicios))
            using (SqlCommand comando = new SqlCommand("SELECT TOP 1 Id, NombreUsuario FROM dbo.Usuarios ORDER BY NombreUsuario", conexion))
            {
                conexion.Open();

                using (SqlDataReader lector = comando.ExecuteReader())
                {
                    if (!lector.Read())
                    {
                        throw new InvalidOperationException("No hay usuarios en la base de servicios para probar.");
                    }

                    id = lector.GetGuid(0);
                    nombre = lector.GetString(1);
                }
            }
        }

        private static void Comprobar(string descripcion, bool correcto)
        {
            Console.WriteLine((correcto ? "  OK    " : "  FALLA ") + descripcion);

            if (!correcto)
            {
                _fallas++;
            }
        }
    }
}
