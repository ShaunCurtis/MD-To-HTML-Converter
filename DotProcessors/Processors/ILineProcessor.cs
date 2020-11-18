using MD_To_HTML_Converter.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace MD_To_HTML_Converter.DotProcessors
{
    public interface ILineProcessor
    {
        public string Name { get; }
                
        public bool Process(DocumentObjectTree Dot);

        public bool ProcessNode(IDOTNode node);
    }
}
