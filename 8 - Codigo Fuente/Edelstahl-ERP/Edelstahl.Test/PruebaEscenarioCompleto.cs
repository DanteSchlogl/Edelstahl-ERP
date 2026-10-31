using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Exceptions.Base;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.BLL.Services;
using Edelstahl.DAL.EntityFramework;
using Edelstahl.DAL.Factory;
using Edelstahl.Domain.Catalogo;
using Edelstahl.Domain.Comercial;
using Edelstahl.Domain.Compras;
using Edelstahl.Domain.Finanzas;
using Edelstahl.Domain.Logistica;
using Edelstahl.Domain.Produccion;
using Edelstahl.Domain.Security;
using Edelstahl.Services.Configuration;

namespace Edelstahl.Test
{
    /// <summary>
    /// Recorre todos los casos de uso de punta a punta: presupuesto, pedido con
    /// reserva de stock, producción, facturación, cobro, remito, hoja de ruta,
    /// entrega, devolución, nota de crédito, compra y pago a proveedor.
    /// </summary>
    public static class PruebaEscenarioCompleto
    {
        private static Usuario UsuarioConRol(string rol)
        {
            return new Usuario
            {
                NombreUsuario = "prueba",
                NombreCompleto = "Usuario de prueba",
                Rol = new Rol { Nombre = rol }
            };
        }

        public static void Ejecutar(ExecutionMode modo)
        {
            Verificador.Seccion("Inicio en modo " + modo);

            ApplicationMode.Reset();
            ApplicationMode.Select(modo);
            FactoryDataAccess.Inicializar(modo);

            Stopwatch reloj = Stopwatch.StartNew();

            if (modo == ExecutionMode.SqlServer)
            {
                InicializadorNegocio.Inicializar();
            }

            SesionActual.Iniciar(UsuarioConRol(NombresRol.Administrador));
            new CargaInicialService().AsegurarDatosBase();

            Verificador.Verificar(true, "Inicialización lista en " + reloj.ElapsedMilliseconds + " ms");

            CargaDeDatos();
            Seguridad();

            Cliente cliente = PrepararCliente();
            Pedido pedido = FlujoComercial(cliente);
            FlujoProduccion(pedido);
            Factura factura = FlujoFinanzas(pedido, cliente);
            RemitoEntrega remito = FlujoLogistica(pedido);
            FlujoDevolucion(remito, factura, cliente);
            FlujoCompras();
            ReglasNegativas(pedido, factura);
            ClientesYPresupuestos(cliente);
            Auditoria();
        }

        // ---------------------------------------------------------------
        // Datos iniciales
        // ---------------------------------------------------------------

        private static void CargaDeDatos()
        {
            Verificador.Seccion("Carga de datos de ejemplo");

            Stopwatch reloj = Stopwatch.StartNew();
            ResultadoImportacion resultado = new CargaInicialService().CargarDatosDeEjemplo(null);

            Console.WriteLine("  (carga: " + reloj.ElapsedMilliseconds + " ms, importados " + resultado.Importados
                + ", omitidos " + resultado.Omitidos + ", errores " + resultado.Errores.Count + ")");

            foreach (string error in resultado.Errores.Take(5))
            {
                Console.WriteLine("  aviso: " + error);
            }

            CatalogoService catalogo = new CatalogoService();
            List<ProductoListaDto> productos = catalogo.BuscarProductos(string.Empty, null, null, false, false);

            Verificador.Verificar(productos.Count >= 480, "El catálogo tiene " + productos.Count + " productos");
            Verificador.Verificar(catalogo.ObtenerCategorias().Count >= 10, "Hay categorías jerárquicas cargadas");
            Verificador.Verificar(catalogo.ObtenerPorSku("MP-316L-3.0") != null, "Las materias primas de ejemplo existen");
            Verificador.Verificar(new ClienteService().ObtenerTodos().Count >= 48, "Se cargaron los clientes");
            Verificador.Verificar(new ComprasService().ObtenerProveedores(string.Empty, true).Count >= 4, "Hay proveedores");
            Verificador.Verificar(new LogisticaService().ObtenerVehiculos(true).Count >= 3, "Hay vehículos");
            Verificador.Verificar(new LogisticaService().ObtenerChoferes(true).Count >= 3, "Hay choferes");

            ResultadoImportacion repetida = new CargaInicialService().CargarDatosDeEjemplo(null);
            Verificador.Igual(0, repetida.Importados, "Repetir la carga no duplica registros");
        }

        private static void Seguridad()
        {
            Verificador.Seccion("Permisos por rol");

            SesionActual.Iniciar(UsuarioConRol(NombresRol.Vendedor));

            Verificador.Falla<AccesoNoAutorizadoException>(
                () => new CatalogoService().AumentoMasivoPrecios(null, 10m, "prueba"),
                "Un vendedor no puede modificar precios en forma masiva");

            Verificador.Falla<AccesoNoAutorizadoException>(
                () => new ComprasService().GuardarProveedor(new Proveedor { Cuit = "30-11111111-1", RazonSocial = "Prueba SA" }),
                "Un vendedor no puede gestionar proveedores");

            SesionActual.Iniciar(UsuarioConRol(NombresRol.Deposito));

            Verificador.Falla<AccesoNoAutorizadoException>(
                () => new FacturacionService().EmitirFactura(Guid.NewGuid(), TipoComprobante.FacturaA),
                "Un responsable de depósito no puede facturar");

            SesionActual.Iniciar(UsuarioConRol(NombresRol.Administrador));
        }

        // ---------------------------------------------------------------
        // Cliente y flujo comercial
        // ---------------------------------------------------------------

