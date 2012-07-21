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
            /// -- might be able to find based on css class or ids ?
            return null;
        }


        private void CrawlPages(String SeedUrl)
        {
            string htmlText = "";
            try
            {
                htmlText = GetWebText(ref SeedUrl, false);
                LinksToCrawl.Links[SeedUrl] = new CrawlInfo(1, true, SeedUrl, SeedUrl);
            }
            catch (Exception)
            {
                LinksToCrawl.Links[SeedUrl] = new CrawlInfo(1, false, SeedUrl, SeedUrl);
            }

            LinkParser.ParseLinks(htmlText, SeedUrl);

            

            ThreadPool.SetMaxThreads(5, 5);
            int LoopWithoutNewUrlsCount = 0;

            while (LoopWithoutNewUrlsCount < 3)
            {
                //Logger.Instance.PushLog();

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
            Boolean resourceNotFound = false;
            if (url.Contains(URLIP) || url.Contains("www.multitech.com") || url.Contains("support.multitech.com") || url.Contains("multitech.net") || (!url.Contains("/") && !url.Contains(@"\")) || url.StartsWith("/"))
            {
                String oldurl = url; ///oldurl will be the one that contains a possible go handle url.
                //Boolean StopLooping = false;
                string htmlText = "";
                try
                {
                    htmlText = GetWebText(ref url, false);
                }

                finally
                {
                    resourceNotFound = htmlText.Contains("Resource Not Found");
                    if (resourceNotFound || htmlText == "" || htmlText == "FAILURE")
                        MarkUrl(oldurl, false, true, true);
                    else if (htmlText == "SUCCESS")
                        MarkUrl(oldurl, true, true, false);
                    //if (oldurl != url)
                    //    MarkUrl(oldurl, false, true);
                }
                //Console.WriteLine("\turl translated: " + url);
                if (oldurl != url && oldurl + "/" != url && !resourceNotFound)
                {
                    LinksToCrawl.Links.Urls[oldurl].RealUrl = url;
                }
                RemoveDropDownMenuCodeFromHtmlText(ref htmlText);
                //RemoveSideMenuFromHtmlText(ref htmlText);
                RemoveFooterFromHtmlText(ref htmlText);

                String[] splits = url.Split(new char[] { '/' });


                LinkParser.ParseLinks(htmlText, url);

                CSSClassParser classParser = new CSSClassParser();
                classParser.ParseForCssClasses(htmlText);


                //Add data to main data lists
                //AddRangeButNoDuplicates(_externalUrls, linkParser.ExternalUrls);
                //AddRangeButNoDuplicates(_otherUrls, linkParser.OtherUrls);
                //AddRangeButNoDuplicates(_failedUrls, linkParser.BadUrls);
                AddRangeButNoDuplicates(_classes, classParser.Classes);

                //foreach (string exception in linkParser.Exceptions)
                //    _exceptions.Add("Link parse exception: " + exception + "on page : " + obj.ToString());

                RemoveSideMenuFromHtmlText(ref htmlText);
                #region SaveFile
                Boolean Saved = false;
                
                //{
                String tmpUrl = url.ToLower();
                if (!url.EndsWith("/"))
                    tmpUrl = tmpUrl + "/";
                Uri tmpUri = new Uri(BASEURI, tmpUrl);

                ///Need to save HTML content
                #endregion

                MarkUrl(oldurl, true, Saved, false);
                //MarkUrl(url, Saved, false);
                //}
                //else
                //{
                //    MarkUrl(oldurl, Saved, false); ///they should both be the same, but because of an added "/" in some cases the oldurl should be more accurate, but the url can still be saved
                //}

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

        private static String[] GetLinks(String Url)
        {
            return null;
        }

        private static String GetHtml(String Url)
        {
            return "";
        }       
    }

    internal class LinkParser
    {
        private const string _LINK_REGEX = "href=\"[ \t\r\n]*[ a-zA-Z./:&\\d_-]+\"";
      

        public static void ParseLinks(String HtmlText/*Page page*/, string sourceUrl)
        {
            //LinkFixers lf = new LinkFixers();
            #region LINK_REGEX
            MatchCollection matches = Regex.Matches(HtmlText, _LINK_REGEX);

            for (int i = 0; i <= matches.Count - 1; i++)
            {
                Match anchorMatch = matches[i];

                //if (anchorMatch.Value.ToLower().Contains("cohen")) { 
                //    Console.Write("BOO!"); }
                if (anchorMatch.Value == String.Empty)
                {
                    if (!LinksToCrawl.Links.BlankUrls.Contains(sourceUrl))
                        LinksToCrawl.Links.BlankUrls.Add(sourceUrl);
                    continue;
                }

                string foundHref = "";
                try
                {
                    foundHref = anchorMatch.Value.Replace("href=\"", "");
                    foundHref = foundHref.Substring(0, foundHref.IndexOf("\"")).ToLower().Replace("\r", "").Replace("\n", "").Trim().ToLower();
                    if (Regex.IsMatch(foundHref, "res+([0-9]{1,7}).asp"))
                    {
                        if (foundHref.EndsWith(".asp"))
                            foundHref = foundHref + "x";
                        //Console.WriteLine("\thref resolution: " + foundHref);
                        //continue;
                    }
                }
                catch (Exception exc)
                {
                    LinksToCrawl.Links.NewUrls[sourceUrl].Exceptions.Add("Error parsing matched href: " + exc.Message);
                }
                //if (sourceUrl.Contains("/support/help/resolutions/res")) ///filter out all the ftp.go links that won't work
                //    continue;
                //else if ((!foundHref.Contains("multitech.prv") && foundHref.StartsWith("http")) &&
                //    (!foundHref.Contains("multitech.com") && foundHref.StartsWith("http")))
                //    continue;
                String[] urlsplits = sourceUrl.Split(new String[] { "/" }, StringSplitOptions.None);

                if (urlsplits.Length > 1)
                {
                    String[] urldoublesplit = urlsplits[urlsplits.Length - 1].Split(new String[] { "." }, StringSplitOptions.None);
                    String[] foundhrefsplit = foundHref.Split(new String[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                    if ((foundhrefsplit[0] == urldoublesplit[0] && urldoublesplit[urldoublesplit.Length - 1].Contains(foundhrefsplit[foundhrefsplit.Length - 1])) || (urldoublesplit[0] == "" && foundHref == "default.asp"))
                    {
                        //if(!LinksToCrawl.Links.NewUrls.ContainsKey(foundHref))
                        //    LinksToCrawl.Links.NewUrls[foundHref] = new CrawlInfo(0, sourceUrl, foundHref, PageType.OldAsp);
                        //_otherUrls.Add(foundHref);
                        continue;
                    }
                }
                String realUrl = foundHref;
                if (realUrl.EndsWith(".go"))
                {
                    realUrl = Crawler.GetUrlFromGoHandler(foundHref);
                    if (realUrl == foundHref)
                    {
                        if (!realUrl.Contains("/email/"))
                        { //log it if its not an email go url
                            Logger.Instance.Log("Invalid Go url (" + foundHref + ") found on page: " + sourceUrl);
                        }
                        continue;//some ignored go handles
                    }

                }

                realUrl = LinkFixers.FixPath(sourceUrl, realUrl);

                //dumb fix
                //realUrl = realUrl.Replace("en_us/en_us", "en_us");

                if (IsExternalUrl(realUrl))
                {
                    if (LinksToCrawl.Links.NewUrls.ContainsKey(realUrl))
                    {
                        if (!LinksToCrawl.Links.NewUrls[realUrl].PagesFoundOn.Contains(sourceUrl))
                            LinksToCrawl.Links.NewUrls[realUrl].PagesFoundOn.Add(sourceUrl);
                    }
                    else
                    {
                        LinksToCrawl.Links.NewUrls[realUrl] = new CrawlInfo(0, sourceUrl, realUrl, PageType.External);
                        if (foundHref.EndsWith(".go"))
                            LinksToCrawl.Links.NewUrls[realUrl].Url = foundHref;
                    }
                    continue;
                }


                if (realUrl.Contains("site_map.asp") ||
                    realUrl.Contains("primers.asp") ||
                    realUrl.Contains("primers/voip.asp") ||
                    realUrl.Contains("primers/wireless.asp") ||
                    realUrl.Contains("/partners/sales_channels/online/") ||
                    realUrl.Contains("/external_device_networking/external_wireless_modems/") ||
                    realUrl.Contains("/external_device_networking/global_modems/approvals.asp")
                    )
                {
                    // do nothing: we don't want the sitemap files in the search index; redirect files corrupt the Crawler
                    continue;
                }
                else
                {
                    if (!LinksToCrawl.Links.NewUrls.ContainsKey(realUrl))//!GoodUrls.Contains(foundHref))
                    {
                        if (!IsAWebPage(realUrl))
                        { //sourceUrl
                            if (!foundHref.Equals(String.Empty))
                            {
                                //realUrl = lf.FixPath(sourceUrl, realUrl);
                                if (!LinksToCrawl.Links.NewUrls.ContainsKey(realUrl))
                                    LinksToCrawl.Links.NewUrls[realUrl] = new CrawlInfo(0, sourceUrl, realUrl, PageType.CssOrPic);
                                //if(!_otherUrls.Contains(foundHref)){
                                //_otherUrls.Add(foundHref);}
                            }
                        }
                        else
                        {
                            //realUrl = lf.FixPath(sourceUrl, realUrl);
                            //if (!GoodUrls.Contains(foundHref))
                            //{
                            //    GoodUrls.Add(foundHref);
                            //}
                            //Boolean testadd;
                            if (foundHref.EndsWith(".go"))
                            {
                                //realUrl = Crawler.GetUrlFromGoHandler(foundHref);
                                //if (realUrl == foundHref)
                                //    continue;//some ignored go handles

                                if (!LinksToCrawl.Links.NewUrls.ContainsKey(realUrl))
                                {
                                    LinksToCrawl.Links.NewUrls[realUrl] = new CrawlInfo(0, false, sourceUrl, foundHref, realUrl);
                                }
                            }
                            else
                            {
                                if (!LinksToCrawl.Links.NewUrls.ContainsKey(realUrl))
                                {
                                    LinksToCrawl.Links.NewUrls[realUrl] = new CrawlInfo(0, false, sourceUrl, foundHref);//, foundHref);
                                }
                            }
                        }
                    }
                }
            }
            #endregion
            #region IMGSRC_REGEX
            MatchCollection imgsrc = Regex.Matches(HtmlText, _IMGSRC_REGEX);
            foreach (Match anchorMatch in imgsrc)
            {
                //if(anchorMatch.Value.ToLower().Contains("cohen"))
                //{
                //    Console.Write("Found the Cohen image");
                //}
                if (anchorMatch.Value == String.Empty)
                {
                    if (!LinksToCrawl.Links.BlankUrls.Contains(sourceUrl))
                        LinksToCrawl.Links.BlankUrls.Add(sourceUrl);
                    //BadUrls.Add("Blank url value on page " + sourceUrl);
                    continue;
                }

                string foundHref = null;
                try
                {
                    foundHref = anchorMatch.Value.Replace("src=\"", "");
                    foundHref = foundHref.Substring(0, foundHref.IndexOf("\"")).ToLower();
                }
                catch (Exception exc)
                {
                    LinksToCrawl.Links.NewUrls[sourceUrl].Exceptions.Add("Error parsing matched href: " + exc.Message);
                    //Exceptions.Add("Error parsing matched href: " + exc.Message);
                }
                //if (!LinksToCrawl.Links.NewUrls.ContainsKey(foundHref))//!GoodUrls.Contains(foundHref))
                //{                  
                if (!IsAWebPage(foundHref))
                { //sourceUrl
                    foundHref = LinkFixers.FixPath(sourceUrl, foundHref);
                    if (!foundHref.Equals(String.Empty))
                    {
                        if (!LinksToCrawl.Links.NewUrls.ContainsKey(foundHref))
                            LinksToCrawl.Links.NewUrls[foundHref] = new CrawlInfo(0, sourceUrl, foundHref, PageType.CssOrPic);
                        //_otherUrls.Add(foundHref);
                        //if (!foundHref.Contains("www.multitech.com") && !foundHref.Contains("stage.multitech.prv"))
                        //{
                        //    foundHref = foundHref.Replace("http://", "");
                        //    if(foundHref.StartsWith("/"))
                        //    {
                        //        foundHref = Crawler.CURRENT_URL + foundHref;
                        //    }
                        //    else
                        //    {
                        //        foundHref = Crawler.CURRENT_URL + "/" + foundHref;
                        //    }
                        //}
                        //else if (!foundHref.Contains("en_us"))
                        //{
                        //    foundHref = foundHref.Replace("http://stage.multitech.prv/", "").Replace("http://www.multitech.com/", "");
                        //    foundHref = Crawler.CURRENT_URL + "/" + foundHref;
                        //}
                        if (IsExternalUrl(foundHref))
                        {
                            LinksToCrawl.Links.NewUrls[foundHref].Type = LinksToCrawl.Links.NewUrls[foundHref].Type | PageType.External;
                        }
                        else if (!File.Exists(foundHref.Replace(Crawler.CURRENT_URL + "/", Crawler.CURRENT_INETPUB).Replace("http://www.multitech.com/en_us/", Crawler.CURRENT_INETPUB).Replace("/", @"\")))
                        {
                            //_badUrls.Add(foundHref + " on page at ("+ sourceUrl +")");
                            LinksToCrawl.Links.NewUrls[foundHref].Type = LinksToCrawl.Links.NewUrls[foundHref].Type | PageType.Bad;
                        }
                    }
                    //}                
                }
            }
            #endregion
            #region IMGURL_REGEX
            MatchCollection imgurl = Regex.Matches(HtmlText, _IMGURL_REGEX);
            foreach (Match anchorMatch in imgurl)
            {
                if (anchorMatch.Value == String.Empty)
                {
                    if (!LinksToCrawl.Links.BlankUrls.Contains(sourceUrl))
                        LinksToCrawl.Links.BlankUrls.Add(sourceUrl);
                    //BadUrls.Add("Blank url value on page " + sourceUrl);
                    continue;
                }

                string foundHref = null;
                try
                {
                    foundHref = anchorMatch.Value.Replace("url(\"", "");
                    foundHref = foundHref.Substring(0, foundHref.IndexOf("\")")).ToLower();
                }
                catch (Exception exc)
                {
                    LinksToCrawl.Links.NewUrls[sourceUrl].Exceptions.Add("Error parsing matched href: " + exc.Message);
                    //Exceptions.Add("Error parsing matched href: " + exc.Message);
                }
                if (!LinksToCrawl.Links.NewUrls.ContainsKey(foundHref))//!GoodUrls.Contains(foundHref))
                {
                    if (!IsAWebPage(foundHref))
                    { //sourceUrl
                        foundHref = LinkFixers.FixPath(sourceUrl, foundHref);
                        if (!foundHref.Equals(String.Empty))
                        {
                            if (!LinksToCrawl.Links.NewUrls.ContainsKey(foundHref))
                                LinksToCrawl.Links.NewUrls[foundHref] = new CrawlInfo(0, sourceUrl, foundHref, PageType.CssOrPic);
                            //_otherUrls.Add(foundHref);
                            if (!File.Exists(foundHref.Replace(Crawler.CURRENT_URL, Crawler.CURRENT_INETPUB).Replace("/", @"\")))
                            {
                                //if (!LinksToCrawl.Links.NewUrls.ContainsKey(foundHref))
                                LinksToCrawl.Links.NewUrls[foundHref].Type = LinksToCrawl.Links.NewUrls[foundHref].Type | PageType.Bad;// = new CrawlInfo(0, sourceUrl, foundHref, PageType.Bad);
                                //_badUrls.Add(foundHref + " on page at (" + sourceUrl + ")");
                            }
                        }
                    }
                }
            }
            #endregion
        }



    }

    public class Links
    {

    }
}
