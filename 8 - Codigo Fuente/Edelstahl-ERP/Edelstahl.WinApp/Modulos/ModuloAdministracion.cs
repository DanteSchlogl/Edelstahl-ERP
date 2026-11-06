using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Services;
using Edelstahl.Domain.Security;
using Edelstahl.WinApp.Ui;

namespace Edelstahl.WinApp.Modulos
{
    public class ModuloAdministracion : ModuloBase
    {
        private readonly AdministracionService _admin = new AdministracionService();
        private readonly UsuarioService _usuarios = new UsuarioService();

        public ModuloAdministracion()
        {
            AgregarPestana("Users", CrearUsuarios(), Permiso.UsuariosAdministrar);
            AgregarPestana("Roles and permissions", CrearRoles(), Permiso.RolesAdministrar);
            AgregarPestana("Help requests", CrearPedidosAyuda(), Permiso.UsuariosAdministrar);
            AgregarPestana("Backups", new PanelRespaldos(), Permiso.RespaldoAdministrar);
            AgregarPestana("Sample data and imports", new PanelDatos(), Permiso.RespaldoAdministrar);
            CargarPestanaActual();
        }

        /// <summary>Bandeja de pedidos de ayuda que mandan los usuarios.</summary>
        private Control CrearPedidosAyuda()
        {
            AyudaService ayuda = new AyudaService();

            PanelLista lista = new PanelLista(() => ayuda.ObtenerPedidos().Select(PedidoAyudaFila.De).ToList());

            Button atender = lista.Agregar("Mark as handled", fila =>
            {
                ayuda.MarcarAtendido(Estilo.IdDe(fila).Value);
                Estilo.Informar(this, Tr.T("The help request was marked as handled."));
                lista.Recargar();
            });

            lista.HabilitarSi(atender, fila => fila != null && Estilo.Valor<string>(fila, "Estado") == Tr.T("Pending"));
            lista.AgregarExportacion();
            return lista;
        }

        /// <summary>Abre la pestana de pedidos de ayuda (se usa desde el aviso de la cabecera).</summary>
        public void MostrarPedidosDeAyuda()
        {
            foreach (TabPage pagina in Pestanas.TabPages)
            {
                if ((pagina.Tag as string) == "Help requests")
                {
                    Pestanas.SelectedTab = pagina;
                }
            }
        }

        private Dictionary<Guid, string> NombresDeRoles()
        {
            return _usuarios.ObtenerRoles().ToDictionary(r => r.Id, r => r.Nombre);
        }

