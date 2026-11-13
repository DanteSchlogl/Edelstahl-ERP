using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Edelstahl.BLL.Services;
using Edelstahl.Domain.Integrity;
using Edelstahl.Domain.Security;
using Edelstahl.WinApp.Ui;

namespace Edelstahl.WinApp
{
    /// <summary>
    /// Se muestra al iniciar cuando los dígitos verificadores (DVH y DVV)
    /// indican que los datos fueron alterados: el sistema no deja entrar. Quien
    /// quiera repararlo se identifica con usuario y contraseña; si tiene
    /// permiso de administrar respaldos ve todos los respaldos hechos y puede
    /// restaurar uno (o, después de revisar los datos, recalcular los dígitos)
    /// sin que nadie del equipo tenga que intervenir.
    /// </summary>
    public sealed class FrmRecuperacionIntegridad : Form
    {
        private readonly RecuperacionService _servicio;
        private ResultadoIntegridad _resultado;

        private readonly DataGridView _gridInconsistencias = Estilo.Grilla();
        private readonly Label _lblResumen = new Label();

        private readonly TextBox _txtUsuario = new TextBox();
        private readonly TextBox _txtPassword = new TextBox();
        private readonly Button _btnIngresar;
        private readonly Label _lblSesion = new Label();

        private readonly GroupBox _grupoRespaldos = new GroupBox();
        private readonly DataGridView _gridRespaldos = Estilo.Grilla();
        private readonly Button _btnRestaurar;
        private readonly Button _btnArchivo;
        private readonly Button _btnRecalcular;

        public FrmRecuperacionIntegridad(ResultadoIntegridad resultado)
            : this(resultado, new RecuperacionService())
        {
        }

        public FrmRecuperacionIntegridad(ResultadoIntegridad resultado, RecuperacionService servicio)
        {
            _resultado = resultado ?? throw new ArgumentNullException(nameof(resultado));
            _servicio = servicio ?? throw new ArgumentNullException(nameof(servicio));

            Text = Tr.T("Data integrity alert");
            ClientSize = new Size(920, 700);
            MinimumSize = new Size(920, 700);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = Estilo.Normal;
            Marca.AplicarIcono(this);

            Panel banda = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = Estilo.Alerta };
            banda.Controls.Add(new Label
            {
                Text = Tr.T("Data integrity alert"),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 8)
            });
            banda.Controls.Add(new Label
            {
                Text = Tr.T("The system found data that was changed outside of Edelstahl ERP. Access is blocked until it is repaired."),
                ForeColor = Color.White,
                AutoSize = false,
                Size = new Size(880, 36),
                Location = new Point(22, 40)
            });
            Controls.Add(banda);

            _lblResumen.AutoSize = false;
            _lblResumen.Location = new Point(20, 90);
            _lblResumen.Size = new Size(880, 36);
            _lblResumen.Font = Estilo.Negrita;
            Controls.Add(_lblResumen);

            _gridInconsistencias.Dock = DockStyle.None;
            _gridInconsistencias.Location = new Point(20, 130);
            _gridInconsistencias.Size = new Size(880, 130);
            Controls.Add(_gridInconsistencias);

            // Identificación
            GroupBox identificacion = new GroupBox
            {
                Text = Tr.T("To repair it, sign in with a user that can manage backups"),
                Location = new Point(20, 272),
                Size = new Size(880, 84)
            };

            identificacion.Controls.Add(new Label { Text = Tr.T("User name"), AutoSize = true, Location = new Point(14, 26) });
            _txtUsuario.Location = new Point(14, 46);
            _txtUsuario.Size = new Size(230, 26);
            identificacion.Controls.Add(_txtUsuario);

            identificacion.Controls.Add(new Label { Text = Tr.T("Password"), AutoSize = true, Location = new Point(260, 26) });
            _txtPassword.Location = new Point(260, 46);
            _txtPassword.Size = new Size(230, 26);
            _txtPassword.UseSystemPasswordChar = true;
            identificacion.Controls.Add(_txtPassword);

            _btnIngresar = Estilo.Boton("Sign in");
            _btnIngresar.Location = new Point(506, 44);
            _btnIngresar.Click += (s, e) => Ingresar();
            identificacion.Controls.Add(_btnIngresar);

            _lblSesion.AutoSize = false;
            _lblSesion.Location = new Point(620, 48);
            _lblSesion.Size = new Size(250, 24);
            _lblSesion.ForeColor = Estilo.Principal;
            _lblSesion.Font = Estilo.Negrita;
            identificacion.Controls.Add(_lblSesion);
            Controls.Add(identificacion);

            // Respaldos
            _grupoRespaldos.Text = Tr.T("Backups made (choose the one to restore)");
            _grupoRespaldos.Location = new Point(20, 368);
            _grupoRespaldos.Size = new Size(880, 270);
            _grupoRespaldos.Enabled = false;

            _gridRespaldos.Dock = DockStyle.None;
            _gridRespaldos.Location = new Point(12, 22);
            _gridRespaldos.Size = new Size(856, 190);
            _grupoRespaldos.Controls.Add(_gridRespaldos);

            _btnRestaurar = Estilo.Boton("Restore selected backup");
            _btnRestaurar.Location = new Point(12, 224);
            _btnRestaurar.Click += (s, e) => RestaurarSeleccionado();
            _grupoRespaldos.Controls.Add(_btnRestaurar);

            _btnArchivo = Estilo.Boton("Restore from another file...");
            _btnArchivo.Location = new Point(210, 224);
            _btnArchivo.Click += (s, e) => RestaurarDesdeArchivo();
            _grupoRespaldos.Controls.Add(_btnArchivo);

