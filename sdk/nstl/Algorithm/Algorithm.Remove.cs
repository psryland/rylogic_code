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
    ///<summary></summary>
    public static partial class Algorithm
    {
        /// <summary>
        /// <para>
        /// RemoveIf removes from the range [first, last) every element x such that pred.Execute(x)
        /// is true. That is, RemoveIf returns an iterator new_last such that the range 
        /// [first, new_last) contains no elements for which pred is true. The iterators 
        /// in the range [new_last, last) are all still dereferenceable, but the elements 
        /// that they point to are unspecified. RemoveIf is stable, meaning that the relative 
        /// order of elements that are not removed is unchanged.
        /// </para>
        /// <para>
        /// The complexity is linear. RemoveIf performs exactly last - first applications of pred.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="InputIter"></typeparam>
        /// <param name="first">
        /// An input iterator pointing to the first element of the range.
        /// </param>
        /// <param name="last">
        /// An input iterator pointing one past the final element of the range.
        /// </param>
        /// <param name="pred">
        /// The predicate that decides if an element is removed from the range.
        /// </param>
        /// <returns>
        /// An iterator pointing one past the final element of the range that is 
        /// free of the removed values.
        /// </returns>
        /// <remarks>
        /// The meaning of "removal" is somewhat subtle. RemoveIf does not destroy any 
        /// iterators, and does not change the distance between first and last. (There's 
        /// no way that it could do anything of the sort.) So, for example, if V is a vector,
        /// RemoveIf(V.Begin(), V.End(), pred) does not change V.Size: V will contain 
        /// just as many elements as it did before. RemoveIf returns an iterator that points 
        /// to the end of the resulting range after elements have been removed from it; 
        /// it follows that the elements after that iterator are of no interest, and may 
        /// be discarded. If you are removing elements from a Sequence, you may simply erase 
        /// them. That is, a reasonable way of removing elements from a range is:
        /// <c>S.Erase(Algorithm.RemoveIf(S.Begin(), S.End(), pred), S.End())</c>
        /// </remarks>
        public static InputIter
            RemoveIf<T, InputIter>(InputIter first, InputIter last, IUnaryFunction<T, bool> pred)
            where InputIter : IForwardIterator<T>
        {
            first = FindIf(first, last, pred);
            if (Equals(first, last))
                return (first);
            else
            {
                IForwardIterator<T> first1 = (IForwardIterator<T>)first.Clone();
                return RemoveCopyIf(first1.PreIncrement(), last, first, pred);
            }
        }
        /// <summary>
        /// <para>
        /// Remove removes from the range [first, last) all elements that are equal to value.
        /// That is, remove returns an iterator new_last such that the range [first, new_last)
        /// contains no elements equal to value. The iterators in the 
        /// range [new_last, last) are all still dereferenceable, but the elements that 
        /// they point to are unspecified. Remove is stable, meaning that the relative order 
        /// of elements that are not equal to value is unchanged.
        /// </para>
        /// The complexity is linear. Remove performs exactly last - first comparisons for equality.
        /// <para>
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="InputIter"></typeparam>
        /// <param name="first">
        /// An input iterator pointing to the first element of the range.
        /// </param>
        /// <param name="last">
        /// An input iterator pointing one past the final element of the range.
        /// </param>
        /// <param name="val">he value to be removed.</param>
        /// <returns>
        /// An iterator pointing one past the final element of the range that is 
        /// free of the removed values.
        /// </returns>
        /// <remarks>
        /// The meaning of "removal" is somewhat subtle. Remove does not destroy any 
        /// iterators, and does not change the distance between first and last. (There's 
        /// no way that it could do anything of the sort.) So, for example, if V is a vector,
        /// RemoveIf(V.Begin(), V.End(), pred) does not change V.Size: V will contain 
        /// just as many elements as it did before. RemoveIf returns an iterator that points 
        /// to the end of the resulting range after elements have been removed from it; 
        /// it follows that the elements after that iterator are of no interest, and may 
        /// be discarded. If you are removing elements from a Sequence, you may simply erase 
        /// them. That is, a reasonable way of removing elements from a range is:
        /// <c>S.erase(Algorithm.RemoveIf(S.Begin(), S.End(), pred), S.End())</c>
        /// </remarks>
        public static InputIter
            Remove<T, InputIter>(InputIter first, InputIter last, T val) 
            where InputIter : IForwardIterator<T>
        {
            return RemoveIf(first, last, Bind.Second(Compare.EqualTo<T>(), val));
        }
    }
}
