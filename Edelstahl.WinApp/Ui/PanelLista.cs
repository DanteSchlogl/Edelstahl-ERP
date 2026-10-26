using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Edelstahl.BLL.Services;
using Edelstahl.Domain.Security;
using Edelstahl.Services.Export;
using Edelstahl.Services.Localization;

namespace Edelstahl.WinApp.Ui
{
    /// <summary>
    /// Lista con barra de acciones, busqueda, detalle opcional y exportacion.
    /// Los datos se leen en un hilo aparte para no congelar la ventana.
    /// </summary>
    public class PanelLista : UserControl
    {
        private readonly Func<IList> _origen;
        private readonly DataGridView _grilla;
        private readonly FlowLayoutPanel _barra;
        private readonly Label _estado;
        private readonly TextBox _buscar;
        private readonly Label _etiquetaBuscar;
        private readonly List<Button> _botonesSeleccion = new List<Button>();
        private readonly Dictionary<Button, Func<bool>> _habilitaciones = new Dictionary<Button, Func<bool>>();

        private DataGridView _detalle;
        private Func<object, IList> _origenDetalle;
        private Func<object, string> _tituloDetalle;
        private Label _rotuloDetalle;
        private SplitContainer _division;
        private IList _datos = new ArrayList();
        private int _version;
        private int _versionDetalle;

        public PanelLista(Func<IList> origen, bool conBusqueda = true)
        {
            _origen = origen;
            Dock = DockStyle.Fill;
            BackColor = Estilo.Fondo;

            _barra = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(8, 8, 8, 4),
                WrapContents = true
            };

            _etiquetaBuscar = new Label { Text = Tr.T("Search"), AutoSize = true, Margin = new Padding(8, 7, 4, 0), Visible = conBusqueda };
            _buscar = new TextBox { Width = 180, Margin = new Padding(0, 3, 12, 0), Visible = conBusqueda };
            _buscar.TextChanged += (remitente, argumentos) => Mostrar();

            _barra.Controls.Add(_etiquetaBuscar);
            _barra.Controls.Add(_buscar);

            Button actualizar = Estilo.Boton("Refresh");
            actualizar.BackColor = Color.FromArgb(120, 130, 150);
            actualizar.Click += (remitente, argumentos) => Recargar();
            _barra.Controls.Add(actualizar);

            _estado = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 22,
                Padding = new Padding(10, 3, 0, 0),
                ForeColor = Estilo.Principal
            };

            _grilla = Estilo.Grilla();
            _grilla.DataBindingComplete += (remitente, argumentos) =>
            {
                AjustarColumnas(_grilla);
                AlCambiarSeleccion();
            };
            _grilla.SelectionChanged += (remitente, argumentos) => AlCambiarSeleccion();
            _grilla.CurrentCellChanged += (remitente, argumentos) => AlCambiarSeleccion();
            _grilla.CellDoubleClick += (remitente, argumentos) =>
            {
                if (argumentos.RowIndex >= 0 && DobleClic != null)
                {
                    DobleClic(Seleccionado);
                }
            };

            Controls.Add(_grilla);
            Controls.Add(_barra);
            Controls.Add(_estado);
            _barra.BringToFront();
            _grilla.BringToFront();

