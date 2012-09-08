using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using MvcIndexer.Extensions;
using System.Threading.Tasks;

namespace MvcIndexer.Holders
{
    public class IndexedPages: IEnumerable<Link>
    {
        public static readonly HashSet<String> STOPWORDS = new HashSet<String>()
        {
            "a", "at", "this" ///etc.
        };
        /// - Needs a means of keeping track of all the links added to it so that it can help the crawler (loop through and find links that haven't been crawled yet?)
        /// - Needs 
        /// 

        private Dictionary<String, Link> _pages = new Dictionary<String, Link>();

        //public static IndexedPages operator +(IndexedPages pagesA, IndexedPages pagesB)
        //{
        //    IndexedPages pages = new IndexedPages(); ///is this going to be a leaky?
        //    foreach (Link link in pagesA)
        //    {
        //        pages.AddLink(link);
        //    }
        //    foreach (Link link in pagesB)
        //    {
        //        pages.AddLink(link);
        //    }
        //    return pages;
        //}

        public Link this[String url]
        {
            get { return _pages[url]; }
            set { _pages[url] = value; }
        }

        public IEnumerable<Link> GetUncrawledLinks()
        {            
            List<Link> Uncrawled = new List<Link>();
            _pages.Map(p =>
            {
                if (!p.Value.Crawled)
                {
                    if(!Uncrawled.Contains(p.Value))
                        Uncrawled.Add(p.Value);
                }
            });
            return Uncrawled;
        }

        /// <summary>
        /// Add a link safely to the collection after parsing
        /// </summary>
        /// <param name="link"></param>
        public void AddLink(Link link)
        {
            ///check if link is unique (judging by the url) and either merge or add it to an indexedpage
            Link linkforpages = FindPage(link);
            _pages[linkforpages.Url] = linkforpages;
        }

        public void AddLink(String url)
        {
            throw new NotImplementedException();
        }

        public void AddLinks(IndexedPages pages)
        {
            foreach (Link link in pages)
                AddLink(link);
        }

        /// <summary>
        /// Find and merge links
        /// </summary>
        /// <param name="link"></param>
        /// <param name="retlink">Link</param>
        /// <returns>
        /// Always returns a safe to use link
        /// </returns>
        private Link FindPage(Link link)
        {
            Link retlink = link;
            Boolean duplicate = false;
            ///get the matching links (should only be 1 at most if this is consistent)
            if (_pages.ContainsKey(link.Url))
            {
                retlink = _pages[link.Url];
                duplicate = true;
            }
            
            ///merge duplicates PagesFoundOn
            ///Do I need to do anything with the other data, or just assume the new link takes priority?
            if (duplicate)
            {
                ///map through all pagesfoundon (should only be one in the "link" but possibly many in the dupe) 
                ///and add it to the list instead of adding the new link
                link.PagesFoundOn.Map(p =>
                    {
                        if (!retlink.PagesFoundOn.Contains(p, (l, i) => l.GetHashCode() == i.GetHashCode()))
                            retlink.PagesFoundOn.Add(p);
                    });
            }
           
            return retlink;
        }