        private static void ClientesYPresupuestos(Cliente empresa)
        {
            Verificador.Seccion("Clientes particulares y estados del presupuesto");

            ClienteService clientes = new ClienteService();
            Cliente particular = clientes.Registrar(new Cliente
            {
                Tipo = TipoPersona.Particular,
                CUIT = "27222333449",
                RazonSocial = "Maria Particular",
                DireccionFacturacion = "Calle 1 100",
                DireccionEntrega = "Calle 1 100",
                Localidad = "CABA",
                Provincia = "Buenos Aires",
                CodigoPostal = "1000",
                Email = "maria.particular@example.test",
                Telefono = "1155550000"
            });

            Cliente leido = clientes.ObtenerPorId(particular.Id);
            Verificador.Igual(TipoPersona.Particular, leido.Tipo, "Un cliente particular se guarda como particular");
            Verificador.Igual("CUIL", leido.EtiquetaDocumento, "El particular se identifica con CUIL");
            Verificador.Igual("CUIT", clientes.ObtenerPorId(empresa.Id).EtiquetaDocumento, "La empresa se identifica con CUIT");

            Verificador.Verificar(Direcciones.Falta("Address not provided") && Direcciones.Falta("   ") && !Direcciones.Falta("Peru 1251"),
                "Una direccion vacia o de relleno cuenta como faltante");

            PresupuestoService presupuestos = new PresupuestoService();
            PresupuestoSolicitud solicitud = new PresupuestoSolicitud { ClienteId = particular.Id, TipoCambio = 1m, Enviar = true, EntregaIncluida = true, PorcentajeAnticipo = 30m };
            solicitud.Lineas.Add(new LineaPresupuestoSolicitud
            {
                Tipo = TipoItemPresupuesto.Servicio, Codigo = "SERV-X", Descripcion = "Servicio", Cantidad = 1m, PrecioUnitario = 100m
            });

            Presupuesto p = presupuestos.CrearPresupuesto(solicitud);
            presupuestos.CambiarEstado(p.Id, EstadoPresupuesto.Aceptado);
            presupuestos.CambiarEstado(p.Id, EstadoPresupuesto.Rechazado);
            Verificador.Igual(EstadoPresupuesto.Rechazado, presupuestos.ObtenerPorId(p.Id).Estado, "Un presupuesto aceptado se puede rechazar mientras no sea pedido");

            Cliente sinDireccion = clientes.ObtenerPorId(particular.Id);
            sinDireccion.DireccionEntrega = "Address not provided";
            clientes.Modificar(sinDireccion);

            Presupuesto q = presupuestos.CrearPresupuesto(solicitud);
            presupuestos.CambiarEstado(q.Id, EstadoPresupuesto.Aceptado);
            Verificador.Falla<ReglaDeNegocioException>(
                () => new PedidoService().ConfirmarDesdePresupuesto(particular.Id, q.Id),
                "No se confirma un pedido con entrega si el cliente no tiene direccion");

            Verificador.Verificar(new CatalogoService().ObtenerFabricables().Any(fab => fab.Sku == "FILTRO-10"),
                "La fabricacion especial se arma sobre un producto fabricable del catalogo");

            Verificador.Falla<ReglaDeNegocioException>(
                () => new AdministracionService().CrearUsuario("amigo.sup", "Amigo Supervisor", "amigo@example.test", "corta", NombresRol.Supervisor),
                "Una contrasena corta se avisa como regla de negocio y no como error inesperado");
            Usuario amigo = new AdministracionService().CrearUsuario("amigo.sup", "Amigo Supervisor", "amigo@example.test", "clave-larga-1", NombresRol.Supervisor);
            new AdministracionService().ModificarDatos(amigo.Id, "Amigo S. Supervisor", "amigo.s@example.test");
            new AdministracionService().RestablecerPassword(amigo.Id, "otra-clave-2");
            Verificador.Verificar(new AdministracionService().ObtenerUsuarios().Any(u => u.Id == amigo.Id && u.NombreCompleto == "Amigo S. Supervisor"),
                "El administrador crea, modifica y restablece la clave de otro usuario");
            Verificador.Verificar(new AdministracionService().ObtenerUsuarios().Any(u => u.Id == amigo.Id && u.DebeCambiarPassword),
                "Al restablecer una clave el usuario queda obligado a elegir una propia");
            new UsuarioService().CambiarPassword(amigo.Id, "otra-clave-2", "clave-propia-3");
            Verificador.Verificar(new AdministracionService().ObtenerUsuarios().Any(u => u.Id == amigo.Id && !u.DebeCambiarPassword),
                "Al elegir su propia clave deja de estar obligado a cambiarla");

            // Pregunta de seguridad y recuperacion de la clave
            UsuarioService usuariosSvc = new UsuarioService();
            Verificador.Falla<ReglaDeNegocioException>(
                () => usuariosSvc.ObtenerPreguntaSeguridad("amigo.sup"),
                "Sin pregunta de seguridad no se puede recuperar la clave con pregunta");
            Verificador.Falla<ReglaDeNegocioException>(
                () => new AdministracionService().DefinirPreguntaSeguridad(amigo.Id, "Pregunta inventada", "Rex"),
                "Solo se aceptan las preguntas de la lista");
            new AdministracionService().DefinirPreguntaSeguridad(amigo.Id, UsuarioService.PreguntasSeguridad[0], "Rex el Perro");
            Verificador.Verificar(usuariosSvc.ObtenerPreguntaSeguridad("amigo.sup") == UsuarioService.PreguntasSeguridad[0],
                "El administrador define la pregunta y el usuario la ve en la pantalla de recuperacion");
            Verificador.Falla<ReglaDeNegocioException>(
                () => usuariosSvc.RecuperarConPregunta("amigo.sup", "otra cosa", "Nueva-clave-9"),
                "Una respuesta incorrecta no cambia la clave");
            usuariosSvc.RecuperarConPregunta("amigo.sup", "  REX   el   perro ", "Nueva-clave-9");
            Verificador.Verificar(usuariosSvc.Autenticar("amigo.sup", "Nueva-clave-9") != null,
                "Con la respuesta correcta (sin importar mayusculas ni espacios) se elige una clave nueva y se puede entrar");
            Verificador.Falla<ReglaDeNegocioException>(
                () => usuariosSvc.CambiarPreguntaSeguridad(amigo.Id, "clave-equivocada", UsuarioService.PreguntasSeguridad[1], "Buenos Aires"),
                "Para cambiar la pregunta hay que escribir la contrasena actual");
            usuariosSvc.CambiarPreguntaSeguridad(amigo.Id, "Nueva-clave-9", UsuarioService.PreguntasSeguridad[1], "Buenos Aires");
            Verificador.Verificar(usuariosSvc.ObtenerPreguntaSeguridad("amigo.sup") == UsuarioService.PreguntasSeguridad[1],
                "El usuario cambia su propia pregunta de seguridad");
            // una respuesta corta (menos de 8 caracteres) tambien se puede guardar y usar
            usuariosSvc.CambiarPreguntaSeguridad(amigo.Id, "Nueva-clave-9", UsuarioService.PreguntasSeguridad[0], "Rex");
            usuariosSvc.RecuperarConPregunta("amigo.sup", "REX", "Nueva-clave-9");
            Verificador.Verificar(usuariosSvc.Autenticar("amigo.sup", "Nueva-clave-9") != null,
                "Una respuesta corta como Rex se guarda y permite recuperar la clave");
            usuariosSvc.CambiarPreguntaSeguridad(amigo.Id, "Nueva-clave-9", UsuarioService.PreguntasSeguridad[1], "Buenos Aires");
            for (int intento = 0; intento < 5; intento++)
            {
                try { usuariosSvc.RecuperarConPregunta("amigo.sup", "mal", "Nueva-clave-10"); } catch (ReglaDeNegocioException) { }
            }
            Verificador.Falla<ReglaDeNegocioException>(
                () => usuariosSvc.RecuperarConPregunta("amigo.sup", "Buenos Aires", "Nueva-clave-10"),
                "Cinco respuestas incorrectas bloquean al usuario aunque despues acierte");
            new AdministracionService().Desbloquear(amigo.Id);

            // Pedido de ayuda al administrador segun el perfil
            Verificador.Verificar(AyudaService.TemasPara(NombresRol.Vendedor).Count > 3
                && AyudaService.TemasPara(NombresRol.Produccion).Contains("Materials are missing for an order")
                && !AyudaService.TemasPara(NombresRol.Vendedor).Contains("Materials are missing for an order"),
                "Cada perfil ve temas de ayuda propios");
            new AyudaService().RegistrarPedido(AyudaService.TemasPara(SesionActual.NombreRol)[0], "prueba automatica");
            Verificador.Verificar(new BitacoraService().ObtenerTodos().Any(r => r.Accion == "Pedido de ayuda" && r.Descripcion.Contains("prueba automatica")),
                "El pedido de ayuda queda registrado en la bitacora");
            PresupuestoSolicitud redondeo = new PresupuestoSolicitud { ClienteId = empresa.Id, TipoCambio = 1m, PorcentajeAnticipo = 30m, Enviar = true };
            redondeo.Lineas.Add(new LineaPresupuestoSolicitud { Tipo = TipoItemPresupuesto.Servicio, Codigo = "SERV-R", Descripcion = "Servicio", Cantidad = 3m, PrecioUnitario = 37.77m });
            Presupuesto pr = presupuestos.CrearPresupuesto(redondeo);
            presupuestos.CambiarEstado(pr.Id, EstadoPresupuesto.Aceptado);
            Pedido conRedondeo = new PedidoService().ConfirmarDesdePresupuesto(empresa.Id, pr.Id, Math.Round(pr.CalcularAnticipo(), 2), "Efectivo", "REC-R", DateTime.Today);
            Verificador.Verificar(conRedondeo != null, "La sena redondeada a dos decimales se acepta al confirmar el pedido");

            Verificador.Verificar(MatrizPermisos.Tiene(NombresRol.Supervisor, Permiso.ReportesGerenciales)
                && MatrizPermisos.Tiene(NombresRol.Supervisor, Permiso.BitacoraVer)
                && !MatrizPermisos.Tiene(NombresRol.Supervisor, Permiso.PedidosConfirmar),
                "El supervisor consulta y ve reportes y bitacora pero no modifica");

            Verificador.Verificar(new ProduccionService().ObtenerTablero(true).Count == 0
                || new ProduccionService().ObtenerResumenOrden(new ProduccionService().ObtenerTablero(true)[0].Id).Count >= 1,
                "El resumen de una orden muestra sus pasos");
            Verificador.Verificar(new LogisticaService().ObtenerHistorialEnvios().Count >= 1
                && new LogisticaService().ObtenerEventosEnvio(new LogisticaService().ObtenerHistorialEnvios()[0].Id).Count >= 1,
                "La bitacora de envios muestra los pasos de cada remito");

            PresupuestoSolicitud aMedidaSol = new PresupuestoSolicitud { ClienteId = empresa.Id, TipoCambio = 1m, PorcentajeAnticipo = 30m, Enviar = true };
            aMedidaSol.Lineas.Add(new LineaPresupuestoSolicitud
            {
                ProductoId = new CatalogoService().ObtenerPorSku("FILTRO-10").Id, Tipo = TipoItemPresupuesto.FabricacionEspecial,
                Cantidad = 1m, PrecioUnitario = 500m, DescripcionTecnica = "Prueba de solicitud a medida"
            });
            Presupuesto aMedida = presupuestos.CrearPresupuesto(aMedidaSol);
            presupuestos.CambiarEstado(aMedida.Id, EstadoPresupuesto.Aceptado);
            Verificador.Falla<ReglaDeNegocioException>(
                () => new PedidoService().ConfirmarDesdePresupuesto(empresa.Id, aMedida.Id, 1m, "Efectivo", "REC-X", DateTime.Today),
                "La sena no puede ser menor al minimo que exige el presupuesto");

            Verificador.Verificar(new ProduccionService().ObtenerOrdenesPorAceptar().Any(o => o.Presupuesto == aMedida.Numero && o.OrdenId == Guid.Empty),
                "Un presupuesto aceptado con fabricacion especial aparece en Produccion esperando la sena");

            Verificador.Verificar(new ProduccionService().GenerarOrdenesDelPedido(Guid.NewGuid()).Count == 0,
                "Un pedido sin lineas a medida no genera ordenes de produccion");

            // Faltantes: se pide mas de lo que hay, se ve la lista y al ingresar el stock desaparece.
            Producto filtro = new CatalogoService().ObtenerPorSku("FILTRO-10");
            PresupuestoSolicitud pedidoGrande = new PresupuestoSolicitud { ClienteId = empresa.Id, TipoCambio = 1m, Enviar = true, PorcentajeAnticipo = 30m };
            pedidoGrande.Lineas.Add(new LineaPresupuestoSolicitud
            {
                ProductoId = filtro.Id, Tipo = TipoItemPresupuesto.Producto, Cantidad = 5000m, PrecioUnitario = filtro.PrecioBase
            });
            Presupuesto grande = presupuestos.CrearPresupuesto(pedidoGrande);
            FaltantesService faltantes = new FaltantesService();
            List<FaltanteDto> lista = faltantes.DePresupuesto(grande.Id);
            Verificador.Verificar(lista.Count == 1 && lista[0].Faltante > 0m && lista[0].Requerido == 5000m, "Se listan los productos que faltan para el presupuesto");
            Verificador.Verificar(faltantes.Generales().Any(x => x.ProductoId == filtro.Id), "La solapa de faltantes incluye el producto que falta");
            new PreparacionService().IngresarStock(lista[0].ProductoId, lista[0].Faltante, "Prueba de faltantes");
            Verificador.Igual(0, faltantes.DePresupuesto(grande.Id).Count, "Con el stock ingresado ya no falta nada");

            Verificador.Verificar(new LogisticaService().ObtenerPedidosParaDespacho().All(d => d.Pendiente > 0m),
                "La lista de despacho solo muestra lo que todavia falta enviar");
            Verificador.Verificar(new LogisticaService().ObtenerPedidosParaDespacho().All(d => d.FechaPedido > DateTime.MinValue),
                "La lista de despacho informa la fecha de cada pedido");
        }

