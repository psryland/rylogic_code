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

using NStl.Iterators;
using System.Collections.Generic;
using System.Collections;
using NStl.Debugging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using NStl.Linq;

namespace NStl.Collections
{
    /// <summary>
    /// A simple class that represents a range bounded by
    /// two iterators.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="Iter"></typeparam>
    [DebuggerDisplay("Dimension:[{Count}]")]
    [DebuggerTypeProxy(typeof(CollectionDebuggerProxy))]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type")]
    public sealed class Range<T, Iter> : ICollection, IRange<T> where Iter : IInputIterator<T>
    {
        private readonly Iter first;
        private readonly Iter last;
        /// <summary>
        /// Constructs a range bounded by two iterators.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="last"></param>
        public Range(Iter first, Iter last)
        {
            this.first = (Iter)first.Clone();
            this.last  = (Iter)last.Clone();
        }
        /// <summary>
        /// An <see cref="IInputIterator{T}"/> implementation pointing 
        /// to the first element of the range.
        /// </summary>
        /// <returns></returns>
        public Iter Begin()
        {
            return (Iter)first.Clone();
        }
        /// <summary>
        /// An <see cref="IInputIterator{T}"/> implementation pointing 
        /// one past the final element of the range.
        /// </summary>
        /// <returns></returns>
        public Iter End()
        {
            return (Iter)last.Clone();
        }
        /// <summary>
        /// Returns FALSE if the collection has any content.
        /// </summary>
        public bool Empty
        {
            get { return Equals(first, last); }
        }
        #region IEnumerable<T> Members

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return NStlUtil.Enumerator(first, last);
        }

        #endregion
        #region IEnumerable Members


        IEnumerator IEnumerable.GetEnumerator()
        {
            return NStlUtil.Enumerator(first, last);
        }

        #endregion
        #region ICollection Members

        void ICollection.CopyTo(System.Array array, int index)
        {
            foreach(T o in this)
                array.SetValue(o, index++);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        public void CopyTo(T[] array, int index)
        {
            foreach (T o in this)
                array[index++] = o;
        }

        int ICollection.Count
        {
            get { return Begin().DistanceTo(End()); }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { return this; }
        }

        #endregion
        #region IRange<T> Members

        IInputIterator<T> IRange<T>.Begin()
        {
            return Begin();
        }

        IInputIterator<T> IRange<T>.End()
        {
            return End();
        }

        #endregion
    }
}
