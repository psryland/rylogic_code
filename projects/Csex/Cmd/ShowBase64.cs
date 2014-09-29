using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Csex
{
	public class ShowBase64 :Cmd
	{
		/// <summary>Display help information in the case of an invalid command line</summary>
		public override void ShowHelp(Exception ex)
		{
			if (ex != null) Console.WriteLine("Error parsing command line: {0}", ex.Message);
			Console.Write(
				"Display a tool for encoding/decoding base64 encoded text"
				);
		}

		/// <summary>Handle a command line option.</summary>
		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option)
			{
			default: return base.CmdLineOption(option, args, ref arg);
			case "-showbase64": return true;
			}
		}

		/// <summary>Return true if all required options have been given</summary>
		public override bool OptionsValid()
		{
			return true;
		}

		/// <summary>Run the command</summary>
		public override int Run()
		{
			var edit = new TextBox
				{
					Anchor    = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
					Location  = new Point(12, 12),
					Multiline = true,
					Name      = "edit",
					Size      = new Size(360, 359),
					TabIndex  = 0,
				};
			var btn_decode = new Button
				{
					Anchor                  = AnchorStyles.Bottom | AnchorStyles.Right,
					Location                = new Point(297, 377),
					Name                    = "btn_decode",
					Size                    = new Size(75, 23),
					TabIndex                = 1,
					Text                    = "Decode",
					UseVisualStyleBackColor = true,
				};
			var btn_encode = new Button
				{
					Anchor                  = AnchorStyles.Bottom | AnchorStyles.Right,
					Location                = new Point(216, 377),
					Name                    = "btn_encode",
					Size                    = new Size(75, 23),
					TabIndex                = 2,
					Text                    = "Encode",
					UseVisualStyleBackColor = true,
				};
			var form = new Form
				{
					AutoScaleDimensions = new SizeF(6F, 13F),
					AutoScaleMode       = AutoScaleMode.Font,
					ClientSize          = new Size(384, 412),
					Name                = "Form1",
					Text                = "Base 64 Encode/Decode",
				};
			form.SuspendLayout();
			form.Controls.Add(btn_encode);
			form.Controls.Add(btn_decode);
			form.Controls.Add(edit);
			form.ResumeLayout(true);

			btn_decode.Click += (s,a) =>
				{
					try
					{
						var intext = edit.Text.ToCharArray();
						var bytes = Convert.FromBase64CharArray(intext, 0, intext.Length);
						var outtext = Encoding.UTF8.GetString(bytes);
						edit.Text = outtext;
					}
					catch (Exception ex)
					{
						MessageBox.Show(form, "Decode Failed:\r\n" + ex.Message, "Decode Failed");
					}
				};
			btn_encode.Click += (s,a) =>
				{
					try
					{
						var intext = edit.Text.ToCharArray();
						var bytes = Encoding.UTF8.GetBytes(intext);
						var outtext = Convert.ToBase64String(bytes, Base64FormattingOptions.InsertLineBreaks);
						edit.Text = outtext;
					}
					catch (Exception ex)
					{
						MessageBox.Show(form, "Encode Failed:\r\n" + ex.Message, "Encode Failed");
					}
				};

			form.ShowDialog();
			return 0;
		}
	}
}
