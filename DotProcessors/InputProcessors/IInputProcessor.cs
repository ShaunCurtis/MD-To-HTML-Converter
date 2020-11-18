using MD_To_HTML_Converter.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MD_To_HTML_Converter.DotProcessors
{
    public interface IInputProcessor
    {
/// <summary>
/// List of PreProcessors to run
/// Sorted list so we can order them correctly
/// </summary>
        public SortedList<int, IBlockProcessor> PreProcessors { get; }

        /// <summary>
        /// Processes the Raw DocumentObject line by line
        /// </summary>
        /// <param name="Dot"></param>
        /// <returns></returns>
        public bool Process(DocumentObjectTree Dot);

        public void CleanUp(DocumentObjectTree Dot);

    }
}
