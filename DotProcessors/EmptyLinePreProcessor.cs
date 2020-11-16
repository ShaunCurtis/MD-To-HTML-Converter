using MD_To_HTML_Converter.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace MD_To_HTML_Converter.DotProcessors
{
    class EmptyLinePreProcessor : IDOTPreProcessor
    {

        public string Name => "Empty Line Pre-Processor";

        public bool Process(DocumentObjectTree Dot)
        {
            var ok = true;
            foreach (var node in Dot.RootNode.Nodes)
            {
                if (node.Value.DOTType == DOTNodeType.Raw)
                {
                    if (string.IsNullOrWhiteSpace(node.Value.Text)) node.Value.ProcessingType = DOTProcessingType.Remove;
                }
            }
            var list = Dot.RootNode.Nodes.Where(item => item.Value.ProcessingType == DOTProcessingType.Remove).ToList();
            list.ForEach(item => Dot.RootNode.DeleteNode(item.Key));
            return ok;
        }
    }
}
