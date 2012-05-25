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
using System.Linq;
using System.Collections.Generic;
using NStl.Iterators;
using NStl.Linq;

namespace NStl
{

    public static partial class Algorithm
    {
        /// <summary>
        /// <para>
        /// Sorts the input range toplogically, that is that each item is placed after all its predecessing
        /// items in the graph. This algorithm is similar to unix's tsort.
        /// </para>
        /// </summary>
        /// <typeparam name="GraphNode"></typeparam>
        /// <typeparam name="InIt"></typeparam>
        /// <typeparam name="OutIt"></typeparam>
        /// <param name="first">
        /// An <see cref="IInputIterator{T}"/> implementation pointing to the first element
        /// of the input range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IInputIterator{T}"/> implementation pointing one past the final element
        /// of the input range.
        /// </param>
        /// <param name="target">
        /// An <see cref="IOutputIterator{T}"/> implemetation that is the target for the sorted elements.
        /// </param>
        /// <param name="parents"></param>
        /// <returns>
        /// An <see cref="IOutputIterator{T}"/> implemetation pointing one past the final
        /// sorted element in the destination range.
        /// </returns>
        public static OutIt TopologicalSort<GraphNode, InIt, OutIt>(InIt first, InIt last, OutIt target, IUnaryFunction<GraphNode,IEnumerable<GraphNode>> parents) 
            where InIt: IInputIterator<GraphNode> 
            where OutIt: IOutputIterator<GraphNode>
        {
            target = (OutIt)target.Clone();
            foreach (GraphNode graphNode in TopologicalSort(first.AsEnumerable(last), parents.Execute))
            {
                target.Value = graphNode;
                target.PreIncrement();
            }
            return target;
        }
        /// <summary>
        /// <para>
        /// Sorts the input range toplogically, that is that each item is placed after all its predecessing
        /// items in the graph. This algorithm is similar to unix's tsort.
        /// </para>
        /// </summary>
        /// <typeparam name="GraphNode"></typeparam>
        /// <param name="graph"></param>
        /// <param name="predecessors"></param>
        /// <returns></returns>
        public static IEnumerable<GraphNode> TopologicalSort<GraphNode>(IEnumerable<GraphNode> graph, Func<GraphNode, IEnumerable<GraphNode>> predecessors)
        {
            List<KeyValuePair<GraphNode, List<GraphNode>>> graphStruct =
                new List<KeyValuePair<GraphNode, List<GraphNode>>>();

            LinkedList<GraphNode> graphNodesWithoutIncomingEdges = new LinkedList<GraphNode>();

            foreach (GraphNode node in graph)
            {
                IEnumerable<GraphNode> preds = predecessors(node);
                graphStruct.Add(new KeyValuePair<GraphNode, List<GraphNode>>(node, new List<GraphNode>(preds)));
                if (!preds.Any())
                    graphNodesWithoutIncomingEdges.AddLast(node);
            }


            while (graphNodesWithoutIncomingEdges.Any())
            {
                GraphNode n = graphNodesWithoutIncomingEdges.First();
                graphNodesWithoutIncomingEdges.RemoveFirst();

                //sortedGraphNodes.Add(n);

                foreach (KeyValuePair<GraphNode, List<GraphNode>> m in graphStruct.Where(x => x.Value.Contains(n)))
                {
                    m.Value.Remove(n);
                    if (!m.Value.Any())
                        graphNodesWithoutIncomingEdges.AddLast(m.Key);
                }
                yield return n;
            }

            // cycles??
            if (graphStruct.Select(x => x.Value).Where(x => x.Any()).Any())
                throw new ArgumentException(Resource.GraphHasAtLeastOneCycle);

            yield break;
        }
    }
}
