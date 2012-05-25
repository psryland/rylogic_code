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
    /// A proxy implementation for bidirectional iterators. It is intended to be used
    /// as an implementation base or for testing purposes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BidirectionalIteratorProxy<T> : BidirectionalIterator<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="innerIt"></param>
        public BidirectionalIteratorProxy(IBidirectionalIterator<T> innerIt)
        {
            this.innerIt = (IBidirectionalIterator<T>)innerIt.Clone();
        }
        /// <summary>
        /// 
        /// </summary>
        private readonly IBidirectionalIterator<T> innerIt;
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IBidirectionalIterator<T> PreDecrement()
        {
            innerIt.PreDecrement();
            return this;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IForwardIterator<T> PreIncrement()
        {
            innerIt.PostIncrement();
            return this;
        }
        /// <summary>
        /// 
        /// </summary>
        public override T Value
        {
            get
            {
                return innerIt.Value;
            }
            set
            {
                innerIt.Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IBidirectionalIterator<T> InnerIt
        {
            get { return innerIt; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IIterator<T> Clone()
        {
            return new BidirectionalIteratorProxy<T>(innerIt);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected override bool Equals(EquatableIterator<T> obj)
        {
            BidirectionalIteratorProxy<T> r = obj as BidirectionalIteratorProxy<T>;
            if (r == null)
                return false;
            return innerIt.Equals(r.innerIt);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override int HashCode()
        {
            return innerIt.GetHashCode();
        }
    }

    /// <summary>
    /// A proxy implementation for forward iterators. It is intended to be used
    /// as an implementation base or for testing purposes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ForwardIteratorProxy<T> : ForwardIterator<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="innerIt"></param>
        public ForwardIteratorProxy(IForwardIterator<T> innerIt)
        {
            this.innerIt = (IForwardIterator<T>)innerIt.Clone();
        }
        /// <summary>
        /// 
        /// </summary>
        private readonly IForwardIterator<T> innerIt;
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IForwardIterator<T> PreIncrement()
        {
            innerIt.PostIncrement();
            return this;
        }
        /// <summary>
        /// 
        /// </summary>
        public override T Value
        {
            get
            {
                return innerIt.Value;
            }
            set
            {
                innerIt.Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected IForwardIterator<T> InnerIt
        {
            get { return innerIt; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IIterator<T> Clone()
        {
            return new ForwardIteratorProxy<T>(innerIt);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected override bool Equals(EquatableIterator<T> obj)
        {
            ForwardIteratorProxy<T> r = obj as ForwardIteratorProxy<T>;
            if (r == null)
                return false;
            return innerIt.Equals(r.innerIt);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override int HashCode()
        {
            return innerIt.GetHashCode();
        }
    }

    /// <summary>
    /// A proxy implementation for <see cref="IBidirectionalInputIterator{T}"/>. It is intended to be used
    /// as an implementation base or for testing purposes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class BidirectionalInputIteratorProxy<T> : BidirectionalInputIterator<T>
    {
        private readonly IBidirectionalInputIterator<T> inner;

        public BidirectionalInputIteratorProxy(IBidirectionalInputIterator<T> inner)
        {
            this.inner = (IBidirectionalInputIterator<T>)inner.Clone();
        }

        public override IBidirectionalInputIterator<T> PreDecrement()
        {
            inner.PreDecrement();
            return this;
        }

        public override IInputIterator<T> PreIncrement()
        {
            inner.PreIncrement();
            return this;
        }

        public override T Value
        {
            get { return inner.Value; }
        }

        public override IIterator<T> Clone()
        {
            return new BidirectionalInputIteratorProxy<T>(inner);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected override bool Equals(EquatableIterator<T> obj)
        {
            BidirectionalInputIteratorProxy<T> r = obj as BidirectionalInputIteratorProxy<T>;
            if (r == null)
                return false;
            return Equals(inner, r.inner);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override int HashCode()
        {
            return inner.GetHashCode();
        }
    }
}
