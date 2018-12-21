using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchHighlights.Common
{
    public class League : ConfigurationSection
    {
        [ConfigurationProperty("Name", IsKey=true, IsRequired=true)]
        public string Name { get; set; }

        [ConfigurationProperty("Url", IsRequired = true)]
        public string Url { get; set; }
    }
}
