using System;
using System.Windows.Input;

namespace Rylogic.Gui.WPF
{
	/// <summary>Command base class for 'ICommand'</summary>
	public abstract class Command : ICommand
	{
		/// <summary>Can execute changed</summary>
		public event EventHandler CanExecuteChanged;
		protected void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

		/// <summary>True if the command is available</summary>
		public virtual bool CanExecute(object _) => true;

		/// <summary>Execute the command</summary>
		public virtual void Execute(object _) { }

		/// <summary>Construct a command from a delegate</summary>
		public static Command<TOwner> Create<TOwner>(TOwner owner, Action execute)
		{
			return new Command<TOwner>(owner, (o, p) => execute(), null);
		}
		public static Command<TOwner> Create<TOwner>(TOwner owner, Action<TOwner> execute)
		{
			return new Command<TOwner>(owner, (o, _) => execute(o), null);
		}
		public static Command<TOwner> Create<TOwner>(TOwner owner, Action<TOwner, object> execute)
		{
			return new Command<TOwner>(owner, (o, p) => execute(o,p), null);
		}
		public static Command<TOwner> Create<TOwner>(TOwner owner, Action<TOwner> execute, Func<TOwner, bool> can_execute)
		{
			return new Command<TOwner>(owner, (o, _) => execute(o), (o, _) => can_execute(o));
		}
	}

	/// <summary>Base class for WPF commands</summary>
	public class Command<TOwner> : Command
	{
		public Command(TOwner owner)
			: this(owner, null, null)
		{ }
		public Command(TOwner owner, Action<TOwner, object> execute, Func<TOwner, object, bool> can_execute)
		{
			Owner = owner;
			m_execute = execute;
			m_can_execute = can_execute;
		}

		/// <summary>The owner</summary>
		protected TOwner Owner { get; }

		/// <summary>True if the command is available</summary>
		public override bool CanExecute(object parameter) => m_can_execute?.Invoke(Owner, parameter) ?? true;
		private Func<TOwner, object, bool> m_can_execute;

		/// <summary>Execute the command</summary>
		public override void Execute(object parameter) => m_execute?.Invoke(Owner, parameter);
		private Action<TOwner, object> m_execute;
	}
}
