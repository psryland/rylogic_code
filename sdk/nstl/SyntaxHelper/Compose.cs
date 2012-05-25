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
using System.Diagnostics.CodeAnalysis;

namespace NStl.SyntaxHelper
{
    /// <summary>
    /// This syntax helper exposes all compose binders of the NSTL.
    /// </summary>
    public static class Compose
    {
        /// <summary>
        /// This method returns a functor that binds a nullary into a unary function.
        /// It can be seen as F(G()).
        /// </summary>
        /// <param name="f">The outer functor.</param>
        /// <param name="g">The inner functor.</param>
        /// <returns></returns>
        public static INullaryFunction<Result>
            FG<Par, Result>(IUnaryFunction<Par, Result> f, INullaryFunction<Par> g)
        {
            return new ComposeFG<Par, Result>(f, g);
        }
        /// <summary>
        /// This method returns a delegate that binds a nullary into a unary delegate.
        /// It can be seen as F(G()).
        /// </summary>
        /// <param name="f">The outer delegate.</param>
        /// <param name="g">The inner delegate.</param>
        /// <returns></returns>
        public static Func<Result>
            FG<Par, Result>(Func<Par, Result> f, Func<Par> g)
        {
            return delegate { return f(g()); };
        }
        /// <summary>
        /// This method creates a binder, that binds the result of one unary function
        /// into a second one: F(G(x)).
        /// </summary>
        /// <param name="f">The outer functor representing F(X).</param>
        /// <param name="g">The inner functor representing G(X).</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Capitalization is intended. Each capital letter represents a function, each lowercase an argument!")]
        public static IUnaryFunction<ParG, Result>
            FGx<ParG, ParF, Result>(IUnaryFunction<ParF, Result> f, IUnaryFunction<ParG, ParF> g)
        {
            return new ComposeFGx<ParG, ParF, Result>(f, g);
        }
        /// <summary>
        /// This method creates a binder, that binds the result of one unary delegate
        /// into a second one: F(G(x)).
        /// </summary>
        /// <param name="f">The outer functor representing F(X).</param>
        /// <param name="g">The inner functor representing G(X).</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Capitalization is intended. Each capital letter represents a function, each lowercase an argument!")]
        public static Func<ParG, Result>
            FGx<ParG, ParF, Result>(Func<ParF, Result> f, Func<ParG, ParF> g)
        {
            return delegate(ParG pg) { return f(g(pg)); };
        }
        /// <summary>
        /// Returns an adapter that binds the result of two unary function
        /// called with the same parameter into a binary function: F(G(x), H(x)).
        /// </summary>
        /// <param name="f">The functor representing F(G, H).</param>
        /// <param name="g">The functor representing G(X).</param>
        /// <param name="h">The functor representing H(X).</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Capitalization is intended. Each capital letter represents a function, each lowercase an argument!")]
        public static IUnaryFunction<X, Result>
            FGxHx<X, ParF1, ParF2, Result>(IBinaryFunction<ParF1, ParF2, Result> f, IUnaryFunction<X, ParF1> g, IUnaryFunction<X, ParF2> h)
        {
            return new ComposeFGxHx<X, ParF1, ParF2, Result>(f, g, h);
        }
        /// <summary>
        /// Returns an adapter that binds the result of two unary delegates
        /// called with the same parameter into a binary delegate: F(G(x), H(x)).
        /// </summary>
        /// <param name="f">The delegate representing F(G, H).</param>
        /// <param name="g">The delegate representing G(X).</param>
        /// <param name="h">The delegate representing H(X).</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Capitalization is intended. Each capital letter represents a function, each lowercase an argument!")]
        public static Func<X, Result>
            FGxHx<X, ParF1, ParF2, Result>(Func<ParF1, ParF2, Result> f, Func<X, ParF1> g, Func<X, ParF2> h)
        {
            return delegate(X x) { return f(g(x), h(x)); };
        }
        /// <summary>
        /// Returns an adapter that binds the result of a binary function 
        /// into a unary function:  F(G(x, y)).
        /// </summary>
        /// <param name="f">The functor representing F().</param>
        /// <param name="g">The functor representing G(x, y).</param>
        /// <returns></returns>
        public static IBinaryFunction<X, Y, Result>
            FGxy<X, Y, ParF, Result>(IUnaryFunction<ParF, Result> f, IBinaryFunction<X, Y, ParF> g)
        {
            return new ComposeFGxy<X, Y, ParF, Result>(f, g);
        }
        /// <summary>
        /// Returns a delegate that binds the result of a binary delegate 
        /// into a unary delegate:  F(G(x, y)).
        /// </summary>
        /// <param name="f">The delegate representing F().</param>
        /// <param name="g">The delegate representing G(x, y).</param>
        /// <returns></returns>
        public static Func<X, Y, Result>
            FGxy<X, Y, ParF, Result>(Func<ParF, Result> f, Func<X, Y, ParF> g)
        {
            return delegate(X x, Y y) { return f(g(x, y)); };
        }
        /// <summary>
        /// Returns an adapter that binds the result of two unary functions 
        /// into a binary function:  F(G(x), H(y)).
        /// </summary>
        /// <param name="f">The functor representing F().</param>
        /// <param name="g">The functor representing G(x).</param>
        /// <param name="h">The functor representing H(y).</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Capitalization is intended. Each capital letter represents a function, each lowercase an argument!")]
        public static IBinaryFunction<X, Y, Result>
            FGxHy<X, Y, ParamF1, ParamF2, Result>(IBinaryFunction<ParamF1, ParamF2, Result> f, IUnaryFunction<X, ParamF1> g, IUnaryFunction<Y, ParamF2> h)
        {
            return new ComposeFGxHy<X, Y, ParamF1, ParamF2, Result>(f, g, h);
        }
        /// <summary>
        /// Returns an adapter that binds the result of two unary delegates
        /// into a binary delegate:  F(G(x), H(y)).
        /// </summary>
        /// <param name="f">The delegate representing F().</param>
        /// <param name="g">The delegate representing G(x).</param>
        /// <param name="h">The delegate representing H(y).</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Capitalization is intended. Each capital letter represents a function, each lowercase an argument!")]
        public static Func<X, Y, Result>
            FGxHy<X, Y, ParamF1, ParamF2, Result>(Func<ParamF1, ParamF2, Result> f, Func<X, ParamF1> g, Func<Y, ParamF2> h)
        {
            return delegate(X x, Y y) { return f(g(x), h(y)); };
        }
    }
}
