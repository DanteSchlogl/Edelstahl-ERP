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
    /// La distribución de los controles está en FrmFaltantes.Designer.cs.
    /// </summary>
    public partial class FrmFaltantes : Form
    {
        private readonly BindingList<FilaFaltante> _filas = new BindingList<FilaFaltante>();
        private bool _puedeIngresar;

        // Constructor sin parámetros: lo usa el diseñador de Visual Studio.
        public FrmFaltantes()
        {
            InitializeComponent();
        }

        public FrmFaltantes(string titulo, string explicacion, IEnumerable<FaltanteDto> faltantes, bool puedeIngresar) : this()
        {
            _puedeIngresar = puedeIngresar;
            Text = Tr.T(titulo);
            Edelstahl.WinApp.Ui.Marca.AplicarIcono(this);

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

            lblAviso.Text = Tr.T(explicacion);

            grilla.DataSource = _filas;
            grilla.ReadOnly = !puedeIngresar;
            grilla.DataBindingComplete += (remitente, argumentos) =>
            {
                foreach (DataGridViewColumn columna in grilla.Columns)
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
            grilla.DataError += (remitente, argumentos) => { argumentos.ThrowException = false; };

            btnMas.Click += (remitente, argumentos) => Sumar(1m);
            btnMenos.Click += (remitente, argumentos) => Sumar(-1m);

            btnAceptar.Text = Tr.T("Add stock and confirm");
            btnAceptar.Enabled = puedeIngresar;
            btnAceptar.Click += (remitente, argumentos) => Aceptar();
            btnCancelar.Text = Tr.T("Cancel");

            if (!puedeIngresar)
            {
                flpPie.Controls.Remove(btnMas);
                flpPie.Controls.Remove(btnMenos);
            }
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
            if (grilla.CurrentRow == null)
            {
                return;
            }

            FilaFaltante fila = (FilaFaltante)grilla.CurrentRow.DataBoundItem;
            grilla.EndEdit();
            decimal nueva = fila.Agregar + diferencia;
            fila.Agregar = nueva < 0m ? 0m : nueva;
            _filas.ResetItem(_filas.IndexOf(fila));
        }

        private void Aceptar()
        {
            grilla.EndEdit();

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
