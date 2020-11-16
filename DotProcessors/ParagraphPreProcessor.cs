using MD_To_HTML_Converter.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace MD_To_HTML_Converter.DotProcessors
{
    class ParagraphPreProcessor : IDOTPreProcessor
    {

        public string Name => "Paragraph Pre-Processor";

        public bool Process(DocumentObjectTree Dot)
        {
            var ok = true;
            foreach (var node in Dot.RootNode.Nodes)
            {
                if (node.Value.DOTType == DOTNodeType.Raw)
                {
                    node.Value.DOTType = DOTNodeType.Text;
                    node.Value.Name = "P";
                }
            }
            return ok;
        }
    }
}
