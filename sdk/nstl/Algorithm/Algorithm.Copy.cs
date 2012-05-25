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
using NStl.Linq;

namespace NStl
{
    public static partial class Algorithm
    {
        /// <summary>
        /// <para>
        /// Copy copies elements from the range [first, last) to the 
        /// destination range. Assignments are performed in forward order.
        /// </para>
        /// The complexity is linear. Exactly last - first assignments are performed.
        /// <para>
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
        /// <param name="destination">
        /// An output iterator pointing to the first element of the destination range.
        /// </param>
        /// <returns>
        /// Returns an iterator pointing one past the last copied element in the 
        /// destination range.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Copy cannot be used to insert elements into an empty range: it overwrites 
        /// elements, rather than inserting elements. If you want to insert elements into 
        /// a range, you can use copy along with insertion adaptors that can be obtained
        /// from the <see cref="NStlUtil"/> helper class.
        /// </para>
        /// <para>
        /// The order of assignments matters in the case where the input and output ranges 
        /// overlap: copy may not be used if result is in the range [first, last). That 
        /// is, it may not be used if the beginning of the output range overlaps with the 
        /// input range, but it may be used if the end of the output range overlaps with 
        /// the input range; CopyBackward has opposite restrictions. If the two ranges 
        /// are completely nonoverlapping, of course, then either algorithm may be used. 
        /// </para>
        /// </remarks>
        public static OutputIt
            Copy<T, OutputIt>(IInputIterator<T> first, IInputIterator<T> last, OutputIt destination)
            where OutputIt : IOutputIterator<T>
        {
            return Copy(first.AsEnumerable(last), destination);
        }
        /// <summary>
        /// <para>
        /// Copy copies elements from the source range to the 
        /// destination range. Assignments are performed in forward order.
        /// </para>
        /// The complexity is linear. Exactly source.Count assignments are performed.
        /// <para>
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="OutputIt"></typeparam>
        /// <param name="source">The source range.</param>
        /// <param name="dest">
        /// An output iterator pointing to the first element of the destination range.
        /// </param>
        /// <returns>
        /// Returns an iterator pointing one past the last copied element in the 
        /// destination range.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Copy cannot be used to insert elements into an empty range: it overwrites 
        /// elements, rather than inserting elements. If you want to insert elements into 
        /// a range, you can use copy along with insertion adaptors that can be obtained
        /// from the <see cref="NStlUtil"/> helper class.
        /// </para>
        /// <para>
        /// The order of assignments matters in the case where the input and output ranges 
        /// overlap: copy may not be used if result is in the source range. That 
        /// is, it may not be used if the beginning of the output range overlaps with the 
        /// input range, but it may be used if the end of the output range overlaps with 
        /// the input range; CopyBackward has opposite restrictions. If the two ranges 
        /// are completely nonoverlapping, of course, then either algorithm may be used. 
        /// </para>
        /// </remarks>
        public static OutputIt
            Copy<T, OutputIt>(IEnumerable<T> source, OutputIt dest)
            where OutputIt : IOutputIterator<T>
        {
            dest = (OutputIt)dest.Clone();
            foreach (T t in source)
            {
                dest.Value = t;
                dest.PreIncrement();
            }
            return dest;
        }
        /// <summary>
        /// <para>
        /// CopyIf copies elements from the range [first, last) to the 
        /// destination range if the predicate avaluates to true. 
        /// Assignments are performed in forward order.
        /// </para>
        /// <para>
        /// The complexity is linear. Al most last - first assignments are performed.
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
        /// The predicate that decides if a value is copied to the destination range.
        /// </param>
        /// <returns>
        /// Returns an iterator pointing one past the last copied element in the 
        /// destination range.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Copy cannot be used to insert elements into an empty range: it overwrites 
        /// elements, rather than inserting elements. If you want to insert elements into 
        /// a range, you can use copy along with insertion adaptors that can be obtained
        /// from the <see cref="NStlUtil"/> helper class.
        /// </para>
        /// <para>
        /// The order of assignments matters in the case where the input and output ranges 
        /// overlap: copy may not be used if result is in the range [first, last). That 
        /// is, it may not be used if the beginning of the output range overlaps with the 
        /// input range, but it may be used if the end of the output range overlaps with 
        /// the input range; copy_backward has opposite restrictions. If the two ranges 
        /// are completely nonoverlapping, of course, then either algorithm may be used. 
        /// </para>
        /// </remarks>
        public static OutputIt
            CopyIf<T, OutputIt>(IInputIterator<T> first, IInputIterator<T> last, OutputIt dest, IUnaryFunction<T, bool> pred)
                                where OutputIt : IOutputIterator<T>
        {
            // make sure that we don't change the iterators that
            // are passed in
            first = (IInputIterator<T>)first.Clone();
            dest = (OutputIt)dest.Clone();
            for (; !Equals(first, last); first.PreIncrement())
            {
                if (pred.Execute(first.Value))
                {
                    dest.Value = first.Value;
                    dest.PreIncrement();
                }
            }
            return dest;
        }
    }
}
