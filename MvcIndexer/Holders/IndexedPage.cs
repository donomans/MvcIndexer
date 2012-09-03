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
                        {
                            retlink.PagesFoundOn.Add(p);
                        }
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

  

    public class Page
    {
        private static readonly HashSet<String> stopwords = new HashSet<String>()
        {
            "a", "at", "this" ///etc.
        };

        public Page(String url, String content, String title = "")
        {
            Url = url;
            _content = content;
            _strippedcontent = content;
            Title = title != "" ? title : GetTitle(content);
        }
        public Page(String url, Int32 priority, Dictionary<String, Int32> keywordspriority, Dictionary<String, Int32> keywords)
        {
            Url = url;
            _keywords = keywords;
            _keywordspriority = keywordspriority;
        }
        
        private Dictionary<String, Int32> _keywords;
        /// <summary>
        /// Keywords and the locations within the StrippedContent
        /// </summary>
        public Dictionary<String, Int32> Keywords
        {
            get { return _keywords; }
            //set { _keywords = value; }
        }
        private Dictionary<String, Int32> _keywordspriority;
        public Dictionary<String, Int32> KeywordsPriority
        {
            get { return _keywordspriority; }
            //set { _keywordspriority = value; }
        }
        public Int32 Priority = 0; ///won't always be used. only used if provided in the initial creation from the Indexable attribute
        private String _content = "";
        private String _strippedcontent = "";
        public String Title = "";
        public String Url = "";
        public String PureContent 
        {
            get{ return _content;}
            //set { _content = value; }
        }
        public String StrippedContent
        {
            get { return _strippedcontent; }
            //set { _strippedcontent = value; }
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
            return title;
        }
        
        private async Task<Dictionary<String, Int32>> FillKeywords()
        {
            ///2) keywords
            ///     - need a list of junk words
            ///     - find common words in the StrippedContent that aren't in the junk word list
            String[] potentialkeywords = StrippedContent.ToLower().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<String, Int32> wordfrequency = new Dictionary<String, Int32>(potentialkeywords.Length);
            foreach (String potentialkeyword in potentialkeywords)
            {
                if (stopwords.Contains(potentialkeyword))
                    continue;
                if (wordfrequency.ContainsKey(potentialkeyword))
                    wordfrequency[potentialkeyword]++;
                else
                    wordfrequency.Add(potentialkeyword, 1);
            }
            List<String> frequentwords = new List<String>(wordfrequency.TakeByValueDescending(20));
            ///3) priority/weighting
            /// -- how to determine weighting?
            ///     - frequency?
            ///     - matching page title?
            ///     - (in querier the locality should give extra weighting in the ordering of results)
            ///     - 
            
            ///4) locations where the determined/provided keywords reside in the document

            return null;
        }
        private async Task<Dictionary<String, Int32>> FillKeywordsPriority()
        {

            return null;
        }

        public async Task RunFilters(HtmlFilter[] filters)
        {
            foreach (HtmlFilter filter in filters)
                _strippedcontent = filter(_strippedcontent);
        }

        public async void StripHtml() 
        {
            ///strip the html and then populate the keywords dictionaries
            _strippedcontent = StripHtml(_strippedcontent);
            _keywords = await FillKeywords();
            _keywordspriority = await FillKeywordsPriority();
        }

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
    }       


    public class Link
    {
        public Boolean Crawled = false;
        public String Url 
        {
            get{ return Page != null ? Page.Url : "";}
        }

        public Page Page = null;//new Page();
        public List<Page> PagesFoundOn = new List<Page>(); ///is this useful?  do I care where they were found?
    }
}