        private static Cliente PrepararCliente()
        {
            ClienteService servicio = new ClienteService();
            Cliente cliente = servicio.ObtenerTodos().First(c => c.Activo && c.RazonSocial.Length > 5);

            cliente.LimiteCredito = 50000000m;
            cliente.Tipo = TipoPersona.Empresa;
            servicio.Modificar(cliente);

            Cliente guardado = servicio.ObtenerPorId(cliente.Id);
            Verificador.Igual(50000000m, guardado.LimiteCredito, "El límite de crédito se actualiza y la integridad se mantiene");

            return guardado;
        }

        private static Pedido FlujoComercial(Cliente cliente)
        {
            Verificador.Seccion("Presupuesto y confirmación del pedido");

            CatalogoService catalogo = new CatalogoService();
            InventarioService inventario = new InventarioService();
            PresupuestoService presupuestos = new PresupuestoService();

            List<ProductoListaDto> conStock = catalogo.BuscarProductos(string.Empty, null, TipoProducto.ProductoTerminado, true, true)
                .Where(p => p.Disponible >= 10m && p.PrecioBase > 0m)
                .Take(2).ToList();

            Verificador.Igual(2, conStock.Count, "Hay productos con stock para vender");

            Producto filtro = catalogo.ObtenerPorSku("FILTRO-10");
            int reservasAntes = inventario.ObtenerReservas(true).Count;

            PresupuestoSolicitud solicitud = new PresupuestoSolicitud
            {
                ClienteId = cliente.Id,
                TipoCambio = 1515m,
                PorcentajeAnticipo = 50m,
                CondicionPago = "50% anticipo, saldo contra entrega",
                PlazoEntrega = "20 días",
                Enviar = true
            };

            foreach (ProductoListaDto p in conStock)
            {
                solicitud.Lineas.Add(new LineaPresupuestoSolicitud
                {
                    ProductoId = p.Id,
                    Tipo = TipoItemPresupuesto.Producto,
                    Cantidad = 3m,
                    PrecioUnitario = p.PrecioBase
                });
            }

            solicitud.Lineas.Add(new LineaPresupuestoSolicitud
            {
                ProductoId = filtro.Id,
                Tipo = TipoItemPresupuesto.FabricacionEspecial,
                Cantidad = 2m,
                PrecioUnitario = filtro.PrecioBase,
                DescripcionTecnica = "Construcción sanitaria en acero 316L, configuración en L, conexión 226."
            });

            solicitud.Lineas.Add(new LineaPresupuestoSolicitud
            {
                Tipo = TipoItemPresupuesto.Servicio,
                Codigo = "SERV-PULIDO",
                Descripcion = "Pulido sanitario",
                Cantidad = 1m,
                PrecioUnitario = 90m
            });

            Presupuesto presupuesto = presupuestos.CrearPresupuesto(solicitud);

            Verificador.Verificar(presupuesto.Numero.StartsWith("PRE-"), "El presupuesto recibe número " + presupuesto.Numero);
            Verificador.Igual(EstadoPresupuesto.Enviado, presupuesto.Estado, "El presupuesto queda enviado");
            Verificador.Igual(7, (int)Math.Round((presupuesto.FechaVencimiento.Date - DateTime.Today).TotalDays), "La vigencia inicial es de 7 días");

            PedidoService pedidos = new PedidoService();

            Verificador.Falla<ReglaDeNegocioException>(
                () => pedidos.ConfirmarDesdePresupuesto(cliente.Id, presupuesto.Id),
                "No se puede confirmar un presupuesto que todavía no fue aceptado");

            presupuestos.CambiarEstado(presupuesto.Id, EstadoPresupuesto.Aceptado);

            Falla(() => presupuestos.CambiarEstado(presupuesto.Id, EstadoPresupuesto.Borrador),
                "Un presupuesto aceptado no vuelve a borrador");

            PresupuestoSolicitud sinSena = CopiarSolicitud(solicitud);
            sinSena.PorcentajeAnticipo = 10m;

            Verificador.Falla<ReglaDeNegocioException>(
                () => presupuestos.CrearPresupuesto(sinSena),
                "La fabricación a medida exige una seña mínima del 30%");

            PresupuestoSolicitud conDescuento = CopiarSolicitud(solicitud);
            conDescuento.PorcentajeDescuentoGeneral = 25m;
            SesionActual.Iniciar(UsuarioConRol(NombresRol.Vendedor));

            Verificador.Falla<AccesoNoAutorizadoException>(
                () => presupuestos.CrearPresupuesto(conDescuento),
                "Un descuento mayor al 10% requiere autorización del administrador");

            SesionActual.Iniciar(UsuarioConRol(NombresRol.Administrador));

            Presupuesto aceptado = presupuestos.ObtenerPorId(presupuesto.Id);

            Pedido pedido = pedidos.ConfirmarDesdePresupuesto(
                cliente.Id, presupuesto.Id, aceptado.CalcularAnticipo(), "Efectivo", "REC-0001", DateTime.Today);

            Verificador.Igual(EstadoPedido.Confirmado, pedido.Estado, "El pedido queda confirmado");
            Verificador.Igual(reservasAntes + 2, inventario.ObtenerReservas(true).Count, "Se reservó el stock de las dos líneas de producto");

            Verificador.Falla<ReglaDeNegocioException>(
                () => pedidos.ConfirmarDesdePresupuesto(cliente.Id, presupuesto.Id, aceptado.CalcularAnticipo(), "Efectivo", "REC-0002", DateTime.Today),
                "El presupuesto no se convierte dos veces en pedido");

            ProductoListaDto sinExistencias = catalogo.BuscarProductos(string.Empty, null, null, true, true)
                .FirstOrDefault(p => p.Disponible > 0m && p.Disponible < 3m);

            if (sinExistencias != null)
            {
                PresupuestoSolicitud grande = CopiarSolicitud(solicitud);
                grande.Lineas.Clear();
                grande.PorcentajeAnticipo = 30m;
                grande.Lineas.Add(new LineaPresupuestoSolicitud
                {
                    ProductoId = sinExistencias.Id,
                    Tipo = TipoItemPresupuesto.Producto,
                    Cantidad = sinExistencias.Disponible + 5m,
                    PrecioUnitario = sinExistencias.PrecioBase
                });

                Presupuesto excesivo = presupuestos.CrearPresupuesto(grande);
                presupuestos.CambiarEstado(excesivo.Id, EstadoPresupuesto.Aceptado);

                Verificador.Falla<ReglaDeNegocioException>(
                    () => pedidos.ConfirmarDesdePresupuesto(cliente.Id, excesivo.Id),
                    "No se confirma un pedido si no alcanza el stock");
            }

            return pedidos.ObtenerPorId(pedido.Id);
        }

