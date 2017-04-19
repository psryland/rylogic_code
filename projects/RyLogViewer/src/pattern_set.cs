using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;
using pr.common;
using pr.container;
using pr.extn;
using pr.gui;

namespace RyLogViewer
{
	public class PatternSet :IFeatureTreeItem
	{
		public PatternSet(string name = "Patterns")
		{
			Name = name;
			Allowed = true;
			Highlights = new PatternCollection<Highlight>("Highlighting Patterns");
			Filters    = new PatternCollection<Filter>   ("Filter Patterns");
			Transforms = new PatternCollection<Transform>("Transform Patterns");
			Actions    = new PatternCollection<ClkAction>("Action Patterns");
		}
		public PatternSet(XElement node)
			:this()
		{
			// Migrate old versions
			#region Upgrade PatternSet
			{
				// Add a null version if not found
				if (node.Element(XmlTag.Version) == null)
					node.Add(new XElement(XmlTag.Version, string.Empty));

				for (string version; (version = node.Element(XmlTag.Version).Value) != Constants.PatternSetVersion;)
				{
					switch (version)
					{
					default:
						throw new Exception("Version {0} Pattern Set is not supported".Fmt(version));
					case "v1.0":
						// Latest
						break;
					}
				}
			}
			#endregion

			var highlights_node = node.Element(XmlTag.Highlights);
			if (highlights_node != null)
				Highlights.AddRange(highlights_node.Elements(XmlTag.Highlight).Select(x => x.As<Highlight>()));

			var filters_node = node.Element(XmlTag.Filters);
			if (filters_node != null)
				Filters.AddRange(filters_node.Elements(XmlTag.Filter).Select(x => x.As<Filter>()));

			var transforms_node = node.Element(XmlTag.Transforms);
			if (transforms_node != null)
				Transforms.AddRange(transforms_node.Elements(XmlTag.Transform).Select(x => x.As<Transform>()));

			var actions_node = node.Element(XmlTag.ClkActions);
			if (actions_node != null)
				Actions.AddRange(actions_node.Elements(XmlTag.ClkAction).Select(x => x.As<ClkAction>()));
		}
		public XElement ToXml(XElement node)
		{
			node.Add2(XmlTag.Version, Constants.PatternSetVersion, false);
			node.Add2(XmlTag.Highlights , XmlTag.Highlight , Highlights , false);
			node.Add2(XmlTag.Filters    , XmlTag.Filter    , Filters    , false);
			node.Add2(XmlTag.Transforms , XmlTag.Transform , Transforms , false);
			node.Add2(XmlTag.ClkActions , XmlTag.ClkAction , Actions    , false);
			return node;
		}

		/// <summary>Set name</summary>
		public string Name
		{
			get;
			set;
		}

		/// <summary>Feature tree 'Allowed' flag</summary>
		public bool Allowed
		{
			get;
			set;
		}

		/// <summary>Child elements</summary>
		IEnumerable<IFeatureTreeItem> IFeatureTreeItem.Children
		{
			get
			{
				yield return Highlights;
				yield return Filters;
				yield return Transforms;
				yield return Actions;
			}
		}

		/// <summary>The collection of highlighting patterns</summary>
		public PatternCollection<Highlight> Highlights
		{
			get { return m_highlights; }
			private set
			{
				if (m_highlights == value) return;
				if (m_highlights != null)
				{
					m_highlights.ListChanging -= HandleCollectionChanged;
				}
				m_highlights = value;
				if (m_highlights != null)
				{
					m_highlights.ListChanging += HandleCollectionChanged;
				}
			}
		}
		private PatternCollection<Highlight> m_highlights;

		/// <summary>The collection of filtering patterns</summary>
		public PatternCollection<Filter> Filters
		{
			get { return m_filters; }
			private set
			{
				if (m_filters == value) return;
				if (m_filters != null)
				{
					m_filters.ListChanging -= HandleCollectionChanged;
				}
				m_filters = value;
				if (m_filters != null)
				{
					m_filters.ListChanging += HandleCollectionChanged;
				}
			}
		}
		private PatternCollection<Filter> m_filters;

		/// <summary>The collection of transform patterns</summary>
		public PatternCollection<Transform> Transforms
		{
			get { return m_transforms; }
			private set
			{
				if (m_transforms == value) return;
				if (m_transforms != null)
				{
					m_transforms.ListChanging -= HandleCollectionChanged;
				}
				m_transforms = value;
				if (m_transforms != null)
				{
					m_transforms.ListChanging += HandleCollectionChanged;
				}
			}
		}
		private PatternCollection<Transform> m_transforms;

		/// <summary>The collection of transform patterns</summary>
		public PatternCollection<ClkAction> Actions
		{
			get { return m_actions; }
			private set
			{
				if (m_actions == value) return;
				if (m_actions != null)
				{
					m_actions.ListChanging -= HandleCollectionChanged;
				}
				m_actions = value;
				if (m_actions != null)
				{
					m_actions.ListChanging += HandleCollectionChanged;
				}
			}
		}
		private PatternCollection<ClkAction> m_actions;

