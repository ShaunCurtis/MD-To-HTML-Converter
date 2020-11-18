using MD_To_HTML_Converter.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace MD_To_HTML_Converter.DotProcessors
{
    class BlockProcessor : IBlockProcessor
    {
        public string Name { get; set; } = $"Code Pre-Processor";

        public List<string> Expressions { get; set; } = new List<string>();

        public List<string> LineExpressions { get; set; } = new List<string>();

        public List<string> EndExpressions { get; set; } = new List<string>();

        public DOTBlockType BlockType { get; set; } = DOTBlockType.None;

        public DOTBlockType LineType { get; set; } = DOTBlockType.None;

        public bool IsMultiline { get; set; } = false;

        public int TextGroup { get; set; } = 3;

        public int MinimumGroups { get; set; } = 2;

        public Dictionary<int, string> Attributes { get; set; } = new Dictionary<int, string>();

        public Dictionary<int, string> Values { get; set; } = new Dictionary<int, string>();

        private bool _isReallyMultiLine => this.IsMultiline && (this.LineExpressions.Count > 0 || this.EndExpressions.Count > 0); 

        public SortedList<int, ILineProcessor> LineProcessors { get; set; } = new SortedList<int, ILineProcessor>()
        {
            { 0, new ExpressionLineProcessor() {
                BlockType = DOTBlockType.BoldBlock,
                Expression = @"(.*)[\*]{2}(.*)[\*]{2}(.*)"
            } },
            { 10, new ExpressionLineProcessor() {
                BlockType = DOTBlockType.ItalicsBlock,
                Expression = @"(.*)[\*]{1}(.*)[\*]{1}(.*)"
            } },
            { 20, new ExpressionLineProcessor() {
                BlockType = DOTBlockType.VariableBlock,
                Expression = @"(.*)[\`]{1}(.+)[\`]{1}(.*)"
            } },
            { 30, new ExpressionLineProcessor() {
                BlockType = DOTBlockType.ImageBlock,
                Expression = @"(.*)[\!][\[]{1}(.+)[\]]{1}\s*[\(]{1}(.+)[\)]{1}(.*)",
                Attributes = new Dictionary<int, string>() { { 3, "src" } }
            } },
            { 40, new ExpressionLineProcessor() {
                BlockType = DOTBlockType.LinkBlock,
                Expression = @"(.*)[\[]{1}(.+)[\]]{1}\s*[\(]{1}(.+)[\)]{1}(.*)",
                Attributes = new Dictionary<int, string>() { { 3, "href" } }
            } },
        };

        public virtual bool Process(IDOTNode rootnode, MatchCollection matches, ref int index)
        {
            var ok = true;

            var basenode = rootnode.Nodes[index];
            var match = matches[0];
            if (match.Groups.Count >= this.MinimumGroups)
            {
                basenode.Text = string.Empty;
                basenode.NodeType = DOTNodeType.Node;
                basenode.BlockType = this.BlockType;
                foreach (var attr in this.Attributes)
                {
                    if (!string.IsNullOrEmpty(match.Groups[attr.Key].Value)) basenode.SetAttribute(attr.Value, match.Groups[attr.Key].Value);
                }
                foreach (var val in this.Values)
                {
                    if (!string.IsNullOrEmpty(match.Groups[val.Key].Value)) basenode.SetValue(val.Value, match.Groups[val.Key].Value);
                }
                if (TextGroup > 0 && !string.IsNullOrEmpty(match.Groups[TextGroup].Value))
                {
                    if (this.IsMultiline) basenode.AddNode(new DOTNode() { BlockType = this.LineType, NodeType = DOTNodeType.Text, Text = match.Groups[TextGroup].Value });
                    else basenode.Text = match.Groups[TextGroup].Value ;
                }
                if (this._isReallyMultiLine)
                {
                    bool islast;
                    do
                    {
                        index++;
                        var node = rootnode.Nodes[index];
                        islast = ProcessNextLine(node, out IDOTNode addnode, out bool backup);
                        if (addnode != null)
                        {
                            basenode.AddNode(addnode);
                            node.ProcessingType = DOTProcessingType.Remove;
                        }
                        if (backup) index--;
                    } while (!islast);
                }
                LineProcessNodes(basenode);
            }
            return ok;
        }

        private bool ProcessNextLine(IDOTNode node, out IDOTNode newnode, out bool backup)
        {
            var islast = false;
            var ismatch = false;
            // Check to see if we have an end match
            if (this.CheckForMatch(this.EndExpressions, node, out newnode))
            {
                node.ProcessingType = DOTProcessingType.Remove;
                islast = true;
            }
            // If we don't have an end match but don't have any Line exressions we're still in the block
            else if ((this.LineExpressions.Count == 0 && this.EndExpressions.Count > 0))
            {
                ismatch = true;
                newnode = new DOTNode() { BlockType = this.LineType, NodeType = DOTNodeType.Text, Text = node.Text };
            }
            // if we have line expressions we check for a match.
            else ismatch = this.CheckForMatch(this.LineExpressions, node, out newnode);
            backup = (!islast) && (!ismatch);
            return islast ? islast : !ismatch;
        }


        private bool CheckForMatch(List<string> expressions, IDOTNode node, out IDOTNode newnode)
        {
            var ismatch = false;
            newnode = null;
            foreach (var expression in expressions)
            {
                var reg = new Regex(expression);
                if (reg.IsMatch(node.Text))
                {
                    ismatch = true;
                    MatchCollection matches = reg.Matches(node.Text);
                    var match = matches[0];
                    if (!string.IsNullOrEmpty(match.Groups[TextGroup].Value))
                    {
                        newnode = new DOTNode() { BlockType = this.LineType, NodeType = DOTNodeType.Text, Text = match.Groups[TextGroup].Value };
                    }
                    break;
                }
            }
            return ismatch;
        }

        protected void LineProcessNodes(IDOTNode basenode)
        {
            foreach (var proc in LineProcessors)
            {
                proc.Value.ProcessNode(basenode);
                foreach (var node in basenode.Nodes)
                {
                    proc.Value.ProcessNode(node.Value);
                }
            }

        }
    }
}
