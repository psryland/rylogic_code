using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.attrib;
using pr.common;
using pr.extn;
using pr.gfx;
using pr.maths;
using pr.util;

namespace pr.gui
{
	public class BluetoothUI :Form
	{
		#region UI Elements
		private SwitchCheckBox m_chk_enable_bt;
		private Label m_lbl_enable_bt;
		private Button m_btn_cancel;
		private Button m_btn_ok;
		private ToolTip m_tt;
		private CheckBox m_chk_show_unknown;
		private CheckBox m_chk_show_remembered;
		private CheckBox m_chk_show_connected;
		private CheckBox m_chk_show_authenticated;
		private CheckBox m_chk_discoverable;
		private ComboBox m_cb_radio;
		private ImageList m_il_bt_device_types;
		private System.Windows.Forms.ListBox m_lb_devices;
		private Button button1;
		#endregion

		public BluetoothUI()
		{
			InitializeComponent();

			Radio = new Bluetooth.Radio();
			Device = null;

			ShowDevices = Bluetooth.EOptions.ReturnAll;

			SetupUI();
			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>Flags for devices to display</summary>
		public Bluetooth.EOptions ShowDevices
		{
			get { return m_show_devices; }
			set
			{
				if (m_show_devices == value) return;
				m_show_devices = value;
				m_chk_show_connected    .Checked = m_show_devices.HasFlag(Bluetooth.EOptions.ReturnConnected);
				m_chk_show_authenticated.Checked = m_show_devices.HasFlag(Bluetooth.EOptions.ReturnAuthenticated);
				m_chk_show_remembered   .Checked = m_show_devices.HasFlag(Bluetooth.EOptions.ReturnRemembered);
				m_chk_show_unknown      .Checked = m_show_devices.HasFlag(Bluetooth.EOptions.ReturnUnknown);
				PopulateDevices();
			}
		}
		private Bluetooth.EOptions m_show_devices;

		/// <summary>Enable/Disable bluetooth</summary>
		public bool BluetoothEnabled
		{
			get { return Radio.Connectible; }
			set
			{
				Radio.Connectible = value;
				m_chk_enable_bt.Checked = value;
				this.BeginInvoke(() => UpdateUI());
			}
		}

		/// <summary>Make the bluetooth radio discoverable</summary>
		public bool Discoverable
		{
			get { return Radio.Discoverable; }
			set
			{
				Radio.Discoverable = value;
				m_chk_discoverable.Checked = value;
				this.BeginInvoke(() => UpdateUI());
			}
		}

		/// <summary>The bluetooth radio to use.</summary>
		public Bluetooth.Radio Radio
		{
			get;
			private set;
		}

		/// <summary>The selected device</summary>
		public Bluetooth.Device Device
		{
			get;
			private set;
		}

		/// <summary>The image list for devices (too allow users at add custom ones)</summary>
		public ImageList DeviceImageList
		{
			get { return m_il_bt_device_types; }
		}

		/// <summary>User provided function for returning the image index for the given device</summary>
		public Func<Bluetooth.Device, int> GetDeviceImageIndex { get; set; }

		/// <summary>Set up the UI</summary>
		private void SetupUI()
		{
			// Radio
			m_cb_radio.ToolTip(m_tt, "Select a specific bluetooth radio");
			m_cb_radio.DataSource = new[]{ new Bluetooth.Radio() }.Concat(Bluetooth.Radios()).ToList();
			m_cb_radio.Format += (s,a) =>
			{
				a.Value = ((Bluetooth.Radio)a.ListItem).Name;
			};
			m_cb_radio.SelectedIndexChanged += (s,a) =>
			{
				Radio = (Bluetooth.Radio)m_cb_radio.SelectedItem;
				UpdateUI();
			};

			// Enable/Disable bluetooth
			m_chk_enable_bt.ToolTip(m_tt, "Enable or Disable the Bluetooth radios on this system");
			m_chk_enable_bt.Checked = BluetoothEnabled;
			m_chk_enable_bt.CheckedChanged += (s,a) =>
			{
				BluetoothEnabled = m_chk_enable_bt.Checked;
			};

			// Discoverable
			m_chk_discoverable.ToolTip(m_tt, "Check to allow other devices to discovery this PC");
			m_chk_discoverable.Checked = Discoverable;
			m_chk_discoverable.CheckedChanged += (s,a) =>
			{
				Discoverable = m_chk_discoverable.Checked;
			};

			// Show connected devices
			m_chk_show_connected.ToolTip(m_tt, "Show devices that have connected with this system");
			m_chk_show_connected.Checked = ShowDevices.HasFlag(Bluetooth.EOptions.ReturnConnected);
			m_chk_show_connected.CheckedChanged += (s,a) =>
			{
				ShowDevices = Bit.SetBits(ShowDevices, Bluetooth.EOptions.ReturnConnected, m_chk_show_connected.Checked);
			};

			// Show authenticated devices
			m_chk_show_authenticated.ToolTip(m_tt, "Show devices that have paired with this system");
			m_chk_show_authenticated.Checked = ShowDevices.HasFlag(Bluetooth.EOptions.ReturnAuthenticated);
			m_chk_show_authenticated.CheckedChanged += (s,a) =>
			{
				ShowDevices = Bit.SetBits(ShowDevices, Bluetooth.EOptions.ReturnAuthenticated, m_chk_show_authenticated.Checked);
			};

			// Show remembered devices
			m_chk_show_remembered.ToolTip(m_tt, "Show devices that have connected with this system in the past");
			m_chk_show_remembered.Checked = ShowDevices.HasFlag(Bluetooth.EOptions.ReturnRemembered);
			m_chk_show_remembered.CheckedChanged += (s,a) =>
			{
				ShowDevices = Bit.SetBits(ShowDevices, Bluetooth.EOptions.ReturnRemembered, m_chk_show_remembered.Checked);
			};

			// Show unknown devices
			m_chk_show_unknown.ToolTip(m_tt, "Show discovered devices that have not yet connected or paired with this system");
			m_chk_show_unknown.Checked = ShowDevices.HasFlag(Bluetooth.EOptions.ReturnUnknown);
			m_chk_show_unknown.CheckedChanged += (s,a) =>
			{
				ShowDevices = Bit.SetBits(ShowDevices, Bluetooth.EOptions.ReturnUnknown, m_chk_show_unknown.Checked);
			};

			// Found devices
			m_lb_devices.SelectedIndexChanged += (s,a) =>
			{
				Device = m_lb_devices.SelectedItem.As<Bluetooth.Device>();
				UpdateUI();
			};
			m_lb_devices.DrawItem += (s,a) =>
			{
				DrawBtDevice(a);
			};
			m_lb_devices.MouseClick += (s,a) =>
			{
				if (a.Button == MouseButtons.Right)
					ShowCMenu(a);
			};
		}

		/// <summary>Update the state of UI elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			m_chk_enable_bt.Checked = BluetoothEnabled;
			m_chk_discoverable.Checked = Discoverable;
		}

