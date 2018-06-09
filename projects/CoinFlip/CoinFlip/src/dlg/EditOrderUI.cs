using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Graphix;
using Rylogic.Gui;
using Rylogic.Maths;
using Rylogic.Utility;
using ComboBox = Rylogic.Gui.ComboBox;

namespace CoinFlip
{
	public class EditOrderUI :ToolForm
	{
		/// <summary>If not null, then 'Trade' exists and this UI should modify the trade</summary>
		private ulong? m_existing_order_id;

		/// <summary>A copy of the trade we were handed to detect changes</summary>
		private Trade m_initial;

		#region UI Elements
		private Button m_btn_ok;
		private ImageList m_il_buttons;
		private Button m_btn_cancel;
		private ValueBox m_tb_price_q2b;
		private ToolTip m_tt;
		private Label m_lbl_volume_in;
		private ValueBox m_tb_volume_in;
		private Label m_lbl_volume_out;
		private ValueBox m_tb_volume_out;
		private Label m_lbl_desc_in;
		private Label m_lbl_exchange;
		private ComboBox m_cb_exchange;
		private Label m_lbl_available_volume_in;
		private Button m_btn_all_in;
		private Label m_lbl_order_type;
		private TextBox m_tb_order_type;
		private TableLayoutPanel m_table0;
		private Panel m_panel_row3_right;
		private Panel m_panel_row2_right;
		private Panel m_panel_row0;
		private Panel m_panel_row2_left;
		private Panel m_panel_row3_left;
		private Panel m_panel_row4;
		private Panel m_panel_row1;
		private ComboBox m_cb_trade_direction;
		private Label m_lbl_trade_type;
		private Label m_lbl_desc_out;
		private Label m_lbl_desc_errors;
		private Label m_lbl_order_price;
		#endregion

		public EditOrderUI(Trade trade, ChartUI chart, ulong? existing_order_id)
			:base(trade.Model.UI, EPin.Centre)
		{
			InitializeComponent();
			HideOnClose = false;

			m_existing_order_id = existing_order_id;
			m_initial = new Trade(trade);
			Trade = trade;
			Chart = chart;
			GfxTrade = new TradeIndicator(this){ Chart = chart.ChartCtrl };

			SetupUI();
			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
			GfxTrade = null;
			Chart = null;
			Trade = null;
		}
		protected override void OnFormClosed(FormClosedEventArgs e)
		{
			if (e.CloseReason == CloseReason.UserClosing && DialogResult == DialogResult.OK)
				ApplyChanges();

			base.OnFormClosed(e);
		}

		/// <summary>The chart to draw associated graphics on</summary>
		public ChartUI Chart
		{
			[DebuggerStepThrough] get { return m_chart; }
			private set
			{
				if (m_chart == value) return;
				if (m_chart != null)
				{
				}
				m_chart = value;
				if (m_chart != null)
				{
					
				}
			}
		}
		private ChartUI m_chart;

		/// <summary>The trade being edited</summary>
		public Trade Trade
		{
			[DebuggerStepThrough] get { return m_trade; }
			private set
			{
				if (m_trade == value) return;
				m_trade = value;
				if (m_trade != null)
					SetTrade();
			}
		}
		private Trade m_trade;

		/// <summary></summary>
		public Model Model
		{
			[DebuggerStepThrough] get { return Trade.Model; }
		}

		/// <summary></summary>
		public TradePair Pair
		{
			[DebuggerStepThrough] get { return Trade?.Pair; }
		}

		/// <summary></summary>
		public Exchange Exchange
		{
			[DebuggerStepThrough] get { return Pair.Exchange; }
		}

		/// <summary></summary>
		public Coin CoinIn
		{
			[DebuggerStepThrough] get { return Trade.CoinIn; }
		}

		/// <summary></summary>
		public Coin CoinOut
		{
			[DebuggerStepThrough] get { return Trade.CoinOut; }
		}

		/// <summary>Return the available balance of currency to sell. Includes the 'm_initial.VolumeIn' when modifying 'Trade'</summary>
		public Unit<decimal> AvailableIn
		{
			get { return Exchange.Balance[CoinIn][Trade.FundId].Available + AdditionalIn; }
		}

		/// <summary>Return the available balance of currency to buy. Includes the 'm_initial.VolumeIn' when modifying 'Trade'</summary>
		public Unit<decimal> AvailableOut
		{
			get { return Exchange.Balance[CoinOut][Trade.FundId].Available; }
		}
		private Unit<decimal> AdditionalIn
		{
			get { return (m_existing_order_id != null ? m_initial.VolumeIn : 0m._(CoinIn)); }
		}

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			// Exchange
			m_cb_exchange.Items.AddRange(Model.Exchanges.Where(x => Model.FindPairOnExchange(Pair.Name, x) != null));
			m_cb_exchange.SelectedItem = Exchange;
			m_cb_exchange.DisplayMember = nameof(Exchange.Name);
			m_cb_exchange.SelectedIndexChanged += (s,a) =>
			{
				SetTrade(exch:(Exchange)m_cb_exchange.SelectedItem);
			};
			m_cb_exchange.Enabled = m_existing_order_id == null;

