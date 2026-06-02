//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
namespace DivisionEngine.Nodes
{
    /// <summary>
    /// Graph containing nodes - works in both editor and runtime.
    /// </summary>
    public class NodeGraph
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<Node> Nodes { get; set; }

        public NodeGraph(string name)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            Nodes = new List<Node>();
        }

        public void AddNode(Node node)
        {
            Nodes.Add(node);
        }

        public void RemoveNode(string nodeId)
        {
            var node = Nodes.Find(n => n.Id == nodeId);
            if (node != null)
                Nodes.Remove(node);
        }

        public object? Evaluate()
        {
            // Simple evaluation - can be expanded later
            foreach (var node in Nodes)
            {
                node.Evaluate();
            }
            return null;
        }
    }
}
