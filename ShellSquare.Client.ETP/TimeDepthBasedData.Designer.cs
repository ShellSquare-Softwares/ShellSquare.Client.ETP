namespace ShellSquare.Client.ETP
{
    partial class TimeDepthBasedData
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.DoneButton = new System.Windows.Forms.Button();
            this.PanelColumn = new System.Windows.Forms.Panel();
            this.LabelColumn = new System.Windows.Forms.Label();
            this.PanelColumn.SuspendLayout();
            this.SuspendLayout();
            // 
            // DoneButton
            // 
            this.DoneButton.BackColor = System.Drawing.Color.White;
            this.DoneButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.DoneButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DoneButton.Location = new System.Drawing.Point(286, 428);
            this.DoneButton.Name = "DoneButton";
            this.DoneButton.Size = new System.Drawing.Size(85, 28);
            this.DoneButton.TabIndex = 0;
            this.DoneButton.Text = "Done";
            this.DoneButton.UseVisualStyleBackColor = false;
            this.DoneButton.Click += new System.EventHandler(this.DoneButton_Click);
            this.DoneButton.MouseLeave += new System.EventHandler(this.DoneButton_MouseLeave);
            this.DoneButton.MouseHover += new System.EventHandler(this.DoneButton_MouseHover);
            // 
            // PanelColumn
            // 
            this.PanelColumn.Controls.Add(this.LabelColumn);
            this.PanelColumn.Location = new System.Drawing.Point(0, -1);
            this.PanelColumn.Name = "PanelColumn";
            this.PanelColumn.Size = new System.Drawing.Size(395, 33);
            this.PanelColumn.TabIndex = 1;
            // 
            // LabelColumn
            // 
            this.LabelColumn.AutoSize = true;
            this.LabelColumn.Font = new System.Drawing.Font("Times New Roman", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LabelColumn.Location = new System.Drawing.Point(15, 8);
            this.LabelColumn.Name = "LabelColumn";
            this.LabelColumn.Size = new System.Drawing.Size(68, 19);
            this.LabelColumn.TabIndex = 0;
            this.LabelColumn.Text = "Column";
            // 
            // TimeDepthBasedData
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(392, 468);
            this.ControlBox = false;
            this.Controls.Add(this.PanelColumn);
            this.Controls.Add(this.DoneButton);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TimeDepthBasedData";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ETP Client";
            this.Load += new System.EventHandler(this.TimeDepthBasedData_Load);
            this.PanelColumn.ResumeLayout(false);
            this.PanelColumn.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button DoneButton;
        private System.Windows.Forms.Panel PanelColumn;
        private System.Windows.Forms.Label LabelColumn;
    }
}