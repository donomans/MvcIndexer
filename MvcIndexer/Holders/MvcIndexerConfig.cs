using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvcIndexer.Holders
{
    public class MvcIndexerConfig
    {
        /// <summary>
        /// Root domain of the index that every URL on site should contain (excluding external URLs)
        /// Ex: http://www.google.com
        /// </summary>
        public String RootDomain { get; set; }
        /// <summary>
        /// Optionally set a seed url, otherwise Indexer will use first found 
        /// url built from Root Domain + Indexable Attribute
        /// </summary>
        public String SeedUrl { get; set; }

        /// <summary>
        /// Type of crawl/indexing to do
        /// </summary>
        public IndexType IndexType { get; set; }

        /// <summary>
        /// Required if IndexType is IndexType.Scheduled
        /// Defaults to 12:00 AM
        /// </summary>
        public DateTime ScheduledStart { get; set; }

        /// <summary>
        /// Automatically filter out the master page from the page content
        /// </summary>
        public Boolean AutoDetectAndRemoveMaster { get; set; }

        /// <summary>
        /// File containing urls and priority.
        /// Necessary to provide a proper priority on complex paths
        /// where an ID or other variable is commonly used to offer 
        /// unique information
        /// </summary>
        public String ConfigurationFile { get; set; }

        /// <summary>
        /// Custom filters to run on each page after a url get
        /// It will run in order provided and 
        /// before any link parsing happens (assuming UrlDiscovery is true)
        /// </summary>
        public HtmlFilter[] Filters { get; set; }

        /// <summary>
        /// Decide whether or not all urls found on pages of Indexable urls should be indexed or not.
        /// Indexed pages other than those in Indexable Controllers/Actions will be given priority 
        /// determined by content if set to true.
        /// If false, urls found on pages that are on Controllers/Actions not included in Indexable 
        /// will be ignored.
        /// </summary>
        public Boolean UrlDiscovery { get; set; }

        /// <summary>
        /// List of words that should be ignored in the index.
        /// </summary>
        public List<String> StopWords { get; set; }

        /// <summary>
        /// Custom weighting function to be run on each keyword for a given page.
        /// </summary>
        public Prioritize KeywordPrioritizer { get; set; }

        /// <summary>
        /// When true, use KeywordPrioritizer in addition to normal priority calculations.  
        /// Otherwise. only use KeywordPrioritizer if it is provided and only use internal calculations if not.
        /// </summary>
        public Boolean UseAdditionalWeighting { get; set; }
    }

    /// <summary>
    /// Takes HTML and spits out modified HTML to use in Index
    /// </summary>
    /// <param name="Html">Unfiltered HTML from WebRequest</param>
    /// <returns>HTML to use in index</returns>
    public delegate String HtmlFilter(String Html);

    /// <summary>
    /// Takes the url, content, title, and a keyword and returns a weighting.
    /// </summary>
    /// <param name="Url">Url of the page</param>
    /// <param name="StrippedContent">Content</param>
    /// <param name="Title"></param>
    /// <param name="Keyword"></param>
    /// <returns></returns>
    public delegate Int32 Prioritize(String Url, String StrippedContent, String Title, String Keyword);


    public enum IndexType
    {
        Continuous,
        Scheduled
    }
}
