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
using System.Collections.Generic;
using NStl.Iterators;
using NStl.Iterators.Private;
using NStl.Iterators.Support;
using NStl.Linq;
using NStl.Util;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Diagnostics.CodeAnalysis;
using NStl.SyntaxHelper;

namespace NStl.Collections.Private
{
    //_RB_Tree

    /// <summary>
    /// The node of the RBtree. This is a straight port from the SGI tree class.
    /// No reorganisation, no nicer interfaces. As this is the workhorse for four
    /// containers and I'm definitively not a tree specialist, I wanted to stay as
    /// close to the original as possible :-),so that I'm able to debug the C++
    /// and the .NET implementation parallel
    /// </summary>
    [Serializable]
    internal class RbTreeNode<T> : ISerializable
    {
        internal RbTreeNode()
        {
        }

        public enum ColorType
        {
            Red = 0,
            Black = 1
        }

        public ColorType Color = ColorType.Black;
        public RbTreeNode<T> Parent = null;
        public RbTreeNode<T> Left;
        public RbTreeNode<T> Right;

        public static RbTreeNode<T> GetMinimum(RbTreeNode<T> x)
        {
            while (x.Left != null)
                x = x.Left;
            return x;
        }

        public static void SetMinimum(RbTreeNode<T> x, RbTreeNode<T> y)
        {
            while (x.Left != null)
                x = x.Left;
            x.Parent.Left = y;
        }

        public static RbTreeNode<T> GetMaximum(RbTreeNode<T> x)
        {
            while (x.Right != null)
                x = x.Right;
            return x;
        }

        public static void SetMaximum(RbTreeNode<T> x, RbTreeNode<T> y)
        {
            while (x.Right != null)
                x = x.Right;
            x.Parent.Right = y;
        }

        public T Value = default(T);

        #region ISerializable Members
        private const int Version = 1;
        protected RbTreeNode(SerializationInfo info, StreamingContext ctxt)
        {
            int version = info.GetInt32("Version");
            Verify.VersionsAreEqual(version, Version);

            Parent  = (RbTreeNode<T>)info.GetValue("Parent", typeof(RbTreeNode<T>));
            Left    = (RbTreeNode<T>)info.GetValue("Left",   typeof(RbTreeNode<T>));
            Right   = (RbTreeNode<T>)info.GetValue("Right",  typeof(RbTreeNode<T>));
            Color   = (ColorType)    info.GetValue("Color",  typeof(ColorType));
            Value   = (T)            info.GetValue("Value",  typeof(T));
        }
        /// <summary>
        /// See <see cref="ISerializable.GetObjectData"/> for details.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Version", Version);

