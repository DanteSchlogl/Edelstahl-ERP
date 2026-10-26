using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Edelstahl.BLL.DTOs;

namespace Edelstahl.WinApp.Ui
{
    /// <summary>
    /// Fila de la verificacion de carga: lo que se factura y se envia, y si ya se controlo en el camion.
    /// </summary>
    public class FilaCarga
    {
        public bool Cargado { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public string Estado { get; set; }

        internal Guid DetallePedidoId { get; set; }
        internal bool Lista { get; set; }
    }

    /// <summary>
    /// Muestra lo que va a salir en el remito y obliga a verificarlo linea por linea al cargar el camion.
    /// Las cantidades vienen de lo facturado y reservado: no se pueden modificar.
    /// </summary>
    public class FrmVerificarCarga : Form
    {
        private readonly BindingList<FilaCarga> _filas = new BindingList<FilaCarga>();
        private readonly DataGridView _grilla = Estilo.Grilla();
        private readonly Label _resumen = new Label { AutoSize = true, Font = Estilo.Negrita, ForeColor = Estilo.Principal };
        private readonly TextBox _observaciones = new TextBox { Width = 560 };
        private readonly Button _generar;

        public FrmVerificarCarga(string pedido, string cliente, IEnumerable<LineaPendienteRemitoDto> lineas)
        {
            Text = Tr.T("Verify the load") + " - " + pedido;
            Size = new Size(900, 560);
            StartPosition = FormStartPosition.CenterParent;
            Font = Estilo.Normal;
            BackColor = Estilo.Superficie;
            MinimizeBox = false;
            ShowInTaskbar = false;

            foreach (LineaPendienteRemitoDto l in lineas)
            {
                bool lista = l.ListaParaDespacho > 0m;

                _filas.Add(new FilaCarga
                {
                    Codigo = l.Codigo,
                    Descripcion = l.Descripcion,
                    Cantidad = lista ? l.ListaParaDespacho : l.Pendiente,
                    Estado = lista ? string.Empty : Tr.T("Not ready to ship"),
                    DetallePedidoId = l.DetallePedidoId,
                    Lista = lista
                });
            }

            Label explicacion = new Label
            {
                Dock = DockStyle.Top,
                Height = 62,
                Padding = new Padding(12, 8, 12, 0),
                ForeColor = Estilo.Principal,
                BackColor = Color.FromArgb(226, 234, 247),
                Text = Tr.T("Customer") + ": " + cliente + Environment.NewLine
                    + Tr.T("Check each line against the goods you are loading onto the truck. The quantities come from the invoice and cannot be changed.")
            };

            _grilla.DataSource = _filas;
            _grilla.ReadOnly = false;
            _grilla.EditMode = DataGridViewEditMode.EditOnEnter;
            _grilla.DataBindingComplete += (remitente, argumentos) =>
            {
                foreach (DataGridViewColumn columna in _grilla.Columns)
                {
                    columna.HeaderText = Tr.Columna(columna.DataPropertyName);
                    columna.ReadOnly = columna.DataPropertyName != "Cargado";

                    if (columna.ValueType == typeof(decimal))
                    {
                        columna.DefaultCellStyle.Format = "N2";
                        columna.DefaultCellStyle.Font = Estilo.Negrita;
                    }
                }

                foreach (DataGridViewRow fila in _grilla.Rows)
                {
                    FilaCarga carga = (FilaCarga)fila.DataBoundItem;

                    if (!carga.Lista)
                    {
                        fila.DefaultCellStyle.ForeColor = Color.Gray;
                        fila.DefaultCellStyle.BackColor = Color.FromArgb(236, 236, 236);
                        fila.Cells[0].ReadOnly = true;
                    }
                }
            };

            // La casilla se aplica al instante, sin esperar a salir de la celda.
            _grilla.CurrentCellDirtyStateChanged += (remitente, argumentos) =>
            {
                if (_grilla.IsCurrentCellDirty)
                {
                    _grilla.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            _grilla.CellValueChanged += (remitente, argumentos) => Actualizar();
            _grilla.DataError += (remitente, argumentos) => { argumentos.ThrowException = false; };

            _generar = Estilo.Boton("Create delivery note");
            _generar.BackColor = Color.FromArgb(46, 125, 50);
            _generar.Click += (remitente, argumentos) => DialogResult = DialogResult.OK;
            Button cancelar = Estilo.Boton("Cancel");
            cancelar.BackColor = Color.Gray;
            cancelar.DialogResult = DialogResult.Cancel;

            FlowLayoutPanel pie = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 96, Padding = new Padding(10, 6, 10, 4) };
            FlowLayoutPanel notas = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
            notas.Controls.Add(new Label { Text = Tr.T("Notes"), AutoSize = true, Margin = new Padding(0, 6, 8, 0) });
            notas.Controls.Add(_observaciones);
            FlowLayoutPanel acciones = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 6, 0, 0) };
            acciones.Controls.Add(_generar);
            acciones.Controls.Add(cancelar);
            acciones.Controls.Add(_resumen);
            _resumen.Margin = new Padding(16, 8, 0, 0);
            pie.FlowDirection = FlowDirection.TopDown;
            pie.Controls.Add(notas);
            pie.Controls.Add(acciones);

            Controls.Add(_grilla);
            Controls.Add(explicacion);
            Controls.Add(pie);
            explicacion.BringToFront();
            _grilla.BringToFront();
            CancelButton = cancelar;
            Actualizar();
        }

        /// <summary>Lineas que van en el remito, con la cantidad exacta que se facturo y esta lista.</summary>
        public List<LineaRemitoSolicitud> Lineas
        {
            get
            {
                return _filas.Where(f => f.Lista && f.Cargado)
                    .Select(f => new LineaRemitoSolicitud { DetallePedidoId = f.DetallePedidoId, Cantidad = f.Cantidad }).ToList();
            }
        }

        public string Observaciones
        {
            get { return _observaciones.Text.Trim(); }
        }

        private void Actualizar()
        {
            int listas = _filas.Count(f => f.Lista);
            int verificadas = _filas.Count(f => f.Lista && f.Cargado);

            _resumen.Text = Tr.T("Verified: {0} of {1}", verificadas, listas);
            _resumen.ForeColor = verificadas == listas && listas > 0 ? Color.FromArgb(46, 125, 50) : Color.FromArgb(176, 58, 46);
            _generar.Enabled = listas > 0 && verificadas == listas;
        }
    }
}
