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
using NStl.Iterators.Support;
using System.IO;

namespace NStl.Iterators.Private
{
    /// <summary>
    /// Output iterator that prints the assigned values separated by the seperator
    /// using Console.Write(). Use the util.console_output function to obtain a correct
    /// initialized iterator
    /// </summary>
    internal class ConsoleOutputIterator<T> : StreamOutputIterator<T>
    {
        internal ConsoleOutputIterator(string seperator)
            : base(Console.Out, seperator)
        {}
		
       
    }
    internal class StreamOutputIterator<T> : OutputIterator<T>
    {
        internal StreamOutputIterator(Stream stream, string seperator)
            : this(new StreamWriter(stream), seperator)
        {}
        internal StreamOutputIterator(TextWriter stream, string seperator)
        {
            this.stream = stream;
            this.seperator = seperator;
        }

        public override T Value
        {
            set
            {
                stream.Write((firstWrite ? "" : seperator) + value);
                firstWrite = false;
                stream.Flush();
            }
        }

        private readonly TextWriter stream;
        private readonly string seperator;
        private bool firstWrite = true;
    }
}
