//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
namespace DivisionEngine.Nodes.Math
{
    public class FloatNode : Node
    {
        public float Value { get; set; }

        public FloatNode()
        {
            Name = "Float";
            Value = 0f;
        }

        public override object? Evaluate()
        {
            return Value;
        }
    }
}
