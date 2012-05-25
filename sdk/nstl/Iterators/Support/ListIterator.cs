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


using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NStl.Linq;

namespace NStl.Iterators.Support
{
    /// <summary>
    /// A random access iterator adapter implememtation for the <see cref="IList"/>
    /// and <see cref="IList{T}"/> interfaces.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1501:AvoidExcessiveInheritance", Justification = "This is an extending implementation inheritance!")]
    public class ListIterator<T> : ListIteratorBase<IList<T>, T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="list"></param>
        /// <param name="idx"></param>
        internal protected ListIterator(IList<T> list, int idx)
            : base(list, idx)
        {}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="list"></param>
        /// <param name="idx"></param>
        internal protected ListIterator(IList list, int idx)
            : base(list.Cast<T>(), idx)
        { }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IIterator<T> Clone()
        {
            return new ListIterator<T>(List, Index);
        }
        #region operators
        /// <summary>
        /// 
        /// </summary>
        /// <param name="it"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Scope = "member", Justification = "Method Plus is already exist!")]
        public static ListIterator<T> operator -(ListIterator<T> it, int rhs)
        {
            return (ListIterator<T>)it.Add(-rhs);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Scope = "member", Justification = "non static Add() already exist")]
        public static ListIterator<T> operator +(ListIterator<T> lhs, int rhs)
        {
            return (ListIterator<T>)lhs.Add(rhs);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="it"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Scope = "member", Justification = "Pre/PostIncrement() already exist")]
        public static ListIterator<T> operator ++(ListIterator<T> it)
        {
            ListIterator<T> tmp = (ListIterator<T>)it.Clone();
            return (ListIterator<T>)tmp.PreIncrement();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="it"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Scope = "member", Justification = "Post/PreDecrement() already exist")]
        public static ListIterator<T> operator --(ListIterator<T> it)
        {
            ListIterator<T> tmp = (ListIterator<T>)it.Clone();
            return (ListIterator<T>)tmp.PreDecrement();
        }
        
        #endregion
    }
}
