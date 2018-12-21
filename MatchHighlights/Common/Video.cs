using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchHighlights.Common
{
    public class Video
    {
        public int Id { get; set; }
        public List<string> VideoIds { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string DailymotionLink { get; set; }
        public string VideoLocalPath { get; set; }
        public List<string> DownloadLinks { get; set; }

    }
}