		/// <summary>Populate the list view of bluetooth devices</summary>
		private void PopulateDevices()
		{
			m_lb_devices.Items.Clear();
			foreach (var device in Bluetooth.Devices(ShowDevices))
				m_lb_devices.Items.Add(device);
		}

		/// <summary>Draw the bluetooth device item in the list view</summary>
		private void DrawBtDevice(DrawItemEventArgs a)
		{
			var device = m_lb_devices.Items[a.Index].As<Bluetooth.Device>();
			var x = a.Bounds.Left + 1;
			var y = a.Bounds.Top + 1;

			// Get the image index for the device type
			var img_index = Maths.Clamp(GetDeviceImageIndex != null
				? GetDeviceImageIndex(device)
				: device.ClassOfDeviceMajor.Assoc<int>("img"),
				0, DeviceImageList.Images.Count);
			var img = DeviceImageList.Images[img_index];

			a.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

			// Cell background
			a.DrawBackground();

			// Device image
			a.Graphics.DrawImage(img, new Point(x,y));
			x += img.Width + 4;

			// Device name
			using (var font = m_lb_devices.Font.Dup(FontStyle.Bold))
			{
				a.Graphics.DrawString(device.Name, font, Brushes.Black, new Point(x, y));
				y += font.Height;
			}

			// Device state
			using (var font = m_lb_devices.Font.Dup(em_size: 8f))
			{
				a.Graphics.DrawString(device.StatusString, font, Brushes.Black, new Point(x, y));
				y += font.Height;
			}

			// Last used/seen
			using (var font = m_lb_devices.Font.Dup(em_size: 6f))
			{
				var s = "Last Used: {0}".Fmt(device.LastUsed.ToString("G", CultureInfo.CurrentCulture));
				a.Graphics.DrawString(s, font, Brushes.Black, new Point(x, y));
				y += font.Height;

				s = "Last Seen: {0}".Fmt(device.LastSeen.ToString("G", CultureInfo.CurrentCulture));
				a.Graphics.DrawString(s, font, Brushes.Black, new Point(x, y));
				y += font.Height;
			}

			// Focus rect
			a.DrawFocusRectangle();
		}

