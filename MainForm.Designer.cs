namespace DMXServer
{
    partial class MainForm
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
                if (receiver != null) receiver.Dispose();
                //if (udpClient != null) udpClient.Close();
            }
            try
            {
                base.Dispose(disposing);
            }
            catch { }
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.cbMidi = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.btnReload = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.cbOSC = new System.Windows.Forms.CheckBox();
            this.lblDebug = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnSetlistExport = new System.Windows.Forms.Button();
            this.lblOscIp = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.btnSetlist = new System.Windows.Forms.Button();
            this.cbOutText = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cbMidiOut = new System.Windows.Forms.ComboBox();
            this.ddlShows = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(351, 65);
            this.btnStart.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(114, 35);
            this.btnStart.TabIndex = 0;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(474, 65);
            this.btnStop.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(112, 35);
            this.btnStop.TabIndex = 1;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // cbMidi
            // 
            this.cbMidi.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbMidi.FormattingEnabled = true;
            this.cbMidi.Location = new System.Drawing.Point(135, 18);
            this.cbMidi.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbMidi.Name = "cbMidi";
            this.cbMidi.Size = new System.Drawing.Size(180, 28);
            this.cbMidi.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(36, 23);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(89, 20);
            this.label1.TabIndex = 3;
            this.label1.Text = "MIDI Input:";
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.HorizontalScrollbar = true;
            this.listBox1.ItemHeight = 20;
            this.listBox1.Location = new System.Drawing.Point(18, 283);
            this.listBox1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(744, 584);
            this.listBox1.TabIndex = 4;
            // 
            // btnReload
            // 
            this.btnReload.Location = new System.Drawing.Point(352, 15);
            this.btnReload.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(112, 35);
            this.btnReload.TabIndex = 16;
            this.btnReload.Text = "Reload";
            this.btnReload.UseVisualStyleBackColor = true;
            this.btnReload.Click += new System.EventHandler(this.btnReload_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(24, 123);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(97, 20);
            this.label6.TabIndex = 24;
            this.label6.Text = "Enable OSC";
            // 
            // cbOSC
            // 
            this.cbOSC.AutoSize = true;
            this.cbOSC.Location = new System.Drawing.Point(130, 123);
            this.cbOSC.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbOSC.Name = "cbOSC";
            this.cbOSC.Size = new System.Drawing.Size(22, 21);
            this.cbOSC.TabIndex = 25;
            this.cbOSC.UseVisualStyleBackColor = true;
            // 
            // lblDebug
            // 
            this.lblDebug.AutoSize = true;
            this.lblDebug.Location = new System.Drawing.Point(388, 123);
            this.lblDebug.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblDebug.Name = "lblDebug";
            this.lblDebug.Size = new System.Drawing.Size(0, 20);
            this.lblDebug.TabIndex = 29;
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.btnSetlistExport);
            this.panel1.Controls.Add(this.lblOscIp);
            this.panel1.Controls.Add(this.label14);
            this.panel1.Controls.Add(this.btnSetlist);
            this.panel1.Controls.Add(this.cbOutText);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.cbMidiOut);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.btnStart);
            this.panel1.Controls.Add(this.btnStop);
            this.panel1.Controls.Add(this.cbMidi);
            this.panel1.Controls.Add(this.lblDebug);
            this.panel1.Controls.Add(this.btnReload);
            this.panel1.Controls.Add(this.cbOSC);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Location = new System.Drawing.Point(16, 80);
            this.panel1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(744, 185);
            this.panel1.TabIndex = 33;
            // 
            // btnSetlistExport
            // 
            this.btnSetlistExport.Location = new System.Drawing.Point(606, 15);
            this.btnSetlistExport.Name = "btnSetlistExport";
            this.btnSetlistExport.Size = new System.Drawing.Size(120, 85);
            this.btnSetlistExport.TabIndex = 57;
            this.btnSetlistExport.Text = "Export Setlist";
            this.btnSetlistExport.UseVisualStyleBackColor = true;
            this.btnSetlistExport.Click += new System.EventHandler(this.btnSetlistExport_Click);
            // 
            // lblOscIp
            // 
            this.lblOscIp.AutoSize = true;
            this.lblOscIp.Location = new System.Drawing.Point(474, 122);
            this.lblOscIp.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblOscIp.Name = "lblOscIp";
            this.lblOscIp.Size = new System.Drawing.Size(75, 20);
            this.lblOscIp.TabIndex = 56;
            this.lblOscIp.Text = "127.0.0.1";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(352, 122);
            this.label14.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(108, 20);
            this.label14.TabIndex = 55;
            this.label14.Text = "OSC Receive:";
            // 
            // btnSetlist
            // 
            this.btnSetlist.Location = new System.Drawing.Point(474, 15);
            this.btnSetlist.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnSetlist.Name = "btnSetlist";
            this.btnSetlist.Size = new System.Drawing.Size(112, 35);
            this.btnSetlist.TabIndex = 46;
            this.btnSetlist.Text = "Setlist";
            this.btnSetlist.UseVisualStyleBackColor = true;
            this.btnSetlist.Click += new System.EventHandler(this.btnSetlist_Click);
            // 
            // cbOutText
            // 
            this.cbOutText.AutoSize = true;
            this.cbOutText.Checked = true;
            this.cbOutText.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbOutText.Location = new System.Drawing.Point(194, 122);
            this.cbOutText.Name = "cbOutText";
            this.cbOutText.Size = new System.Drawing.Size(118, 24);
            this.cbOutText.TabIndex = 45;
            this.cbOutText.Text = "Output Text";
            this.cbOutText.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(24, 72);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(101, 20);
            this.label4.TabIndex = 36;
            this.label4.Text = "MIDI Output:";
            // 
            // cbMidiOut
            // 
            this.cbMidiOut.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbMidiOut.FormattingEnabled = true;
            this.cbMidiOut.Location = new System.Drawing.Point(135, 65);
            this.cbMidiOut.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbMidiOut.Name = "cbMidiOut";
            this.cbMidiOut.Size = new System.Drawing.Size(180, 28);
            this.cbMidiOut.TabIndex = 35;
            // 
            // ddlShows
            // 
            this.ddlShows.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddlShows.FormattingEnabled = true;
            this.ddlShows.Location = new System.Drawing.Point(134, 17);
            this.ddlShows.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ddlShows.Name = "ddlShows";
            this.ddlShows.Size = new System.Drawing.Size(180, 28);
            this.ddlShows.TabIndex = 34;
            this.ddlShows.SelectedIndexChanged += new System.EventHandler(this.ddlShows_SelectedIndexChanged);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(14, 22);
            this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(110, 20);
            this.label9.TabIndex = 43;
            this.label9.Text = "Current Show:";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(783, 886);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.ddlShows);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.listBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "GigControl";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.ComboBox cbMidi;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Button btnReload;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox cbOSC;
        private System.Windows.Forms.Label lblDebug;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cbMidiOut;
        private System.Windows.Forms.ComboBox ddlShows;
        private System.Windows.Forms.Label label9;
        public System.Windows.Forms.CheckBox cbOutText;
        private System.Windows.Forms.Button btnSetlist;
        private System.Windows.Forms.Label lblOscIp;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Button btnSetlistExport;
    }
}

