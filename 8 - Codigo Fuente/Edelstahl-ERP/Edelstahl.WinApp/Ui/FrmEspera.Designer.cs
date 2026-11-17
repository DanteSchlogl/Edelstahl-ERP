namespace Edelstahl.WinApp.Ui
{
    partial class FrmEspera
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
            this.lblMensaje = new System.Windows.Forms.Label();
            this.barra = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            //
            // lblMensaje
            //
            this.lblMensaje.Location = new System.Drawing.Point(16, 14);
            this.lblMensaje.Name = "lblMensaje";
            this.lblMensaje.Size = new System.Drawing.Size(388, 24);
            this.lblMensaje.TabIndex = 0;
            this.lblMensaje.Text = "Please wait...";
            //
            // barra
            //
            this.barra.Location = new System.Drawing.Point(16, 46);
            this.barra.Name = "barra";
            this.barra.Size = new System.Drawing.Size(388, 20);
            this.barra.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.barra.TabIndex = 1;
            //
            // FrmEspera
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(420, 90);
            this.ControlBox = false;
            this.Controls.Add(this.lblMensaje);
            this.Controls.Add(this.barra);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "FrmEspera";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Edelstahl ERP";
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblMensaje;
        private System.Windows.Forms.ProgressBar barra;
    }
}
