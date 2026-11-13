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
    /// Fila editable de la ventana de faltantes: lo que falta y la cantidad a ingresar.
    /// </summary>
    public class FilaFaltante
    {
        public Guid ProductoId { get; set; }
        public string Sku { get; set; }
        public string Producto { get; set; }
        public decimal Requerido { get; set; }
        public decimal Disponible { get; set; }
        public decimal Faltante { get; set; }
        public decimal Agregar { get; set; }
    }

    /// <summary>
    /// Lista en rojo de los productos que faltan, con un contador por fila para ingresar el stock.
    /// Se usa al confirmar un pedido: se agrega lo que falta y se sigue sin salir de la pantalla.
    /// </summary>
    public class FrmFaltantes : Form
    {
        private readonly BindingList<FilaFaltante> _filas = new BindingList<FilaFaltante>();
        private readonly DataGridView _grilla = Estilo.Grilla();
        private readonly bool _puedeIngresar;

        public FrmFaltantes(string titulo, string explicacion, IEnumerable<FaltanteDto> faltantes, bool puedeIngresar)
        {
            _puedeIngresar = puedeIngresar;
            Text = Tr.T(titulo);
            Size = new Size(920, 520);
            StartPosition = FormStartPosition.CenterParent;
            Edelstahl.WinApp.Ui.Marca.AplicarIcono(this);
            Font = Estilo.Normal;
            BackColor = Estilo.Superficie;
            MinimizeBox = false;
            ShowInTaskbar = false;

            foreach (FaltanteDto f in faltantes)
            {
                _filas.Add(new FilaFaltante
                {
                    ProductoId = f.ProductoId,
                    Sku = f.Sku,
                    Producto = f.Producto,
                    Requerido = f.Requerido,
                    Disponible = f.Disponible,
                    Faltante = f.Faltante,
                    Agregar = f.Faltante
                });
            }

            Label aviso = new Label
            {
                Dock = DockStyle.Top,
                Height = 58,
                Padding = new Padding(12, 10, 12, 0),
                Font = Estilo.Negrita,
                ForeColor = Color.FromArgb(150, 30, 30),
                BackColor = Color.FromArgb(255, 226, 224),
                Text = Tr.T(explicacion)
            };

            _grilla.DataSource = _filas;
            _grilla.ReadOnly = !puedeIngresar;
            _grilla.EditMode = DataGridViewEditMode.EditOnEnter;
            _grilla.DefaultCellStyle.BackColor = Color.FromArgb(255, 238, 236);
            _grilla.DefaultCellStyle.ForeColor = Color.FromArgb(120, 20, 20);
            _grilla.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(255, 230, 227);
            _grilla.DataBindingComplete += (remitente, argumentos) =>
            {
                foreach (DataGridViewColumn columna in _grilla.Columns)
                {
                    columna.HeaderText = Tr.Columna(columna.DataPropertyName);
                    columna.Visible = columna.DataPropertyName != "ProductoId";
                    columna.ReadOnly = columna.DataPropertyName != "Agregar";

                    if (columna.ValueType == typeof(decimal))
                    {
                        columna.DefaultCellStyle.Format = "N2";
                    }

                    if (columna.DataPropertyName == "Agregar")
                    {
                        columna.DefaultCellStyle.BackColor = Color.White;
                        columna.DefaultCellStyle.ForeColor = Color.FromArgb(20, 70, 140);
                        columna.DefaultCellStyle.Font = Estilo.Negrita;
                        columna.HeaderCell.Style.BackColor = Color.FromArgb(46, 125, 50);
                    }
                }
            };
            _grilla.DataError += (remitente, argumentos) => { argumentos.ThrowException = false; };

            Button mas = Estilo.Boton("+ 1");
            mas.MinimumSize = new Size(44, 30);
            mas.Click += (remitente, argumentos) => Sumar(1m);
            Button menos = Estilo.Boton("- 1");
            menos.MinimumSize = new Size(44, 30);
            menos.Click += (remitente, argumentos) => Sumar(-1m);

            Button aceptar = Estilo.Boton("Add stock and confirm");
            aceptar.BackColor = Color.FromArgb(46, 125, 50);
            aceptar.Enabled = puedeIngresar;
            aceptar.Click += (remitente, argumentos) => Aceptar();
            Button cancelar = Estilo.Boton("Cancel");
            cancelar.BackColor = Color.Gray;
            cancelar.DialogResult = DialogResult.Cancel;

            FlowLayoutPanel pie = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(10, 10, 10, 4) };

            if (puedeIngresar)
            {
                pie.Controls.Add(mas);
                pie.Controls.Add(menos);
            }

            pie.Controls.Add(aceptar);
            pie.Controls.Add(cancelar);

            Controls.Add(_grilla);
            Controls.Add(aviso);
            Controls.Add(pie);
            aviso.BringToFront();
            _grilla.BringToFront();
            CancelButton = cancelar;
        }

        /// <summary>Cantidades elegidas para ingresar, por producto.</summary>
        public List<KeyValuePair<Guid, decimal>> Ingresos
        {
            get
            {
                return _filas.Where(f => f.Agregar > 0m).Select(f => new KeyValuePair<Guid, decimal>(f.ProductoId, f.Agregar)).ToList();
            }
        }

        private void Sumar(decimal diferencia)
        {
            if (_grilla.CurrentRow == null)
            {
                return;
            }

            FilaFaltante fila = (FilaFaltante)_grilla.CurrentRow.DataBoundItem;
            _grilla.EndEdit();
            decimal nueva = fila.Agregar + diferencia;
            fila.Agregar = nueva < 0m ? 0m : nueva;
            _filas.ResetItem(_filas.IndexOf(fila));
        }

        private void Aceptar()
        {
            _grilla.EndEdit();

            foreach (FilaFaltante fila in _filas)
            {
                if (fila.Agregar < fila.Faltante)
                {
                    Estilo.Avisar(this, Tr.T("Add at least the missing quantity of {0}.", fila.Sku));
                    return;
                }
            }

            DialogResult = DialogResult.OK;
        }
    }
}
