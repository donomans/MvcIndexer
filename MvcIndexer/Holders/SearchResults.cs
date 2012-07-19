using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvcIndexer.Holders
{
    public class SearchResults
    {
        public List<SearchResult> Results = new List<SearchResult>();

        public SearchResults()
        {
        }
    }

    public class SearchResult
    {
        public String Path = "";
        public Int32 FixedPriority = 0;
        public Int32 RealPriority = 0;
        public String Title = "";

        public SearchResult()
        {
        }
    }
}
