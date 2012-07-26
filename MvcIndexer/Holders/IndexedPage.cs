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

        private Dictionary<String, IndexedPage> _pages = new Dictionary<String, IndexedPage>();

        public Dictionary<String, IndexedPage> Pages { get { return _pages; } set { throw new Exception("Cannot set IndexedPages.Pages"); } }



        public IEnumerable<Link> GetUncrawledLinks()
        {
            List<Link> Uncrawled = new List<Link>();
            _pages.Map(p =>
            {
                p.Value.Links.LinkBundle.Map(l =>
                {
                    if (!l.Crawled)
                    {
                        if(!Uncrawled.Contains(l))
                            Uncrawled.Add(l);
                    }
                });
            });
            return Uncrawled;
        }
    }

    public class IndexedPage
    {
        public String[] Keywords = null;
        public Int32 Priority = 0;
        private String _content = "";
        public String Title = "";

        public String PureContent = "";

        public Links Links = null;

        public IndexedPage(String Html)
        {
            _content = Html;
        }
    }

    public class Links
    {
        public List<Link> LinkBundle = new List<Link>();

        public IEnumerable<Link> GetUncrawledLinks()
        {
            List<Link> Uncrawled = new List<Link>();
            LinkBundle.Map(l =>             
            {
                if (!l.Crawled)
                {
                    if (!Uncrawled.Contains(l))
                        Uncrawled.Add(l);
                }
            });
            return Uncrawled;
        }
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

    public class Link
    {
        public Boolean Crawled = false;
        public String Url = "";
    }
}
