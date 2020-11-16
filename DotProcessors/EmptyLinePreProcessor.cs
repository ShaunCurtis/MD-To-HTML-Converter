using MD_To_HTML_Converter.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace MD_To_HTML_Converter.DotProcessors
{
    class EmptyLinePreProcessor : DOTPreProcessor
    {


        public EmptyLinePreProcessor()
        {
            Name = "Empty Line Pre-Processor";
        }

        protected override void PreProcess(DocumentObjectTree Dot)
        {
            foreach (var node in Dot.RootNode.Nodes)
            {
                if (node.Value.NodeType == DOTNodeType.Raw)
                {
                    if (string.IsNullOrWhiteSpace(node.Value.Text)) node.Value.ProcessingType = DOTProcessingType.Remove;
                }
            }
        }
    }
}
