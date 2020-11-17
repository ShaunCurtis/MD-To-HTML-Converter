using System;
using System.Collections.Generic;
using System.Text;

namespace MD_To_HTML_Converter.Data
{
    public class DocumentObjectTree
    {
        public string FileName { get; set; }

        public IDOTNode RootNode { get; set; } = default;

        public void ToConsole()
        {

            if (this.RootNode != null)
            {
                foreach (var node in this.RootNode.Nodes)
                {
                    var sid = node.Key.ToString();
                    node.Value.ToConsole(sid, string.Empty);
                }
            }
        }
    }
}
