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


using System.Collections.Generic;
using System.Diagnostics;
using NStl.Exceptions;
using System.Collections;
using System.Linq;

namespace NStl.Iterators.Support
{
    /// <summary>
    /// Iterator wrapper for the .NET <see cref="IEnumerable{T}"/> interface. 
    /// </summary>
    /// <remarks>
    /// This is a very thin wrapper and should be used with care and for single pass algorithms only.
    /// </remarks>
    public class EnumerableIterator<T> : InputIterator<T>
    {
        private readonly IEnumerable<T> enumerable;
        private readonly IEnumerator<T> enumerator;
        private bool isEnd = false;
        private int currentPosition = -1;
       
        /// <summary></summary>
        internal EnumerableIterator(IEnumerable<T> enumerable)
        {
            this.enumerable = enumerable;
            enumerator = enumerable.GetEnumerator();
            MoveAhead();
        }
        internal EnumerableIterator(IEnumerable enumerable)
            : this(enumerable.Cast<T>())
        {
        }

        /// <summary>Creates an end iterator!</summary>
        internal EnumerableIterator(IEnumerable<T> enumerable, bool end)
            :this(enumerable)
        {
            Debug.Assert(end);
            isEnd = true;
        }
        internal EnumerableIterator(IEnumerable enumerable, bool end)
            :this(enumerable.Cast<T>(), end)
        {}
        private void MoveAhead()
        {
            if (isEnd)
                return;
            isEnd = !enumerator.MoveNext();
            ++currentPosition;
        }
        /// <summary>
        /// See <see cref="IInputIterator{T}.PreIncrement()"/> for details.
        /// </summary>
        /// <returns></returns>
        public override IInputIterator<T> PreIncrement()
        {
            MoveAhead();
            return this;
        }

        /// <summary>
        /// See <see cref="IInputIterator{T}.Value"/> for details.
        /// </summary>
        public override T Value
        {
            get
            {
                if (isEnd)
                    throw new DereferenceEndIteratorException();
                return enumerator.Current;
            }
        }
        /// <summary>
        /// See <see cref="IIterator{T}.Clone()"/> for details.
        /// </summary>
        /// <returns></returns>
        public override IIterator<T> Clone()
        {
            EnumerableIterator<T> newIt = new EnumerableIterator<T>(enumerable);
            for (int i = 0; i < currentPosition; ++i)
                newIt.MoveAhead();
            newIt.isEnd = isEnd;
            return newIt;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected override bool Equals(EquatableIterator<T> obj)
        {
            EnumerableIterator<T> rhs = obj as EnumerableIterator<T>;
            if (rhs == null)
                return false;

            if (isEnd && rhs.isEnd)
                return true;
            if (isEnd || rhs.isEnd)
                return false;

            return enumerable == rhs.enumerable
                   && currentPosition == rhs.currentPosition;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override int HashCode()
        {
            return enumerable.GetHashCode() ^ currentPosition.GetHashCode();
        }
    }
}