            LanguageService.IdiomaCambiado += AlCambiarIdioma;
            Disposed += (remitente, argumentos) => LanguageService.IdiomaCambiado -= AlCambiarIdioma;
        }

        // F5 recarga la lista desde cualquier parte del panel.
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F5)
            {
                Recargar();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private object _filaNotificada;

        /// <summary>Se produce cuando cambia la fila elegida (o llegan datos nuevos).</summary>
        public event Action<object> FilaCambiada;

        public Action<object> DobleClic { get; set; }

        public object Seleccionado
        {
            get
            {
                // Al ocultar la columna del Id la grilla puede quedar sin celda actual
                // aunque haya una fila marcada, por eso se mira primero la seleccion.
                if (_grilla.SelectedRows.Count > 0)
                {
                    return _grilla.SelectedRows[0].DataBoundItem;
                }

                return _grilla.CurrentRow == null ? null : _grilla.CurrentRow.DataBoundItem;
            }
        }

        public Guid? IdSeleccionado
        {
            get { return Estilo.IdDe(Seleccionado); }
        }

        public IList Datos
        {
            get { return _datos; }
        }

        public TextBox CuadroBusqueda
        {
            get { return _buscar; }
        }

        /// <summary>
        /// Agrega un boton a la barra. Si requiereSeleccion es true solo se habilita con una fila elegida.
        /// </summary>
        public Button Agregar(string clave, Action<object> accion, bool requiereSeleccion = true, Permiso? permiso = null)
        {
            if (permiso.HasValue && !Autorizacion_Tiene(permiso.Value))
            {
                return null;
            }

            Button boton = Estilo.Boton(clave);

            boton.Click += (remitente, argumentos) =>
            {
                object fila = Seleccionado;

                if (requiereSeleccion && fila == null)
                {
                    Estilo.Avisar(this, Tr.T("Select a row first."));
                    return;
                }

                Estilo.Ejecutar(this, () => accion(fila));
            };

            if (requiereSeleccion)
            {
                _botonesSeleccion.Add(boton);
                boton.Enabled = false;
            }

            _barra.Controls.Add(boton);
            return boton;
        }

        /// <summary>
        /// Habilita el boton solo cuando la fila elegida cumple la condicion.
        /// </summary>
        public void HabilitarSi(Button boton, Func<object, bool> condicion)
        {
            if (boton == null)
            {
                return;
            }

            _habilitaciones[boton] = () => condicion(Seleccionado);
        }

        /// <summary>Tine todas las filas (por ejemplo en rojo para los faltantes).</summary>
        public void PintarFilas(Color fondo, Color texto)
        {
            _grilla.DefaultCellStyle.BackColor = fondo;
            _grilla.DefaultCellStyle.ForeColor = texto;
            _grilla.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(Math.Max(fondo.R - 6, 0), Math.Max(fondo.G - 6, 0), Math.Max(fondo.B - 6, 0));
            _grilla.AlternatingRowsDefaultCellStyle.ForeColor = texto;
        }

        public void AgregarExportacion()
        {
            Button csv = Estilo.Boton("Export CSV");
            csv.BackColor = Color.FromArgb(90, 110, 90);
            csv.Click += (remitente, argumentos) => Exportar(false);
            Button xml = Estilo.Boton("Export XML");
            xml.BackColor = Color.FromArgb(90, 110, 90);
            xml.Click += (remitente, argumentos) => Exportar(true);
            _barra.Controls.Add(csv);
            _barra.Controls.Add(xml);
        }

        public void ConDetalle(Func<object, IList> origenDetalle, Func<object, string> titulo = null)
        {
            _origenDetalle = origenDetalle;
            _tituloDetalle = titulo;
            Controls.Remove(_grilla);

            _division = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 260
            };

            _detalle = Estilo.Grilla();
            _detalle.DataBindingComplete += (remitente, argumentos) => AjustarColumnas(_detalle);

            _division.Panel1.Controls.Add(_grilla);
            _division.Panel2.Controls.Add(_detalle);

            if (titulo != null)
            {
                _rotuloDetalle = new Label
                {
                    Dock = DockStyle.Top,
                    Height = 26,
                    Font = Estilo.Negrita,
                    ForeColor = Estilo.Acento,
                    TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                    Padding = new Padding(4, 0, 0, 0)
                };
                _division.Panel2.Controls.Add(_rotuloDetalle);
            }
            Controls.Add(_division);
            _division.BringToFront();
        }

        public Control PonerEnBarra(Control control)
        {
            _barra.Controls.Add(control);
            return control;
        }

        private static bool Autorizacion_Tiene(Permiso permiso)
        {
            return Edelstahl.BLL.Infrastructure.Autorizacion.Tiene(permiso);
        }

        public void Recargar()
        {
            if (!IsHandleCreated)
            {
                // La lista aun no esta en pantalla: se carga cuando se cree la ventana.
                EventHandler alCrear = null;
                alCrear = (remitente, argumentos) =>
                {
                    HandleCreated -= alCrear;
                    Recargar();
                };
                HandleCreated += alCrear;
                return;
            }

            int version = Interlocked.Increment(ref _version);
            _estado.Text = Tr.T("Loading...");

            Task.Run(() =>
            {
                try
                {
                    IList datos = _origen();
                    return new KeyValuePair<IList, Exception>(datos, null);
                }
                catch (Exception ex)
                {
                    return new KeyValuePair<IList, Exception>(null, ex);
                }
            }).ContinueWith(tarea =>
            {
                if (IsDisposed || !IsHandleCreated || version != _version)
                {
                    return;
                }

                try
                {
                    BeginInvoke((MethodInvoker)(() => Recibir(tarea.Result)));
                }
                catch (InvalidOperationException)
                {
                    // El formulario se cerro mientras se leian los datos.
                }
            });
        }

        private void Recibir(KeyValuePair<IList, Exception> resultado)
        {
            if (resultado.Value != null)
            {
                _estado.Text = resultado.Value is Edelstahl.BLL.Exceptions.Base.BusinessRuleException
                    ? resultado.Value.Message
                    : Tr.T("The data could not be loaded.");
                _datos = new ArrayList();
                _grilla.DataSource = null;
                return;
            }

            _datos = resultado.Key ?? new ArrayList();
            Mostrar();
        }

        private void Mostrar()
        {
            IList visibles = Filtrar(_datos, _buscar.Text);
            Guid? anterior = IdSeleccionado;

            _grilla.DataSource = visibles.Count == 0 ? null : visibles;

            if (visibles.Count == 0)
            {
                _grilla.DataSource = null;
            }

            if (anterior.HasValue)
            {
                foreach (DataGridViewRow fila in _grilla.Rows)
                {
                    if (Estilo.IdDe(fila.DataBoundItem) == anterior)
                    {
                        _grilla.CurrentCell = fila.Cells.Cast<DataGridViewCell>().FirstOrDefault(c => c.Visible);
                        break;
                    }
                }
            }

            _estado.Text = Tr.T("{0} record(s)", visibles.Count);
            AlCambiarSeleccion();
        }

        private static IList Filtrar(IList datos, string texto)
        {
            if (datos == null || datos.Count == 0 || string.IsNullOrWhiteSpace(texto))
            {
                return datos ?? new ArrayList();
            }

            string buscado = texto.Trim().ToLowerInvariant();
            Type tipo = datos[0].GetType();
            PropertyInfo[] propiedades = tipo.GetProperties()
                .Where(p => p.PropertyType == typeof(string)).ToArray();

            IList resultado = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(tipo));

            foreach (object fila in datos)
            {
                foreach (PropertyInfo propiedad in propiedades)
                {
                    string valor = propiedad.GetValue(fila, null) as string;

                    if (valor != null && valor.ToLowerInvariant().Contains(buscado))
                    {
                        resultado.Add(fila);
                        break;
                    }
                }
            }

            return resultado;
        }

        private void AlCambiarSeleccion()
        {
            bool hay = Seleccionado != null;

            foreach (Button boton in _botonesSeleccion)
            {
                Func<bool> condicion;
                boton.Enabled = hay && (!_habilitaciones.TryGetValue(boton, out condicion) || condicion());
            }

            object fila = Seleccionado;

            if (ReferenceEquals(fila, _filaNotificada))
            {
                return;
            }

            _filaNotificada = fila;

            if (_detalle != null)
            {
                CargarDetalle(fila);
            }

            Action<object> avisar = FilaCambiada;

            if (avisar != null)
            {
                avisar(fila);
            }
        }

        private void CargarDetalle(object fila)
        {
            int version = Interlocked.Increment(ref _versionDetalle);

            if (_rotuloDetalle != null)
            {
                _rotuloDetalle.Text = fila == null ? string.Empty : _tituloDetalle(fila);
            }

            if (fila == null)
            {
                _detalle.DataSource = null;
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    return _origenDetalle(fila);
                }
                catch (Exception)
                {
                    return null;
                }
            }).ContinueWith(tarea =>
            {
                if (IsDisposed || !IsHandleCreated || version != _versionDetalle)
                {
                    return;
                }

                try
                {
                    BeginInvoke((MethodInvoker)(() =>
                    {
                        IList datos = tarea.Result;
                        _detalle.DataSource = datos == null || datos.Count == 0 ? null : datos;
                    }));
                }
                catch (InvalidOperationException)
                {
                }
            });
        }

        private void AlCambiarIdioma(object remitente, EventArgs argumentos)
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)(() => AlCambiarIdioma(remitente, argumentos)));
                return;
            }

            _etiquetaBuscar.Text = Tr.T("Search");

            foreach (Control control in _barra.Controls)
            {
                Button boton = control as Button;

                if (boton != null && boton.Tag is string)
                {
                    boton.Text = Tr.T((string)boton.Tag);
                }
            }

            Recargar();
        }

        private static void AjustarColumnas(DataGridView grilla)
        {
            AjustarColumnasInterno(grilla);

            if (grilla.Rows.Count > 0 && grilla.CurrentCell == null)
            {
                DataGridViewColumn primera = grilla.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.Visible);

                if (primera != null)
                {
                    grilla.CurrentCell = grilla.Rows[0].Cells[primera.Index];
                    grilla.Rows[0].Selected = true;
                }
            }
        }

        private static void AjustarColumnasInterno(DataGridView grilla)
        {
            foreach (DataGridViewColumn columna in grilla.Columns)
            {
                string nombre = columna.DataPropertyName;
                Type tipo = columna.ValueType;

                columna.HeaderText = Tr.Columna(nombre);

                bool oculta = nombre == "Id" || nombre.EndsWith("Id", StringComparison.Ordinal)
                    || nombre.EndsWith("Clave", StringComparison.Ordinal) || tipo == typeof(Guid) || tipo == typeof(Guid?);

                columna.Visible = !oculta;

                Type real = Nullable.GetUnderlyingType(tipo) ?? tipo;

                if (real == typeof(decimal))
                {
                    columna.DefaultCellStyle.Format = "N2";
                    columna.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                else if (real == typeof(DateTime))
                {
                    columna.DefaultCellStyle.Format = "g";
                }
                else if (real == typeof(bool))
                {
                    columna.HeaderText = Tr.Columna(nombre);
                }
            }
        }

        private void Exportar(bool xml)
        {
            if (_datos == null || _datos.Count == 0)
            {
                Estilo.Avisar(this, Tr.T("There is nothing to export."));
                return;
            }

            using (SaveFileDialog dialogo = new SaveFileDialog())
            {
                dialogo.Filter = xml ? "XML (*.xml)|*.xml" : "CSV (*.csv)|*.csv";
                dialogo.FileName = "export_" + DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture);

                if (dialogo.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                Type tipo = _datos[0].GetType();
                string ruta = dialogo.FileName;

                Estilo.Ejecutar(this, () =>
                {
                    if (xml)
                    {
                        MethodInfo guardar = typeof(SerializadorXml).GetMethod("Guardar").MakeGenericMethod(tipo);
                        guardar.Invoke(null, new object[] { _datos, ruta });
                    }
                    else
                    {
                        object exportador = Activator.CreateInstance(typeof(ExportadorCsv<>).MakeGenericType(tipo));
                        exportador.GetType().GetMethod("Exportar").Invoke(exportador, new object[] { _datos, ruta });
                    }

                    new BitacoraService().RegistrarAuditoria("Reportes", "Exportar", tipo.Name, null,
                        "Exportacion de " + _datos.Count + " registros a " + Path.GetFileName(ruta));

                    Estilo.Informar(this, Tr.T("File saved: {0}", ruta));
                });
            }
        }
    }
}
