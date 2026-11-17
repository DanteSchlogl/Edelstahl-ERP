namespace Edelstahl.WinApp
{
    partial class FrmRecuperarPassword
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
            this.lblTitulo = new System.Windows.Forms.Label();
            this.lblUsuarioCaption = new System.Windows.Forms.Label();
            this.txtUsuario = new System.Windows.Forms.TextBox();
            this.btnVerPregunta = new System.Windows.Forms.Button();
            this.lblPregunta = new System.Windows.Forms.Label();
            this.lblRespuestaCaption = new System.Windows.Forms.Label();
            this.txtRespuesta = new System.Windows.Forms.TextBox();
            this.lblNuevaCaption = new System.Windows.Forms.Label();
            this.txtNueva = new System.Windows.Forms.TextBox();
            this.lblConfirmarCaption = new System.Windows.Forms.Label();
            this.txtConfirmar = new System.Windows.Forms.TextBox();
            this.chkMostrar = new System.Windows.Forms.CheckBox();
            this.btnCambiar = new System.Windows.Forms.Button();
            this.btnCancelar = new System.Windows.Forms.Button();
            this.linea = new System.Windows.Forms.Panel();
            this.lblContactoTitulo = new System.Windows.Forms.Label();
            this.lblContacto = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // lblTitulo
            //
            this.lblTitulo.AutoSize = true;
            this.lblTitulo.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitulo.ForeColor = System.Drawing.Color.FromArgb(54, 69, 98);
            this.lblTitulo.Location = new System.Drawing.Point(24, 16);
            this.lblTitulo.Name = "lblTitulo";
            this.lblTitulo.Size = new System.Drawing.Size(185, 30);
            this.lblTitulo.TabIndex = 0;
            this.lblTitulo.Text = "Recover password";
            //
            // lblUsuarioCaption
            //
            this.lblUsuarioCaption.AutoSize = true;
            this.lblUsuarioCaption.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblUsuarioCaption.Location = new System.Drawing.Point(24, 64);
            this.lblUsuarioCaption.Name = "lblUsuarioCaption";
            this.lblUsuarioCaption.Size = new System.Drawing.Size(65, 15);
            this.lblUsuarioCaption.TabIndex = 1;
            this.lblUsuarioCaption.Text = "User name";
            //
            // txtUsuario
            //
            this.txtUsuario.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.txtUsuario.Location = new System.Drawing.Point(24, 86);
            this.txtUsuario.Name = "txtUsuario";
            this.txtUsuario.Size = new System.Drawing.Size(330, 27);
            this.txtUsuario.TabIndex = 2;
            //
            // btnVerPregunta
            //
            this.btnVerPregunta.BackColor = System.Drawing.Color.FromArgb(103, 120, 166);
            this.btnVerPregunta.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnVerPregunta.FlatAppearance.BorderSize = 0;
            this.btnVerPregunta.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnVerPregunta.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnVerPregunta.ForeColor = System.Drawing.Color.White;
            this.btnVerPregunta.Location = new System.Drawing.Point(366, 84);
            this.btnVerPregunta.Name = "btnVerPregunta";
            this.btnVerPregunta.Size = new System.Drawing.Size(170, 34);
            this.btnVerPregunta.TabIndex = 3;
            this.btnVerPregunta.Text = "View security question";
            this.btnVerPregunta.UseVisualStyleBackColor = false;
            //
            // lblPregunta
            //
            this.lblPregunta.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblPregunta.ForeColor = System.Drawing.Color.FromArgb(54, 69, 98);
            this.lblPregunta.Location = new System.Drawing.Point(24, 126);
            this.lblPregunta.Name = "lblPregunta";
            this.lblPregunta.Size = new System.Drawing.Size(512, 40);
            this.lblPregunta.TabIndex = 4;
            //
            // lblRespuestaCaption
            //
            this.lblRespuestaCaption.AutoSize = true;
            this.lblRespuestaCaption.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblRespuestaCaption.Location = new System.Drawing.Point(24, 176);
            this.lblRespuestaCaption.Name = "lblRespuestaCaption";
            this.lblRespuestaCaption.Size = new System.Drawing.Size(47, 15);
            this.lblRespuestaCaption.TabIndex = 5;
            this.lblRespuestaCaption.Text = "Answer";
            //
            // txtRespuesta
            //
            this.txtRespuesta.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.txtRespuesta.Location = new System.Drawing.Point(24, 198);
            this.txtRespuesta.Name = "txtRespuesta";
            this.txtRespuesta.Size = new System.Drawing.Size(512, 27);
            this.txtRespuesta.TabIndex = 6;
            this.txtRespuesta.UseSystemPasswordChar = true;
            //
            // lblNuevaCaption
            //
            this.lblNuevaCaption.AutoSize = true;
            this.lblNuevaCaption.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblNuevaCaption.Location = new System.Drawing.Point(24, 244);
            this.lblNuevaCaption.Name = "lblNuevaCaption";
            this.lblNuevaCaption.Size = new System.Drawing.Size(84, 15);
            this.lblNuevaCaption.TabIndex = 7;
            this.lblNuevaCaption.Text = "New password";
            //
            // txtNueva
            //
            this.txtNueva.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.txtNueva.Location = new System.Drawing.Point(24, 266);
            this.txtNueva.Name = "txtNueva";
            this.txtNueva.Size = new System.Drawing.Size(512, 27);
            this.txtNueva.TabIndex = 8;
            this.txtNueva.UseSystemPasswordChar = true;
            //
            // lblConfirmarCaption
            //
            this.lblConfirmarCaption.AutoSize = true;
            this.lblConfirmarCaption.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblConfirmarCaption.Location = new System.Drawing.Point(24, 312);
            this.lblConfirmarCaption.Name = "lblConfirmarCaption";
            this.lblConfirmarCaption.Size = new System.Drawing.Size(104, 15);
            this.lblConfirmarCaption.TabIndex = 9;
            this.lblConfirmarCaption.Text = "Confirm password";
            //
            // txtConfirmar
            //
            this.txtConfirmar.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.txtConfirmar.Location = new System.Drawing.Point(24, 334);
            this.txtConfirmar.Name = "txtConfirmar";
            this.txtConfirmar.Size = new System.Drawing.Size(512, 27);
            this.txtConfirmar.TabIndex = 10;
            this.txtConfirmar.UseSystemPasswordChar = true;
            //
            // chkMostrar
            //
            this.chkMostrar.AutoSize = true;
            this.chkMostrar.Location = new System.Drawing.Point(24, 372);
            this.chkMostrar.Name = "chkMostrar";
            this.chkMostrar.Size = new System.Drawing.Size(107, 19);
            this.chkMostrar.TabIndex = 11;
            this.chkMostrar.Text = "Show password";
            this.chkMostrar.UseVisualStyleBackColor = true;
            //
            // btnCambiar
            //
            this.btnCambiar.BackColor = System.Drawing.Color.FromArgb(103, 120, 166);
            this.btnCambiar.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCambiar.FlatAppearance.BorderSize = 0;
            this.btnCambiar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCambiar.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnCambiar.ForeColor = System.Drawing.Color.White;
            this.btnCambiar.Location = new System.Drawing.Point(24, 412);
            this.btnCambiar.Name = "btnCambiar";
            this.btnCambiar.Size = new System.Drawing.Size(190, 34);
            this.btnCambiar.TabIndex = 12;
            this.btnCambiar.Text = "Change password";
            this.btnCambiar.UseVisualStyleBackColor = false;
            //
            // btnCancelar
            //
            this.btnCancelar.BackColor = System.Drawing.Color.FromArgb(120, 125, 135);
            this.btnCancelar.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancelar.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancelar.FlatAppearance.BorderSize = 0;
            this.btnCancelar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancelar.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnCancelar.ForeColor = System.Drawing.Color.White;
            this.btnCancelar.Location = new System.Drawing.Point(226, 412);
            this.btnCancelar.Name = "btnCancelar";
            this.btnCancelar.Size = new System.Drawing.Size(110, 34);
            this.btnCancelar.TabIndex = 13;
            this.btnCancelar.Text = "Cancel";
            this.btnCancelar.UseVisualStyleBackColor = false;
            //
            // linea
            //
            this.linea.BackColor = System.Drawing.Color.FromArgb(215, 220, 230);
            this.linea.Location = new System.Drawing.Point(24, 462);
            this.linea.Name = "linea";
            this.linea.Size = new System.Drawing.Size(512, 1);
            this.linea.TabIndex = 14;
            //
            // lblContactoTitulo
            //
            this.lblContactoTitulo.AutoSize = true;
            this.lblContactoTitulo.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblContactoTitulo.Location = new System.Drawing.Point(24, 476);
            this.lblContactoTitulo.Name = "lblContactoTitulo";
            this.lblContactoTitulo.Size = new System.Drawing.Size(285, 15);
            this.lblContactoTitulo.TabIndex = 15;
            this.lblContactoTitulo.Text = "If you cannot recover it, contact the administrator:";
            //
            // lblContacto
            //
            this.lblContacto.AutoSize = true;
            this.lblContacto.Location = new System.Drawing.Point(24, 500);
            this.lblContacto.Name = "lblContacto";
            this.lblContacto.Size = new System.Drawing.Size(0, 15);
            this.lblContacto.TabIndex = 16;
            //
            // FrmRecuperarPassword
            //
            this.AcceptButton = this.btnVerPregunta;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.btnCancelar;
            this.ClientSize = new System.Drawing.Size(560, 600);
            this.Controls.Add(this.lblTitulo);
            this.Controls.Add(this.lblUsuarioCaption);
            this.Controls.Add(this.txtUsuario);
            this.Controls.Add(this.btnVerPregunta);
            this.Controls.Add(this.lblPregunta);
            this.Controls.Add(this.lblRespuestaCaption);
            this.Controls.Add(this.txtRespuesta);
            this.Controls.Add(this.lblNuevaCaption);
            this.Controls.Add(this.txtNueva);
            this.Controls.Add(this.lblConfirmarCaption);
            this.Controls.Add(this.txtConfirmar);
            this.Controls.Add(this.chkMostrar);
            this.Controls.Add(this.btnCambiar);
            this.Controls.Add(this.btnCancelar);
            this.Controls.Add(this.linea);
            this.Controls.Add(this.lblContactoTitulo);
            this.Controls.Add(this.lblContacto);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmRecuperarPassword";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Recover password";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblTitulo;
        private System.Windows.Forms.Label lblUsuarioCaption;
        private System.Windows.Forms.TextBox txtUsuario;
        private System.Windows.Forms.Button btnVerPregunta;
        private System.Windows.Forms.Label lblPregunta;
        private System.Windows.Forms.Label lblRespuestaCaption;
        private System.Windows.Forms.TextBox txtRespuesta;
        private System.Windows.Forms.Label lblNuevaCaption;
        private System.Windows.Forms.TextBox txtNueva;
        private System.Windows.Forms.Label lblConfirmarCaption;
        private System.Windows.Forms.TextBox txtConfirmar;
        private System.Windows.Forms.CheckBox chkMostrar;
        private System.Windows.Forms.Button btnCambiar;
        private System.Windows.Forms.Button btnCancelar;
        private System.Windows.Forms.Panel linea;
        private System.Windows.Forms.Label lblContactoTitulo;
        private System.Windows.Forms.Label lblContacto;
    }
}
