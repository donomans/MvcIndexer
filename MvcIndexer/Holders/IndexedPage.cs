using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using MvcIndexer.Extensions;

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
        /// <summary>
        /// Keywords and the locations within the StrippedContent
        /// </summary>
        public Dictionary<String, Int32> Keywords = new Dictionary<String, Int32>();
        public Dictionary<String, Int32> KeywordPriority = null;// = new Dictionary<String, Int32>();///won't always be used.
        public Int32 Priority = 0;
        private String _content = "";
        private String _strippedcontent = "";
        public String Title = "";
        public String Url = "";
        public String PureContent 
        {
            get{ return _content;}
            set
            { 
                _content = value;
            }
        }
        public String StrippedContent
        {
            get { return _strippedcontent; }
            set { _strippedcontent = value; }
        }
        
    }       


    public class Link
    {
        public Boolean Crawled = false;
        public String Url 
        {
            get{ return Page != null ? Page.Url : "";}
        }

        public Page Page = new Page();
        public List<Page> PagesFoundOn = new List<Page>(); ///is this useful?  do I care where they were found?
    }
}
