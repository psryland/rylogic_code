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
        /// This algorithm searches for two adjacent elements that fullfill a
        /// condition specified by a binary predicate.
        /// </para>
        /// <para>
        /// The compexity is linear.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="InIt"></typeparam>
        /// <param name="first">
        /// An input iterator pointing to the first element of the range.
        /// </param>
        /// <param name="last">
        /// An input iterator pointing one past the final element of the range.
        /// </param>
        /// <param name="predicate">
        /// The predicate that has to to be satisfied.
        /// </param>
        /// <returns>
        /// An iterator pointing to the first element of the adjacent pair.
        /// </returns>
        public static InIt
            AdjacentFind<T, InIt>(InIt first, InIt last,
                                  IBinaryFunction<T, T, bool> predicate) 
            where InIt : IInputIterator<T>
        {
            first = (InIt)first.Clone();
            for (InIt firstb; !Equals((firstb = (InIt)first.Clone()), last) && !Equals(first.PreIncrement(), last); )
                if (predicate.Execute(firstb.Value, first.Value))
                    return (firstb);
            return (last);
        }
        /// <summary>
        /// <para>
        /// This algorithm searches for two adjacent elements that are equal.
        /// </para>
        /// <para>
        /// The compexity is linear.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="InIt"></typeparam>
        /// <param name="first">
        /// An input iterator pointing to the first element of the range.
        /// </param>
        /// <param name="last">
        /// An input iterator pointing one past the final element of the range.
        /// </param>
        /// <returns>
        /// An iterator pointing to the first element of the adjacent pair.
        /// </returns>
        /// <remarks>
        /// When using this overload you are forced to fully qualify all generic 
        /// arguments due to a lack in the C#'s compiler parameter inference.
        /// </remarks>
        public static InIt
            AdjacentFind<T, InIt>(InIt first, InIt last) where InIt : IInputIterator<T>
        {
            return AdjacentFind(first, last, Compare.EqualTo<T>());
        }
    }
}
