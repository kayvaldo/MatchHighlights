using HtmlAgilityPack;
using MatchHighlights.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace MatchHighlights
{
    public class Program
    {
        static void Main(string[] args)
        {
            // var x = GetVideoTags()
            //string y = "/soccer/copa-america/group-b/brazil-vs-ecuador/1-2156785/";
            //var t = y.ToLower().Contains("ecuador") && y.ToLower().Contains("brazil");
            var leagues = GetLeaguesFromConfig();
            if (leagues.Any())
            {
                foreach (var league in leagues.Take(1))
                {

                    try
                    {
                        //Get league page
                        Console.WriteLine("League: {0}", league.Name);
                        // var leaguePage = RequestPage(league.Url);

                        //Get videos info
                        // GetVideos(leaguePage);
                        var videos = GetVideos(league);
                        Console.WriteLine("Got {0} videos.", videos.Count);


                        //Request page for each video and get 
                        foreach (var vid in videos.Take(2))
                        {
                            try
                            {
                                Console.WriteLine("Getting info for: {0}", vid.Title);
                                if (!VideoExists(vid))
                                {
                                    //Get video Id
                                    GetVideoId(vid);
                                    GetVideoTags(vid.Title);
                                    continue;

                                    if (vid.VideoIds.Count > 0)
                                    {
                                        foreach (var item in vid.VideoIds)
                                        {
                                            Console.WriteLine("Video Id for {0}: {1}", vid.Title, item);
                                        }
                                    }


                                    //Get download links
                                    GetDownloadLinks(vid);

                                    var localPath = ConfigurationManager.AppSettings["LocalPath"];
                                    var client = new WebClient();

                                    foreach (var item in vid.DownloadLinks)
                                    {
                                        Console.WriteLine("Downloading {0}: {1}", vid.Title, item);
                                        client.DownloadFile(new Uri(item), localPath + vid.Title.PadRight(vid.DownloadLinks.IndexOf(item), ' ') + ".mp4");
                                        Console.WriteLine("Done downloading....");
                                        SaveVideoId(item, vid.Title);
                                    }


                                }
                                else
                                {
                                    Console.WriteLine("File already exists.");
                                }

                            }
                            catch (Exception)
                            {
                                continue;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }


            }
            else
            {
                Console.WriteLine("No leagues found.");
            }

            ExitStrategy();
        }

        private static void GetDownloadLinks(Video vid)
        {
            vid.DownloadLinks = new List<string>();
            foreach (var item in vid.VideoIds)
            {
                var template = string.Format("https://cdn.video.playwire.com/19004/videos/{0}/video-sd.mp4", item.Trim());
                vid.DownloadLinks.Add(template);
            }
        }

        private static bool VideoExists(Video vid)
        {
            try
            {
                var filePath = ConfigurationManager.AppSettings["VideoIds"];
                FileInfo file = new FileInfo(filePath);
                StreamReader reader = new StreamReader(filePath);
                var content = reader.ReadToEnd();
                int index = content.IndexOf(vid.Title);
                reader.Close();
                return index >= 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not save video id. {0}", ex.Message + ex.StackTrace);
                return false;
            }

        }

        private static string RequestPage(string url)
        {
            // Create a request for the league page. 
            WebRequest request = WebRequest.Create(url);
            // If required by the server, set the credentials.
            request.Credentials = CredentialCache.DefaultCredentials;
            // Get the response.
            WebResponse response = request.GetResponse();
            // Display the status.
            Console.WriteLine(((HttpWebResponse)response).StatusDescription);
            // Get the stream containing content returned by the server.
            Stream dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd();
            // Display the content.
            //Console.WriteLine(responseFromServer);
            // Clean up the streams and the response.
            reader.Close();
            response.Close();
            dataStream.Close();
            return responseFromServer;
        }

        private static void ExitStrategy()
        {
            var command = Console.ReadLine();
            if (command.Trim().ToLower().Equals("exit"))
            {
                Environment.Exit(0);
            }
            else
            {
                ExitStrategy();
            }
        }

        private static string GetVideoId(string html)
        {
            var delimiter = "vid-id=";
            var firstIndex = html.IndexOf(delimiter);
            var secondIndex = html.IndexOf('"', firstIndex + delimiter.Length + 1);
            var diffIndex = secondIndex - firstIndex - delimiter.Length - 1;
            var id = html.Substring(firstIndex + delimiter.Length + 1, diffIndex);
            return id;
        }

        private static void GetVideoId(Video vid)
        {
            var webGet = new HtmlWeb();
            var document = webGet.Load(vid.Url);
            string preId = ConfigurationManager.AppSettings["PreId"];
            string postId = ConfigurationManager.AppSettings["PostId"];
            string timeDelimiter = ConfigurationManager.AppSettings["TimeDelimiter"];
            vid.VideoIds = new List<string>();

            var links = document.DocumentNode.Descendants().Where(lnks => lnks.Attributes["data-config"] != null);

            foreach (var item in links)
            {
                var id = item.Attributes["data-config"].Value.Replace(preId, string.Empty).Replace(postId, string.Empty);
                vid.VideoIds.Add(id);
            }

            //add match date to title (only if its a match)
            if (vid.Title.Contains(" vs "))
            {
                var time = document.DocumentNode.Descendants().Where(lnks => lnks.Name.Equals("time")).FirstOrDefault();
                if (time != null && !string.IsNullOrEmpty(time.InnerText))
                {
                    vid.Title += " " + timeDelimiter + " " + time.InnerText;
                }
            }

        }

        public static List<string> GetVideoTags(string videoTitle)
        {
            List<string> tags = new List<string>();
            char timeDelimiter = ConfigurationManager.AppSettings["TimeDelimiter"].ToCharArray().ElementAt(0);
            var name = videoTitle.Split(timeDelimiter)[0].Trim();
            var date = videoTitle.Split(timeDelimiter)[1].Trim();
            var year = date.Split(',')[1].Trim();
            int yearNumber = Convert.ToInt32(year.Trim());
            var monthString = date.Split(',')[0].Split(' ')[0];
            var day = date.Split(',')[0].Split(' ')[1];
            int dayNumber = Convert.ToInt32(day.Trim());
            var monthNumber = GetMonthNumber(monthString);
            var matchDate = new DateTime(yearNumber, monthNumber, dayNumber);
            var match = name.Replace(" vs ", " : ");
            var hometeam = match.Split(':')[0].ToLower().Trim();
            var awayteam = match.Split(':')[1].ToLower().Trim();
            var scorersList = new List<string>();
            scorersList = GetScorers(name, matchDate);
            var matchScore = string.Empty;
            var scores = scorersList.Where(x => x.Contains("scores:"));

            if (scores != null && scores.Count() > 0)
            {
                matchScore = scores.FirstOrDefault().Split(':')[1];
                name += matchScore;
            }

            //arsenal vs chelsea 
            tags.Add(name);

            //arsenal vs chelsea home
            tags.Add(string.Format("{0} {1} home", name, matchScore));

            //arsenal vs chelsea away
            tags.Add(string.Format("{0} {1} away", name, matchScore));

            //arsenal vs chelsea June 2, 2016
            tags.Add(string.Format("{0} vs {1} {5} {2} {3}, {4}", hometeam, awayteam, GetMonthName(monthNumber), day, year, matchScore));

            //chelsea vs arsenal June 2, 2016
            tags.Add(string.Format("{0} vs {1} {5} {2} {3}, {4}", awayteam, hometeam, GetMonthName(monthNumber), day, year, matchScore));

            //arsenal vs chelsea 06-02-2016
            tags.Add(string.Format("{0} vs {1} {5} {2}-{3}-{4}", hometeam, awayteam, monthNumber, day, year, matchScore));

            //chelsea vs arsenal 06-02-2016
            tags.Add(string.Format("{0} vs {1} {5} {2}-{3}-{4}", awayteam, hometeam, monthNumber, day, year, matchScore));


            foreach (var item in scorersList)
            {
                if (item.Contains("scores:"))
                {
                    continue;
                }
                //van persie 61'
                if (item.Trim().EndsWith("'"))
                {
                    tags.Add(item);
                    continue;
                }
                //van persie goal arsenal vs chelsea 2016
                tags.Add(string.Format("{0} goal {1} {2}", item, name, year));
            }

            return tags;


        }

        private static List<string> GetScorers(string matchName, DateTime matchDate)
        {
            var scorersList = new List<string>();


            //Get livescores.com match page : http://www.livescores.com/soccer/2016-06-02/
            var baseUrl = @"http://www.livescores.com";


            var matchLink = GetMatchLink(matchName, matchDate);

            if (matchLink == null)
            {
                matchLink = GetMatchLink(matchName, matchDate.AddDays(1));
            }

            if (matchLink != null)
            {
                var matchUrl = string.Format("{0}{1}", baseUrl, matchLink.Attributes["href"].Value);
                var webGet = new HtmlWeb();
                var document = webGet.Load(matchUrl);

                #region MyRegion
                //var collection = document.DocumentNode.Descendants().Where(lnks => lnks.Name.Equals("span") &&
                //                                                          lnks.Attributes["class"].Value.Equals("name") &&
                //                                                          !string.IsNullOrEmpty(lnks.InnerText));
                //Console.Clear();
                //Console.WriteLine(matchUrl);
                //foreach (var item in collection)
                //{
                //    Console.WriteLine(item.InnerText);
                //}

                //var linksOnPage = from lnks in document.DocumentNode.Descendants()
                //                  where lnks.Name == "span"
                //                  &&
                //                       lnks.Attributes["class"] != null
                //                       && !string.IsNullOrEmpty(lnks.Attributes["class"].Value)
                //                       && lnks.Attributes["class"].Value.Equals("name") 
                //                  select lnks;
                #endregion

                var matchScore = document.DocumentNode.Descendants().Where(lnks => lnks.Name.Equals("div") &&
                                                                          lnks.Attributes["class"] != null && lnks.Attributes["class"].Value.Equals("sco") &&
                                                                          !string.IsNullOrEmpty(lnks.InnerText)).FirstOrDefault();
                if (matchScore != null)
                {
                    scorersList.Add(string.Format("scores: {0}", matchScore.InnerText));
                }

                var scorers = document.DocumentNode.Descendants().Where(lnks => lnks.Name.Equals("span") &&
                                                                          lnks.Attributes["class"] != null && lnks.Attributes["class"].Value.Equals("name") &&
                                                                          !string.IsNullOrEmpty(lnks.InnerText));
                foreach (var item in scorers)
                {
                    var parent = item.ParentNode;
                    var grandParent = parent.ParentNode;
                    var greatGrandParent = grandParent.ParentNode;

                    //check for goal
                    var goalNodes = parent.ChildNodes.Where(x => x.Name.Equals("span") && x.Attributes["class"] != null && x.Attributes["class"].Value.Equals("inc goal"));
                    if (goalNodes != null && goalNodes.Count() > 0)
                    {
                        var goalNode = goalNodes.FirstOrDefault();
                        scorersList.Add(item.InnerText);

                        //get goal time
                        var timeNodes = greatGrandParent.ChildNodes.Where(y => y.Attributes["class"] != null && y.Attributes["class"].Value.Equals("min"));
                        if (timeNodes != null && timeNodes.Count() > 0)
                        {
                            var timeNode = timeNodes.FirstOrDefault();
                            scorersList.Add(string.Format("{0} {1}", item.InnerText, timeNode.InnerText));
                        }


                    }
                }
            }

            return scorersList;
        }

        private static HtmlNode GetMatchLink(string matchName, DateTime matchDate)
        {
            HtmlNode matchLink = null;

            try
            {
                var match = matchName.Replace(" vs ", " : ");
                var hometeam = match.Split(':')[0].ToLower().Trim();
                var awayteam = match.Split(':')[1].ToLower().Trim();
                var baseUrl = @"http://www.livescores.com/soccer";
                var livescoresUrl = string.Format("{3}/{0}-{1}-{2}/", matchDate.Year, matchDate.Month, matchDate.Day, baseUrl);
                var webGet = new HtmlWeb();
                var document = webGet.Load(livescoresUrl);

                #region MyRegion
                //var collection = document.DocumentNode.Descendants().Where(lnks => lnks.Attributes["href"] != null && lnks.Attributes["href"].Value.Contains("-vs-"));
                //Console.Clear();
                //Console.WriteLine(livescoresUrl);
                //foreach (var item in collection)
                //{
                //    Console.WriteLine(item.Attributes["href"].Value + ":" + (item.Attributes["href"].Value.Contains(hometeam) && item.Attributes["href"].Value.Contains(awayteam)));
                //}

                //var linksOnPage = from lnks in document.DocumentNode.Descendants()
                //                  where lnks.Name == "a"
                //                  &&
                //                       lnks.Attributes["href"] != null &&
                //                       lnks.Attributes["href"].Value.Contains(hometeam) &&
                //                       lnks.Attributes["href"].Value.Contains(awayteam)
                //                  select lnks;
                #endregion


                var links = document.DocumentNode.Descendants().Where(lnks => lnks.Name.Equals("a") && lnks.Attributes["href"].Value.Contains(hometeam)
                                                                           && lnks.Attributes["href"].Value.Contains(awayteam));

                if (links != null && links.Count() > 0)
                {
                    matchLink = links.FirstOrDefault();
                }

            }
            catch (Exception)
            {

            }
            return matchLink;
        }

        public static string GetMonthName(int monthNumber)
        {
            if (monthNumber > 12)
            {
                return string.Empty;
            }

            List<string> months = new List<string>();
            months.Add("");
            months.Add("january");
            months.Add("february");
            months.Add("march");
            months.Add("april");
            months.Add("may");
            months.Add("june");
            months.Add("july");
            months.Add("august");
            months.Add("september");
            months.Add("october");
            months.Add("november");
            months.Add("december");
            var month = months.ElementAt(monthNumber);

            return month;
        }

        public static List<string> GetScorers(string matchName, string day, string monthNumber, string year)
        {
            var scorersList = new List<string>();
            var match = matchName.Replace(" vs ", " : ");
            var hometeam = match.Split(':')[0].ToLower().Trim();
            var awayteam = match.Split(':')[1].ToLower().Trim();

            //Get livescores.com match page : http://www.livescores.com/soccer/2016-06-02/
            var baseUrl = @"http://www.livescores.com/soccer";
            var livescoresUrl = string.Format("{3}/{0}-{1}-{2}/", year, monthNumber, day, baseUrl);
            var webGet = new HtmlWeb();
            var document = webGet.Load(livescoresUrl);
            //var collection = document.DocumentNode.Descendants().Where(lnks => lnks.Attributes["href"] != null);
            //foreach (var item in collection)
            //{
            //    Console.WriteLine(item.Attributes["href"].Value);
            //}
            var links = document.DocumentNode.Descendants().Where(lnks => lnks.Attributes["href"].Value.ToLower().Contains(hometeam) &&
                                                                  lnks.Attributes["href"].Value.ToLower().Replace('-', ' ').Contains(awayteam));
            if (links.Count() < 1)
            {
                //check next day
                //int nextday = Convert.ToInt32(day) + 1;
                //var baseUrl = @"http://www.livescores.com/soccer";
                //var livescoresUrl = string.Format("{3}/{0}-{1}-{2}/", year, monthNumber, day, baseUrl);
                //var webGet = new HtmlWeb();
                //var document = webGet.Load(livescoresUrl);
            }

            //if (links != null)
            //{
            //    var matchUrl = string.Format("{0}{1}", baseUrl, links.Attributes["href"].Value);
            //    var document2 = webGet.Load(matchUrl);
            //    var scorers = document.DocumentNode.Descendants().Where(lnks => lnks.Name.Equals("span") &&
            //                                                              lnks.Attributes["class"].Value.Equals("name") &&
            //                                                              !string.IsNullOrEmpty( lnks.InnerText)) ;
            //    foreach (var item in scorers)
            //    {
            //        scorersList.Add(item.InnerText); 
            //    }
            //}

            return scorersList;
        }

        private static int GetMonthNumber(string monthString)
        {
            List<string> months = new List<string>();
            months.Add("");
            months.Add("january");
            months.Add("february");
            months.Add("march");
            months.Add("april");
            months.Add("may");
            months.Add("june");
            months.Add("july");
            months.Add("august");
            months.Add("september");
            months.Add("october");
            months.Add("november");
            months.Add("december");
            var index = months.IndexOf(monthString.ToLower());
            return index;
        }

        private static List<Video> GetVideos(League league)
        {
            var webGet = new HtmlWeb();
            var document = webGet.Load(league.Url);
            List<Video> videos = new List<Video>();

            //var linksThatDoNotOpenInNewWindow = document.DocumentNode.SelectNodes("//div[@]");
            var links = document.DocumentNode.Descendants().Where(lnks => lnks.Attributes["href"] != null &&
                //lnks.InnerText.Trim().Length > 0 );
                                   lnks.Attributes["href"].Value.Contains("http://www.matchhighlight.com/latest-goals/") &&
                                   lnks.Attributes["title"] != null);

            foreach (var lnks in links)
            {
                var vid = new Video();
                //vid.VideoId = lnks.Attributes["data-id"].Value;
                vid.Url = lnks.Attributes["href"].Value;
                vid.Title = lnks.Attributes["title"].Value;
                videos.Add(vid);
            }

            #region LinqText
            //var linksOnPage = from lnks in document.DocumentNode.Descendants()
            //                  where lnks.Name == "a"
            //                  &&
            //                       lnks.Attributes["href"] != null &&
            //                       lnks.InnerText.Trim().Length > 0 &&
            //                       lnks.Attributes["href"].Value.Contains("http://www.footballtube.com/videos") &&
            //                       !lnks.Attributes["href"].Value.Equals("http://www.footballtube.com/videos") &&
            //                       !lnks.Attributes["href"].Value.Contains("http://www.footballtube.com/videos/category")
            //                  select new
            //                  {
            //                      id = lnks.Attributes["data-id"],
            //                      Url = lnks.Attributes["href"].Value,
            //                      Text = lnks.InnerText
            //                  };
            #endregion

            return videos;

        }

        private static List<League> GetLeaguesFromConfig()
        {
            NameValueCollection settings = ConfigurationManager.GetSection("LeagueGroup/Leagues") as NameValueCollection;
            List<League> leagues = new List<League>();
            if (settings != null & settings.Count > 0)
            {
                foreach (var key in settings.AllKeys)
                {
                    var league = new League() { Name = key, Url = settings[key] };
                    leagues.Add(league);
                }
            }

            return leagues;
        }

        private static string GetDailymotionLink(string html)
        {
            var firstIndex = html.IndexOf("https://www.dailymotion.com/embed/video");
            var secondIndex = html.IndexOf("https://www.dailymotion.com/embed/video", firstIndex + 1);
            var endIndex = html.IndexOf('"', secondIndex);
            var diffIndex = endIndex - secondIndex;
            var link = html.Substring(secondIndex, diffIndex);
            //Remove the embed "/embed"
            var dailymotionLink = link.Remove(27, 6);
            return dailymotionLink;
        }

        private static Dictionary<string, string> GetDownloadLinks(string html)
        {
            var delimeter = "\"qualities\":";
            var delimeter2 = "\"reporting\"";
            var firstIndex = html.IndexOf(delimeter);
            var secondIndex = html.IndexOf(delimeter2);
            Dictionary<string, string> videoInfo = new Dictionary<string, string>();

            var qualities = GetVideoQualities();

            foreach (var quality in qualities)
            {
                var url = GetDownloadLinkForSpecifiedQuality(html, quality);
                if (!string.IsNullOrEmpty(url))
                {
                    videoInfo.Add(quality.Key, url);
                }
            }

            return videoInfo;
        }

        private static Dictionary<string, string> GetVideoQualities()
        {
            Dictionary<string, string> qualities = new Dictionary<string, string>();
            qualities.Add("240", "\"240\":[{");
            qualities.Add("380", "\"380\":[{");
            qualities.Add("480", "\"480\":[{");
            qualities.Add("720", "\"720\":[{");
            qualities.Add("1280", "\"1280\":[{");

            return qualities;
        }

        private static string GetDownloadLinkForSpecifiedQuality(string html, KeyValuePair<string, string> quality)
        {
            var delimeter = "\"}]";
            var firstIndex = html.IndexOf(quality.Value);
            var url = string.Empty;
            if (firstIndex >= 0)
            {
                var secondIndex = html.IndexOf(delimeter, firstIndex);
                if (secondIndex > firstIndex)
                {
                    var diffIndex = secondIndex - firstIndex - delimeter.Length - delimeter.Length - 3;
                    var link = html.Substring(firstIndex + quality.Value.Length + 1, diffIndex);
                    var cleanedLink = link.Replace("\\/", @"/");
                    url = @"https://" + cleanedLink.Substring(cleanedLink.IndexOf("www"));
                }
                else
                {
                    return string.Empty;
                }

            }

            return url;
        }


        //private static void SaveVideoId(Video vid)
        //{
        //    try
        //    {
        //        var filePath = ConfigurationManager.AppSettings["VideoIds"];
        //        FileInfo file = new FileInfo(filePath);
        //        StreamWriter writer = new StreamWriter(filePath, true);
        //        writer.WriteLine("{0} {1}", vid.VideoId, vid.Title);
        //        writer.Flush(); writer.Close();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Could not save video id. {0}", ex.Message + ex.StackTrace);
        //    }

        //}

        private static void SaveVideoId(string link, string title)
        {
            try
            {
                var filePath = ConfigurationManager.AppSettings["VideoIds"];
                FileInfo file = new FileInfo(filePath);
                StreamWriter writer = new StreamWriter(filePath, true);
                writer.WriteLine("{0} {1}", link, title);
                writer.Flush(); writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not save video id. {0}", ex.Message + ex.StackTrace);
            }

        }
    }
}
