//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
namespace DivisionEngine.Nodes.Math
{
    public class AddNode : Node
    {
        public AddNode()
        {
            Name = "Add";
        }

        public override object? Evaluate()
        {
            // In a real implementation, you'd traverse connections
            // For now, just return a placeholder
            return 0f;
        }
    }
}
