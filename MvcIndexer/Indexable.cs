using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using MvcIndexer.Holders;
using MvcIndexer.Extensions;
using MvcIndexer.Holders;

namespace MvcIndexer
{
    [AttributeUsage(AttributeTargets.Method)] ///handle classes also in the future.
    public sealed class Indexable : System.Attribute
    {
        /// <summary>
        /// Additional portions of Url to be used - 
        /// appending to Path (if provided) or what can be 
        /// assumed based on the Controller name and Method name
        /// ex: /15/potato
        /// result: 
        /// "/{controller}/{action}/15/potato or"
        /// Path + "/15/potato" if provided
        /// ex: ?q=mvc&type=all
        /// result: 
        /// "/{controller}/{action}?q=mvc&type=all" or
        /// Path + "?q=mvc&type=all" if provided
        /// 
        /// **Provide a blank string if Path alone is also to be indexed
        /// </summary>
        public String[] AdditionalUrl { get; set; }
        /// <summary>
        /// if Path differs from standard /{controller}/{action}
        /// </summary>
        public String Path { get; set; }
        /// <summary>
        /// Used when each keyword has a different priority
        /// </summary>
        public Dictionary<String, Int32> KeywordsAndPriority { get; set; }
        /// <summary>
        /// Set of keywords that use the Priority value
        /// </summary>
        public String[] Keywords { get; set; }
        /// <summary>
        /// Priority in results 
        /// (1-101; 101 for absolute top priority, 1-100 for smart priority)
        /// </summary>
        public Int32 Priority { get; set; }

        protected internal static IndexedPages GetIndexable()
        {
            List<MethodInfo> mi = new List<MethodInfo>(GetAllIndexableMethods());
            //IndexUrls urls = new IndexUrls();
            IndexedPages pages = new IndexedPages();

            foreach (MethodInfo method in mi)
            {
                Indexable i = (Indexable)Attribute.GetCustomAttribute(method, typeof(Indexable));
                
                if (i.AdditionalUrl != null && i.AdditionalUrl.Length > 0)
                {
                    foreach(String addurl in i.AdditionalUrl)
                    {
                        String UrlPath = "";
                        if(i.Path == null || i.Path == "")
                        {
                            if (addurl == "")
                                UrlPath = "/" + method.DeclaringType.Name.Replace("Controller", "") + "/" + method.Name;
                            else
                                UrlPath = "/" + method.DeclaringType.Name.Replace("Controller", "") + "/" + method.Name + "/" + i.AdditionalUrl;
                        }
                        else
                        {
                            if (addurl == "")
                                UrlPath = i.Path;
                            else
                            {
                                if (i.Path.EndsWith("/"))
                                    UrlPath = i.Path.Substring(0, i.Path.Length - 1) + "/" + addurl;
                                else
                                    UrlPath = i.Path + "/" + addurl;
                            }
                        }
                        
                        pages.AddLink(new Link()
                        {
                            Crawled = false,
                            Page = new Page(UrlPath, i.Priority, 
                                i.KeywordsAndPriority, 
                                i.Keywords)
                        });
                    }
                }
            }
            return pages;
        }

        private static IEnumerable<MethodInfo> GetAllIndexableMethods()
        {
            ///need to keep in mind that class with Indexable is possible?
            List<MethodInfo> mi = new List<MethodInfo>();
            Assembly.GetExecutingAssembly().GetTypes().Map(t =>
                t.GetMethods().Map(m =>
                {
                    if (Attribute.GetCustomAttributes(m, typeof(Indexable)).Count() > 0)
                        mi.Add(m);
                }));
            return mi;
        }
    }
}
