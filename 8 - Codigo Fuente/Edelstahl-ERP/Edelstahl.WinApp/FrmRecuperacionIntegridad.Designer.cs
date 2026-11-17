namespace Edelstahl.WinApp
{
    partial class FrmRecuperacionIntegridad
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle encabezado1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle celdas1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle alternas1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle encabezado2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle celdas2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle alternas2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.pnlBanda = new System.Windows.Forms.Panel();
            this.lblTituloBanda = new System.Windows.Forms.Label();
            this.lblMensajeBanda = new System.Windows.Forms.Label();
            this.lblResumen = new System.Windows.Forms.Label();
            this.grillaInconsistencias = new Edelstahl.WinApp.Ui.GrillaMarca();
            this.grpIdentificacion = new System.Windows.Forms.GroupBox();
            this.lblUsuarioCaption = new System.Windows.Forms.Label();
            this.txtUsuario = new System.Windows.Forms.TextBox();
            this.lblPasswordCaption = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.btnIngresar = new Edelstahl.WinApp.Ui.BotonEstilo();
            this.lblSesion = new System.Windows.Forms.Label();
            this.grpRespaldos = new System.Windows.Forms.GroupBox();
            this.grillaRespaldos = new Edelstahl.WinApp.Ui.GrillaMarca();
            this.btnRestaurar = new Edelstahl.WinApp.Ui.BotonEstilo();
            this.btnArchivo = new Edelstahl.WinApp.Ui.BotonEstilo();
            this.btnRecalcular = new Edelstahl.WinApp.Ui.BotonEstilo();
            this.btnSalir = new Edelstahl.WinApp.Ui.BotonEstilo();
            this.pnlBanda.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grillaInconsistencias)).BeginInit();
            this.grpIdentificacion.SuspendLayout();
            this.grpRespaldos.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grillaRespaldos)).BeginInit();
            this.SuspendLayout();
            //
            // pnlBanda
            //
            this.pnlBanda.BackColor = System.Drawing.Color.FromArgb(176, 58, 46);
            this.pnlBanda.Controls.Add(this.lblTituloBanda);
            this.pnlBanda.Controls.Add(this.lblMensajeBanda);
            this.pnlBanda.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlBanda.Location = new System.Drawing.Point(0, 0);
            this.pnlBanda.Name = "pnlBanda";
            this.pnlBanda.Size = new System.Drawing.Size(920, 78);
            this.pnlBanda.TabIndex = 0;
            //
            // lblTituloBanda
            //
            this.lblTituloBanda.AutoSize = true;
            this.lblTituloBanda.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTituloBanda.ForeColor = System.Drawing.Color.White;
            this.lblTituloBanda.Location = new System.Drawing.Point(20, 8);
            this.lblTituloBanda.Name = "lblTituloBanda";
            this.lblTituloBanda.Size = new System.Drawing.Size(215, 30);
            this.lblTituloBanda.TabIndex = 0;
            this.lblTituloBanda.Text = "Data integrity alert";
            //
            // lblMensajeBanda
            //
            this.lblMensajeBanda.ForeColor = System.Drawing.Color.White;
            this.lblMensajeBanda.Location = new System.Drawing.Point(22, 40);
            this.lblMensajeBanda.Name = "lblMensajeBanda";
            this.lblMensajeBanda.Size = new System.Drawing.Size(880, 36);
            this.lblMensajeBanda.TabIndex = 1;
            this.lblMensajeBanda.Text = "Access is blocked until the data is repaired.";
            //
            // lblResumen
            //
            this.lblResumen.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblResumen.Location = new System.Drawing.Point(20, 90);
            this.lblResumen.Name = "lblResumen";
            this.lblResumen.Size = new System.Drawing.Size(880, 36);
            this.lblResumen.TabIndex = 1;
            //
            // grillaInconsistencias
            //
            this.grillaInconsistencias.AllowUserToAddRows = false;
            this.grillaInconsistencias.AllowUserToDeleteRows = false;
            this.grillaInconsistencias.AllowUserToResizeRows = false;
            alternas1.BackColor = System.Drawing.Color.FromArgb(255, 245, 210);
            this.grillaInconsistencias.AlternatingRowsDefaultCellStyle = alternas1;
            this.grillaInconsistencias.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.grillaInconsistencias.BackgroundColor = System.Drawing.Color.FromArgb(255, 250, 228);
            this.grillaInconsistencias.BorderStyle = System.Windows.Forms.BorderStyle.None;
            encabezado1.BackColor = System.Drawing.Color.FromArgb(54, 69, 98);
            encabezado1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            encabezado1.ForeColor = System.Drawing.Color.White;
            this.grillaInconsistencias.ColumnHeadersDefaultCellStyle = encabezado1;
            this.grillaInconsistencias.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            celdas1.BackColor = System.Drawing.Color.FromArgb(255, 253, 242);
            this.grillaInconsistencias.DefaultCellStyle = celdas1;
            this.grillaInconsistencias.EnableHeadersVisualStyles = false;
            this.grillaInconsistencias.GridColor = System.Drawing.Color.FromArgb(230, 218, 170);
            this.grillaInconsistencias.Location = new System.Drawing.Point(20, 130);
            this.grillaInconsistencias.MultiSelect = false;
            this.grillaInconsistencias.Name = "grillaInconsistencias";
            this.grillaInconsistencias.ReadOnly = true;
            this.grillaInconsistencias.RowHeadersVisible = false;
            this.grillaInconsistencias.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grillaInconsistencias.Size = new System.Drawing.Size(880, 130);
            this.grillaInconsistencias.TabIndex = 2;
            //
            // grpIdentificacion
            //
            this.grpIdentificacion.Controls.Add(this.lblUsuarioCaption);
            this.grpIdentificacion.Controls.Add(this.txtUsuario);
            this.grpIdentificacion.Controls.Add(this.lblPasswordCaption);
            this.grpIdentificacion.Controls.Add(this.txtPassword);
            this.grpIdentificacion.Controls.Add(this.btnIngresar);
            this.grpIdentificacion.Controls.Add(this.lblSesion);
            this.grpIdentificacion.Location = new System.Drawing.Point(20, 272);
            this.grpIdentificacion.Name = "grpIdentificacion";
            this.grpIdentificacion.Size = new System.Drawing.Size(880, 84);
            this.grpIdentificacion.TabIndex = 3;
            this.grpIdentificacion.TabStop = false;
            this.grpIdentificacion.Text = "To repair it, sign in with a user that can manage backups";
            //
            // lblUsuarioCaption
            //
            this.lblUsuarioCaption.AutoSize = true;
            this.lblUsuarioCaption.Location = new System.Drawing.Point(14, 26);
            this.lblUsuarioCaption.Name = "lblUsuarioCaption";
            this.lblUsuarioCaption.Size = new System.Drawing.Size(61, 15);
            this.lblUsuarioCaption.TabIndex = 0;
            this.lblUsuarioCaption.Text = "User name";
            //
            // txtUsuario
            //
            this.txtUsuario.Location = new System.Drawing.Point(14, 46);
            this.txtUsuario.Name = "txtUsuario";
            this.txtUsuario.Size = new System.Drawing.Size(230, 23);
            this.txtUsuario.TabIndex = 1;
            //
            // lblPasswordCaption
            //
            this.lblPasswordCaption.AutoSize = true;
            this.lblPasswordCaption.Location = new System.Drawing.Point(260, 26);
            this.lblPasswordCaption.Name = "lblPasswordCaption";
            this.lblPasswordCaption.Size = new System.Drawing.Size(57, 15);
            this.lblPasswordCaption.TabIndex = 2;
            this.lblPasswordCaption.Text = "Password";
            //
            // txtPassword
            //
            this.txtPassword.Location = new System.Drawing.Point(260, 46);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(230, 23);
            this.txtPassword.TabIndex = 3;
            this.txtPassword.UseSystemPasswordChar = true;
            //
            // btnIngresar
            //
            this.btnIngresar.AutoSize = true;
            this.btnIngresar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnIngresar.BackColor = System.Drawing.Color.FromArgb(52, 101, 164);
            this.btnIngresar.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnIngresar.FlatAppearance.BorderSize = 0;
            this.btnIngresar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnIngresar.ForeColor = System.Drawing.Color.White;
            this.btnIngresar.Location = new System.Drawing.Point(506, 44);
            this.btnIngresar.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.btnIngresar.MinimumSize = new System.Drawing.Size(90, 30);
            this.btnIngresar.Name = "btnIngresar";
            this.btnIngresar.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnIngresar.Size = new System.Drawing.Size(90, 30);
            this.btnIngresar.TabIndex = 4;
            this.btnIngresar.Tag = "Sign in";
            this.btnIngresar.Text = "Sign in";
            this.btnIngresar.UseVisualStyleBackColor = false;
            //
            // lblSesion
            //
            this.lblSesion.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblSesion.ForeColor = System.Drawing.Color.FromArgb(54, 69, 98);
            this.lblSesion.Location = new System.Drawing.Point(620, 48);
            this.lblSesion.Name = "lblSesion";
            this.lblSesion.Size = new System.Drawing.Size(250, 24);
            this.lblSesion.TabIndex = 5;
            //
            // grpRespaldos
            //
            this.grpRespaldos.Controls.Add(this.grillaRespaldos);
            this.grpRespaldos.Controls.Add(this.btnRestaurar);
            this.grpRespaldos.Controls.Add(this.btnArchivo);
            this.grpRespaldos.Controls.Add(this.btnRecalcular);
            this.grpRespaldos.Enabled = false;
            this.grpRespaldos.Location = new System.Drawing.Point(20, 368);
            this.grpRespaldos.Name = "grpRespaldos";
            this.grpRespaldos.Size = new System.Drawing.Size(880, 270);
            this.grpRespaldos.TabIndex = 4;
            this.grpRespaldos.TabStop = false;
            this.grpRespaldos.Text = "Backups made (choose the one to restore)";
            //
            // grillaRespaldos
            //
            this.grillaRespaldos.AllowUserToAddRows = false;
            this.grillaRespaldos.AllowUserToDeleteRows = false;
            this.grillaRespaldos.AllowUserToResizeRows = false;
            alternas2.BackColor = System.Drawing.Color.FromArgb(255, 245, 210);
            this.grillaRespaldos.AlternatingRowsDefaultCellStyle = alternas2;
            this.grillaRespaldos.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.grillaRespaldos.BackgroundColor = System.Drawing.Color.FromArgb(255, 250, 228);
            this.grillaRespaldos.BorderStyle = System.Windows.Forms.BorderStyle.None;
            encabezado2.BackColor = System.Drawing.Color.FromArgb(54, 69, 98);
            encabezado2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            encabezado2.ForeColor = System.Drawing.Color.White;
            this.grillaRespaldos.ColumnHeadersDefaultCellStyle = encabezado2;
            this.grillaRespaldos.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            celdas2.BackColor = System.Drawing.Color.FromArgb(255, 253, 242);
            this.grillaRespaldos.DefaultCellStyle = celdas2;
            this.grillaRespaldos.EnableHeadersVisualStyles = false;
            this.grillaRespaldos.GridColor = System.Drawing.Color.FromArgb(230, 218, 170);
            this.grillaRespaldos.Location = new System.Drawing.Point(12, 22);
            this.grillaRespaldos.MultiSelect = false;
            this.grillaRespaldos.Name = "grillaRespaldos";
            this.grillaRespaldos.ReadOnly = true;
            this.grillaRespaldos.RowHeadersVisible = false;
            this.grillaRespaldos.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grillaRespaldos.Size = new System.Drawing.Size(856, 190);
            this.grillaRespaldos.TabIndex = 0;
            //
            // btnRestaurar
            //
            this.btnRestaurar.AutoSize = true;
            this.btnRestaurar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnRestaurar.BackColor = System.Drawing.Color.FromArgb(52, 101, 164);
            this.btnRestaurar.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRestaurar.FlatAppearance.BorderSize = 0;
            this.btnRestaurar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRestaurar.ForeColor = System.Drawing.Color.White;
            this.btnRestaurar.Location = new System.Drawing.Point(12, 224);
            this.btnRestaurar.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.btnRestaurar.MinimumSize = new System.Drawing.Size(90, 30);
            this.btnRestaurar.Name = "btnRestaurar";
            this.btnRestaurar.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnRestaurar.Size = new System.Drawing.Size(180, 30);
            this.btnRestaurar.TabIndex = 1;
            this.btnRestaurar.Tag = "Restore selected backup";
            this.btnRestaurar.Text = "Restore selected backup";
            this.btnRestaurar.UseVisualStyleBackColor = false;
            //
            // btnArchivo
            //
            this.btnArchivo.AutoSize = true;
            this.btnArchivo.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnArchivo.BackColor = System.Drawing.Color.FromArgb(52, 101, 164);
            this.btnArchivo.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnArchivo.FlatAppearance.BorderSize = 0;
            this.btnArchivo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnArchivo.ForeColor = System.Drawing.Color.White;
            this.btnArchivo.Location = new System.Drawing.Point(210, 224);
            this.btnArchivo.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.btnArchivo.MinimumSize = new System.Drawing.Size(90, 30);
            this.btnArchivo.Name = "btnArchivo";
            this.btnArchivo.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnArchivo.Size = new System.Drawing.Size(200, 30);
            this.btnArchivo.TabIndex = 2;
            this.btnArchivo.Tag = "Restore from another file...";
            this.btnArchivo.Text = "Restore from another file...";
            this.btnArchivo.UseVisualStyleBackColor = false;
            //
            // btnRecalcular
            //
            this.btnRecalcular.AutoSize = true;
            this.btnRecalcular.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnRecalcular.BackColor = System.Drawing.Color.FromArgb(52, 101, 164);
            this.btnRecalcular.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRecalcular.FlatAppearance.BorderSize = 0;
            this.btnRecalcular.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRecalcular.ForeColor = System.Drawing.Color.White;
            this.btnRecalcular.Location = new System.Drawing.Point(430, 224);
            this.btnRecalcular.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.btnRecalcular.MinimumSize = new System.Drawing.Size(90, 30);
            this.btnRecalcular.Name = "btnRecalcular";
            this.btnRecalcular.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnRecalcular.Size = new System.Drawing.Size(210, 30);
            this.btnRecalcular.TabIndex = 3;
            this.btnRecalcular.Tag = "Recalculate the check digits...";
            this.btnRecalcular.Text = "Recalculate the check digits...";
            this.btnRecalcular.UseVisualStyleBackColor = false;
            //
            // btnSalir
            //
            this.btnSalir.AutoSize = true;
            this.btnSalir.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnSalir.BackColor = System.Drawing.Color.FromArgb(120, 125, 135);
            this.btnSalir.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSalir.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnSalir.FlatAppearance.BorderSize = 0;
            this.btnSalir.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSalir.ForeColor = System.Drawing.Color.White;
            this.btnSalir.Location = new System.Drawing.Point(790, 652);
            this.btnSalir.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.btnSalir.MinimumSize = new System.Drawing.Size(90, 30);
            this.btnSalir.Name = "btnSalir";
            this.btnSalir.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnSalir.Size = new System.Drawing.Size(90, 30);
            this.btnSalir.TabIndex = 5;
            this.btnSalir.Tag = "Exit";
            this.btnSalir.Text = "Exit";
            this.btnSalir.UseVisualStyleBackColor = false;
            //
            // FrmRecuperacionIntegridad
            //
            this.AcceptButton = this.btnIngresar;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.btnSalir;
            this.ClientSize = new System.Drawing.Size(920, 700);
            this.Controls.Add(this.pnlBanda);
            this.Controls.Add(this.lblResumen);
            this.Controls.Add(this.grillaInconsistencias);
            this.Controls.Add(this.grpIdentificacion);
            this.Controls.Add(this.grpRespaldos);
            this.Controls.Add(this.btnSalir);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(920, 700);
            this.Name = "FrmRecuperacionIntegridad";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Data integrity alert";
            this.pnlBanda.ResumeLayout(false);
            this.pnlBanda.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grillaInconsistencias)).EndInit();
            this.grpIdentificacion.ResumeLayout(false);
            this.grpIdentificacion.PerformLayout();
            this.grpRespaldos.ResumeLayout(false);
            this.grpRespaldos.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grillaRespaldos)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlBanda;
        private System.Windows.Forms.Label lblTituloBanda;
        private System.Windows.Forms.Label lblMensajeBanda;
        private System.Windows.Forms.Label lblResumen;
        private Edelstahl.WinApp.Ui.GrillaMarca grillaInconsistencias;
        private System.Windows.Forms.GroupBox grpIdentificacion;
        private System.Windows.Forms.Label lblUsuarioCaption;
        private System.Windows.Forms.TextBox txtUsuario;
        private System.Windows.Forms.Label lblPasswordCaption;
        private System.Windows.Forms.TextBox txtPassword;
        private Edelstahl.WinApp.Ui.BotonEstilo btnIngresar;
        private System.Windows.Forms.Label lblSesion;
        private System.Windows.Forms.GroupBox grpRespaldos;
        private Edelstahl.WinApp.Ui.GrillaMarca grillaRespaldos;
        private Edelstahl.WinApp.Ui.BotonEstilo btnRestaurar;
        private Edelstahl.WinApp.Ui.BotonEstilo btnArchivo;
        private Edelstahl.WinApp.Ui.BotonEstilo btnRecalcular;
        private Edelstahl.WinApp.Ui.BotonEstilo btnSalir;
    }
}
