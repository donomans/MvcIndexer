using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MvcIndexer.Holders;

namespace MvcIndexer.Holders
{
    public class IndexCache
    {///should this be a singleton?  
     ///probably not.  querier can be, for convenience of interface, 
     ///but this is probably something that the querier should have as a private member?
     
     ///IndexCache is something that can continually be rebuilt and reset over some period of time
     ///there can be multiple.  potentially for different "indexes" on the site (like for a support section or articles or whatever)
     
        private static readonly IndexCache _indexcache = new IndexCache();

        /// <summary>
        /// cache dictionary - 
        /// Key is a keyword
        /// Value is the page contents and information of pages matching that Keyword
        /// </summary>
        private Dictionary<String, List<Page>> _cache = new Dictionary<String, List<Page>>(); 
        
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
