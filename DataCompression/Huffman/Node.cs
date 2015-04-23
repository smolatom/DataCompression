using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCompression.Huffman
{
    public class Node
    {
        public Node LeftChild { get; set; }
        public Node RightChild { get; set; }

        public Node Parent { get; set; }
        public byte Index { get; set; }
        public byte Value { get; set; }
        public uint Occurences { get; set; }

        public Node(byte index)
        {
            Index = index;
        }

        public Node(Node parent, byte index, byte value)
        {
            Parent = parent;
            Index = index;
            Value = value;
            Occurences = 1;
        }
    }
}
