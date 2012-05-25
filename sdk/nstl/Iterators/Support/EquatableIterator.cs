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


namespace NStl.Iterators.Support
{
    /// <summary>
    /// Base class for all iterator implementation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class EquatableIterator<T> : IIterator<T>
    {
        /// <summary>
        /// Returns an exact copy of this object.
        /// </summary>
        /// <returns></returns>
        public abstract IIterator<T> Clone();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator ==(EquatableIterator<T> lhs, EquatableIterator<T> rhs)
        {
            return Equals(lhs, rhs);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator !=(EquatableIterator<T> lhs, EquatableIterator<T> rhs)
        {
            return !Equals(lhs, rhs);
        }
        /// <summary>
        /// See <see cref="object.Equals(object)"/> for details.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public sealed override bool Equals(object value)
        {
            EquatableIterator<T> rhs = value as EquatableIterator<T>;
            if(rhs == null)
                return false;
            return Equals(rhs);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected abstract bool Equals(EquatableIterator<T> obj);
        /// <summary>
        /// See <see cref="object.GetHashCode"/> for details.
        /// </summary>
        /// <returns></returns>
        public sealed override int GetHashCode()
        {
            return HashCode();
        }
        /// <summary>
        /// When implemented, it returns the hash code for this object.
        /// </summary>
        /// <returns></returns>
        protected abstract int HashCode();
    }
}
