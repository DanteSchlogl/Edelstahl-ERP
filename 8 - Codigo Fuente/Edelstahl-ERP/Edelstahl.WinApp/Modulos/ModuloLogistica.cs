using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.BLL.Services;
using Edelstahl.Domain.Finanzas;
using Edelstahl.Domain.Logistica;
using Edelstahl.Domain.Security;
using Edelstahl.WinApp.Ui;

namespace Edelstahl.WinApp.Modulos
{
    /// <summary>
    /// Logistica: remitos (original y dos copias de colores), hojas de ruta, flota y devoluciones.
    /// </summary>
    public class ModuloLogistica : ModuloBase
    {
        private readonly LogisticaService _logistica = new LogisticaService();
        private readonly DevolucionService _devoluciones = new DevolucionService();
        private readonly DocumentosService _documentos = new DocumentosService();

        public ModuloLogistica()
        {
            AgregarPestana("Ready to ship", CrearDespacho(), Permiso.LogisticaVer);
            AgregarPestana("Delivery notes", CrearRemitos(), Permiso.LogisticaVer);
            AgregarPestana("Route sheets", CrearHojas(), Permiso.LogisticaVer);
            AgregarPestana("Shipment log", CrearHistorialEnvios(), Permiso.LogisticaVer);
            AgregarPestana("Returns", CrearDevoluciones(), Permiso.DevolucionesGestionar);
            AgregarPestana("Vehicles", CrearVehiculos(), Permiso.LogisticaVer);
            AgregarPestana("Drivers", CrearChoferes(), Permiso.LogisticaVer);
            CargarPestanaActual();
        }

        // ---------------------------------------------------------------
        // Despacho y remitos
        // ---------------------------------------------------------------

