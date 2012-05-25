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
        /// RemoveCopyIf copies elements from the range [first, last) to a range beginning 
        /// at result, except that elements for which pred is true are not copied. The 
        /// return value is the end of the resulting range. This operation is stable, meaning 
        /// that the relative order of the elements that are copied is the same as in 
        /// the range [first, last).
        /// </para>
        /// <para>
        /// The complexity is linear. Exactly last - first applications of pred, and at 
        /// most last - first assignments.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="OutputIt"></typeparam>
        /// <param name="first">
        /// An input iterator pointing to the first element of the range.
        /// </param>
        /// <param name="last">
        /// An input iterator pointing one past the final element of the range.
        /// </param>
        /// <param name="dest">
        /// An output iterator pointing to the first element of the destination range.
        /// </param>
        /// <param name="pred">
        /// The preidcate that decides if a a value is removed and copied.
        /// </param>
        /// <returns>
        /// An output iterator pointing one past the final element of the destination range.
        /// </returns>
        public static OutputIt
            RemoveCopyIf<T, OutputIt>(IInputIterator<T> first, IInputIterator<T> last, OutputIt dest, IUnaryFunction<T, bool> pred)
                where OutputIt : IOutputIterator<T>
        {
            first = (IInputIterator<T>)first.Clone();
            dest = (OutputIt)dest.Clone();
            for ( ; !Equals(first, last); first.PreIncrement())
                if (!pred.Execute(first.Value))
                    (dest.PostIncrement()).Value = first.Value;
            return dest;
        }
        /// <summary>
        /// <para>
        /// RemoveCopy copies elements that are not equal to value from the range [first, last) 
        /// to a range beginning at result. The return value is the end of the resulting 
        /// range. This operation is stable, meaning that the relative order of the elements 
        /// that are copied is the same as in the range [first, last).
        /// </para>
        /// <para>
        /// The complexity is linear. Exactly last - first comparisons for equality, and at 
        /// most last - first assignments.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="OutputIt"></typeparam>
        /// <param name="first">
        /// An input iterator pointing to the first element of the range.
        /// </param>
        /// <param name="last">
        /// An input iterator pointing one past the final element of the range.
        /// </param>
        /// <param name="dest">
        /// An output iterator pointing to the first element of the destination range.
        /// </param>
        /// <param name="val">
        /// The value that will be removed from the source range and copied into the 
        /// destination range.
        /// </param>
        /// <returns>
        /// An output iterator pointing one past the final element of the destination range.
        /// </returns>
        public static OutputIt
            RemoveCopy<T, OutputIt>(IInputIterator<T> first, IInputIterator<T> last, OutputIt dest, T val)
                where OutputIt : IOutputIterator<T>
        {
            return RemoveCopyIf<T, OutputIt>(first, last, dest, Bind.Second(Compare.EqualTo<T>(), val));
        }
	}
}
