/*
 * Copyright (c) 2006, Second Life Reverse Engineering Team
 * All rights reserved.
 *
 * - Redistribution and use in source and binary forms, with or without 
 *   modification, are permitted provided that the following conditions are met:
 *
 * - Redistributions of source code must retain the above copyright notice, this
 *   list of conditions and the following disclaimer.
 * - Neither the name of the Second Life Reverse Engineering Team nor the names 
 *   of its contributors may be used to endorse or promote products derived from
 *   this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using libsecondlife;
using libsecondlife.Packets;

namespace SLAccountant
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class frmSLAccountant : System.Windows.Forms.Form
	{
		private System.Windows.Forms.GroupBox grpLogin;
		private System.Windows.Forms.TextBox txtPassword;
		private System.Windows.Forms.TextBox txtLastName;
		private System.Windows.Forms.Button cmdConnect;
		private System.Windows.Forms.TextBox txtFirstName;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label lblName;
		private System.Windows.Forms.Label lblBalance;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox txtFind;
		private System.Windows.Forms.Button cmdFind;
		private System.Windows.Forms.TextBox txtTransfer;
		private System.Windows.Forms.Button cmdTransfer;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.ListView lstFind;
		private System.Windows.Forms.ColumnHeader colName;
		private System.Windows.Forms.ColumnHeader colOnline;
		private System.Windows.Forms.ColumnHeader colUuid;

		// libsecondlife instance
		private SecondLife client;

		public frmSLAccountant()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			client.Network.Logout();
			
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.grpLogin = new System.Windows.Forms.GroupBox();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.txtPassword = new System.Windows.Forms.TextBox();
			this.txtLastName = new System.Windows.Forms.TextBox();
			this.cmdConnect = new System.Windows.Forms.Button();
			this.txtFirstName = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.lblName = new System.Windows.Forms.Label();
			this.lblBalance = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.txtFind = new System.Windows.Forms.TextBox();
			this.cmdFind = new System.Windows.Forms.Button();
			this.txtTransfer = new System.Windows.Forms.TextBox();
			this.cmdTransfer = new System.Windows.Forms.Button();
			this.label7 = new System.Windows.Forms.Label();
			this.lstFind = new System.Windows.Forms.ListView();
			this.colName = new System.Windows.Forms.ColumnHeader();
			this.colOnline = new System.Windows.Forms.ColumnHeader();
			this.colUuid = new System.Windows.Forms.ColumnHeader();
			this.grpLogin.SuspendLayout();
			this.SuspendLayout();
			// 
			// grpLogin
			// 
			this.grpLogin.Controls.Add(this.label3);
			this.grpLogin.Controls.Add(this.label2);
			this.grpLogin.Controls.Add(this.label1);
			this.grpLogin.Controls.Add(this.txtPassword);
			this.grpLogin.Controls.Add(this.txtLastName);
			this.grpLogin.Controls.Add(this.cmdConnect);
			this.grpLogin.Controls.Add(this.txtFirstName);
			this.grpLogin.Enabled = false;
			this.grpLogin.Location = new System.Drawing.Point(16, 344);
			this.grpLogin.Name = "grpLogin";
			this.grpLogin.Size = new System.Drawing.Size(560, 80);
			this.grpLogin.TabIndex = 50;
			this.grpLogin.TabStop = false;
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(280, 24);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(120, 16);
			this.label3.TabIndex = 50;
			this.label3.Text = "Password";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(152, 24);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(120, 16);
			this.label2.TabIndex = 50;
			this.label2.Text = "Last Name";
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(16, 24);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(120, 16);
			this.label1.TabIndex = 50;
			this.label1.Text = "First Name";
			// 
			// txtPassword
			// 
			this.txtPassword.Location = new System.Drawing.Point(280, 40);
			this.txtPassword.Name = "txtPassword";
			this.txtPassword.PasswordChar = '*';
			this.txtPassword.Size = new System.Drawing.Size(120, 20);
			this.txtPassword.TabIndex = 2;
			this.txtPassword.Text = "";
			// 
			// txtLastName
			// 
			this.txtLastName.Location = new System.Drawing.Point(152, 40);
			this.txtLastName.Name = "txtLastName";
			this.txtLastName.Size = new System.Drawing.Size(112, 20);
			this.txtLastName.TabIndex = 1;
			this.txtLastName.Text = "";
			// 
			// cmdConnect
			// 
			this.cmdConnect.Location = new System.Drawing.Point(424, 40);
			this.cmdConnect.Name = "cmdConnect";
			this.cmdConnect.Size = new System.Drawing.Size(120, 24);
			this.cmdConnect.TabIndex = 3;
			this.cmdConnect.Text = "Connect";
			this.cmdConnect.Click += new System.EventHandler(this.cmdConnect_Click);
			// 
			// txtFirstName
			// 
			this.txtFirstName.Location = new System.Drawing.Point(16, 40);
			this.txtFirstName.Name = "txtFirstName";
			this.txtFirstName.Size = new System.Drawing.Size(120, 20);
			this.txtFirstName.TabIndex = 0;
			this.txtFirstName.Text = "";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(16, 8);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(48, 16);
			this.label4.TabIndex = 50;
			this.label4.Text = "Name:";
			// 
			// lblName
			// 
			this.lblName.Location = new System.Drawing.Point(64, 8);
			this.lblName.Name = "lblName";
			this.lblName.Size = new System.Drawing.Size(184, 16);
			this.lblName.TabIndex = 50;
			// 
			// lblBalance
			// 
			this.lblBalance.Location = new System.Drawing.Point(512, 8);
			this.lblBalance.Name = "lblBalance";
			this.lblBalance.Size = new System.Drawing.Size(64, 16);
			this.lblBalance.TabIndex = 50;
			this.lblBalance.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// label6
			// 
			this.label6.Location = new System.Drawing.Point(456, 8);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(56, 16);
			this.label6.TabIndex = 50;
			this.label6.Text = "Balance:";
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(16, 40);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(88, 16);
			this.label5.TabIndex = 50;
			this.label5.Text = "People Search";
			// 
			// txtFind
			// 
			this.txtFind.Enabled = false;
			this.txtFind.Location = new System.Drawing.Point(16, 56);
			this.txtFind.Name = "txtFind";
			this.txtFind.Size = new System.Drawing.Size(184, 20);
			this.txtFind.TabIndex = 4;
			this.txtFind.Text = "";
			// 
			// cmdFind
			// 
			this.cmdFind.Enabled = false;
			this.cmdFind.Location = new System.Drawing.Point(208, 56);
			this.cmdFind.Name = "cmdFind";
			this.cmdFind.Size = new System.Drawing.Size(48, 24);
			this.cmdFind.TabIndex = 5;
			this.cmdFind.Text = "Find";
			this.cmdFind.Click += new System.EventHandler(this.cmdFind_Click);
			// 
			// txtTransfer
			// 
			this.txtTransfer.Enabled = false;
			this.txtTransfer.Location = new System.Drawing.Point(360, 192);
			this.txtTransfer.MaxLength = 7;
			this.txtTransfer.Name = "txtTransfer";
			this.txtTransfer.Size = new System.Drawing.Size(104, 20);
			this.txtTransfer.TabIndex = 7;
			this.txtTransfer.Text = "";
			// 
			// cmdTransfer
			// 
			this.cmdTransfer.Enabled = false;
			this.cmdTransfer.Location = new System.Drawing.Point(472, 192);
			this.cmdTransfer.Name = "cmdTransfer";
			this.cmdTransfer.Size = new System.Drawing.Size(104, 24);
			this.cmdTransfer.TabIndex = 8;
			this.cmdTransfer.Text = "Transfer Lindens";
			this.cmdTransfer.Click += new System.EventHandler(this.cmdTransfer_Click);
			// 
			// label7
			// 
			this.label7.Location = new System.Drawing.Point(360, 176);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(88, 16);
			this.label7.TabIndex = 17;
			this.label7.Text = "Amount:";
			// 
			// lstFind
			// 
			this.lstFind.Activation = System.Windows.Forms.ItemActivation.OneClick;
			this.lstFind.AllowColumnReorder = true;
			this.lstFind.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
																					  this.colName,
																					  this.colOnline,
																					  this.colUuid});
			this.lstFind.FullRowSelect = true;
			this.lstFind.HideSelection = false;
			this.lstFind.Location = new System.Drawing.Point(16, 88);
			this.lstFind.Name = "lstFind";
			this.lstFind.Size = new System.Drawing.Size(336, 248);
			this.lstFind.Sorting = System.Windows.Forms.SortOrder.Ascending;
			this.lstFind.TabIndex = 6;
			this.lstFind.View = System.Windows.Forms.View.Details;
			// 
			// colName
			// 
			this.colName.Text = "Name";
			this.colName.Width = 120;
			// 
			// colOnline
			// 
			this.colOnline.Text = "Online";
			this.colOnline.Width = 50;
			// 
			// colUuid
			// 
			this.colUuid.Text = "UUID";
			this.colUuid.Width = 150;
			// 
			// frmSLAccountant
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(592, 437);
			this.Controls.Add(this.lstFind);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.cmdTransfer);
			this.Controls.Add(this.txtTransfer);
			this.Controls.Add(this.txtFind);
			this.Controls.Add(this.cmdFind);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.lblBalance);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.lblName);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.grpLogin);
			this.Name = "frmSLAccountant";
			this.Text = "SL Accountant";
			this.Load += new System.EventHandler(this.frmSLAccountant_Load);
			this.grpLogin.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
            frmSLAccountant frm = new frmSLAccountant();
            frm.ShowDialog();
		}

        private delegate void StringParamInvoker(string value);
        private delegate void ListViewItemParamInvoker(ListViewItem item);

        private void UpdateBalance(string value)
        {
            lblBalance.Text = value;
        }

        private void AddFindItem(ListViewItem item)
        {
            lock (lstFind)
            {
                lstFind.Items.Add(item);
            }
        }

		private void BalanceHandler(Packet packet, Simulator simulator)
		{
            string value = ((MoneyBalanceReplyPacket)packet).MoneyData.MoneyBalance.ToString();
            this.BeginInvoke(new StringParamInvoker(UpdateBalance), new object[] { value });
		}

		private void DirPeopleHandler(Packet packet, Simulator simulator)
		{
            DirPeopleReplyPacket reply = (DirPeopleReplyPacket)packet;

            foreach (DirPeopleReplyPacket.QueryRepliesBlock block in reply.QueryReplies)
            {
                ListViewItem listItem = new ListViewItem(new string[] { 
                Helpers.FieldToUTF8String(block.FirstName) + " " + Helpers.FieldToUTF8String(block.LastName), 
                (block.Online ? "Yes" : "No"), block.AgentID.ToString() });

                this.BeginInvoke(new ListViewItemParamInvoker(AddFindItem), new object[] { listItem });
            }
		}

		private void frmSLAccountant_Load(object sender, System.EventArgs e)
		{
			client = new SecondLife();

			// Install our packet handlers
            client.Network.RegisterCallback(PacketType.MoneyBalanceReply, new NetworkManager.PacketCallback(BalanceHandler));
            client.Network.RegisterCallback(PacketType.DirPeopleReply, new NetworkManager.PacketCallback(DirPeopleHandler));

			grpLogin.Enabled = true;
		}

		private void cmdConnect_Click(object sender, System.EventArgs e)
		{
			if (cmdConnect.Text == "Connect")
			{
				cmdConnect.Text = "Disconnect";
				txtFirstName.Enabled = txtLastName.Enabled = txtPassword.Enabled = false;

				if (client.Network.Login(txtFirstName.Text, txtLastName.Text, txtPassword.Text, 
                    "accountant", "jhurliman@metaverseindustries.com"))
				{
					Random rand = new Random();
					
					lblName.Text = client.ToString();

                    // AgentSetAppearance
                    AgentSetAppearancePacket appearance = new AgentSetAppearancePacket();
                    appearance.VisualParam = new AgentSetAppearancePacket.VisualParamBlock[218];
                    // Setup some random appearance values
                    for (int i = 0; i < 218; i++)
                    {
                        appearance.VisualParam[i] = new AgentSetAppearancePacket.VisualParamBlock();
                        appearance.VisualParam[i].ParamValue = (byte)rand.Next(255);
                    }
                    appearance.AgentData.AgentID = client.Network.AgentID;
                    appearance.AgentData.SessionID = client.Network.SessionID;
                    appearance.AgentData.SerialNum = 1;
                    appearance.AgentData.Size = new LLVector3(0.45F, 0.6F, 1.831094F);
                    appearance.ObjectData.TextureEntry = new byte[0];

                    client.Network.SendPacket(appearance);

                    // Request our balance
                    client.Self.RequestBalance();

					txtFind.Enabled = cmdFind.Enabled = true;
					txtTransfer.Enabled = cmdTransfer.Enabled = true;
				}
				else
				{
					MessageBox.Show(this, "Error logging in: " + client.Network.LoginMessage);
					cmdConnect.Text = "Connect";
					txtFirstName.Enabled = txtLastName.Enabled = txtPassword.Enabled = true;
					txtFind.Enabled = cmdFind.Enabled = false;
					lblName.Text = lblBalance.Text = "";
					txtTransfer.Enabled = cmdTransfer.Enabled = false;
				}
			}
			else
			{
				client.Network.Logout();
				cmdConnect.Text = "Connect";
				txtFirstName.Enabled = txtLastName.Enabled = txtPassword.Enabled = true;
				txtFind.Enabled = cmdFind.Enabled = false;
				lblName.Text = lblBalance.Text = "";
				txtTransfer.Enabled = cmdTransfer.Enabled = false;
			}
		}

		private void cmdFind_Click(object sender, System.EventArgs e)
		{
			lstFind.Items.Clear();

            DirFindQueryPacket query = new DirFindQueryPacket();
            query.AgentData.AgentID = client.Network.AgentID;
            query.AgentData.SessionID = client.Network.SessionID;
            query.QueryData.QueryFlags = 1;
            query.QueryData.QueryID = LLUUID.Random();
            query.QueryData.QueryStart = 0;
            query.QueryData.QueryText = Helpers.StringToField(txtFind.Text);
            query.Header.Reliable = true;

            client.Network.SendPacket(query);
		}

		private void cmdTransfer_Click(object sender, System.EventArgs e)
		{
			int amount = 0;

			try
			{
				amount = System.Convert.ToInt32(txtTransfer.Text);
			}
			catch (Exception)
			{
				MessageBox.Show(txtTransfer.Text + " is not a valid amount");
				return;
			}

			if (lstFind.SelectedItems.Count != 1)
			{
				MessageBox.Show("Find an avatar using the directory search and select " + 
					"their name to transfer money");
				return;
			}
			
			client.Self.GiveMoney(new LLUUID(lstFind.SelectedItems[0].SubItems[2].Text),
			    amount, "SLAccountant payment");
		}
	}
}
