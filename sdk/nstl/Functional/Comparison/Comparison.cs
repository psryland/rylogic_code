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

namespace NStl
{
    /// <summary>
    /// Compares two objects. It is expected that the objects 
    /// implement the IComparable interface.
    /// </summary>
    [Serializable]
    internal class EqualTo<T, U> : IBinaryPredicate<T, U>
    {
        /// <summary>
        /// Compares two objects using the IComparable interface
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        bool IBinaryFunction<T, U, bool>.Execute(T lhs, U rhs)
        { return Equals(lhs, rhs); }
    }
    [Serializable]
    internal class NotEqualTo<T> : IBinaryPredicate<T, T>
        where T : IComparable<T>
    {
        /// <summary>
        /// Compares two objects using the IComparable interface
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        bool IBinaryFunction<T, T, bool>.Execute(T lhs, T rhs)
        { return lhs.CompareTo(rhs) != 0; }
    }
    /// <summary>
    /// Compares two objects. It is expected that the objects 
    /// implement the IComparable interface.
    /// </summary>
    [Serializable]
    internal class Less<T> : IBinaryPredicate<T, T>
        where T : IComparable<T>
    {
        /// <summary>
        /// Compares two objects using the IComparable interface
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        bool IBinaryFunction<T, T, bool>.Execute(T lhs, T rhs)
        { return lhs.CompareTo(rhs) < 0; }
    }
    [Serializable]
    internal class LessEqual<T> : IBinaryPredicate<T, T>
        where T : IComparable<T>
    {
        /// <summary>
        /// Compares two objects using the IComparable interface
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        bool IBinaryFunction<T, T, bool>.Execute(T lhs, T rhs)
        { return lhs.CompareTo(rhs) <= 0; }
    }
    /// <summary>
    /// Compares two objects. It is expected that the objects 
    /// implement the IComparable interface.
    /// </summary>
    [Serializable]
    internal class Greater<T> : IBinaryPredicate<T, T>
        where T : IComparable<T>
    {
        /// <summary>
        /// Compares two objects using the IComparable interface
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        bool IBinaryFunction<T, T, bool>.Execute(T lhs, T rhs)
        { return lhs.CompareTo(rhs) > 0; }
    }
    [Serializable]
    internal class GreaterEqual<T> : IBinaryPredicate<T, T>
        where T : IComparable<T>
    {
        /// <summary>
        /// Compares two objects using the IComparable interface
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        bool IBinaryFunction<T, T, bool>.Execute(T lhs, T rhs)
        { return lhs.CompareTo(rhs) >= 0; }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>Might look duplicate to DefaultComparer, but is not. IComparer
    /// can compare two types of objects!!</remarks>
    [Serializable]
    internal class DefaultComparerT<T> : IBinaryPredicate<T, T>
    {
        /// <summary>
        /// ctor, takes a IComparer and a enum saying how to interpret the IComparer
        /// </summary>
        /// <param name="obj">Specifies what to do, e.g. compare as "less"</param>
        /// <param name="comparison">An IComparer implementation</param>
        public DefaultComparerT(CompareAction obj, IComparer<T> comparison)
        {
            compareAction = obj;
            comparer = comparison;
        }
        bool IBinaryFunction<T, T, bool>.Execute(T lhs, T rhs)
        {
            switch (compareAction)
            {
                case CompareAction.Less:
                    return comparer.Compare(lhs, rhs) < 0;
                case CompareAction.LessEqual:
                    return comparer.Compare(lhs, rhs) <= 0;
                case CompareAction.Equal:
                    return comparer.Compare(lhs, rhs) == 0;
                case CompareAction.GreaterEqual:
                    return comparer.Compare(lhs, rhs) >= 0;
                case CompareAction.Greater:
                    return comparer.Compare(lhs, rhs) > 0;
                case CompareAction.NotEqual:
                    return comparer.Compare(lhs, rhs) != 0;
                default:
                    throw new InvalidOperationException("Unknown CompareAction!");
            }
        }

        private readonly CompareAction compareAction;
        private readonly IComparer<T> comparer;
    }
    /// <summary>
    /// This object provides the possibility to use custom IComparer
    /// implementations in stl.net algorithms instead of the provided
    /// comparing functors that need IComparable to be implemented by all passed
    /// in objects. It is also intended to serve as an adaptor to "old" code and
    /// ICompare implementations.
    /// </summary>
    /// <remarks>This is not a original STL functor, but a useful extension for the .NET world</remarks>
    [Serializable]
    internal class DefaultComparer<T, V> : IBinaryPredicate<T, V>
    {
        /// <summary>
        /// ctor, takes a IComparer and a enum saying how to interpret the IComparer
        /// </summary>
        /// <param name="obj">Specifies what to do, e.g. compare as "less"</param>
        /// <param name="comparison">An IComparer implementation</param>
        public DefaultComparer(CompareAction obj, IComparer comparison)
        {
            compareAction = obj;
            comparer = comparison;
        }
        bool IBinaryFunction<T, V, bool>.Execute(T lhs, V rhs)
        {
            switch (compareAction)
            {
                case CompareAction.Less:
                    return comparer.Compare(lhs, rhs) < 0;
                case CompareAction.LessEqual:
                    return comparer.Compare(lhs, rhs) <= 0;
                case CompareAction.Equal:
                    return comparer.Compare(lhs, rhs) == 0;
                case CompareAction.GreaterEqual:
                    return comparer.Compare(lhs, rhs) >= 0;
                case CompareAction.Greater:
                    return comparer.Compare(lhs, rhs) > 0;
                case CompareAction.NotEqual:
                    return comparer.Compare(lhs, rhs) != 0;
                default:
                    throw new InvalidOperationException("Unknown CompareAction!");
            }
        }

        private readonly CompareAction compareAction;
        private readonly IComparer comparer;
    }
}
