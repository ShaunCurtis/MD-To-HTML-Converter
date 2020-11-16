using MD_To_HTML_Converter.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace MD_To_HTML_Converter.DotProcessors
{
    class CodePreProcessor : DOTPreProcessor
    {

        public CodePreProcessor()
        {
        Name = "Code Block Pre-Processor";

    }

    public override bool Process(DocumentObjectTree Dot)
        {
            var ok = true;
            var isCode = false;
            foreach (var node in Dot.RootNode.Nodes)
            {
                if (node.Value.NodeType == DOTNodeType.Raw)
                {
                    if (isCode)
                    {
                        node.Value.ProcessingType = DOTProcessingType.CodeBlock;
                        node.Value.BlockType = DOTBlockType.CodeBlock;
                        node.Value.NodeType = DOTNodeType.Text;
                    }
                    if (node.Value.Text.StartsWith("```"))
                    {
                        node.Value.ProcessingType =  isCode ?  DOTProcessingType.EndCodeBlock: DOTProcessingType.StartCodeBlock ;
                        node.Value.BlockType = DOTBlockType.CodeBlock;
                        node.Value.NodeType = DOTNodeType.Text;
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
                    masterNode.BlockType = DOTBlockType.CodeBlock;
                    var lang = node.Value.Text.Replace("```", "");
                    if (!string.IsNullOrEmpty(lang)) node.Value.SetAttribute("lang", lang);
                    node.Value.Text = string.Empty;
                    node.Value.NodeType = DOTNodeType.Node;
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
            CleanUp(Dot);
            return ok;
        }
    }
}
