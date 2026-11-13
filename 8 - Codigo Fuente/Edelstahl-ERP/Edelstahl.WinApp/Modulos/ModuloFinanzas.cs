using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Services;
using Edelstahl.Domain.Finanzas;
using Edelstahl.Domain.Security;
using Edelstahl.WinApp.Ui;

namespace Edelstahl.WinApp.Modulos
{
    /// <summary>
    /// Finanzas: facturas, cobros, pagos a proveedores, notas de credito y cuentas a cobrar y pagar.
    /// </summary>
    public class ModuloFinanzas : ModuloBase
    {
        private readonly FacturacionService _facturacion = new FacturacionService();
        private readonly FinanzasService _finanzas = new FinanzasService();
        private readonly ReporteService _reportes = new ReporteService();
        private readonly DocumentosService _documentos = new DocumentosService();

        public ModuloFinanzas()
        {
            AgregarPestana("Invoices", CrearFacturas(), Permiso.PedidosVer);
            AgregarPestana("Collections", CrearCobros(), Permiso.CuentaCorrienteVer);
            AgregarPestana("Credit notes", CrearNotas(), Permiso.PedidosVer);
            AgregarPestana("Supplier payments", CrearPagos(), Permiso.ComprasVer);
            AgregarPestana("To collect (customers)", CrearPorCobrar(), Permiso.CuentaCorrienteVer);
            AgregarPestana("To pay (suppliers)", CrearPorPagar(), Permiso.ReportesGerenciales);
            AgregarPestana("Payment methods", CrearMedios(), Permiso.CobrosRegistrar);
            CargarPestanaActual();
        }

        private Control CrearFacturas()
        {
            PanelLista lista = null;
            lista = new PanelLista(() => _facturacion.ObtenerFacturas(null, null, false));

            lista.Agregar("Print invoice", fila =>
            {
                Estilo.AbrirArchivo(this, _documentos.GenerarFactura(Estilo.IdDe(fila).Value));
            }, true, Permiso.PedidosVer);

            lista.Agregar("Register collection", fila =>
            {
                CobroCliente cobro = DialogoCobro.Registrar(this, _finanzas, Estilo.Valor<Guid>(fila, "ClienteId"), Estilo.IdDe(fila).Value, Estilo.Valor<decimal>(fila, "Saldo"));

                if (cobro == null)
                {
                    return;
                }

                Estilo.Informar(this, Tr.T("Collection {0} registered.", cobro.Numero));
                lista.Recargar();
            }, true, Permiso.CobrosRegistrar);

            lista.Agregar("Issue credit note", fila =>
            {
                Respuesta datos = Dialogo.Pedir(this, "Issue credit note",
                    Campo.Numero("monto", "Net amount", 0m),
                    Campo.Texto("motivo", "Reason", "", true, 200));

                if (datos == null)
                {
                    return;
                }

                NotaCredito nota = _facturacion.EmitirNotaCredito(Estilo.IdDe(fila).Value, null, datos.Numero("monto"), datos.Texto("motivo"));
                Estilo.AbrirArchivo(this, _documentos.GenerarNotaCredito(nota.Id));
                lista.Recargar();
            }, true, Permiso.FacturasEmitir);

            lista.AgregarExportacion();
            return lista;
        }

        private Control CrearCobros()
        {
            PanelLista lista = new PanelLista(() => _finanzas.ObtenerCobros(null));
            lista.AgregarExportacion();
            return lista;
        }

        private Control CrearNotas()
        {
            PanelLista lista = new PanelLista(() => _facturacion.ObtenerNotasCredito(null));

            lista.Agregar("Print credit note", fila =>
            {
                Estilo.AbrirArchivo(this, _documentos.GenerarNotaCredito(Estilo.IdDe(fila).Value));
            }, true, Permiso.PedidosVer);

            lista.AgregarExportacion();
            return lista;
        }

        private Control CrearPagos()
        {
            PanelLista lista = new PanelLista(() => _finanzas.ObtenerPagos(null));
            lista.AgregarExportacion();
            return lista;
        }

