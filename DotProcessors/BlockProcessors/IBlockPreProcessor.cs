using MD_To_HTML_Converter.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace MD_To_HTML_Converter.DotProcessors
{
    public interface IBlockPreProcessor
    {
        public string Name { get; }

        public Dictionary<DOTProcessingType, string> Expressions {get; set;}

        public bool Process(DocumentObjectTree Dot);

        public void CleanUp(DocumentObjectTree Dot)
        {
            var list = Dot.RootNode.Nodes.Where(item => item.Value.ProcessingType == DOTProcessingType.Remove).ToList();
            list.ForEach(item => Dot.RootNode.DeleteNode(item.Key));
        }

    }
}
