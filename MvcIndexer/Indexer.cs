using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using MvcIndexer.Holders;

namespace MvcIndexer
{
    public class MvcIndexer
    {
        private static readonly MvcIndexer _indexer = new MvcIndexer();

        private String root = "";
        private IndexType type = IndexType.Continuous; 
        private DateTime start = new DateTime();
        private String seed = "";
        private Boolean discovery = false;

        private IndexUrls urls = null;

        private Boolean automaster = false;
        private HtmlFilter[] filters = null;

        static MvcIndexer()
        {
        }

        private MvcIndexer()
        { 
        }

        public MvcIndexer Indexer
        {
            get { return _indexer; }
            private set { }
        }


        public void SetConfiguration(MvcIndexerConfig config)
        {
            root = config.RootDomain;
            type = config.IndexType;
            start = config.ScheduledStart;
            seed = config.SeedUrl;
            discovery = config.UrlDiscovery;

            automaster = config.AutoDetectAndRemoveMaster;
            filters = config.Filters;
        }

        public Boolean StartIndexer()
        {            
            if (!(root == ""))
            {
                urls = Indexable.GetIndexable();
                ///get the stuff started
                String seedurl = "";
                if (seed == null || seed == "")
                    seedurl = seed;
                else
                    seedurl = root + urls.Urls[0].Path;

                IndexedPage[] indexpages = Index.Crawl(seedurl);
                

                return true;
            }
            else
                return false;
        }
    }

    internal class Index
    {
        public static IndexedPage[] Crawl(String seed)
        {
            ///1) Get HTML of seed
            ///     - put in _content
            ///2) Parse Urls
            ///     - toss out urls if UrlDiscovery is false
            ///3) Run Filters if present
            ///     - put in PureContent
            ///4) if crawl type is continuous slowly churn through them based on some 
            /// arbitrary limit based on a page every 3 seconds or something.
            /// if crawl type is scheduled, do a threadpool and burn through them
            ///5) if automaster is true then try to toss the common elements found
            return null;
        }

        private static String[] GetLinks(String Url)
        {
            return null;
        }

        private static String GetHtml(String Url)
        {
            return "";
        }

       
    }
}
