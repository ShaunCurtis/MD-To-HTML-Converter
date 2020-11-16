using MD_To_HTML_Converter.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace MD_To_HTML_Converter.DotProcessors
{
    class HeaderPreProcessor : DOTPreProcessor, IDOTPreProcessor
    {
        public HeaderPreProcessor()
        {
            this.Name = "Header Pre-Processor";
    }

    public override bool Process(DocumentObjectTree Dot)
        {
            var ok = true;
            foreach (var node in Dot.RootNode.Nodes)
            {
                if (node.Value.NodeType == DOTNodeType.Raw)
                {
                    var reg = new Regex(@"([#]+)\s(.+)");
                    if (reg.IsMatch(node.Value.Text)) {
                        MatchCollection matches = reg.Matches(node.Value.Text);
                        node.Value.BlockType = DOTBlockType.HeadingBlock;
                        node.Value.ProcessingType = DOTProcessingType.HeadingListBlock;
                        if (matches[0].Groups.Count == 3)
                        {
                            node.Value.SetAttribute("Level", matches[0].Groups[1].Value.Length);
                            node.Value.Text = matches[0].Groups[2].Value;
                            node.Value.NodeType = DOTNodeType.Text;
                        }
                        else
                        {
                            node.Value.ProcessingType = DOTProcessingType.Remove;
                            node.Value.NodeType = DOTNodeType.Node;
                        }

                    }
                }
            }
            var list = Dot.RootNode.Nodes.Where(item => item.Value.ProcessingType == DOTProcessingType.Remove).ToList();
            list.ForEach(item => Dot.RootNode.DeleteNode(item.Key));
            return ok;
        }
    }
}
