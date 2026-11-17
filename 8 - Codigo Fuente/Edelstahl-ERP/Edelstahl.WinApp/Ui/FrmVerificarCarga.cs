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
    /// La distribución de los controles está en FrmVerificarCarga.Designer.cs.
    /// </summary>
    public partial class FrmVerificarCarga : Form
    {
        private readonly BindingList<FilaCarga> _filas = new BindingList<FilaCarga>();

        // Constructor sin parámetros: lo usa el diseñador de Visual Studio.
        public FrmVerificarCarga()
        {
            InitializeComponent();
        }

        public FrmVerificarCarga(string pedido, string cliente, IEnumerable<LineaPendienteRemitoDto> lineas) : this()
        {
            Text = Tr.T("Verify the load") + " - " + pedido;
            Edelstahl.WinApp.Ui.Marca.AplicarIcono(this);

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

            lblExplicacion.Text = Tr.T("Customer") + ": " + cliente + Environment.NewLine
                + Tr.T("Check each line against the goods you are loading onto the truck. The quantities come from the invoice and cannot be changed.");

            grilla.DataSource = _filas;
            grilla.ReadOnly = false;
            grilla.DataBindingComplete += (remitente, argumentos) =>
            {
                foreach (DataGridViewColumn columna in grilla.Columns)
                {
                    columna.HeaderText = Tr.Columna(columna.DataPropertyName);
                    columna.ReadOnly = columna.DataPropertyName != "Cargado";

                    if (columna.ValueType == typeof(decimal))
                    {
                        columna.DefaultCellStyle.Format = "N2";
                        columna.DefaultCellStyle.Font = Estilo.Negrita;
                    }
                }

                foreach (DataGridViewRow fila in grilla.Rows)
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
            grilla.CurrentCellDirtyStateChanged += (remitente, argumentos) =>
            {
                if (grilla.IsCurrentCellDirty)
                {
                    grilla.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            grilla.CellValueChanged += (remitente, argumentos) => Actualizar();
            grilla.DataError += (remitente, argumentos) => { argumentos.ThrowException = false; };

            btnGenerar.Text = Tr.T("Create delivery note");
            btnGenerar.Click += (remitente, argumentos) => DialogResult = DialogResult.OK;
            btnCancelar.Text = Tr.T("Cancel");
            lblNotas.Text = Tr.T("Notes");

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
            get { return txtObservaciones.Text.Trim(); }
        }

        private void Actualizar()
        {
            int listas = _filas.Count(f => f.Lista);
            int verificadas = _filas.Count(f => f.Lista && f.Cargado);

            lblResumen.Text = Tr.T("Verified: {0} of {1}", verificadas, listas);
            lblResumen.ForeColor = verificadas == listas && listas > 0 ? Color.FromArgb(46, 125, 50) : Color.FromArgb(176, 58, 46);
            btnGenerar.Enabled = listas > 0 && verificadas == listas;
        }
    }
}