        private static void Falla(Action accion, string descripcion)
        {
            Verificador.Falla<ReglaDeNegocioException>(accion, descripcion);
        }

        private static PresupuestoSolicitud CopiarSolicitud(PresupuestoSolicitud origen)
        {
            PresupuestoSolicitud copia = new PresupuestoSolicitud
            {
                ClienteId = origen.ClienteId,
                Moneda = origen.Moneda,
                TipoCambio = origen.TipoCambio,
                PorcentajeIva = origen.PorcentajeIva,
                PorcentajeAnticipo = origen.PorcentajeAnticipo,
                CondicionPago = origen.CondicionPago,
                PlazoEntrega = origen.PlazoEntrega,
                Enviar = true
            };

            copia.Lineas.AddRange(origen.Lineas);
            return copia;
        }

        // ---------------------------------------------------------------
        // Producción
        // ---------------------------------------------------------------

        private static void FlujoProduccion(Pedido pedido)
        {
            Verificador.Seccion("Producción y control de calidad");

            ProduccionService produccion = new ProduccionService();
            CatalogoService catalogo = new CatalogoService();
            InventarioService inventario = new InventarioService();

            PedidoPendienteFabricacionDto pendiente = produccion.ObtenerPedidosPendientes()
                .FirstOrDefault(p => p.PedidoId == pedido.Id);

            Verificador.Verificar(pendiente != null, "El pedido figura entre los pendientes de fabricación");

            Producto lamina = catalogo.ObtenerPorSku("MP-316L-3.0");
            Producto varilla = catalogo.ObtenerPorSku("IN-TIG-308L");
            decimal stockLaminaAntes = inventario.ConsultarStock("MP-316L-3.0", null, false).Sum(s => s.Fisica);

            Verificador.Falla<ReglaDeNegocioException>(
                () => produccion.GenerarOrden(pedido.Id, pendiente.DetallePedidoId, PrioridadOrden.Normal, null,
                    new List<MaterialRequerido>(), string.Empty, string.Empty),
                "No se genera una orden sin materiales");

            ResultadoGeneracionOrden generada = produccion.GenerarOrden(
                pedido.Id, pendiente.DetallePedidoId, PrioridadOrden.Urgente, DateTime.Today.AddDays(15),
                new List<MaterialRequerido>
                {
                    new MaterialRequerido { ProductoId = lamina.Id, Cantidad = 6m },
                    new MaterialRequerido { ProductoId = varilla.Id, Cantidad = 2m }
                },
                "plano-filtro-10.pdf", "Prioridad del cliente");

            OrdenFabricacion orden = generada.Orden;

            Verificador.Verificar(orden.Numero.StartsWith("OF-"), "La orden recibe número " + orden.Numero);
            Verificador.Igual(EstadoProduccion.Pendiente, orden.Estado, "La orden nace pendiente");
            Verificador.Igual(0, generada.Faltantes.Count, "Los materiales quedaron reservados");

            Verificador.Falla<ReglaDeNegocioException>(
                () => produccion.GenerarOrden(pedido.Id, pendiente.DetallePedidoId, PrioridadOrden.Normal, null,
                    new List<MaterialRequerido> { new MaterialRequerido { ProductoId = lamina.Id, Cantidad = 1m } },
                    string.Empty, string.Empty),
                "Una línea no puede tener dos órdenes activas");

            Verificador.Falla<ReglaDeNegocioException>(
                () => produccion.RegistrarAvance(orden.Id, "Corte", 10m, string.Empty),
                "No se registra avance antes de iniciar");

            Verificador.Falla<ReglaDeNegocioException>(
                () => produccion.IniciarProduccion(orden.Id), "Sin la factura que asienta la sena no se manda a producir");
            new FacturacionService().EmitirFactura(pedido.Id, TipoComprobante.FacturaA);

            produccion.IniciarProduccion(orden.Id);
            Verificador.Falla<ReglaDeNegocioException>(
                () => produccion.IniciarProduccion(orden.Id), "Una orden iniciada no puede iniciarse de nuevo");

            Verificador.Igual(EstadoPedido.EnProduccion, FactoryDataAccess.PedidoRepository.GetById(pedido.Id).Estado,
                "El pedido pasa a producción");

            produccion.RegistrarAvance(orden.Id, "Corte", 40m, string.Empty);
            Verificador.Falla<ReglaDeNegocioException>(
                () => produccion.RegistrarAvance(orden.Id, "Plegado", 20m, string.Empty),
                "El avance no puede retroceder");

            List<MaterialOrdenDto> materiales = produccion.ObtenerMateriales(orden.Id);
            MaterialOrdenDto chapa = materiales.First(m => m.Sku == "MP-316L-3.0");

            Verificador.Falla<ReglaDeNegocioException>(
                () => produccion.RegistrarConsumo(orden.Id, chapa.Id, 50m, 0m, string.Empty),
                "No se consume más de lo reservado");

            produccion.RegistrarConsumo(orden.Id, chapa.Id, 5m, 0.5m, "Corte de la envolvente");

            decimal stockLaminaDespues = inventario.ConsultarStock("MP-316L-3.0", null, false).Sum(s => s.Fisica);
            Verificador.Igual(stockLaminaAntes - 5.5m, stockLaminaDespues, "El consumo y la merma bajan el stock físico");

            Verificador.Falla<ReglaDeNegocioException>(
                () => produccion.EnviarACalidad(orden.Id), "Una orden al 40% no se envia a calidad");
            Verificador.Igual(EstadoProduccion.EnProceso, produccion.ObtenerOrden(orden.Id).Estado, "La orden incompleta sigue en proceso");
            Verificador.Igual(5m, produccion.ObtenerMateriales(orden.Id).First(m => m.Sku == "MP-316L-3.0").Consumida,
                "Enviar una orden incompleta no consume materiales");

            produccion.RegistrarAvance(orden.Id, "Soldadura", 100m, string.Empty);
            produccion.EnviarACalidad(orden.Id);

            Verificador.Falla<ReglaDeNegocioException>(
                () => produccion.RegistrarControlCalidad(orden.Id, ResultadoCalidad.Rechazado, string.Empty),
                "Un rechazo debe explicar el motivo");

            produccion.RegistrarControlCalidad(orden.Id, ResultadoCalidad.Reproceso, "Soldadura con poros");
            Verificador.Igual(EstadoProduccion.EnProceso, produccion.ObtenerOrden(orden.Id).Estado, "El reproceso devuelve la orden a proceso");

            produccion.EnviarACalidad(orden.Id);
            produccion.RegistrarControlCalidad(orden.Id, ResultadoCalidad.Aprobado, string.Empty);

            OrdenFabricacion terminada = produccion.ObtenerOrden(orden.Id);
            Verificador.Igual(EstadoProduccion.Finalizada, terminada.Estado, "La orden queda finalizada con control aprobado");
            Verificador.Verificar(terminada.FechaFinReal.HasValue, "Se registra la fecha real de fin");

            StockDto terminado = inventario.ConsultarStock("FILTRO-10", null, false).FirstOrDefault();
            Verificador.Verificar(terminado != null && terminado.Fisica == 2m && terminado.Reservada == 2m,
                "El producto terminado ingresa al stock y queda reservado para el pedido");

            Verificador.Igual(EstadoPedido.ListoParaEntrega, FactoryDataAccess.PedidoRepository.GetById(pedido.Id).Estado,
                "El pedido queda listo para entrega");

            Verificador.Falla<ReglaDeNegocioException>(
                () => produccion.CancelarOrden(orden.Id, "ya no hace falta"),
                "Una orden finalizada no se cancela");
        }

