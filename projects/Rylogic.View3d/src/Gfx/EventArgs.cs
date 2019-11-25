//#define PR_VIEW3D_CREATE_STACKTRACE
using System;
using System.ComponentModel;

namespace Rylogic.Gfx
{
	public sealed partial class View3d
	{
		public class AddFileProgressEventArgs :CancelEventArgs
		{
			public AddFileProgressEventArgs(Guid context_id, string filepath, long file_offset, bool complete)
			{
				ContextId = context_id;
				Filepath = filepath;
				FileOffset = file_offset;
				Complete = complete;
			}

			/// <summary>An anonymous pointer unique to each 'AddFile' call</summary>
			public Guid ContextId { get; }

			/// <summary>The file currently being parsed</summary>
			public string Filepath { get; }

			/// <summary>How far through the current file parsing is up to</summary>
			public long FileOffset { get; }

			/// <summary>Last progress update notification</summary>
			public bool Complete { get; }
		}

		public class SourcesChangedEventArgs :EventArgs
		{
			public SourcesChangedEventArgs(ESourcesChangedReason reason, bool before)
			{
				Reason = reason;
				Before = before;
			}

			/// <summary>The cause of the source changes</summary>
			public ESourcesChangedReason Reason { get; }

			/// <summary>True if files are about to change</summary>
			public bool Before { get; }
		}

		public class SceneChangedEventArgs :EventArgs
		{
			public SceneChangedEventArgs(View3DSceneChanged args)
			{
				ChangeType = args.ChangeType;
				ContextIds = args.ContextIds;
				Object = args.Object;
			}

			/// <summary>How the scene was changed</summary>
			public ESceneChanged ChangeType { get; }

			/// <summary>The context ids of the objects that were changed in the scene</summary>
			public Guid[] ContextIds { get; }

			/// <summary>The LdrObject involved in the change (single object changes only)</summary>
			public Object? Object { get; }
		}

		public class AnimationEventArgs :EventArgs
		{
			public AnimationEventArgs(EAnimCommand command, double clock)
			{
				Command = command;
				Clock = clock;
			}

			/// <summary>The state change for the update</summary>
			public EAnimCommand Command { get; }

			/// <summary>The current animation clock value</summary>
			public double Clock { get; }
		}

		public class SettingChangeEventArgs :EventArgs
		{
			public SettingChangeEventArgs(ESettings setting)
			{
				Setting = setting;
			}

			/// <summary>The setting that changed</summary>
			public ESettings Setting { get; }
		}
	}
}