            _btnRecalcular = Estilo.Boton("Recalculate the check digits...");
            _btnRecalcular.Location = new Point(430, 224);
            _btnRecalcular.Click += (s, e) => Recalcular();
            _grupoRespaldos.Controls.Add(_btnRecalcular);
            Controls.Add(_grupoRespaldos);

            Button salir = Estilo.Boton("Exit");
            salir.BackColor = Color.FromArgb(120, 125, 135);
            salir.Location = new Point(790, 652);
            salir.DialogResult = DialogResult.Cancel;
            Controls.Add(salir);
            CancelButton = salir;
            AcceptButton = _btnIngresar;

            MostrarResultado();
        }

        private void MostrarResultado()
        {
            _lblResumen.Text = Tr.T("{0} record(s) changed outside the system. Table check digit (DVV): {1}. Detected on {2}.",
                _resultado.RegistrosAlterados,
                Tr.T(_resultado.AlteradaLaTabla ? "does not match" : "correct"),
                _resultado.FechaVerificacion.ToString("dd/MM/yyyy HH:mm:ss"));

            _gridInconsistencias.DataSource = _resultado.Inconsistencias
                .Select(i => new InconsistenciaFila
                {
                    Tabla = i.Tabla,
                    Tipo = Tr.T(i.Tipo == TipoDigito.Horizontal ? "Record digit (DVH)" : "Table digit (DVV)"),
                    Registro = i.Referencia,
                    Detalle = i.Detalle
                })
                .ToList();

            Estilo.TitularColumnas(_gridInconsistencias);
        }

        private void Ingresar()
        {
            string usuario = _txtUsuario.Text;
            string clave = _txtPassword.Text;

            bool correcto = Estilo.Ejecutar(this, () => _servicio.Autenticar(usuario, clave));

            _txtPassword.Clear();

            if (!correcto)
            {
                return;
            }

            _txtUsuario.Enabled = false;
            _txtPassword.Enabled = false;
            _btnIngresar.Enabled = false;
            _lblSesion.Text = Tr.T("Signed in as {0}", _servicio.NombreUsuario);

            CargarRespaldos();
            _grupoRespaldos.Enabled = true;
        }

        private void CargarRespaldos()
        {
            List<RegistroRespaldo> respaldos = new List<RegistroRespaldo>();

            Estilo.Ejecutar(this, () => respaldos = _servicio.ObtenerRespaldos());

            _gridRespaldos.DataSource = respaldos.Select(RespaldoFila.De).ToList();
            Estilo.TitularColumnas(_gridRespaldos);

            // Se propone el más nuevo de los sanos.
            int primero = respaldos.FindIndex(r => r.Sano);

            if (primero >= 0 && primero < _gridRespaldos.Rows.Count)
            {
                _gridRespaldos.ClearSelection();
                _gridRespaldos.Rows[primero].Selected = true;
            }
        }

        private void RestaurarSeleccionado()
        {
            Guid? id = _gridRespaldos.CurrentRow == null ? null : Estilo.IdDe(_gridRespaldos.CurrentRow.DataBoundItem);

            if (!id.HasValue)
            {
                Estilo.Avisar(this, Tr.T("Select the backup to restore."));
                return;
            }

            RegistroRespaldo respaldo = _servicio.ObtenerRespaldos().FirstOrDefault(r => r.Id == id.Value);

            if (respaldo == null)
            {
                Estilo.Avisar(this, Tr.T("The backup was not found."));
                return;
            }

            string aviso = respaldo.Sano
                ? Tr.T("The business data will be replaced by the backup of {0}. The current data is saved first as a safety copy. Continue?",
                    respaldo.FechaHora.ToString("dd/MM/yyyy HH:mm"))
                : Tr.T("This backup was NOT verified as healthy. The business data will be replaced by the backup of {0}. Continue anyway?",
                    respaldo.FechaHora.ToString("dd/MM/yyyy HH:mm"));

            if (!Estilo.Confirmar(this, aviso))
            {
                return;
            }

            Restaurar(() => _servicio.RestaurarRespaldo(respaldo));
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

                if (!Estilo.Confirmar(this, Tr.T("The business data will be replaced by the content of the file '{0}'. The current data is saved first as a safety copy. Continue?", System.IO.Path.GetFileName(ruta))))
                {
                    return;
                }

                Restaurar(() => _servicio.RestaurarArchivo(ruta));
            }
        }

        private void Recalcular()
        {
            if (!Estilo.Confirmar(this, Tr.T("Recalculating accepts the data as it is now: any change made outside the system becomes valid. Do it only after checking that the data is correct. Continue?")))
            {
                return;
            }

            Restaurar(() => _servicio.RecalcularDigitos());
        }

        /// <summary>
        /// Ejecuta la reparación con una ventana de espera y decide si ya se puede entrar.
        /// </summary>
        private void Restaurar(Func<ResultadoIntegridad> reparacion)
        {
            ResultadoIntegridad resultado = null;

            bool terminado = Estilo.Ejecutar(this, () =>
                FrmEspera.Ejecutar("Repairing the data...", () =>
                {
                    resultado = reparacion();
                    return string.Empty;
                }));

            if (!terminado || resultado == null)
            {
                return;
            }

            if (resultado.Correcto)
            {
                Estilo.Informar(this, Tr.T("The data passed the integrity check. You can sign in now."));
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            _resultado = resultado;
            MostrarResultado();
            CargarRespaldos();
            Estilo.Avisar(this, Tr.T("The data still does not pass the integrity check. Choose another backup."));
        }
    }
}
