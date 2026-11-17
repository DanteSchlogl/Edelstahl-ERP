namespace Edelstahl.WinApp.Ui
{
    partial class FrmFamiliasUsuario
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
            this.lblEncabezado = new System.Windows.Forms.Label();
            this.lblDisponibles = new System.Windows.Forms.Label();
            this.lstDisponibles = new System.Windows.Forms.ListBox();
            this.btnAsignar = new Edelstahl.WinApp.Ui.BotonEstilo();
            this.btnQuitar = new Edelstahl.WinApp.Ui.BotonEstilo();
            this.lblAsignadas = new System.Windows.Forms.Label();
            this.lstAsignadas = new System.Windows.Forms.ListBox();
            this.lblArbol = new System.Windows.Forms.Label();
            this.tvPermisos = new System.Windows.Forms.TreeView();
            this.btnCerrar = new Edelstahl.WinApp.Ui.BotonEstilo();
            this.SuspendLayout();
            //
            // lblEncabezado
            //
            this.lblEncabezado.BackColor = System.Drawing.Color.FromArgb(226, 234, 247);
            this.lblEncabezado.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblEncabezado.ForeColor = System.Drawing.Color.FromArgb(54, 69, 98);
            this.lblEncabezado.Location = new System.Drawing.Point(0, 0);
            this.lblEncabezado.Name = "lblEncabezado";
            this.lblEncabezado.Padding = new System.Windows.Forms.Padding(12, 8, 12, 0);
            this.lblEncabezado.Size = new System.Drawing.Size(760, 64);
            this.lblEncabezado.TabIndex = 0;
            this.lblEncabezado.Text = "Families of user";
            //
            // lblDisponibles
            //
            this.lblDisponibles.AutoSize = true;
            this.lblDisponibles.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblDisponibles.Location = new System.Drawing.Point(16, 76);
            this.lblDisponibles.Name = "lblDisponibles";
            this.lblDisponibles.Size = new System.Drawing.Size(106, 15);
            this.lblDisponibles.TabIndex = 1;
            this.lblDisponibles.Text = "Available families";
            //
            // lstDisponibles
            //
            this.lstDisponibles.FormattingEnabled = true;
            this.lstDisponibles.IntegralHeight = false;
            this.lstDisponibles.ItemHeight = 15;
            this.lstDisponibles.Location = new System.Drawing.Point(16, 98);
            this.lstDisponibles.Name = "lstDisponibles";
            this.lstDisponibles.Size = new System.Drawing.Size(300, 152);
            this.lstDisponibles.TabIndex = 2;
            //
            // btnAsignar
            //
            this.btnAsignar.AutoSize = true;
            this.btnAsignar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnAsignar.BackColor = System.Drawing.Color.FromArgb(52, 101, 164);
            this.btnAsignar.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAsignar.FlatAppearance.BorderSize = 0;
            this.btnAsignar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAsignar.ForeColor = System.Drawing.Color.White;
            this.btnAsignar.Location = new System.Drawing.Point(330, 134);
            this.btnAsignar.MinimumSize = new System.Drawing.Size(100, 30);
            this.btnAsignar.Name = "btnAsignar";
            this.btnAsignar.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnAsignar.Size = new System.Drawing.Size(100, 30);
            this.btnAsignar.TabIndex = 3;
            this.btnAsignar.Text = "Assign";
            this.btnAsignar.UseVisualStyleBackColor = false;
            //
            // btnQuitar
            //
            this.btnQuitar.AutoSize = true;
            this.btnQuitar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnQuitar.BackColor = System.Drawing.Color.FromArgb(52, 101, 164);
            this.btnQuitar.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnQuitar.FlatAppearance.BorderSize = 0;
            this.btnQuitar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnQuitar.ForeColor = System.Drawing.Color.White;
            this.btnQuitar.Location = new System.Drawing.Point(330, 176);
            this.btnQuitar.MinimumSize = new System.Drawing.Size(100, 30);
            this.btnQuitar.Name = "btnQuitar";
            this.btnQuitar.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnQuitar.Size = new System.Drawing.Size(100, 30);
            this.btnQuitar.TabIndex = 4;
            this.btnQuitar.Text = "Remove";
            this.btnQuitar.UseVisualStyleBackColor = false;
            //
            // lblAsignadas
            //
            this.lblAsignadas.AutoSize = true;
            this.lblAsignadas.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblAsignadas.Location = new System.Drawing.Point(444, 76);
            this.lblAsignadas.Name = "lblAsignadas";
            this.lblAsignadas.Size = new System.Drawing.Size(101, 15);
            this.lblAsignadas.TabIndex = 5;
            this.lblAsignadas.Text = "Assigned families";
            //
            // lstAsignadas
            //
            this.lstAsignadas.FormattingEnabled = true;
            this.lstAsignadas.IntegralHeight = false;
            this.lstAsignadas.ItemHeight = 15;
            this.lstAsignadas.Location = new System.Drawing.Point(444, 98);
            this.lstAsignadas.Name = "lstAsignadas";
            this.lstAsignadas.Size = new System.Drawing.Size(300, 152);
            this.lstAsignadas.TabIndex = 6;
            //
            // lblArbol
            //
            this.lblArbol.AutoSize = true;
            this.lblArbol.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblArbol.Location = new System.Drawing.Point(16, 264);
            this.lblArbol.Name = "lblArbol";
            this.lblArbol.Size = new System.Drawing.Size(398, 15);
            this.lblArbol.TabIndex = 7;
            this.lblArbol.Text = "Permissions this user receives through families and direct permissions";
            //
            // tvPermisos
            //
            this.tvPermisos.Location = new System.Drawing.Point(16, 286);
            this.tvPermisos.Name = "tvPermisos";
            this.tvPermisos.ShowLines = true;
            this.tvPermisos.Size = new System.Drawing.Size(728, 198);
            this.tvPermisos.TabIndex = 8;
            //
            // btnCerrar
            //
            this.btnCerrar.AutoSize = true;
            this.btnCerrar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnCerrar.BackColor = System.Drawing.Color.FromArgb(52, 101, 164);
            this.btnCerrar.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCerrar.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnCerrar.FlatAppearance.BorderSize = 0;
            this.btnCerrar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCerrar.ForeColor = System.Drawing.Color.White;
            this.btnCerrar.Location = new System.Drawing.Point(644, 496);
            this.btnCerrar.MinimumSize = new System.Drawing.Size(100, 30);
            this.btnCerrar.Name = "btnCerrar";
            this.btnCerrar.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnCerrar.Size = new System.Drawing.Size(100, 30);
            this.btnCerrar.TabIndex = 9;
            this.btnCerrar.Text = "Close";
            this.btnCerrar.UseVisualStyleBackColor = false;
            //
            // FrmFamiliasUsuario
            //
            this.AcceptButton = this.btnCerrar;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(255, 252, 238);
            this.CancelButton = this.btnCerrar;
            this.ClientSize = new System.Drawing.Size(760, 540);
            this.Controls.Add(this.lblDisponibles);
            this.Controls.Add(this.lstDisponibles);
            this.Controls.Add(this.btnAsignar);
            this.Controls.Add(this.btnQuitar);
            this.Controls.Add(this.lblAsignadas);
            this.Controls.Add(this.lstAsignadas);
            this.Controls.Add(this.lblArbol);
            this.Controls.Add(this.tvPermisos);
            this.Controls.Add(this.btnCerrar);
            this.Controls.Add(this.lblEncabezado);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmFamiliasUsuario";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Families of user";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblEncabezado;
        private System.Windows.Forms.Label lblDisponibles;
        private System.Windows.Forms.ListBox lstDisponibles;
        private Edelstahl.WinApp.Ui.BotonEstilo btnAsignar;
        private Edelstahl.WinApp.Ui.BotonEstilo btnQuitar;
        private System.Windows.Forms.Label lblAsignadas;
        private System.Windows.Forms.ListBox lstAsignadas;
        private System.Windows.Forms.Label lblArbol;
        private System.Windows.Forms.TreeView tvPermisos;
        private Edelstahl.WinApp.Ui.BotonEstilo btnCerrar;
    }
}