        public IEnumerator<Link> GetEnumerator()
        {
            return _pages.Values.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class KeywordInfo
    {
        public Int32[] Location = null;
        public Int32 Priority;
    }

    public class Page
    {
        public Page(String url, String content, String title = "")
        {
            Url = url;
            _content = content;
            _strippedcontent = content;
            Title = title != "" ? title : GetTitle(content);
        }
        public Page(String url, Int32 priority, Dictionary<String, Int32> keywordspriority, String[] keywords)
        {
            Url = url;

            ///combine keywordspriority and keywords in the Dictionary<String, KeywordInfo> of _keywords
            _keywords = keywords.ToDictionaryKey<String, KeywordInfo>(k =>  new KeywordInfo());
            foreach (KeyValuePair<String, Int32> keypriority in keywordspriority)
            {
                if (_keywords.ContainsKey(keypriority.Key))
                    _keywords[keypriority.Key].Priority = keypriority.Value;
                else
                    _keywords.Add(keypriority.Key, new KeywordInfo() { Priority = keypriority.Value });
            }
        }
        
        private Dictionary<String, KeywordInfo> _keywords;
        /// <summary>
        /// Keywords, the locations within the StrippedContent, and their weight
        /// </summary>
        public Dictionary<String, KeywordInfo> Keywords
        {
            get { return _keywords; }
        }
        public Int32 Priority = 0; ///won't always be used. only used if provided in the initial creation from the Indexable attribute
        private String _content = "";
        private String _strippedcontent = "";
        public String Title = "";
        public String Url = "";
        public String PureContent 
        {
            get{ return _content;}
        }
        public String StrippedContent
        {
            get { return _strippedcontent; }
        }


        private static String GetTitle(String content)
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
            return StripHtml(title);
        }
        
        private async Task FillKeywords()
        {
            ///2) keywords
            ///     - need a list of junk words
            ///     - find common words in the StrippedContent that aren't in the junk word list
            String[] potentialkeywords = StrippedContent.ToLower().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<String, Int32> wordfrequency = new Dictionary<String, Int32>(potentialkeywords.Length);
            foreach (String potentialkeyword in potentialkeywords)
            {
                ///strip out punctuation
                List<String> strippedkeywords = StripAndStem(potentialkeyword);
                foreach (String strippedkeyword in strippedkeywords)
                {
                    if (IndexedPages.STOPWORDS.Contains(strippedkeyword))
                        continue;
                    if (wordfrequency.ContainsKey(strippedkeyword))
                        wordfrequency[strippedkeyword]++;
                    else
                        wordfrequency.Add(strippedkeyword, 1);
                }
            }
            List<String> frequentwords = new List<String>(wordfrequency.TakeByValueDescending(20));
            foreach (String keyword in frequentwords)
            {
                if (_keywords.ContainsKey(keyword))
                {
                    ///increase the weight by 4
                    _keywords[keyword].Priority += 4;
                }
                KeywordInfo info = new KeywordInfo();
                

            }
            foreach (KeyValuePair<String, KeywordInfo> kvp in _keywords)
            {
                ///Cycle through all keyvaluepairs in _keywords
                ///3) update locations
                ///4) priority/weighting
                /// -- how to determine weighting?
                ///     - frequency?
                ///     - matching in page title?
                ///     - matching in url
                ///     - priority function provided by user (similar to the HtmlFilters)
                ///     - (in querier the locality should give extra weighting in the ordering of results)
                ///     - frequency * 3 + (1 + 4 for each found keyword that matches a real keyword + 10 for being in the title)
            }
        }

        public async Task RunFilters(HtmlFilter[] filters)
        {
            foreach (HtmlFilter filter in filters)
                _strippedcontent = filter(_strippedcontent);
        }

        public List<String> StripAndStem(String source)
        {   ///®™
            List<String> dashsplits = new List<String>(source.Split(new []{"-", "®", "™"}, StringSplitOptions.RemoveEmptyEntries));
            String Stripped =  new String(source.Where(c => !Char.IsPunctuation(c)).ToArray());
            if (Stripped == "" || dashsplits.Contains(Stripped))
                return dashsplits;
            else
            {
                if (dashsplits.Count == 1 && !Stripped.Contains(dashsplits[0]))
                    return new List<String>() { Stripped };
                else
                {
                    List<String> striplist = new List<String>();
                    foreach (String s in dashsplits)
                        striplist.AddRange(StripAndStem(s));
                    striplist.Add(Stripped);
                    return striplist;
                }
            }
        }
        public async void StripHtml() 
        {
            ///strip the html and then populate the keywords dictionaries
            _strippedcontent = StripHtml(_strippedcontent);
            await FillKeywords();
        }
        
        private static String StripHtml(String source)
        {
            List<Char> array = new List<Char>(source.Length);
            Boolean inside = false;
            Boolean dquotes = false;
            Boolean squotes = false;

            ///1) look for tags or things that need to be fully removed (entire containing contents) and remove them
            //source = source.Replace("<br>", " ").Replace("<br />", " ");
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
            source = source.Replace(Environment.NewLine, "").Replace("<br>", " ").Replace("<br />", " ").Replace("  ", "");

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
    }       


    public class Link
    {
        public Boolean Crawled = false;
        public String Url 
        {
            get{ return Page != null ? Page.Url : "";}
        }

        public Page Page = null;//new Page();
        public List<Page> PagesFoundOn = new List<Page>(); ///this can be used to add extra weighting to a page in search results - if it has lots of links to it, it's probably important.
    }
}
