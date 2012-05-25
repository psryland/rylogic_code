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
    ///  Adapts a <see cref="IBidirectionalIterator{T}"/> to a <see cref="IBidirectionalInputIterator{T}"/>.
    /// </summary>
    public class BidirectionalIterator2InputBidirectionalIteratorAdapter<T> : BidirectionalInputIterator<T>
    {
        private readonly IBidirectionalIterator<T> inner;
        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="inner"></param>
        public BidirectionalIterator2InputBidirectionalIteratorAdapter(IBidirectionalIterator<T> inner)
        {
            this.inner = (IBidirectionalIterator<T>)inner.Clone();
        }
        /// <summary>
        /// See <see cref="IBidirectionalInputIterator{T}.PreDecrement"/> for details.
        /// </summary>
        /// <returns></returns>
        public override IBidirectionalInputIterator<T> PreDecrement()
        {
            Inner.PreDecrement();
            return this;
        }
        /// <summary>
        /// See <see cref="IInputIterator{T}.PreIncrement"/> for details.
        /// </summary>
        /// <returns></returns>
        public override IInputIterator<T> PreIncrement()
        {
            Inner.PreIncrement();
            return this;
        }
        /// <summary>
        /// See <see cref="IInputIterator{T}.Value"/> for details.
        /// </summary>
        public override T Value
        {
            get { return Inner.Value; }
        }
        /// <summary>
        /// The adapted iterator.
        /// </summary>
        protected internal IBidirectionalIterator<T> Inner
        {
            get { return inner; }
        }

        /// <summary>
        /// See <see cref="IIterator{T}.Clone"/> for details.
        /// </summary>
        /// <returns></returns>
        public override IIterator<T> Clone()
        {
            return new BidirectionalIterator2InputBidirectionalIteratorAdapter<T>(Inner);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected override bool Equals(EquatableIterator<T> obj)
        {
            BidirectionalIterator2InputBidirectionalIteratorAdapter<T> rhs = 
                obj as BidirectionalIterator2InputBidirectionalIteratorAdapter<T>;
            if (rhs == null)
                return false;
            return Inner.Equals(rhs.Inner);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override int HashCode()
        {
            return Inner.GetHashCode();
        }
    }
}