        // ---------------------------------------------------------------
        // Finanzas
        // ---------------------------------------------------------------

        private static Factura FlujoFinanzas(Pedido pedido, Cliente cliente)
        {
            Verificador.Seccion("Facturación, cobro y cuenta corriente");

            FacturacionService facturacion = new FacturacionService();
            FinanzasService finanzas = new FinanzasService();

            Verificador.Falla<ReglaDeNegocioException>(
                () => facturacion.EmitirFactura(Guid.NewGuid(), TipoComprobante.FacturaB), "No se factura un pedido inexistente");

            ClienteService clientesFin = new ClienteService();
            Cliente cambiante = clientesFin.ObtenerPorId(cliente.Id);
            cambiante.Tipo = TipoPersona.Particular;
            clientesFin.Modificar(cambiante);
            Verificador.Falla<ReglaDeNegocioException>(
                () => facturacion.EmitirFactura(pedido.Id, TipoComprobante.FacturaA), "Un particular (CUIL) no recibe factura A");
            cambiante.Tipo = TipoPersona.Empresa;
            clientesFin.Modificar(cambiante);

            Factura factura = facturacion.ObtenerFacturaDePedido(pedido.Id);

            Verificador.Verificar(System.Text.RegularExpressions.Regex.IsMatch(factura.Numero, @"^\d{4}-\d{8}$"), "Numeración fiscal " + factura.Numero);
            Verificador.Verificar(factura.MontoTotal > 0m && factura.Iva > 0m, "La factura A discrimina el IVA");
            Verificador.Igual(Math.Round(pedido.CalcularTotalEnPesos(), 2), factura.MontoTotal, "El total facturado coincide con el pedido en pesos");
            Verificador.Verificar(factura.SaldoPendiente < factura.MontoTotal && factura.SaldoPendiente > 0m, "La seña se descuenta del saldo");
            Verificador.Igual(EstadoPago.Parcial, factura.EstadoPago, "La factura queda con pago parcial");

            Verificador.Falla<ReglaDeNegocioException>(
                () => facturacion.EmitirFactura(pedido.Id, TipoComprobante.FacturaB), "Un pedido no se factura dos veces");

            Cliente conDeuda = new ClienteService().ObtenerPorId(cliente.Id);
            Verificador.Igual(factura.SaldoPendiente, conDeuda.DeudaActual, "La deuda del cliente es el saldo de la factura");

            List<MovimientoCuentaDto> cuenta = finanzas.ObtenerCuentaCliente(cliente.Id, null, null);
            Verificador.Verificar(cuenta.Count >= 2, "La cuenta corriente registra factura y seña");
            Verificador.Igual(factura.SaldoPendiente, cuenta.Last().Saldo, "El saldo de la cuenta corriente coincide");

            MedioPago efectivo = finanzas.ObtenerMediosPago(true).First(m => m.Codigo == "EFECTIVO");
            MedioPago transferencia = finanzas.ObtenerMediosPago(true).First(m => m.Codigo == "TRANSFERENCIA");

            Verificador.Falla<ReglaDeNegocioException>(
                () => finanzas.RegistrarCobro(cliente.Id, factura.Id, efectivo.Id, factura.SaldoPendiente + 1m, string.Empty, string.Empty, string.Empty),
                "No se cobra más que el saldo de la factura");

            Verificador.Falla<ReglaDeNegocioException>(
                () => finanzas.RegistrarCobro(cliente.Id, factura.Id, transferencia.Id, 100m, string.Empty, string.Empty, string.Empty),
                "La transferencia exige referencia y entidad");

            decimal saldoInicial = factura.SaldoPendiente;
            decimal parcial = Math.Round(saldoInicial / 2m, 2);
            finanzas.RegistrarCobro(cliente.Id, factura.Id, transferencia.Id, parcial, "TRF-123", "Banco Ejemplo", "Primer pago");
            finanzas.RegistrarCobro(cliente.Id, factura.Id, efectivo.Id, saldoInicial - parcial, string.Empty, string.Empty, "Saldo");

            Factura cobrada = facturacion.ObtenerFactura(factura.Id);
            Verificador.Igual(EstadoPago.Pagada, cobrada.EstadoPago, "La factura queda pagada");
            Verificador.Igual(0m, cobrada.SaldoPendiente, "El saldo de la factura es cero");
            Verificador.Igual(0m, new ClienteService().ObtenerPorId(cliente.Id).DeudaActual, "La deuda del cliente vuelve a cero");

            ResumenCuentaClienteDto resumen = finanzas.ObtenerResumenCliente(cliente.Id);
            Verificador.Igual(0, resumen.FacturasPendientes, "No quedan facturas pendientes");

            return cobrada;
        }

