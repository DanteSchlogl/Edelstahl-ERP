using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Services;
using Edelstahl.Domain.Comercial;

namespace Edelstahl.WinApp.Ui
{
    public enum ModoLineas
    {
        Presupuesto,
        Compra,
        Materiales
    }

    /// <summary>
    /// Formulario para armar las lineas de un presupuesto o de una orden de compra.
    /// </summary>
    public class EditorLineas : Form
    {
        private readonly ModoLineas _modo;
        private readonly BindingList<ItemLinea> _items = new BindingList<ItemLinea>();
        private readonly ComboBox _producto = new ComboBox { Width = 340, DropDownStyle = ComboBoxStyle.DropDown };
        private readonly NumericUpDown _cantidad = new NumericUpDown { Width = 80, DecimalPlaces = 2, Minimum = 0.01m, Maximum = 1000000m, Value = 1m };
        private readonly NumericUpDown _precio = new NumericUpDown { Width = 100, DecimalPlaces = 2, Maximum = 1000000000m, ThousandsSeparator = true };
        private readonly NumericUpDown _descuento = new NumericUpDown { Width = 60, DecimalPlaces = 1, Maximum = 100m };
        private readonly DataGridView _grilla = Estilo.Grilla();
        private readonly Label _total = new Label { AutoSize = true, Font = Estilo.Negrita, ForeColor = Estilo.Principal };
        private List<ProductoListaDto> _productos;
        private List<ProductoListaDto> _materiasPrimas = new List<ProductoListaDto>();

