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
    /// This enum is used to adapt a .NET <see cref="IComparer"/>/<see cref="IComparer{T}"/> implementation
    /// to a comparison functor. It specifies how the comparer will 
    /// interpret the return value of the IComparer interface
    /// </summary>
    [Serializable]
    public enum CompareAction
    {
        /// <summary>
        /// IComparer.Compare(lhs, rhs) &lt;  0
        /// </summary>
        Less,
        /// <summary>
        /// IComparer.Compare(lhs, rhs) &lt;=  0
        /// </summary>
        LessEqual,
        /// <summary>
        /// IComparer.Compare(lhs, rhs) == 0
        /// </summary>
        Equal,
        /// <summary>
        /// IComparer.Compare(lhs, rhs) !=  0
        /// </summary>
        NotEqual,
        /// <summary>
        /// IComparer.Compare(lhs, rhs) &gt;= 0
        /// </summary>
        GreaterEqual,
        /// <summary>
        /// IComparer.Compare(lhs, rhs) &gt; 0
        /// </summary>
        Greater
    }
}

