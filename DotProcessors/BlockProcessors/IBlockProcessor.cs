using MD_To_HTML_Converter.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace MD_To_HTML_Converter.DotProcessors
{
    public interface IBlockProcessor
    {
        public string Name { get; }

        public List<string> Expressions { get; }

        public bool Process(IDOTNode node, MatchCollection matches, ref int index);

    }
}
