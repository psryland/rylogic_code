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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;

namespace NStl.Exceptions
{
    /// <summary>
    /// This exception is thrown when you try to access a range that is empty.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors",
        Justification = "Don't want this exception thrown elsewhere!"), Serializable]
    public sealed class ContainerEmptyException : Exception
    {
        private ContainerEmptyException(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
        }

        [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors",
            Justification = "Don't want this exception thrown elsewhere!")]
        internal ContainerEmptyException()
            : base(Resource.ContainerIsEmpty)
        {
        }
    }

    /// <summary>
    /// This exception is thrown when you have tried to dereference an iterator that is at or past the end of a sequence.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors",
        Justification = "Don't want this exception thrown elsewhere!"), Serializable]
    public sealed class DereferenceEndIteratorException : InvalidOperationException
    {
        private DereferenceEndIteratorException(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
        }

        [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors",
            Justification = "Don't want this exception thrown elsewhere!")]
        internal DereferenceEndIteratorException()
            : base(Resource.MustNotDerefEndIterator)
        {
        }
    }

    /// <summary>
    /// This exception is thrown when a requested method couldn't be found for a specific Type.
    /// This might happen whe the method name was mispelled, the argument types did not match or
    /// the method was not public.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors"), Serializable]
    public sealed class NoMatchingMethodFoundException : Exception
    {
        private NoMatchingMethodFoundException(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
        }

        internal NoMatchingMethodFoundException(Type tp, string methodName)
            : base(string.Format(new NumberFormatInfo(), Resource.MethodNotFound, methodName, tp))
        {
        }
    }

    /// <summary>
    /// This exception is thrown when a requested property couldn't be found for a specific Type.
    /// This might happen whe the property name was mispelled, the argument types did not match or
    /// the property was not public.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors"), Serializable]
    public sealed class NoMatchingPropertyFoundException : Exception
    {
        private NoMatchingPropertyFoundException(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
        }

        internal NoMatchingPropertyFoundException(Type tp, string propName)
            : base(string.Format(new NumberFormatInfo(), Resource.PropertyNotFound, propName, tp))
        {
        }
    }

    /// <summary>
    /// This exception is thrown when two instance were expected to be the same and were not.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors"), Serializable]
    public sealed class NotTheSameInstanceException : Exception
    {
        private NotTheSameInstanceException(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
        }

        [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
        internal NotTheSameInstanceException(string msg)
            : base(msg)
        {
        }
    }

    /// <summary>
    /// Thrown when an iterator of a different container was used as input to
    /// a container method.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors"), Serializable]
    public sealed class EndIteratorIsNotAValidInputException : Exception
    {
        private EndIteratorIsNotAValidInputException(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
        }

        [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
        internal EndIteratorIsNotAValidInputException()
            : base(Resource.EndIteratorIsNotAValidinput)
        {
        }
    }

    /// <summary>
    /// Thrown on desirialization when an object differs in version from the serialized data.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors"), Serializable]
    public sealed class IncompatibleVersionException : Exception
    {
        private IncompatibleVersionException(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
        }

        internal IncompatibleVersionException(int actual, int expected)
            : base(string.Format(new NumberFormatInfo(), Resource.VersionDiffer, actual, expected))
        {
        }
    }
}