using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using MvcIndexer.Holders;
using System.Text.RegularExpressions;
using System.Threading;

namespace MvcIndexer
{
    public class MvcIndexer
    {
        private static readonly MvcIndexer _indexer = new MvcIndexer();

        public String Root { get; set; }
        private IndexType type = IndexType.Continuous; 
        private DateTime start = new DateTime();
        private String seed = "";
        public Boolean Discovery { get; set; }

        private IndexUrls urls = null;

        public Boolean AutoMasterRemoval { get; set; }
        public HtmlFilter[] Filters { get; set; }

        static MvcIndexer()
        {
        }

        private MvcIndexer()
        { 
        }

        public static MvcIndexer Indexer
        {
            get { return _indexer; }
            private set { }
        }


        public void SetConfiguration(MvcIndexerConfig config)
        {
            Root = config.RootDomain;
            type = config.IndexType;
            start = config.ScheduledStart;
            seed = config.SeedUrl;
            Discovery = config.UrlDiscovery;

            AutoMasterRemoval = config.AutoDetectAndRemoveMaster;
            Filters = config.Filters;
        }

        public Boolean StartIndexer()
        {
            if (!(Root == ""))
            {
                urls = Indexable.GetIndexable();
                ///get the stuff started
                String seedurl = "";
                if (seed == null || seed == "")
                    seedurl = seed;
                else
                    seedurl = Root + urls.Urls[0].Path;

                IndexedPage[] indexpages = Index.Crawl(seedurl);
                

                return true;
            }
            else
                return false;
        }
    }

    internal class Index
    {
        public static IndexedPage[] Crawl(String SeedUrl, String Host)
        {
            MvcIndexer.Indexer.Root = Host;

            return null;
        }
        public static IndexedPage[] Crawl(String SeedUrl)
        {
            if (MvcIndexer.Indexer.Root == "")
            {
                Uri seed = new Uri(SeedUrl);
                MvcIndexer.Indexer.Root = seed.Host;
            }
            ///0) split off a thread
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
            /// -- might be able to find based on css class or ids ?
            return null;
        }


        private void CrawlPages(String SeedUrl)
        {
            String htmlText;
            try
            {
                if (GetHtml(out htmlText, ref SeedUrl) == (HttpCode.OK200 | HttpCode.Reroute3XX))
                {
                    LinksToCrawl.Links[SeedUrl] = new CrawlInfo(1, true, SeedUrl, SeedUrl);
                }
            }
            catch (Exception)
            {
                htmlText = null;
                LinksToCrawl.Links[SeedUrl] = new CrawlInfo(1, false, SeedUrl, SeedUrl) ;
            }

            LinkParser.ParseLinks(htmlText, SeedUrl);


            
            ThreadPool.SetMaxThreads(5, 5);
            Int32 LoopWithoutNewUrlsCount = 0;

            while (LoopWithoutNewUrlsCount < 3)
            {
                LinksToCrawl.Links.UpdateUrls();

                Boolean LoopWithoutNewUrls = true;

                lock (LinksToCrawl.Lock)
                {
                    foreach (KeyValuePair<String, CrawlInfo> kvp in LinksToCrawl.Links.Urls)
                    {
                        if (!kvp.Value.Crawled && !kvp.Value.GiveUp && kvp.Value.Type == PageType.Normal && !kvp.Value.Exception)
                        {
                            try
                            {
                                ThreadPool.QueueUserWorkItem(new WaitCallback(CrawlCallback), kvp.Key);
                            }
                            catch (Exception ex)
                            {
                                kvp.Value.Exceptions.Add(kvp.Key + " caused an exception: " + ex.Message);// + ", parent urls: " + String.Join(", ", kvp.Value.PagesFoundOn.ToArray()));
                                if (!ex.Message.Contains("The operation has timed out"))
                                    kvp.Value.Exception = true;
                                MarkUrl(kvp.Key, false, false, kvp.Value.Exception);
                            }
                            LoopWithoutNewUrls = false;
                            LoopWithoutNewUrlsCount = 0;
                        }
                    }
                }
                if (LoopWithoutNewUrls)
                    LoopWithoutNewUrlsCount++;

            }
        }
        public void CrawlCallback(object obj)
        {
            String url = obj.ToString();
            if (url.Contains(MvcIndexer.Indexer.Root))
            {
                String oldurl = url; ///oldurl will be the one that contains a possible rerouted url.
                String htmlText;
                try
                {
                    if (GetHtml(out htmlText, ref url) != (HttpCode.OK200 | HttpCode.Reroute3XX))
                    {
                        MarkUrl(oldurl, false, true, true);
                        return;
                    }
                }
                catch(Exception)
                {
                    MarkUrl(oldurl, false, true, true);
                    return;
                }
               
                if (oldurl != url && oldurl + "/" != url)
                {
                    LinksToCrawl.Links.Urls[oldurl].Url = url;
                }

                LinkParser.ParseLinks(htmlText, oldurl);

                LinksToCrawl.Links.Urls[oldurl].Page = new IndexedPage(htmlText);

                foreach (HtmlFilter htmlfilter in MvcIndexer.Indexer.Filters)
                {
                    htmlText = htmlfilter(htmlText);
                }

                if (MvcIndexer.Indexer.AutoMasterRemoval)
                {
                    ///remove the master or something?
                }

                LinksToCrawl.Links.Urls[oldurl].Page.PureContent = htmlText;

                MarkUrl(oldurl, true, true, false);
            }
            else
                MarkUrl(url, false, true, false);
        }

