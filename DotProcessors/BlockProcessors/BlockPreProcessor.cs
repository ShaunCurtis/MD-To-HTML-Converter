using MD_To_HTML_Converter.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace MD_To_HTML_Converter.DotProcessors
{
    class BlockPreProcessor : IBlockPreProcessor
    {

        public virtual string Name { set; get; } = "Pre-Processor";

        public Dictionary<DOTProcessingType, string> Expressions { get; set; } = new Dictionary<DOTProcessingType, string>();

        public virtual bool Process(DocumentObjectTree Dot)
        {
            PreProcess(Dot);
            ReProcess(Dot);
            CleanUp(Dot);
            return true;
        }

        protected virtual void PreProcess(DocumentObjectTree Dot) { }

        protected virtual void ReProcess(DocumentObjectTree Dot) { }

        protected void CleanUp(DocumentObjectTree Dot)
        {
            var list = Dot.RootNode.Nodes.Where(item => item.Value.ProcessingType == DOTProcessingType.Remove).ToList();
            list.ForEach(item => Dot.RootNode.DeleteNode(item.Key));
        }
    }
}
