using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvcIndexer.Holders
{
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
}
