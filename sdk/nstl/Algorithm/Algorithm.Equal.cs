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
using NStl.SyntaxHelper;

namespace NStl
{
    public static partial class Algorithm
    {
        /// <summary>
        /// <para>
        /// Equal returns true if the two ranges [first1, last1) and [first2, first2 + (last1 - first1)) 
        /// are identical when compared element-by-element, and otherwise returns false.
        /// </para>
        /// <para>
        /// The complexity is linear.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first1">
        /// An input iterator pointing to the first element of the first range.
        /// </param>
        /// <param name="last1">
        /// An input iterator pointing one past the final element of the first range.
        /// </param>
        /// <param name="first2">
        /// An input iterator pointing to the first element of the second range.
        /// </param>
        /// <param name="pred">
        /// The binary predicate that is used to compare two elements.
        /// </param>
        /// <returns>TRUE, if both ranges contain the same elements int the same order.</returns>
        public static bool
            Equal<T>(IInputIterator<T> first1, IInputIterator<T> last1, IInputIterator<T> first2, IBinaryFunction<T, T, bool> pred)
        {
            return (Equals((Mismatch(first1, last1, first2, pred).Key), last1));
        }
        /// <summary>
        /// <para>
        /// Equal returns true if the two ranges [first1, last1) and [first2, first2 + (last1 - first1)) 
        /// are identical when compared element-by-element, and otherwise returns false.
        /// </para>
        /// <para>
        /// The complexity is linear.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first1">
        /// An input iterator pointing to the first element of the first range.
        /// </param>
        /// <param name="last1">
        /// An input iterator pointing one past the final element of the first range.
        /// </param>
        /// <param name="first2">
        /// An input iterator pointing to the first element of the second range.
        /// </param>
        /// <returns>TRUE, if both ranges contain the same elements int the same order.</returns>
        public static bool
            Equal<T>(IInputIterator<T> first1, IInputIterator<T> last1, IInputIterator<T> first2)
        {
            return (Equals((Mismatch(first1, last1, first2, Compare.EqualTo<T>()).Key), last1));
        }
    }
}
