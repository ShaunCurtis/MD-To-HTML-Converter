using MD_To_HTML_Converter.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace MD_To_HTML_Converter.DotProcessors
{
    class ListPreProcessor : DOTPreProcessor, IDOTPreProcessor
    {

        public override string Name => "List Pre-Processor";

        public ListPreProcessor()
        {
            this.Expressions.Add(DOTProcessingType.StartOrderedListBlock, @"^\s*([0-9]+)\.\s(.*)");
            this.Expressions.Add(DOTProcessingType.StartUnOrderedListBlock, @"^\s*(\*)\s(.*)");
        }


        protected override void PreProcess(DocumentObjectTree Dot)
        {
            var blocktype = DOTBlockType.None;
            foreach (var node in Dot.RootNode.Nodes)
            {
                if (node.Value.NodeType == DOTNodeType.Raw)
                {
                    foreach (var exp in this.Expressions)
                    {
                        if (PreProcessLine(node, blocktype, exp.Value, exp.Key))
                        {
                            blocktype = node.Value.BlockType;
                            break;
                        }
                        blocktype = node.Value.BlockType;
                    }
                }
            }
        }

        protected bool PreProcessLine(KeyValuePair<int, IDOTNode> node, DOTBlockType blockType, string exp, DOTProcessingType startType )
        {
            var processed = false;

            DOTProcessingType lineType = DOTProcessingType.None; 
            if (blockType == DOTBlockType.OrderedListBlock) lineType = DOTProcessingType.OrderedListBlock;
            if (blockType == DOTBlockType.UnOrderedListBlock) lineType = DOTProcessingType.UnOrderedListBlock;
            var islist = lineType == DOTProcessingType.UnOrderedListBlock || lineType == DOTProcessingType.OrderedListBlock;
            if (node.Value.NodeType == DOTNodeType.Raw)
            {
                var reg = new Regex(exp);
                if (reg.IsMatch(node.Value.Text))
                {
                    processed = true;
                    MatchCollection matches = reg.Matches(node.Value.Text);
                    if (islist)
                    {
                        node.Value.BlockType = blockType;
                        node.Value.ProcessingType = lineType;
                    }
                    else
                    {
                        node.Value.ProcessingType = startType;
                        if (int.TryParse(matches[0].Groups[1].Value, out int value))
                        {
                            node.Value.SetAttribute("start", value);
                            node.Value.BlockType = DOTBlockType.OrderedListBlock;
                        }
                        else node.Value.BlockType = DOTBlockType.UnOrderedListBlock;
                    }
                    if (matches[0].Groups.Count == 3) node.Value.Text = matches[0].Groups[2].Value;
                }
            }
            return processed;
        }

        protected override void ReProcess(DocumentObjectTree Dot)
        {
            IDOTNode masterNode = null;
            foreach (var node in Dot.RootNode.Nodes)
            {

                if (node.Value.ProcessingType == DOTProcessingType.StartOrderedListBlock || node.Value.ProcessingType == DOTProcessingType.StartUnOrderedListBlock)
                {
                    masterNode = node.Value;
                    if (node.Value.ProcessingType == DOTProcessingType.StartOrderedListBlock) masterNode.BlockType = DOTBlockType.OrderedListBlock;
                    else masterNode.BlockType = DOTBlockType.UnOrderedListBlock;
                    var newnode = new DOTNode() { BlockType = DOTBlockType.ListItemBlock, NodeType = DOTNodeType.Text, Text = node.Value.Text };
                    node.Value.Text = string.Empty;
                    node.Value.NodeType = DOTNodeType.Node;
                    node.Value.Nodes.Add(0, newnode);
                }
                else if (node.Value.ProcessingType == DOTProcessingType.OrderedListBlock || node.Value.ProcessingType == DOTProcessingType.UnOrderedListBlock)
                {
                    var newnode = (IDOTNode)node.Value.Clone();
                    newnode.BlockType = DOTBlockType.ListItemBlock;
                    newnode.NodeType = DOTNodeType.Text;
                    if (masterNode != null) masterNode.AddNode(newnode);
                    node.Value.ProcessingType = DOTProcessingType.Remove;
                }
            }
        }
    }
}
