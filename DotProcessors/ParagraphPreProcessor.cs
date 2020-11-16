using MD_To_HTML_Converter.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace MD_To_HTML_Converter.DotProcessors
{
    class ParagraphPreProcessor : DOTPreProcessor
    {
        public ParagraphPreProcessor()
        {
            Name = "Paragraph Pre-Processor";
        }


        public override bool Process(DocumentObjectTree Dot)
        {
            var ok = true;
            foreach (var node in Dot.RootNode.Nodes)
            {
                if (node.Value.NodeType == DOTNodeType.Raw)
                {
                    node.Value.NodeType = DOTNodeType.Text;
                    node.Value.BlockType = DOTBlockType.ParagraphBlock;
                }
            }
            return ok;
        }
    }
}