		/// <summary>Raised when a pattern is added/removed</summary>
		public event EventHandler Changed;
		protected virtual void OnChanged()
		{
			Changed.Raise(this);
		}
		private void HandleCollectionChanged<T>(object sender, ListChgEventArgs<T> e)
		{
			if (e.ChangeType == ListChg.ItemAdded || e.ChangeType == ListChg.ItemRemoved)
				OnChanged();
		}

		/// <summary>Create a default collection of patterns</summary>
		public static PatternSet Default()
		{
			var ps = new PatternSet();
			#region Highlights
			{
				ps.Highlights.Add(new Highlight
				{
					Expr        = @"(Error:)|(E/)",
					PatnType    = EPattern.RegularExpression,
					ForeColour  = Color.FromArgb(0xff,0xff,0xff,0xff),
					BackColour  = Color.FromArgb(0xff,0x8b,0x00,0x00),
					IgnoreCase  = false,
					Invert      = false,
					WholeLine   = false,
					BinaryMatch = true,
					Active      = true,
				});
				ps.Highlights.Add(new Highlight
				{
					Expr        = @"(Warn:)|(W/)",
					PatnType    = EPattern.RegularExpression,
					ForeColour  = Color.FromArgb(0xff,0x00,0x00,0x00),
					BackColour  = Color.FromArgb(0xff,0xff,0xff,0x00),
					IgnoreCase  = false,
					Invert      = false,
					WholeLine   = false,
					BinaryMatch = true,
					Active      = true,
				});
				ps.Highlights.Add(new Highlight
				{
					Expr        = @"(Info:)|(I/)",
					PatnType    = EPattern.RegularExpression,
					ForeColour  = Color.FromArgb(0xff,0x00,0x00,0x00),
					BackColour  = Color.FromArgb(0xff,0xc4,0xff,0xff),
					IgnoreCase  = false,
					Invert      = false,
					WholeLine   = false,
					BinaryMatch = true,
					Active      = true,
				});
				ps.Highlights.Add(new Highlight
				{
					Expr        = @"#",
					PatnType    = EPattern.Substring,
					ForeColour  = Color.FromArgb(0xff,0x00,0x00,0x00),
					BackColour  = Color.FromArgb(0xff,0xc4,0xff,0xc7),
					IgnoreCase  = false,
					Invert      = false,
					WholeLine   = false,
					BinaryMatch = true,
					Active      = true,
				});
				ps.Highlights.Add(new Highlight
				{
					Expr = @"\w+\.txt",
					Active = true,
					PatnType = EPattern.RegularExpression,
					IgnoreCase = false,
					Invert = false,
					WholeLine = false,
					ForeColour = Color.FromArgb(0xFF,0x2A,0x00,0xFF),
					BackColour = Color.FromArgb(0xFF,0xB3,0xCD,0xF2),
					BinaryMatch = false,
				});
			}
			#endregion
			#region Filters
			{
				ps.Filters.Add(new Filter
				{
					Expr = @"##",
					Active = true,
					PatnType = EPattern.Substring,
					IgnoreCase = false,
					Invert = false,
					WholeLine = false,
					IfMatch = EIfMatch.Reject
				});
			}
			#endregion
			#region Transforms
			{
			}
			#endregion
			#region Actions
			{
				ps.Actions.Add(new ClkAction
				{
					Expr = @"\w+\.txt",
					Active = true,
					PatnType = EPattern.RegularExpression,
					IgnoreCase = false,
					Invert = false,
					WholeLine = false,
					Executable = @"C:\windows\notepad.exe",
					Arguments = @"{FilePath}",
					WorkingDirectory = string.Empty
				});
			}
			#endregion
			return ps;
		}

		/// <summary>Load a pattern set from file</summary>
		public static PatternSet Load(string filepath)
		{
			if (!Path_.FileExists(filepath))
				throw new Exception("Pattern set file '{0}' does not exist".Fmt(filepath));

			var root = XDocument.Load(filepath).Root;
			if (root == null)
				throw new Exception("Pattern set has no root xml node");

			return new PatternSet(root) { Name = Path_.FileTitle(filepath) };
		}

		/// <summary>Save the pattern set to a file</summary>
		public void Save(string filepath, SaveOptions options = SaveOptions.None)
		{
			var root = new XDocument(new XElement(XmlTag.Root)).Root;
			ToXml(root);
			root.Save(filepath, options);
		}

		/// <summary>Collection of patterns</summary>
		public class PatternCollection<T> :BindingListEx<T>, IFeatureTreeItem where T:IFeatureTreeItem
		{
			public PatternCollection(string name)
			{
				Name = name;
				Allowed = true;
			}

			/// <summary>Collection name</summary>
			public string Name
			{
				get;
				private set;
			}

			/// <summary>Feature tree allowed flag</summary>
			public bool Allowed
			{
				get;
				set;
			}

			/// <summary>Contained patterns</summary>
			IEnumerable<IFeatureTreeItem> IFeatureTreeItem.Children
			{
				get { return this.Cast<IFeatureTreeItem>(); }
			}
		}
	}
}