            info.AddValue("Parent", Parent);
            info.AddValue("Left", Left);
            info.AddValue("Right", Right);
            info.AddValue("Color", Color);
            info.AddValue("Value", Value);
       }
        #endregion
    }

    /// <summary>
    /// The tree iterator
    /// </summary>
    internal class RbTreeIterator<T> : BidirectionalIterator<T>
    {
        public RbTreeIterator(RbTreeNode<T> node)
        {
            Node = node;
        }

        internal RbTreeNode<T> Node;

        private void Increment()
        {
            if (Node.Right != null)
            {
                Node = Node.Right;
                while (Node.Left != null)
                    Node = Node.Left;
            }
            else
            {
                RbTreeNode<T> y = Node.Parent;
                while (Node == y.Right)
                {
                    Node = y;
                    y = y.Parent;
                }
                if (Node.Right != y)
                    Node = y;
            }
        }

        private void Decrement()
        {
            if (Node.Color == RbTreeNode<T>.ColorType.Red &&
                Node.Parent.Parent == Node)
                Node = Node.Right;
            else if (Node.Left != null)
            {
                RbTreeNode<T> y = Node.Left;
                while (y.Right != null)
                    y = y.Right;
                Node = y;
            }
            else
            {
                RbTreeNode<T> y = Node.Parent;
                while (Node == y.Left)
                {
                    Node = y;
                    y = y.Parent;
                }
                Node = y;
            }
        }

        #region IBidirectionalIterator Members

        public override IBidirectionalIterator<T> PreDecrement()
        {
            Decrement();
            return this;
        }

        public override IForwardIterator<T> PreIncrement()
        {
            Increment();
            return this;
        }

        #endregion

        #region iterator Members

        public override T Value
        {
            get { return Node.Value; }
            set { Node.Value = value; }
        }

        protected override int HashCode()
        {
            return Node.GetHashCode();
        }

        protected override bool Equals(EquatableIterator<T> obj)
        {
            RbTreeIterator<T> rhs = obj as RbTreeIterator<T>;
            if (rhs == null)
                return false;
            return Node == rhs.Node;
        }

        #endregion

        #region ICloneable Members

        public override IIterator<T> Clone()
        {
            return new RbTreeIterator<T>(Node);
        }

        #endregion
    }

    /// <summary>
    /// The Tree! 
    /// </summary>
    [Serializable]
    internal class RbTree<Key, Value> : ISerializable
    {
        // ... map : _Rb_tree<Key, map.value_type<Key, Value>
        // ... set : _Rb_tree<Key, Key>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="__comp"></param>
        /// <param name="__keyof"></param>
        public RbTree(IBinaryFunction<Key, Key, bool> __comp, IUnaryFunction<Value, Key> __keyof)
        {
            header = GetNode();
            NodeCount = 0;
            keyCompare = __comp;
            keyOfValue = __keyof;
            EmptyInitialize();
        }

        public RbTree(RbTree<Key, Value> x)
        {
            if (x.Root == null)
                EmptyInitialize();
            else
            {
                SetColor(header, RbTreeNode<Value>.ColorType.Red);
                Root = Copy(x.Root, header);
                Leftmost = RbTreeNode<Value>.GetMinimum(Root);
                Rightmost = RbTreeNode<Value>.GetMaximum(Root);
            }
            keyCompare = x.keyCompare;
            keyOfValue = x.keyOfValue;
            NodeCount = x.NodeCount;
        }

        public RbTreeIterator<Value> Begin()
        {
            return new RbTreeIterator<Value>(Leftmost);
        }

        public RbTreeIterator<Value> End()
        {
            return new RbTreeIterator<Value>(header);
        }

        public IBidirectionalIterator<Value> RBegin()
        {
            return new BidirectionalReverseIterator<Value>(End());
        }

        public IBidirectionalIterator<Value> REnd()
        {
            return new BidirectionalReverseIterator<Value>(Begin());
        }

        public bool Empty()
        {
            return NodeCount == 0;
        }

        public int Size()
        {
            return NodeCount;
        }

        public void Swap(RbTree<Key, Value> t)
        {
            Algorithm.Swap(ref header, ref t.header);
            Algorithm.Swap(ref NodeCount, ref t.NodeCount);
            Algorithm.Swap(ref keyCompare, ref t.keyCompare);
            Algorithm.Swap(ref keyOfValue, ref t.keyOfValue);
        }

        public KeyValuePair<IBidirectionalIterator<Value>, bool>
            InsertUnique(Value v)
        {
            RbTreeNode<Value> y = header;
            RbTreeNode<Value> x = Root;
            bool __comp = true;
            while (x != null)
            {
                y = x;
                __comp = keyCompare.Execute(keyOfValue.Execute(v), ExtractKey(x));
                x = __comp ? GetLeft(x) : GetRight(x);
            }
            RbTreeIterator<Value> __j = new RbTreeIterator<Value>(y);
            if (__comp)
                if (Equals(__j, Begin()))
                    return new KeyValuePair<IBidirectionalIterator<Value>, bool>(Insert(x, y, v), true);
                else
                    __j.PreDecrement();
            if (keyCompare.Execute(ExtractKey(__j.Node), keyOfValue.Execute(v)))
                return new KeyValuePair<IBidirectionalIterator<Value>, bool>(Insert(x, y, v), true);
            return new KeyValuePair<IBidirectionalIterator<Value>, bool>(__j, false);
        }

        public RbTreeIterator<Value>
            InsertUnique(IForwardIterator<Value> pos, Value v)
        {
            RbTreeIterator<Value> __position = (RbTreeIterator<Value>) pos;
            if (__position.Node == header.Left)
            {
                // begin()
                if (Size() > 0 &&
                    keyCompare.Execute(keyOfValue.Execute(v), ExtractKey(__position.Node)))
                    return Insert(__position.Node, __position.Node, v);
                    // first argument just needs to be non-null 
                else
                    return (RbTreeIterator<Value>) InsertUnique(v).Key;
            }
            else if (__position.Node == header)
            {
                // end()
                if (keyCompare.Execute(ExtractKey(Rightmost), keyOfValue.Execute(v)))
                    return Insert(null, Rightmost, v);
                else
                    return (RbTreeIterator<Value>) InsertUnique(v).Key;
            }
            else
            {
                RbTreeIterator<Value> __before = (RbTreeIterator<Value>) __position.Clone();
                __before.PreDecrement();
                if (keyCompare.Execute(ExtractKey(__before.Node), keyOfValue.Execute(v))
                    && keyCompare.Execute(keyOfValue.Execute(v), ExtractKey(__position.Node)))
                {
                    if (GetRight(__before.Node) == null)
                        return Insert(null, __before.Node, v);
                    else
                        return Insert(__position.Node, __position.Node, v);
                    // first argument just needs to be non-null 
                }
                else
                    return (RbTreeIterator<Value>) InsertUnique(v).Key;
            }
        } //insert_unique
        public void
            InsertUnique(IInputIterator<Value> __first, IInputIterator<Value> __last)
        {
            __first = (IInputIterator<Value>) __first.Clone();
            for (; !Equals(__first, __last); __first.PreIncrement())
                InsertUnique(__first.Value);
        }

        public RbTreeIterator<Value>
            InsertEqual(Value v)
        {
            RbTreeNode<Value> y = header;
            RbTreeNode<Value> x = Root;
            while (x != null)
            {
                y = x;
                x = keyCompare.Execute(keyOfValue.Execute(v), ExtractKey(x))
                        ? GetLeft(x)
                        : GetRight(x);
            }
            return Insert(x, y, v);
        }

        public RbTreeIterator<Value>
            InsertEqual(IForwardIterator<Value> where, Value v)
        {
            RbTreeIterator<Value> __position = (RbTreeIterator<Value>) where;
            if (__position.Node == header.Left)
            {
                // begin()
                if (Size() > 0
                    && !keyCompare.Execute(ExtractKey(__position.Node), keyOfValue.Execute(v)))
                    return Insert(__position.Node, __position.Node, v);
                    // first argument just needs to be non-null 
                else
                    return InsertEqual(v);
            }
            else if (__position.Node == header)
            {
// end()
                if (!keyCompare.Execute(keyOfValue.Execute(v), ExtractKey(Rightmost)))
                    return Insert(null, Rightmost, v);
                else
                    return InsertEqual(v);
            }
            else
            {
                RbTreeIterator<Value> __before = (RbTreeIterator<Value>) __position.Clone();
                __before.PreDecrement();
                if (!keyCompare.Execute(keyOfValue.Execute(v), ExtractKey(__before.Node))
                    && !keyCompare.Execute(ExtractKey(__position.Node), keyOfValue.Execute(v)))
                {
                    if (GetRight(__before.Node) == null)
                        return Insert(null, __before.Node, v);
                    else
                        return Insert(__position.Node, __position.Node, v);
                    // first argument just needs to be non-null 
                }
                else
                    return InsertEqual(v);
            }
        }

        public void
            InsertEqual(IInputIterator<Value> __first, IInputIterator<Value> __last)
        {
            __first = (IInputIterator<Value>) __first.Clone();
            for (; !Equals(__first, __last); __first.PreIncrement())
                InsertEqual(__first.Value);
        }


        public void
            Erase(RbTreeIterator<Value> where)
        {
            if (Equals(where, End()))
                return;
            RbTreeNode<Value> y =
                RebalanceForErase(where.Node,
                                  ref header.Parent,
                                  ref header.Left,
                                  ref header.Right);
            DestroyNode(y);
            --NodeCount;
        }

        public int
            Erase(Key x)
        {
            KeyValuePair<RbTreeIterator<Value>, RbTreeIterator<Value>> p = EqualRange(x);
            int n = p.Key.DistanceTo(p.Value); //GEN
            Erase(p.Key, p.Value);
            return n;
        }

        public int
            Erase(RbTreeIterator<Value> __first, RbTreeIterator<Value> __last)
        {
            int rv = 0;
            if (Equals(__first, Begin()) && Equals(__last, End()))
            {
                rv = Size();
                Clear();
            }
            else
            {
                __first = (RbTreeIterator<Value>) __first.Clone();
                while (!Equals(__first, __last))
                {
                    Erase((RbTreeIterator<Value>) __first.PostIncrement());
                    ++rv;
                }
            }
            return rv;
        }


        public RbTreeIterator<Value>
            LowerBound(Key k)
        {
            RbTreeNode<Value> y = header; // Last node which is not less than k. 
            RbTreeNode<Value> x = Root; // Current node. 

            while (x != null)
            {
                if (!keyCompare.Execute(ExtractKey(x), k))
                {
                    y = x;
                    x = GetLeft(x);
                }
                else
                    x = GetRight(x);
            }

            return new RbTreeIterator<Value>(y);
        }

        public RbTreeIterator<Value>
            UpperBound(Key k)
        {
            RbTreeNode<Value> y = header; // Last node which is greater than k. 
            RbTreeNode<Value> x = Root; // Current node. 

            while (x != null)
            {
                if (keyCompare.Execute(k, ExtractKey(x)))
                {
                    y = x;
                    x = GetLeft(x);
                }
                else
                    x = GetRight(x);
            }

            return new RbTreeIterator<Value>(y);
        }

        public KeyValuePair<RbTreeIterator<Value>, RbTreeIterator<Value>>
            EqualRange(Key k)
        {
            return new KeyValuePair<RbTreeIterator<Value>, RbTreeIterator<Value>>(LowerBound(k), UpperBound(k));
        }


        public RbTreeIterator<Value>
            Find(Key k)
        {
            RbTreeIterator<Value> __j = LowerBound(k);
            return (Equals(__j, End()) || keyCompare.Execute(k, ExtractKey(__j.Node)))
                       ? End()
                       : __j;
        }

        public int
            Count(Key key)
        {
            Verify.ArgumentNotNull(key, "key");
            KeyValuePair<RbTreeIterator<Value>, RbTreeIterator<Value>> p = EqualRange(key);
            int n = p.Key.DistanceTo(p.Value);
            return n;
        }

        public void Clear()
        {
            if (NodeCount != 0)
            {
                Erase(Root);
                Leftmost = header;
                Root = null;
                Rightmost = header;
                NodeCount = 0;
            }
        }

        public void Dispose()
        {
            PutNode(ref header);
            Clear();
        }


        // ... Private section

        #region Rotation and Rebalance

        private static void
            RotateLeft(RbTreeNode<Value> x, ref RbTreeNode<Value> __root)
        {
            RbTreeNode<Value> y = x.Right;
            x.Right = y.Left;
            if (y.Left != null)
                y.Left.Parent = x;
            y.Parent = x.Parent;

            if (x == __root)
                __root = y;
            else if (x == x.Parent.Left)
                x.Parent.Left = y;
            else
                x.Parent.Right = y;
            y.Left = x;
            x.Parent = y;
        }

        private static void
            RotateRight(RbTreeNode<Value> x, ref RbTreeNode<Value> __root)
        {
            RbTreeNode<Value> y = x.Left;
            x.Left = y.Right;
            if (y.Right != null)
                y.Right.Parent = x;
            y.Parent = x.Parent;

            if (x == __root)
                __root = y;
            else if (x == x.Parent.Right)
                x.Parent.Right = y;
            else
                x.Parent.Left = y;
            y.Right = x;
            x.Parent = y;
        }

        private static void
            Rebalance(RbTreeNode<Value> x, ref RbTreeNode<Value> __root)
        {
            x.Color = RbTreeNode<Value>.ColorType.Red;
            while (x != __root && x.Parent.Color == RbTreeNode<Value>.ColorType.Red)
            {
                if (x.Parent == x.Parent.Parent.Left)
                {
                    RbTreeNode<Value> y = x.Parent.Parent.Right;
                    if (y != null && y.Color == RbTreeNode<Value>.ColorType.Red)
                    {
                        x.Parent.Color = RbTreeNode<Value>.ColorType.Black;
                        y.Color = RbTreeNode<Value>.ColorType.Black;
                        x.Parent.Parent.Color = RbTreeNode<Value>.ColorType.Red;
                        x = x.Parent.Parent;
                    }
                    else
                    {
                        if (x == x.Parent.Right)
                        {
                            x = x.Parent;
                            RotateLeft(x, ref __root);
                        }
                        x.Parent.Color = RbTreeNode<Value>.ColorType.Black;
                        x.Parent.Parent.Color = RbTreeNode<Value>.ColorType.Red;
                        RotateRight(x.Parent.Parent, ref __root);
                    }
                }
                else
                {
                    RbTreeNode<Value> y = x.Parent.Parent.Left;
                    if (y != null && y.Color == RbTreeNode<Value>.ColorType.Red)
                    {
                        x.Parent.Color = RbTreeNode<Value>.ColorType.Black;
                        y.Color = RbTreeNode<Value>.ColorType.Black;
                        x.Parent.Parent.Color = RbTreeNode<Value>.ColorType.Red;
                        x = x.Parent.Parent;
                    }
                    else
                    {
                        if (x == x.Parent.Left)
                        {
                            x = x.Parent;
                            RotateRight(x, ref __root);
                        }
                        x.Parent.Color = RbTreeNode<Value>.ColorType.Black;
                        x.Parent.Parent.Color = RbTreeNode<Value>.ColorType.Red;
                        RotateLeft(x.Parent.Parent, ref __root);
                    }
                }
            }
            __root.Color = RbTreeNode<Value>.ColorType.Black;
        }
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member")]
        private static RbTreeNode<Value>
            RebalanceForErase(RbTreeNode<Value> z,
                              ref RbTreeNode<Value> __root,
                              ref RbTreeNode<Value> __leftmost,
                              ref RbTreeNode<Value> __rightmost)
        {
            RbTreeNode<Value> y = z;
            RbTreeNode<Value> x;
            RbTreeNode<Value> __x_parent;
            if (y.Left == null) // z has at most one non-null child. y == z.
                x = y.Right; // x might be null.
            else if (y.Right == null) // z has exactly one non-null child. y == z.
                x = y.Left; // x is not null.
            else
            {
                // z has two non-null children.  Set y to
                y = y.Right; //   z's successor.  x might be null.
                while (y.Left != null)
                    y = y.Left;
                x = y.Right;
            }
            if (y != z)
            {
                // relink y in place of z.  y is z's successor
                z.Left.Parent = y;
                y.Left = z.Left;
                if (y != z.Right)
                {
                    __x_parent = y.Parent;
                    if (x != null) x.Parent = y.Parent;
                    y.Parent.Left = x; // y must be a child of _M_left
                    y.Right = z.Right;
                    z.Right.Parent = y;
                }
                else
                    __x_parent = y;
                if (__root == z)
                    __root = y;
                else if (z.Parent.Left == z)
                    z.Parent.Left = y;
                else
                    z.Parent.Right = y;
                y.Parent = z.Parent;
                Algorithm.Swap(ref y.Color, ref z.Color);
                y = z;
                // y now points to node to be actually deleted
            }
            else
            {
                // y == z
                __x_parent = y.Parent;
                if (x != null) x.Parent = y.Parent;
                if (__root == z)
                    __root = x;
                else if (z.Parent.Left == z)
                    z.Parent.Left = x;
                else
                    z.Parent.Right = x;
                if (__leftmost == z)
                    if (z.Right == null) // z._M_left must be null also
                        __leftmost = z.Parent;
                        // makes __leftmost == _M_header if z == __root
                    else
                        __leftmost = RbTreeNode<Value>.GetMinimum(x);
                if (__rightmost == z)
                    if (z.Left == null) // z._M_right must be null also
                        __rightmost = z.Parent;
                        // makes __rightmost == _M_header if z == __root
                    else // x == z._M_left
                        __rightmost = RbTreeNode<Value>.GetMaximum(x);
            }
            if (y.Color != RbTreeNode<Value>.ColorType.Red)
            {
                while (x != __root && (x == null || x.Color == RbTreeNode<Value>.ColorType.Black))
                    if (x == __x_parent.Left)
                    {
                        RbTreeNode<Value> __w = __x_parent.Right;
                        if (__w.Color == RbTreeNode<Value>.ColorType.Red)
                        {
                            __w.Color = RbTreeNode<Value>.ColorType.Black;
                            __x_parent.Color = RbTreeNode<Value>.ColorType.Red;
                            RotateLeft(__x_parent, ref __root);
                            __w = __x_parent.Right;
                        }
                        if ((__w.Left == null ||
                             __w.Left.Color == RbTreeNode<Value>.ColorType.Black) &&
                            (__w.Right == null ||
                             __w.Right.Color == RbTreeNode<Value>.ColorType.Black))
                        {
                            __w.Color = RbTreeNode<Value>.ColorType.Red;
                            x = __x_parent;
                            __x_parent = __x_parent.Parent;
                        }
                        else
                        {
                            if (__w.Right == null ||
                                __w.Right.Color == RbTreeNode<Value>.ColorType.Black)
                            {
                                if (__w.Left != null) __w.Left.Color = RbTreeNode<Value>.ColorType.Black;
                                __w.Color = RbTreeNode<Value>.ColorType.Red;
                                RotateRight(__w, ref __root);
                                __w = __x_parent.Right;
                            }
                            __w.Color = __x_parent.Color;
                            __x_parent.Color = RbTreeNode<Value>.ColorType.Black;
                            if (__w.Right != null) __w.Right.Color = RbTreeNode<Value>.ColorType.Black;
                            RotateLeft(__x_parent, ref __root);
                            break;
                        }
                    }
                    else
                    {
                        // same as above, with _M_right <. _M_left.
                        RbTreeNode<Value> __w = __x_parent.Left;
                        if (__w.Color == RbTreeNode<Value>.ColorType.Red)
                        {
                            __w.Color = RbTreeNode<Value>.ColorType.Black;
                            __x_parent.Color = RbTreeNode<Value>.ColorType.Red;
                            RotateRight(__x_parent, ref __root);
                            __w = __x_parent.Left;
                        }
                        if ((__w.Right == null ||
                             __w.Right.Color == RbTreeNode<Value>.ColorType.Black) &&
                            (__w.Left == null ||
                             __w.Left.Color == RbTreeNode<Value>.ColorType.Black))
                        {
                            __w.Color = RbTreeNode<Value>.ColorType.Red;
                            x = __x_parent;
                            __x_parent = __x_parent.Parent;
                        }
                        else
                        {
                            if (__w.Left == null ||
                                __w.Left.Color == RbTreeNode<Value>.ColorType.Black)
                            {
                                if (__w.Right != null) __w.Right.Color = RbTreeNode<Value>.ColorType.Black;
                                __w.Color = RbTreeNode<Value>.ColorType.Red;
                                RotateLeft(__w, ref __root);
                                __w = __x_parent.Left;
                            }
                            __w.Color = __x_parent.Color;
                            __x_parent.Color = RbTreeNode<Value>.ColorType.Black;
                            if (__w.Left != null)
                                __w.Left.Color = RbTreeNode<Value>.ColorType.Black;
                            RotateRight(__x_parent, ref __root);
                            break;
                        }
                    }
                if (x != null) x.Color = RbTreeNode<Value>.ColorType.Black;
            }
            return y;
        } //_Rb_tree_rebalance_for_erase

        #endregion//Rotation and Rebalance

        private static RbTreeNode<Value>
            GetNode()
        {
            return new RbTreeNode<Value>();
        }

        private static void
            PutNode(ref RbTreeNode<Value> p)
        {
            p = null;
        } //noop

        private static RbTreeNode<Value>
            CreateNode(Value x)
        {
            RbTreeNode<Value> __tmp = GetNode();
            __tmp.Value = x;
            return __tmp;
        }

        private static RbTreeNode<Value>
             CloneNode(RbTreeNode<Value> x)
        {
            RbTreeNode<Value> __tmp = CreateNode(x.Value);
            __tmp.Color = x.Color;
            __tmp.Left = null;
            __tmp.Right = null;
            return __tmp;
        }

        private static void
            DestroyNode(RbTreeNode<Value> p)
        {
            p.Value = default(Value);
            PutNode(ref p);
        }

        #region Access Helper

        private RbTreeNode<Value> Root
        {
            get { return header.Parent; }
            set { header.Parent = value; }
        }

        private RbTreeNode<Value> Leftmost
        {
            get { return header.Left; }
            set { header.Left = value; }
        }

        private RbTreeNode<Value> Rightmost
        {
            get { return header.Right; }
            set { header.Right = value; }
        }

        private static RbTreeNode<Value> GetLeft(RbTreeNode<Value> x)
        {
            return x.Left;
        }

        private static void SetLeft(RbTreeNode<Value> x, RbTreeNode<Value> val)
        {
            x.Left = val;
        }

        private static RbTreeNode<Value> GetRight(RbTreeNode<Value> x)
        {
            return x.Right;
        }

        private static void SetRight(RbTreeNode<Value> x, RbTreeNode<Value> val)
        {
            x.Right = val;
        }

        //private static RbTreeNode<Value> GetParent(RbTreeNode<Value> x)
        //{
        //    return x.Parent;
        //}

        private static void SetParent(RbTreeNode<Value> x, RbTreeNode<Value> val)
        {
            x.Parent = val;
        }

        private static Value GetValue(RbTreeNode<Value> x)
        {
            return x.Value;
        }

        //private static void SetValue(RbTreeNode<Value> x, Value val)
        //{
        //    x.Value = val;
        //}

        private Key ExtractKey(RbTreeNode<Value> x)
        {
            return keyOfValue.Execute(GetValue(x));
        }

        //private static RbTreeNode<Value>.ColorType GetColor(RbTreeNode<Value> x)
        //{
        //    return x.Color;
        //}

        private static void SetColor(RbTreeNode<Value> x, RbTreeNode<Value>.ColorType color)
        {
            x.Color = color;
        }

        #endregion

        private RbTreeIterator<Value> Insert(RbTreeNode<Value> x, RbTreeNode<Value> y, Value v)
        {
            RbTreeNode<Value> z;

            if (y == header || x != null ||
                keyCompare.Execute(keyOfValue.Execute(v), ExtractKey(y)))
            {
                z = CreateNode(v);
                SetLeft(y, z); // also makes Leftmost() = z 
                //    when y == _M_header
                if (y == header)
                {
                    Root = z;
                    Rightmost = z;
                }
                else if (y == Leftmost)
                    Leftmost = z; // maintain Leftmost() pointing to min node
            }
            else
            {
                z = CreateNode(v);
                SetRight(y, z);
                if (y == Rightmost)
                    Rightmost = z; // maintain _M_rightmost() pointing to max node
            }
            SetParent(z, y);
            SetLeft(z, null);
            SetRight(z, null);
            Rebalance(z, ref header.Parent);
            ++NodeCount;
            return new RbTreeIterator<Value>(z);
        }

        private static void Erase(RbTreeNode<Value> x)
        {
// erase without rebalancing
            while (x != null)
            {
                Erase(GetRight(x));
                RbTreeNode<Value> y = GetLeft(x);
                DestroyNode(x);
                x = y;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is a fail safe implementation taken from the SGI STL!")]
        private static  RbTreeNode<Value> Copy(RbTreeNode<Value> x, RbTreeNode<Value> p)
        {
// structural copy.  x and p must be non-null.
            RbTreeNode<Value> __top = CloneNode(x);
            __top.Parent = p;

            try
            {
                if (x.Right != null)
                    __top.Right = Copy(GetRight(x), __top);
                p = __top;
                x = GetLeft(x);

                while (x != null)
                {
                    RbTreeNode<Value> y = CloneNode(x);
                    p.Left = y;
                    y.Parent = p;
                    if (x.Right != null)
                        y.Right = Copy(GetRight(x), y);
                    p = y;
                    x = GetLeft(x);
                }
            }
            catch
            {
                Erase(__top);
            }

            return __top;
        }

        private void EmptyInitialize()
        {
            SetColor(header, RbTreeNode<Value>.ColorType.Red); // used to distinguish header from 
            // __root, in iterator.operator++
            Root = null;
            Leftmost = header;
            Rightmost = header;
        }

        /// <summary>
        /// default ctor, initialzes an empty tree using func.less as
        /// the comparison functor
        /// </summary>

        #region Debugging

        private static int
            __black_count(RbTreeNode<Value> __node, RbTreeNode<Value> __root)
        {
            if (__node == null)
                return 0;
            else
            {
                int __bc = __node.Color == RbTreeNode<Value>.ColorType.Black ? 1 : 0;
                if (__node == __root)
                    return __bc;
                else
                    return __bc + __black_count(__node.Parent, __root);
            }
        }

        internal bool __rb_verify()
        {
            if (NodeCount == 0 || Equals(Begin(), End()))
                return NodeCount == 0 && Equals(Begin(), End())
                       && header.Left == header && header.Right == header;

            int __len = __black_count(Leftmost, Root);
            RbTreeIterator<Value> __end = End();
            for (RbTreeIterator<Value> __it = Begin(); !Equals(__it, __end); __it.PreIncrement())
            {
                RbTreeNode<Value> x = __it.Node;
                RbTreeNode<Value> __L = GetLeft(x);
                RbTreeNode<Value> __R = GetRight(x);

                if (x.Color == RbTreeNode<Value>.ColorType.Red)
                    if ((__L != null && __L.Color == RbTreeNode<Value>.ColorType.Red)
                        || (__R != null && __R.Color == RbTreeNode<Value>.ColorType.Red))
                        return false;

                if (__L != null && keyCompare.Execute(ExtractKey(x), ExtractKey(__L)))
                    return false;
                if (__R != null && keyCompare.Execute(ExtractKey(__R), ExtractKey(x)))
                    return false;

                if (__L == null && __R == null && __black_count(x, Root) != __len)
                    return false;
            }

            if (Leftmost != RbTreeNode<Value>.GetMinimum(Root))
                return false;
            if (Rightmost != RbTreeNode<Value>.GetMaximum(Root))
                return false;

            return true;
        } //__rb_verify

        #endregion

        //the data
        private RbTreeNode<Value> header;
        private int NodeCount; // keeps track of size of tree
        private IBinaryFunction<Key, Key, bool> keyCompare; //comparer
        private IUnaryFunction<Value, Key> keyOfValue; //key extractor

        #region ISerializable Members
        private const int Version = 1;
        protected RbTree(SerializationInfo info, StreamingContext ctxt)
        {
            int version = info.GetInt32("Version");
            Verify.VersionsAreEqual(version, Version);

            header = (RbTreeNode<Value>)info.GetValue("header", typeof(RbTreeNode<Value>));
            NodeCount = info.GetInt32("NodeCount");
            keyCompare = (IBinaryFunction<Key, Key, bool>)info.GetValue("keyCompare", typeof(IBinaryFunction<Key, Key, bool>));
            keyOfValue = (IUnaryFunction<Value, Key>)info.GetValue("keyOfValue", typeof(IUnaryFunction<Value, Key>));
        }
        /// <summary>
        /// See <see cref="ISerializable.GetObjectData"/> for details.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Version", Version);

            info.AddValue("header", header);
            info.AddValue("NodeCount", NodeCount);
            info.AddValue("keyCompare", keyCompare, typeof(IBinaryFunction<Key, Key, bool>));
            info.AddValue("keyOfValue", keyOfValue, typeof(IUnaryFunction<Value, Key>));
        }
        #endregion
    }
}
