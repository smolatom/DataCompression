namespace DataCompression.Huffman
{
    internal class Node
    {
        protected internal Node LeftChild { get; set; }
        protected internal Node RightChild { get; set; }

        protected internal Node Parent { get; set; }
        protected internal byte Index { get; set; }
        protected internal byte Value { get; set; }
        protected internal uint Occurences { get; set; }

        protected internal Node(byte index)
        {
            Index = index;
        }

        protected internal Node(Node parent, byte index, byte value)
        {
            Parent = parent;
            Index = index;
            Value = value;
            Occurences = 1;
        }
    }
}
