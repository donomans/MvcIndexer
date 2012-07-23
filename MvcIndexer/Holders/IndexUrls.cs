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






    public sealed class LinksToCrawl
    {
        private static readonly LinksToCrawl _Links = new LinksToCrawl();
        public static readonly Object Lock = new Object();
        public CrawlInfos NewUrls = new CrawlInfos();
        public CrawlInfos Urls = new CrawlInfos();
        public List<String> BlankUrls = new List<String>();
        public List<String> GeneralExceptions = new List<String>();
        public CrawlInfo this[String Url]
        {
            get
            {
                if (Urls.ContainsKey(Url))
                    return Urls[Url];
                else
                    return new CrawlInfo(0, false, Url, Url);
            }
            set
            {
                Urls[Url] = value;
            }
        }

        public void UpdateUrls()
        {
            lock (Lock)
            {
                foreach (KeyValuePair<String, CrawlInfo> kvp in NewUrls)
                {
                    if (!Urls.ContainsKey(kvp.Key))
                    {
                        Urls.Add(kvp.Key, kvp.Value);
                    }
                }
            }
        }
        public static LinksToCrawl Links
        {
            get { return _Links; }
            set { }
        }
        static LinksToCrawl()
        {
        }
        private LinksToCrawl()
        {
        }
    }
    public class CrawlInfos : IEnumerable<KeyValuePair<String, CrawlInfo>>
    {
        private Dictionary<String, CrawlInfo> Urls = new Dictionary<String, CrawlInfo>(7000);

        public CrawlInfo this[String url]
        {
            get
            {
                return Urls[url.ToLower()];
            }
            set
            {
                Urls[url.ToLower()] = value;
            }
        }

        public List<CrawlInfo> FindAll(Predicate<CrawlInfo> match)
        {
            List<CrawlInfo> retUrls = new List<CrawlInfo>();
            foreach (CrawlInfo url in Urls.Values)
                if (match(url))
                    retUrls.Add(url);
            return retUrls;
        }
        public Boolean ContainsKey(String url)
        {
            return Urls.ContainsKey(url.ToLower());
        }
        public void Add(String url, CrawlInfo crawlinfo)
        {
            Urls.Add(url.ToLower(), crawlinfo);
        }

        public void Clear()
        {
            Urls.Clear();
        }

        #region IEnumerable<KeyValuePair<string,CrawlInfo>> Members

        public IEnumerator<KeyValuePair<String, CrawlInfo>> GetEnumerator()
        {
            return Urls.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
    [Flags]
    public enum PageType
    {
        Normal = 0,
        External = 1,
        Bad = 4
    }
    public class CrawlInfo
    {


        public Int32 TryCount;
        public Boolean Crawled;
        public Boolean GiveUp;
        public Boolean Exception = false;
        public PageType Type = PageType.Normal;
        public List<String> PagesFoundOn = new List<String>();
        public String RealUrl;
        public String Url;
        public List<String> Exceptions = new List<String>();
        public CrawlInfo(int trycount, Boolean crawled, String FoundOnUrl, String realUrl)
        {
            TryCount = trycount;
            Crawled = crawled;
            GiveUp = false;
            PagesFoundOn.Add(FoundOnUrl);
            RealUrl = realUrl;
            Url = "";
        }
        public CrawlInfo(int trycount, String FoundOnUrl, String realUrl, PageType type)
        {
            TryCount = trycount;
            Crawled = false;
            GiveUp = false;
            PagesFoundOn.Add(FoundOnUrl);
            RealUrl = realUrl;
            Url = "";
            Type = type;
        }
        public CrawlInfo(int trycount, Boolean crawled, String FoundOnUrl, String GoHandleUrl, String realUrl)
        {
            TryCount = trycount;
            Crawled = crawled;
            GiveUp = false;
            PagesFoundOn.Add(FoundOnUrl);
            RealUrl = realUrl;
            Url = GoHandleUrl;
        }
        private CrawlInfo()
        {
        }
    }
    public class LinkFixers
    {
        /// <summary>
        /// Fixes a path. Ensures it is a fully functional absolute url.
        /// </summary>
        /// <param name="originatingUrl">The url that the link was found in.</param>
        /// <param name="link">The link to be fixed up.</param>
        /// <returns>A fixed url that is fit to be fetched.</returns>
        public static string FixPath(string originatingUrl, string link)
        {
            return new Uri(new Uri(originatingUrl), link).AbsoluteUri;
        }      
    }
}
