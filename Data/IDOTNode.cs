using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace MD_To_HTML_Converter.Data
{
    public interface IDOTNode : ICloneable
    {
        public DOTNodeType NodeType { get; set; }

        public DOTBlockType BlockType { get; set; }

        public DOTProcessingType ProcessingType { get; set; }

        public Dictionary<string, object> Values { get; set; }

        public Dictionary<string, object> Attributes { get; set; }

        public SortedList<int, IDOTNode> Nodes { get; set; }

        public string Text { get; set; }

        public IDOTNode ParentNode { get; set; } 

        public bool GetAttribute(string key, out object value)
        {
            value = null;
            if (this.Attributes.ContainsKey(key)) value = this.Attributes[key];
            return this.Attributes.ContainsKey(key);
        }

        public object GetAttribute(string key)
        {
            if (this.Attributes.ContainsKey(key)) return this.Attributes[key];
            else return null;
        }

        public bool GetAttributeAsString(string key, out string value)
        {
            value = string.Empty;
            var val = GetAttribute(key);
            if (val is string)
            {
                value = (string)this.Attributes[key];
                return true;
            }
            return false;
        }

        public void SetAttribute(string key, object value)
        {
            if (this.Attributes.ContainsKey(key)) this.Attributes[key] = value;
            else this.Attributes.Add(key, value);
        }
        public bool GetValue(string key, out object value)
        {
            value = null;
            if (this.Values.ContainsKey(key)) value = this.Values[key];
            return this.Values.ContainsKey(key);
        }

        public object GetValue(string key)
        {
            if (this.Values.ContainsKey(key)) return this.Values[key];
            else return null;
        }

        public bool GetValueAsString(string key, out string value)
        {
            value = string.Empty;
            var val = GetValue(key);
            if (val is string)
            {
                value = (string)this.Values[key];
                return true;
            }
            return false;
        }

        public void SetValue(string key, object value)
        {
            if (this.Values.ContainsKey(key)) this.Values[key] = value;
            else this.Values.Add(key, value);
        }


        public bool GetNode(int key, out object value)
        {
            value = null;
            if (this.Nodes.ContainsKey(key)) value = this.Nodes[key];
            return this.Nodes.ContainsKey(key);
        }

        public object GetNode(int key)
        {
            if (this.Nodes.ContainsKey(key)) return this.Nodes[key];
            else return null;
        }

        public void AddNode(IDOTNode value)
        {
            var maxindex = 0;
            if (this.Nodes.Count > 0) maxindex = this.Nodes.Max(item => item.Key);
            maxindex++;
            this.Nodes.Add(maxindex, value);
        }

        public void DeleteNode(int key)
        {
            if (this.Nodes.ContainsKey(key)) this.Nodes.Remove(key);
        }

        public void IndexNodes()
        {
            var list = new SortedList<int, IDOTNode>();
            var index = 0;
            foreach (var node in this.Nodes)
            {
                list.Add(index++, node.Value);
            }
            this.Nodes = list;
        }

        public void ToConsole(string id, string label)
        {
            Console.WriteLine($"{label}{id}> - {this.NodeType} - {this.BlockType} - {this.Text} - {this.GetAttributeString()}");
            foreach (var node in this.Nodes)
            {
                var sid = $"{id}.{node.Key}";
                node.Value.ToConsole(sid, $"=={label}");
            }
        }

        public string GetAttributeString()
        {
            var sb = new StringBuilder();
            foreach (var attr in this.Attributes) sb.Append($"{attr.Key}=\"{attr.Value}\" ");
            return sb.ToString();
        }

    }
}
