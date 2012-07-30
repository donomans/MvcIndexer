using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace MvcIndexer.Holders
{
    public class IndexedPages
    {
        /// - Needs a means of keeping track of all the links added to it so that it can help the crawler (loop through and find links that haven't been crawled yet?)
        /// - Needs 
        /// 

        private Dictionary<String, Link> _pages = new Dictionary<String, Link>();

        
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

        /// <summary>
        /// Find and merge links
        /// </summary>
        /// <param name="link"></param>
        /// <param name="retlink">Link</param>
        /// <returns>
        /// True if duplicate was found (update _pages)
        /// False if new link is appropriate
        /// </returns>
        private Link FindPage(Link link)
        {
            Link retlink = link;
            Boolean duplicate = false;
            ///get the matching links (should only be 1 at most if this is consistent)
            _pages.Map(p =>
            {
                if (p.Key == link.Url)
                {
                    retlink = p.Value;
                    duplicate = true;
                }
            });
            
            ///merge duplicates
            if (duplicate)
            {
                ///map through all pagesfoundon (should only be one in the "link" but possibly many in the dupe) and add it to the list
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
    }

    //public class IndexedPage
    //{
    //    public Page Page = new Page();

    //    public Link[] Links = null;

    //    public IndexedPage()
    //    {
    //    }
    //}

    public class Page
    {
        public String[] Keywords = null;
        public Int32 Priority = 0;
        private String _content = "";
        public String Title = "";

        public String PureContent = "";        
    }       

    //public class Links
    //{ //this doesn't seem to be adding any value
    //    public List<Link> LinkBundle = new List<Link>();


    //    public IEnumerable<Link> GetUncrawledLinks()
    //    {//not sure if needed
    //        List<Link> Uncrawled = new List<Link>();
    //        LinkBundle.Map(l =>             
    //        {
    //            if (!l.Crawled)
    //            {
    //                if (!Uncrawled.Contains(l))
    //                    Uncrawled.Add(l);
    //            }
    //        });
    //        return Uncrawled;
    //    }       
    //}

    public class Link
    {
        public Boolean Crawled = false;
        public String Url = "";

        public Page Page = new Page();
        public List<Page> PagesFoundOn = new List<Page>(); ///is this useful?  do I care where they were found?
    }
}
