using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Edelstahl.WinApp.Ui
{
    public enum TipoCampo
    {
        Texto,
        Multilinea,
        Clave,
        Numero,
        Entero,
        Fecha,
        Combo,
        Check
    }

    /// <summary>
    /// Describe un dato que se pide al usuario en un cuadro de dialogo.
    /// </summary>
    public class Campo
    {
        public Campo(string nombre, string etiqueta, TipoCampo tipo)
        {
            Nombre = nombre;
            Etiqueta = etiqueta;
            Tipo = tipo;
            Maximo = 1000000000m;
        }

        public string Nombre { get; private set; }
        public string Etiqueta { get; private set; }
        public TipoCampo Tipo { get; private set; }
        public object Valor { get; set; }
        public bool Obligatorio { get; set; }
        public decimal Minimo { get; set; }
        public decimal Maximo { get; set; }
        public int Decimales { get; set; }
        public bool FechaOpcional { get; set; }
        public int MaxLargo { get; set; }
        public List<Opcion> Opciones { get; set; }
        public string SeguirA { get; set; }
        public Func<object, decimal> Calcular { get; set; }

        /// <summary>
        /// El valor del campo se completa solo a partir de la opcion elegida en otra lista.
        /// </summary>
        /// <summary>Nombre del campo de clave que este campo debe repetir (confirmacion de contrasena).</summary>
        public string ConfirmaA { get; set; }

        public Campo Confirmando(string campoClave)
        {
            ConfirmaA = campoClave;
            return this;
        }

        /// <summary>Exige que el numero sea mayor que cero.</summary>
        public Campo Positivo()
        {
            Obligatorio = true;
            return this;
        }

        /// <summary>Define el valor inicial del campo (por ejemplo, la opcion elegida de un combo).</summary>
        public Campo Con(object valor)
        {
            Valor = valor;
            return this;
        }

        public Campo Siguiendo(string campoLista, Func<object, decimal> calcular)
        {
            SeguirA = campoLista;
            Calcular = calcular;
            return this;
        }

        public static Campo Texto(string nombre, string etiqueta, string valor = "", bool obligatorio = false, int maxLargo = 200)
        {
            return new Campo(nombre, etiqueta, TipoCampo.Texto) { Valor = valor, Obligatorio = obligatorio, MaxLargo = maxLargo };
        }

        public static Campo Multilinea(string nombre, string etiqueta, string valor = "", bool obligatorio = false)
        {
            return new Campo(nombre, etiqueta, TipoCampo.Multilinea) { Valor = valor, Obligatorio = obligatorio, MaxLargo = 1000 };
        }

        public static Campo Clave(string nombre, string etiqueta)
        {
            return new Campo(nombre, etiqueta, TipoCampo.Clave) { Obligatorio = true, MaxLargo = 100 };
        }

        public static Campo Numero(string nombre, string etiqueta, decimal valor = 0m, decimal minimo = 0m, decimal maximo = 1000000000m, int decimales = 2)
        {
            return new Campo(nombre, etiqueta, TipoCampo.Numero) { Valor = valor, Minimo = minimo, Maximo = maximo, Decimales = decimales };
        }

        public static Campo Entero(string nombre, string etiqueta, int valor = 0, int minimo = 0, int maximo = 1000000)
        {
            return new Campo(nombre, etiqueta, TipoCampo.Entero) { Valor = (decimal)valor, Minimo = minimo, Maximo = maximo };
        }

        public static Campo Fecha(string nombre, string etiqueta, DateTime? valor = null, bool opcional = false)
        {
            return new Campo(nombre, etiqueta, TipoCampo.Fecha) { Valor = valor ?? DateTime.Today, FechaOpcional = opcional };
        }

        public static Campo Combo(string nombre, string etiqueta, List<Opcion> opciones, bool obligatorio = true)
        {
            return new Campo(nombre, etiqueta, TipoCampo.Combo) { Opciones = opciones, Obligatorio = obligatorio };
        }

        public static Campo Check(string nombre, string etiqueta, bool valor = false)
        {
            return new Campo(nombre, etiqueta, TipoCampo.Check) { Valor = valor };
        }
    }

    /// <summary>
    /// Valores devueltos por el cuadro de dialogo.
    /// </summary>
    public class Respuesta
    {
        private readonly Dictionary<string, object> _valores = new Dictionary<string, object>();

        internal void Poner(string nombre, object valor)
        {
            _valores[nombre] = valor;
        }

        public string Texto(string nombre)
        {
            object valor;
            return _valores.TryGetValue(nombre, out valor) && valor != null ? valor.ToString().Trim() : string.Empty;
        }

        public decimal Numero(string nombre)
        {
            object valor;
            return _valores.TryGetValue(nombre, out valor) && valor is decimal ? (decimal)valor : 0m;
        }

        public int Entero(string nombre)
        {
            return (int)Numero(nombre);
        }

        public DateTime? Fecha(string nombre)
        {
            object valor;
            return _valores.TryGetValue(nombre, out valor) && valor is DateTime ? (DateTime?)valor : null;
        }

        public bool Check(string nombre)
        {
            object valor;
            return _valores.TryGetValue(nombre, out valor) && valor is bool && (bool)valor;
        }

        public object Seleccion(string nombre)
        {
            object valor;
            return _valores.TryGetValue(nombre, out valor) ? valor : null;
        }

        public Guid GuidDe(string nombre)
        {
            object valor = Seleccion(nombre);
            return valor is Guid ? (Guid)valor : Guid.Empty;
        }
    }

    /// <summary>
    /// Cuadro de dialogo generico armado a partir de una lista de campos.
    /// </summary>
    public static class Dialogo
    {
        public static Respuesta Pedir(IWin32Window padre, string tituloClave, params Campo[] campos)
        {
            return Pedir(padre, tituloClave, null, campos);
        }

        public static Respuesta Pedir(IWin32Window padre, string tituloClave, string notaClave, params Campo[] campos)
        {
            using (Form formulario = new Form())
            {
                formulario.Text = Tr.T(tituloClave);
                formulario.FormBorderStyle = FormBorderStyle.FixedDialog;
                formulario.StartPosition = FormStartPosition.CenterParent;
                Edelstahl.WinApp.Ui.Marca.AplicarIcono(formulario);
                formulario.MaximizeBox = false;
                formulario.MinimizeBox = false;
                formulario.ShowInTaskbar = false;
                formulario.Font = Estilo.Normal;
                formulario.BackColor = Estilo.Superficie;

                TableLayoutPanel tabla = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    ColumnCount = 2,
                    Padding = new Padding(16, 14, 16, 4)
                };

                tabla.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));

                int proximaFila = 0;

                if (!string.IsNullOrEmpty(notaClave))
                {
                    Label nota = new Label
                    {
                        Text = Tr.T(notaClave),
                        AutoSize = true,
                        MaximumSize = new Size(480, 0),
                        ForeColor = Estilo.Principal,
                        Margin = new Padding(0, 0, 0, 10)
                    };

                    tabla.Controls.Add(nota, 0, 0);
                    tabla.SetColumnSpan(nota, 2);
                    proximaFila = 1;
                }

                Dictionary<string, Control> controles = new Dictionary<string, Control>();
                Dictionary<string, CheckBox> opcionales = new Dictionary<string, CheckBox>();

                foreach (Campo campo in campos)
                {
                    Label etiqueta = new Label
                    {
                        Text = Tr.T(campo.Etiqueta),
                        AutoSize = true,
                        Anchor = AnchorStyles.Left,
                        Margin = new Padding(0, 8, 12, 8)
                    };

                    Control control = CrearControl(campo, opcionales);
                    control.Margin = new Padding(0, 4, 0, 4);
                    controles[campo.Nombre] = control;

                    int fila = proximaFila++;
                    tabla.RowCount = proximaFila;
                    tabla.Controls.Add(etiqueta, 0, fila);
                    tabla.Controls.Add(control, 1, fila);
                }

                // Si hay campos de clave se agrega una casilla para ver lo que se escribe.
                List<TextBox> claves = campos.Where(c => c.Tipo == TipoCampo.Clave).Select(c => (TextBox)controles[c.Nombre]).ToList();

                if (claves.Count > 0)
                {
                    CheckBox ver = new CheckBox { Text = Tr.T("Show password"), AutoSize = true, Margin = new Padding(0, 2, 0, 6) };
                    ver.CheckedChanged += (remitente, argumentos) => { foreach (TextBox clave in claves) { clave.UseSystemPasswordChar = !ver.Checked; } };
                    int filaVer = proximaFila++;
                    tabla.RowCount = proximaFila;
                    tabla.Controls.Add(ver, 1, filaVer);
                }

                // Un campo numerico puede seguir a una lista: al elegir otra opcion se
                // recalcula su valor (el usuario puede modificarlo despues).
                foreach (Campo campo in campos)
                {
                    ComboBox origen;
                    NumericUpDown destino = controles[campo.Nombre] as NumericUpDown;

                    if (campo.SeguirA == null || destino == null || campo.Calcular == null
                        || (origen = controles[campo.SeguirA] as ComboBox) == null)
                    {
                        continue;
                    }

                    Func<object, decimal> calcular = campo.Calcular;
                    Action actualizar = () =>
                    {
                        Opcion elegida = origen.SelectedItem as Opcion;

                        if (elegida != null)
                        {
                            destino.Value = Limitar(calcular(elegida.Valor), destino.Minimum, destino.Maximum);
                        }
                    };

                    origen.SelectedIndexChanged += (remitente, argumentos) => actualizar();
                    actualizar();
                }

                Button aceptar = Estilo.Boton("Accept");
                Button cancelar = Estilo.Boton("Cancel");
                cancelar.BackColor = Color.Gray;
                cancelar.DialogResult = DialogResult.Cancel;

                FlowLayoutPanel botones = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    FlowDirection = FlowDirection.RightToLeft,
                    Height = 48,
                    Padding = new Padding(12, 8, 12, 4)
                };

                botones.Controls.Add(cancelar);
                botones.Controls.Add(aceptar);

                formulario.Controls.Add(tabla);
                formulario.Controls.Add(botones);
                formulario.AcceptButton = aceptar;
                formulario.CancelButton = cancelar;
                formulario.ClientSize = new Size(tabla.PreferredSize.Width + 4, tabla.PreferredSize.Height + 52);

                Respuesta respuesta = new Respuesta();

                aceptar.Click += (remitente, argumentos) =>
                {
                    string error = Validar(campos, controles, opcionales, respuesta);

                    if (error == null)
                    {
                        formulario.DialogResult = DialogResult.OK;
                    }
                    else
                    {
                        Estilo.Avisar(formulario, error);
                    }
                };

                return formulario.ShowDialog(padre) == DialogResult.OK ? respuesta : null;
            }
        }

        private static Control CrearControl(Campo campo, Dictionary<string, CheckBox> opcionales)
        {
            switch (campo.Tipo)
            {
                case TipoCampo.Multilinea:
                    return new TextBox
                    {
                        Multiline = true,
                        Height = 70,
                        Width = 320,
                        ScrollBars = ScrollBars.Vertical,
                        MaxLength = campo.MaxLargo > 0 ? campo.MaxLargo : 1000,
                        Text = campo.Valor == null ? string.Empty : campo.Valor.ToString()
                    };

                case TipoCampo.Clave:
                    return new TextBox
                    {
                        Width = 320,
                        UseSystemPasswordChar = true,
                        MaxLength = campo.MaxLargo > 0 ? campo.MaxLargo : 100
                    };

                case TipoCampo.Numero:
                case TipoCampo.Entero:
                    return new NumericUpDown
                    {
                        Width = 160,
                        DecimalPlaces = campo.Tipo == TipoCampo.Entero ? 0 : campo.Decimales,
                        Minimum = campo.Minimo,
                        Maximum = campo.Maximo,
                        ThousandsSeparator = true,
                        Value = Limitar(campo.Valor is decimal ? (decimal)campo.Valor : 0m, campo.Minimo, campo.Maximo)
                    };

                case TipoCampo.Fecha:
                    {
                        DateTimePicker fecha = new DateTimePicker
                        {
                            Width = 160,
                            Format = DateTimePickerFormat.Short,
                            Value = campo.Valor is DateTime ? (DateTime)campo.Valor : DateTime.Today
                        };

                        if (campo.FechaOpcional)
                        {
                            fecha.ShowCheckBox = true;
                            fecha.Checked = false;
                        }

                        return fecha;
                    }

                case TipoCampo.Combo:
                    {
                        ComboBox combo = new ComboBox
                        {
                            Width = 320,
                            DropDownStyle = campo.Opciones != null && campo.Opciones.Count > 15 ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList
                        };

                        if (combo.DropDownStyle == ComboBoxStyle.DropDown)
                        {
                            combo.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                            combo.AutoCompleteSource = AutoCompleteSource.ListItems;
                        }

                        if (campo.Opciones != null)
                        {
                            foreach (Opcion opcion in campo.Opciones)
                            {
                                combo.Items.Add(opcion);
                            }
                        }

                        if (combo.Items.Count > 0)
                        {
                            combo.SelectedIndex = 0;

                            if (campo.Valor != null)
                            {
                                for (int i = 0; i < combo.Items.Count; i++)
                                {
                                    Opcion candidata = combo.Items[i] as Opcion;

                                    if (candidata != null && candidata.Valor != null && candidata.Valor.Equals(campo.Valor))
                                    {
                                        combo.SelectedIndex = i;
                                        break;
                                    }
                                }
                            }
                        }

                        return combo;
                    }

                case TipoCampo.Check:
                    return new CheckBox
                    {
                        AutoSize = true,
                        Checked = campo.Valor is bool && (bool)campo.Valor
                    };

                default:
                    return new TextBox
                    {
                        Width = 320,
                        MaxLength = campo.MaxLargo > 0 ? campo.MaxLargo : 200,
                        Text = campo.Valor == null ? string.Empty : campo.Valor.ToString()
                    };
            }
        }

        private static decimal Limitar(decimal valor, decimal minimo, decimal maximo)
        {
            return valor < minimo ? minimo : (valor > maximo ? maximo : valor);
        }

        private static string Validar(
            Campo[] campos,
            Dictionary<string, Control> controles,
            Dictionary<string, CheckBox> opcionales,
            Respuesta respuesta)
        {
            foreach (Campo campo in campos)
            {
                Control control = controles[campo.Nombre];

                switch (campo.Tipo)
                {
                    case TipoCampo.Numero:
                    case TipoCampo.Entero:
                        if (campo.Tipo == TipoCampo.Numero && campo.Obligatorio && ((NumericUpDown)control).Value <= 0m)
                        {
                            return Tr.T("Enter a value greater than zero for {0}.", Tr.T(campo.Etiqueta));
                        }

                        respuesta.Poner(campo.Nombre, ((NumericUpDown)control).Value);
                        break;

                    case TipoCampo.Fecha:
                        {
                            DateTimePicker fecha = (DateTimePicker)control;
                            bool usar = !fecha.ShowCheckBox || fecha.Checked;
                            respuesta.Poner(campo.Nombre, usar ? (object)fecha.Value.Date : null);
                            break;
                        }

                    case TipoCampo.Combo:
                        {
                            ComboBox combo = (ComboBox)control;
                            Opcion opcion = null;
                            int coincide = combo.FindStringExact(combo.Text);

                            // Con autocompletado el texto puede cambiar sin actualizar la seleccion: manda lo que se ve.
                            if (coincide >= 0)
                            {
                                opcion = combo.Items[coincide] as Opcion;
                            }

                            if (opcion == null)
                            {
                                opcion = combo.SelectedItem as Opcion;
                            }

                            if (opcion == null && campo.Obligatorio)
                            {
                                return Tr.T("Select a value for {0}.", Tr.T(campo.Etiqueta));
                            }

                            respuesta.Poner(campo.Nombre, opcion == null ? null : opcion.Valor);
                            break;
                        }

                    case TipoCampo.Check:
                        respuesta.Poner(campo.Nombre, ((CheckBox)control).Checked);
                        break;

                    default:
                        {
                            string texto = control.Text.Trim();

                            if (campo.ConfirmaA != null && texto != controles[campo.ConfirmaA].Text.Trim())
                            {
                                return Tr.T("The passwords do not match.");
                            }

                            if (campo.Obligatorio && texto.Length == 0)
                            {
                                return Tr.T("{0} is required.", Tr.T(campo.Etiqueta));
                            }

                            respuesta.Poner(campo.Nombre, texto);
                            break;
                        }
                }
            }

            return null;
        }
    }
}
