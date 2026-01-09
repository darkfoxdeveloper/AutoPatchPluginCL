namespace AutoPatch
{
    partial class Main
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            this.pBarProgress = new System.Windows.Forms.ProgressBar();
            this.lblCurrentVer = new System.Windows.Forms.Label();
            this.bgWorkerAutoPatch = new System.ComponentModel.BackgroundWorker();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnPlay = new MetroFramework.Controls.MetroButton();
            this.contextMenu = new MetroFramework.Controls.MetroContextMenu(this.components);
            this.showHideTitleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveLayoutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // pBarProgress
            // 
            this.pBarProgress.Location = new System.Drawing.Point(34, 143);
            this.pBarProgress.Name = "pBarProgress";
            this.pBarProgress.Size = new System.Drawing.Size(673, 33);
            this.pBarProgress.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.pBarProgress.TabIndex = 0;
            // 
            // lblCurrentVer
            // 
            this.lblCurrentVer.AutoSize = true;
            this.lblCurrentVer.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.999999F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCurrentVer.Location = new System.Drawing.Point(582, 30);
            this.lblCurrentVer.Name = "lblCurrentVer";
            this.lblCurrentVer.Size = new System.Drawing.Size(156, 25);
            this.lblCurrentVer.TabIndex = 1;
            this.lblCurrentVer.Text = "Current version: ";
            // 
            // bgWorkerAutoPatch
            // 
            this.bgWorkerAutoPatch.WorkerReportsProgress = true;
            this.bgWorkerAutoPatch.WorkerSupportsCancellation = true;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.999999F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStatus.Location = new System.Drawing.Point(31, 112);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(140, 25);
            this.lblStatus.TabIndex = 2;
            this.lblStatus.Text = "Downloading...";
            this.lblStatus.Click += new System.EventHandler(this.LblStatus_Click);
            // 
            // btnPlay
            // 
            this.btnPlay.Enabled = false;
            this.btnPlay.Location = new System.Drawing.Point(321, 182);
            this.btnPlay.Name = "btnPlay";
            this.btnPlay.Size = new System.Drawing.Size(150, 35);
            this.btnPlay.TabIndex = 3;
            this.btnPlay.Text = "Launch ConquerLoader";
            this.btnPlay.UseSelectable = true;
            this.btnPlay.Click += new System.EventHandler(this.BtnPlay_Click);
            // 
            // contextMenu
            // 
            this.contextMenu.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showHideTitleToolStripMenuItem,
            this.saveLayoutToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.contextMenu.Name = "contextMenu";
            this.contextMenu.Size = new System.Drawing.Size(241, 133);
            // 
            // showHideTitleToolStripMenuItem
            // 
            this.showHideTitleToolStripMenuItem.Name = "showHideTitleToolStripMenuItem";
            this.showHideTitleToolStripMenuItem.Size = new System.Drawing.Size(240, 32);
            this.showHideTitleToolStripMenuItem.Text = "Show/Hide Title";
            this.showHideTitleToolStripMenuItem.Click += new System.EventHandler(this.ShowHideTitleToolStripMenuItem_Click);
            // 
            // saveLayoutToolStripMenuItem
            // 
            this.saveLayoutToolStripMenuItem.Name = "saveLayoutToolStripMenuItem";
            this.saveLayoutToolStripMenuItem.Size = new System.Drawing.Size(240, 32);
            this.saveLayoutToolStripMenuItem.Text = "Save layout";
            this.saveLayoutToolStripMenuItem.Click += new System.EventHandler(this.SaveLayoutToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(240, 32);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItem_Click);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(728, 225);
            this.ContextMenuStrip = this.contextMenu;
            this.Controls.Add(this.btnPlay);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.lblCurrentVer);
            this.Controls.Add(this.pBarProgress);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Main";
            this.Padding = new System.Windows.Forms.Padding(18, 60, 18, 19);
            this.Resizable = false;
            this.Text = "AutoPatch for ConquerLoader";
            this.Load += new System.EventHandler(this.Main_Load);
            this.contextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar pBarProgress;
        private System.Windows.Forms.Label lblCurrentVer;
        private System.ComponentModel.BackgroundWorker bgWorkerAutoPatch;
        private System.Windows.Forms.Label lblStatus;
        private MetroFramework.Controls.MetroButton btnPlay;
        private MetroFramework.Controls.MetroContextMenu contextMenu;
        private System.Windows.Forms.ToolStripMenuItem saveLayoutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showHideTitleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
    }
}

