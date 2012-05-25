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
        /// Transform aplies the unary function object to each object in the
        /// input range and assigns the result to the destination range.
        /// </para>
        /// <para>
        /// The complexity is linear. The operation is applied exactly 
        /// last - first times in the case of the unary version, or last1 - first1 
        /// in the case of the binary version.
        /// </para>
        /// </summary>
        /// <typeparam name="InType">
        /// The type of the input range.
        /// </typeparam>
        /// <typeparam name="OutType">
        /// The type of thy output range.
        /// </typeparam>
        /// <typeparam name="OutIt"></typeparam>
        /// <param name="first">
        /// An input iterator pointing to the first element of the input range.
        /// </param>
        /// <param name="last">
        /// An input iterator pointing one past the final element of the input range.
        /// </param>
        /// <param name="result">
        /// An output iterator pointing to the first element of the destination range.
        /// </param>
        /// <param name="func">
        /// The unary function that is applied to each object.
        /// </param>
        /// <returns>
        /// An iterator pointing one past the final element of the output range.
        /// </returns>
        public static OutIt
            Transform<InType, OutType, OutIt>(IInputIterator<InType> first, IInputIterator<InType> last,
                                   OutIt result, IUnaryFunction<InType, OutType> func)
            where OutIt : IOutputIterator<OutType>
        {
            first = (IInputIterator<InType>)first.Clone();
            result = (OutIt)result.Clone();
            for (; !Equals(first, last); first.PreIncrement(), result.PreIncrement())
                result.Value = func.Execute(first.Value);
            return result;
        }
        /// <summary>
        /// <para>
        /// Transform applies the binary fnction to a pair of elements from both 
        /// source ranges and assigns the result to the destination range.
        /// </para>
        /// <para>
        /// The complexity is linear. The operation is applied exactly 
        /// last - first times in the case of the unary version, or last1 - first1 
        /// in the case of the binary version.
        /// </para>
        /// </summary>
        /// <typeparam name="InType1">
        /// The type of the first input range.
        /// </typeparam>
        /// /// <typeparam name="InType2">
        /// The type of the second input range.
        /// </typeparam>
        /// <typeparam name="OutType">
        /// The type of thy output range.
        /// </typeparam>
        /// <typeparam name="OutIt"></typeparam>
        /// <param name="first1">
        /// An input iterator pointing to the first element of the first input range.
        /// </param>
        /// <param name="last1">
        /// An input iterator pointing one past the final element of the first input range.
        /// </param>
        /// <param name="first2">
        /// An input iterator pointing to the first element of the second input range.
        /// </param>
        /// <param name="result">
        /// An output iterator pointing to the first element of the destination range.
        /// </param>
        /// <param name="func">
        /// The binary function that is applied.
        /// </param>
        /// <returns>
        /// An iterator pointing one past the final element of the output range.
        /// </returns>
        public static OutIt
            Transform<InType1, InType2, OutType, OutIt>(IInputIterator<InType1> first1, IInputIterator<InType1> last1,
                                           IInputIterator<InType2> first2,
                                           OutIt result, IBinaryFunction<InType1, InType2, OutType> func)
            where OutIt : IOutputIterator<OutType>
        {
            first1 = (IInputIterator<InType1>)first1.Clone();
            first2 = (IInputIterator<InType2>)first2.Clone();
            result = (OutIt)result.Clone();
            for (; !Equals(first1, last1); first1.PreIncrement(), first2.PreIncrement(), result.PreIncrement())
                result.Value = func.Execute(first1.Value, first2.Value);
            return result;
        }
	}
}