        private Control CrearUsuarios()
        {
            PanelLista lista = new PanelLista(() =>
            {
                Dictionary<Guid, string> roles = NombresDeRoles();
                return _admin.ObtenerUsuarios().Select(u => UsuarioFila.De(u, roles)).ToList();
            });

            lista.Agregar("New user", fila =>
            {
                List<Opcion> roles = _usuarios.ObtenerRoles().Where(r => r.Activo)
                    .Select(r => new Opcion(r.Nombre, r.Nombre)).ToList();

                Respuesta datos = Dialogo.Pedir(this, "New user",
                    "The password must have at least 8 characters.",
                    Campo.Texto("usuario", "User name", "", true, 50),
                    Campo.Texto("nombre", "Full name", "", true, 100),
                    Campo.Texto("email", "Email", "", true, 100),
                    Campo.Clave("clave", "Password"),
                    Campo.Clave("clave2", "Confirm password").Confirmando("clave"),
                    Campo.Combo("rol", "Role", roles),
                    Campo.Combo("pregunta", "Security question (optional)", DialogosUsuario.PreguntasSeguridad(true), false),
                    Campo.Clave("respuestaSeg", "Answer to the security question"));

                if (datos == null)
                {
                    return;
                }

                _admin.CrearUsuario(datos.Texto("usuario"), datos.Texto("nombre"), datos.Texto("email"),
                    datos.Texto("clave"), (string)datos.Seleccion("rol"),
                    (string)datos.Seleccion("pregunta"), datos.Texto("respuestaSeg"));
                lista.Recargar();
            }, false);

            Button estado = lista.Agregar("Activate / deactivate", fila =>
            {
                _admin.CambiarEstado(Estilo.IdDe(fila).Value, !Estilo.Valor<bool>(fila, "Activo"));
                lista.Recargar();
            });

            lista.Agregar("Unlock", fila =>
            {
                _admin.Desbloquear(Estilo.IdDe(fila).Value);
                Estilo.Informar(this, Tr.T("The user was unlocked."));
                lista.Recargar();
            });

            lista.Agregar("Reset password", fila =>
            {
                Respuesta datos = Dialogo.Pedir(this, "Reset password",
                    "The password must have at least 8 characters.",
                    Campo.Clave("clave", "New password"),
                    Campo.Clave("clave2", "Confirm password").Confirmando("clave"));

                if (datos == null)
                {
                    return;
                }

                _admin.RestablecerPassword(Estilo.IdDe(fila).Value, datos.Texto("clave"));
                Estilo.Informar(this, Tr.T("The password was reset."));
            });

            lista.Agregar("Security question", fila =>
            {
                Respuesta datos = Dialogo.Pedir(this, "Security question",
                    "Define the security question of the selected user. They will use it to recover their password.",
                    Campo.Combo("pregunta", "Question", DialogosUsuario.PreguntasSeguridad(false)),
                    Campo.Clave("respuesta", "Answer"),
                    Campo.Clave("respuesta2", "Confirm answer").Confirmando("respuesta"));

                if (datos == null)
                {
                    return;
                }

                _admin.DefinirPreguntaSeguridad(Estilo.IdDe(fila).Value, (string)datos.Seleccion("pregunta"), datos.Texto("respuesta"));
                Estilo.Informar(this, Tr.T("The security question was saved."));
            });

            lista.Agregar("Edit user", fila =>
            {
                Respuesta datos = Dialogo.Pedir(this, "Edit user",
                    Campo.Texto("nombre", "Full name", Estilo.Valor<string>(fila, "Nombre"), true, 100),
                    Campo.Texto("email", "Email", Estilo.Valor<string>(fila, "Email"), true, 100));

                if (datos == null)
                {
                    return;
                }

                _admin.ModificarDatos(Estilo.IdDe(fila).Value, datos.Texto("nombre"), datos.Texto("email"));
                lista.Recargar();
            });

            lista.Agregar("Change role", fila =>
            {
                List<Opcion> roles = _usuarios.ObtenerRoles().Where(r => r.Activo)
                    .Select(r => new Opcion(r.Nombre, r.Nombre)).ToList();

                Respuesta datos = Dialogo.Pedir(this, "Change role", Campo.Combo("rol", "Role", roles));

                if (datos == null)
                {
                    return;
                }

                _admin.CambiarRol(Estilo.IdDe(fila).Value, (string)datos.Seleccion("rol"));
                lista.Recargar();
            });

            lista.AgregarExportacion();
            return lista;
        }

        private Control CrearRoles()
        {
            PanelLista lista = new PanelLista(() =>
            {
                return _admin.ObtenerRoles().Select(r => new RolFila
                {
                    Id = r.Id,
                    Nombre = r.Nombre,
                    Descripcion = r.Descripcion,
                    Activo = r.Activo,
                    Permisos = AdministracionService.PermisosDeRol(r.Nombre).Count()
                }).ToList();
            });

            lista.ConDetalle(fila => AdministracionService.PermisosDeRol(Estilo.Valor<string>(fila, "Nombre"))
                .Select(p => new PermisoFila { Permiso = Tr.T(Nombres.Permiso(p.ToString())) }).ToList());

            lista.Agregar("Activate / deactivate", fila =>
            {
                _admin.CambiarEstadoRol(Estilo.IdDe(fila).Value, !Estilo.Valor<bool>(fila, "Activo"));
                lista.Recargar();
            });

            return lista;
        }

        public static string Humanizar(string nombre)
        {
            System.Text.StringBuilder texto = new System.Text.StringBuilder();

            for (int i = 0; i < nombre.Length; i++)
            {
                if (i > 0 && char.IsUpper(nombre[i]))
                {
                    texto.Append(' ');
                }

                texto.Append(nombre[i]);
            }

            return texto.ToString();
        }
    }