        private Control CrearPorCobrar()
        {
            PanelLista lista = null;
            lista = new PanelLista(() => _finanzas.ObtenerCobrosPendientes());

            lista.Agregar("Register collection", fila =>
            {
                CobroCliente cobro = DialogoCobro.Registrar(this, _finanzas, Estilo.Valor<Guid>(fila, "ClienteId"),
                    Estilo.Valor<Guid>(fila, "FacturaId"), Estilo.Valor<decimal>(fila, "Falta"));

                if (cobro != null)
                {
                    Estilo.Informar(this, Tr.T("Collection {0} registered.", cobro.Numero));
                    lista.Recargar();
                }
            }, true, Permiso.CobrosRegistrar);

            lista.Agregar("Expected payment method", fila =>
            {
                List<Opcion> formas = new List<Opcion>
                {
                    new Opcion(Tr.T("Cash"), Tr.T("Cash")),
                    new Opcion(Tr.T("Bank transfer"), Tr.T("Bank transfer")),
                    new Opcion(Tr.T("Check"), Tr.T("Check")),
                    new Opcion(Tr.T("Card"), Tr.T("Card")),
                    new Opcion(Tr.T("Cash on delivery"), Tr.T("Cash on delivery"))
                };

                Respuesta datos = Dialogo.Pedir(this, "Expected payment method",
                    "Note how the customer plans to pay the balance. It does not register any collection.",
                    Campo.Combo("forma", "Payment method", formas),
                    Campo.Texto("detalle", "Notes", "", false, 100));

                if (datos == null)
                {
                    return;
                }

                string forma = Convert.ToString(datos.Seleccion("forma"));
                string detalle = datos.Texto("detalle");
                _finanzas.ActualizarFormaDePago(Estilo.Valor<Guid>(fila, "PedidoId"), detalle.Length == 0 ? forma : forma + " - " + detalle);
                lista.Recargar();
            }, true, Permiso.CobrosRegistrar);

            lista.AgregarExportacion();
            return lista;
        }

        private Control CrearPorPagar()
        {
            PanelLista lista = new PanelLista(() => _reportes.CuentasPorPagar());
            lista.AgregarExportacion();
            return lista;
        }

        private Control CrearMedios()
        {
            PanelLista lista = new PanelLista(() => _finanzas.ObtenerMediosPago(false).Select(m => new MedioPagoFila
            {
                Id = m.Id,
                Codigo = m.Codigo,
                Nombre = m.Nombre,
                RequiereReferencia = m.RequiereReferencia,
                Activo = m.Activo
            }).ToList(), false);

            return lista;
        }
    }

    /// <summary>
    /// Reportes de gestion con rango de fechas y exportacion.
    /// </summary>
    public class ModuloReportes : ModuloBase
    {
        public ModuloReportes()
        {
            AgregarPestana("Indicators", new PanelIndicadores(), Permiso.ReportesVer);
            AgregarPestana("Reports", new PanelReportes(), Permiso.ReportesVer);
            CargarPestanaActual();
        }
    }

    public class PanelIndicadores : UserControl, IRecargable
    {
        private readonly FlowLayoutPanel _tarjetas = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(16) };
        private readonly PanelLista _alertas;

        public PanelIndicadores()
        {
            Dock = DockStyle.Fill;
            BackColor = Estilo.Fondo;

            _alertas = new PanelLista(() => new InventarioService().ObtenerAlertas(), false);
            _alertas.AgregarExportacion();

            Label titulo = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(16, 6, 0, 0),
                Font = Estilo.Negrita,
                Text = Tr.T("Products below the reorder point")
            };

