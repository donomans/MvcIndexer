using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MvcIndexer.Holders;

namespace MvcIndexer
{
    public class Querier
    {
        private static readonly Querier _Indexer;
        static Querier()
        {
            _Indexer = new Querier();
        }
        private Querier()
        {
        }

        public Querier Query
        {
            get { return _Indexer; }
        }

        internal void SetIndex(IndexCache index)
        {
        }

        public SearchResults Get(String Query)
        {
            ///- stem the words?  driver vs drivers
            ///- weight results based on given keyword weight and also found keyword locality in the pages
            ///- query in quotes should only show results that include both words on the page with locality prioritized
            return null;
        }

        public SearchResults Get(String Query, String Index)
        {
            return null;
        }

    }
}
