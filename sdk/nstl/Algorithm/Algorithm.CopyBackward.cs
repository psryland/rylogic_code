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
        /// <para>Copies the source range backwards into the destination range.</para>
        /// <para>The compelxity is linear. Exactly last - first assignments are performed.</para>
        /// <para>
        /// The order of assignments matters in the case where the input and output ranges overlap: 
        /// CopyBackward may not be used if result is in the range [first, last). That is, it may 
        /// not be used if the end of the output range overlaps with the input range, but it may 
        /// be used if the beginning of the output range overlaps with the input range; 
        /// <see cref="Copy{T,OutputIt}(IInputIterator{T},IInputIterator{T},OutputIt)"/> has opposite restrictions. 
        /// If the two ranges are completely nonoverlapping,  of course, then either algorithm may be used.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="OutIt"></typeparam>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <param name="destEnd"></param>
        /// <returns></returns>
        /// <remarks>
        /// DestEnd is an iterator that points to the end of the output range. This is highly unusual: 
        /// in all other STL algorithms that denote an output range by a single iterator, that 
        /// iterator points to the beginning of the range.
        /// </remarks>
        public static OutIt
            CopyBackward<T, OutIt>(IBidirectionalInputIterator<T> first, IBidirectionalInputIterator<T> last, OutIt destEnd)
            where OutIt : IBidirectionalIterator<T>
        {
            last = (IBidirectionalInputIterator<T>)last.Clone();
            destEnd = (OutIt)destEnd.Clone();
            while (!Equals(first, last))
                (destEnd.PreDecrement()).Value = (last.PreDecrement()).Value;
            return (destEnd);
        }
    }
}
