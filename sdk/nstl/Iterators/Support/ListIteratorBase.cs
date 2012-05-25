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


using System;
using System.Collections.Generic;

namespace NStl.Iterators.Support
{
    /// <summary>
    /// Base class for all iterators that work for containser hat implement <see cref="IList{T}"/>.
    /// </summary>
    /// <typeparam name="Container"></typeparam>
    /// <typeparam name="T"></typeparam>
    public abstract class ListIteratorBase<Container, T> : RandomAccessIterator<T> where Container: IList<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="list"></param>
        /// <param name="idx"></param>
        internal protected ListIteratorBase(Container list, int idx)
        {
            this.list = list;
            this.idx = idx;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected override bool Equals(EquatableIterator<T> obj)
        {
            ListIteratorBase<Container, T> rhs = obj as ListIteratorBase<Container, T>;
            if (rhs == null)
                return false;

            return Equals(list, rhs.list) // equals on purpose. Could be an adapter, so referenceEquals is wrong
                && idx == rhs.idx;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override int HashCode()
        {
            return list.GetHashCode() ^ idx.GetHashCode();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IBidirectionalIterator<T> PreDecrement()
        {
            --idx;
            return this;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IForwardIterator<T> PreIncrement()
        {
            ++idx;
            return this;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public override int Diff(IRandomAccessIterator<T> rhs)
        {
            return idx - ((ListIteratorBase<Container, T>)rhs).idx;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public override bool Less(IRandomAccessIterator<T> rhs)
        {
            return idx < ((ListIteratorBase<Container, T>)rhs).idx;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public override IRandomAccessIterator<T> Add(int count)
        {
            ListIteratorBase<Container, T> i = (ListIteratorBase<Container, T>)Clone();
            i.idx += count;
            return i;
        }
        /// <summary>
        /// 
        /// </summary>
        public override T Value
        {
            get{ return list[idx]; }
            set{ list[idx] = value;}
        }
        internal int Index { get { return idx; } }
        internal Container List { get { return list; } }
        private readonly Container list;
        private int idx;
    }
}
