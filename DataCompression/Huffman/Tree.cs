using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCompression.Huffman
{
    class Tree
    {
        public Node Root { get; set; }

        public Tree()
        {
            NYT = createNYT(null, MAXIMAL_INDEX);
            Root = NYT;
            actualIndex = MAXIMAL_INDEX;
            nodesInTree = new Dictionary<byte, Node>();
            occurenceBlocks = new Dictionary<uint, List<Node>>();
            addNewNodeToBlock(Root);
        }

        private const byte MAXIMAL_INDEX = 127;
        private byte actualIndex;
        private readonly Dictionary<byte, Node> nodesInTree;
        private readonly Dictionary<uint, List<Node>> occurenceBlocks;
        private Node NYT;

        public bool Contains(byte charAsciCode)
        {
            return nodesInTree.ContainsKey(charAsciCode);
        }

        public bool[] Add(byte asciiCode)
        {
            if (Contains(asciiCode))
            {
                var existingChar = nodesInTree[asciiCode];
                var code = getCode(existingChar).ToArray();
                updateTree(existingChar);
                return code;
            }
            else //Vznik dvou nových uzlů z NYTu: uzel pro nový znak a nový NYT
            {
                var code = NYT == Root ? getAsciCode(asciiCode) : getNYTCode().Concat(getAsciCode(asciiCode));

                var newChar = new Node(NYT, --actualIndex, asciiCode);
                nodesInTree.Add(newChar.Value, newChar);
                addNewNodeToBlock(newChar);
                NYT.RightChild = newChar;
                occurenceBlocks[NYT.Occurences].Remove(NYT);
                NYT.Occurences++;
                addNewNodeToBlock(NYT);
                NYT.LeftChild = createNYT(NYT, --actualIndex);
                NYT = NYT.LeftChild;
                var processedChar = NYT.Parent;
                if (processedChar == Root) //konec, vrátit pouze zakódované ascii
                    return code.ToArray();
                updateTree(processedChar.Parent);
                return code.ToArray();
            }
        }

        private bool[] getAsciCode(byte asciiCode)
        {
            var bits = new BitArray(new[] { asciiCode });
            var code = new List<bool>();
            for (int i = 0; i < 7; i++)
            {
                code.Add(bits[i]);
            }
            code.Reverse();
            return code.ToArray();
        }

        private bool[] getAsciCode(Node newNode)
        {
            var bits = new BitArray(new[] { newNode.Value });
            var code = new List<bool>();
            for (int i = 0; i < 7; i++)
            {
                code.Add(bits[i]);
            }
            code.Reverse();
            if (nodesInTree.Count != 1)
                code.InsertRange(0, getNYTCode());
            return code.ToArray();
        }

        private IEnumerable<bool> getNYTCode()
        {
            return getCode(NYT);
        }

        private IEnumerable<bool> getCode(Node node)
        {
            return getParentCode(node);
        }

        private IEnumerable<bool> getParentCode(Node node)
        {
            var code = new List<bool>();
            if (node.Parent != Root)
            {
                var parentCode = getParentCode(node.Parent);
                code.AddRange(parentCode);
            }
            var oneBitCode = node.Parent.RightChild == node;
            code.Add(oneBitCode);
            return code;
        }

        private void addNewNodeToBlock(Node newNode)
        {
            if (!occurenceBlocks.ContainsKey(newNode.Occurences))
                occurenceBlocks.Add(newNode.Occurences, new List<Node>());
            occurenceBlocks[newNode.Occurences].Add(newNode);
        }

        private void updateTree(Node processedNode)
        {
            while (true)
            {
                var block = occurenceBlocks[processedNode.Occurences];
                var nodeWithMaxIndexInBlock = block.OrderByDescending(x => x.Index).First();
                if (processedNode.Index != nodeWithMaxIndexInBlock.Index)
                    swapNodes(nodeWithMaxIndexInBlock, processedNode);
                occurenceBlocks[processedNode.Occurences].Remove(processedNode);
                processedNode.Occurences++;
                addNewNodeToBlock(processedNode);

                if (processedNode.Parent != null)
                {
                    processedNode = processedNode.Parent;
                    continue;
                }
                break;
            }
        }

        private static void swapNodes(Node nodeWithMaxIndexInBlock, Node processedNode)
        {
            var parentOfNodeWithMaxIndexInBlock = nodeWithMaxIndexInBlock.Parent;
            var parentOfProcessedNode = processedNode.Parent;
            if (parentOfNodeWithMaxIndexInBlock.LeftChild == nodeWithMaxIndexInBlock)
                parentOfNodeWithMaxIndexInBlock.LeftChild = processedNode;
            else
                parentOfNodeWithMaxIndexInBlock.RightChild = processedNode;
            processedNode.Parent = parentOfNodeWithMaxIndexInBlock;
            if (parentOfProcessedNode.LeftChild == processedNode)
                parentOfProcessedNode.LeftChild = nodeWithMaxIndexInBlock;
            else
                parentOfProcessedNode.RightChild = nodeWithMaxIndexInBlock;
            nodeWithMaxIndexInBlock.Parent = parentOfProcessedNode;

            var processedNodeIndex = processedNode.Index;
            processedNode.Index = nodeWithMaxIndexInBlock.Index;
            nodeWithMaxIndexInBlock.Index = processedNodeIndex;
        }

        private Node createNYT(Node parent, byte index)
        {
            return new Node(index) { Parent = parent };
        }
    }
}
