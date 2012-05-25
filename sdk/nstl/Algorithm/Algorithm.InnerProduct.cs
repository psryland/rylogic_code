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

namespace NStl
{
    public static partial class Algorithm
    {
        /// <summary>
        /// <para>
        /// InnerProduct returns init plus the inner product of the two ranges.
        /// That is, it first initializes the result to init and then, for each 
        /// iterator i in [first1, last1), in order from the beginning to the 
        /// end of the range, updates the result by 
        /// result = opPlus.Execute(result, opMultiply.Execute(first1.Value, first2.Value)).
        /// </para>
        /// <para>
        /// There are several reasons why it is important that InnerProduct starts with 
        /// the value init. One of the most basic is that this allows InnerProduct to 
        /// have a well-defined result even if [first1, last1) is an empty range: 
        /// if it is empty, the return value is init. The ordinary inner product 
        /// corresponds to setting init to 0.
        /// </para>
        /// <para>
        /// The complexity is linear. Exactly last1 - first1 applications of each binary operation.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first1">
        /// An <see cref="IInputIterator{T}"/> implementation pointing 
        /// to the first element of the first input range.
        /// </param>
        /// <param name="last1">
        /// An <see cref="IInputIterator{T}"/> implementation pointing 
        /// one past the final element of the first input range.
        /// </param>
        /// <param name="first2">
        /// An <see cref="IInputIterator{T}"/> implementation pointing 
        /// to the first element of the second input range.
        /// </param>
        /// <param name="init">The initial value.</param>
        /// <param name="opPlus">The functor that is used to add the result of opMultiply.</param>
        /// <param name="opMultiply">The functor that is used to multiply the corresponding elements of both ranges.</param>
        /// <returns></returns>
        /// <remarks>
        /// Neither binary operation is required to be either associative or 
        /// commutative: the order of all operations is specified.
        /// </remarks>
        public static T
           InnerProduct<T>(IInputIterator<T> first1, IInputIterator<T> last1,
                            IInputIterator<T> first2, T init,
                            IBinaryFunction<T, T, T> opPlus, IBinaryFunction<T, T, T> opMultiply)
        {
            first1 = (IInputIterator<T>)first1.Clone();
            first2 = (IInputIterator<T>)first2.Clone();
            for (; !Equals(first1, last1); first1.PreIncrement(), first2.PreIncrement())
                init = opPlus.Execute(init, opMultiply.Execute(first1.Value, first2.Value));
            return init;
        }
    }
}
