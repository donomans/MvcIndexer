using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using MvcIndexer.Holders;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Net.Http;

namespace MvcIndexer
{
    public class MvcIndexer
    {
        private IndexUrls urls = null;

        private static Boolean IndexRunning = false;
        private static Boolean ServingSearches = false;

        private static MvcIndexerConfig configuration = new MvcIndexerConfig();


        public static MvcIndexerConfig Configuration
        {
            get { return configuration; }
            set { configuration = value; }
        }

        public static Boolean StartIndexer(MvcIndexerConfig Config = null)
        {
            if (!IndexRunning)
            {
                IndexRunning = true;
                if (Configuration != null)
                    configuration = Config;

                if (configuration.RootDomain != "")
                {
                    IndexUrls urls = Indexable.GetIndexable();
                    ///get the stuff started
                    String seedurl = "";
                    if ((configuration.SeedUrl == null || configuration.SeedUrl == "") && urls != null && urls.Urls.Count > 0)
                        configuration.SeedUrl = configuration.RootDomain + urls.Urls.First().Path;
                    Index i = new Index();
                    i.Crawl(seedurl, ip => { });
                    

                    return IndexRunning;
                }
                else
                {
                    IndexRunning = false;
                    return IndexRunning;
                }
            }
            else
                return IndexRunning;
        }
    }

    internal class Index
    {
        private static IndexedPages index = new IndexedPages();
        private static Boolean crawling = false;
        
        /// <summary>
        /// Crawl the site starting with the SeedUrl and stores the index when done
        /// </summary>
        /// <param name="SeedUrl"></param>
        public async void Crawl(String SeedUrl, Action<IndexedPages> CrawlDoneCallBack)///not sure here if i should pass another function responsible for copying the IndexedPages to the IndexCache or just do it automatically
        {
            Uri seed = new Uri(SeedUrl);
            ///0) Spin off a task or something
            ///1) Get HTML of seed
            ///     - put in _content
            ///2) Parse Urls
            ///     - toss out urls if UrlDiscovery is false
            ///3) Cycle through all urls until everything has been crawled
            ///4) Run Filters if present
            ///     - put in PureContent
            ///5) If crawl type is continuous slowly churn through them based on some 
            /// arbitrary limit based on a page every 3 seconds or something.
            /// if crawl type is scheduled, do a taskfactory and burn through them
            ///6) If automaster is true then try to toss the common elements found
            /// -- might be able to find based on css class or ids ?
            ///7) Call the CallBack function
            ///
            CrawlPageAsync(seed.ToString());
            CrawlDoneCallBack(index);///???
            return;
        }

        private async void CrawlPageAsync(String url)
        {
            HttpClient client = new HttpClient();
            await client.GetAsync(url).ContinueWith(CrawlPageResponseAsync);
        }
        private async void CrawlPageResponseAsync(Task<HttpResponseMessage> html)
        {
            Page p = new Page();
            p.Url = html.Result.RequestMessage.RequestUri.ToString();
            p.PureContent = await html.Result.Content.ReadAsStringAsync();
            ///need to determine:
            ///1) title
            ///2) keywords
            ///3) priority/weighting
            
            index.AddLink(new Link() { Crawled = true, Page = p });
            ///do something with the web request response (the html).
            //HttpWebResponse httpresponse = (HttpWebResponse)response.Result; 
        }

     
    }

    internal class LinkParser
    {
        private const String LINK_REGEX = "href=\"[ \t\r\n]*[ a-zA-Z./:&\\d_-]+\"";


        /// <summary>
        /// this needs work - what to return? directly populate the IndexedPages somehow?
        /// 
        /// </summary>
        /// <param name="HtmlText"></param>
        /// <param name="sourceUrl"></param>
        /// <returns></returns>
        public static List<String> ParseLinks(String HtmlText, String sourceUrl)
        {
            MatchCollection matches = Regex.Matches(HtmlText, LINK_REGEX);
            List<String> urls = new List<String>();
            for (Int32 i = 0; i <= matches.Count - 1; i++)
            {
                Match anchorMatch = matches[i];

                if (anchorMatch.Value == String.Empty)
                {
                    ///blank match
                    continue;
                }

                String foundHref = "";
                try
                {
                    foundHref = anchorMatch.Value.Replace("href=\"", "");
                    foundHref = foundHref.Substring(0, foundHref.IndexOf("\"")).Replace("\r", "").Replace("\n", "").Trim().ToLower();
                }
                catch (Exception)
                {
                    ///parsing exception
                }
                               
                String realUrl = FixPath(sourceUrl, foundHref);

                if (realUrl == "" || foundHref == "")
                    continue;

                if (!realUrl.Contains(MvcIndexer.Configuration.RootDomain))
                {
                    ///do I care about external links?
                    continue;
                }

                if (IsAWebPage(realUrl))
                {
                    urls.Add(realUrl);
                }
                else
                    continue;
            }
            return urls;
        }
        private static bool IsAWebPage(String foundHref)
        {
            if (foundHref.Contains("javascript:"))
                return false;
            ///fairly simple check to weed out common non html page files
            String extension = foundHref.Substring(foundHref.LastIndexOf(".") + 1, foundHref.Length - foundHref.LastIndexOf(".") - 1);
            switch (extension)
            {
                case "gif":
                case "swf":
                case "ico":
                case "jpg":
                case "png":
                case "css":
                case "jpeg":
                    return false;
                default:
                    return true;
            }
        }            
        public static string FixPath(string originatingUrl, string link)
        {
            return new Uri(new Uri(originatingUrl), link).AbsoluteUri;
        }
    }
}