        // ---------------------------------------------------------------
        // Logística
        // ---------------------------------------------------------------

        private static RemitoEntrega FlujoLogistica(Pedido pedido)
        {
            Verificador.Seccion("Remito, hoja de ruta y entrega");

            LogisticaService logistica = new LogisticaService();
            InventarioService inventario = new InventarioService();

            PedidoDespachoDto despacho = logistica.ObtenerPedidosParaDespacho().First(p => p.PedidoId == pedido.Id);
            Verificador.Verificar(despacho.Habilitado, "El pedido está habilitado para despacho: " + despacho.Motivo);

            List<LineaPendienteRemitoDto> lineas = logistica.ObtenerLineasPendientes(pedido.Id);
            Verificador.Verificar(lineas.Count >= 3 && lineas.All(l => l.ListaParaDespacho == l.Pendiente), "Todas las líneas físicas están listas");

            decimal reservadoAntes = inventario.ObtenerReservas(true).Sum(r => r.Pendiente);

            RemitoEntrega remito = logistica.GenerarRemito(
                pedido.Id,
                lineas.Select(l => new LineaRemitoSolicitud { DetallePedidoId = l.DetallePedidoId, Cantidad = l.Pendiente }).ToList(),
                "Entregar en horario comercial");

            Verificador.Verificar(remito.Detalles.Count == lineas.Count, "El remito incluye " + remito.Detalles.Count + " líneas");
            Verificador.Igual(0m, logistica.ObtenerLineasPendientes(pedido.Id).Sum(l => l.Pendiente), "No queda nada pendiente de remitir");

            Verificador.Falla<ReglaDeNegocioException>(
                () => logistica.GenerarRemito(pedido.Id,
                    new List<LineaRemitoSolicitud> { new LineaRemitoSolicitud { DetallePedidoId = lineas[0].DetallePedidoId, Cantidad = 1m } }, string.Empty),
                "No se remita más de lo pendiente");

            Vehiculo chico = logistica.ObtenerVehiculos(true).First(v => v.CapacidadKg <= 2000m);
            Chofer camionero = logistica.ObtenerChoferes(true).First(c => c.CategoriaLicencia.StartsWith("B"));
            Chofer pesado = logistica.ObtenerChoferes(true).First(c => c.CategoriaLicencia.StartsWith("C"));

            Verificador.Verificar(LogisticaService.CategoriaHabilita("B2", 1800m), "La categoría B habilita un vehículo liviano");
            Verificador.Verificar(!LogisticaService.CategoriaHabilita("B2", 9000m), "La categoría B no habilita un camión");

            Vehiculo camion = logistica.ObtenerVehiculos(true).First(v => v.CapacidadKg > 3500m);
            Verificador.Falla<ReglaDeNegocioException>(
                () => logistica.CrearHojaRuta(camion.Id, camionero.Id, DateTime.Today, new List<Guid> { remito.Id }, string.Empty),
                "El chofer con licencia B no puede conducir el camión");

            Verificador.Falla<ReglaDeNegocioException>(
                () => logistica.CrearHojaRuta(chico.Id, pesado.Id, DateTime.Today.AddDays(-1), new List<Guid> { remito.Id }, string.Empty),
                "La fecha planificada no puede estar en el pasado");

            HojaRuta hoja = logistica.CrearHojaRuta(chico.Id, pesado.Id, DateTime.Today, new List<Guid> { remito.Id }, string.Empty);
            Verificador.Igual(EstadoRemito.Asignado, logistica.ObtenerRemito(remito.Id).Estado, "El remito queda asignado a la hoja");

            Verificador.Falla<ReglaDeNegocioException>(
                () => logistica.CrearHojaRuta(chico.Id, pesado.Id, DateTime.Today, new List<Guid> { remito.Id }, string.Empty),
                "Un remito asignado no se vuelve a incluir en otra hoja");

            logistica.IniciarHojaRuta(hoja.Id, 12000m);

            Verificador.Igual(EstadoRemito.EnTransito, logistica.ObtenerRemito(remito.Id).Estado, "El remito sale en tránsito");
            Verificador.Igual(EstadoPedido.EnDistribucion, FactoryDataAccess.PedidoRepository.GetById(pedido.Id).Estado, "El pedido pasa a distribución");
            Verificador.Verificar(inventario.ObtenerReservas(true).Sum(r => r.Pendiente) <= reservadoAntes - lineas.Sum(l => l.Pendiente) + 0.001m,
                "El despacho consume las reservas del pedido");

            Verificador.Falla<ReglaDeNegocioException>(
                () => logistica.IniciarHojaRuta(hoja.Id, 12000m), "Una hoja iniciada no se inicia de nuevo");

            List<ParadaDto> paradas = logistica.ObtenerParadas(hoja.Id);

            Verificador.Falla<ReglaDeNegocioException>(
                () => logistica.RegistrarEntrega(paradas[0].DetalleId, ResultadoEntrega.EntregadoConforme, string.Empty, string.Empty, string.Empty, string.Empty, null),
                "La entrega conforme exige los datos de quien recibe");

            logistica.RegistrarEntrega(paradas[0].DetalleId, ResultadoEntrega.EntregadoConforme,
                "Ana Receptora", "30111222", string.Empty, "Sin novedades", 12040m);

            Verificador.Igual(EstadoRemito.Entregado, logistica.ObtenerRemito(remito.Id).Estado, "El remito queda entregado");
            Verificador.Igual(EstadoHojaRuta.Finalizada, logistica.ObtenerHoja(hoja.Id).Estado, "La hoja de ruta se finaliza");
            Verificador.Igual(EstadoPedido.Entregado, FactoryDataAccess.PedidoRepository.GetById(pedido.Id).Estado, "El pedido queda entregado");

            Verificador.Falla<ReglaDeNegocioException>(
                () => logistica.RegistrarEntrega(paradas[0].DetalleId, ResultadoEntrega.EntregadoConforme, "X", "1", string.Empty, string.Empty, null),
                "No se registra dos veces la misma entrega");

            return logistica.ObtenerRemito(remito.Id);
        }

        // ---------------------------------------------------------------
        // Devoluciones
        // ---------------------------------------------------------------

