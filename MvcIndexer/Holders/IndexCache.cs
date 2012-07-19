using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MvcIndexer.Holders;

namespace MvcIndexer.Holders
{
    public class IndexCache
    {
        private static readonly IndexCache _indexcache = new IndexCache();

        /// <summary>
        /// cache dictionary - 
        /// Key is a keyword
        /// Value is the page contents and information
        /// </summary>
        private Dictionary<String, IndexedPage> _cache = new Dictionary<String, IndexedPage>();

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