        private Control CrearDespacho()
        {
            PanelLista lista = null;
            lista = new PanelLista(() => _logistica.ObtenerPedidosParaDespacho());

            Button generar = lista.Agregar("Create delivery note", fila =>
            {
                if (!Estilo.Valor<bool>(fila, "Habilitado"))
                {
                    Estilo.Avisar(this, Estilo.Valor<string>(fila, "Motivo"));
                    return;
                }

                Guid pedidoId = Estilo.Valor<Guid>(fila, "PedidoId");
                List<LineaPendienteRemitoDto> lineas = _logistica.ObtenerLineasPendientes(pedidoId)
                    .Where(l => l.Pendiente > 0m).ToList();

                if (lineas.Count == 0)
                {
                    Estilo.Avisar(this, Tr.T("This order has nothing pending to ship."));
                    return;
                }

                // Las cantidades salen de lo facturado y reservado: no se editan. Se verifican una por una.
                List<LineaRemitoSolicitud> solicitud;
                string observaciones;

                using (FrmVerificarCarga verificar = new FrmVerificarCarga(
                    Estilo.Valor<string>(fila, "Pedido"), Estilo.Valor<string>(fila, "Cliente"), lineas))
                {
                    if (verificar.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    solicitud = verificar.Lineas;
                    observaciones = verificar.Observaciones;
                }

                RemitoEntrega remito = _logistica.GenerarRemito(pedidoId, solicitud, observaciones);
                new BitacoraService().RegistrarAuditoria("Logística", "VerificarCargaRemito", "RemitoEntrega", remito.Id,
                    remito.Numero + ": carga verificada (" + solicitud.Count + " líneas)");
                string ruta = _documentos.GenerarRemito(remito.Id);
                lista.Recargar();
                Estilo.AbrirArchivo(this, ruta);
            }, true, Permiso.LogisticaGestionar);

            lista.AgregarExportacion();
            return lista;
        }

        private Control CrearRemitos()
        {
            PanelLista lista = null;
            lista = new PanelLista(() => _logistica.ObtenerRemitos(null, null));

            lista.Agregar("Print delivery note (3 copies)", fila =>
            {
                Estilo.AbrirArchivo(this, _documentos.GenerarRemito(Estilo.IdDe(fila).Value));
            }, true, Permiso.LogisticaVer);

            lista.Agregar("Assign to route sheet", fila =>
            {
                NuevaHojaDeRuta(Estilo.IdDe(fila));
                lista.Recargar();
            }, true, Permiso.LogisticaGestionar);

            lista.Agregar("Cancel", fila =>
            {
                Respuesta datos = Dialogo.Pedir(this, "Cancel", Campo.Texto("motivo", "Reason", "", true, 200));

                if (datos != null)
                {
                    _logistica.CancelarRemito(Estilo.IdDe(fila).Value, datos.Texto("motivo"));
                    lista.Recargar();
                }
            }, true, Permiso.LogisticaGestionar);

            lista.AgregarExportacion();
            return lista;
        }

        // ---------------------------------------------------------------
        // Hojas de ruta
        // ---------------------------------------------------------------

        private void NuevaHojaDeRuta(Guid? preseleccionado)
        {
        List<RemitoDto> remitos = _logistica.ObtenerRemitosDisponiblesParaRuta();

        if (remitos.Count == 0)
        {
            Estilo.Avisar(this, Tr.T("There are no delivery notes available for a route."));
            return;
        }

        if (preseleccionado.HasValue && !remitos.Any(r => r.Id == preseleccionado.Value))
        {
            Estilo.Avisar(this, Tr.T("This delivery note is not available for a route sheet (it may already be on one)."));
            return;
        }

        List<Opcion> vehiculos = _logistica.ObtenerVehiculos(true)
            .Select(v => new Opcion(v.Patente + "  " + v.Marca + " " + v.Modelo + "  (" + v.CapacidadKg.ToString("N0") + " kg)", v.Id)).ToList();
        List<Opcion> choferes = _logistica.ObtenerChoferes(true)
            .Select(c => new Opcion(c.NombreCompleto, c.Id)).ToList();

        List<Campo> campos = new List<Campo>
        {
            Campo.Combo("vehiculo", "Vehicle", vehiculos),
            Campo.Combo("chofer", "Driver", choferes),
            Campo.Fecha("fecha", "Planned date", DateTime.Today)
        };

        for (int i = 0; i < remitos.Count && i < 12; i++)
        {
            campos.Add(Campo.Check("r" + i, remitos[i].Numero + "  " + remitos[i].Cliente + "  " + remitos[i].PesoKg.ToString("N0") + " kg")
            .Con(preseleccionado.HasValue && remitos[i].Id == preseleccionado.Value));
        }

        campos.Add(Campo.Texto("obs", "Notes", "", false, 200));

        Respuesta datos = Dialogo.Pedir(this, "New route sheet",
            "Check the delivery notes to include. They are visited in the order shown.",
            campos.ToArray());

        if (datos == null)
        {
            return;
        }

        List<Guid> elegidos = new List<Guid>();

        for (int i = 0; i < remitos.Count && i < 12; i++)
        {
            if (datos.Check("r" + i))
            {
                elegidos.Add(remitos[i].Id);
            }
        }

        HojaRuta hoja = _logistica.CrearHojaRuta(datos.GuidDe("vehiculo"), datos.GuidDe("chofer"),
            datos.Fecha("fecha") ?? DateTime.Today, elegidos, datos.Texto("obs"));

        Estilo.Informar(this, Tr.T("Route sheet {0} created.", hoja.Numero));
        }

        private Control CrearHistorialEnvios()
        {
            PanelLista lista = null;
            lista = new PanelLista(() => _logistica.ObtenerHistorialEnvios());
            lista.ConDetalle(fila => _logistica.ObtenerEventosEnvio(Estilo.IdDe(fila).Value));

            lista.Agregar("Reschedule delivery", fila =>
            {
                Respuesta datos = Dialogo.Pedir(this, "Reschedule delivery",
                    "The delivery note becomes available again for a new route sheet. The failed attempt stays on record.",
                    Campo.Texto("motivo", "Reason", "", true, 200));

                if (datos == null)
                {
                    return;
                }

                _logistica.ReprogramarRemito(Estilo.IdDe(fila).Value, datos.Texto("motivo"));
                lista.Recargar();
                Estilo.Informar(this, Tr.T("Delivery rescheduled. Assign it to a new route sheet from Delivery notes."));
            }, true, Permiso.LogisticaGestionar);

            lista.AgregarExportacion();
            return lista;
        }

        private Control CrearHojas()
        {
            PanelLista lista = null;
            lista = new PanelLista(() => _logistica.ObtenerHojas(null));
            lista.ConDetalle(fila => _logistica.ObtenerParadas(Estilo.IdDe(fila).Value));

            lista.Agregar("New route sheet", fila =>
            {
                NuevaHojaDeRuta(null);
                lista.Recargar();
            }, false, Permiso.LogisticaGestionar);

            lista.Agregar("Start route", fila =>
            {
                _logistica.IniciarHojaRuta(Estilo.IdDe(fila).Value, null);
                lista.Recargar();
                Estilo.Informar(this, Tr.T("Route started."));
            }, true, Permiso.LogisticaGestionar);

            lista.Agregar("Register delivery", fila =>
            {
                List<ParadaDto> pendientes = _logistica.ObtenerParadas(Estilo.IdDe(fila).Value).Where(p => p.Pendiente).ToList();

                if (pendientes.Count == 0)
                {
                    Estilo.Avisar(this, Tr.T("This route has no pending stops."));
                    return;
                }

                List<Opcion> resultados = Enum.GetValues(typeof(ResultadoEntrega)).Cast<ResultadoEntrega>()
                    .Select(r => new Opcion(Etiquetas.De(r), r)).ToList();

                Respuesta datos = Dialogo.Pedir(this, "Register delivery",
                    Campo.Combo("parada", "Stop", pendientes.Select(p => new Opcion(p.Orden + ". " + p.Remito + "  " + p.Cliente, p.DetalleId)).ToList()),
                    Campo.Combo("resultado", "Result", resultados),
                    Campo.Texto("receptor", "Received by", "", false, 100),
                    Campo.Texto("documento", "Receiver ID", "", false, 30),
                    Campo.Texto("motivo", "Reason (if not delivered)", "", false, 200),
                    Campo.Texto("obs", "Notes", "", false, 200));

                if (datos == null)
                {
                    return;
                }

                _logistica.RegistrarEntrega(datos.GuidDe("parada"), (ResultadoEntrega)datos.Seleccion("resultado"),
                    datos.Texto("receptor"), datos.Texto("documento"), datos.Texto("motivo"), datos.Texto("obs"), null);
                lista.Recargar();

            }, true, Permiso.LogisticaGestionar);

            lista.Agregar("Open route in web map", fila =>
            {
                List<ParadaDto> paradas = _logistica.ObtenerParadas(Estilo.IdDe(fila).Value);

                if (paradas.Count == 0)
                {
                    Estilo.Avisar(this, Tr.T("This route has no stops."));
                    return;
                }

                List<string> destinos = paradas.Select(p => (p.Direccion + " " + p.Cliente).Trim()).ToList();
                string url = "https://www.google.com/maps/dir/?api=1&origin=" + Uri.EscapeDataString("Buenos Aires, Argentina")
                    + "&destination=" + Uri.EscapeDataString(destinos[destinos.Count - 1]);

                if (destinos.Count > 1)
                {
                    url += "&waypoints=" + Uri.EscapeDataString(string.Join("|", destinos.Take(destinos.Count - 1)));
                }

                Estilo.AbrirUrl(this, url);
            }, true, Permiso.LogisticaVer);

            lista.AgregarExportacion();
            return new PanelHojasRuta(lista, _logistica);
        }

        // ---------------------------------------------------------------
        // Devoluciones
        // ---------------------------------------------------------------

        private Control CrearDevoluciones()
        {
            PanelLista lista = null;
            lista = new PanelLista(() => _devoluciones.ObtenerDevoluciones(null));
            lista.ConDetalle(fila => _devoluciones.ObtenerDetalles(Estilo.IdDe(fila).Value));

            lista.Agregar("Register return", fila =>
            {
                List<RemitoDto> entregados = _logistica.ObtenerRemitos(null, EstadoRemito.Entregado);

                if (entregados.Count == 0)
                {
                    Estilo.Avisar(this, Tr.T("There are no delivered notes to return against."));
                    return;
                }

                Respuesta paso1 = Dialogo.Pedir(this, "Register return",
                    Campo.Combo("remito", "Delivery note", entregados.Select(r => new Opcion(r.Numero + "  " + r.Cliente, r.Id)).ToList()));

                if (paso1 == null)
                {
                    return;
                }

                RemitoEntrega remito = _logistica.ObtenerRemito(paso1.GuidDe("remito"));
                List<Campo> campos = new List<Campo>();
                List<DetalleRemito> detalles = remito.Detalles.ToList();

                for (int i = 0; i < detalles.Count; i++)
                {
                    campos.Add(Campo.Numero("l" + i, detalles[i].Codigo + "  " + detalles[i].Descripcion, 0m, 0m, detalles[i].Cantidad, 3));
                }

                campos.Add(Campo.Texto("motivo", "Reason", "", true, 200));

                Respuesta paso2 = Dialogo.Pedir(this, "Register return",
                    "Enter the quantity returned for each line (zero when it is not returned).", campos.ToArray());

                if (paso2 == null)
                {
                    return;
                }

                List<LineaDevolucionSolicitud> solicitud = new List<LineaDevolucionSolicitud>();

                for (int i = 0; i < detalles.Count; i++)
                {
                    if (paso2.Numero("l" + i) > 0m)
                    {
                        solicitud.Add(new LineaDevolucionSolicitud
                        {
                            DetalleRemitoId = detalles[i].Id,
                            Cantidad = paso2.Numero("l" + i),
                            Motivo = paso2.Texto("motivo")
                        });
                    }
                }

                Devolucion devolucion = _devoluciones.RegistrarDevolucion(remito.Id, paso2.Texto("motivo"), solicitud);
                Estilo.Informar(this, Tr.T("Return {0} registered.", devolucion.Numero));
                lista.Recargar();
            }, false, Permiso.DevolucionesGestionar);

            lista.Agregar("Receive", fila =>
            {
                _devoluciones.RecibirDevolucion(Estilo.IdDe(fila).Value);
                lista.Recargar();
            }, true, Permiso.DevolucionesGestionar);

            lista.Agregar("Inspect and resolve line", fila =>
            {
                List<DetalleDevolucionDto> detalles = _devoluciones.ObtenerDetalles(Estilo.IdDe(fila).Value);

                if (detalles.Count == 0)
                {
                    return;
                }

                List<Opcion> acciones = Enum.GetValues(typeof(AccionResuelta)).Cast<AccionResuelta>()
                    .Select(a => new Opcion(Etiquetas.De(a), a)).ToList();

                Respuesta datos = Dialogo.Pedir(this, "Inspect and resolve line",
                    Campo.Combo("detalle", "Line", detalles.Select(d => new Opcion(d.Codigo + "  " + d.Descripcion + "  (" + d.Devuelta.ToString("N2") + ")", d.Id)).ToList()),
                    Campo.Combo("accion", "Action", acciones),
                    Campo.Check("apto", "Fit to return to stock", true),
                    Campo.Numero("reingresada", "Quantity back to stock", 0m, 0m, 1000000m, 3),
                    Campo.Numero("descartada", "Quantity discarded", 0m, 0m, 1000000m, 3),
                    Campo.Texto("condicion", "Condition", "", false, 100),
                    Campo.Texto("obs", "Notes", "", false, 200));

                if (datos == null)
                {
                    return;
                }

                _devoluciones.ResolverDetalle(new ResolucionDevolucion
                {
                    DetalleDevolucionId = datos.GuidDe("detalle"),
                    Accion = (AccionResuelta)datos.Seleccion("accion"),
                    AptoReingreso = datos.Check("apto"),
                    CantidadReingresada = datos.Numero("reingresada"),
                    CantidadDescartada = datos.Numero("descartada"),
                    Condicion = datos.Texto("condicion"),
                    Observaciones = datos.Texto("obs")
                });
                lista.Recargar();
            }, true, Permiso.DevolucionesGestionar);

            lista.Agregar("Issue credit note", fila =>
            {
                Guid devolucionId = Estilo.IdDe(fila).Value;
                NotaCreditoSugerida sugerida = _devoluciones.CalcularNotaCreditoSugerida(devolucionId);

                Respuesta datos = Dialogo.Pedir(this, "Issue credit note",
                    Tr.T("Invoice {0}. Suggested net amount: {1}", sugerida.Factura, sugerida.MontoNeto.ToString("N2")),
                    Campo.Numero("monto", "Net amount", sugerida.MontoNeto),
                    Campo.Texto("motivo", "Reason", Tr.T("Customer return"), true, 200));

                if (datos == null)
                {
                    return;
                }

                NotaCredito nota = new FacturacionService().EmitirNotaCredito(sugerida.FacturaId, devolucionId, datos.Numero("monto"), datos.Texto("motivo"));
                Estilo.AbrirArchivo(this, _documentos.GenerarNotaCredito(nota.Id));
                lista.Recargar();
            }, true, Permiso.FacturasEmitir);

            lista.Agregar("Cancel", fila =>
            {
                Respuesta datos = Dialogo.Pedir(this, "Cancel", Campo.Texto("motivo", "Reason", "", true, 200));

                if (datos != null)
                {
                    _devoluciones.CancelarDevolucion(Estilo.IdDe(fila).Value, datos.Texto("motivo"));
                    lista.Recargar();
                }
            }, true, Permiso.DevolucionesGestionar);

            lista.AgregarExportacion();
            return lista;
        }

        // ---------------------------------------------------------------
        // Flota
        // ---------------------------------------------------------------

        private Control CrearVehiculos()
        {
            PanelLista lista = null;
            lista = new PanelLista(() => _logistica.ObtenerVehiculos(false).Select(VehiculoFila.De).ToList());

            lista.Agregar("New vehicle", fila =>
            {
                if (EditarVehiculo(new Vehiculo { Id = Guid.NewGuid(), Activo = true, Estado = "Disponible" }, true))
                {
                    lista.Recargar();
                }
            }, false, Permiso.LogisticaGestionar);

            lista.Agregar("Edit", fila =>
            {
                Vehiculo vehiculo = _logistica.ObtenerVehiculos(false).First(v => v.Id == Estilo.IdDe(fila).Value);

                if (EditarVehiculo(vehiculo, false))
                {
                    lista.Recargar();
                }
            }, true, Permiso.LogisticaGestionar);

            lista.AgregarExportacion();
            return lista;
        }

        private bool EditarVehiculo(Vehiculo vehiculo, bool nuevo)
        {
            Respuesta datos = Dialogo.Pedir(this, nuevo ? "New vehicle" : "Edit vehicle",
                Campo.Texto("patente", "Plate", vehiculo.Patente ?? "", true, 10),
                Campo.Texto("marca", "Make", vehiculo.Marca ?? "", true, 50),
                Campo.Texto("modelo", "Model", vehiculo.Modelo ?? "", true, 50),
                Campo.Entero("anio", "Year", vehiculo.Anio > 0 ? vehiculo.Anio : DateTime.Today.Year, 1980, 2100),
                Campo.Numero("capacidad", "Capacity (kg)", vehiculo.CapacidadKg, 0m, 100000m, 0),
                Campo.Fecha("seguro", "Insurance expires", vehiculo.VencimientoSeguro ?? DateTime.Today.AddMonths(6), true),
                Campo.Check("activo", "Active", nuevo ? true : vehiculo.Activo));

            if (datos == null)
            {
                return false;
            }

            vehiculo.Patente = datos.Texto("patente").ToUpperInvariant();
            vehiculo.Marca = datos.Texto("marca");
            vehiculo.Modelo = datos.Texto("modelo");
            vehiculo.Anio = datos.Entero("anio");
            vehiculo.CapacidadKg = datos.Numero("capacidad");
            vehiculo.VencimientoSeguro = datos.Fecha("seguro");
            vehiculo.Activo = datos.Check("activo");

            _logistica.GuardarVehiculo(vehiculo);
            return true;
        }

        private Control CrearChoferes()
        {
            PanelLista lista = null;
            lista = new PanelLista(() => _logistica.ObtenerChoferes(false).Select(ChoferFila.De).ToList());

            lista.Agregar("New driver", fila =>
            {
                if (EditarChofer(new Chofer { Id = Guid.NewGuid(), Activo = true }, true))
                {
                    lista.Recargar();
                }
            }, false, Permiso.LogisticaGestionar);

            lista.Agregar("Edit", fila =>
            {
                Chofer chofer = _logistica.ObtenerChoferes(false).First(c => c.Id == Estilo.IdDe(fila).Value);

                if (EditarChofer(chofer, false))
                {
                    lista.Recargar();
                }
            }, true, Permiso.LogisticaGestionar);

            lista.AgregarExportacion();
            return lista;
        }

        private bool EditarChofer(Chofer chofer, bool nuevo)
        {
            Respuesta datos = Dialogo.Pedir(this, nuevo ? "New driver" : "Edit driver",
                Campo.Texto("documento", "ID number", chofer.Documento ?? "", true, 20),
                Campo.Texto("apellido", "Last name", chofer.Apellido ?? "", true, 60),
                Campo.Texto("nombre", "First name", chofer.Nombre ?? "", true, 60),
                Campo.Texto("licencia", "License number", chofer.LicenciaConducir ?? "", true, 30),
                Campo.Texto("categoria", "License category", chofer.CategoriaLicencia ?? "C1", true, 5),
                Campo.Fecha("vencimiento", "License expires", chofer.VencimientoLicencia > DateTime.MinValue ? chofer.VencimientoLicencia : DateTime.Today.AddYears(1)),
                Campo.Texto("telefono", "Phone", chofer.Telefono ?? "", false, 40),
                Campo.Check("activo", "Active", nuevo ? true : chofer.Activo));

            if (datos == null)
            {
                return false;
            }

            chofer.Documento = datos.Texto("documento");
            chofer.Apellido = datos.Texto("apellido");
            chofer.Nombre = datos.Texto("nombre");
            chofer.LicenciaConducir = datos.Texto("licencia");
            chofer.CategoriaLicencia = datos.Texto("categoria").ToUpperInvariant();
            chofer.VencimientoLicencia = datos.Fecha("vencimiento") ?? DateTime.Today;
            chofer.Telefono = datos.Texto("telefono");
            chofer.Activo = datos.Check("activo");

            _logistica.GuardarChofer(chofer);
            return true;
        }
    }