            Controls.Add(_alertas);
            Controls.Add(titulo);
            Controls.Add(_tarjetas);
            _tarjetas.BringToFront();
            titulo.BringToFront();
            _alertas.BringToFront();
        }

        public void Recargar()
        {
            IndicadoresDto datos = null;

            try
            {
                datos = new ReporteService().ObtenerIndicadores();
            }
            catch (Exception)
            {
                return;
            }

            _tarjetas.Controls.Clear();
            Tarjeta("Sales this month", datos.VentasDelMes.ToString("N2"));
            Tarjeta("Active orders", datos.PedidosActivos.ToString());
            Tarjeta("Orders in production", datos.OrdenesEnProceso.ToString());
            Tarjeta("Open invoices", datos.FacturasPendientes.ToString());
            Tarjeta("Receivable", datos.CuentasPorCobrar.ToString("N2"));
            Tarjeta("Payable", datos.CuentasPorPagar.ToString("N2"));
            Tarjeta("Stock alerts", datos.ProductosEnAlerta.ToString());
            Tarjeta("Deliveries this month", datos.EntregasDelMes.ToString());
            Tarjeta("Open purchase orders", datos.OrdenesDeCompraAbiertas.ToString());

            _alertas.Recargar();
        }

        private void Tarjeta(string clave, string valor)
        {
            Panel tarjeta = new Panel
            {
                Width = 200,
                Height = 78,
                BackColor = System.Drawing.Color.White,
                Margin = new Padding(0, 0, 12, 12),
                Padding = new Padding(10)
            };

            Label etiqueta = new Label { Text = Tr.T(clave), Dock = DockStyle.Top, Height = 22, ForeColor = Estilo.Lateral };
            Label cifra = new Label { Text = valor, Dock = DockStyle.Fill, Font = new System.Drawing.Font("Segoe UI", 15f, System.Drawing.FontStyle.Bold), ForeColor = Estilo.Principal };
            tarjeta.Controls.Add(cifra);
            tarjeta.Controls.Add(etiqueta);
            _tarjetas.Controls.Add(tarjeta);
        }
    }

    public class PanelReportes : UserControl, IRecargable
    {
        private readonly ReporteService _servicio = new ReporteService();
        private readonly ComboBox _tipo = new ComboBox { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly DateTimePicker _desde = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Today.Year, 1, 1) };
        private readonly DateTimePicker _hasta = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
        private readonly PanelLista _lista;
        private int _indice;
        private DateTime _fDesde = new DateTime(DateTime.Today.Year, 1, 1);
        private DateTime _fHasta = DateTime.Today;

        public PanelReportes()
        {
            Dock = DockStyle.Fill;

            _lista = new PanelLista(Ejecutar, false);
            _lista.AgregarExportacion();

            _tipo.Items.AddRange(new object[]
            {
                Tr.T("Sales by period"), Tr.T("Sales by customer"), Tr.T("Sales by product"),
                Tr.T("Stock valuation"), Tr.T("Receivables"), Tr.T("Payables")
            });
            _tipo.SelectedIndex = 0;

            FlowLayoutPanel filtros = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8, 6, 8, 0), BackColor = Estilo.Superficie };
            filtros.Controls.Add(new Label { Text = Tr.T("Report"), AutoSize = true, Margin = new Padding(6, 7, 3, 0) });
            filtros.Controls.Add(_tipo);
            filtros.Controls.Add(new Label { Text = Tr.T("From"), AutoSize = true, Margin = new Padding(10, 7, 3, 0) });
            filtros.Controls.Add(_desde);
            filtros.Controls.Add(new Label { Text = Tr.T("To"), AutoSize = true, Margin = new Padding(10, 7, 3, 0) });
            filtros.Controls.Add(_hasta);

            Button ejecutar = Estilo.Boton("Run");
            ejecutar.Margin = new Padding(10, 0, 0, 0);
            ejecutar.Click += (remitente, argumentos) => Recargar();
            filtros.Controls.Add(ejecutar);

            Controls.Add(_lista);
            Controls.Add(filtros);
            filtros.BringToFront();
            _lista.BringToFront();
        }

        public void Recargar()
        {
            _indice = _tipo.SelectedIndex;
            _fDesde = _desde.Value;
            _fHasta = _hasta.Value;
            _lista.Recargar();
        }

        private IList Ejecutar()
        {
            switch (_indice)
            {
                case 1: return _servicio.VentasPorCliente(_fDesde, _fHasta);
                case 2: return _servicio.VentasPorProducto(_fDesde, _fHasta);
                case 3: return _servicio.StockValorizado();
                case 4: return _servicio.CuentasPorCobrar();
                case 5: return _servicio.CuentasPorPagar();
                default: return _servicio.VentasPorPeriodo(_fDesde, _fHasta);
            }
        }
    }
}
