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
using NStl.Iterators;
using NStl.SyntaxHelper;

namespace NStl
{
    public static partial class Algorithm
    {
        /// <summary>
        /// <para>
        /// Mismatch finds the first position where the two ranges [first1, last1) and [first2, first2 + (last1 - first1)) 
        /// differ by using a binary predicate.
        /// </para>
        /// <para>
        /// The complexity is linear. At most last1 - first1 comparisons.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="InIt1"></typeparam>
        /// <typeparam name="InIt2"></typeparam>
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
        /// <returns>
        /// A pair that contains the iterators pointing to the mismatching position, or a pair
        /// of end iterators if the ranges contain the same elements in the same order.
        /// </returns>
        public static KeyValuePair<InIt1, InIt2>
            Mismatch<T, InIt1, InIt2>(InIt1 first1, InIt1 last1, InIt2 first2, IBinaryFunction<T, T, bool> pred)
            where InIt1 : IInputIterator<T>
            where InIt2 : IInputIterator<T>
        {
            first1 = (InIt1)first1.Clone();
            first2 = (InIt2)first2.Clone();
            for (; !Equals(first1, last1) && pred.Execute(first1.Value, first2.Value); )
            {
                first1.PreIncrement();
                first2.PreIncrement();
            }
            return new KeyValuePair<InIt1, InIt2>(first1, first2);
        }
        /// <summary>
        /// <para>
        /// Mismatch finds the first position where the two ranges [first1, last1) and [first2, first2 + (last1 - first1)) 
        /// differ by using <see cref="object.Equals(object , object )"/>.
        /// </para>
        /// <para>
        /// The complexity is linear. At most last1 - first1 comparisons.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="FwdIt1"></typeparam>
        /// <typeparam name="FwdIt2"></typeparam>
        /// <param name="first1">
        /// An input iterator pointing to the first element of the first range.
        /// </param>
        /// <param name="last1">
        /// An input iterator pointing one past the final element of the first range.
        /// </param>
        /// <param name="first2">
        /// An input iterator pointing to the first element of the second range.
        /// </param>
        /// <returns>
        /// A pair that contains the iterators pointing to the mismatching position, or a pair
        /// of end iterators if the ranges contain the same elements in the same order.
        /// </returns>
        /// <remarks>
        /// Due to weak argument inference of C# you must specify the full generic argument list
        /// for this overload. It is recomanded to use <see cref="Mismatch{T, InIt1, InIt2}(InIt1, InIt1, InIt2, IBinaryFunction{T,T,bool})"/>
        /// instead using <see cref="Compare.EqualTo{T}()"/>, as the type inference will work here.
        /// </remarks>
        public static KeyValuePair<FwdIt1, FwdIt2>
            Mismatch<T, FwdIt1, FwdIt2>(FwdIt1 first1, FwdIt1 last1, FwdIt2 first2)
            where FwdIt1 : IInputIterator<T>
            where FwdIt2 : IInputIterator<T>
        {
            return Mismatch(first1, last1, first2, Compare.EqualTo<T>());
        }

    }
}
