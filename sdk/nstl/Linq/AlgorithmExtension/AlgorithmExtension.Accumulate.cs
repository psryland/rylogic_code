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

namespace NStl.Linq.AlgorithmExtension
{
    ///<summary>
    /// Provides the NSTL algorithms as a set of static 
    /// (Shared in Visual Basic) extension methods for standard .NET and
    /// NSTL containers.
    ///</summary>
    public static partial class AlgorithmExtension
    {
        /// <summary>
        /// <para>
        /// Accumulate is a generalization of summation: 
        /// it computes the sum (or some other binary operation)
        /// of init and all of the elements in the input range. 
        /// </para>
        /// <para>
        /// The complexity is linear. Exactly range.Count() 
        /// invocations of the binary operation.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="range">The input range.</param>
        /// <param name="init">The initial value that every element is added to.</param>
        /// <param name="binaryFunction">
        /// A functor that combines two elements to one output.
        /// </param>
        /// <returns>
        /// The sum of all items in the range.
        /// </returns>
        /// <remarks>
        /// There are several reasons why it is important that accumulate 
        /// starts with the value init. One of the most basic is that this 
        /// allows Accumulate to have a well-defined result even 
        /// if [first, last) is an empty range: if it is empty, the 
        /// return value is init. If you want to find the sum of all 
        /// of the elements in [first, last), you can just pass 0 as init.
        /// </remarks>
        public static T
            Accumulate<T>(this IEnumerable<T> range, T init,
                          IBinaryFunction<T, T, T> binaryFunction)
        {
            return Accumulate(range, init, binaryFunction.Execute);
        }
        /// <summary>
        /// <para>
        /// Accumulate is a generalization of summation: 
        /// it computes the sum (or some other binary operation)
        /// of init and all of the elements in the input range. 
        /// </para>
        /// <para>
        /// The complexity is linear. Exactly range.Count() 
        /// invocations of the binary operation.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="range">The input range.</param>
        /// <param name="init">The initial value that every element is added to.</param>
        /// <param name="binaryFunction">
        /// A functor that combines two elements to one output.
        /// </param>
        /// <returns>
        /// The sum of all items in the range.
        /// </returns>
        /// <remarks>
        /// There are several reasons why it is important that accumulate 
        /// starts with the value init. One of the most basic is that this 
        /// allows Accumulate to have a well-defined result even 
        /// if [first, last) is an empty range: if it is empty, the 
        /// return value is init. If you want to find the sum of all 
        /// of the elements in [first, last), you can just pass 0 as init.
        /// </remarks>
        public static T
            Accumulate<T>(this IEnumerable<T> range, T init,
                          Func<T, T, T> binaryFunction)
        {
            foreach (T value in range)
                init = binaryFunction(init, value);
            return init;
        }
    }
}
