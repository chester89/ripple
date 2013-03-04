using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ripple
{
    public static class XElementExtensions
    {
        public static IEnumerable<XElement> Descendants(this XElement element, Func<XElement, bool> filter)
        {
            return element.Descendants().Where(filter);
        }

        public static IEnumerable<XElement> DescendantsWithLocalName(this XElement element, string name)
        {
            return element.Descendants(x => x.Name.LocalName == name);
        }
    }
}
