using MD_To_HTML_Converter.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace MD_To_HTML_Converter.DotProcessors
{
    class ExpressionLineProcessor : ILineProcessor
    {
        public string Name => $"{this.BlockType} Processor";

        public string Expression { get; set; } = string.Empty;

        public DOTBlockType BlockType { get; set; } = DOTBlockType.None;

        public int TextGroup { get; set; } = 2;

        public Dictionary<int, string> Attributes { get; set; } = new Dictionary<int, string>();

        private int PostTextGroupPosition => (Attributes?.Count ?? 0) + 3; 

        public bool Process(DocumentObjectTree Dot)
        {
            foreach (var node in Dot.RootNode.Nodes)
            {
                {
                    ProcessNode(node.Value);
                }
            }
            return true;
        }

        public bool ProcessNode(IDOTNode node)
        {
            var ok = true;

            var reg = new Regex(this.Expression);
            if (reg.IsMatch(node.Text))
            {
                MatchCollection matches = reg.Matches(node.Text);
                for (int i = 0; i < matches.Count; i++)
                {
                    var match = matches[i];
                    if (match.Groups.Count > 2)
                    {
                        node.Text = string.Empty;
                        node.NodeType = DOTNodeType.Node;
                         node.AddNode(new DOTNode() { BlockType = DOTBlockType.TextBlock, NodeType = DOTNodeType.Text, Text = match.Groups[1].Value });
                        var newnode = (IDOTNode)new DOTNode() { BlockType = this.BlockType, NodeType = DOTNodeType.Text, Text = match.Groups[2].Value };
                        foreach (var attr in this.Attributes)
                        {
                            if (!string.IsNullOrEmpty(match.Groups[attr.Key].Value)) newnode.SetAttribute(attr.Value, match.Groups[attr.Key].Value);
                        }
                        node.AddNode(newnode);
                        if (match.Groups.Count == (PostTextGroupPosition + 1) && !string.IsNullOrEmpty(match.Groups[3].Value)) node.AddNode(new DOTNode() { BlockType = DOTBlockType.TextBlock, NodeType = DOTNodeType.Text, Text = match.Groups[PostTextGroupPosition].Value });
                    }
                }
            }
            // Check to see if er have sub nodes and if so process them
            if (node.Nodes.Count > 0)
            {
                foreach (var n in node.Nodes)
                {
                    ProcessNode(n.Value);
                }
            }

            return ok;
        }
    }
}
