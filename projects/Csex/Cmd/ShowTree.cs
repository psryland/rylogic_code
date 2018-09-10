using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Gui.WinForms;

namespace Csex
{
	public class ShowTree :Cmd
	{
		private string m_infile;

		/// <summary>Display help information in the case of an invalid command line</summary>
		public override void ShowHelp(Exception ex)
		{
			if (ex != null) Console.WriteLine("Error parsing command line: {0}", ex.Message);
			Console.Write(
				"Display a text file as a tree\n" +
				" Syntax: Csex -ShowTree -f filepath\n"
				);
		}

		/// <summary>Handle a command line option.</summary>
		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option)
			{
			default: return base.CmdLineOption(option, args, ref arg);
			case "-showtree": return true;
			case "-f": m_infile = args[arg++]; return true;
			}
		}

		/// <summary>Return true if all required options have been given</summary>
		public override bool OptionsValid()
		{
			return m_infile.HasValue();
		}

		class Node
		{
			public string m_value;
			public readonly int m_indent;
			public readonly List<Node> m_children;
			public readonly Node m_parent;
			public Node(string value, int indent, Node parent = null)
			{
				m_value = value;
				m_indent = indent;
				m_children = new List<Node>();
				m_parent = parent;
			}
		}

		/// <summary>Run the command</summary>
		public override int Run()
		{
			var root = new Node("root", -1);
			var current = root;
			using (var fs = new StreamReader(m_infile))
			{
				for (string line; (line = fs.ReadLine()) != null;)
				{
					var indent = line.Count(char.IsWhiteSpace);
					while (current.m_indent >= indent)
						current = current.m_parent;

					var child = new Node(line, indent, current);
					current.m_children.Add(child);
					current = child;
				}
			}

			var tree = new TreeGridView
				{
					Dock = DockStyle.Fill,
					AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
					ColumnHeadersVisible = false,
					RowHeadersVisible = false,
				};

			tree.Columns.Add(new TreeGridColumn());
			BuildTree(tree.Nodes, root);
			tree.ExpandAll();

			using (var form = new Form{FormBorderStyle = FormBorderStyle.Sizable, Size = new Size(1024,768)})
			{
				form.Controls.Add(tree);
				form.ShowDialog();
			}
			return 0;
		}

		private void BuildTree(TreeGridNodeCollection nodes,Node value)
		{
			var node = nodes.Add(value.m_value);
			foreach (var child in value.m_children)
				BuildTree(node.Nodes, child);
		}
	}
}
