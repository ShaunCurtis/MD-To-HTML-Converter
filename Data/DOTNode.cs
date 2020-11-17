using System;
using System.Collections.Generic;
using System.Text;

namespace MD_To_HTML_Converter.Data
{
    class DOTNode : IDOTNode, ICloneable
    {
        public DOTBlockType BlockType { get; set; }

        public DOTNodeType NodeType { get; set; } = DOTNodeType.None;

        public DOTProcessingType ProcessingType { get; set; } = DOTProcessingType.None;

        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

        public SortedList<int, IDOTNode> Nodes { get; set; } = new SortedList<int, IDOTNode>();

        public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();

        public string Text { get; set; } = string.Empty;

        public object Clone()
        {
            var newNode = new DOTNode() { NodeType = this.NodeType, Text= this.Text, BlockType = this.BlockType };
            foreach (var att in this.Attributes) newNode.Attributes.Add(att.Key, att.Value);
            foreach (var val in this.Values) newNode.Values.Add(val.Key, val.Value);
            newNode.BlockType = newNode.BlockType == DOTBlockType.None ? DOTBlockType.TextBlock: newNode.BlockType;
            var index = 0;
            foreach (var node in this.Nodes)
            {
                newNode.Nodes.Add(index++, (IDOTNode)node.Value.Clone());
            }  
            return newNode;
        }
    }
}
