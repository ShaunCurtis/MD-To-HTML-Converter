using System;
using System.Collections.Generic;
using System.Text;

namespace MD_To_HTML_Converter.Data
{
    class DOTNode : IDOTNode, ICloneable
    {
        public string Name { get; set; } = "Node";

        public DOTNodeType DOTType { get; set; } = DOTNodeType.None;

        public DOTProcessingType ProcessingType { get; set; } = DOTProcessingType.None;

        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

        public SortedList<int, IDOTNode> Nodes { get; set; } = new SortedList<int, IDOTNode>();

        public string Text { get; set; } = string.Empty;

        public object Clone()
        {
            var newNode = new DOTNode() { DOTType = this.DOTType, Text= this.Text };
            foreach (var att in this.Attributes) newNode.Attributes.Add(att.Key, att.Value);
            newNode.Name = "TEXTBLOCK";
            var index = 0;
            foreach (var node in this.Nodes)
            {
                newNode.Nodes.Add(index++, (IDOTNode)node.Value.Clone());
            }  

            return newNode;
        }
    }
}
