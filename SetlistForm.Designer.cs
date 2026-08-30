namespace DMXServer
{
    partial class SetlistForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SetlistForm));
            this.lbAvailable = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtNewName = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtNewOSC = new System.Windows.Forms.TextBox();
            this.btnAddNew = new System.Windows.Forms.Button();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.btnUp = new System.Windows.Forms.Button();
            this.btnDown = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.clbAllocated = new System.Windows.Forms.CheckedListBox();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lbAvailable
            // 
            this.lbAvailable.AllowDrop = true;
            this.lbAvailable.FormattingEnabled = true;
            this.lbAvailable.ItemHeight = 20;
            this.lbAvailable.Location = new System.Drawing.Point(18, 163);
            this.lbAvailable.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.lbAvailable.Name = "lbAvailable";
            this.lbAvailable.Size = new System.Drawing.Size(354, 404);
            this.lbAvailable.TabIndex = 0;
            this.lbAvailable.DragDrop += new System.Windows.Forms.DragEventHandler(this.lbAvailable_DragDrop);
            this.lbAvailable.DragEnter += new System.Windows.Forms.DragEventHandler(this.lbAvailable_DragEnter);
            this.lbAvailable.MouseDown += new System.Windows.Forms.MouseEventHandler(this.lbAvailable_MouseDown);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(18, 138);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(122, 20);
            this.label1.TabIndex = 4;
            this.label1.Text = "Available Songs";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(18, 14);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(55, 20);
            this.label3.TabIndex = 10;
            this.label3.Text = "Name:";
            // 
            // txtNewName
            // 
            this.txtNewName.Location = new System.Drawing.Point(18, 38);
            this.txtNewName.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtNewName.Name = "txtNewName";
            this.txtNewName.Size = new System.Drawing.Size(238, 26);
            this.txtNewName.TabIndex = 11;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(18, 74);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(110, 20);
            this.label4.TabIndex = 12;
            this.label4.Text = "OSC Address:";
            // 
            // txtNewOSC
            // 
            this.txtNewOSC.Location = new System.Drawing.Point(18, 98);
            this.txtNewOSC.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtNewOSC.Name = "txtNewOSC";
            this.txtNewOSC.Size = new System.Drawing.Size(238, 26);
            this.txtNewOSC.TabIndex = 13;
            // 
            // btnAddNew
            // 
            this.btnAddNew.Location = new System.Drawing.Point(268, 72);
            this.btnAddNew.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnAddNew.Name = "btnAddNew";
            this.btnAddNew.Size = new System.Drawing.Size(104, 35);
            this.btnAddNew.TabIndex = 14;
            this.btnAddNew.Text = "Add New";
            this.btnAddNew.UseVisualStyleBackColor = true;
            this.btnAddNew.Click += new System.EventHandler(this.btnAddNew_Click);
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(382, 228);
            this.btnAdd.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(38, 35);
            this.btnAdd.TabIndex = 2;
            this.btnAdd.Text = ">";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnRemove
            // 
            this.btnRemove.Location = new System.Drawing.Point(382, 272);
            this.btnRemove.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(38, 35);
            this.btnRemove.TabIndex = 3;
            this.btnRemove.Text = "<";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(424, 32);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(110, 20);
            this.label2.TabIndex = 5;
            this.label2.Text = "Current Setlist";
            // 
            // btnUp
            // 
            this.btnUp.Location = new System.Drawing.Point(429, 580);
            this.btnUp.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnUp.Name = "btnUp";
            this.btnUp.Size = new System.Drawing.Size(104, 35);
            this.btnUp.TabIndex = 6;
            this.btnUp.Text = "Move up";
            this.btnUp.UseVisualStyleBackColor = true;
            this.btnUp.Click += new System.EventHandler(this.btnUp_Click);
            // 
            // btnDown
            // 
            this.btnDown.Location = new System.Drawing.Point(542, 580);
            this.btnDown.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnDown.Name = "btnDown";
            this.btnDown.Size = new System.Drawing.Size(111, 35);
            this.btnDown.TabIndex = 7;
            this.btnDown.Text = "Move down";
            this.btnDown.UseVisualStyleBackColor = true;
            this.btnDown.Click += new System.EventHandler(this.btnDown_Click);
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(662, 580);
            this.btnClear.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(123, 35);
            this.btnClear.TabIndex = 8;
            this.btnClear.Text = "Clear Setlist";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // clbAllocated
            // 
            this.clbAllocated.AllowDrop = true;
            this.clbAllocated.FormattingEnabled = true;
            this.clbAllocated.Location = new System.Drawing.Point(429, 57);
            this.clbAllocated.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.clbAllocated.Name = "clbAllocated";
            this.clbAllocated.Size = new System.Drawing.Size(354, 510);
            this.clbAllocated.TabIndex = 9;
            this.clbAllocated.DragDrop += new System.Windows.Forms.DragEventHandler(this.clbAllocated_DragDrop);
            this.clbAllocated.DragEnter += new System.Windows.Forms.DragEventHandler(this.clbAllocated_DragEnter);
            this.clbAllocated.DragOver += new System.Windows.Forms.DragEventHandler(this.clbAllocated_DragOver);
            this.clbAllocated.MouseDown += new System.Windows.Forms.MouseEventHandler(this.clbAllocated_MouseDown);
            // 
            // btnUpdate
            // 
            this.btnUpdate.Location = new System.Drawing.Point(268, 29);
            this.btnUpdate.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(104, 35);
            this.btnUpdate.TabIndex = 15;
            this.btnUpdate.Text = "Update";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(268, 117);
            this.btnDelete.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(104, 35);
            this.btnDelete.TabIndex = 16;
            this.btnDelete.Text = "Delete";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // SetlistForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(810, 623);
            this.Controls.Add(this.clbAllocated);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.btnDown);
            this.Controls.Add(this.btnUp);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.lbAvailable);
            this.Controls.Add(this.btnAddNew);
            this.Controls.Add(this.btnUpdate);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.txtNewOSC);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtNewName);
            this.Controls.Add(this.label3);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "SetlistForm";
            this.Text = "Setlist";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SetlistForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox lbAvailable;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtNewName;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtNewOSC;
        private System.Windows.Forms.Button btnAddNew;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnUp;
        private System.Windows.Forms.Button btnDown;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.CheckedListBox clbAllocated;
    }
}
