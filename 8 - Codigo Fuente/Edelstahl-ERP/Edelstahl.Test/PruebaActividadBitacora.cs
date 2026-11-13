using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Services;
using Edelstahl.DAL.Factory;
using Edelstahl.Domain.Catalogo;
using Edelstahl.Domain.Comercial;
using Edelstahl.Domain.Compras;
using Edelstahl.Domain.Finanzas;
using Edelstahl.Domain.Logistica;
using Edelstahl.Domain.Security;
using Edelstahl.Services.Configuration;

namespace Edelstahl.Test
{
    /// <summary>
    /// Usa el programa como lo haría una persona (altas de usuarios, ingresos con clave correcta e incorrecta,
    /// cambios de rol, compras, presupuestos, pedidos, catálogo, stock, logística...) contra SQL Server, varias
    /// veces, para que la bitácora quede con actividad de todo tipo. Usa clientes ya cargados y seis empleados (Gastón, Carlos, Mariano, Juan, Nicolás y María).
    /// Uso: Edelstahl.Test.exe bitacora [repeticiones]
    /// </summary>
    public static class PruebaActividadBitacora
    {
        private const string ClaveTemporal = "Prueba-2026-ok";
        private const string ClaveNueva = "Prueba-2026-nueva";

        private static readonly Dictionary<string, Usuario> PorRol = new Dictionary<string, Usuario>();
        private static int _hechos;
        private static int _errores;

        /// <summary>Si hay un usuario real para el rol, las pruebas lo usan en lugar de uno inventado.</summary>
        public static Usuario UsuarioReal(string rol)
        {
            Usuario u;
            return PorRol.TryGetValue(rol, out u) ? u : null;
        }