		/// <summary>Create a context menu for the device list</summary>
		private void ShowCMenu(MouseEventArgs args)
		{
			var device = m_lb_devices.SelectedItem.As<Bluetooth.Device>();

			var cmenu = new ContextMenuStrip();

			if (!device.IsPaired)
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Pair"));
				opt.Click += (s,a) =>
				{
					device.Pair();
				};
			}

			cmenu.Show(m_lb_devices, args.Location);
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BluetoothUI));
			this.m_lbl_enable_bt = new System.Windows.Forms.Label();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.button1 = new System.Windows.Forms.Button();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_chk_show_unknown = new System.Windows.Forms.CheckBox();
			this.m_chk_show_remembered = new System.Windows.Forms.CheckBox();
			this.m_chk_show_connected = new System.Windows.Forms.CheckBox();
			this.m_chk_show_authenticated = new System.Windows.Forms.CheckBox();
			this.m_chk_discoverable = new System.Windows.Forms.CheckBox();
			this.m_il_bt_device_types = new System.Windows.Forms.ImageList(this.components);
			this.m_lb_devices = new System.Windows.Forms.ListBox();
			this.m_cb_radio = new pr.gui.ComboBox();
			this.m_chk_enable_bt = new pr.gui.SwitchCheckBox();
			this.SuspendLayout();
			// 
			// m_lbl_enable_bt
			// 
			this.m_lbl_enable_bt.AutoSize = true;
			this.m_lbl_enable_bt.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
			this.m_lbl_enable_bt.Location = new System.Drawing.Point(12, 15);
			this.m_lbl_enable_bt.Name = "m_lbl_enable_bt";
			this.m_lbl_enable_bt.Size = new System.Drawing.Size(78, 20);
			this.m_lbl_enable_bt.TabIndex = 1;
			this.m_lbl_enable_bt.Text = "Bluetooth";
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(271, 273);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 3;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(190, 273);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 4;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// button1
			// 
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.button1.Location = new System.Drawing.Point(12, 273);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 5;
			this.button1.Text = "button1";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Visible = false;
			// 
			// m_chk_show_unknown
			// 
			this.m_chk_show_unknown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_chk_show_unknown.AutoSize = true;
			this.m_chk_show_unknown.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.m_chk_show_unknown.Location = new System.Drawing.Point(202, 250);
			this.m_chk_show_unknown.Name = "m_chk_show_unknown";
			this.m_chk_show_unknown.Size = new System.Drawing.Size(144, 17);
			this.m_chk_show_unknown.TabIndex = 6;
			this.m_chk_show_unknown.Text = "Show Unknown Devices";
			this.m_chk_show_unknown.UseVisualStyleBackColor = true;
			// 
			// m_chk_show_remembered
			// 
			this.m_chk_show_remembered.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_chk_show_remembered.AutoSize = true;
			this.m_chk_show_remembered.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.m_chk_show_remembered.Location = new System.Drawing.Point(185, 232);
			this.m_chk_show_remembered.Name = "m_chk_show_remembered";
			this.m_chk_show_remembered.Size = new System.Drawing.Size(161, 17);
			this.m_chk_show_remembered.TabIndex = 7;
			this.m_chk_show_remembered.Text = "Show Remembered Devices";
			this.m_chk_show_remembered.UseVisualStyleBackColor = true;
			// 
			// m_chk_show_connected
			// 
			this.m_chk_show_connected.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_chk_show_connected.AutoSize = true;
			this.m_chk_show_connected.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.m_chk_show_connected.Location = new System.Drawing.Point(29, 232);
			this.m_chk_show_connected.Name = "m_chk_show_connected";
			this.m_chk_show_connected.Size = new System.Drawing.Size(150, 17);
			this.m_chk_show_connected.TabIndex = 8;
			this.m_chk_show_connected.Text = "Show Connected Devices";
			this.m_chk_show_connected.UseVisualStyleBackColor = true;
			// 
			// m_chk_show_authenticated
			// 
			this.m_chk_show_authenticated.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_chk_show_authenticated.AutoSize = true;
			this.m_chk_show_authenticated.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.m_chk_show_authenticated.Location = new System.Drawing.Point(51, 250);
			this.m_chk_show_authenticated.Name = "m_chk_show_authenticated";
			this.m_chk_show_authenticated.Size = new System.Drawing.Size(128, 17);
			this.m_chk_show_authenticated.TabIndex = 9;
			this.m_chk_show_authenticated.Text = "Show Paired Devices";
			this.m_chk_show_authenticated.UseVisualStyleBackColor = true;
			// 
			// m_chk_discoverable
			// 
			this.m_chk_discoverable.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_chk_discoverable.AutoSize = true;
			this.m_chk_discoverable.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.m_chk_discoverable.Location = new System.Drawing.Point(258, 44);
			this.m_chk_discoverable.Name = "m_chk_discoverable";
			this.m_chk_discoverable.Size = new System.Drawing.Size(88, 17);
			this.m_chk_discoverable.TabIndex = 10;
			this.m_chk_discoverable.Text = "Discoverable";
			this.m_chk_discoverable.UseVisualStyleBackColor = true;
			// 
			// m_il_bt_device_types
			// 
			this.m_il_bt_device_types.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_il_bt_device_types.ImageStream")));
			this.m_il_bt_device_types.TransparentColor = System.Drawing.Color.Transparent;
			this.m_il_bt_device_types.Images.SetKeyName(0, "bt_misc.png");
			this.m_il_bt_device_types.Images.SetKeyName(1, "bt_computer.png");
			this.m_il_bt_device_types.Images.SetKeyName(2, "bt_phone.png");
			this.m_il_bt_device_types.Images.SetKeyName(3, "bt_lan_access.png");
			this.m_il_bt_device_types.Images.SetKeyName(4, "bt_audio.png");
			this.m_il_bt_device_types.Images.SetKeyName(5, "bt_peripheral.png");
			this.m_il_bt_device_types.Images.SetKeyName(6, "bt_imaging.png");
			this.m_il_bt_device_types.Images.SetKeyName(7, "bt_wearable.png");
			// 
			// m_lb_devices
			// 
			this.m_lb_devices.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lb_devices.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_lb_devices.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.m_lb_devices.FormattingEnabled = true;
			this.m_lb_devices.IntegralHeight = false;
			this.m_lb_devices.ItemHeight = 54;
			this.m_lb_devices.Location = new System.Drawing.Point(10, 60);
			this.m_lb_devices.Name = "m_lb_devices";
			this.m_lb_devices.Size = new System.Drawing.Size(336, 166);
			this.m_lb_devices.TabIndex = 12;
			// 
			// m_cb_radio
			// 
			this.m_cb_radio.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_cb_radio.DisplayProperty = null;
			this.m_cb_radio.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_radio.FormattingEnabled = true;
			this.m_cb_radio.Location = new System.Drawing.Point(225, 17);
			this.m_cb_radio.Name = "m_cb_radio";
			this.m_cb_radio.PreserveSelectionThruFocusChange = false;
			this.m_cb_radio.Size = new System.Drawing.Size(121, 21);
			this.m_cb_radio.TabIndex = 11;
			// 
			// m_chk_enable_bt
			// 
			this.m_chk_enable_bt.AutoCheck = true;
			this.m_chk_enable_bt.AutoSize = true;
			this.m_chk_enable_bt.Checked = false;
			this.m_chk_enable_bt.CheckedColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(118)))), ((int)(((byte)(255)))));
			this.m_chk_enable_bt.FlatAppearance.BorderSize = 0;
			this.m_chk_enable_bt.Location = new System.Drawing.Point(96, 10);
			this.m_chk_enable_bt.Name = "m_chk_enable_bt";
			this.m_chk_enable_bt.Padding = new System.Windows.Forms.Padding(3);
			this.m_chk_enable_bt.Size = new System.Drawing.Size(64, 32);
			this.m_chk_enable_bt.TabIndex = 0;
			this.m_chk_enable_bt.Text = "Enable Bluetooth";
			this.m_chk_enable_bt.ThumbColor = System.Drawing.Color.WhiteSmoke;
			this.m_chk_enable_bt.UncheckedColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
			this.m_chk_enable_bt.UseVisualStyleBackColor = true;
			// 
			// BluetoothUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(358, 308);
			this.Controls.Add(this.m_lb_devices);
			this.Controls.Add(this.m_cb_radio);
			this.Controls.Add(this.m_chk_discoverable);
			this.Controls.Add(this.m_chk_show_authenticated);
			this.Controls.Add(this.m_chk_show_connected);
			this.Controls.Add(this.m_chk_show_remembered);
			this.Controls.Add(this.m_chk_show_unknown);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_lbl_enable_bt);
			this.Controls.Add(this.m_chk_enable_bt);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(370, 200);
			this.Name = "BluetoothUI";
			this.Text = "Choose a Bluetooth Device";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