    /// <summary>
    /// Pantalla de consulta de la bitacora con filtros por fecha, usuario, modulo y texto.
    /// </summary>
    public class VisorBitacora : UserControl, IRecargable
    {
        private readonly BitacoraService _servicio = new BitacoraService();
        private readonly DateTimePicker _desde;
        private readonly DateTimePicker _hasta;
        private readonly ComboBox _usuario;
        private readonly ComboBox _modulo;
        private readonly ComboBox _tipo;
        private readonly TextBox _texto;
        private readonly PanelLista _lista;
        private bool _cargandoFiltros;

        public VisorBitacora()
        {
            Dock = DockStyle.Fill;

            _lista = new PanelLista(Consultar, false);
            _lista.AgregarExportacion();

            FlowLayoutPanel filtros = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(8, 6, 8, 0),
                BackColor = Estilo.Superficie
            };

            _desde = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
            _hasta = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            _usuario = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            _modulo = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            _tipo = new ComboBox { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
            _texto = new TextBox { Width = 160 };

            _tipo.Items.AddRange(new object[] { string.Empty, "Informacion", "Auditoria", "Seguridad", "Error" });

            Button buscar = Estilo.Boton("Search");
            buscar.Click += (remitente, argumentos) => { Instantanea(); _lista.Recargar(); };

            Agregar(filtros, "From", _desde);
            Agregar(filtros, "To", _hasta);
            Agregar(filtros, "User", _usuario);
            Agregar(filtros, "Module", _modulo);
            Agregar(filtros, "Event type", _tipo);
            Agregar(filtros, "Text", _texto);
            filtros.Controls.Add(buscar);

            Controls.Add(_lista);
            Controls.Add(filtros);
            filtros.BringToFront();
            _lista.BringToFront();
        }

        private static void Agregar(FlowLayoutPanel panel, string clave, Control control)
        {
            Label etiqueta = new Label { Text = Tr.T(clave), AutoSize = true, Margin = new Padding(6, 7, 3, 0) };
            control.Margin = new Padding(0, 3, 6, 0);
            panel.Controls.Add(etiqueta);
            panel.Controls.Add(control);
        }

        public void Recargar()
        {
            _cargandoFiltros = true;

            RellenarLista(_usuario, _servicio.ObtenerUsuarios());
            RellenarLista(_modulo, _servicio.ObtenerModulos());

            _cargandoFiltros = false;
            Instantanea();
            _lista.Recargar();
        }

        private static void RellenarLista(ComboBox combo, List<string> valores)
        {
            object actual = combo.SelectedItem;
            combo.Items.Clear();
            combo.Items.Add(string.Empty);
            combo.Items.AddRange(valores.Cast<object>().ToArray());
            combo.SelectedItem = actual ?? string.Empty;

            if (combo.SelectedIndex < 0)
            {
                combo.SelectedIndex = 0;
            }
        }

        private DateTime _fDesde = DateTime.Today.AddDays(-30);
        private DateTime _fHasta = DateTime.Today;
        private string _fUsuario = string.Empty;
        private string _fModulo = string.Empty;
        private string _fTipo = string.Empty;
        private string _fTexto = string.Empty;

        private void Instantanea()
        {
            _fDesde = _desde.Value;
            _fHasta = _hasta.Value;
            _fUsuario = Convert.ToString(_usuario.SelectedItem);
            _fModulo = Convert.ToString(_modulo.SelectedItem);
            _fTipo = Convert.ToString(_tipo.SelectedItem);
            _fTexto = _texto.Text;
        }

        private IList Consultar()
        {
            return _servicio.Consultar(_fDesde, _fHasta, _fUsuario, _fModulo, _fTipo, _fTexto)
                .Select(BitacoraFila.De).ToList();
        }
    }

    /// <summary>
    /// Respaldos de las bases: carpeta de destino, respaldo manual y automatico, lista de
    /// respaldos hechos (con la marca de si los datos estaban sanos) y restauracion.
    /// </summary>
    public class PanelRespaldos : UserControl
    {
        private readonly RespaldoService _servicio = new RespaldoService();
        private readonly IntegridadService _integridad = new IntegridadService();
        private readonly TextBox _carpeta = new TextBox { ReadOnly = true, Width = 460, BackColor = System.Drawing.Color.White };
        private readonly CheckBox _automatico = new CheckBox { AutoSize = true, Margin = new Padding(0, 6, 0, 0) };
        private readonly DataGridView _grilla = Estilo.Grilla();
        private readonly Label _estado = new Label { Dock = DockStyle.Bottom, Height = 34, Padding = new Padding(10, 8, 10, 0) };
        private bool _cargando;

