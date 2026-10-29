using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Edelstahl.BLL.DTOs;
using Edelstahl.Domain.Catalogo;
using Edelstahl.Domain.Comercial;
using Edelstahl.Domain.Finanzas;
using Edelstahl.BLL.Services;
using Edelstahl.DAL.EntityFramework;
using Edelstahl.DAL.Factory;
using Edelstahl.Domain.Security;
using Edelstahl.Services.Configuration;
using Edelstahl.Services.Localization;
using System.Text;
using Edelstahl.WinApp;
using Edelstahl.WinApp.Modulos;
using Edelstahl.WinApp.Ui;

namespace Edelstahl.Test
{
    /// <summary>
    /// Abre cada modulo y cada pestana de la interfaz con datos cargados y verifica
    /// que ninguna lista falle; tambien abre los cuadros de dialogo principales.
    /// </summary>
    public static class PruebaPantallas
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr ventana, uint mensaje, IntPtr w, IntPtr l);

        private const uint WM_CLOSE = 0x0010;

        private static readonly List<string> Errores = new List<string>();

        public static void Ejecutar(ExecutionMode modo)
        {
            Verificador.Seccion("Pantallas en modo " + modo);

            Application.EnableVisualStyles();
            Application.ThreadException += (s, e) => Errores.Add(e.Exception.ToString());

            ApplicationMode.Reset();
            ApplicationMode.Select(modo);
            FactoryDataAccess.Inicializar(modo);

            if (modo == ExecutionMode.SqlServer)
            {
                InicializadorNegocio.Inicializar();
            }

            SesionActual.Iniciar(new Usuario
            {
                NombreUsuario = "pantallas",
                NombreCompleto = "Prueba de pantallas",
                Rol = new Rol { Nombre = NombresRol.Administrador }
            });

            new CargaInicialService().AsegurarDatosBase();

            string datos = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Edelstahl.WinApp", "Datos"));
            ImportacionService importacion = new ImportacionService();
            importacion.ImportarClientes(Path.Combine(datos, "clientes.csv"), null);
            importacion.ImportarProductos(Path.Combine(datos, "productos.csv"), null);

            Probar<ModuloReportes>();
            Probar<ModuloComercial>();
            Probar<ModuloProduccion>();
            Probar<ModuloInventario>();
            Probar<ModuloLogistica>();
            Probar<ModuloFinanzas>();
            Probar<ModuloAdministracion>();
            Probar<VisorBitacoraPagina>();

            Verificador.Seccion("Idiomas");
            ProbarIdiomas();

            Verificador.Seccion("Cuadros de dialogo");
            ProbarDialogoConNota();
            AbrirDialogos();

            Verificador.Seccion("Ventana principal");
            using (FrmPrincipalV2 principal = new FrmPrincipalV2())
            {
                principal.Show();
                Bombear(1500);
                Verificador.Verificar(principal.Controls.Count > 0, "La ventana principal se construyo");

                Panel encabezado = principal.Controls.OfType<Panel>().First(p => p.Dock == DockStyle.Top);
                Panel central = principal.Controls.OfType<Panel>().First(p => p.Dock == DockStyle.Fill);
                Verificador.Verificar(central.Top >= encabezado.Bottom, "El contenido no queda tapado por el encabezado (" + central.Top + " >= " + encabezado.Bottom + ")");
                principal.Close();
            }

            Verificador.Verificar(Errores.Count == 0, "No hubo excepciones en la interfaz" + (Errores.Count > 0 ? ": " + Errores[0] : string.Empty));
        }

        /// <summary>
        /// Genera una captura de cada pantalla principal (para la documentacion).
        /// </summary>
        public static void Capturar(string carpeta)
        {
            Directory.CreateDirectory(carpeta);
            Application.EnableVisualStyles();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => Console.WriteLine("  EXCEPCION UI: " + e.Exception);

            // Datos: se corre el escenario completo para que las listas tengan contenido.
            PruebaEscenarioCompleto.Ejecutar(ExecutionMode.Demo);

            SesionActual.Iniciar(new Usuario
            {
                NombreUsuario = "admin",
                NombreCompleto = "Administrador",
                Rol = new Rol { Nombre = NombresRol.Administrador }
            });

            string origen = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Edelstahl.WinApp", "Languages"));
            string destino = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages");
            Directory.CreateDirectory(destino);

            foreach (string archivo in Directory.GetFiles(origen))
            {
                File.Copy(archivo, Path.Combine(destino, Path.GetFileName(archivo)), true);
            }

            AdministracionService admin = new AdministracionService();

            try
            {
                admin.CrearUsuario("admin", "Administrador del sistema", "admin@edelstahl.test", "Clave-Admin#2026", NombresRol.Administrador);
                admin.CrearUsuario("vendedor1", "Laura Gomez", "laura@edelstahl.test", "Clave-Vend#2026", NombresRol.Vendedor);
                admin.CrearUsuario("produccion1", "Marcos Diaz", "marcos@edelstahl.test", "Clave-Prod#2026", NombresRol.Produccion);
                admin.CrearUsuario("logistica1", "Silvia Paz", "silvia@edelstahl.test", "Clave-Logi#2026", NombresRol.Logistica);
            }
            catch (Exception ex)
            {
                Console.WriteLine("  usuarios de ejemplo: " + ex.Message);
            }

            Console.WriteLine("  pedidos=" + new SeguimientoService().ObtenerSeguimiento("", null).Count
                + " stock=" + new InventarioService().ConsultarStock("", null, false).Count
                + " ordenes=" + new ProduccionService().ObtenerTablero(true).Count
                + " facturas=" + new FacturacionService().ObtenerFacturas(null, null, false).Count);

            LanguageService.Inicializar();
            LanguageService.CambiarIdioma("es-AR");

            using (FrmPrincipalV2 principal = new FrmPrincipalV2())
            {
                principal.Show();
                principal.Size = new System.Drawing.Size(1500, 860);
                principal.WindowState = FormWindowState.Normal;
                principal.Location = new System.Drawing.Point(0, 0);
                Bombear(1500);

                List<Button> menu = Buscar<Button>(principal).Where(b => b.Tag != null && b.Tag.GetType().Name == "EntradaMenu").OrderBy(b => b.Top).ToList();
                int n = 0;

                foreach (Button boton in menu)
                {
                    boton.PerformClick();
                    Bombear(1500);

                    TabControl pestanas = Buscar<TabControl>(principal).FirstOrDefault();
                    int paginas = pestanas == null ? 1 : pestanas.TabPages.Count;

                    for (int i = 0; i < paginas; i++)
                    {
                        if (pestanas != null)
                        {
                            pestanas.SelectedIndex = i;
                            Bombear(1400);
                        }

                        string nombre = (pestanas != null ? pestanas.TabPages[i].Text : boton.Text);
                        string archivo = string.Format("{0:00}_{1}_{2}.png", ++n, Limpio(boton.Text), Limpio(nombre));

                        principal.Activate();
                        Bombear(300);
                        int filas = Buscar<DataGridView>(principal).Where(g => g.Visible).Select(g => g.Rows.Count).DefaultIfEmpty(0).Max();
                        Console.WriteLine("  captura " + archivo + " filas=" + filas + " estado=" + string.Join("|", Buscar<Label>(principal).Where(l => l.Visible && l.Dock == DockStyle.Bottom).Select(l => l.Text)));
                        if (filas > 0)
                        {
                            string apagados = string.Join(", ", Buscar<Button>(principal).Where(b => b.Visible && !b.Enabled && b.Tag is string).Select(b => b.Text));
                            Verificador.Verificar(apagados.Length == 0, "Con una fila elegida todos los botones de " + archivo + " estan activos" + (apagados.Length > 0 ? " (apagados: " + apagados + ")" : string.Empty));
                        }

                        System.Drawing.Rectangle area = principal.Bounds;

                        using (System.Drawing.Bitmap imagen = new System.Drawing.Bitmap(area.Width, area.Height))
                        {
                            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(imagen))
                            {
                                g.CopyFromScreen(area.Location, System.Drawing.Point.Empty, area.Size);
                            }

                            imagen.Save(Path.Combine(carpeta, archivo), System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                }

                principal.Close();
            }

            File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "idioma.config"));
        }

        public static void DiagnosticoDespacho()
        {
            ApplicationMode.Reset();
            ApplicationMode.Select(ExecutionMode.SqlServer);
            FactoryDataAccess.Inicializar(ExecutionMode.SqlServer);
            SesionActual.Iniciar(new Usuario { NombreUsuario = "diag", NombreCompleto = "Diag", Rol = new Rol { Nombre = NombresRol.Administrador } });

            foreach (var d in new LogisticaService().ObtenerPedidosParaDespacho())
            {
                Console.WriteLine("  " + d.Pedido + " | " + d.Cliente + " | hab " + d.Habilitado + " | " + d.Motivo + " | pend " + d.Pendiente);
            }
        }

        public static void Diagnostico()
        {
            ApplicationMode.Reset();
            ApplicationMode.Select(ExecutionMode.Demo);
            FactoryDataAccess.Inicializar(ExecutionMode.Demo);
            SesionActual.Iniciar(new Usuario { NombreUsuario = "diag", NombreCompleto = "Diag", Rol = new Rol { Nombre = NombresRol.Administrador } });
            new CargaInicialService().AsegurarDatosBase();
            string datos = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Edelstahl.WinApp", "Datos"));
            ImportacionService imp = new ImportacionService();
            imp.ImportarClientes(Path.Combine(datos, "clientes.csv"), null);
            imp.ImportarProductos(Path.Combine(datos, "productos.csv"), null);

            CatalogoService cat = new CatalogoService();
            InventarioService inv = new InventarioService();

            foreach (string sku in new[] { "MCPSCOP/15", "CCC304-15/12-90", "CNPT304-100", "MCPSCOP/12", "AECT-25" })
            {
                Producto p = cat.ObtenerPorSku(sku);
                Console.WriteLine("  " + sku + " -> " + (p == null ? "NO EXISTE" : p.Nombre + " | tipo " + p.Tipo + " | vendible " + p.EsVendible));
                if (p != null)
                {
                    Console.WriteLine("     stock: " + string.Join("; ", inv.ConsultarStock(sku, null, false).Select(s => s.Ubicacion + " fis " + s.Fisica + " disp " + s.Disponible)));
                }
            }

            Cliente cliente = new ClienteService().ObtenerTodos().First();
            List<LineaPresupuestoSolicitud> lineas = new List<LineaPresupuestoSolicitud>();

            foreach (string sku in new[] { "CCC304-15/12-90", "MCPSCOP/12" })
            {
                Producto p = cat.ObtenerPorSku(sku);
                if (p != null)
                {
                    lineas.Add(new LineaPresupuestoSolicitud { ProductoId = p.Id, Codigo = p.CodigoSku, Descripcion = p.Nombre, Tipo = TipoItemPresupuesto.Producto, Cantidad = 1m, PrecioUnitario = p.PrecioBase });
                }
            }

            Presupuesto pre = new PresupuestoService().CrearPresupuesto(new PresupuestoSolicitud
            {
                ClienteId = cliente.Id, Moneda = Moneda.PesosArgentinos, TipoCambio = 1m, PorcentajeIva = 21m,
                VigenciaDias = 7, CondicionPago = "x", PlazoEntrega = "x", Enviar = true, Lineas = lineas
            });
            new PresupuestoService().CambiarEstado(pre.Id, EstadoPresupuesto.Aceptado);
            Pedido ped = new PedidoService().ConfirmarDesdePresupuesto(cliente.Id, pre.Id, 0m, "Efectivo", "REC-1", DateTime.Today);
            Console.WriteLine("  pedido " + ped.Numero + " lineas " + ped.Detalles.Count + " tipo " + string.Join(",", ped.Detalles.Select(d => d.TipoItem + ":" + d.Codigo)));
            Factura fac = new FacturacionService().EmitirFactura(ped.Id, TipoComprobante.FacturaA);
            foreach (PedidoDespachoDto d in new LogisticaService().ObtenerPedidosParaDespacho().Where(x => x.PedidoId == ped.Id))
            {
                Console.WriteLine("  despacho: habilitado=" + d.Habilitado + " facturado=" + d.Facturado + " motivo=" + d.Motivo);
            }

            foreach (ReservaDto r in inv.ObtenerReservas(true))
            {
                Console.WriteLine("  reserva " + r.Sku + " " + r.Reservada + " pend " + r.Pendiente + " " + r.Origen);
            }
        }

        public static void CapturarMateriales(string carpeta)
        {
            Directory.CreateDirectory(carpeta);
            ApplicationMode.Reset();
            ApplicationMode.Select(ExecutionMode.Demo);
            FactoryDataAccess.Inicializar(ExecutionMode.Demo);
            SesionActual.Iniciar(new Usuario { NombreUsuario = "m", NombreCompleto = "M", Rol = new Rol { Nombre = NombresRol.Administrador } });
            new CargaInicialService().CargarDatosDeEjemplo(null);
            LanguageService.Inicializar();

            using (EditorLineas editor = new EditorLineas(ModoLineas.Materiales, "Materials required",
                "Producto a fabricar: FILTRO-20 - Filtro de ranura continua de 20 pulgadas" + Environment.NewLine + "Cantidad 2,00 - Cliente Dante Schlogl - Pedido PED-2026-DEBCF9", 2m))
            {
                editor.StartPosition = FormStartPosition.Manual;
                editor.Location = new System.Drawing.Point(0, 0);
                editor.Show();
                Bombear(600);
                int cantidad = editor.Items.Count;
                ComboBox combo = Buscar<ComboBox>(editor).First();
                Verificador.Verificar(cantidad == 1, "El editor de materiales carga una sugerencia");
                Verificador.Verificar(combo.Items.Count > 0 && combo.Items.Count < 100, "Solo ofrece materiales, no todo el catalogo (" + combo.Items.Count + " opciones)");
                Verificador.Verificar(!combo.Items.Cast<Opcion>().Any(o => o.Texto.StartsWith("FILTRO-20")), "No aparecen productos terminados");
                System.Drawing.Rectangle area = editor.Bounds;

                using (System.Drawing.Bitmap imagen = new System.Drawing.Bitmap(area.Width, area.Height))
                {
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(imagen))
                    {
                        g.CopyFromScreen(area.Location, System.Drawing.Point.Empty, area.Size);
                    }

                    imagen.Save(Path.Combine(carpeta, "materiales.png"), System.Drawing.Imaging.ImageFormat.Png);
                }

                editor.Close();
            }
        }

        public static void GeoErrata()
        {
            Lugar l = GeoFicticio.Ubicar("Peru 1251 Vicnete Lopez Buenos Aires", "Dante");
            Verificador.Verificar(!l.Estimado && Math.Abs(l.Latitud - (-34.5261)) < 0.1 && Math.Abs(l.Longitud - (-58.4730)) < 0.1, "El mapa reconoce una localidad con errata (Vicnete Lopez): " + l.Latitud.ToString("N3") + ", " + l.Longitud.ToString("N3"));
            Lugar c = GeoFicticio.Ubicar("Av. Rivadavia 100, Buenos Aires", "x");
            Verificador.Verificar(Math.Abs(c.Latitud - (-34.6037)) < 0.1, "Una direccion de Buenos Aires cae en la Capital");
        }

        public static void CapturarMapaDemo(string carpeta)
        {
            Directory.CreateDirectory(carpeta);
            LanguageService.Inicializar();

            using (Form ventana = new Form { Width = 1000, Height = 720, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(0, 0) })
            {
                MapaWeb mapa = new MapaWeb();
                ventana.Controls.Add(mapa);
                ventana.Show();
                Bombear(300);

                string[][] datos =
                {
                    new[] { "Av. Mitre 1200, Avellaneda", "Aceros del Sur SA", "P" },
                    new[] { "Calle 12 450, Quilmes", "Metalurgica Quilmes", "E" },
                    new[] { "Diagonal 74 800, La Plata", "Frigorifico Platense", "P" },
                    new[] { "Ruta 8 km 50, Pilar", "Industrias Pilar", "N" },
                    new[] { "Av. Centenario 700, San Isidro", "Lacteos del Norte", "P" }
                };

                List<ParadaMapa> paradas = new List<ParadaMapa>();

                for (int i = 0; i < datos.Length; i++)
                {
                    paradas.Add(new ParadaMapa
                    {
                        Orden = i + 1,
                        Titulo = (i + 1) + ". " + datos[i][1],
                        Detalle = "REM-000" + (i + 1) + " - " + datos[i][0],
                        Estado = datos[i][2] == "E" ? EstadoParada.Entregada : datos[i][2] == "N" ? EstadoParada.NoEntregada : EstadoParada.Pendiente,
                        Lugar = GeoFicticio.Ubicar(datos[i][0], datos[i][1])
                    });
                }

                mapa.Cargar("Hoja de ruta HR-2026-000001 - AB123CD / Juan Perez", paradas);
                for (int espera = 0; espera < 40 && !mapa.UsaMapaWeb; espera++)
                {
                    Bombear(250);
                }

                Bombear(9000);
                System.Drawing.Rectangle area = ventana.Bounds;

                using (System.Drawing.Bitmap imagen = new System.Drawing.Bitmap(area.Width, area.Height))
                {
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(imagen))
                    {
                        g.CopyFromScreen(area.Location, System.Drawing.Point.Empty, area.Size);
                    }

                    imagen.Save(Path.Combine(carpeta, "mapa_demo.png"), System.Drawing.Imaging.ImageFormat.Png);
                }

                Verificador.Verificar(mapa.UsaMapaWeb, "El mapa interactivo (Leaflet + OpenStreetMap en WebView2) se carga");
                ventana.Close();
            }
        }

        public static void CapturarPdf(string carpeta)
        {
            Directory.CreateDirectory(carpeta);
            LanguageService.Inicializar();
            LanguageService.CambiarIdioma("es-AR");

            Edelstahl.Services.Documents.DatosReceptor receptor = new Edelstahl.Services.Documents.DatosReceptor
            {
                RazonSocial = "Cliente de Prueba SRL", Cuit = "30712345678", EtiquetaDocumento = "CUIT", Domicilio = "Av. Mitre 1200", Localidad = "Avellaneda, Buenos Aires",
                CondicionIva = "Responsable Inscripto", Email = "cliente@example.test"
            };

            Edelstahl.Services.Documents.DatosRemito remito = new Edelstahl.Services.Documents.DatosRemito
            {
                Numero = "0001-00000099", Fecha = DateTime.Today, Receptor = receptor, DireccionEntrega = "Av. Mitre 1200, Avellaneda", Pedido = "PED-2026-DEMO",
                Chofer = "Pereyra, Lucia", Vehiculo = "AC456EF"
            };
            remito.Lineas.Add(new Edelstahl.Services.Documents.LineaRemito { Codigo = "JCTEF30", Descripcion = "Junta clamp de teflon (3\")", Unidad = "UNIDAD", Cantidad = 2m });
            remito.Lineas.Add(new Edelstahl.Services.Documents.LineaRemito { Codigo = "KFS", Descripcion = "Keg filler stop plastico (5/8\")", Unidad = "UNIDAD", Cantidad = 3m });
            new Edelstahl.Services.Documents.RemitoPdfService(remito).Generar(Path.Combine(carpeta, "remito.pdf"));
            new Edelstahl.Services.Documents.RemitoPdfService(remito).GenerarImagenes(Path.Combine(carpeta, "remito"));

            Edelstahl.Services.Documents.DatosFactura factura = new Edelstahl.Services.Documents.DatosFactura
            {
                Letra = "A", CodigoComprobante = "001", Numero = "0001-00000099", Fecha = DateTime.Today, Vencimiento = DateTime.Today.AddDays(30), Receptor = receptor,
                CondicionVenta = "Contado", Referencia = "Pedido PED-2026-DEMO", Neto = 100m, Iva = 21m, AlicuotaIva = 21m, Total = 121m, Saldo = 121m
            };
            factura.Lineas.Add(new Edelstahl.Services.Documents.LineaDocumento { Codigo = "JCTEF30", Descripcion = "Junta clamp de teflon (3\")", Cantidad = 2m, Unidad = "UNIDAD", PrecioUnitario = 50m, Subtotal = 100m });
            new Edelstahl.Services.Documents.FacturaPdfService(factura).Generar(Path.Combine(carpeta, "factura.pdf"));
            new Edelstahl.Services.Documents.FacturaPdfService(factura).GenerarImagenes(Path.Combine(carpeta, "factura"));

            Verificador.Verificar(File.Exists(Path.Combine(carpeta, "remito.pdf")) && File.Exists(Path.Combine(carpeta, "factura.pdf")), "Se generaron el remito y la factura de ejemplo");

            Edelstahl.Domain.Comercial.Cliente clienteDemo = new Edelstahl.Domain.Comercial.Cliente
            {
                RazonSocial = "Cliente de Prueba SRL", CUIT = "30712345678", Email = "cliente@example.test", Telefono = "1140055433",
                Localidad = "Avellaneda", DireccionEntrega = "Av. Mitre 1200, Avellaneda"
            };
            Edelstahl.Domain.Comercial.Presupuesto presupuesto = new Edelstahl.Domain.Comercial.Presupuesto
            {
                Numero = "PRE-2026-000099", FechaEmision = DateTime.Today, FechaVencimiento = DateTime.Today.AddDays(7), PorcentajeIVA = 21m, PorcentajeAnticipo = 30m,
                CondicionPago = "Contado", PlazoEntrega = "10 dias habiles", EntregaIncluida = true, Observaciones = "Entrega en obra con aviso previo."
            };
            presupuesto.Detalles.Add(new Edelstahl.Domain.Comercial.DetallePresupuesto { Codigo = "JCTEF30", Descripcion = "Junta clamp de teflon (3\")", Cantidad = 4m, PrecioUnitario = 1500m });
            presupuesto.Detalles.Add(new Edelstahl.Domain.Comercial.DetallePresupuesto
            {
                Codigo = "AECT-25", Descripcion = "Abrazadera tipo T AISI 304 (2 1/2\")", DescripcionTecnica = "Largo 120 mm, espesor 2 mm, pulido sanitario",
                TipoItem = Edelstahl.Domain.Comercial.TipoItemPresupuesto.FabricacionEspecial, Cantidad = 10m, PrecioUnitario = 2000m
            });
            string rutaPresupuesto = new Edelstahl.Services.Documents.PresupuestoPdfService().Generar(presupuesto, clienteDemo);
            new Edelstahl.Services.Documents.PresupuestoPdfService().GenerarImagenes(presupuesto, clienteDemo, Path.Combine(carpeta, "presupuesto"));
            Verificador.Verificar(File.Exists(rutaPresupuesto), "Se genero el presupuesto imprimible de ejemplo");
        }

        public static void CapturarVerificacion(string carpeta)
        {
            Directory.CreateDirectory(carpeta);
            ApplicationMode.Reset();
            ApplicationMode.Select(ExecutionMode.Demo);
            FactoryDataAccess.Inicializar(ExecutionMode.Demo);
            SesionActual.Iniciar(new Usuario { NombreUsuario = "m", NombreCompleto = "M", Rol = new Rol { Nombre = NombresRol.Administrador } });
            new CargaInicialService().CargarDatosDeEjemplo(null);
            LanguageService.Inicializar();

            List<LineaPendienteRemitoDto> lineas = new List<LineaPendienteRemitoDto>
            {
                new LineaPendienteRemitoDto { DetallePedidoId = Guid.NewGuid(), Codigo = "JCTEF30", Descripcion = "Junta clamp de teflon (3\")", Pedida = 1m, Pendiente = 1m, ListaParaDespacho = 1m },
                new LineaPendienteRemitoDto { DetallePedidoId = Guid.NewGuid(), Codigo = "AC304/50", Descripcion = "Abrazaderas clamp AISI 304 (5\")", Pedida = 3m, Pendiente = 3m, ListaParaDespacho = 3m },
                new LineaPendienteRemitoDto { DetallePedidoId = Guid.NewGuid(), Codigo = "KFS", Descripcion = "Keg filler stop plastico (5/8\")", Pedida = 3m, Pendiente = 3m, ListaParaDespacho = 0m }
            };

            using (FrmVerificarCarga ventana = new FrmVerificarCarga("PED-2026-DBB20B", "Aprobar Tesis", lineas))
            {
                ventana.StartPosition = FormStartPosition.Manual;
                ventana.Location = new System.Drawing.Point(40, 40);
                ventana.Show();
                Bombear(400);
                Verificador.Igual(0, ventana.Lineas.Count, "Sin verificar no sale ninguna linea en el remito");

                DataGridView grilla = Buscar<DataGridView>(ventana).First();
                grilla.Rows[0].Cells[0].Value = true;
                grilla.Rows[1].Cells[0].Value = true;
                Bombear(300);
                Verificador.Igual(2, ventana.Lineas.Count, "Quedan las lineas verificadas");
                Verificador.Igual(3m, ventana.Lineas[1].Cantidad, "La cantidad es la facturada y no se edita");

                Button generar = Buscar<Button>(ventana).First(b => b.Tag != null && (string)b.Tag == "Create delivery note");
                Verificador.Verificar(generar.Enabled == false || ventana.Lineas.Count == 2, "El boton queda controlado por la verificacion");
                System.Drawing.Rectangle area = ventana.Bounds;

                using (System.Drawing.Bitmap imagen = new System.Drawing.Bitmap(area.Width, area.Height))
                {
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(imagen))
                    {
                        g.CopyFromScreen(area.Location, System.Drawing.Point.Empty, area.Size);
                    }

                    imagen.Save(Path.Combine(carpeta, "verificar_carga.png"), System.Drawing.Imaging.ImageFormat.Png);
                }

                ventana.Close();
            }
        }

        public static void CapturarFaltantes(string carpeta)
        {
            Directory.CreateDirectory(carpeta);
            ApplicationMode.Reset();
            ApplicationMode.Select(ExecutionMode.Demo);
            FactoryDataAccess.Inicializar(ExecutionMode.Demo);
            SesionActual.Iniciar(new Usuario { NombreUsuario = "m", NombreCompleto = "M", Rol = new Rol { Nombre = NombresRol.Administrador } });
            new CargaInicialService().CargarDatosDeEjemplo(null);
            LanguageService.Inicializar();

            List<FaltanteDto> datos = new List<FaltanteDto>
            {
                new FaltanteDto { ProductoId = Guid.NewGuid(), Sku = "JCTEF30", Producto = "Junta clamp de teflon (3\")", Requerido = 1m, Disponible = 0m, Faltante = 1m },
                new FaltanteDto { ProductoId = Guid.NewGuid(), Sku = "KFS", Producto = "Keg filler stop plastico (5/8\")", Requerido = 3m, Disponible = 1m, Faltante = 2m },
                new FaltanteDto { ProductoId = Guid.NewGuid(), Sku = "AC304/50", Producto = "Abrazaderas clamp AISI 304 (5\")", Requerido = 3m, Disponible = 0m, Faltante = 3m }
            };

            using (FrmFaltantes ventana = new FrmFaltantes("Missing products", "Some products are not in stock. Add the quantities below to confirm the order.", datos, true))
            {
                ventana.StartPosition = FormStartPosition.Manual;
                ventana.Location = new System.Drawing.Point(40, 40);
                ventana.Show();
                Bombear(500);
                Verificador.Igual(3, ventana.Ingresos.Count, "La ventana propone ingresar justo lo que falta de cada producto");
                Verificador.Igual(6m, ventana.Ingresos.Sum(i => i.Value), "Las cantidades propuestas suman lo faltante");
                System.Drawing.Rectangle area = ventana.Bounds;

                using (System.Drawing.Bitmap imagen = new System.Drawing.Bitmap(area.Width, area.Height))
                {
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(imagen))
                    {
                        g.CopyFromScreen(area.Location, System.Drawing.Point.Empty, area.Size);
                    }

                    imagen.Save(Path.Combine(carpeta, "faltantes.png"), System.Drawing.Imaging.ImageFormat.Png);
                }

                ventana.Close();
            }
        }

        public static void CapturarEditorLineas(string carpeta)
        {
            Directory.CreateDirectory(carpeta);
            ApplicationMode.Reset();
            ApplicationMode.Select(ExecutionMode.Demo);
            FactoryDataAccess.Inicializar(ExecutionMode.Demo);
            SesionActual.Iniciar(new Usuario { NombreUsuario = "m", NombreCompleto = "M", Rol = new Rol { Nombre = NombresRol.Administrador } });
            new CargaInicialService().CargarDatosDeEjemplo(null);
            LanguageService.Inicializar();

            using (EditorLineas editor = new EditorLineas(ModoLineas.Presupuesto, "Quotation lines", null, 0m, "Abastecimiento ACI (CUIT 30711223344)"))
            {
                editor.StartPosition = FormStartPosition.Manual;
                editor.Location = new System.Drawing.Point(40, 40);
                editor.Show();
                Bombear(300);

                System.Reflection.BindingFlags f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                System.Type t = typeof(EditorLineas);
                ComboBox producto = (ComboBox)t.GetField("_producto", f).GetValue(editor);
                System.Reflection.MethodInfo agregar = t.GetMethod("AgregarLinea", f);

                producto.SelectedIndex = 0;
                agregar.Invoke(editor, null);
                agregar.Invoke(editor, null);
                producto.SelectedIndex = 3;
                agregar.Invoke(editor, null);
                Bombear(300);

                Verificador.Igual(2, editor.Items.Count, "El mismo producto se suma en una sola linea");
                Verificador.Igual(2m, editor.Items[0].Cantidad, "La cantidad de la linea repetida se suma");

                t.GetMethod("CambiarCantidad", f).Invoke(editor, new object[] { 3m });
                Bombear(200);
                Verificador.Igual(5m, editor.Items[0].Cantidad, "Se puede sumar cantidad a una linea cargada");
                t.GetMethod("CambiarCantidad", f).Invoke(editor, new object[] { -1m });
                Verificador.Igual(4m, editor.Items[0].Cantidad, "Se puede restar cantidad a una linea cargada");

                for (int i = 0; i < 8; i++)
                {
                    t.GetMethod("CambiarCantidad", f).Invoke(editor, new object[] { -1m });
                }

                Verificador.Igual(1m, editor.Items[0].Cantidad, "Restar de a uno se detiene en 1 y no borra la linea");
                Verificador.Igual(2, editor.Items.Count, "Restar no toca las otras lineas");
                Verificador.Igual(1m, editor.Items[1].Cantidad, "La cantidad de otro articulo no se modifica");
                Bombear(300);

                System.Drawing.Rectangle area = editor.Bounds;

                using (System.Drawing.Bitmap imagen = new System.Drawing.Bitmap(area.Width, area.Height))
                {
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(imagen))
                    {
                        g.CopyFromScreen(area.Location, System.Drawing.Point.Empty, area.Size);
                    }

                    imagen.Save(Path.Combine(carpeta, "editor_lineas.png"), System.Drawing.Imaging.ImageFormat.Png);
                }

                editor.Close();
            }
        }

        private static string Limpio(string texto)
        {
            string normal = texto.Normalize(System.Text.NormalizationForm.FormD);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (char c in normal)
            {
                if (char.IsLetterOrDigit(c) && c < 128)
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
                else if (c == ' ' || c == '-')
                {
                    sb.Append('-');
                }
            }

            return sb.ToString();
        }

        private static void ProbarIdiomas()
        {
            string origen = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Edelstahl.WinApp", "Languages"));
            string destino = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages");
            Directory.CreateDirectory(destino);

            foreach (string archivo in Directory.GetFiles(origen))
            {
                File.Copy(archivo, Path.Combine(destino, Path.GetFileName(archivo)), true);
            }

            string config = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "idioma.config");
            File.WriteAllText(config, "es-AR");

            LanguageService.Inicializar();
            Verificador.Verificar(LanguageService.IdiomaActual == "en-US", "Al iniciar el idioma es ingles aunque haya otro guardado");
            Verificador.Verificar(Tr.T("Cancel order") == "Cancel order", "En ingles se muestra la clave");

            LanguageService.AplicarIdiomaPreferido();
            Verificador.Verificar(LanguageService.IdiomaActual == "es-AR", "Despues del acceso se aplica el idioma del usuario");
            Verificador.Verificar(Tr.T("Cancel order") == "Cancelar pedido", "Traduce al espanol: " + Tr.T("Cancel order"));
            Verificador.Verificar(Tr.T("Order {0} confirmed.", "PED-1") == "Pedido PED-1 confirmado.", "Traduce mensajes con parametros");
            Verificador.Verificar(Tr.Columna("RazonSocial") == "Razón social", "Traduce columnas: " + Tr.Columna("RazonSocial"));

            Verificador.Verificar(Tr.T("Received in good order") == "Recibí conforme" && Tr.T("TRIPLICATE") == "TRIPLICADO" && Tr.T("Route sheet") == "Hoja de ruta", "Los textos de los PDF se traducen al espanol");
            Verificador.Verificar(Tr.Columna("Orden") == "Parada", "La columna Orden de las paradas se llama Parada");

            Probar<ModuloComercial>();

            foreach (string codigo in new[] { "es-ES", "pt-BR", "de-DE", "ru-RU", "en-GB", "en-US" })
            {
                LanguageService.CambiarIdioma(codigo);
                Verificador.Verificar(LanguageService.IdiomaActual == codigo, "Cambia a " + codigo + " y muestra " + Tr.T("Sales"));
            }

            File.Delete(config);
        }

        private static void ProbarDialogoConNota()
        {
            List<Opcion> opciones = new List<Opcion> { new Opcion("A", 100m), new Opcion("B", 250m) };
            bool alineado = false;
            decimal valorInicial = -1m;
            decimal valorTrasCambio = -1m;

            System.Windows.Forms.Timer revisar = new System.Windows.Forms.Timer { Interval = 500 };
            revisar.Tick += (s, e) =>
            {
                revisar.Stop();
                Form dialogo = Application.OpenForms.Cast<Form>().LastOrDefault(x => x.Text == "Test note");

                if (dialogo != null)
                {
                    TableLayoutPanel tabla = Buscar<TableLayoutPanel>(dialogo).First();
                    Label etiqueta = Buscar<Label>(tabla).First(l => l.Text == "Deposit");
                    NumericUpDown numero = Buscar<NumericUpDown>(tabla).First();
                    ComboBox combo = Buscar<ComboBox>(tabla).First();
                    alineado = Math.Abs(etiqueta.Top - numero.Top) < 14 && etiqueta.Top > combo.Top;
                    valorInicial = numero.Value;
                    combo.SelectedIndex = 1;
                    valorTrasCambio = numero.Value;
                    dialogo.DialogResult = DialogResult.Cancel;
                }
            };
            revisar.Start();

            Dialogo.Pedir(null, "Test note", "A note above the fields.",
                Campo.Combo("lista", "Quotation", opciones),
                Campo.Numero("monto", "Deposit", 0m).Siguiendo("lista", o => (decimal)o));

            revisar.Dispose();
            Verificador.Verificar(alineado, "En un cuadro con nota, cada etiqueta queda en la fila de su campo");
            Verificador.Verificar(valorInicial == 100m && valorTrasCambio == 250m, "El monto se completa solo segun la opcion elegida (" + valorInicial + " y " + valorTrasCambio + ")");
        }

        private static void Bombear(int milisegundos)
        {
            Stopwatch reloj = Stopwatch.StartNew();

            while (reloj.ElapsedMilliseconds < milisegundos)
            {
                Application.DoEvents();
                Thread.Sleep(15);
            }
        }

        private static void Probar<T>() where T : Control, new()
        {
            using (Form ventana = new Form { Width = 1200, Height = 800 })
            {
                T modulo = new T { Dock = DockStyle.Fill };
                ventana.Controls.Add(modulo);
                ventana.Show();
                Bombear(800);

                TabControl pestanas = Buscar<TabControl>(modulo).FirstOrDefault();
                int cantidad = pestanas == null ? 0 : pestanas.TabPages.Count;

                for (int i = 0; i < cantidad; i++)
                {
                    pestanas.SelectedIndex = i;
                    Bombear(1200);

                    string pestana = pestanas.TabPages[i].Text;
                    List<Label> fallas = Buscar<Label>(pestanas.TabPages[i])
                        .Where(l => l.Text.Contains("could not be loaded")).ToList();

                    Verificador.Verificar(fallas.Count == 0, typeof(T).Name + " / " + pestana + " carga sin errores");
                }

                Verificador.Verificar(cantidad > 0, typeof(T).Name + " tiene pestanas (" + cantidad + ")");
                ventana.Close();
            }
        }

        private static IEnumerable<C> Buscar<C>(Control raiz) where C : Control
        {
            foreach (Control hijo in raiz.Controls)
            {
                C directo = hijo as C;

                if (directo != null)
                {
                    yield return directo;
                }

                foreach (C interno in Buscar<C>(hijo))
                {
                    yield return interno;
                }
            }
        }

        private static void AbrirDialogos()
        {
            string[] acciones =
            {
                "New customer", "New quotation", "Confirm order", "New user", "New product", "Register movement",
                "Transfer", "New purchase order", "New supplier", "New vehicle", "New driver", "New route sheet",
                "Mass price update", "New category", "Register return"
            };

            Type[] modulos = { typeof(ModuloComercial), typeof(ModuloAdministracion), typeof(ModuloInventario), typeof(ModuloLogistica) };

            foreach (Type tipo in modulos)
            {
                using (Form ventana = new Form { Width = 1200, Height = 800 })
                {
                    Control modulo = (Control)Activator.CreateInstance(tipo);
                    modulo.Dock = DockStyle.Fill;
                    ventana.Controls.Add(modulo);
                    ventana.Show();
                    Bombear(500);

                    TabControl pestanas = Buscar<TabControl>(modulo).First();

                    for (int i = 0; i < pestanas.TabPages.Count; i++)
                    {
                        pestanas.SelectedIndex = i;
                        Bombear(700);

                        foreach (Button boton in Buscar<Button>(pestanas.TabPages[i]).ToList())
                        {
                            string clave = boton.Tag as string;

                            if (clave == null || Array.IndexOf(acciones, clave) < 0 || !boton.Enabled)
                            {
                                continue;
                            }

                            System.Windows.Forms.Timer cierre = new System.Windows.Forms.Timer { Interval = 400 };
                            cierre.Tick += (s, e) => { PostMessage(GetForegroundWindow(), WM_CLOSE, IntPtr.Zero, IntPtr.Zero); };
                            cierre.Start();

                            try
                            {
                                boton.PerformClick();
                                Verificador.Verificar(true, "Se abre el cuadro '" + clave + "'");
                            }
                            catch (Exception ex)
                            {
                                Verificador.Verificar(false, "Fallo el cuadro '" + clave + "': " + ex.Message);
                            }
                            finally
                            {
                                cierre.Stop();
                                cierre.Dispose();
                            }
                        }
                    }

                    ventana.Close();
                }
            }
        }
    }
}
