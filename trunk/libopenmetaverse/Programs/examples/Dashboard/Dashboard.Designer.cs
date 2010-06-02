namespace Dashboard
{
    partial class Dashboard
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.localChat1 = new OpenMetaverse.GUI.LocalChat();
            this.statusOutput1 = new OpenMetaverse.GUI.StatusOutput();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.avatarList1 = new OpenMetaverse.GUI.AvatarList();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.friendsList1 = new OpenMetaverse.GUI.FriendList();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.groupList1 = new OpenMetaverse.GUI.GroupList();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.inventoryTree1 = new OpenMetaverse.GUI.InventoryTree();
            this.miniMap1 = new OpenMetaverse.GUI.MiniMap();
            this.messageBar1 = new OpenMetaverse.GUI.MessageBar();
            this.loginPanel1 = new OpenMetaverse.GUI.LoginPanel();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.tabPage4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.miniMap1)).BeginInit();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(0, 40);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer3);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(792, 500);
            this.splitContainer1.SplitterDistance = 553;
            this.splitContainer1.TabIndex = 4;
            // 
            // splitContainer3
            // 
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.Location = new System.Drawing.Point(0, 0);
            this.splitContainer3.Name = "splitContainer3";
            this.splitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.localChat1);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.statusOutput1);
            this.splitContainer3.Size = new System.Drawing.Size(553, 500);
            this.splitContainer3.SplitterDistance = 377;
            this.splitContainer3.TabIndex = 0;
            // 
            // localChat1
            // 
            this.localChat1.Client = null;
            this.localChat1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.localChat1.Location = new System.Drawing.Point(0, 0);
            this.localChat1.Name = "localChat1";
            this.localChat1.Size = new System.Drawing.Size(553, 377);
            this.localChat1.TabIndex = 4;
            // 
            // statusOutput1
            // 
            this.statusOutput1.BackColor = System.Drawing.Color.White;
            this.statusOutput1.Client = null;
            this.statusOutput1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusOutput1.Location = new System.Drawing.Point(0, 0);
            this.statusOutput1.Name = "statusOutput1";
            this.statusOutput1.ReadOnly = true;
            this.statusOutput1.Size = new System.Drawing.Size(553, 119);
            this.statusOutput1.TabIndex = 0;
            this.statusOutput1.Text = "";
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.tabControl1);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.miniMap1);
            this.splitContainer2.Size = new System.Drawing.Size(235, 500);
            this.splitContainer2.SplitterDistance = 263;
            this.splitContainer2.TabIndex = 9;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Controls.Add(this.tabPage4);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(235, 263);
            this.tabControl1.TabIndex = 1;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.avatarList1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(227, 237);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Nearby";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // avatarList1
            // 
            this.avatarList1.Client = null;
            this.avatarList1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.avatarList1.Location = new System.Drawing.Point(3, 3);
            this.avatarList1.MultiSelect = false;
            this.avatarList1.Name = "avatarList1";
            this.avatarList1.Size = new System.Drawing.Size(221, 231);
            this.avatarList1.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.avatarList1.TabIndex = 2;
            this.avatarList1.UseCompatibleStateImageBehavior = false;
            this.avatarList1.View = System.Windows.Forms.View.Details;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.friendsList1);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(227, 237);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Friends";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // friendsList1
            // 
            this.friendsList1.Client = null;
            this.friendsList1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.friendsList1.Location = new System.Drawing.Point(3, 3);
            this.friendsList1.Name = "friendsList1";
            this.friendsList1.Size = new System.Drawing.Size(221, 231);
            this.friendsList1.TabIndex = 5;
            this.friendsList1.UseCompatibleStateImageBehavior = false;
            this.friendsList1.View = System.Windows.Forms.View.Details;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.groupList1);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(227, 237);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Groups";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // groupList1
            // 
            this.groupList1.Client = null;
            this.groupList1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupList1.Location = new System.Drawing.Point(0, 0);
            this.groupList1.Name = "groupList1";
            this.groupList1.Size = new System.Drawing.Size(227, 237);
            this.groupList1.TabIndex = 7;
            this.groupList1.UseCompatibleStateImageBehavior = false;
            this.groupList1.View = System.Windows.Forms.View.Details;
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.inventoryTree1);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Size = new System.Drawing.Size(227, 237);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Inventory";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // inventoryTree1
            // 
            this.inventoryTree1.Client = null;
            this.inventoryTree1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.inventoryTree1.Location = new System.Drawing.Point(0, 0);
            this.inventoryTree1.Name = "inventoryTree1";
            this.inventoryTree1.Size = new System.Drawing.Size(227, 237);
            this.inventoryTree1.TabIndex = 1;
            // 
            // miniMap1
            // 
            this.miniMap1.BackColor = System.Drawing.SystemColors.Control;
            this.miniMap1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.miniMap1.Client = null;
            this.miniMap1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.miniMap1.Location = new System.Drawing.Point(0, 0);
            this.miniMap1.Name = "miniMap1";
            this.miniMap1.Size = new System.Drawing.Size(235, 233);
            this.miniMap1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.miniMap1.TabIndex = 11;
            this.miniMap1.TabStop = false;
            // 
            // messageBar1
            // 
            this.messageBar1.Client = null;
            this.messageBar1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.messageBar1.Location = new System.Drawing.Point(0, 543);
            this.messageBar1.Name = "messageBar1";
            this.messageBar1.Size = new System.Drawing.Size(792, 25);
            this.messageBar1.TabIndex = 6;
            // 
            // loginPanel1
            // 
            this.loginPanel1.Client = null;
            this.loginPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.loginPanel1.Location = new System.Drawing.Point(0, 0);
            this.loginPanel1.Name = "loginPanel1";
            this.loginPanel1.Size = new System.Drawing.Size(792, 37);
            this.loginPanel1.TabIndex = 5;
            // 
            // Dashboard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(792, 568);
            this.Controls.Add(this.messageBar1);
            this.Controls.Add(this.loginPanel1);
            this.Controls.Add(this.splitContainer1);
            this.Name = "Dashboard";
            this.Text = "Dashboard";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel2.ResumeLayout(false);
            this.splitContainer3.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.tabPage4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.miniMap1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private OpenMetaverse.GUI.MiniMap miniMap1;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private OpenMetaverse.GUI.LocalChat localChat1;
        private OpenMetaverse.GUI.StatusOutput statusOutput1;
        private OpenMetaverse.GUI.LoginPanel loginPanel1;
        private OpenMetaverse.GUI.MessageBar messageBar1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private OpenMetaverse.GUI.AvatarList avatarList1;
        private System.Windows.Forms.TabPage tabPage2;
        private OpenMetaverse.GUI.FriendList friendsList1;
        private System.Windows.Forms.TabPage tabPage3;
        private OpenMetaverse.GUI.GroupList groupList1;
        private System.Windows.Forms.TabPage tabPage4;
        private OpenMetaverse.GUI.InventoryTree inventoryTree1;

    }
}