			// Trade direction
			m_cb_trade_direction.DataSource = Enum<ETradeType>.ValuesArray;
			m_cb_trade_direction.SelectedItem = Trade.TradeType;
			m_cb_trade_direction.Format += (s,a) =>
			{
				a.Value =
					(ETradeType)a.ListItem == ETradeType.B2Q ? $"{Pair.Base} → {Pair.Quote}" :
					(ETradeType)a.ListItem == ETradeType.Q2B ? $"{Pair.Quote} → {Pair.Base}" :
					throw new Exception("Unknown trade type");
			};
			m_cb_trade_direction.SelectedIndexChanged += (s,a) =>
			{
				SetTrade(tt:(ETradeType)m_cb_trade_direction.SelectedItem);
			};
			m_cb_trade_direction.Enabled = m_existing_order_id == null;

			// Order price
			m_lbl_order_price.Text = $"Order Price ({Trade.Pair.RateUnits}):";
			m_tb_price_q2b.ToolTip(m_tt, "The price at which to make the trade");
			m_tb_price_q2b.ValueType = typeof(decimal);
			m_tb_price_q2b.ValidateText = t =>
			{
				return
					decimal.TryParse(t, out var v) &&
					Trade.PriceRange.Contains(v._(Pair.RateUnits));
			};
			m_tb_price_q2b.ValueChanged += (s,a) =>
			{
				SetTrade(price_q2b:((decimal)m_tb_price_q2b.Value)._(Trade.Pair.RateUnits));
			};

			// Volume In
			m_lbl_volume_in.Text = $"Volume to Sell ({CoinIn}):";
			m_tb_volume_in.ToolTip(m_tt, $"The amount of {CoinIn} to sell. Use '%' for percentage of available balance.");
			m_tb_volume_in.ValueType = typeof(decimal);
			m_tb_volume_in.ValidateText = t =>
			{
				var vol = VolIn(t);
				return vol != null && Trade.VolumeRangeIn.Contains(vol.Value);
			};
			m_tb_volume_in.TextToValue = t =>
			{
				return (decimal)VolIn(t).Value;
			};
			m_tb_volume_in.ValueToText = v =>
			{
				return ((decimal)v)._(CoinIn).ToString("G8",false);
			};
			m_tb_volume_in.ValueChanged += (s,a) =>
			{
				SetTrade(volume_in:((decimal)m_tb_volume_in.Value)._(CoinIn));
			};

			// Volume Out
			m_lbl_volume_out.Text = $"Volume to Buy ({CoinOut}):";
			m_tb_volume_out.ToolTip(m_tt, $"The amount of {CoinOut} to buy. Use '%' for percentage of available balance.");
			m_tb_volume_out.ValueType = typeof(decimal);
			m_tb_volume_out.ValidateText = t =>
			{
				var vol = VolOut(t);
				return vol != null && Trade.VolumeRangeOut.Contains(vol.Value);
			};
			m_tb_volume_out.TextToValue = t =>
			{
				return (decimal)VolOut(t).Value;
			};
			m_tb_volume_out.ValueToText = v =>
			{
				return ((decimal)v)._(CoinOut).ToString("G8",false);
			};
			m_tb_volume_out.ValueChanged += (s,a) =>
			{
				SetTrade(volume_out:((decimal)m_tb_volume_out.Value)._(CoinOut));
			};

			// All In
			m_btn_all_in.ToolTip(m_tt, $"Use the entire available balance of {CoinIn}");
			m_btn_all_in.Click += (s,a) =>
			{
				SetTrade(volume_in:AvailableIn);
			};

			// Ok button - not default to prevent 'Enter' causing trades to be made
			m_btn_ok.Text = m_existing_order_id != null ? "Modify Order" : "Create Order";
			m_btn_ok.Click += (s,a) =>
			{
				DialogResult = m_btn_ok.DialogResult;
				Close();
			};

			// Clean a value text string with optional %
			string Clean(string text, out bool pc)
			{
				text = text.Trim(' ', '\t');
				pc = text.EndsWith("%");
				return text.TrimEnd('%');
			}
			Unit<decimal>? VolIn(string text)
			{
				text = Clean(text, out var pc);
				return decimal.TryParse(text, out var value)
					? (pc ? AvailableIn * value * 0.01m : value._(CoinIn))
					: (Unit<decimal>?)null;
			}
			Unit<decimal>? VolOut(string text)
			{
				text = Clean(text, out var pc);
				return decimal.TryParse(text, out var value)
					? (pc ? AvailableOut * value * 0.01m : value._(CoinOut))
					: (Unit<decimal>?)null;
			}
		}

		/// <summary>Update the UI</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			var available_in = AvailableIn;
			var validate = Trade.Validate(additional_balance_in:AdditionalIn);
			var vol_in_pc = Math_.Clamp(Math_.Div((decimal)Trade.VolumeIn * 100m, (decimal)available_in, 0m), 0m, 100m);

