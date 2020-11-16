using MD_To_HTML_Converter.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace MD_To_HTML_Converter.DotProcessors
{
    class ListPreProcessor : IDOTPreProcessor
    {

        public string Name => "List Pre-Processor";

        public bool Process(DocumentObjectTree Dot)
        {
            var ok = true;
            var isList = false;
            var blocktype = DOTBlockType.None;
            var expressions = new Dictionary<DOTProcessingType, string>();

            expressions.Add(DOTProcessingType.StartOrderedListBlock, @"^\s*([0-9]+)\.\s(.*)");
            expressions.Add(DOTProcessingType.StartUnOrderedListBlock, @"^\s*(\*)\s(.*)");
            foreach (var node in Dot.RootNode.Nodes)
            {
                if (node.Value.DOTType == DOTNodeType.Raw)
                {
                    foreach (var exp in expressions)
                    {
                        blocktype = ProcessLine(node, blocktype,  exp.Value, exp.Key);
                    }
                    //var reg = new Regex(@"([0-9]+)\.\s(.*)");
                    //if (reg.IsMatch(node.Value.Text))
                    //{
                    //    MatchCollection matches = reg.Matches(node.Value.Text);
                    //    if (isList) node.Value.ProcessingType = DOTProcessingType.OrderedListBlock;
                    //    else
                    //    {
                    //        isList = true;
                    //        node.Value.ProcessingType = DOTProcessingType.StartOrderedListBlock;
                    //        node.Value.SetAttribute("START", int.Parse(matches[0].Groups[1].Value));
                    //    }
                    //    if (matches[0].Groups.Count == 3)
                    //    {
                    //        node.Value.Text = matches[0].Groups[2].Value;
                    //    }
                    //}
                    //else isList = false;
                }
            }
            foreach (var node in Dot.RootNode.Nodes)
            {
                if (node.Value.DOTType == DOTNodeType.Raw)
                {
                    var reg = new Regex(@"(\*)\s(.*)");
                    if (reg.IsMatch(node.Value.Text))
                    {
                        MatchCollection matches = reg.Matches(node.Value.Text);
                        node.Value.ProcessingType = isList ? DOTProcessingType.UnOrderedListBlock : DOTProcessingType.StartUnOrderedListBlock;
                        isList = !isList;
                        if (matches[0].Groups.Count == 3)
                        {
                            node.Value.Text = matches[0].Groups[2].Value;
                        }
                    }
                }
            }
            IDOTNode masterNode = null;
            foreach (var node in Dot.RootNode.Nodes)
            {
                if (node.Value.ProcessingType == DOTProcessingType.StartOrderedListBlock || node.Value.ProcessingType == DOTProcessingType.StartUnOrderedListBlock)
                {
                    isList = true;
                    masterNode = node.Value;
                    if (node.Value.ProcessingType == DOTProcessingType.StartOrderedListBlock) masterNode.Name = "OL";
                    else masterNode.Name = "UL";
                    var newnode = new DOTNode() { Name = "LI", DOTType = DOTNodeType.Text , Text = node.Value.Text};
                    node.Value.Text = string.Empty;
                    node.Value.Nodes.Add(0, newnode);
                }
                else if (node.Value.ProcessingType == DOTProcessingType.OrderedListBlock || node.Value.ProcessingType == DOTProcessingType.UnOrderedListBlock)
                {
                    isList = false;
                    var newnode = (IDOTNode)node.Value.Clone();
                    newnode.Name = "LI";
                    newnode.DOTType = DOTNodeType.Text;
                    if (masterNode != null) masterNode.AddNode(newnode);
                    node.Value.ProcessingType = DOTProcessingType.Remove;
                }
            }
            var list = Dot.RootNode.Nodes.Where(item => item.Value.ProcessingType == DOTProcessingType.Remove).ToList();
            list.ForEach(item => Dot.RootNode.DeleteNode(item.Key));
            return ok;
        }

        DOTBlockType ProcessLine(KeyValuePair<int, IDOTNode> node, DOTBlockType blockType, string exp, DOTProcessingType startType )
        {
            DOTProcessingType lineType = DOTProcessingType.None; 
            if (blockType == DOTBlockType.OrderedListBlock) lineType = DOTProcessingType.OrderedListBlock;
            if (blockType == DOTBlockType.UnOrderedListBlock) lineType = DOTProcessingType.UnOrderedListBlock;
            var islist = lineType == DOTProcessingType.UnOrderedListBlock || lineType == DOTProcessingType.OrderedListBlock;
            if (node.Value.DOTType == DOTNodeType.Raw)
            {
                var reg = new Regex(exp);
                if (reg.IsMatch(node.Value.Text))
                {
                    MatchCollection matches = reg.Matches(node.Value.Text);
                    if (islist) node.Value.ProcessingType = lineType;
                    else
                    {
                        node.Value.ProcessingType =  startType;
                        node.Value.SetAttribute("START", int.Parse(matches[0].Groups[1].Value));
                    }
                    if (matches[0].Groups.Count == 3) node.Value.Text = matches[0].Groups[2].Value;
                }
                else blockType = DOTBlockType.None;
            }
            return blockType;
        }
    }
}
