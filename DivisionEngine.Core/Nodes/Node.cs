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
    /// Base node that can exist in both editor and runtime.
    /// </summary>
    public abstract class Node
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public float X { get; set; }
        public float Y { get; set; }

        // Simple connection storage: output node id -> input node id
        public List<(string OutputId, string InputId)> Connections { get; set; }

        protected Node()
        {
            Id = Guid.NewGuid().ToString();
            Name = "Node";
            Connections = new List<(string, string)>();
        }

        public abstract object? Evaluate();
    }
}
