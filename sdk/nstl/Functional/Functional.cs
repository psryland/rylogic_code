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

namespace NStl
{
    /// <summary>
    /// This class contains all diverse functional related things.
    /// </summary>
    public static class Functional
    {
        /// <summary>
        /// Returns a unction object that will clone and return the passed in parameter.
        /// </summary>
        /// <returns></returns>
        public static IUnaryFunction<T, T>
            Cloner<T>() where T : ICloneable
        {
            return new Cloner<T>();
        }
        
        /// <summary>
        /// Returns a functor that can be used to check if the passed in argument is null/Nothing.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IUnaryPredicate<T>
            IsNull<T>() where T: class
        {
            return new IsNull<T>();
        }
        /// <summary>
        /// Returns a functor that can be used to check if the passed in argument the default value.
        /// The default value for refernce types is null/Nothing. For value types refer to their documentation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <remarks>
        /// For example, a System.Int32 defaults to 0, bool to fale and so on.
        /// </remarks>
        public static IUnaryPredicate<T>
            IsDefault<T>()
        {
            return new IsDefault<T>();
        }
       
        /// <summary>
        /// Returns a functor that compares if the <see cref="System.Type"/> in the first parameter
        /// derives or implements the <see cref="System.Type"/> in the second parameter.
        /// </summary>
        /// <returns></returns>
        public static IBinaryPredicate<Type, Type> IsKindOf()
        {
            return new IsKindOf();
        }

        /// <summary>
        /// Helper that converts any function with two arguments into a BinaryFunction.
        /// The returned functor will pass the arguments that are passed into 
        /// BinaryFunction.Execute(..) as arguments into the stored delegate.
        /// </summary>
        /// <typeparam name="Arg1">The type of the first argument.</typeparam>
        /// <typeparam name="Arg2">The type of the second argument.</typeparam>
        /// <typeparam name="Result">The type of the result.</typeparam>
        /// <param name="del">The function to adapt to a binary function object.</param>
        /// <returns>A binary function object.</returns>
        public static IBinaryFunction<Arg1, Arg2, Result>
            PtrFun<Arg1, Arg2, Result>(Func<Arg1, Arg2, Result> del)
        {
            return new BinaryDelegate2BinaryFunctionAdapter<Arg1, Arg2, Result>(del);
        }
        /// <summary>
        /// Helper that converts any void function with two arguments into a BinaryFunction.
        /// The returned functor will pass the arguments that are passed into 
        /// BinaryFunction.Execute(..) as arguments into the stored delegate.
        /// </summary>
        /// <typeparam name="Arg1">The type of the first argument.</typeparam>
        /// <typeparam name="Arg2">The type of the second argument.</typeparam>
        /// <param name="del">The function to adapt to a binary function object.</param>
        /// <returns>A binary function object.</returns>
        public static IBinaryVoidFunction<Arg1, Arg2>
            PtrFun<Arg1, Arg2>(Action<Arg1, Arg2> del)
        {
            return new BinaryVoidDelegate2BinaryVoidFunctionAdapter<Arg1, Arg2>(del);
        }
        /// <summary>
        /// Helper that converts any function with one argument into a UnaryFunction.
        /// The returned functor will pass the argument that is passed into 
        /// UnaryFunction.Execute(..) as an argument into the stored delegate.
        /// </summary>
        /// <typeparam name="Arg">The type of the argument.</typeparam>
        /// <typeparam name="Result">The type of the return value.</typeparam>
        /// <param name="del">The function to adapt.</param>
        /// <returns></returns>
        public static IUnaryFunction<Arg, Result>
            PtrFun<Arg, Result>(Func<Arg, Result> del)
        {
            return new UnaryDelegate2UnaryFunctionAdapter<Arg, Result>(del);
        }
        /// <summary>
        /// Helper that converts any void function with one argument into a UnaryFunction.
        /// The returned functor will pass the argument that is passed into 
        /// UnaryFunction.Execute(..) as an argument into the stored delegate.
        /// </summary>
        /// <typeparam name="Arg">The type of the argument.</typeparam>
        /// <param name="del"></param>
        /// <returns></returns>
        public static IUnaryVoidFunction<Arg>
            PtrFun<Arg>(Action<Arg> del)
        {
            return new UnaryVoidDelegate2UnaryVoidFunctionAdapter<Arg>(del);
        }
        /// <summary>
        /// Helper that converts any void function with zero arguments into a NullaryFunction.
        /// </summary>
        /// <typeparam name="Result"></typeparam>
        /// <param name="del"></param>
        /// <returns></returns>
        public static INullaryFunction<Result>
            PtrFun<Result>(Func<Result> del)
        {
            return new NullaryDelegate2NullaryFunctionAdapter<Result>(del);
        }
    }
}
