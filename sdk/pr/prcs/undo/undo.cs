//***************************************************
// Undo/Redo functionally
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;

namespace pr.undo
{
	// Base class for a single reversable change
	public abstract class Change
	{
		// Applies the reverse of the change and
		// returns a Change object that reverses this reverse.
		public abstract Change Reverse();

		// Helper method for common edits that just swap values
		protected static void Swap<T>(ref T a, ref T b) { T c = a; a = b; b = c; }
	}
	
	// A transaction is a collection of 'Changes' that can be cycled through
	// forwards and backwards.
	public class Transaction :Change ,IDisposable
	{
		// The list of reversable changes and our current position in the list
		// <  m_index = undo's
		// >= m_index = redo's
		private readonly List<Change> m_changes = new List<Change>();
		private int m_index = 0;
		
		// Transactions can be nested. Parent forms a linked list of nested transactions
		private Transaction Parent = null;

		// The current active transaction instance
		// Null means no instance is currently active
		public static Transaction Current = null;
		
		// Begin a new transaction
		private Transaction() {}
		public static Transaction Begin()
		{
			Transaction tx = new Transaction();
			tx.Parent = Current;
			Current = tx;
			return tx;
		}

		// End a transaction
		// Committing a transaction makes it a single change in the parent transaction
		public static void End()
		{
			if (Current == null) return;
			Current.TrimRedos();
			if (Current.m_changes.Count > 0 && Current.Parent != null) Current.Parent.Add(Current);
			Current = Current.Parent;
		}

		// Add a change to this transaction
		public void Add(Change change)
		{
			TrimRedos();
			m_changes.Add(change);
			++m_index;
		}

		// Reverse all changes in this transaction
		public override Change Reverse()
		{
			if (m_index > 0) Rollback();
			else			 Rollforward();
			return this;
		}

		// Move the index back to the first change, undo'ing along the way
		public void Rollback()
		{
			for (;m_index-- > 0;)
				m_changes[m_index].Reverse();
		}

		// Move the index to the last change, redo'ing along the way
		public void Rollforward()
		{
			for (; m_index < m_changes.Count; ++m_index)
				m_changes[m_index].Reverse();
		}

		// Remove all changes from the current index to the end of the buffer
		private void TrimRedos()
		{
			m_changes.RemoveRange(m_index, m_changes.Count - m_index);
		}

		// Unless 'End()' has been called, rollback all changes
		public void Dispose()
		{
			if (Current == Parent) return;
			Rollback();
		}
	}

	// Double buffered instance of 'T'
	public sealed class DblBuffer<T> :Change
    {
        public  T Current;
        private T Other;

        public DblBuffer(T current, T backup)
        {
            Current = current;
            Other = backup;
        }
        public override Change Reverse()
        {
            Swap(ref Current, ref Other);
			return this;
        }
    }

    /// <summary>
    /// A method that sets a property on an instance to a value. 
    /// </summary>
    /// <typeparam name="Owner">Type that has the property to set.</typeparam>
    /// <typeparam name="Type">Type to set</typeparam>
    /// <param name="instance">Instance to set property on.</param>
    /// <param name="value">Value to set property to.</param>
    public delegate void PropertySetter<Owner, Type>(Owner instance, Type value) where Owner : class;
    
    /// <summary>
    /// Represents a instance method that sets a given instance property to a given value. 
    /// </summary>
    /// <typeparam name="Type">Type of property to set</typeparam>
    /// <param name="value">Value to set property to.</param>
    public delegate void InstancePropertySetter<Type>(Type value);

	// Wrapper object for setting a property on an object
	// Use:
	//	Transaction.Current.Add(new PropertyChange(MethodName, object, old_value, new_value));
	public class PropertyChange<Owner, Type> :Change where Owner : class
	{
		private readonly PropertySetter<Owner, Type> m_setter;
		private readonly Owner m_instance;
		private Type m_old_value;
		private Type m_new_value;

        public PropertyChange(PropertySetter<Owner, Type> setter, Owner instance, Type old_value, Type new_value)
        {
            m_setter = setter;
            m_instance = instance;
            m_old_value = old_value;
            m_new_value = new_value;
        }
		public override Change Reverse()
		{
            m_setter(m_instance, m_old_value);
            Swap(ref m_old_value, ref m_new_value);
            return this;
        }
    }
	
	// Use:
	// Transaction.Current.Add(new InstancePropertyChange(delegate(int v) { Value = v }, old_value, new_value));
	public class InstancePropertyChange<Type> :Change
	{
	    private readonly InstancePropertySetter<Type> m_setter;
	    private Type m_old_value;
	    private Type m_new_value;

	    public InstancePropertyChange(InstancePropertySetter<Type> setter, Type old_value, Type new_value)
	    {
	        m_setter = setter;
	        m_old_value = old_value;
	        m_new_value = new_value;
	    }
	    public override Change Reverse()
	    {
	        m_setter(m_old_value);
	        Swap(ref m_old_value, ref m_new_value);
	        return this;
	    }
	}
	

	//// Wrap a type to make it reversable
	//// Adds 4 bytes to 'T' unless Re
	//public struct Reversible<T>
	//{
	//    private T m_value;
	//    private DblBuffer<T> m_dblbuf;

	//    public Reversible(T initialiser)
	//    {
	//        m_value = initialiser;
	//        m_dblbuf = null;
	//    }

	//    // Access the wrapped type via 'Value'
	//    public T Value
	//    {
	//        get { return m_dblbuf == null ? m_value : m_dblbuf.Current; }
	//        set
	//        {
	//            if (Transaction.Current != null)
	//            {
	//                if (m_dblbuf == null) m_dblbuf = new DblBuffer<T>(value, m_value);
	//            }

	//            Transaction txn = Transaction.Current;

	//            if (m_dblbuf == null)
	//            {
	//                if (txn != null)	{ m_dblbuf = new DblBuffer<T>(value, m_value); txn.Add(m_dblbuf); }
	//                else				{ m_value = value; }
	//            }
	//            else
	//            {
	//                if (txn != null)
	//                {
	//                    ReversibleStorageEdit<T> storageEdit = new ReversibleStorageEdit<T>(storage, storage.CurrentValue);
	//                    storage.CurrentValue = value;
	//                    txn.Add(storageEdit);
	//                }
	//                else
	//                {
	//                    m_dblbuf.Current = value;
	//                }
	//            }
	//        }
	//    }

	//    public override int GetHashCode()
	//    {
	//        object obj = Value;
	//        if (obj == null) return 0;
	//        return obj.GetHashCode();
	//    }
	//    public override string ToString()
	//    {
	//        object obj = Value;
	//        if (obj == null) return null;
	//        return obj.ToString();
	//    }
	//    public override bool Equals(object obj)
	//    {
	//        object obj2 = Value;
	//        if (obj2 == null) return false;
	//        return obj2.Equals(obj);
	//    }
	//}
}