        private void MarkUrl(String Url, Boolean Crawled, Boolean GiveUp, Boolean Exception)
        {
            LinksToCrawl.Links.Urls[Url].Crawled = Crawled;
            LinksToCrawl.Links.Urls[Url].Exception = Exception;

            LinksToCrawl.Links.Urls[Url].GiveUp = GiveUp;

            if (++LinksToCrawl.Links.Urls[Url].TryCount > 4 && !LinksToCrawl.Links.Urls[Url].Crawled)
                LinksToCrawl.Links.Urls[Url].GiveUp = true;
        }


        private static HttpCode GetHtml(out String HtmlText, ref String Url)
        {
            ///get text, check retrieved url after reroutes, and return httpcode
            HtmlText = "";
            return HttpCode.NotFound404;
        }
        private enum HttpCode
        {
            OK200 = 200,
            NotFound404 = 404,
            SomethingBad4XX = 4,
            Reroute3XX = 3
        }
    }

    internal class LinkParser
    {
        private const String LINK_REGEX = "href=\"[ \t\r\n]*[ a-zA-Z./:&\\d_-]+\"";

        public static void ParseLinks(String HtmlText, String sourceUrl)
        {
            MatchCollection matches = Regex.Matches(HtmlText, LINK_REGEX);

            for (Int32 i = 0; i <= matches.Count - 1; i++)
            {
                Match anchorMatch = matches[i];

                if (anchorMatch.Value == String.Empty)
                {
                    if (!LinksToCrawl.Links.BlankUrls.Contains(sourceUrl))
                        LinksToCrawl.Links.BlankUrls.Add(sourceUrl);
                    continue;
                }

                String foundHref = "";
                try
                {
                    foundHref = anchorMatch.Value.Replace("href=\"", "");
                    foundHref = foundHref.Substring(0, foundHref.IndexOf("\"")).ToLower().Replace("\r", "").Replace("\n", "").Trim().ToLower();
                }
                catch (Exception exc)
                {
                    LinksToCrawl.Links.NewUrls[sourceUrl].Exceptions.Add("Error parsing matched href: " + exc.Message);
                }
                               
                String realUrl = LinkFixers.FixPath(sourceUrl, foundHref);

                if (realUrl == "" || foundHref == "")
                    continue;

                if (!realUrl.Contains(MvcIndexer.Indexer.Root))
                {
                    if (LinksToCrawl.Links.NewUrls.ContainsKey(realUrl))
                    {
                        if (!LinksToCrawl.Links.NewUrls[realUrl].PagesFoundOn.Contains(sourceUrl))
                            LinksToCrawl.Links.NewUrls[realUrl].PagesFoundOn.Add(sourceUrl);
                    }
                    else
                    {
                        LinksToCrawl.Links.NewUrls[realUrl] = new CrawlInfo(0, sourceUrl, realUrl, PageType.External);
                    }
                    continue;
                }

                if (LinksToCrawl.Links.NewUrls.ContainsKey(realUrl))
                {
                    if (!LinksToCrawl.Links.NewUrls[realUrl].PagesFoundOn.Contains(sourceUrl))
                        LinksToCrawl.Links.NewUrls[realUrl].PagesFoundOn.Add(sourceUrl);                   
                }
                else
                {
                    if (IsAWebPage(realUrl))
                    {
                        if (!LinksToCrawl.Links.NewUrls.ContainsKey(realUrl))
                        {
                            LinksToCrawl.Links.NewUrls[realUrl] = new CrawlInfo(0, false, sourceUrl, foundHref);
                        }

                    }
                }
                
            }
        }
        private static bool IsAWebPage(String foundHref)
        {
            if (foundHref.Contains("javascript:"))
                return false;

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


    }
}
