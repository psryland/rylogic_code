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



namespace NStl.SyntaxHelper
{
    /// <summary>
    /// Syntax helper that offers all logical functors of the NSTL.
    /// </summary>
    public static class Logical
    {
        /// <summary>
        /// Returns the complement of a binary predicate.
        /// </summary>
        /// <param name="predicate">The binary predicate to be negated.</param>
        /// <returns>
        /// A binary predicate that is the negation of the binary predicate modified.
        /// </returns>
        public static IBinaryPredicate<T, U>
            Not2<T, U>(IBinaryFunction<T, U, bool> predicate)
        {
            return new BinaryNegate<T, U>(predicate);
        }
        /// <summary>
        /// Returns the complement of a unary predicate.
        /// </summary>
        /// <param name="predicate">The unary predicate to be negated.</param>
        /// <returns>A binary predicate that is the negation of the unary predicate modified.</returns>
        public static IUnaryPredicate<T>
            Not1<T>(IUnaryFunction<T, bool> predicate)
        {
            return new UnaryNegate<T>(predicate);
        }
        /// <summary>
        /// Creates a functor that will perform lhs AND rhs with the input parameters.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Returns lhs AND rhs. If you want to use this functor to combine the output 
        /// from two other functors, have a look at <see cref="Compose"/>.
        /// </remarks>
        public static IBinaryPredicate<bool, bool> And()
        {
            return new LogicalAnd();
        }
        /// <summary>
        /// Creates a functor that will perform the negation of the passed in parameter.
        /// </summary>
        /// <returns></returns>
        public static IUnaryPredicate<bool> Not()
        {
            return new LogicalNot();
        }
        /// <summary>
        /// Creates a functor that will perform lhs || rhs of the passed in arguments.
        /// </summary>
        /// <returns></returns>
        public static IBinaryPredicate<bool, bool> Or()
        {
            return new LogicalOr();
        }
    }
}
