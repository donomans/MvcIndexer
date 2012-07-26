using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvcIndexer.Holders
{
    /// <summary>
    /// list of urls gathered from Indexable attribute
    /// </summary>
    public class IndexUrls
    {
        public List<IndexUrl> Urls = new List<IndexUrl>();
    }

    public class IndexUrl
    {
        public String Path { get; set; }
        //public String[] AdditionalUrls { get; set; }
        public Dictionary<String, Int32> KeywordsAndPriority { get; set; }
        public String[] Keywords { get; set; }
        public Int32 Priority { get; set; }
    }
}
