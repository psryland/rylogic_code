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
using System.Collections;
using System.Collections.Generic;

namespace NStl.SyntaxHelper
{
    /// <summary>
    /// Syntax helper to access all comparison related functors of the NSTL.
    /// </summary>
    public static class Compare
    {
        /// <summary>
        /// Returns a functor that compares two objects by 
        /// calling <see cref="object.Equals(object, object)"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IBinaryPredicate<T, T> EqualTo<T>()
        {
            return new EqualTo<T, T>();
        }
        /// <summary>
        /// Returns a functor that compares two objects. It is expected that 
        /// the objects to be compared implement the IComparable interface.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IBinaryPredicate<T, T> NotEqualTo<T>()
            where T : IComparable<T>
        {
            return new NotEqualTo<T>();
        }
        /// <summary>
        /// Returns a functor that compares two objects. It is expected that 
        /// the objects to be compared implement the IComparable interface.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IBinaryPredicate<T, T> Less<T>()
            where T : IComparable<T>
        {
            return new Less<T>();
        }
        /// <summary>
        /// Returns a functor that compares two objects. It is expected that 
        /// the objects to be compared implement the IComparable interface.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IBinaryPredicate<T, T> LessEqual<T>()
            where T : IComparable<T>
        {
            return new LessEqual<T>();
        }
        /// <summary>
        /// Returns a functor that compares two objects. It is expected that 
        /// the objects to be compared implement the IComparable interface.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IBinaryPredicate<T, T> Greater<T>()
            where T : IComparable<T>
        {
            return new Greater<T>();
        }
        /// <summary>
        /// Returns a functor that compares two objects. It is expected that 
        /// the objects to be compared implement the IComparable interface.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IBinaryPredicate<T, T> GreaterEqual<T>()
            where T : IComparable<T>
        {
            return new GreaterEqual<T>();
        }

        /// <summary>
        /// Returns a functor which provides the possibility to use an <see cref="IComparer"/>
        /// implementations inside stl.net algorithms. 
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="action"></param>
        /// <param name="comparison"></param>
        /// <returns></returns>
        public static IBinaryPredicate<U, V>
            With<U, V>(CompareAction action, IComparer comparison)
        {
            return new DefaultComparer<U, V>(action, comparison);
        }
        /// <summary>
        /// Returns a functor which provides the possibility to use an <see cref="IComparer{T}"/>
        /// implementations inside NStl algorithms. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="comparison"></param>
        /// <returns></returns>
        public static IBinaryPredicate<T, T>
            With<T>(CompareAction action, IComparer<T> comparison)
        {
            return new DefaultComparerT<T>(action, comparison);
        }        
    }
}
