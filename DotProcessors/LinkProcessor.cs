using MD_To_HTML_Converter.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MD_To_HTML_Converter.DotProcessors
{
    class LinkProcessor : IDOTProcessor
    {
        public string Name => $"{this.BlockType} Processor";

        public string Expression = @"(.*)[\[]{1}(.+)[\]]{1}\s*[\(]{1}(.+)[\)]{1}(.*)";

        public DOTBlockType BlockType = DOTBlockType.LinkBlock;

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
                        node.Text = string.Empty;
                        // check if we have pre text to link and add text node if we do
                        if (!string.IsNullOrEmpty(match.Groups[1].Value)) node.AddNode(new DOTNode() { BlockType = DOTBlockType.TextBlock, NodeType = DOTNodeType.Text, Text = match.Groups[1].Value });
                        // Add Link Node
                        var newnode = (IDOTNode)new DOTNode() { BlockType = this.BlockType, NodeType = DOTNodeType.Text, Text = match.Groups[2].Value };
                        newnode.SetAttribute("URL", match.Groups[3].Value);
                        node.AddNode(newnode);
                        // check if we have post text to link and add text node if we do
                        if (match.Groups.Count == 4 && !string.IsNullOrEmpty(match.Groups[3].Value)) node.AddNode(new DOTNode() { BlockType = DOTBlockType.TextBlock, NodeType = DOTNodeType.Text, Text = match.Groups[3].Value });
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
