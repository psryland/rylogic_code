using System;
using System.ComponentModel;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.util;

namespace pr.gui
{
	public class PatternFilter :UserControl
	{
		#region UI Elements
		private ComboBox m_cb_pattern;
		private Button m_btn_patn_detail;
		#endregion

		public PatternFilter()
		{
			InitializeComponent();
			History = new Pattern[0];
			Pattern = new Pattern(EPattern.Substring, string.Empty);
			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}

		/// <summary>The pattern</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Pattern Pattern
		{
			get { return m_pattern; }
			set
			{
				if (m_pattern != value)
				{
					m_pattern = value ?? new Pattern(EPattern.Substring, string.Empty);

					// Save the pattern to the history
					if (m_pattern.Expr.HasValue())
						History = Util.AddToHistoryList(History, new Pattern(m_pattern), false, 10);

					// Assign the pattern to the combo box (if set up)
					if (m_cb_pattern.ValueType == typeof(Pattern))
						m_cb_pattern.Value = new Pattern(m_pattern);
				}

				// Notify pattern changed/applied
				OnPatternChanged();
			}
		}
		private Pattern m_pattern;

		/// <summary>Previous patterns</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Pattern[] History
		{
			get { return m_history; }
			set
			{
				if (m_history == value) return;
				m_history = value;

				m_cb_pattern.Items.Clear();
				m_cb_pattern.Items.AddRange(m_history);
			}
		}
		private Pattern[] m_history;

		/// <summary>Raised when the pattern is changed</summary>
		public event EventHandler PatternChanged;
		protected virtual void OnPatternChanged()
		{
			PatternChanged.Raise(this, EventArgs.Empty);
		}

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			// Detail button
			m_btn_patn_detail.Click += (s,a) =>
			{
				// Show a modal form with the full pattern UI
				Pattern = PatternUI.ShowDialog(this, Pattern);
			};

			// Pattern expression combo
			m_cb_pattern.ValueType = typeof(Pattern);
			m_cb_pattern.DisplayMember = nameof(Pattern.Expr);
			m_cb_pattern.ValidateText = t =>
			{
				var patn = ((Pattern)m_cb_pattern.Value) ?? new Pattern(Pattern);
				patn.Expr = t;
				return patn.IsValid;
			};
			m_cb_pattern.ValueToText = v =>
			{
				return ((Pattern)v)?.Expr ?? string.Empty;
			};
			m_cb_pattern.TextToValue = t =>
			{
				var patn = ((Pattern)m_cb_pattern.Value) ?? new Pattern(Pattern);
				patn.Expr = t;
				return patn;
			};
			m_cb_pattern.ValueCommitted += (s,a) =>
			{
				Pattern = (Pattern)a.Value;
			};
			m_cb_pattern.Value = Pattern;
		}

		#region Component Designer generated code
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PatternFilter));
			this.m_cb_pattern = new pr.gui.ComboBox();
			this.m_btn_patn_detail = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// m_cb_pattern
			// 
			this.m_cb_pattern.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_cb_pattern.BackColorInvalid = System.Drawing.Color.White;
			this.m_cb_pattern.BackColorValid = System.Drawing.Color.White;
			this.m_cb_pattern.CommitValueOnFocusLost = true;
			this.m_cb_pattern.DisplayProperty = null;
			this.m_cb_pattern.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_cb_pattern.ForeColorValid = System.Drawing.Color.Black;
			this.m_cb_pattern.FormattingEnabled = true;
			this.m_cb_pattern.Location = new System.Drawing.Point(3, 3);
			this.m_cb_pattern.Name = "m_cb_pattern";
			this.m_cb_pattern.PreserveSelectionThruFocusChange = false;
			this.m_cb_pattern.Size = new System.Drawing.Size(531, 21);
			this.m_cb_pattern.TabIndex = 0;
			this.m_cb_pattern.UseValidityColours = true;
			this.m_cb_pattern.Value = null;
			// 
			// m_btn_patn_type
			// 
			this.m_btn_patn_detail.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_patn_detail.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("m_btn_patn_type.BackgroundImage")));
			this.m_btn_patn_detail.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.m_btn_patn_detail.Location = new System.Drawing.Point(540, 3);
			this.m_btn_patn_detail.Name = "m_btn_patn_type";
			this.m_btn_patn_detail.Size = new System.Drawing.Size(23, 21);
			this.m_btn_patn_detail.TabIndex = 1;
			this.m_btn_patn_detail.UseVisualStyleBackColor = true;
			// 
			// PatternFilter
			// 
			this.Controls.Add(this.m_btn_patn_detail);
			this.Controls.Add(this.m_cb_pattern);
			this.Name = "PatternFilter";
			this.Size = new System.Drawing.Size(566, 27);
			this.ResumeLayout(false);

		}
		#endregion
	}
}
