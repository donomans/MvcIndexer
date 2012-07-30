using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MvcIndexer.Holders;

namespace MvcIndexer.Holders
{
    public class IndexCache
    {///should this be a singleton?
     
        private static readonly IndexCache _indexcache = new IndexCache();

        /// <summary>
        /// cache dictionary - 
        /// Key is a keyword
        /// Value is the page contents and information of pages matching that Keyword
        /// </summary>
        private Dictionary<String, Page[]> _cache = new Dictionary<String, Page[]>();

        static IndexCache()
        {
        }

        private IndexCache()
        {
        }

        public IndexCache Cache
        {
            get { return _indexcache; }
            private set { }
        }



    }
}