        private static void FlujoDevolucion(RemitoEntrega remito, Factura factura, Cliente cliente)
        {
            Verificador.Seccion("Devolución, reingreso y nota de crédito");

            DevolucionService devoluciones = new DevolucionService();
            InventarioService inventario = new InventarioService();

            DetalleRemito primera = remito.Detalles.First(d => d.Codigo != "FILTRO-10");
            decimal stockAntes = inventario.ConsultarStock(primera.Codigo, null, false).Sum(s => s.Fisica);

            Verificador.Falla<ReglaDeNegocioException>(
                () => devoluciones.RegistrarDevolucion(remito.Id, "Defecto",
                    new List<LineaDevolucionSolicitud> { new LineaDevolucionSolicitud { DetalleRemitoId = primera.Id, Cantidad = primera.Cantidad + 1m } }),
                "No se devuelve más de lo entregado");

            DetalleRemito aMedida = remito.Detalles.FirstOrDefault(d => d.Codigo == "FILTRO-10");

            if (aMedida != null)
            {
                Verificador.Falla<ReglaDeNegocioException>(
                    () => devoluciones.RegistrarDevolucion(remito.Id, "Defecto",
                        new List<LineaDevolucionSolicitud> { new LineaDevolucionSolicitud { DetalleRemitoId = aMedida.Id, Cantidad = 1m } }),
                    "Lo fabricado a medida no admite devolución");
            }

            Devolucion devolucion = devoluciones.RegistrarDevolucion(remito.Id, "Producto con rayaduras",
                new List<LineaDevolucionSolicitud> { new LineaDevolucionSolicitud { DetalleRemitoId = primera.Id, Cantidad = 1m, Motivo = "Rayado" } });

            Verificador.Igual(EstadoDevolucion.Solicitada, devolucion.Estado, "La devolución nace solicitada");

            Verificador.Falla<ReglaDeNegocioException>(
                () => devoluciones.ResolverDetalle(new ResolucionDevolucion
                {
                    DetalleDevolucionId = devolucion.Detalles[0].Id,
                    Accion = AccionResuelta.ReingresoStock,
                    AptoReingreso = true,
                    CantidadReingresada = 1m
                }),
                "No se inspecciona antes de recibir la devolución");

            devoluciones.RecibirDevolucion(devolucion.Id);

            Verificador.Falla<ReglaDeNegocioException>(
                () => devoluciones.ResolverDetalle(new ResolucionDevolucion
                {
                    DetalleDevolucionId = devolucion.Detalles[0].Id,
                    Accion = AccionResuelta.ReingresoStock,
                    AptoReingreso = false,
                    CantidadReingresada = 1m
                }),
                "Solo reingresa al stock lo aprobado en la inspección");

            devoluciones.ResolverDetalle(new ResolucionDevolucion
            {
                DetalleDevolucionId = devolucion.Detalles[0].Id,
                Accion = AccionResuelta.ReingresoStock,
                AptoReingreso = true,
                CantidadReingresada = 1m,
                Condicion = "Rayadura superficial",
                Observaciones = "Apto para la venta"
            });

            Verificador.Igual(stockAntes + 1m, inventario.ConsultarStock(primera.Codigo, null, false).Sum(s => s.Fisica),
                "El producto aprobado vuelve al stock");

            Verificador.Igual(EstadoDevolucion.Resuelta, devoluciones.ObtenerDevoluciones(null).First(d => d.Id == devolucion.Id).EstadoClave,
                "La devolución queda resuelta");

            NotaCreditoSugerida sugerida = devoluciones.CalcularNotaCreditoSugerida(devolucion.Id);
            Verificador.Verificar(sugerida.MontoNeto > 0m, "Se calcula el importe de la nota de crédito: " + sugerida.MontoNeto.ToString("N2"));

            FacturacionService facturacion = new FacturacionService();

            Verificador.Falla<ReglaDeNegocioException>(
                () => facturacion.EmitirNotaCredito(factura.Id, devolucion.Id, factura.MontoNeto * 2m, "Exceso"),
                "La nota de crédito no puede superar la factura");

            NotaCredito nota = facturacion.EmitirNotaCredito(sugerida.FacturaId, devolucion.Id, sugerida.MontoNeto, "Devolución de mercadería");

            Verificador.Verificar(nota.MontoTotal > nota.MontoNeto, "La nota de crédito incluye el IVA");
            Verificador.Igual(1, facturacion.ObtenerNotasCredito(factura.Id).Count, "La nota queda asociada a la factura");

            Verificador.Falla<ReglaDeNegocioException>(
                () => facturacion.EmitirNotaCredito(factura.Id, Guid.NewGuid(), 10m, "Sin devolución"),
                "Sin una devolución resuelta no hay nota de crédito");
        }

        // ---------------------------------------------------------------
        // Compras
        // ---------------------------------------------------------------

