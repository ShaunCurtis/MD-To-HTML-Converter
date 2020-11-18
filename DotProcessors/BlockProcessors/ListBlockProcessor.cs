using MD_To_HTML_Converter.Data;
using System.Text.RegularExpressions;

namespace MD_To_HTML_Converter.DotProcessors
{
    class ListBlockProcessor : BlockProcessor, IBlockProcessor
    {
        private string _unorderedExpression = @"^\s*(\-)\s(.*)";
        private string _orderedExpression = @"^\s*([0-9]+)\.\s(.*)";
        private DOTBlockType _blockType = DOTBlockType.OrderedListBlock;

        private DOTBlockType _linetype = DOTBlockType.ListItemBlock;

        public ListBlockProcessor()
        {
            this.Name = $"List Block Pre-Processor";
            this.Expressions.Add(_orderedExpression);
            this.Expressions.Add(_unorderedExpression);
            this.TextGroup = 2;
            this.Attributes.Add(1, "start");
        }

        public override bool Process(IDOTNode rootnode, MatchCollection matches, ref int index)
        {
            var ok = true;

            var basenode = rootnode.Nodes[index];
            if (Regex.IsMatch(basenode.Text, _unorderedExpression)) _blockType = DOTBlockType.UnOrderedListBlock;
            var match = matches[0];
            if (match.Groups.Count > 2)
            {
                basenode.Text = string.Empty;
                basenode.NodeType = DOTNodeType.Node;
                basenode.BlockType = _blockType;
                foreach (var attr in this.Attributes)
                {
                    if (!string.IsNullOrEmpty(match.Groups[attr.Key].Value)) basenode.SetAttribute(attr.Value, match.Groups[attr.Key].Value);
                }
                if (!string.IsNullOrEmpty(match.Groups[TextGroup].Value))
                {
                    basenode.AddNode(new DOTNode() { BlockType = _linetype, NodeType = DOTNodeType.Text, Text = match.Groups[TextGroup].Value });
                }
                bool islast;
                do
                {
                    index++;
                    if (index > rootnode.Nodes.Count) break;
                    var node = rootnode.Nodes[index];
                    islast = ProcessNextLine(node, out IDOTNode addnode);
                    if (addnode != null)
                    {
                        basenode.AddNode(addnode);
                        node.ProcessingType = DOTProcessingType.Remove;
                    }
                } while (!islast);
                index--;
                LineProcessNodes(basenode);
            }
            return ok;
        }

        private bool ProcessNextLine(IDOTNode node, out IDOTNode newnode)
        {
            var islast = true;
            newnode = null;
            foreach (var expression in this.Expressions)
            {
                if (Regex.IsMatch(node.Text, expression))
                {
                    islast = false;
                    MatchCollection matches = Regex.Matches(node.Text, expression);
                    var match = matches[0];
                    if (!string.IsNullOrEmpty(match.Groups[TextGroup].Value))
                    {
                        newnode = new DOTNode() { BlockType = _linetype, NodeType = DOTNodeType.Text, Text = match.Groups[TextGroup].Value };
                    }
                    break;
                }
            }
            return islast;
        }

    }
}
