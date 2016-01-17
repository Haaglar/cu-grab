﻿using cu_grab.Downloader.RTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Controls;

namespace cu_grab
{
    public class RTE : DownloadAbstract
    {
        private const String EpisodePlaylistUrl = @"https://www.rte.ie/rteavgen/getplaylist/?type=web&format=json&id=";
        private const String CndUrl = @"http://cdn.rasset.ie";

        private ListBox objectList;
        private List<RTEShows> rteShows;
        private List<Episode> selectedShowEpisodes = new List<Episode>();
        public RTE(ListBox oList)
        {
            objectList = oList;
        }
        /// <summary>
        /// Fills the listbox with the JSON from rte search
        /// </summary>
        public override void fillShowsList()
        {
            WebRequest reqSearchJs = HttpWebRequest.Create(@"https://www.rte.ie/player/shows.js?v=2");
            WebResponse resSearchJs = reqSearchJs.GetResponse();

            using (StreamReader srjs = new StreamReader(resSearchJs.GetResponseStream(), System.Text.Encoding.UTF8))
            {
                string jsonjs = srjs.ReadToEnd(); 
                int jslen = jsonjs.Length;
                jsonjs = jsonjs.Substring(12, jslen - 12); // remove the "var shows = "

                JavaScriptSerializer jss = new JavaScriptSerializer();
                rteShows = jss.Deserialize<List<RTEShows>>(jsonjs);
                rteShows = rteShows.OrderBy(x => x.v).ToList(); //Order By name 
            }
            objectList.ItemsSource = rteShows;
            resSearchJs.Close();
        }
        /// <summary>
        /// Sets the ListBox to RTE
        /// </summary>
        public override void setActive()
        {
            objectList.ItemsSource = rteShows;
        }
        public override string clickDisplayedShow()
        {
            //Get page content
            String pageShow;
            WebRequest reqShow = HttpWebRequest.Create("https://www.rte.ie/player/lh/show/" + rteShows[objectList.SelectedIndex].id);
            using (WebResponse resShow = reqShow.GetResponse()) //>using
            {
                using (Stream responseStream = resShow.GetResponseStream())
                {
                    using (StreamReader srShow = new StreamReader(responseStream, System.Text.Encoding.UTF8))
                    {
                        pageShow = srShow.ReadToEnd();
                    }
                }
            }
            //Crop stuff
            int startPoint = pageShow.IndexOf("main-content-box clearfix"); // Start of episode list (two occurances, want first anyway)
            int endPoint = pageShow.IndexOf("main-content-box-container  black");   //recommended rubbish
            pageShow = pageShow.Substring(startPoint, endPoint - startPoint);

            //Regex searches
            Regex regName = new Regex(@"thumbnail-title"">(.*)<");
            Regex regID = new Regex(@"href="".*/([0-9].*)/");

            MatchCollection matchName = regName.Matches(pageShow);
            MatchCollection matchID = regID.Matches(pageShow);

            //Add episodes to list
            int i = 0;
            foreach (Match match in matchName)
            {
                String ID = matchID[i].Groups[1].Value;
                String description = match.Groups[1].Value;
                selectedShowEpisodes.Add(new Episode(description, ID));
                i++;
            }
            String selectedShow = rteShows[objectList.SelectedIndex].v;
            //Clean the name for windows
            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
            {
                selectedShow = selectedShow.Replace(c, '-');
            }
            objectList.ItemsSource = selectedShowEpisodes;
            return selectedShow;
        }
        public override string getUrl()
        {
            //Construct URL
            String urlJson = EpisodePlaylistUrl +  selectedShowEpisodes[objectList.SelectedIndex].EpisodeID;
            String showJsonString;
            WebRequest reqShow = HttpWebRequest.Create(urlJson);
            using (WebResponse resShowUrl = reqShow.GetResponse())
            {
                using (Stream responseStreamUrl = resShowUrl.GetResponseStream())
                {
                    using (StreamReader srShowUrl = new StreamReader(responseStreamUrl, System.Text.Encoding.UTF8))
                    {
                        showJsonString= srShowUrl.ReadToEnd();
                    }
                }
            }
            //Get the hls url
            Regex getHlsUrl = new Regex(@"hls_url""\: ""(.*?)""");
            String urlSuffix = getHlsUrl.Matches(showJsonString)[0].Groups[1].Value;
            String manifestHlsUrl = CndUrl + "/manifest" + urlSuffix;

            return getHighestBitrate(manifestHlsUrl);
            
        }
        private String getHighestBitrate(String url)
        {
            WebRequest reqManifest = HttpWebRequest.Create(url);
            using (WebResponse resManifest = reqManifest.GetResponse())
            {
                using (Stream responseStreamManifest = resManifest.GetResponseStream())
                {
                    using (StreamReader srShowManifest = new StreamReader(responseStreamManifest, System.Text.Encoding.UTF8))
                    {
                        String line; // current line 
                        String finalUrl = "";
                        Regex regexBandwidth = new Regex(@"(?<=\bBANDWIDTH=)([0-9]+)"); //Quality Selection
                        int index = 0;
                        int row = -1;
                        long bandwidth = 0;
                        long tmp = 0;
                        //Get the highest quality link
                        while ((line = srShowManifest.ReadLine()) != null)
                        {
                            if (row == index)
                            {
                                finalUrl = line;
                            }
                            index++;
                            MatchCollection matchBand = regexBandwidth.Matches(line);
                            if (matchBand.Count > 0)
                            {
                                tmp = int.Parse(matchBand[0].Value);
                                if (tmp > bandwidth)
                                {
                                    row = index;
                                    bandwidth = tmp;
                                }
                            }
                        }
                        return CndUrl + finalUrl; // Its a relative adress
                    }
                }
            }
           
        }
        public override void cleanEpisodes()
        {
            selectedShowEpisodes.Clear();
            objectList.ItemsSource = rteShows;
        }
        public override string getSelectedName()
        {
            return selectedShowEpisodes[objectList.SelectedIndex].Name;
        }
        public override string getSubtitles()
        {
            return "";
        }
    }
}
