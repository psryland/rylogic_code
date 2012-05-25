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
using NStl.Binder;

namespace NStl.SyntaxHelper
{
    /// <summary>
    /// Syntax helper that exposes all function binders.
    /// </summary>
    public static class Bind
    {
        /// <summary>
        /// A helper function that creates an adapter 
        /// to convert a unary function object into a nullary 
        /// function object by binding the argument of the 
        /// unary function to a specified value.
        /// </summary>
        /// <typeparam name="Param2Bind"></typeparam>
        /// <typeparam name="Result"></typeparam>
        /// <param name="func"></param>
        /// <param name="par"></param>
        /// <returns></returns>
        public static INullaryFunction<Result>
            Zeroth<Param2Bind, Result>(IUnaryFunction<Param2Bind, Result> func, Param2Bind par)
        {
            return new Binder0th<Param2Bind, Result>(func, par);
        }
        /// <summary>
        /// A helper function that creates a delegate 
        /// to convert a unary delegate into a nullary 
        /// delegate by binding the argument of the 
        /// unary delegate to a specified value.
        /// </summary>
        /// <typeparam name="Param2Bind"></typeparam>
        /// <typeparam name="Result"></typeparam>
        /// <param name="func"></param>
        /// <param name="par"></param>
        /// <returns></returns>
        public static Func<Result>
            Zeroth<Param2Bind, Result>(Func<Param2Bind, Result> func, Param2Bind par)
        {
            return delegate { return func(par); };
        }
        /// <summary>
        /// A helper function that creates an adapter 
        /// to convert a binary function object into a unary 
        /// function object by binding the first argument of the 
        /// binary function to a specified value.
        /// </summary>
        /// <param name="func">The binary function object that is adapted to a unary function by parameter binding</param>
        /// <param name="left">The parameter that is bound to the function.</param>
        /// <returns></returns>
        public static IUnaryFunction<Param2, Result>
            First<Param2Bind, Param2, Result>(IBinaryFunction<Param2Bind, Param2, Result> func, Param2Bind left)
        {
            return new Binder1st<Param2Bind, Param2, Result>(func, left);
        }
        /// <summary>
        /// A helper function that creates a delegate 
        /// to convert a binary delegate object into a unary 
        /// delegate by binding the first argument of the 
        /// binary delegate to a specified value.
        /// </summary>
        /// <param name="func">The binary delegate object that is adapted to a unary function by parameter binding</param>
        /// <param name="left">The parameter that is bound to the delegate.</param>
        /// <returns></returns>
        public static Func<Param2, Result>
            First<Param2Bind, Param2, Result>(Func<Param2Bind, Param2, Result> func, Param2Bind left)
        {
            return delegate(Param2 p2) { return func(left, p2); };
        }
        /// <summary>
        /// A helper function that creates an adapter 
        /// to convert a binary function object into a unary 
        /// function object by binding the first argument of the 
        /// binary function to a specified value.
        /// </summary>
        /// <param name="func">
        /// The binary function object that is adapted to a unary function by parameter binding.
        /// </param>
        /// <param name="left">The parameter that is bound to the function.</param>
        /// <returns></returns>
        public static IUnaryPredicate<Param2>
            First<Param2Bind, Param2>(IBinaryPredicate<Param2Bind, Param2> func, Param2Bind left)
        {
            return new Binder1stPred<Param2Bind, Param2>(func, left);
        }
        /// <summary>
        /// Creates an adapter to convert a binary function object into a unary 
        /// function object by binding the second argument of the 
        /// binary function to a specified value.
        /// </summary>
        /// <param name="func">
        /// The binary function object that is adapted to a unary function by parameter binding.
        /// </param>
        /// <param name="right">The parameter that is bound to the function.</param>
        /// <returns></returns>
        public static IUnaryFunction<Param1, Result>
            Second<Param1, Param2Bind, Result>(IBinaryFunction<Param1, Param2Bind, Result> func, Param2Bind right)
        {
            return new Binder2nd<Param1, Param2Bind, Result>(func, right);
        }
        /// <summary>
        /// Creates an adapter to convert a binary delegate object into a unary 
        /// delegate by binding the second argument of the 
        /// binary delegate to a specified value.
        /// </summary>
        /// <param name="func">
        /// The binary delegate object that is adapted to a unary delegate by parameter binding.
        /// </param>
        /// <param name="right">The parameter that is bound to the function.</param>
        /// <returns></returns>
        public static Func<Param1, Result>
            Second<Param1, Param2Bind, Result>(Func<Param1, Param2Bind, Result> func, Param2Bind right)
        {
            return delegate(Param1 p1) { return func(p1, right); };
        }
        /// <summary>
        /// Creates an adapter to convert a binary function object into a unary 
        /// function object by binding the second argument of the 
        /// binary function to a specified value.
        /// </summary>
        /// <typeparam name="Param1"></typeparam>
        /// <typeparam name="Param2Bind"></typeparam>
        /// <param name="func"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static IUnaryPredicate<Param1>
            Second<Param1, Param2Bind>(IBinaryPredicate<Param1, Param2Bind> func, Param2Bind right)
        {
            return new Binder2ndPred<Param1, Param2Bind>(func, right);
        }
    }
}