    /// <summary>
    /// Hojas de ruta a la izquierda y mapa del recorrido a la derecha.
    /// </summary>
    public class PanelHojasRuta : UserControl, IRecargable
    {
        private readonly PanelLista _lista;
        private readonly MapaWeb _mapa = new MapaWeb();
        private readonly LogisticaService _servicio;
        private int _version;

        public PanelHojasRuta(PanelLista lista, LogisticaService servicio)
        {
            _lista = lista;
            _servicio = servicio;
            Dock = DockStyle.Fill;

            TableLayoutPanel tabla = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            tabla.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            tabla.Controls.Add(_lista, 0, 0);
            tabla.Controls.Add(_mapa, 1, 0);
            Controls.Add(tabla);

            _lista.FilaCambiada += CargarMapa;
            _mapa.Cargar(string.Empty, null);
        }

        public void Recargar()
        {
            _lista.Recargar();
        }

        private void CargarMapa(object fila)
        {
            int version = ++_version;

            if (fila == null)
            {
                _mapa.Cargar(string.Empty, null);
                return;
            }

            Guid hojaId = Estilo.IdDe(fila).Value;
            string titulo = Tr.T("Route sheet {0}", Estilo.Valor<string>(fila, "Numero"))
                + " - " + Estilo.Valor<string>(fila, "Vehiculo") + " / " + Estilo.Valor<string>(fila, "Chofer");

            System.Threading.Tasks.Task.Run(() => _servicio.ObtenerParadas(hojaId)).ContinueWith(tarea =>
            {
                if (IsDisposed || !IsHandleCreated || version != _version || tarea.IsFaulted)
                {
                    return;
                }

                try
                {
                    BeginInvoke((MethodInvoker)(() =>
                    {
                        string entregado = Etiquetas.De(ResultadoEntrega.EntregadoConforme);
                        string parcial = Etiquetas.De(ResultadoEntrega.EntregaParcial);

                        List<ParadaMapa> paradas = tarea.Result.Select(p => new ParadaMapa
                        {
                            Orden = p.Orden,
                            Titulo = p.Orden + ". " + p.Cliente,
                            Detalle = p.Remito + " - " + p.Direccion,
                            Estado = p.Pendiente ? EstadoParada.Pendiente
                                : (p.Resultado == entregado || p.Resultado == parcial ? EstadoParada.Entregada : EstadoParada.NoEntregada),
                            Lugar = GeoFicticio.Ubicar(p.Direccion, p.Cliente)
                        }).ToList();

                        _mapa.Cargar(titulo, paradas);
                    }));
                }
                catch (InvalidOperationException)
                {
                }
            });
        }
    }
}
