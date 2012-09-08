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
        private static Boolean IndexRunning = false;
        private static Boolean ServingSearches = false;

        private static MvcIndexerConfig configuration = new MvcIndexerConfig();


        public static MvcIndexerConfig Configuration
        {
            get { return configuration; }
            set { configuration = value; }
        }

        public static async Task<Boolean> StartIndexer(MvcIndexerConfig Config = null)
        {
            if (!IndexRunning)
            {
                if (Configuration != null)
                    configuration = Config;

                if (configuration.RootDomain != "")
                {
                    //IndexUrls urls = Indexable.GetIndexable();
                    ///get the stuff started
                    //String seedurl = "";
                    if (configuration.SeedUrl == null || configuration.SeedUrl == "")// && urls != null && urls.Urls.Count > 0)
                        configuration.SeedUrl = configuration.RootDomain;// +urls.Urls.First().Path;
                    Index i = new Index();
                    await i.Crawl(configuration.SeedUrl, CrawlCompleteAsync);
                    IndexRunning = true;

                    return IndexRunning;
                }
                else
                    throw new ArgumentNullException("Config.RootDomain");///not quite accurate
            }
            else
                return IndexRunning;
        }

        public static async Task CrawlCompleteAsync(IndexedPages index)
        {
            ///1) Fill the IndexCache with the index
            ///2) Set the IndexRunning to false
            IndexRunning = false;
        }
    }

    internal class Index
    {
        public delegate Task FinishedCrawlAsync(IndexedPages index);
        
        private static IndexedPages index = new IndexedPages();
        private static Boolean crawling = false;
        
        /// <summary>
        /// Crawl the site starting with the SeedUrl and stores the index when done
        /// </summary>
        /// <param name="SeedUrl"></param>
        public async Task Crawl(String SeedUrl, FinishedCrawlAsync CrawlDoneCallBack)///not sure here if i should pass another function responsible for copying the IndexedPages to the IndexCache or just do it automatically
        {
            ///0) Spin off a task or something
            ///1) Get HTML of seed
            ///     - put in _content
            //HttpResponseMessage response = CrawlPage(SeedUrl);
            String html = await CrawlPage(SeedUrl);//await response.Content.ReadAsStringAsync();
            
            ///2) Parse Urls
            ///     - toss out urls if UrlDiscovery is false
            if (MvcIndexer.Configuration.UrlDiscovery)
            {
                List<String> urls = LinkParser.ParseLinks(html, SeedUrl);
                foreach (String url in urls)
                    index.AddLink(url);
            }
            
            index.AddLinks(Indexable.GetIndexable());

            #region add the seed page
            Page p = new Page(SeedUrl, html);
            
            await p.RunFilters(MvcIndexer.Configuration.Filters);
            p.StripHtml();

            index.AddLink(new Link()
            {
                Crawled = true,
                Page = p
            });
            #endregion
            ///3) Cycle through all urls until everything has been crawled
            IEnumerable<Link> links = index.GetUncrawledLinks();
            Int32 blankcounter = 0;
            while (blankcounter < 5)
            {
                foreach (Link link in links)
                {
                    await CrawlPageAsync(link.Url);
                }
                links = index.GetUncrawledLinks();
                if (links.Count() > 0)
                {
                    blankcounter++;
                    Thread.Sleep(10000); ///sleep to give index a chance to repopulate with more links
                }
            }
            
            
            ///5) If crawl type is continuous slowly churn through them based on some 
            /// arbitrary limit based on a page every 3 seconds or something.
            /// if crawl type is scheduled, do a taskfactory and burn through them
            
            ///7) Call the CallBack function

            if(CrawlDoneCallBack != null)
                await CrawlDoneCallBack(index);///???
        }

        /// <summary>
        /// Get the page text from the url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private async Task<String> CrawlPage(String url)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);
            return await response.Content.ReadAsStringAsync();
        }
        private async Task CrawlPageAsync(String url)
        {
            HttpClient client = new HttpClient();
            await client.GetAsync(url).ContinueWith(CrawlPageResponseAsync);
        }
        private async void CrawlPageResponseAsync(Task<HttpResponseMessage> html)
        {
            ///index contains the link - need to look up the Link record and fill out the information
            String url = html.Result.RequestMessage.RequestUri.ToString();
            
            Link link = index[url];
            if(link == null)
            {
                link = new Link(){
                    Crawled = true, 
                    Page = new Page(url, await html.Result.Content.ReadAsStringAsync())
                };
                index[url] = link;
            }
            
            ///need to:
            ///2) Run filters and StripHtml
            /// -- if automaster is on then try to strip out the master before calling StripHtml
            await link.Page.RunFilters(MvcIndexer.Configuration.Filters);
            if(MvcIndexer.Configuration.AutoDetectAndRemoveMaster)
            {
                ///hmm.
            }
            
            link.Page.StripHtml();
            
            ///5) Add new links, if UrlDiscovery is on
            if (MvcIndexer.Configuration.UrlDiscovery)
            {
                List<String> hrefs = LinkParser.ParseLinks(link.Page.PureContent, url);
                foreach (String href in hrefs)
                    index.AddLink(href);
            }
            
            //index.AddLink(new Link() { Crawled = true, Page = p });
        }

        

    }

    internal class LinkParser
    {
        private const String LINKREGEX = "href=\"[ \t\r\n]*[ a-zA-Z./:&\\d_-]+\"";

        
        public static List<String> ParseLinks(String HtmlText, String sourceUrl)
        {
            MatchCollection matches = Regex.Matches(HtmlText, LINKREGEX);
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
