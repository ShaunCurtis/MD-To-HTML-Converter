using MD_To_HTML_Converter.Data;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace MD_To_HTML_Converter.DotProcessors
{
    class CodeBlockProcessor : BlockProcessor, IBlockProcessor
    {
        public CodeBlockProcessor()
        {
            this.Name = $"Code Block Pre-Processor";
            this.Expressions.Add(@"(.*)[\`]{3}(\S*)\s*(.*)");
            this.LineExpressions.Clear();
            this.EndExpressions.Add(@"(.*)[\`]{3}");
            this.BlockType = DOTBlockType.CodeBlock;
            this.LineType = DOTBlockType.CodeLine;
            this.IsMultiline = true;
            this.TextGroup = 3;
            this.Attributes.Add(2, "lang");
            this.LineProcessors = new SortedList<int, ILineProcessor>();
        }

        public override bool Process(IDOTNode rootnode, MatchCollection matches, ref int index)
        {
            return base.Process(rootnode, matches, ref index);
        }

        //public override bool Process(IDOTNode rootnode, MatchCollection matches, ref int index)
        //{
        //    var ok = true;

        //    var basenode = rootnode.Nodes[index];
        //    var match = matches[0];
        //    if (match.Groups.Count > 2)
        //    {
        //        basenode.Text = string.Empty;
        //        basenode.NodeType = DOTNodeType.Node;
        //        basenode.BlockType = DOTBlockType.CodeBlock;
        //        foreach (var attr in this.Attributes)
        //        {
        //            if (!string.IsNullOrEmpty(match.Groups[attr.Key].Value)) basenode.SetAttribute(attr.Value, match.Groups[attr.Key].Value);
        //        }
        //        if (!string.IsNullOrEmpty(match.Groups[PostTextGroup].Value))
        //        {
        //            basenode.AddNode(new DOTNode() { BlockType = DOTBlockType.CodeLine, NodeType = DOTNodeType.Text, Text = match.Groups[PostTextGroup].Value });
        //        }
        //        bool islast;
        //        do
        //        {
        //            index++;
        //            var node = rootnode.Nodes[index];
        //            islast = ProcessNextLine(node, out IDOTNode addnode);
        //            if (addnode != null)
        //            {
        //                basenode.AddNode(addnode);
        //            }
        //            node.ProcessingType = DOTProcessingType.Remove;
        //        } while (!islast);
        //    }
        //    return ok;
        //}

        //private bool ProcessNextLine(IDOTNode node, out IDOTNode newnode)
        //{
        //    var islast = false;
        //    newnode = null;
        //    bool ismatch = false;
        //    foreach (var expression in this.Expressions)
        //    {
        //        var reg = new Regex(expression);
        //        if (reg.IsMatch(node.Text))
        //        {
        //            ismatch = true;
        //            islast = true;
        //            MatchCollection matches = reg.Matches(node.Text);
        //            var match = matches[0];
        //            if (!string.IsNullOrEmpty(match.Groups[PreTextGroup].Value))
        //            {
        //                newnode = new DOTNode() { BlockType = DOTBlockType.CodeLine, NodeType = DOTNodeType.Text, Text = match.Groups[PreTextGroup].Value };
        //            }
        //        }
        //    }
        //    if (!ismatch) newnode = new DOTNode() { BlockType = DOTBlockType.CodeLine, NodeType = DOTNodeType.Text, Text = node.Text };
        //    return islast;
        //}
    }
}