        public EditorLineas(ModoLineas modo, string tituloClave, string encabezado = null, decimal cantidadSugerida = 0m, string cliente = null)
        {
            _modo = modo;
            Text = Tr.T(tituloClave) + (string.IsNullOrEmpty(cliente) ? string.Empty : " - " + Tr.T("Customer") + ": " + cliente);
            Size = new Size(940, 580);
            StartPosition = FormStartPosition.CenterParent;
            Font = Estilo.Normal;
            BackColor = Estilo.Superficie;
            MinimizeBox = false;
            ShowInTaskbar = false;

            CatalogoService catalogo = new CatalogoService();

            if (modo == ModoLineas.Materiales)
            {
                // Solo materiales e insumos para fabricar: no los productos terminados del catalogo.
                _productos = new List<ProductoListaDto>();

                foreach (Edelstahl.Domain.Catalogo.TipoProducto tipo in new[]
                {
                    Edelstahl.Domain.Catalogo.TipoProducto.MateriaPrima,
                    Edelstahl.Domain.Catalogo.TipoProducto.InsumoConsumible,
                    Edelstahl.Domain.Catalogo.TipoProducto.Componente,
                    Edelstahl.Domain.Catalogo.TipoProducto.Semielaborado
                })
                {
                    List<ProductoListaDto> grupo = catalogo.BuscarProductos(string.Empty, null, tipo, true, false);
                    _productos.AddRange(grupo);

                    if (tipo == Edelstahl.Domain.Catalogo.TipoProducto.MateriaPrima)
                    {
                        _materiasPrimas = grupo;
                    }
                }

                _productos = _productos.OrderBy(p => p.Sku).ToList();
            }
            else
            {
                _productos = catalogo.BuscarProductos(string.Empty, null, null, true, modo == ModoLineas.Presupuesto);
            }

            foreach (ProductoListaDto producto in _productos)
            {
                _producto.Items.Add(new Opcion(producto.Sku + "  " + producto.Nombre, producto));
            }

            _producto.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            _producto.AutoCompleteSource = AutoCompleteSource.ListItems;
            _producto.SelectedIndexChanged += (remitente, argumentos) => PonerPrecio();

            FlowLayoutPanel entrada = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(10, 10, 10, 4) };
            FlowLayoutPanel fila1 = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
            FlowLayoutPanel fila2 = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 6, 0, 0) };
            entrada.Controls.Add(fila1);
            entrada.Controls.Add(fila2);

            fila1.Controls.Add(Etiqueta("Product"));
            fila1.Controls.Add(_producto);
            fila1.Controls.Add(Etiqueta("Quantity"));
            fila1.Controls.Add(_cantidad);
            if (modo != ModoLineas.Materiales)
            {
                fila1.Controls.Add(Etiqueta(modo == ModoLineas.Presupuesto ? "Unit price" : "Unit cost"));
                fila1.Controls.Add(_precio);
                fila1.Controls.Add(Etiqueta("Discount %"));
                fila1.Controls.Add(_descuento);
            }

            Button agregar = Estilo.Boton("Add line");
            agregar.Click += (remitente, argumentos) => AgregarLinea();
            fila2.Controls.Add(agregar);

            if (modo == ModoLineas.Presupuesto)
            {
                Button especial = Estilo.Boton("Add custom manufacturing");
                especial.Click += (remitente, argumentos) => AgregarEspecial();
                fila2.Controls.Add(especial);
            }

            Button mas = Estilo.Boton("+ 1");
            mas.MinimumSize = new Size(44, 30);
            mas.Click += (remitente, argumentos) => CambiarCantidad(1m);
            Button menos = Estilo.Boton("- 1");
            menos.MinimumSize = new Size(44, 30);
            menos.Click += (remitente, argumentos) => CambiarCantidad(-1m);
            Button exacta = Estilo.Boton("Change quantity");
            exacta.Click += (remitente, argumentos) => PedirCantidad();
            Button modificar = Estilo.Boton("Edit line");
            modificar.Click += (remitente, argumentos) => ModificarLinea();
            fila2.Controls.Add(mas);
            fila2.Controls.Add(menos);
            fila2.Controls.Add(exacta);
            fila2.Controls.Add(modificar);

            Button quitar = Estilo.Boton("Remove line");
            quitar.BackColor = Color.Gray;
            quitar.Click += (remitente, argumentos) =>
            {
                if (_grilla.CurrentRow != null)
                {
                    _items.Remove((ItemLinea)_grilla.CurrentRow.DataBoundItem);
                    Totalizar();
                }
            };
            fila2.Controls.Add(quitar);

            _grilla.CellDoubleClick += (remitente, argumentos) => { if (argumentos.RowIndex >= 0) { ModificarLinea(); } };

            _grilla.DataSource = _items;
            _grilla.DataBindingComplete += (remitente, argumentos) =>
            {
                foreach (DataGridViewColumn columna in _grilla.Columns)
                {
                    columna.HeaderText = Tr.Columna(columna.DataPropertyName);
                    columna.Visible = columna.DataPropertyName != "ProductoId" && columna.DataPropertyName != "Tipo"
                        && !(modo == ModoLineas.Materiales && (columna.DataPropertyName == "Precio" || columna.DataPropertyName == "Descuento" || columna.DataPropertyName == "Subtotal"));

                    if (columna.ValueType == typeof(decimal))
                    {
                        columna.DefaultCellStyle.Format = "N2";
                    }
                }
            };

            Button aceptar = Estilo.Boton("Accept");
            Button cancelar = Estilo.Boton("Cancel");
            cancelar.BackColor = Color.Gray;
            cancelar.DialogResult = DialogResult.Cancel;
            aceptar.Click += (remitente, argumentos) =>
            {
                if (_items.Count == 0)
                {
                    Estilo.Avisar(this, Tr.T("Add at least one line."));
                    return;
                }

                DialogResult = DialogResult.OK;
            };

            FlowLayoutPanel pie = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(10, 8, 10, 4) };
            pie.Controls.Add(aceptar);
            pie.Controls.Add(cancelar);
            pie.Controls.Add(_total);
            _total.Margin = new Padding(20, 8, 0, 0);

            Controls.Add(_grilla);
            Controls.Add(entrada);
            Controls.Add(pie);

            if (!string.IsNullOrEmpty(encabezado))
            {
                Label cabecera = new Label
                {
                    Dock = DockStyle.Top,
                    Height = 64,
                    Padding = new Padding(12, 10, 12, 0),
                    Font = Estilo.Negrita,
                    ForeColor = Estilo.Principal,
                    BackColor = Color.FromArgb(226, 234, 247),
                    Text = encabezado
                };

                Controls.Add(cabecera);
                cabecera.BringToFront();
            }

            entrada.BringToFront();
            _grilla.BringToFront();
            AcceptButton = null;
            CancelButton = cancelar;

            if (modo == ModoLineas.Materiales)
            {
                _total.Visible = false;

                // Sugerencia: el material con mas existencia, en la cantidad pedida (se puede cambiar).
                ProductoListaDto sugerido = _materiasPrimas.OrderByDescending(p => p.Disponible).FirstOrDefault()
                    ?? _productos.OrderByDescending(p => p.Disponible).FirstOrDefault();

                if (sugerido != null && cantidadSugerida > 0m)
                {
                    _items.Add(new ItemLinea
                    {
                        ProductoId = sugerido.Id,
                        Codigo = sugerido.Sku,
                        Descripcion = sugerido.Nombre,
                        Cantidad = cantidadSugerida,
                        Precio = sugerido.CostoActual,
                        Tipo = (int)TipoItemPresupuesto.Producto
                    });
                }
            }

            Totalizar();
        }

        /// <summary>Vuelve a cargar lineas ya armadas (para no perderlas si el guardado falla).</summary>
        public void PonerItems(IEnumerable<ItemLinea> items)
        {
            foreach (ItemLinea item in items)
            {
                _items.Add(item);
            }

            Totalizar();
        }

        public List<ItemLinea> Items
        {
            get { return _items.ToList(); }
        }

        private Label Etiqueta(string clave)
        {
            return new Label { Text = Tr.T(clave), AutoSize = true, Margin = new Padding(8, 7, 3, 0) };
        }

        private ProductoListaDto Elegido()
        {
            Opcion opcion = _producto.SelectedItem as Opcion;
            return opcion == null ? null : (ProductoListaDto)opcion.Valor;
        }

        private void PonerPrecio()
        {
            ProductoListaDto producto = Elegido();

            if (producto != null)
            {
                decimal valor = _modo == ModoLineas.Presupuesto ? producto.PrecioBase : producto.CostoActual;
                _precio.Value = Math.Min(Math.Max(valor, _precio.Minimum), _precio.Maximum);
            }
        }

        private void AgregarLinea()
        {
            ProductoListaDto producto = Elegido();

            if (producto == null)
            {
                Estilo.Avisar(this, Tr.T("Select a product from the list."));
                return;
            }

            if (_modo != ModoLineas.Materiales && _precio.Value <= 0m)
            {
                Estilo.Avisar(this, Tr.T("Enter a unit price greater than zero."));
                return;
            }

            // Si el producto ya esta con el mismo precio y descuento, se suma la cantidad a esa linea.
            ItemLinea existente = _items.FirstOrDefault(i => i.ProductoId == producto.Id
                && i.Tipo == (int)TipoItemPresupuesto.Producto
                && i.Precio == _precio.Value && i.Descuento == _descuento.Value);

            if (existente != null)
            {
                existente.Cantidad += _cantidad.Value;
                _items.ResetItem(_items.IndexOf(existente));
            }
            else
            {
                _items.Add(new ItemLinea
                {
                    ProductoId = producto.Id,
                    Codigo = producto.Sku,
                    Descripcion = producto.Nombre,
                    Cantidad = _cantidad.Value,
                    Precio = _precio.Value,
                    Descuento = _descuento.Value,
                    Tipo = (int)TipoItemPresupuesto.Producto
                });
            }

            Totalizar();
        }

        private ItemLinea LineaActual()
        {
            if (_grilla.CurrentRow == null)
            {
                Estilo.Avisar(this, Tr.T("Select a line first."));
                return null;
            }

            return (ItemLinea)_grilla.CurrentRow.DataBoundItem;
        }

        private void CambiarCantidad(decimal diferencia)
        {
            ItemLinea linea = LineaActual();

            if (linea == null)
            {
                return;
            }

            // Cada articulo es independiente: restar nunca baja de cero ni borra la linea.
            decimal nueva = linea.Cantidad + diferencia;

            if (nueva <= 0m)
            {
                return;
            }

            AplicarCantidad(linea, nueva);
        }

        /// <summary>
        /// Cambia cantidad, precio y descuento de la linea elegida (y la descripcion si es fabricacion especial).
        /// </summary>
        private void ModificarLinea()
        {
            ItemLinea linea = LineaActual();

            if (linea == null)
            {
                return;
            }

            bool especial = linea.Tipo == (int)TipoItemPresupuesto.FabricacionEspecial;
            string descripcion = linea.Descripcion ?? string.Empty;
            string tecnica = string.Empty;

            if (especial)
            {
                int corte = descripcion.IndexOf(" | ", StringComparison.Ordinal);

                if (corte >= 0)
                {
                    tecnica = descripcion.Substring(corte + 3);
                    descripcion = descripcion.Substring(0, corte);
                }
            }

            List<Campo> campos = new List<Campo>();

            if (especial)
            {
                campos.Add(Campo.Texto("descripcion", "Description", descripcion, true, 200));
                campos.Add(Campo.Multilinea("tecnica", "Technical description", tecnica, true));
            }

            campos.Add(Campo.Numero("cantidad", "Quantity", linea.Cantidad, 0.01m, 1000000m));

            if (_modo != ModoLineas.Materiales)
            {
                campos.Add(Campo.Numero("precio", "Unit price", linea.Precio, 0m, 1000000000m).Positivo());
                campos.Add(Campo.Numero("descuento", "Discount %", linea.Descuento, 0m, 100m));
            }

            Respuesta datos = Dialogo.Pedir(this, "Edit line", campos.ToArray());

            if (datos == null)
            {
                return;
            }

            int posicion = _items.IndexOf(linea);
            linea.Cantidad = datos.Numero("cantidad");

            if (_modo != ModoLineas.Materiales)
            {
                linea.Precio = datos.Numero("precio");
                linea.Descuento = datos.Numero("descuento");
            }

            if (especial)
            {
                linea.Descripcion = datos.Texto("descripcion") + " | " + datos.Texto("tecnica");
            }

            _items.ResetItem(posicion);
            _grilla.CurrentCell = _grilla.Rows[posicion].Cells.Cast<DataGridViewCell>().FirstOrDefault(c => c.Visible);
            Totalizar();
        }

        private void PedirCantidad()
        {
            ItemLinea linea = LineaActual();

            if (linea == null)
            {
                return;
            }

            Respuesta datos = Dialogo.Pedir(this, "Change quantity",
                Campo.Numero("cantidad", "Quantity", linea.Cantidad, 0.01m, 1000000m));

            if (datos != null)
            {
                AplicarCantidad(linea, datos.Numero("cantidad"));
            }
        }

        private void AplicarCantidad(ItemLinea linea, decimal cantidad)
        {
            int posicion = _items.IndexOf(linea);

            linea.Cantidad = cantidad;
            _items.ResetItem(posicion);
            _grilla.CurrentCell = _grilla.Rows[posicion].Cells.Cast<DataGridViewCell>().FirstOrDefault(c => c.Visible);

            Totalizar();
        }

        private void AgregarEspecial()
        {
            // Una fabricacion especial se hace sobre un producto fabricable del catalogo: hay que elegirlo.
            List<ProductoListaDto> fabricables = new CatalogoService().ObtenerFabricables();

            if (fabricables.Count == 0)
            {
                Estilo.Avisar(this, Tr.T("There are no products that can be manufactured to measure."));
                return;
            }

            List<Opcion> opciones = fabricables.Select(p => new Opcion(p.Sku + "  " + p.Nombre, p)).ToList();

            Respuesta datos = Dialogo.Pedir(this, "Custom manufacturing",
                "A custom manufacturing line requires a deposit of at least 30%.",
                Campo.Combo("producto", "Product to manufacture", opciones),
                Campo.Texto("descripcion", "Description", "", true, 200),
                Campo.Multilinea("tecnica", "Technical description", "", true),
                Campo.Numero("cantidad", "Quantity", 1m, 0.01m, 1000000m),
                Campo.Numero("precio", "Unit price", 0m, 0m, 1000000000m).Positivo());

            if (datos == null)
            {
                return;
            }

            ProductoListaDto baseProducto = (ProductoListaDto)datos.Seleccion("producto");

            _items.Add(new ItemLinea
            {
                ProductoId = baseProducto.Id,
                Codigo = baseProducto.Sku,
                Descripcion = datos.Texto("descripcion") + " | " + datos.Texto("tecnica"),
                Cantidad = datos.Numero("cantidad"),
                Precio = datos.Numero("precio"),
                Tipo = (int)TipoItemPresupuesto.FabricacionEspecial
            });

            Totalizar();
        }

        private void Totalizar()
        {
            _total.Text = Tr.T("Net total: {0}", _items.Sum(i => i.Subtotal).ToString("N2"));
        }
    }
}
