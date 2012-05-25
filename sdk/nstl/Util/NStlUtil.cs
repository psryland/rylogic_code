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
using NStl.Iterators;
using NStl.Iterators.Private;
using System.IO;

namespace NStl
{
    /// <summary>
    /// This class provides functionality, such as combining the NStl iterators
    /// with the .Net containers. It also contains STL functions that are
    /// not part of a special section of the C++ STL, e.g. advance.
    /// </summary>
    public static partial class NStlUtil
    {
        /// <summary>
        /// Creates an output iterator that will print all values to the <see cref="Console"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="seperator">The seperator string that is inserted between the values.</param>
        /// <returns></returns>
        public static IOutputIterator<T>
            ConsoleOutput<T>(string seperator)
        {
            return new ConsoleOutputIterator<T>(seperator);
        }
        /// <summary>
        /// Creates an output iterator that will print all values to the stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream"></param>
        /// <param name="seperator">The seperator string that is inserted between the values.</param>
        /// <returns></returns>
        public static IOutputIterator<T>
            StreamOutput<T>(Stream stream, string seperator)
        {
            return new StreamOutputIterator<T>(stream, seperator);
        }
        /// <summary>
        /// Creates an output iterator that will print all values to the stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="writer"></param>
        /// <param name="seperator">The seperator string that is inserted between the values.</param>
        /// <returns></returns>
        public static IOutputIterator<T>
            TextWriterOutput<T>(TextWriter writer, string seperator)
        {
            return new StreamOutputIterator<T>(writer, seperator);
        }
        /// <summary>
        /// Creates an output iterator that eats the assigned values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IOutputIterator<T>
            BlackHoleOutput<T>()
        {
            return new BlackholeOutputIterator<T>();
        }
    }
}