        private static void FlujoCompras()
        {
            Verificador.Seccion("Compras, recepción y pago a proveedor");

            ComprasService compras = new ComprasService();
            FinanzasService finanzas = new FinanzasService();
            InventarioService inventario = new InventarioService();
            CatalogoService catalogo = new CatalogoService();

            Producto chapa = catalogo.ObtenerPorSku("MP-304-2.0");
            Proveedor proveedor = compras.ObtenerProveedores(string.Empty, true).First();
            decimal costoAntes = catalogo.ObtenerProducto(chapa.Id).CostoActual;

            Verificador.Falla<ReglaDeNegocioException>(
                () => compras.CrearOrden(proveedor.Id, DateTime.Today.AddDays(-2), string.Empty, string.Empty,
                    new List<ItemCompra> { new ItemCompra { ProductoId = chapa.Id, Cantidad = 5m, Costo = 40m } }, true),
                "La entrega estimada no puede ser anterior a la emisión");

            Verificador.Falla<ReglaDeNegocioException>(
                () => compras.CrearOrden(proveedor.Id, null, string.Empty, string.Empty, new List<ItemCompra>(), true),
                "La orden de compra necesita al menos un ítem");

            OrdenCompra orden = compras.CrearOrden(proveedor.Id, DateTime.Today.AddDays(7), "30 días", "Reposición",
                new List<ItemCompra> { new ItemCompra { ProductoId = chapa.Id, Cantidad = 50m, Costo = 44m, Descuento = 5m } }, true);

            Verificador.Verificar(orden.Numero.StartsWith("OC-"), "La orden recibe número " + orden.Numero);
            Verificador.Igual(EstadoOrdenCompra.Emitida, orden.Estado, "La orden queda emitida");
            Verificador.Igual(orden.MontoTotal, orden.SaldoPendiente, "El saldo pendiente inicial es el total");

            Proveedor conSaldo = compras.ObtenerProveedores(string.Empty, true).First(p => p.Id == proveedor.Id);
            Verificador.Igual(orden.MontoTotal, conSaldo.SaldoCuenta, "El proveedor acumula la deuda de la orden");

            DetalleOrdenCompraDto detalle = compras.ObtenerDetalles(orden.Id).First();

            Verificador.Falla<ReglaDeNegocioException>(
                () => compras.RegistrarRecepcion(orden.Id, "R-0001",
                    new List<RecepcionItem> { new RecepcionItem { DetalleOrdenCompraId = detalle.DetalleId, Recibida = 50m, Aceptada = 40m, Rechazada = 5m } }, string.Empty),
                "Lo recibido debe ser igual a lo aceptado más lo rechazado");

            Verificador.Falla<ReglaDeNegocioException>(
                () => compras.RegistrarRecepcion(orden.Id, "R-0001",
                    new List<RecepcionItem> { new RecepcionItem { DetalleOrdenCompraId = detalle.DetalleId, Recibida = 50m, Aceptada = 45m, Rechazada = 5m } }, string.Empty),
                "El rechazo exige el motivo");

            decimal stockAntes = inventario.ConsultarStock("MP-304-2.0", null, false).Sum(s => s.Fisica);

            compras.RegistrarRecepcion(orden.Id, "R-0001",
                new List<RecepcionItem>
                {
                    new RecepcionItem
                    {
                        DetalleOrdenCompraId = detalle.DetalleId, Recibida = 50m, Aceptada = 45m, Rechazada = 5m,
                        MotivoRechazo = "Chapas con corrosión", Lote = "L-2026-01", Colada = "CERT-8891"
                    }
                }, "Recepción parcial");

            Verificador.Igual(stockAntes + 45m, inventario.ConsultarStock("MP-304-2.0", null, false).Sum(s => s.Fisica),
                "Solo lo aceptado ingresa al stock");

            OrdenCompraDto estado = compras.ObtenerOrdenes(proveedor.Id, null).First(o => o.Id == orden.Id);
            Verificador.Verificar(estado.Estado.Length > 0 && compras.ObtenerOrden(orden.Id).Estado == EstadoOrdenCompra.ParcialmenteRecibida,
                "La orden queda parcialmente recibida");

            Verificador.Verificar(catalogo.ObtenerProducto(chapa.Id).CostoActual != costoAntes, "El costo promedio ponderado se actualiza");

            compras.RegistrarRecepcion(orden.Id, "R-0002",
                new List<RecepcionItem> { new RecepcionItem { DetalleOrdenCompraId = detalle.DetalleId, Recibida = 5m, Aceptada = 5m } }, string.Empty);

            Verificador.Igual(EstadoOrdenCompra.Recibida, compras.ObtenerOrden(orden.Id).Estado, "La orden queda recibida al completar");

            Verificador.Falla<ReglaDeNegocioException>(
                () => compras.CancelarOrden(orden.Id, "Error"), "Una orden con material recibido no se cancela");

            MedioPago transferencia = finanzas.ObtenerMediosPago(true).First(m => m.Codigo == "TRANSFERENCIA");

            Verificador.Falla<ReglaDeNegocioException>(
                () => finanzas.RegistrarPagoProveedor(proveedor.Id, orden.Id, transferencia.Id, orden.MontoTotal + 1m, "TRF-9", "Banco", string.Empty),
                "No se paga más que el saldo de la orden");

            finanzas.RegistrarPagoProveedor(proveedor.Id, orden.Id, transferencia.Id, 1000m, "TRF-9", "Banco Ejemplo", "Anticipo");

            Verificador.Igual(orden.MontoTotal - 1000m, compras.ObtenerOrden(orden.Id).SaldoPendiente, "El pago reduce el saldo de la orden");
            Verificador.Igual(2, finanzas.ObtenerCuentaProveedor(proveedor.Id, null, null).Count, "La cuenta del proveedor registra orden y pago");

            List<SugerenciaCompraDto> sugerencias = compras.SugerirCompras();
            Console.WriteLine("  (sugerencias de compra: " + sugerencias.Count + ")");

            OrdenCompra borrador = compras.CrearOrden(proveedor.Id, null, string.Empty, string.Empty,
                new List<ItemCompra> { new ItemCompra { ProductoId = chapa.Id, Cantidad = 1m, Costo = 40m } }, false);
            compras.CancelarOrden(borrador.Id, "Prueba de cancelación");
            Verificador.Igual(EstadoOrdenCompra.Cancelada, compras.ObtenerOrden(borrador.Id).Estado, "Un borrador puede cancelarse");
        }

        // ---------------------------------------------------------------
        // Reglas generales
        // ---------------------------------------------------------------

        private static void ReglasNegativas(Pedido pedido, Factura factura)
        {
            Verificador.Seccion("Inventario y cancelaciones");

            InventarioService inventario = new InventarioService();
            CatalogoService catalogo = new CatalogoService();
            Producto producto = catalogo.ObtenerPorSku("IN-DISCO-PULIDO");
            Guid ubicacionId = inventario.ConsultarStock("IN-DISCO-PULIDO", null, false).First().UbicacionId;

            Verificador.Falla<ReglaDeNegocioException>(
                () => inventario.RegistrarMovimiento(new MovimientoSolicitud
                {
                    ProductoId = producto.Id, UbicacionId = ubicacionId,
                    Tipo = TipoMovimientoStock.AjusteNegativo, Cantidad = 100000m, Observaciones = "Prueba"
                }),
                "No se puede dejar el stock en negativo");

            Verificador.Falla<ReglaDeNegocioException>(
                () => inventario.RegistrarMovimiento(new MovimientoSolicitud
                {
                    ProductoId = producto.Id, UbicacionId = ubicacionId,
                    Tipo = TipoMovimientoStock.AjustePositivo, Cantidad = 1m
                }),
                "Un ajuste debe indicar el motivo");

            decimal antes = inventario.ConsultarStock("IN-DISCO-PULIDO", null, false).Sum(s => s.Fisica);

            inventario.RegistrarMovimiento(new MovimientoSolicitud
            {
                ProductoId = producto.Id, UbicacionId = ubicacionId,
                Tipo = TipoMovimientoStock.AjusteNegativo, Cantidad = 10m, Observaciones = "Rotura"
            });

            Verificador.Igual(antes - 10m, inventario.ConsultarStock("IN-DISCO-PULIDO", null, false).Sum(s => s.Fisica), "El ajuste negativo baja el stock");

            List<UbicacionDto> ubicaciones = inventario.ObtenerUbicaciones(true);
            inventario.Transferir(producto.Id, ubicacionId, ubicaciones.First(u => u.Id != ubicacionId).Id, 5m, "Reubicación");
            Verificador.Igual(antes - 10m, inventario.ConsultarStock("IN-DISCO-PULIDO", null, false).Sum(s => s.Fisica), "La transferencia no cambia el total");

            Verificador.Falla<ReglaDeNegocioException>(
                () => new PedidoService().Cancelar(pedido.Id, "Prueba"),
                "Un pedido entregado no se cancela");

            int alertas = inventario.ObtenerAlertas().Count;
            Console.WriteLine("  (alertas de reposición: " + alertas + ")");

            int afectados = catalogo.AumentoMasivoPrecios(null, 10m, "Prueba");
            Verificador.Verificar(afectados > 100, "El aumento masivo alcanzó " + afectados + " productos");
            catalogo.AumentoMasivoPrecios(null, -9.09m, "Reversión de la prueba");

            Verificador.Falla<ReglaDeNegocioException>(
                () => catalogo.AumentoMasivoPrecios(null, 0m, "Sin cambio"), "Un aumento nulo se rechaza");
        }

        private static void Auditoria()
        {
            Verificador.Seccion("Bitácora");

            BitacoraService bitacora = new BitacoraService();
            int total = bitacora.ObtenerTodos().Count;

            Verificador.Verificar(total >= 30, "La bitácora registró " + total + " eventos");
            Verificador.Verificar(bitacora.ObtenerTodos().Any(r => r.Modulo == "Producción"), "Hay eventos del módulo Producción");
            Verificador.Verificar(bitacora.ObtenerTodos().Any(r => r.Resultado == "Fallido"), "Se registran los intentos denegados");
        }
    }
}
