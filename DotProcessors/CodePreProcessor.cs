using MD_To_HTML_Converter.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace MD_To_HTML_Converter.DotProcessors
{
    class CodePreProcessor : IDOTPreProcessor
    {

        public string Name => "Code Block Pre-Processor";

        public bool Process(DocumentObjectTree Dot)
        {
            var ok = true;
            var isCode = false;
            foreach (var node in Dot.RootNode.Nodes)
            {
                if (node.Value.DOTType == DOTNodeType.Raw)
                {
                    if (isCode)
                    {
                        node.Value.ProcessingType = DOTProcessingType.CodeBlock;
                        node.Value.DOTType = DOTNodeType.Code;
                    }
                    if (node.Value.Text.StartsWith("```"))
                    {
                        node.Value.ProcessingType =  isCode ?  DOTProcessingType.EndCodeBlock: DOTProcessingType.StartCodeBlock ;
                        node.Value.DOTType = DOTNodeType.Code;
                        isCode = !isCode;
                    }
                }
            }
            IDOTNode masterNode = null;
            foreach (var node in Dot.RootNode.Nodes)
            {
                if (node.Value.ProcessingType == DOTProcessingType.StartCodeBlock)
                {
                    isCode = true;
                    masterNode = node.Value;
                    masterNode.Name = "PRE";
                    var lang = node.Value.Text.Replace("```", "");
                    if (!string.IsNullOrEmpty(lang)) node.Value.SetAttribute("lang", lang);
                    node.Value.Text = string.Empty;
                }
                else if (node.Value.ProcessingType == DOTProcessingType.EndCodeBlock)
                {
                    isCode = false;
                    node.Value.ProcessingType = DOTProcessingType.Remove;
                }
                else if (node.Value.ProcessingType == DOTProcessingType.CodeBlock)
                {
                    if (masterNode != null) masterNode.AddNode((IDOTNode)node.Value.Clone());
                    node.Value.ProcessingType = DOTProcessingType.Remove;
                }
            }
            var list = Dot.RootNode.Nodes.Where(item => item.Value.ProcessingType == DOTProcessingType.Remove).ToList();
            list.ForEach(item => Dot.RootNode.DeleteNode(item.Key));
            return ok;
        }
    }
}
