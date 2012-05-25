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
using NStl.Exceptions;
using System.Collections;

namespace NStl.Util
{
    static class Verify
    {
        internal static void ArgumentNotNull(object o, string argumentName)
        {
            if (o == null)
                throw new ArgumentNullException(argumentName);
        }
        internal static void ArgumentNotNullOrEmpty(IEnumerable e, string argumentName)
        {
            ArgumentNotNull(e, argumentName);
            if (!e.GetEnumerator().MoveNext())
                throw new ArgumentException(Resource.EnumerableIsEmpty, argumentName);
        }
        internal static void InstanceEquals(object lhs, object rhs, string msg)
        {
            if (!ReferenceEquals(lhs, rhs))
                throw new NotTheSameInstanceException(msg);
        }
        internal static void AreEqual<Ex>(object lhs, object rhs, string msg) where Ex : Exception, new()
        {
            if (!Equals(lhs, rhs))
            {
                Exception ex = (Exception)Activator.CreateInstance(typeof (Ex), new object[] {msg});
                throw ex;
            }
        }
        internal static void VersionsAreEqual(int actual, int expected)
        {
            if (actual != expected)
                throw new IncompatibleVersionException(actual, expected);
        }

        public static void AreNotEqual<Ex>(object lhs, object rhs, string message) where Ex : Exception, new()
        {
            if (Equals(lhs, rhs))
            {
                Exception ex = (Exception) Activator.CreateInstance(typeof (Ex), new object[] {message});
                throw ex;
            }
        }

        public static void ArgumentGreaterEqualsZero(long arg, string argumentName)
        {
            if(arg < 0)
                throw new ArgumentException(Resource.ArgMustBeGraterThanZero, argumentName);
        }
        public static void IndexInRange(long arg, long low, long high, string argName)
        {
            if (arg < low || arg >high)
                throw new ArgumentOutOfRangeException(argName);
        }
    }
}


