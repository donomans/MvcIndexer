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
            private set { }
        }

        internal void SetIndex(IndexCache index)
        {
        }

        public SearchResults Get(String Query)
        {
            ///How can I search through content of IndexCache quickly?
            return null;
        }

    }
}
