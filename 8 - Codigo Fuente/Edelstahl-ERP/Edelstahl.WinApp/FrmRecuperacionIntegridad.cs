using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Edelstahl.BLL.Services;
using Edelstahl.BLL.Services.Respaldo;
using Edelstahl.Domain.Integrity;
using Edelstahl.Services.DomainModel.Security;
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
    /// La distribución de los controles está en FrmRecuperacionIntegridad.Designer.cs.
    /// </summary>
    public sealed partial class FrmRecuperacionIntegridad : Form
    {
        private RecuperacionService _servicio;
        private ResultadoIntegridad _resultado;

        // Constructor sin parámetros: lo usa el diseñador de Visual Studio.
        public FrmRecuperacionIntegridad()
        {
            InitializeComponent();
        }

        public FrmRecuperacionIntegridad(ResultadoIntegridad resultado)
            : this(resultado, new RecuperacionService())
        {
        }

        public FrmRecuperacionIntegridad(ResultadoIntegridad resultado, RecuperacionService servicio)
            : this()
        {
            _resultado = resultado ?? throw new ArgumentNullException(nameof(resultado));
            _servicio = servicio ?? throw new ArgumentNullException(nameof(servicio));

            Text = Tr.T("Data integrity alert");
            Marca.AplicarIcono(this);

            lblTituloBanda.Text = Tr.T("Data integrity alert");
            lblMensajeBanda.Text = Tr.T("The system found data that was changed outside of Edelstahl ERP. Access is blocked until it is repaired.");

            grpIdentificacion.Text = Tr.T("To repair it, sign in with a user that can manage backups");
            lblUsuarioCaption.Text = Tr.T("User name");
            lblPasswordCaption.Text = Tr.T("Password");
            btnIngresar.Text = Tr.T("Sign in");
            btnIngresar.Click += (s, e) => Ingresar();

            grpRespaldos.Text = Tr.T("Backups made (choose the one to restore)");
            btnRestaurar.Text = Tr.T("Restore selected backup");
            btnRestaurar.Click += (s, e) => RestaurarSeleccionado();
            btnArchivo.Text = Tr.T("Restore from another file...");
            btnArchivo.Click += (s, e) => RestaurarDesdeArchivo();
            btnRecalcular.Text = Tr.T("Recalculate the check digits...");
            btnRecalcular.Click += (s, e) => Recalcular();

            btnSalir.Text = Tr.T("Exit");

            MostrarResultado();
        }
        private void MostrarResultado()
        {
            lblResumen.Text = Tr.T("{0} record(s) changed outside the system. Table check digit (DVV): {1}. Detected on {2}.",
                _resultado.RegistrosAlterados,
                Tr.T(_resultado.AlteradaLaTabla ? "does not match" : "correct"),
                _resultado.FechaVerificacion.ToString("dd/MM/yyyy HH:mm:ss"));

            grillaInconsistencias.DataSource = _resultado.Inconsistencias
                .Select(i => new InconsistenciaFila
                {
                    Tabla = i.Tabla,
                    Tipo = Tr.T(i.Tipo == TipoDigito.Horizontal ? "Record digit (DVH)" : "Table digit (DVV)"),
                    Registro = i.Referencia,
                    Detalle = i.Detalle
                })
                .ToList();

            Estilo.TitularColumnas(grillaInconsistencias);
        }

        private void Ingresar()
        {
            string usuario = txtUsuario.Text;
            string clave = txtPassword.Text;

            bool correcto = Estilo.Ejecutar(this, () => _servicio.Autenticar(usuario, clave));

            txtPassword.Clear();

            if (!correcto)
            {
                return;
            }

            txtUsuario.Enabled = false;
            txtPassword.Enabled = false;
            btnIngresar.Enabled = false;
            lblSesion.Text = Tr.T("Signed in as {0}", _servicio.NombreUsuario);

            CargarRespaldos();
            grpRespaldos.Enabled = true;
        }

        private void CargarRespaldos()
        {
            List<RegistroRespaldo> respaldos = new List<RegistroRespaldo>();

            Estilo.Ejecutar(this, () => respaldos = _servicio.ObtenerRespaldos());

            grillaRespaldos.DataSource = respaldos.Select(RespaldoFila.De).ToList();
            Estilo.TitularColumnas(grillaRespaldos);

            // Se propone el más nuevo de los sanos.
            int primero = respaldos.FindIndex(r => r.Sano);

            if (primero >= 0 && primero < grillaRespaldos.Rows.Count)
            {
                grillaRespaldos.ClearSelection();
                grillaRespaldos.Rows[primero].Selected = true;
            }
        }

        private void RestaurarSeleccionado()
        {
            Guid? id = grillaRespaldos.CurrentRow == null ? null : Estilo.IdDe(grillaRespaldos.CurrentRow.DataBoundItem);

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
