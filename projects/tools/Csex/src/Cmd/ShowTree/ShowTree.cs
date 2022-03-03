using System;
using Rylogic.Extn;

namespace Csex
{
	public class ShowTree :Cmd
	{
		private string m_infile = string.Empty;

		/// <inheritdoc/>
		public override void ShowHelp(Exception? ex)
		{
			if (ex != null) Console.WriteLine("Error parsing command line: {0}", ex.Message);
			Console.Write(
				"Display a text file as a tree\n" +
				" Syntax: Csex -ShowTree -f filepath\n"
				);
		}

		/// <inheritdoc/>
		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option)
			{
				case "-showtree": return true;
				case "-f": m_infile = args[arg++]; return true;
				default: return base.CmdLineOption(option, args, ref arg);
			}
		}

		/// <inheritdoc/>
		public override Exception? Validate()
		{
			return
				!m_infile.HasValue() ? new Exception("No in-filepath given") :
				null;
		}

		/// <inheritdoc/>
		public override int Run()
		{
			/*
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
			*/
			return 0;
		}

#if false
		private void BuildTree(TreeGridNodeCollection nodes,Node value)
		{
			var node = nodes.Add(value.m_value);
			foreach (var child in value.m_children)
				BuildTree(node.Nodes, child);
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

#endif
	}
}