        public PanelRespaldos()
        {
            Dock = DockStyle.Fill;

            FlowLayoutPanel carpeta = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10, 10, 10, 0) };
            carpeta.Controls.Add(new Label { Text = Tr.T("Backup folder:"), AutoSize = true, Margin = new Padding(0, 6, 6, 0) });
            carpeta.Controls.Add(_carpeta);

            Button elegir = Estilo.Boton("Choose folder...");
            elegir.Margin = new Padding(6, 0, 6, 0);
            elegir.Click += (s, e) => ElegirCarpeta();
            carpeta.Controls.Add(elegir);

            Button predeterminada = Estilo.Boton("Use the SQL Server folder");
            predeterminada.Click += (s, e) => GuardarCarpeta(string.Empty);
            carpeta.Controls.Add(predeterminada);

            FlowLayoutPanel automatico = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10, 6, 10, 0) };
            _automatico.Text = Tr.T("Create a backup automatically once a day when the program starts, only if the data passes the integrity check");
            _automatico.CheckedChanged += (s, e) => GuardarAutomatico();
            automatico.Controls.Add(_automatico);

            FlowLayoutPanel botones = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };

            Button crear = Estilo.Boton("Create backup now");
            crear.Click += (s, e) => Crear();

            Button restaurar = Estilo.Boton("Restore selected backup");
            restaurar.Click += (s, e) => Restaurar();

            Button archivo = Estilo.Boton("Restore from file...");
            archivo.Click += (s, e) => RestaurarDesdeArchivo();

            Button verificar = Estilo.Boton("Check data integrity");
            verificar.Click += (s, e) => Verificar();

            Button actualizar = Estilo.Boton("Refresh");
            actualizar.Click += (s, e) => Cargar();

            botones.Controls.AddRange(new Control[] { crear, restaurar, archivo, verificar, actualizar });

            Panel contenedor = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 0, 10, 0) };
            contenedor.Controls.Add(_grilla);

            Controls.Add(contenedor);
            Controls.Add(_estado);
            Controls.Add(botones);
            Controls.Add(automatico);
            Controls.Add(carpeta);
            carpeta.BringToFront();
            automatico.BringToFront();
            botones.BringToFront();
            contenedor.BringToFront();

            _estado.Text = Tr.T("Backups are made for the business and services databases. Only the business database is restored from here.");

            Cargar();
        }

        private void Cargar()
        {
            _cargando = true;

            try
            {
                string actual = _servicio.Configuracion.Carpeta;
                _carpeta.Text = string.IsNullOrWhiteSpace(actual) ? Tr.T("(SQL Server default folder)") : actual;
                _automatico.Checked = _servicio.Configuracion.Automatico;

                Estilo.Ejecutar(this, () =>
                {
                    _grilla.DataSource = _servicio.ObtenerRespaldos().Select(RespaldoFila.De).ToList();
                    Estilo.TitularColumnas(_grilla);
                });
            }
            finally
            {
                _cargando = false;
            }
        }

        private void ElegirCarpeta()
        {
            using (FolderBrowserDialog dialogo = new FolderBrowserDialog())
            {
                dialogo.Description = Tr.T("Choose the folder where the backups are saved.");

                if (!string.IsNullOrWhiteSpace(_servicio.Configuracion.Carpeta) &&
                    System.IO.Directory.Exists(_servicio.Configuracion.Carpeta))
                {
                    dialogo.SelectedPath = _servicio.Configuracion.Carpeta;
                }

                if (dialogo.ShowDialog(this) == DialogResult.OK)
                {
                    GuardarCarpeta(dialogo.SelectedPath);
                }
            }
        }

        private void GuardarCarpeta(string carpeta)
        {
            _servicio.Configuracion.Carpeta = carpeta;
            _servicio.Configuracion.Guardar();
            Cargar();
        }

        private void GuardarAutomatico()
        {
            if (_cargando)
            {
                return;
            }

            _servicio.Configuracion.Automatico = _automatico.Checked;
            _servicio.Configuracion.Guardar();
        }

        private void Crear()
        {
            List<RegistroRespaldo> creados = null;

            bool correcto = Estilo.Ejecutar(this, () =>
                FrmEspera.Ejecutar("Creating the backup...", () =>
                {
                    creados = _servicio.CrearRespaldoManual();
                    return string.Empty;
                }));

            if (!correcto || creados == null)
            {
                return;
            }

            Cargar();

            bool sano = creados.All(r => r.Sano);
            _estado.Text = Tr.T(sano
                ? "Backup created. The data passed the integrity check."
                : "Backup created, but the data did NOT pass the integrity check, so it is not marked as healthy.");
        }

        private RegistroRespaldo Seleccionado()
        {
            Guid? id = _grilla.CurrentRow == null ? null : Estilo.IdDe(_grilla.CurrentRow.DataBoundItem);

            if (!id.HasValue)
            {
                Estilo.Avisar(this, Tr.T("Select the backup to restore."));
                return null;
            }

            return _servicio.ObtenerRespaldos().FirstOrDefault(r => r.Id == id.Value);
        }

        private void Restaurar()
        {
            RegistroRespaldo respaldo = Seleccionado();

            if (respaldo == null)
            {
                return;
            }

            string aviso = respaldo.Sano
                ? Tr.T("The business data will be replaced by the backup of {0}. The current data is saved first as a safety copy. Continue?", respaldo.FechaHora.ToString("dd/MM/yyyy HH:mm"))
                : Tr.T("This backup was NOT verified as healthy. The business data will be replaced by the backup of {0}. Continue anyway?", respaldo.FechaHora.ToString("dd/MM/yyyy HH:mm"));

            if (Estilo.Confirmar(this, aviso))
            {
                EjecutarRestauracion(() => _servicio.Restaurar(respaldo));
            }
        }

        private void RestaurarDesdeArchivo()
        {
            using (OpenFileDialog dialogo = new OpenFileDialog())
            {
                dialogo.Filter = "SQL Server backup (*.bak)|*.bak";
                dialogo.Title = Tr.T("Choose the backup file to restore.");

                if (dialogo.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                string ruta = dialogo.FileName;

                if (Estilo.Confirmar(this, Tr.T("The business data will be replaced by the content of the file '{0}'. The current data is saved first as a safety copy. Continue?", System.IO.Path.GetFileName(ruta))))
                {
                    EjecutarRestauracion(() => _servicio.RestaurarDesdeArchivo(ruta));
                }
            }
        }

        private void EjecutarRestauracion(Func<Edelstahl.Domain.Integrity.ResultadoIntegridad> restauracion)
        {
            Edelstahl.Domain.Integrity.ResultadoIntegridad resultado = null;

            bool correcto = Estilo.Ejecutar(this, () =>
                FrmEspera.Ejecutar("Restoring the backup...", () =>
                {
                    resultado = restauracion();
                    return string.Empty;
                }));

            if (!correcto || resultado == null)
            {
                return;
            }

            Estilo.Informar(this, Tr.T(resultado.Correcto
                ? "The backup was restored and the data passed the integrity check. The program will close; open it again to continue."
                : "The backup was restored, but the data still does not pass the integrity check. The program will close; when you open it again the recovery screen will appear."));

            Application.Exit();
        }

        private void Verificar()
        {
            Edelstahl.Domain.Integrity.ResultadoIntegridad resultado = null;

            bool correcto = Estilo.Ejecutar(this, () =>
                FrmEspera.Ejecutar("Checking the integrity of the data...", () =>
                {
                    resultado = _integridad.VerificarYRegistrar("verificacion manual");
                    return string.Empty;
                }));

            if (!correcto || resultado == null)
            {
                return;
            }

            if (resultado.Correcto)
            {
                Estilo.Informar(this, Tr.T("The data passed the integrity check ({0} customer records checked).", resultado.RegistrosRevisados));
            }
            else
            {
                Estilo.Avisar(this, Tr.T("The integrity check found {0} altered record(s). The alteration was recorded in the audit log. Close the program and open it again to use the recovery screen.", resultado.RegistrosAlterados));
            }
        }
    }

    /// <summary>
    /// Carga de datos de ejemplo e importacion de archivos.
    /// Las operaciones largas se ejecutan en un hilo y muestran progreso.
    /// </summary>
    public class PanelDatos : UserControl
    {
        private readonly ProgressBar _progreso = new ProgressBar { Width = 420, Height = 18, Style = ProgressBarStyle.Marquee, Visible = false };
        private readonly TextBox _salida = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, BackColor = System.Drawing.Color.White };
        private readonly FlowLayoutPanel _botones = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
        private bool _ocupado;

        public PanelDatos()
        {
            Dock = DockStyle.Fill;

            Button ejemplo = Estilo.Boton("Load sample data");
            ejemplo.Click += (remitente, argumentos) => Hacer("Sample data", () =>
            {
                ResultadoImportacion r = new CargaInicialService().CargarDatosDeEjemplo(new Progress<int>(p => { }));
                return Resumen(r);
            });

            Button clientes = Estilo.Boton("Import customers (CSV)");
            clientes.Click += (remitente, argumentos) => Importar(true);

            Button productos = Estilo.Boton("Import products (CSV)");
            productos.Click += (remitente, argumentos) => Importar(false);

            _botones.Controls.AddRange(new Control[] { ejemplo, clientes, productos });

            Label nota = new Label
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(10, 6, 10, 0),
                Text = Tr.T("Imports skip records that already exist.")
            };

            Panel inferior = new Panel { Dock = DockStyle.Bottom, Height = 30, Padding = new Padding(10, 6, 10, 0) };
            inferior.Controls.Add(_progreso);

            Controls.Add(_salida);
            Controls.Add(inferior);
            Controls.Add(nota);
            Controls.Add(_botones);
            _botones.BringToFront();
            nota.BringToFront();
            _salida.BringToFront();
        }

        private static string Resumen(ResultadoImportacion r)
        {
            string texto = Tr.T("Total {0}, imported {1}, skipped {2}.", r.Total, r.Importados, r.Omitidos);

            if (r.Errores != null && r.Errores.Count > 0)
            {
                texto += Environment.NewLine + string.Join(Environment.NewLine, r.Errores.Take(20));
            }

            return texto;
        }

        private void Importar(bool clientes)
        {
            using (OpenFileDialog dialogo = new OpenFileDialog())
            {
                dialogo.Filter = "CSV (*.csv)|*.csv";

                string carpeta = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Datos");

                if (System.IO.Directory.Exists(carpeta))
                {
                    dialogo.InitialDirectory = carpeta;
                }

                if (dialogo.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                string ruta = dialogo.FileName;

                Hacer(clientes ? "Customers" : "Products", () =>
                {
                    ImportacionService servicio = new ImportacionService();
                    ResultadoImportacion r = clientes
                        ? servicio.ImportarClientes(ruta, new Progress<int>(p => { }))
                        : servicio.ImportarProductos(ruta, new Progress<int>(p => { }));
                    return Resumen(r);
                });
            }
        }

        private void Hacer(string tituloClave, Func<string> trabajo)
        {
            if (_ocupado)
            {
                return;
            }

            _ocupado = true;
            _botones.Enabled = false;
            _progreso.Visible = true;
            _salida.Text = Tr.T("Working...") + Environment.NewLine;

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    return trabajo();
                }
                catch (Edelstahl.Domain.Exceptions.BusinessRuleException ex)
                {
                    return ex.Message;
                }
                catch (Exception)
                {
                    return Tr.T("The operation could not be completed.");
                }
            }).ContinueWith(tarea =>
            {
                try
                {
                    BeginInvoke((MethodInvoker)(() =>
                    {
                        _salida.Text = Tr.T(tituloClave) + Environment.NewLine + tarea.Result;
                        _progreso.Visible = false;
                        _botones.Enabled = true;
                        _ocupado = false;
                    }));
                }
                catch (InvalidOperationException)
                {
                }
            });
        }
    }
}
