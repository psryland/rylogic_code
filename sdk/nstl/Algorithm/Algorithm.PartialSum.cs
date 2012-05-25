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
        /// PartialSum calculates a generalized partial sum: 
        /// first.Value is assigned to result.Value, 
        /// the sum of first.Value and (first + 1).Value is 
        /// assigned to (result + 1).Value, and so on.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="FwdIt"></typeparam>
        /// <param name="first">
        /// An <see cref="IInputIterator{T}"/> pointing to the first element of the i.
        /// </param>
        /// <param name="last"></param>
        /// <param name="result"></param>
        /// <param name="opAdd"></param>
        /// <returns>
        /// An <see cref="IOutputIterator{T}"/> pointing to the new end of the target range.
        /// </returns>
        public static FwdIt
            PartialSum<T, FwdIt>(IInputIterator<T> first, IInputIterator<T> last,
                                  FwdIt result, IBinaryFunction<T, T, T> opAdd)
            where FwdIt : IOutputIterator<T>
        {
            result = (FwdIt)result.Clone();
            if (Equals(first, last))
                return result;
            first = (IInputIterator<T>)first.Clone();
            result.Value = first.Value;
            T Value = first.Value;
            while (!Equals(first.PreIncrement(), last))
            {
                Value = opAdd.Execute(Value, first.Value);
                (result.PreIncrement()).Value = Value;
            }
            return (FwdIt)result.PostIncrement();
        }
    }
}
