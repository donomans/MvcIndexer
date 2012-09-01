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
        private static readonly HashSet<String> stopwords = new HashSet<String>()
        {
            ""
        };
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
            HttpResponseMessage response = CrawlPage(SeedUrl);
            String html = await response.Content.ReadAsStringAsync();
            
            ///2) Parse Urls
            ///     - toss out urls if UrlDiscovery is false
            if (MvcIndexer.Configuration.UrlDiscovery)
            {
                List<String> urls = LinkParser.ParseLinks(html, SeedUrl);
                foreach (String url in urls)
                    index.AddLink(url);
            }
            
            index.AddLinks(Indexable.GetIndexable());
            Page p = new Page();
            p.PureContent = html;
            p.StrippedContent = p.PureContent;
            p.Title = GetTitle(p.PureContent);
            p.Keywords = GetKeywords(p.StrippedContent);
            foreach (HtmlFilter filter in MvcIndexer.Configuration.Filters)
                p.StrippedContent = filter(p.StrippedContent);

            p.StrippedContent = LinkParser.StripHtml(p.StrippedContent);
            p.Url = SeedUrl;
            p.Keywords = GetKeywords(p.StrippedContent);
            p.KeywordPriority = GetKeywordsPriority(p);
            index.AddLink(new Link()
            {
                Crawled = true,
                Page = p
            });
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

        private HttpResponseMessage CrawlPage(String url)
        {
            HttpClient client = new HttpClient();
            return client.GetAsync(url).Result;
        }
        private async Task CrawlPageAsync(String url)
        {
            HttpClient client = new HttpClient();
            await client.GetAsync(url).ContinueWith(CrawlPageResponseAsync);
        }
        private async void CrawlPageResponseAsync(Task<HttpResponseMessage> html)
        {
            ///index contains the link - need to look up the Link record and fill out the information
            Page p = new Page();
            p.Url = html.Result.RequestMessage.RequestUri.ToString();
            p.PureContent = await html.Result.Content.ReadAsStringAsync();
            p.StrippedContent = p.PureContent;
            ///need to determine:
            ///1) title
            p.Title = GetTitle(p.PureContent);
            ///2) Run filters and StripHtml
            /// -- if automaster is on then try to strip out the master before calling StripHtml
            foreach (HtmlFilter filter in MvcIndexer.Configuration.Filters)
                p.StrippedContent = filter(p.StrippedContent);
            
            if(MvcIndexer.Configuration.AutoDetectAndRemoveMaster)
            {
                ///hmm.
            }
            
            p.StrippedContent = LinkParser.StripHtml(p.StrippedContent);
            
            ///2) keywords
            ///     - need a list of junk words
            ///     - find common words in the StrippedContent that aren't in the junk word list
            ///3) priority/weighting
            ///4) locations where the determined/provided keywords reside in the document
            p.Keywords = GetKeywords(p.StrippedContent);
            p.KeywordPriority = GetKeywordsPriority(p);
            
            ///5) Add new links, if UrlDiscovery is on
            if (MvcIndexer.Configuration.UrlDiscovery)
            {
                List<String> urls = LinkParser.ParseLinks(p.PureContent, );
                foreach (String url in urls)
                    index.AddLink(url);
            }
            
            //index.AddLink(new Link() { Crawled = true, Page = p });
        }

        private String GetTitle(String content)
        {
            String title = "";
            Int32 titlestart = content.IndexOf("<title>");
            if (titlestart > 0)
            {
                Int32 titleend = content.IndexOf("</title>");
                title = content.Substring(titlestart + "<title>".Length, titleend - titlestart);
            }
            else
            {
                titlestart = content.IndexOf("<h1>");
                if (titlestart > 0)
                {
                    Int32 titleend = content.IndexOf("</h1>");
                    title = content.Substring(titlestart + "<h1>".Length, titleend - titlestart);
                }
            }
            return title;
        }
        private Dictionary<String, Int32> GetKeywords(String strippedcontent)
        {
            return null;
        }
        private Dictionary<String, Int32> GetKeywordsPriority(Page page)
        {
            return null;
        }

    }

    internal class LinkParser
    {
        private const String LINK_REGEX = "href=\"[ \t\r\n]*[ a-zA-Z./:&\\d_-]+\"";

        public static String StripHtml(String source)
        {
            List<Char> array = new List<Char>(source.Length);
            Boolean inside = false;
            Boolean dquotes = false;
            Boolean squotes = false;

            ///1) look for tags or things that need to be fully removed (entire containing contents) and remove them
            source = source.Replace("&nbsp;", "").Replace("Â", "").Replace("<br>", " ").Replace("<br />", " ");///special case junk - need to add more things like the trademark symbols and stuff like that
            String lowersource = source.ToLower();
            Int32 scriptindex = lowersource.IndexOf("<script");
            while (scriptindex > 0)
            {
                Int32 scriptendindex = lowersource.IndexOf("</script>", scriptindex) + "</script>".Length;
                lowersource = lowersource.Remove(scriptindex, scriptendindex - scriptindex);
                source = source.Remove(scriptindex, scriptendindex - scriptindex);

                scriptindex = lowersource.IndexOf("<script");
            }

            ///really cheesy way for now to remove most of the extra spacing
            source = source.Replace(Environment.NewLine, "").Replace("  ", "");

            ///2) search entire contents for < and > tags outside of quotes and remove those pieces
            foreach (Char c in source)
            {
                switch (c)
                {
                    case '\r':
                    case '\n':
                    case '\t':
                        continue;
                    case '\'':
                        if (inside && !dquotes)
                        {
                            if (squotes)
                                squotes = false;
                            else
                                squotes = true;
                            continue;
                        }
                        break;
                    case '"':
                        if (inside && !squotes)
                        {
                            if (dquotes)
                                dquotes = false;
                            else
                                dquotes = true;
                            continue;
                        }
                        break;
                    case '>':
                        if (dquotes || squotes) ///if we're within the inside and we are in quotes then we have to keep going
                            continue;
                        inside = false;
                        continue;
                    case '<':
                        if (dquotes || squotes) ///if we're in quotes then we have to keep going
                            continue;
                        inside = true;
                        continue;
                }

                if (!inside)
                {
                    array.Add(c);
                }
            }

            return new String(array.ToArray()).Trim();
        }

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