			// Update fields
			m_tb_price_q2b.Value = (decimal)Trade.PriceQ2B;
			m_tb_volume_in.Value = (decimal)Trade.VolumeIn;
			m_tb_volume_out.Value = (decimal)Trade.VolumeOut;

			// Order type
			m_tb_order_type.Text = $"{Trade.OrderType}";

			// Available funds
			m_lbl_available_volume_in.Text = $"Available: {available_in.ToString("G8",true)}";

			// Describe the trade
			m_lbl_desc_in.Text = 
				$"Trading {vol_in_pc:G4}% of {CoinIn} balance";
			m_lbl_desc_out.Text =
				$"After Fees: {Trade.VolumeNett.ToString("G6", true)}";
			m_lbl_desc_errors.Text =
				validate.ToErrorDescription();

			// Enable the 'OK' button if the trade is valid
			m_btn_ok.Enabled = validate == EValidation.Valid && (m_existing_order_id == null || !Trade.Equals(m_initial));
			m_btn_ok.ToolTip(m_tt, validate.ToErrorDescription());
		}

		/// <summary>Set the values on the trade</summary>
		private void SetTrade(Exchange exch = null, ETradeType? tt = null, Unit<decimal>? price_q2b = null, Unit<decimal>? volume_in = null, Unit<decimal>? volume_out = null)
		{
			if (UpdatingTradeValues) return;
			using (UpdatingTrade())
			{
				if (exch != null)
				{
					Trade.Pair = Model.FindPairOnExchange(Pair.Name, exch);
				}
				if (tt != null)
				{
					Trade.TradeType = tt.Value;
				}
				if (price_q2b != null)
				{
					// Cause the Volume In amount to stay fixed
					if (volume_in == null)
						volume_in = Trade.VolumeIn;

					Trade.PriceQ2B = price_q2b.Value;
				}
				if (volume_in != null)
				{
					Trade.VolumeIn = volume_in.Value;
				}
				if (volume_out != null)
				{
					Trade.VolumeOut = volume_out.Value;
				}

				// Limit to the available balance
				var bal = AvailableIn;
				if (Trade.VolumeIn > bal)
				{
					Trade.VolumeIn = bal;
				}

				// Recreate the graphics
				GfxTrade?.Invalidate();
				m_chart?.Invalidate();

				UpdateUI();
			}
		}

		/// <summary>True while the trade values are being updated</summary>
		private bool UpdatingTradeValues
		{
			get { return m_updating_trade_values != 0; }
		}
		private Scope UpdatingTrade()
		{
			return Scope.Create(() => ++m_updating_trade_values, x => --m_updating_trade_values);
		}
		private int m_updating_trade_values;

		/// <summary>Commit the changes to the trade or create a new trade</summary>
		private void ApplyChanges()
		{
			m_btn_ok.Enabled = false;

			// Stop orders
			if (Trade.OrderType == EPlaceOrderType.Stop)
			{
				MsgBox.Show(Model.UI, "Stop orders are not currently supported");
				return;
			}

			// Create or update the trade
			if (m_existing_order_id == null)
			{
				try { Trade.CreateOrder(); }
				catch (Exception ex)
				{
					Model.Log.Write(ELogLevel.Error, ex, "Failed to create trade");
					MsgBox.Show(Model.UI, $"Failed to create trade.\r\n{ex.Message}", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			else if (m_initial != Trade)
			{
				try
				{
					// Cancel the previous order
					Exchange.CancelOrder(Pair, m_existing_order_id.Value);

					// Flag that a balance update is required
					Exchange.BalanceUpdateRequired = true;

					// Wait for the balance update to complete
					Model.WaitWhile(() => Exchange.BalanceUpdateRequired, TimeSpan.FromMinutes(1.0));

					// Place the updated order
					Trade.CreateOrder();
				}
				catch (Exception ex)
				{
					Model.Log.Write(ELogLevel.Error, ex, "Modifying a trade failed");
					MsgBox.Show(Model.UI, $"Failed to modify this trade.\r\n{ex.Message}", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		#region Graphics

		/// <summary>The trade price indicator</summary>
		private TradeIndicator GfxTrade
		{
			get { return m_gfx_trade; }
			set
			{
				if (m_gfx_trade == value) return;
				Util.Dispose(ref m_gfx_trade);
				m_gfx_trade = value;
			}
		}
		private TradeIndicator m_gfx_trade;

		/// <summary>Chart graphics representing the trade</summary>
		private class TradeIndicator :ChartControl.Element
		{
			private const float GripperWidthFrac = 0.05f;
			private const float GripperHeight = 24f;
			private readonly EditOrderUI m_ui;

			public TradeIndicator(EditOrderUI ui)
				:base(Guid.NewGuid(), m4x4.Identity, "Trade")
			{
				m_ui = ui;
			}
			protected override void Dispose(bool disposing)
			{
				Gfx = null;
				base.Dispose(disposing);
			}
			protected override void SetChartCore(ChartControl chart)
			{
				if (Chart != null)
				{
					Chart.MouseDown -= HandleMouseDown;
				}
				base.SetChartCore(chart);
				if (Chart != null)
				{
					Chart.MouseDown += HandleMouseDown;
				}

				// Handlers
				void HandleMouseDown(object sender, MouseEventArgs args)
				{
					if (Hovered && args.Button == MouseButtons.Left)
						Chart.MouseOperations.SetPending(MouseButtons.Left, new DragPrice(m_ui));
				}
			}

			/// <summary>The graphics object</summary>
			public View3d.Object Gfx
			{
				get { return m_gfx; }
				private set
				{
					if (m_gfx == value) return;
					Util.Dispose(ref m_gfx);
					m_gfx = value;
				}
			}
			private View3d.Object m_gfx;

			/// <summary>Position/Colour the graphics</summary>
			protected override void UpdateGfxCore()
			{
				base.UpdateGfxCore();

				// Price level
				var price = m_ui.Trade.PriceQ2B;

				// Colour based on trade direction
				var col = (Colour32)(
					m_ui.Trade.TradeType == ETradeType.Q2B ? m_ui.Chart.ChartSettings.AskColour :
					m_ui.Trade.TradeType == ETradeType.B2Q ? m_ui.Chart.ChartSettings.BidColour :
					throw new Exception("Unknown trade type"));

				var ldr =
					$"*Group trade "+
					$"{{"+
					$"  *Line gripper {col} {{ {1f - GripperWidthFrac} 0 0  1 0 0 *Width {{{GripperHeight}}} }}"+
					$"  *Line level {col} {{0 0 0 1 0 0}}"+
					$"  *Line halo {col.Alpha(0.25f)} {{0 0 0 1 0 0 *Width {{{GripperHeight * 0.75f}}} *Hidden }}"+
					$"  *Font{{*Name{{\"tahoma\"}} *Size{{8}} *Weight{{500}} *Colour{{FFFFFFFF}}}}"+
					$"  *Text price {{ \"{price.ToString("G8",false)}\" *Billboard *Anchor {{+1 0}} *BackColour {{{col}}} *o2w{{*pos{{1 0 0}}}} *NoZTest }}"+
					$"}}";

				Gfx = new View3d.Object(ldr, false, Id, null);
			}

			/// <summary>Add the graphics to the chart</summary>
			protected override void UpdateSceneCore(View3d.Window window)
			{
				base.UpdateSceneCore(window);
				if (Gfx == null) return;

				if (Visible)
				{
					Gfx.Child("halo").Visible = Hovered;
					Gfx.O2P =
						m4x4.Translation((float)Chart.XAxis.Min, (float)(decimal)m_ui.Trade.PriceQ2B, ZOrder.Indicators) * 
						m4x4.Scale((float)Chart.XAxis.Span, 1f, 1f, v4.Origin);

					window.AddObject(Gfx);
				}
				else
				{
					window.RemoveObject(Gfx);
				}
			}

			/// <summary>Hit test the trade price indicator</summary>
			public override ChartControl.HitTestResult.Hit HitTest(PointF chart_point, Point client_point, Keys modifier_keys, View3d.Camera cam)
			{
				var price = (float)(decimal)m_ui.Trade.PriceQ2B;

				// Find the nearest point to 'client_point' on the line
				var chart_pt = chart_point;
				var client_pt = Chart.ChartToClient(new PointF(chart_pt.X, price));

				// If the point is within tolerance of the gripper
				if (Math_.Frac(Chart.XAxis.Min, chart_pt.X, Chart.XAxis.Max) > 1.0f - GripperWidthFrac &&
					Math.Abs(client_pt.Y - client_point.Y) < GripperHeight)
					return new ChartControl.HitTestResult.Hit(this, new PointF(chart_pt.X, price), null);

				return null;
			}

			/// <summary>Drag the indicator to change the price</summary>
			private class DragPrice :ChartControl.MouseOp
			{
				public DragPrice(EditOrderUI ui)
					:base(ui.Chart.ChartCtrl)
				{
					UI = ui;
				}
				public override void Dispose()
				{
					UI = null;
					base.Dispose();
				}
				public override void MouseMove(MouseEventArgs e)
				{
					var chart_pt = m_chart.ClientToChart(e.Location);
					UI.SetTrade(price_q2b: ((decimal)chart_pt.Y)._(UI.Pair.RateUnits));
					base.MouseMove(e);
				}

				/// <summary>The owning UI</summary>
				private EditOrderUI UI
				{
					get { return m_ui; }
					set
					{
						if (m_ui == value) return;
						if (m_ui != null) m_ui.Disposed -= HandleUIDisposed;
						m_ui = value;
						if (m_ui != null) m_ui.Disposed += HandleUIDisposed;
						void HandleUIDisposed(object sender, EventArgs e)
						{
							Dispose();
						}
					}
				}
				private EditOrderUI m_ui;
			}
		}

		#endregion

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditOrderUI));
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_il_buttons = new System.Windows.Forms.ImageList(this.components);
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_tb_price_q2b = new Rylogic.Gui.ValueBox();
			this.m_lbl_order_price = new System.Windows.Forms.Label();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_lbl_volume_in = new System.Windows.Forms.Label();
			this.m_tb_volume_in = new Rylogic.Gui.ValueBox();
			this.m_lbl_volume_out = new System.Windows.Forms.Label();
			this.m_tb_volume_out = new Rylogic.Gui.ValueBox();
			this.m_lbl_desc_in = new System.Windows.Forms.Label();
			this.m_lbl_exchange = new System.Windows.Forms.Label();
			this.m_cb_exchange = new Rylogic.Gui.ComboBox();
			this.m_lbl_available_volume_in = new System.Windows.Forms.Label();
			this.m_btn_all_in = new System.Windows.Forms.Button();
			this.m_lbl_order_type = new System.Windows.Forms.Label();
			this.m_tb_order_type = new System.Windows.Forms.TextBox();
			this.m_table0 = new System.Windows.Forms.TableLayoutPanel();
			this.m_panel_row1 = new System.Windows.Forms.Panel();
			this.m_cb_trade_direction = new Rylogic.Gui.ComboBox();
			this.m_lbl_trade_type = new System.Windows.Forms.Label();
			this.m_panel_row3_right = new System.Windows.Forms.Panel();
			this.m_panel_row2_right = new System.Windows.Forms.Panel();
			this.m_panel_row0 = new System.Windows.Forms.Panel();
			this.m_panel_row2_left = new System.Windows.Forms.Panel();
			this.m_panel_row3_left = new System.Windows.Forms.Panel();
			this.m_panel_row4 = new System.Windows.Forms.Panel();
			this.m_lbl_desc_out = new System.Windows.Forms.Label();
			this.m_lbl_desc_errors = new System.Windows.Forms.Label();
			this.m_table0.SuspendLayout();
			this.m_panel_row1.SuspendLayout();
			this.m_panel_row3_right.SuspendLayout();
			this.m_panel_row2_right.SuspendLayout();
			this.m_panel_row0.SuspendLayout();
			this.m_panel_row2_left.SuspendLayout();
			this.m_panel_row3_left.SuspendLayout();
			this.m_panel_row4.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_btn_ok.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.m_btn_ok.ImageKey = "check_accept.png";
			this.m_btn_ok.ImageList = this.m_il_buttons;
			this.m_btn_ok.Location = new System.Drawing.Point(87, 10);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
			this.m_btn_ok.Size = new System.Drawing.Size(174, 49);
			this.m_btn_ok.TabIndex = 0;
			this.m_btn_ok.Text = "Create Order";
			this.m_btn_ok.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_il_buttons
			// 
			this.m_il_buttons.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_il_buttons.ImageStream")));
			this.m_il_buttons.TransparentColor = System.Drawing.Color.Transparent;
			this.m_il_buttons.Images.SetKeyName(0, "check_accept.png");
			this.m_il_buttons.Images.SetKeyName(1, "check_reject.png");
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_btn_cancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.m_btn_cancel.ImageKey = "check_reject.png";
			this.m_btn_cancel.ImageList = this.m_il_buttons;
			this.m_btn_cancel.Location = new System.Drawing.Point(267, 10);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
			this.m_btn_cancel.Size = new System.Drawing.Size(136, 49);
			this.m_btn_cancel.TabIndex = 1;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_tb_price_q2b
			// 
			this.m_tb_price_q2b.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_price_q2b.BackColor = System.Drawing.Color.White;
			this.m_tb_price_q2b.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_price_q2b.BackColorValid = System.Drawing.Color.White;
			this.m_tb_price_q2b.CommitValueOnFocusLost = true;
			this.m_tb_price_q2b.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_tb_price_q2b.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_price_q2b.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_price_q2b.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_price_q2b.Location = new System.Drawing.Point(18, 21);
			this.m_tb_price_q2b.Name = "m_tb_price_q2b";
			this.m_tb_price_q2b.Size = new System.Drawing.Size(182, 22);
			this.m_tb_price_q2b.TabIndex = 0;
			this.m_tb_price_q2b.UseValidityColours = true;
			this.m_tb_price_q2b.Value = null;
			// 
			// m_lbl_order_price
			// 
			this.m_lbl_order_price.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_order_price.AutoSize = true;
			this.m_lbl_order_price.Location = new System.Drawing.Point(4, 5);
			this.m_lbl_order_price.Name = "m_lbl_order_price";
			this.m_lbl_order_price.Size = new System.Drawing.Size(63, 13);
			this.m_lbl_order_price.TabIndex = 3;
			this.m_lbl_order_price.Text = "Order Price:";
			// 
			// m_lbl_volume_in
			// 
			this.m_lbl_volume_in.AutoSize = true;
			this.m_lbl_volume_in.Location = new System.Drawing.Point(5, 6);
			this.m_lbl_volume_in.Name = "m_lbl_volume_in";
			this.m_lbl_volume_in.Size = new System.Drawing.Size(77, 13);
			this.m_lbl_volume_in.TabIndex = 4;
			this.m_lbl_volume_in.Text = "Volume to Sell:";
			// 
			// m_tb_volume_in
			// 
			this.m_tb_volume_in.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_volume_in.BackColor = System.Drawing.Color.White;
			this.m_tb_volume_in.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_volume_in.BackColorValid = System.Drawing.Color.White;
			this.m_tb_volume_in.CommitValueOnFocusLost = true;
			this.m_tb_volume_in.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_tb_volume_in.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_volume_in.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_volume_in.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_volume_in.Location = new System.Drawing.Point(18, 22);
			this.m_tb_volume_in.Name = "m_tb_volume_in";
			this.m_tb_volume_in.Size = new System.Drawing.Size(182, 22);
			this.m_tb_volume_in.TabIndex = 0;
			this.m_tb_volume_in.UseValidityColours = true;
			this.m_tb_volume_in.Value = null;
			// 
			// m_lbl_volume_out
			// 
			this.m_lbl_volume_out.AutoSize = true;
			this.m_lbl_volume_out.Location = new System.Drawing.Point(6, 6);
			this.m_lbl_volume_out.Name = "m_lbl_volume_out";
			this.m_lbl_volume_out.Size = new System.Drawing.Size(78, 13);
			this.m_lbl_volume_out.TabIndex = 6;
			this.m_lbl_volume_out.Text = "Volume to Buy:";
			// 
			// m_tb_volume_out
			// 
			this.m_tb_volume_out.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_volume_out.BackColor = System.Drawing.Color.White;
			this.m_tb_volume_out.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_volume_out.BackColorValid = System.Drawing.Color.White;
			this.m_tb_volume_out.CommitValueOnFocusLost = true;
			this.m_tb_volume_out.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_tb_volume_out.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_volume_out.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_volume_out.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_volume_out.Location = new System.Drawing.Point(22, 22);
			this.m_tb_volume_out.Name = "m_tb_volume_out";
			this.m_tb_volume_out.Size = new System.Drawing.Size(175, 22);
			this.m_tb_volume_out.TabIndex = 0;
			this.m_tb_volume_out.UseValidityColours = true;
			this.m_tb_volume_out.Value = null;
			// 
			// m_lbl_desc_in
			// 
			this.m_lbl_desc_in.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_desc_in.Location = new System.Drawing.Point(15, 72);
			this.m_lbl_desc_in.Margin = new System.Windows.Forms.Padding(0);
			this.m_lbl_desc_in.Name = "m_lbl_desc_in";
			this.m_lbl_desc_in.Size = new System.Drawing.Size(185, 112);
			this.m_lbl_desc_in.TabIndex = 8;
			this.m_lbl_desc_in.Text = "description";
			// 
			// m_lbl_exchange
			// 
			this.m_lbl_exchange.AutoSize = true;
			this.m_lbl_exchange.Location = new System.Drawing.Point(38, 13);
			this.m_lbl_exchange.Name = "m_lbl_exchange";
			this.m_lbl_exchange.Size = new System.Drawing.Size(58, 13);
			this.m_lbl_exchange.TabIndex = 9;
			this.m_lbl_exchange.Text = "Exchange:";
			// 
			// m_cb_exchange
			// 
			this.m_cb_exchange.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.m_cb_exchange.BackColorInvalid = System.Drawing.Color.White;
			this.m_cb_exchange.BackColorValid = System.Drawing.Color.White;
			this.m_cb_exchange.CommitValueOnFocusLost = true;
			this.m_cb_exchange.DisplayProperty = null;
			this.m_cb_exchange.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_exchange.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_cb_exchange.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_cb_exchange.ForeColorValid = System.Drawing.Color.Black;
			this.m_cb_exchange.FormattingEnabled = true;
			this.m_cb_exchange.Location = new System.Drawing.Point(125, 8);
			this.m_cb_exchange.Name = "m_cb_exchange";
			this.m_cb_exchange.PreserveSelectionThruFocusChange = false;
			this.m_cb_exchange.Size = new System.Drawing.Size(169, 24);
			this.m_cb_exchange.TabIndex = 0;
			this.m_cb_exchange.UseValidityColours = true;
			this.m_cb_exchange.Value = null;
			// 
			// m_lbl_available_volume_in
			// 
			this.m_lbl_available_volume_in.AutoSize = true;
			this.m_lbl_available_volume_in.Location = new System.Drawing.Point(38, 47);
			this.m_lbl_available_volume_in.Name = "m_lbl_available_volume_in";
			this.m_lbl_available_volume_in.Size = new System.Drawing.Size(50, 13);
			this.m_lbl_available_volume_in.TabIndex = 11;
			this.m_lbl_available_volume_in.Text = "Available";
			// 
			// m_btn_all_in
			// 
			this.m_btn_all_in.Image = ((System.Drawing.Image)(resources.GetObject("m_btn_all_in.Image")));
			this.m_btn_all_in.Location = new System.Drawing.Point(19, 45);
			this.m_btn_all_in.Margin = new System.Windows.Forms.Padding(0);
			this.m_btn_all_in.Name = "m_btn_all_in";
			this.m_btn_all_in.Size = new System.Drawing.Size(16, 16);
			this.m_btn_all_in.TabIndex = 1;
			this.m_btn_all_in.UseVisualStyleBackColor = true;
			// 
			// m_lbl_order_type
			// 
			this.m_lbl_order_type.AutoSize = true;
			this.m_lbl_order_type.Location = new System.Drawing.Point(6, 6);
			this.m_lbl_order_type.Name = "m_lbl_order_type";
			this.m_lbl_order_type.Size = new System.Drawing.Size(63, 13);
			this.m_lbl_order_type.TabIndex = 13;
			this.m_lbl_order_type.Text = "Order Type:";
			// 
			// m_tb_order_type
			// 
			this.m_tb_order_type.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_order_type.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_tb_order_type.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_tb_order_type.Location = new System.Drawing.Point(22, 22);
			this.m_tb_order_type.Name = "m_tb_order_type";
			this.m_tb_order_type.ReadOnly = true;
			this.m_tb_order_type.Size = new System.Drawing.Size(175, 22);
			this.m_tb_order_type.TabIndex = 0;
			// 
			// m_table0
			// 
			this.m_table0.ColumnCount = 2;
			this.m_table0.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.m_table0.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.m_table0.Controls.Add(this.m_lbl_desc_errors, 0, 4);
			this.m_table0.Controls.Add(this.m_panel_row1, 1, 1);
			this.m_table0.Controls.Add(this.m_panel_row3_right, 1, 3);
			this.m_table0.Controls.Add(this.m_panel_row2_right, 1, 2);
			this.m_table0.Controls.Add(this.m_panel_row0, 0, 0);
			this.m_table0.Controls.Add(this.m_panel_row2_left, 0, 2);
			this.m_table0.Controls.Add(this.m_panel_row3_left, 0, 3);
			this.m_table0.Controls.Add(this.m_panel_row4, 0, 5);
			this.m_table0.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table0.Location = new System.Drawing.Point(0, 0);
			this.m_table0.Name = "m_table0";
			this.m_table0.RowCount = 6;
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table0.Size = new System.Drawing.Size(412, 403);
			this.m_table0.TabIndex = 16;
			// 
			// m_panel_row1
			// 
			this.m_panel_row1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_table0.SetColumnSpan(this.m_panel_row1, 2);
			this.m_panel_row1.Controls.Add(this.m_cb_trade_direction);
			this.m_panel_row1.Controls.Add(this.m_lbl_trade_type);
			this.m_panel_row1.Location = new System.Drawing.Point(0, 40);
			this.m_panel_row1.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel_row1.Name = "m_panel_row1";
			this.m_panel_row1.Size = new System.Drawing.Size(412, 42);
			this.m_panel_row1.TabIndex = 7;
			// 
			// m_cb_trade_direction
			// 
			this.m_cb_trade_direction.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.m_cb_trade_direction.BackColorInvalid = System.Drawing.Color.White;
			this.m_cb_trade_direction.BackColorValid = System.Drawing.Color.White;
			this.m_cb_trade_direction.CommitValueOnFocusLost = true;
			this.m_cb_trade_direction.DisplayProperty = null;
			this.m_cb_trade_direction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_trade_direction.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_cb_trade_direction.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_cb_trade_direction.ForeColorValid = System.Drawing.Color.Black;
			this.m_cb_trade_direction.FormattingEnabled = true;
			this.m_cb_trade_direction.Location = new System.Drawing.Point(125, 10);
			this.m_cb_trade_direction.Name = "m_cb_trade_direction";
			this.m_cb_trade_direction.PreserveSelectionThruFocusChange = false;
			this.m_cb_trade_direction.Size = new System.Drawing.Size(169, 24);
			this.m_cb_trade_direction.TabIndex = 0;
			this.m_cb_trade_direction.UseValidityColours = true;
			this.m_cb_trade_direction.Value = null;
			// 
			// m_lbl_trade_type
			// 
			this.m_lbl_trade_type.AutoSize = true;
			this.m_lbl_trade_type.Location = new System.Drawing.Point(13, 15);
			this.m_lbl_trade_type.Name = "m_lbl_trade_type";
			this.m_lbl_trade_type.Size = new System.Drawing.Size(83, 13);
			this.m_lbl_trade_type.TabIndex = 9;
			this.m_lbl_trade_type.Text = "Trade Direction:";
			// 
			// m_panel_row3_right
			// 
			this.m_panel_row3_right.Controls.Add(this.m_lbl_desc_out);
			this.m_panel_row3_right.Controls.Add(this.m_tb_volume_out);
			this.m_panel_row3_right.Controls.Add(this.m_lbl_volume_out);
			this.m_panel_row3_right.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_panel_row3_right.Location = new System.Drawing.Point(206, 133);
			this.m_panel_row3_right.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel_row3_right.Name = "m_panel_row3_right";
			this.m_panel_row3_right.Size = new System.Drawing.Size(206, 188);
			this.m_panel_row3_right.TabIndex = 4;
			// 
			// m_panel_row2_right
			// 
			this.m_panel_row2_right.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel_row2_right.Controls.Add(this.m_tb_order_type);
			this.m_panel_row2_right.Controls.Add(this.m_lbl_order_type);
			this.m_panel_row2_right.Location = new System.Drawing.Point(206, 82);
			this.m_panel_row2_right.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel_row2_right.Name = "m_panel_row2_right";
			this.m_panel_row2_right.Size = new System.Drawing.Size(206, 51);
			this.m_panel_row2_right.TabIndex = 2;
			// 
			// m_panel_row0
			// 
			this.m_panel_row0.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_table0.SetColumnSpan(this.m_panel_row0, 2);
			this.m_panel_row0.Controls.Add(this.m_cb_exchange);
			this.m_panel_row0.Controls.Add(this.m_lbl_exchange);
			this.m_panel_row0.Location = new System.Drawing.Point(0, 0);
			this.m_panel_row0.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel_row0.Name = "m_panel_row0";
			this.m_panel_row0.Size = new System.Drawing.Size(412, 40);
			this.m_panel_row0.TabIndex = 0;
			// 
			// m_panel_row2_left
			// 
			this.m_panel_row2_left.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel_row2_left.Controls.Add(this.m_lbl_order_price);
			this.m_panel_row2_left.Controls.Add(this.m_tb_price_q2b);
			this.m_panel_row2_left.Location = new System.Drawing.Point(0, 82);
			this.m_panel_row2_left.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel_row2_left.Name = "m_panel_row2_left";
			this.m_panel_row2_left.Size = new System.Drawing.Size(206, 51);
			this.m_panel_row2_left.TabIndex = 1;
			// 
			// m_panel_row3_left
			// 
			this.m_panel_row3_left.Controls.Add(this.m_lbl_desc_in);
			this.m_panel_row3_left.Controls.Add(this.m_tb_volume_in);
			this.m_panel_row3_left.Controls.Add(this.m_btn_all_in);
			this.m_panel_row3_left.Controls.Add(this.m_lbl_volume_in);
			this.m_panel_row3_left.Controls.Add(this.m_lbl_available_volume_in);
			this.m_panel_row3_left.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_panel_row3_left.Location = new System.Drawing.Point(0, 133);
			this.m_panel_row3_left.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel_row3_left.Name = "m_panel_row3_left";
			this.m_panel_row3_left.Size = new System.Drawing.Size(206, 188);
			this.m_panel_row3_left.TabIndex = 3;
			// 
			// m_panel_row4
			// 
			this.m_panel_row4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_table0.SetColumnSpan(this.m_panel_row4, 2);
			this.m_panel_row4.Controls.Add(this.m_btn_cancel);
			this.m_panel_row4.Controls.Add(this.m_btn_ok);
			this.m_panel_row4.Location = new System.Drawing.Point(0, 334);
			this.m_panel_row4.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel_row4.Name = "m_panel_row4";
			this.m_panel_row4.Size = new System.Drawing.Size(412, 69);
			this.m_panel_row4.TabIndex = 6;
			// 
			// m_lbl_desc_out
			// 
			this.m_lbl_desc_out.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_desc_out.Location = new System.Drawing.Point(19, 72);
			this.m_lbl_desc_out.Margin = new System.Windows.Forms.Padding(0);
			this.m_lbl_desc_out.Name = "m_lbl_desc_out";
			this.m_lbl_desc_out.Size = new System.Drawing.Size(178, 112);
			this.m_lbl_desc_out.TabIndex = 9;
			this.m_lbl_desc_out.Text = "description";
			// 
			// m_lbl_desc_errors
			// 
			this.m_lbl_desc_errors.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_desc_errors.AutoSize = true;
			this.m_table0.SetColumnSpan(this.m_lbl_desc_errors, 2);
			this.m_lbl_desc_errors.Location = new System.Drawing.Point(0, 321);
			this.m_lbl_desc_errors.Margin = new System.Windows.Forms.Padding(0);
			this.m_lbl_desc_errors.Name = "m_lbl_desc_errors";
			this.m_lbl_desc_errors.Size = new System.Drawing.Size(412, 13);
			this.m_lbl_desc_errors.TabIndex = 9;
			this.m_lbl_desc_errors.Text = "description";
			// 
			// EditOrderUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(412, 403);
			this.Controls.Add(this.m_table0);
			this.MinimumSize = new System.Drawing.Size(367, 346);
			this.Name = "EditOrderUI";
			this.Text = "Edit Order";
			this.m_table0.ResumeLayout(false);
			this.m_table0.PerformLayout();
			this.m_panel_row1.ResumeLayout(false);
			this.m_panel_row1.PerformLayout();
			this.m_panel_row3_right.ResumeLayout(false);
			this.m_panel_row3_right.PerformLayout();
			this.m_panel_row2_right.ResumeLayout(false);
			this.m_panel_row2_right.PerformLayout();
			this.m_panel_row0.ResumeLayout(false);
			this.m_panel_row0.PerformLayout();
			this.m_panel_row2_left.ResumeLayout(false);
			this.m_panel_row2_left.PerformLayout();
			this.m_panel_row3_left.ResumeLayout(false);
			this.m_panel_row3_left.PerformLayout();
			this.m_panel_row4.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion
	}
}
