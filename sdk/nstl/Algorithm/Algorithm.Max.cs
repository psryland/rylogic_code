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
using NStl.SyntaxHelper;

namespace NStl
{
    public static partial class Algorithm
    {
        /// <summary>
        /// <para>
        /// Max returns the greater of its two arguments; it returns 
        /// the first argument if neither is greater than the other.
        /// </para>
        /// <para>
        /// For primitive types such as <see cref="float"/> or <see cref="sbyte"/>
        /// consider using the <see cref="Math"/> class. Prefer this implementation
        /// if you need to provide an external comparison implementation.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lhs">The first element to be compared.</param>
        /// <param name="rhs">The second element to be compared.</param>
        /// <param name="less">A functor that is used to determine if one element is less that another.</param>
        /// <returns>
        /// Returns the maximum element.
        /// </returns>
        public static T
            Max<T>(T lhs, T rhs, IBinaryFunction<T, T, bool> less)
        {
            return less.Execute(lhs, rhs) ? rhs : lhs;
        }
        /// <summary>
        /// <para>
        /// Max returns the greater of its two arguments; it returns 
        /// the first argument if neither is greater than the other.
        /// </para>
        /// <para>
        /// For primitive types such as <see cref="float"/> or <see cref="sbyte"/>
        /// consider using the <see cref="Math"/> class.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lhs">The first element to be compared.</param>
        /// <param name="rhs">The second element to be compared.</param>
        /// <returns>
        /// Returns the maximum element.
        /// </returns>
        public static T
            Max<T>(T lhs, T rhs) where T : IComparable<T>
        {
            return Max(lhs, rhs, Compare.Less<T>());
        }
    }
}
