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
        /// Returns the first iterator i in the range [first, last) such that pred.Execute(i.Value) is true. 
        /// Returns last if no such iterator exists.
        /// </para>
        /// <para>
        /// The complexity is linear. At most last - first applications of Pred.
        /// </para>
        /// </summary>
        /// <param name="first">A forward iterator addressing the position 
        /// of the first element in the range.</param>
        /// <param name="last">A forward iterator addressing the position 
        /// behind the last element in the range.</param>
        /// <param name="pred">A binary predicate that specifies if two values are equal</param>
        /// <returns>An iterator that points to the Element or "last".</returns>
        public static InputIt
            FindIf<T, InputIt>(InputIt first, InputIt last, IUnaryFunction<T, bool> pred)
            where InputIt : IInputIterator<T>
        {
            // !!! tweak, as we cannot use traits to dispatch. A simple overload for 
            // !!! random access does not work, because the C# compiler fails in a lot
            // !!! of situations to infer the generic arguments!!
            IRandomAccessIterator<T> rndFirst = first as IRandomAccessIterator<T>;
            if (rndFirst != null)
                return (InputIt)FindIf(rndFirst, (IRandomAccessIterator<T>)last, pred);

            first = (InputIt)first.Clone();
            for (; !Equals(first, last); first.PreIncrement())
            {
                if (pred.Execute(first.Value))
                    break;
            }
            return first;
        }
        /// <summary>
        /// <para>
        /// Returns the first iterator i in the range [first, last] such that 
        /// Object.Equals(i.value, value) == true. Returns last if no such iterator exists.
        /// </para>
        /// <para>
        /// The complexity is linear. At most last - first comparisons for equality.
        /// </para>
        /// </summary>
        /// <param name="first">
        /// An input iterator pointing to the first element of the range.
        /// </param>
        /// <param name="last">
        /// An input iterator pointing one past the final element of the range.
        /// </param>
        /// <param name="val">The value to be searched for</param>
        /// <returns>
        /// An forward iterator addressing the first occurrence of the 
        /// specified value in the range
        /// </returns>
        public static InputIt
            Find<T, InputIt>(InputIt first, InputIt last, T val) where InputIt : IInputIterator<T>
        {
            return FindIf(first, last, Bind.Second(Compare.EqualTo<T>(), val));
        }

        private static IRandomAccessIterator<T>
            FindIf<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> last, IUnaryFunction<T, bool> predicate)
        {
            first = (IRandomAccessIterator<T>)first.Clone();
            int _Trip_count = (last.Diff(first)) >> 2;
            for (; _Trip_count > 0; --_Trip_count)
            {
                if (predicate.Execute(first.Value))
                    return first;
                first.PreIncrement();

                if (predicate.Execute(first.Value))
                    return first;
                first.PreIncrement();

                if (predicate.Execute(first.Value))
                    return first;
                first.PreIncrement();

                if (predicate.Execute(first.Value))
                    return first;
                first.PreIncrement();
            }

            switch (last.Diff(first))
            {
                case 3:
                    if (predicate.Execute(first.Value))
                        return first;
                    first.PreIncrement();
                    goto case 2;
                case 2:
                    if (predicate.Execute(first.Value))
                        return first;
                    first.PreIncrement();
                    goto case 1;
                case 1:
                    if (predicate.Execute(first.Value))
                        return first;
                    first.PreIncrement();
                    goto case 0;
                case 0:
                    goto default;
                default:
                    return last;
            }
        }

    }
}
    
