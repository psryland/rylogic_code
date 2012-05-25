#region Copyright (c) 2003 - 2008, Andreas Mueller
/////////////////////////////////////////////////////////////////////////////////////////
// 
// Copyright (c) 2003 - 2008, Andreas Mueller.
// All rights reserved. This program and the accompanying materials
// are made available under the terms of the Eclipse Public License v1.0
// which accompanies this distribution, and is available at
// http://www.eclipse.org/legal/epl-v10.html
//
// Contributors:
//    Andreas Mueller - initial API and implementation
//
// 
// This software is derived from software bearing the following
// restrictions:
// 
// Copyright (c) 1994
// Hewlett-Packard Company
// 
// Permission to use, copy, modify, distribute and sell this software
// and its documentation for any purpose is hereby granted without fee,
// provided that the above copyright notice appear in all copies and
// that both that copyright notice and this permission notice appear
// in supporting documentation.  Hewlett-Packard Company makes no
// representations about the suitability of this software for any
// purpose.  It is provided "as is" without express or implied warranty.
// 
// 
// Copyright (c) 1996,1997
// Silicon Graphics Computer Systems, Inc.
// 
// Permission to use, copy, modify, distribute and sell this software
// and its documentation for any purpose is hereby granted without fee,
// provided that the above copyright notice appear in all copies and
// that both that copyright notice and this permission notice appear
// in supporting documentation.  Silicon Graphics makes no
// representations about the suitability of this software for any
// purpose.  It is provided "as is" without express or implied warranty.
// 
// 
// (C) Copyright Nicolai M. Josuttis 1999.
// Permission to copy, use, modify, sell and distribute this software
// is granted provided this copyright notice appears in all copies.
// This software is provided "as is" without express or implied
// warranty, and with no claim as to its suitability for any purpose.
// 
/////////////////////////////////////////////////////////////////////////////////////////
#endregion


using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
namespace NStl.Iterators.Support
{
    /// <summary>
    /// A forward iterator is expected to provide read/write access to the underlying
    /// values and it also provides the user with the possibility to move forward over 
    /// the range.
    /// </summary>
    [DebuggerDisplay("Value:[{Value}]")]
    public abstract class ForwardIterator<T> : EquatableIterator<T>, IForwardIterator<T>
    {
        /// <summary>
        /// Moves this iterator one ahead.
        /// </summary>
        /// <returns>This iterator.</returns>
        public abstract IForwardIterator<T> PreIncrement();

        /// <summary>
        /// Moves this iterator one ahead.
        /// </summary>
        /// <returns>A copy of this iterator at the old position.</returns>
        public virtual IForwardIterator<T> PostIncrement()
        {
            IForwardIterator<T> old = (IForwardIterator<T>)Clone();
            PreIncrement();
            return old;
        }
        /// <summary>
        /// The value that this iterator points to.
        /// </summary>
        public abstract T Value { get; set; }

        #region operators
        /// <summary>
        /// 
        /// </summary>
        /// <param name="it"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Scope = "member", Justification = "Pre/PostIncrement() already exist")]
        public static ForwardIterator<T> operator ++(ForwardIterator<T> it)
        {
            ForwardIterator<T> i = (ForwardIterator<T>) it.Clone();
            return (ForwardIterator<T>)i.PreIncrement();
        }
        #endregion

        #region input_iterator<T> Members

        T IInputIterator<T>.Value
        {
            get { return Value; }
        }

        #endregion
        #region movable_iterator<input_iterator<T>> Members

        IInputIterator<T> IInputIterator<T>.PreIncrement()
        {
            return PreIncrement();
        }

        IInputIterator<T> IInputIterator<T>.PostIncrement()
        {
            return PostIncrement();
        }

        #endregion
        #region output_iterator<T> Members

        T IOutputIterator<T>.Value
        {
            set { Value = value; }
        }

        #endregion
        #region movable_iterator<output_iterator<T>> Members

        IOutputIterator<T> IOutputIterator<T>.PreIncrement()
        {
            return PreIncrement();
        }

        IOutputIterator<T> IOutputIterator<T>.PostIncrement()
        {
            return PostIncrement();
        }

        #endregion

       
    }
}
