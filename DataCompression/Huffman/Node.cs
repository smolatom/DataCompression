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

        protected internal bool IsCharacterLeaf { get { return isLeaf && Occurences != 0; } }

        protected internal bool IsNYT { get { return isLeaf && Occurences == 0 && Value == 0; } }

        private bool isLeaf { get { return LeftChild == null && RightChild == null; } }

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