        private static void Paso(string descripcion, Action accion)
        {
            try
            {
                accion();
                _hechos++;
                Console.WriteLine("  hecho   " + descripcion);
            }
            catch (Exception ex)
            {
                _errores++;
                Console.WriteLine("  AVISO   " + descripcion + " -> " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        /// <summary>Hace algo que el sistema debe rechazar (queda en la bitácora como intento fallido o acceso denegado).</summary>
        private static void Rechazado(string descripcion, Action accion)
        {
            try
            {
                accion();
                _errores++;
                Console.WriteLine("  AVISO   " + descripcion + " -> no fue rechazado");
            }
            catch (Exception ex)
            {
                _hechos++;
                Console.WriteLine("  hecho   " + descripcion + " (rechazado: " + ex.Message + ")");
            }
        }

        private static Usuario AdministradorReal()
        {
            Usuario admin = new AdministracionService().ObtenerUsuarios()
                .FirstOrDefault(u => u.Activo && u.Rol != null && u.Rol.Nombre == NombresRol.Administrador);

            if (admin == null)
            {
                throw new InvalidOperationException("No hay un administrador activo en la base.");
            }

            return admin;
        }

        private static readonly string[,] Personas =
        {
            { "gaston", "Gastón", NombresRol.Vendedor },
            { "carlos.deposito", "Carlos (depósito)", NombresRol.Deposito },
            { "mariano", "Mariano", NombresRol.Compras },
            { "juan", "Juan", NombresRol.Produccion },
            { "nicolas", "Nicolás", NombresRol.Logistica },
            { "maria", "María", NombresRol.Supervisor }
        };

        private static string NombreDe(string rol)
        {
            for (int i = 0; i < Personas.GetLength(0); i++)
            {
                if (Personas[i, 2] == rol)
                {
                    return Personas[i, 0];
                }
            }

            return null;
        }

        /// <summary>Ingresa con la clave correcta (la temporal o la que se eligió después) y deja la sesión iniciada.</summary>
        private static Usuario Entrar(string nombre)
        {
            Usuario u;

            try
            {
                u = new UsuarioService().Autenticar(nombre, ClaveNueva);
            }
            catch (Exception)
            {
                u = new UsuarioService().Autenticar(nombre, ClaveTemporal);
            }

            SesionActual.Iniciar(u);
            return u;
        }

        public static int Ejecutar(int repeticiones)
        {
            Verificador.Seccion("Actividad de prueba para la bitácora (" + repeticiones + " repeticiones)");

            ApplicationMode.Reset();
            ApplicationMode.Select(ExecutionMode.SqlServer);
            FactoryDataAccess.Inicializar(ExecutionMode.SqlServer);
            FactoryDataAccess.PrepararBaseDeDatos();

            SesionActual.Iniciar(new Usuario { NombreUsuario = "inicio", NombreCompleto = "Inicio", Rol = new Rol { Nombre = NombresRol.Administrador } });
            Usuario admin = AdministradorReal();
            SesionActual.Iniciar(admin);
            Console.WriteLine("  administrador de la base: " + admin.NombreUsuario);

            PorRol[NombresRol.Administrador] = admin;

            Usuarios(admin);
            Permisos();
            Empleados();

            for (int vuelta = 1; vuelta <= repeticiones; vuelta++)
            {
                Console.WriteLine("---- vuelta " + vuelta + " de " + repeticiones);
                Ventas(vuelta);
                Compras(vuelta);
                Catalogo(vuelta);
                Stock(vuelta);
                Logistica(vuelta);
                Seguridad(vuelta);
                PermisosIndividuales(vuelta);
            }

            SesionActual.Iniciar(admin);
            Paso("Respaldo manual de las bases", () => new RespaldoService().CrearRespaldoManual());
            Paso("Verificación de integridad", () =>
            {
                if (!new IntegridadService().Verificar().Correcto)
                {
                    throw new InvalidOperationException("La integridad no está correcta después de la actividad.");
                }
            });

            Console.WriteLine();
            Console.WriteLine("Actividad: " + _hechos + " acciones registradas, " + _errores + " avisos.");
            Verificador.Verificar(_errores == 0, "La actividad se completó sin avisos (" + _errores + ")");
            return _errores;
        }

        // ---------------------------------------------------------------
        // Usuarios y roles
        // ---------------------------------------------------------------

        private static void Usuarios(Usuario admin)
        {
            Verificador.Seccion("Usuarios, ingresos y roles");
            AdministracionService administracion = new AdministracionService();
            UsuarioService usuarios = new UsuarioService();

            for (int i = 0; i < Personas.GetLength(0); i++)
            {
                string nombre = Personas[i, 0];
                string completo = Personas[i, 1];
                string rol = Personas[i, 2];

                if (administracion.ObtenerUsuarios().Any(u => u.NombreUsuario == nombre))
                {
                    Console.WriteLine("  (ya existe " + nombre + ")");
                    continue;
                }

                Paso("Alta de " + nombre + " con el rol " + rol,
                    () => administracion.CrearUsuario(nombre, completo, nombre + "@example.test", ClaveTemporal, rol));
            }

            Rechazado("Alta de un usuario con clave corta", () => administracion.CrearUsuario("prueba.corta", "Corta", "c@example.test", "123", NombresRol.Vendedor));
            Rechazado("Alta de un usuario repetido", () => administracion.CrearUsuario("gaston", "Repetido", "r@example.test", ClaveTemporal, NombresRol.Vendedor));

            for (int i = 0; i < Personas.GetLength(0); i++)
            {
                string nombre = Personas[i, 0];
                string rol = Personas[i, 2];

                Paso("Primer ingreso de " + nombre + " y cambio de la clave temporal", () =>
                {
                    Usuario u;

                    try
                    {
                        u = usuarios.Autenticar(nombre, ClaveNueva);
                        PorRol[rol] = u;
                        return;
                    }
                    catch (Exception)
                    {
                    }

                    u = usuarios.Autenticar(nombre, ClaveTemporal);
                    usuarios.CambiarPassword(u.Id, ClaveTemporal, ClaveNueva);
                    PorRol[rol] = usuarios.Autenticar(nombre, ClaveNueva);
                });
            }

            Rechazado("Ingreso sin completar la clave", () => usuarios.Autenticar("gaston", string.Empty));
            Rechazado("Ingreso con un usuario que no existe", () => usuarios.Autenticar("usuario.fantasma", "cualquiera-123"));
            Rechazado("Ingreso con clave incorrecta (1)", () => usuarios.Autenticar("gaston", "clave-equivocada-1"));
            Rechazado("Ingreso con clave incorrecta (2)", () => usuarios.Autenticar("gaston", "clave-equivocada-2"));
            Paso("Ingreso correcto de gaston después de los intentos fallidos", () => usuarios.Autenticar("gaston", ClaveNueva));

            for (int i = 1; i <= 5; i++)
            {
                int n = i;
                Rechazado("Clave incorrecta " + n + " de carlos.deposito", () => usuarios.Autenticar("carlos.deposito", "mal-" + n + "-xxxxxx"));
            }

            Rechazado("Ingreso de carlos.deposito estando bloqueado", () => usuarios.Autenticar("carlos.deposito", ClaveNueva));

            Usuario deposito = administracion.ObtenerUsuarios().First(u => u.NombreUsuario == "carlos.deposito");
            SesionActual.Iniciar(admin);
            Paso("El administrador desbloquea a carlos.deposito", () => administracion.Desbloquear(deposito.Id));
            Paso("Ingreso correcto de carlos.deposito", () => usuarios.Autenticar("carlos.deposito", ClaveNueva));

            Usuario compras = administracion.ObtenerUsuarios().First(u => u.NombreUsuario == "mariano");
            Usuario produccion = administracion.ObtenerUsuarios().First(u => u.NombreUsuario == "juan");
            Usuario logistica = administracion.ObtenerUsuarios().First(u => u.NombreUsuario == "nicolas");
            Usuario supervisor = administracion.ObtenerUsuarios().First(u => u.NombreUsuario == "maria");

            Paso("Pregunta de seguridad de mariano definida por el administrador",
                () => administracion.DefinirPreguntaSeguridad(compras.Id, UsuarioService.PreguntasSeguridad[0], "Rex el perro"));
            Paso("mariano cambia su propia pregunta de seguridad",
                () => usuarios.CambiarPreguntaSeguridad(compras.Id, ClaveNueva, UsuarioService.PreguntasSeguridad[1], "Calle Falsa"));
            Rechazado("Recuperar la clave con una respuesta equivocada", () => usuarios.RecuperarConPregunta("mariano", "otra cosa", "Nueva-clave-99"));
            Paso("Recuperar la clave con la respuesta correcta", () => usuarios.RecuperarConPregunta("mariano", "Calle Falsa", ClaveNueva));
            Rechazado("Recuperar la clave de un usuario inexistente", () => usuarios.RecuperarConPregunta("usuario.fantasma", "x", "Nueva-clave-99"));

            Paso("El administrador modifica los datos de nicolas", () => administracion.ModificarDatos(logistica.Id, "Nicolás (modificado)", "nicolas@example.test"));
            Paso("El administrador restablece la clave de juan", () => administracion.RestablecerPassword(produccion.Id, ClaveTemporal));
            Paso("juan elige su propia clave", () =>
            {
                Usuario u = usuarios.Autenticar("juan", ClaveTemporal);
                usuarios.CambiarPassword(u.Id, ClaveTemporal, ClaveNueva);
            });

            Paso("Cambio de rol: maria pasa a Vendedor", () => administracion.CambiarRol(supervisor.Id, NombresRol.Vendedor));
            Paso("Cambio de rol: maria pasa a Responsable de Depósito", () => administracion.CambiarRol(supervisor.Id, NombresRol.Deposito));
            Paso("Cambio de rol: maria vuelve a Supervisor", () => administracion.CambiarRol(supervisor.Id, NombresRol.Supervisor));

            Paso("El administrador desactiva a juan", () => administracion.CambiarEstado(produccion.Id, false));
            Rechazado("Ingreso de un usuario desactivado", () => usuarios.Autenticar("juan", ClaveNueva));
            Paso("El administrador vuelve a activar a juan", () => administracion.CambiarEstado(produccion.Id, true));
            Paso("Ingreso correcto de juan", () => PorRol[NombresRol.Produccion] = usuarios.Autenticar("juan", ClaveNueva));

            foreach (Rol rol in administracion.ObtenerRoles().Where(r => r.Nombre == NombresRol.Supervisor || r.Nombre == NombresRol.Logistica))
            {
                Rol esteRol = rol;
                Paso("El administrador desactiva el rol " + esteRol.Nombre, () => administracion.CambiarEstadoRol(esteRol.Id, false));
                Paso("El administrador vuelve a activar el rol " + esteRol.Nombre, () => administracion.CambiarEstadoRol(esteRol.Id, true));
            }

            SesionActual.Iniciar(admin);
        }

        // ---------------------------------------------------------------
        // Los seis empleados: roles, permisos y claves
        // ---------------------------------------------------------------

        private const string ClaveOtra = "Empleado-2026-b";

        private static void Empleados()
        {
            Verificador.Seccion("Empleados: roles, permisos y claves");
            Usuario admin = PorRol[NombresRol.Administrador];
            SesionActual.Iniciar(admin);
            AdministracionService administracion = new AdministracionService();
            UsuarioService usuarios = new UsuarioService();

            for (int i = 0; i < Personas.GetLength(0); i++)
            {
                string nombre = Personas[i, 0];
                string rolPropio = Personas[i, 2];
                string rolAjeno = Personas[(i + 1) % Personas.GetLength(0), 2];
                string rolOtro = Personas[(i + 2) % Personas.GetLength(0), 2];
                Usuario u = administracion.ObtenerUsuarios().First(x => x.NombreUsuario == nombre);

                SesionActual.Iniciar(admin);
                Paso(nombre + ": el administrador le da el rol " + rolAjeno, () => administracion.CambiarRol(u.Id, rolAjeno));
                Paso(nombre + ": el administrador le da el rol " + rolOtro, () => administracion.CambiarRol(u.Id, rolOtro));
                Paso(nombre + ": vuelve a su rol " + rolPropio, () => administracion.CambiarRol(u.Id, rolPropio));

                Permiso nuevo = i % 2 == 0 ? Permiso.CatalogoVer : Permiso.ReportesVer;
                Permiso baja = i % 2 == 0 ? Permiso.ReportesGerenciales : Permiso.CatalogoVer;
                Func<Permiso, bool, Dictionary<Permiso, bool>> pedir = (p, v) =>
                {
                    Dictionary<Permiso, bool> d = administracion.ObtenerPermisosDeUsuario(u.Id).ToDictionary(x => x.Permiso, x => x.Efectivo);
                    d[p] = v;
                    return d;
                };
                Paso(nombre + ": se le agrega el permiso " + nuevo, () => administracion.CambiarPermisosDeUsuario(u.Id, pedir(nuevo, true)));
                Paso(nombre + ": se le quita el permiso " + baja, () => administracion.CambiarPermisosDeUsuario(u.Id, pedir(baja, false)));
                Paso(nombre + ": se le vuelve a dar el permiso " + baja, () => administracion.CambiarPermisosDeUsuario(u.Id, pedir(baja, true)));
                Paso(nombre + ": se restablecen sus permisos", () => administracion.RestablecerPermisos(u.Id));

                Paso(nombre + " cambia su propia clave", () =>
                {
                    Usuario e = Entrar(nombre);
                    string vieja = ClaveNueva;
                    try { usuarios.Autenticar(nombre, ClaveNueva); } catch (Exception) { vieja = ClaveTemporal; }
                    usuarios.CambiarPassword(e.Id, vieja, ClaveOtra);
                    usuarios.CambiarPassword(e.Id, ClaveOtra, ClaveNueva);
                });
                SesionActual.Iniciar(admin);
                Rechazado(nombre + " intenta cambiar su clave con la actual equivocada", () => usuarios.CambiarPassword(u.Id, "no-es-esta-123", ClaveOtra));
                Paso(nombre + ": el administrador le restablece la clave", () => administracion.RestablecerPassword(u.Id, ClaveTemporal));
                Paso(nombre + " elige una clave nueva en su primer ingreso", () =>
                {
                    Usuario e = usuarios.Autenticar(nombre, ClaveTemporal);
                    usuarios.CambiarPassword(e.Id, ClaveTemporal, ClaveNueva);
                    PorRol[rolPropio] = usuarios.Autenticar(nombre, ClaveNueva);
                });
                SesionActual.Iniciar(admin);
                Paso(nombre + ": el administrador modifica sus datos", () => administracion.ModificarDatos(u.Id, Personas[i, 1], nombre + "@edelstahl.example.test"));
            }

            SesionActual.Iniciar(admin);
        }

        // ---------------------------------------------------------------
        // Permisos: lo que cada rol puede y no puede hacer
        // ---------------------------------------------------------------

        private static void Permisos()
        {
            Verificador.Seccion("Permisos por rol (accesos denegados)");

            Entrar("gaston");
            Rechazado("Un vendedor intenta guardar un proveedor", () => new ComprasService().GuardarProveedor(new Proveedor { Cuit = "30-11111111-1", RazonSocial = "Prueba SA" }));
            Rechazado("Un vendedor intenta un aumento masivo de precios", () => new CatalogoService().AumentoMasivoPrecios(null, 5m, "prueba"));
            Rechazado("Un vendedor intenta crear un usuario", () => new AdministracionService().CrearUsuario("prueba.x", "X", "x@example.test", ClaveTemporal, NombresRol.Vendedor));

            Entrar("carlos.deposito");
            Rechazado("Un responsable de depósito intenta emitir una factura", () => new FacturacionService().EmitirFactura(Guid.NewGuid(), TipoComprobante.FacturaA));
            Rechazado("Un responsable de depósito intenta crear una orden de compra", () => new ComprasService().CrearOrden(Guid.NewGuid(), null, string.Empty, string.Empty, new List<ItemCompra>(), true));

            Entrar("mariano");
            Rechazado("Un responsable de compras intenta crear un usuario", () => new AdministracionService().CrearUsuario("prueba.y", "Y", "y@example.test", ClaveTemporal, NombresRol.Vendedor));

            Entrar("nicolas");
            Rechazado("Un responsable de logística intenta guardar un producto", () => new CatalogoService().GuardarCategoria(new CategoriaProducto { Codigo = "X", Nombre = "X", Activo = true }));

            Entrar("juan");
            Rechazado("Un responsable de producción intenta guardar un proveedor", () => new ComprasService().GuardarProveedor(new Proveedor { Cuit = "30-22222222-2", RazonSocial = "Prueba 2 SA" }));

            Entrar("maria");
            Rechazado("Un supervisor intenta crear un usuario", () => new AdministracionService().CrearUsuario("prueba.z", "Z", "z@example.test", ClaveTemporal, NombresRol.Vendedor));

            SesionActual.Iniciar(PorRol[NombresRol.Administrador]);
        }

        // ---------------------------------------------------------------
        // Ventas
        // ---------------------------------------------------------------

        private static string Cuit(int semilla)
        {
            // Un CUIT con 11 dígitos distinto en cada vuelta; el servicio lo normaliza y valida el formato.
            return "2077" + (1000000 + semilla * 137 + DateTime.Now.Second * 11 + DateTime.Now.Millisecond % 97).ToString().PadLeft(7, '0');
        }

        private static void Ventas(int vuelta)
        {
            Verificador.Seccion("Ventas (vuelta " + vuelta + ")");
            Entrar("gaston");

            ClienteService clientes = new ClienteService();
            PresupuestoService presupuestos = new PresupuestoService();
            PedidoService pedidos = new PedidoService();
            CatalogoService catalogo = new CatalogoService();

            Cliente cliente = null;

            Paso("Se elige un cliente ya cargado y se completan sus datos", () =>
            {
                List<Cliente> reales = clientes.ObtenerTodos()
                    .Where(c => c.Activo && !string.IsNullOrWhiteSpace(c.RazonSocial)
                        && !c.RazonSocial.StartsWith("Cliente de prueba") && !c.RazonSocial.StartsWith("Cliente descartable")
                        && !c.RazonSocial.StartsWith("NOSirve") && !c.RazonSocial.StartsWith("Cliente Prueba"))
                    .OrderBy(c => c.RazonSocial).ToList();

                if (reales.Count == 0)
                {
                    throw new InvalidOperationException("No hay clientes cargados.");
                }

                cliente = reales[(vuelta * 7 + DateTime.Now.Second) % reales.Count];
                Console.WriteLine("  cliente elegido: " + cliente.RazonSocial);

                // Si le falta dirección, crédito o saldo disponible, se le agregan (es parte de lo que hace una persona).
                bool cambia = false;

                if (string.IsNullOrWhiteSpace(cliente.DireccionFacturacion) || cliente.DireccionFacturacion.StartsWith("Address not"))
                {
                    cliente.DireccionFacturacion = "Calle de Ejemplo " + (100 + vuelta * 13);
                    cliente.DireccionEntrega = cliente.DireccionFacturacion;
                    cambia = true;
                }

                if (cliente.LimiteCredito - cliente.DeudaActual < 5000000m)
                {
                    cliente.AutorizadoACredito = true;
                    cliente.LimiteCredito = cliente.DeudaActual + 5000000m;
                    cambia = true;
                }

                if (cambia)
                {
                    clientes.Modificar(cliente);
                }
            });

            if (cliente == null)
            {
                return;
            }

            Paso("Se modifica el teléfono del cliente", () =>
            {
                Cliente c = clientes.ObtenerPorId(cliente.Id);
                c.Telefono = "1166660" + vuelta.ToString().PadLeft(3, '0');
                clientes.Modificar(c);
            });

            Rechazado("Alta de un cliente repetido (mismo CUIT que uno ya cargado)", () => clientes.Registrar(new Cliente
            {
                Tipo = TipoPersona.Empresa, CUIT = cliente.CUIT, RazonSocial = "Repetido", DireccionFacturacion = "Calle 1", DireccionEntrega = "Calle 1",
                Localidad = "CABA", Provincia = "Buenos Aires", CodigoPostal = "1000", Email = "r@example.test", Telefono = "1100000000"
            }));

            List<ProductoListaDto> conStock = catalogo.BuscarProductos(string.Empty, null, TipoProducto.ProductoTerminado, true, true)
                .Where(p => p.Disponible >= 10m && p.PrecioBase > 0m).Take(2).ToList();

            Func<decimal, PresupuestoSolicitud> solicitud = anticipo =>
            {
                PresupuestoSolicitud s = new PresupuestoSolicitud
                {
                    ClienteId = cliente.Id, TipoCambio = 1515m, PorcentajeAnticipo = anticipo, CondicionPago = "Contado",
                    PlazoEntrega = "10 días", Enviar = true, EntregaIncluida = true
                };

                foreach (ProductoListaDto p in conStock)
                {
                    s.Lineas.Add(new LineaPresupuestoSolicitud { ProductoId = p.Id, Tipo = TipoItemPresupuesto.Producto, Cantidad = 2m, PrecioUnitario = p.PrecioBase });
                }

                s.Lineas.Add(new LineaPresupuestoSolicitud
                {
                    Tipo = TipoItemPresupuesto.Servicio, Codigo = "SERV-PRUEBA", Descripcion = "Servicio de prueba", Cantidad = 1m, PrecioUnitario = 1500m
                });

                return s;
            };

            Presupuesto aceptado = null;
            Presupuesto rechazado = null;
            Presupuesto vencible = null;

            Paso("Presupuesto 1 (se acepta y se confirma como pedido)", () =>
            {
                aceptado = presupuestos.CrearPresupuesto(solicitud(0m));
                presupuestos.CambiarEstado(aceptado.Id, EstadoPresupuesto.Aceptado);
            });

            Paso("Presupuesto 2 (se rechaza)", () =>
            {
                rechazado = presupuestos.CrearPresupuesto(solicitud(0m));
                presupuestos.CambiarEstado(rechazado.Id, EstadoPresupuesto.Rechazado);
            });

            Paso("Presupuesto 3 (se extiende la vigencia)", () =>
            {
                vencible = presupuestos.CrearPresupuesto(solicitud(0m));
                presupuestos.ExtenderVigencia(vencible.Id, 7);
            });

            Rechazado("Un descuento general de 40% sin autorización", () =>
            {
                PresupuestoSolicitud s = solicitud(0m);
                s.PorcentajeDescuentoGeneral = 40m;
                presupuestos.CrearPresupuesto(s);
            });

            Rechazado("Confirmar un presupuesto que no fue aceptado", () => pedidos.ConfirmarDesdePresupuesto(cliente.Id, vencible.Id));

            Pedido pedido = null;

            if (aceptado != null)
            {
                Paso("Se confirma el pedido del presupuesto 1", () => pedido = pedidos.ConfirmarDesdePresupuesto(cliente.Id, aceptado.Id));
            }

            if (pedido != null)
            {
                Paso("Se cancela el pedido confirmado", () => pedidos.Cancelar(pedido.Id, "Cancelación de prueba " + vuelta));
            }

            Paso("Se imprime el presupuesto aceptado", () =>
            {
                Presupuesto p = presupuestos.ObtenerPorId(aceptado.Id);
                new Edelstahl.Services.Documents.PresupuestoPdfService().Generar(p, clientes.ObtenerPorId(cliente.Id));
            });
        }

        // ---------------------------------------------------------------
        // Compras
        // ---------------------------------------------------------------

        private static void Compras(int vuelta)
        {
            Verificador.Seccion("Compras y pago a proveedores (vuelta " + vuelta + ")");
            Entrar("mariano");

            ComprasService compras = new ComprasService();
            CatalogoService catalogo = new CatalogoService();
            FinanzasService finanzas = new FinanzasService();
            Producto chapa = catalogo.ObtenerPorSku("MP-304-2.0");

            Proveedor proveedor = null;

            Paso("Alta de un proveedor", () =>
            {
                proveedor = compras.GuardarProveedor(new Proveedor
                {
                    Cuit = "30-" + (70000000 + vuelta * 773 + DateTime.Now.Millisecond).ToString().PadLeft(8, '0') + "-" + (vuelta % 9 + 1),
                    RazonSocial = "Proveedor de prueba " + vuelta + "-" + DateTime.Now.ToString("HHmmss"),
                    Email = "proveedor" + vuelta + "@example.test", Telefono = "1144440000", Direccion = "Ruta 8 km " + vuelta, Activo = true,
                    LimiteCredito = 100000000m, PlazoEntregaDias = 5
                });
            });

            if (proveedor == null)
            {
                proveedor = compras.ObtenerProveedores(string.Empty, true).First();
            }

            OrdenCompra entregada = null;
            OrdenCompra anulada = null;

            Paso("Orden de compra 1 emitida", () =>
                entregada = compras.CrearOrden(proveedor.Id, DateTime.Today.AddDays(7), "30 días", "Reposición de prueba " + vuelta,
                    new List<ItemCompra> { new ItemCompra { ProductoId = chapa.Id, Cantidad = 20m, Costo = 44m, Descuento = 0m } }, true));

            Paso("Orden de compra 2 en borrador", () =>
                anulada = compras.CrearOrden(proveedor.Id, null, string.Empty, "Borrador de prueba " + vuelta,
                    new List<ItemCompra> { new ItemCompra { ProductoId = chapa.Id, Cantidad = 3m, Costo = 40m } }, false));

            if (anulada != null)
            {
                Paso("Se emite la orden de compra 2", () => compras.EmitirOrden(anulada.Id));
                Paso("Se cancela la orden de compra 2", () => compras.CancelarOrden(anulada.Id, "Cancelación de prueba " + vuelta));
            }

            Paso("Pedir stock: se consultan las sugerencias de compra", () => compras.SugerirCompras());

            if (entregada != null)
            {
                Paso("Se imprime la orden de compra", () => new DocumentosService().GenerarOrdenCompra(entregada.Id));

                DetalleOrdenCompraDto detalle = compras.ObtenerDetalles(entregada.Id).First();

                Entrar("carlos.deposito");
                Rechazado("Un responsable de depósito intenta registrar una recepción de compra", () =>
                    compras.RegistrarRecepcion(entregada.Id, "R-X" + vuelta,
                        new List<RecepcionItem> { new RecepcionItem { DetalleOrdenCompraId = detalle.DetalleId, Recibida = 1m, Aceptada = 1m } }, string.Empty));

                Entrar("mariano");

                Paso("Recepción parcial de la orden de compra", () =>
                    compras.RegistrarRecepcion(entregada.Id, "R-P" + vuelta + "A",
                        new List<RecepcionItem> { new RecepcionItem { DetalleOrdenCompraId = detalle.DetalleId, Recibida = 12m, Aceptada = 10m, Rechazada = 2m, MotivoRechazo = "Chapas con corrosión", Lote = "L-P" + vuelta } },
                        "Recepción parcial de prueba"));
                Paso("Se recibe el resto de la orden de compra", () =>
                    compras.RegistrarRecepcion(entregada.Id, "R-P" + vuelta + "B",
                        new List<RecepcionItem> { new RecepcionItem { DetalleOrdenCompraId = detalle.DetalleId, Recibida = 10m, Aceptada = 10m } }, string.Empty));

                SesionActual.Iniciar(PorRol[NombresRol.Administrador]);
                MedioPago transferencia = finanzas.ObtenerMediosPago(true).First(m => m.Codigo == "TRANSFERENCIA");
                Paso("Pago parcial al proveedor", () => finanzas.RegistrarPagoProveedor(proveedor.Id, entregada.Id, transferencia.Id, 500m, "TRF-P" + vuelta, "Banco de prueba", "Anticipo"));
                Paso("Segundo pago al proveedor", () => finanzas.RegistrarPagoProveedor(proveedor.Id, entregada.Id, transferencia.Id, 250m, "TRF-Q" + vuelta, "Banco de prueba", "Saldo parcial"));
            }
        }

        // ---------------------------------------------------------------
        // Catálogo
        // ---------------------------------------------------------------

        private static void Catalogo(int vuelta)
        {
            Verificador.Seccion("Catálogo (vuelta " + vuelta + ")");
            SesionActual.Iniciar(PorRol[NombresRol.Administrador]);

            CatalogoService catalogo = new CatalogoService();
            CategoriaProducto categoria = null;
            string marca = DateTime.Now.ToString("HHmmss");

            Paso("Alta de una categoría", () => categoria = catalogo.GuardarCategoria(new CategoriaProducto { Codigo = "PRUEBA-" + vuelta + "-" + marca, Nombre = "Categoría de prueba " + vuelta, Descripcion = "Creada por la prueba de bitácora", Activo = true }));

            Producto producto = null;

            Paso("Alta de un producto nuevo", () => producto = catalogo.GuardarProducto(new Producto
            {
                CodigoSku = "PRUEBA-" + vuelta + "-" + marca,
                Nombre = "Producto de prueba " + vuelta,
                Descripcion = "Producto creado para probar la bitácora",
                CategoriaId = categoria == null ? (Guid?)null : categoria.Id,
                Tipo = TipoProducto.ProductoTerminado,
                UnidadMedida = "UNIDAD",
                PrecioBase = 1000m + vuelta * 100m,
                CostoActual = 600m,
                StockMinimo = 5m,
                PuntoReposicion = 10m,
                EsVendible = true,
                EsComprable = true,
                Activo = true
            }));

            if (producto != null)
            {
                Paso("Se modifica el producto (precio y descripción)", () =>
                {
                    Producto p = catalogo.ObtenerProducto(producto.Id);
                    p.PrecioBase += 250m;
                    p.Descripcion = "Descripción modificada en la vuelta " + vuelta;
                    catalogo.GuardarProducto(p);
                });
                Paso("Se desactiva el producto", () => catalogo.CambiarEstadoProducto(producto.Id, false));
                Paso("Se vuelve a activar el producto", () => catalogo.CambiarEstadoProducto(producto.Id, true));
            }

            if (categoria != null)
            {
                Paso("Aumento masivo de precios en la categoría de prueba", () => catalogo.AumentoMasivoPrecios(categoria.Id, 5m, "Ajuste de prueba " + vuelta));
            }

            Rechazado("Alta de un producto repetido", () =>
            {
                Producto repetido = catalogo.ObtenerProducto(producto.Id);
                repetido.Id = Guid.NewGuid();
                catalogo.GuardarProducto(repetido);
            });
        }

        // ---------------------------------------------------------------
        // Stock
        // ---------------------------------------------------------------

        private static void Stock(int vuelta)
        {
            Verificador.Seccion("Stock (vuelta " + vuelta + ")");
            Entrar("carlos.deposito");

            InventarioService inventario = new InventarioService();
            CatalogoService catalogo = new CatalogoService();
            Producto disco = catalogo.ObtenerPorSku("IN-DISCO-PULIDO");
            List<StockDto> stock = inventario.ConsultarStock("IN-DISCO-PULIDO", null, false);

            if (disco == null || stock.Count == 0)
            {
                Paso("No hay stock del producto de prueba", () => { throw new InvalidOperationException("Falta IN-DISCO-PULIDO"); });
                return;
            }

            Guid ubicacion = stock.First().UbicacionId;

            Paso("Ajuste positivo de stock", () => inventario.RegistrarMovimiento(new MovimientoSolicitud
            { ProductoId = disco.Id, UbicacionId = ubicacion, Tipo = TipoMovimientoStock.AjustePositivo, Cantidad = 5m, Observaciones = "Ajuste de prueba " + vuelta }));
            Paso("Ajuste negativo de stock", () => inventario.RegistrarMovimiento(new MovimientoSolicitud
            { ProductoId = disco.Id, UbicacionId = ubicacion, Tipo = TipoMovimientoStock.AjusteNegativo, Cantidad = 5m, Observaciones = "Rotura de prueba " + vuelta }));
            Rechazado("Un ajuste que deja el stock en negativo", () => inventario.RegistrarMovimiento(new MovimientoSolicitud
            { ProductoId = disco.Id, UbicacionId = ubicacion, Tipo = TipoMovimientoStock.AjusteNegativo, Cantidad = 9999999m, Observaciones = "Imposible" }));

            Paso("Alta de un depósito", () => inventario.GuardarDeposito(new Deposito
            { Codigo = "DP" + vuelta + DateTime.Now.ToString("mmss"), Nombre = "Depósito de prueba " + vuelta, Localidad = "Avellaneda", Responsable = "Carlos", Activo = true }));
        }

        // ---------------------------------------------------------------
        // Logística
        // ---------------------------------------------------------------

        private static void Logistica(int vuelta)
        {
            Verificador.Seccion("Logística (vuelta " + vuelta + ")");
            Entrar("nicolas");

            LogisticaService logistica = new LogisticaService();
            string letras = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            string sufijo = letras[vuelta % letras.Length].ToString() + letras[(vuelta * 7 + DateTime.Now.Second) % letras.Length];
            string patente = "PR" + (100 + vuelta * 11 + DateTime.Now.Millisecond % 89) + sufijo;

            Paso("Alta de un vehículo", () => logistica.GuardarVehiculo(new Vehiculo
            {
                Patente = patente, Marca = "Iveco", Modelo = "Daily prueba", Anio = 2022, CapacidadKg = 3500m,
                VencimientoSeguro = DateTime.Today.AddMonths(8), VencimientoVtv = DateTime.Today.AddMonths(8), Estado = "DISPONIBLE", Activo = true
            }));

            Paso("Alta de un chofer", () => logistica.GuardarChofer(new Chofer
            {
                Documento = (20000000 + vuelta * 4321 + DateTime.Now.Millisecond).ToString(), Apellido = "Prueba" + vuelta, Nombre = "Chofer",
                LicenciaConducir = "LIC-P" + vuelta + DateTime.Now.ToString("mmss"), CategoriaLicencia = "C", VencimientoLicencia = DateTime.Today.AddYears(2),
                Telefono = "1133330000", Activo = true
            }));
        }


        // ---------------------------------------------------------------
        // Permisos individuales: el administrador agrega, quita y modifica
        // ---------------------------------------------------------------

        private static void PermisosIndividuales(int vuelta)
        {
            Verificador.Seccion("Permisos individuales (vuelta " + vuelta + ")");
            SesionActual.Iniciar(PorRol[NombresRol.Administrador]);
            AdministracionService admin = new AdministracionService();

            object[,] casos =
            {
                { "gaston", Permiso.ProveedoresGestionar, Permiso.PresupuestosGestionar, Permiso.ReportesGerenciales },
                { "carlos.deposito", Permiso.ComprasGestionar, Permiso.InventarioMover, Permiso.CatalogoEditar },
                { "mariano", Permiso.CatalogoEditar, Permiso.ProveedoresGestionar, Permiso.PagosRegistrar },
                { "nicolas", Permiso.ReportesGerenciales, Permiso.DevolucionesGestionar, Permiso.ComprasVer },
                { "juan", Permiso.CalidadRegistrar, Permiso.ProduccionGestionar, Permiso.LogisticaVer }
            };

            for (int i = 0; i < casos.GetLength(0); i++)
            {
                string nombre = (string)casos[i, 0];
                Permiso agregar = (Permiso)casos[i, 1];
                Permiso quitar = (Permiso)casos[i, 2];
                Permiso otro = (Permiso)casos[i, 3];
                Guid id = admin.ObtenerUsuarios().First(u => u.NombreUsuario == nombre).Id;

                Func<Permiso, bool, Dictionary<Permiso, bool>> pedir = (permiso, valor) =>
                {
                    Dictionary<Permiso, bool> d = admin.ObtenerPermisosDeUsuario(id).ToDictionary(p => p.Permiso, p => p.Efectivo);
                    d[permiso] = valor;
                    return d;
                };

                Paso("El administrador agrega " + agregar + " a " + nombre, () => admin.CambiarPermisosDeUsuario(id, pedir(agregar, true)));
                Paso("El administrador quita " + quitar + " a " + nombre, () => admin.CambiarPermisosDeUsuario(id, pedir(quitar, false)));
                Paso("El administrador modifica: le agrega también " + otro, () => admin.CambiarPermisosDeUsuario(id, pedir(otro, true)));
                Paso("El administrador le quita lo que había agregado (" + agregar + ")", () => admin.CambiarPermisosDeUsuario(id, pedir(agregar, false)));
                Paso("El administrador consulta los permisos de " + nombre, () => admin.ObtenerPermisosDeUsuario(id));
                Rechazado("Se intenta dar el permiso de administrar usuarios a " + nombre, () => admin.CambiarPermisosDeUsuario(id, pedir(Permiso.UsuariosAdministrar, true)));

                Entrar(nombre);
                Rechazado(nombre + " intenta darse un permiso a sí mismo", () => admin.CambiarPermisosDeUsuario(id, pedir(Permiso.ComprasGestionar, true)));
                SesionActual.Iniciar(PorRol[NombresRol.Administrador]);

                Paso("El administrador restablece los permisos de " + nombre + " a los de su rol", () => admin.RestablecerPermisos(id));
            }

            Rechazado("Se intenta quitar un permiso a un administrador", () =>
                admin.CambiarPermisosDeUsuario(PorRol[NombresRol.Administrador].Id, new Dictionary<Permiso, bool> { { Permiso.ComprasVer, false } }));

            SesionActual.Iniciar(PorRol[NombresRol.Administrador]);
        }

        // ---------------------------------------------------------------
        // Seguridad y sesiones
        // ---------------------------------------------------------------

        private static void Seguridad(int vuelta)
        {
            Verificador.Seccion("Sesiones (vuelta " + vuelta + ")");
            UsuarioService usuarios = new UsuarioService();

            foreach (string nombre in new[] { "gaston", "carlos.deposito", "mariano", "nicolas" })
            {
                string n = nombre;
                Paso("Ingreso de " + n, () => usuarios.Autenticar(n, ClaveNueva));
            }

            Rechazado("Ingreso con clave incorrecta de nicolas", () => usuarios.Autenticar("nicolas", "mal-" + vuelta + "-yyyyy"));
            Paso("Prueba.ventas cambia su clave y la vuelve a cambiar", () =>
            {
                Usuario u = usuarios.Autenticar("gaston", ClaveNueva);
                usuarios.CambiarPassword(u.Id, ClaveNueva, "Prueba-2026-otra" + vuelta);
                usuarios.CambiarPassword(u.Id, "Prueba-2026-otra" + vuelta, ClaveNueva);
            });

            SesionActual.Iniciar(PorRol[NombresRol.Administrador]);
        }
    }
}
