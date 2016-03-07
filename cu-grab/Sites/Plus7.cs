﻿using System;
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
    public class Plus7 : DownloadAbstract
    {
        private String tvShowsUrl = @"https://au.tv.yahoo.com/plus7/data/tv-shows/"; //Json object used to provide search suggestions
        private List<ShowsP7> showsP7;
        private List<EpisodesGeneric> selectedShowEpisodes = new List<EpisodesGeneric>();
        private BCoveJson bCoveJson; //Json from the api request 

        //Stuff for downloading
        private String apiUrl = "http://c.brightcove.com/services/json/player/media/?command=find_media_by_reference_id";
        private String publisherIdMain = "2376984108001";
        private String publisherIdAlt = "2376984109001"; //Alternate for extras and TV Snax

        /// <summary>
        /// Standard constructor
        /// </summary>
        /// <param name="lBoxContent">The ListBox in which the content is displayed in</param>
        public Plus7(ListBox lBoxContent) : base(lBoxContent){}
       
        /// <summary>
        /// Fills showsP7 with data taken from the search feature on the P7 website
        /// </summary>
        public override void FillShowsList()
        {
            WebRequest reqSearchJs = HttpWebRequest.Create(tvShowsUrl);
            WebResponse resSearchJs = reqSearchJs.GetResponse();

            using (StreamReader srjs = new StreamReader(resSearchJs.GetResponseStream(), System.Text.Encoding.UTF8))
            {
                string jsonjs = srjs.ReadToEnd();
                JavaScriptSerializer jss = new JavaScriptSerializer();
                showsP7 = jss.Deserialize<List<ShowsP7>>(jsonjs);
                showsP7 = showsP7.OrderBy(x => x.title).ToList(); 
            }
            listBoxContent.ItemsSource = showsP7;
            resSearchJs.Close();
        }
        /// <summary>
        /// Fills selectedShowEpisodes with episdes from the selected show
        /// </summary>
        /// <returns>The name of the selected Show</returns>
        public override String ClickDisplayedShow()
        {
            String pageShow;
            WebRequest reqShow = HttpWebRequest.Create(showsP7[listBoxContent.SelectedIndex].url);
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

            //Honestly I dont want to do this, but I dont want to use external libraries and HTMLDocument doesnt like me so screw it.  
            //string regexHeadline = @"""headline"">([0-9].)"; 
            Regex regexLoadmore = new Regex(@"data-url=""(.*)"" rel=""next""");
            MatchCollection matchLoadMore = regexLoadmore.Matches(pageShow);
            if (matchLoadMore.Count != 0)//If it hasn't loaded all of the episodes make another request
            {
                String updatedUrl = matchLoadMore[0].Groups[1].Value;
                updatedUrl = updatedUrl.Replace("&amp;", @"&");            //Fix ampersands
                updatedUrl = updatedUrl.Replace("2/?pp=10", @"1/?pp=70"); //Get the first 70 results for the show (Make this dynamic)
                updatedUrl = @"http://au.tv.yahoo.com" + updatedUrl + "&bucket=exclusiveBkt"; //Add on additional GET value and prefix
                Uri uri = new Uri(updatedUrl);
                WebRequest reqShowAll = HttpWebRequest.Create(uri);
                WebResponse resShowAll = reqShowAll.GetResponse();
                StreamReader srShowAll = new StreamReader(resShowAll.GetResponseStream(), System.Text.Encoding.UTF8);
                pageShow = srShowAll.ReadToEnd();
                resShowAll.Close();
                srShowAll.Close();
            
            }
            else
            {
                //If we dont need to load, remove the stuff off the side (recommended crap) so its cleaned for regex
                int startPoint = pageShow.IndexOf("class=\"g-col-8 g-xl-30-40 g-l-20-30 g-m-row g-main"); //Main content for shows
                int endPoint = pageShow.IndexOf("class=\"g-col-4 g-xl-10-40 g-l-10-30 g-m-row g-rail");   //Side reccommendations div
                pageShow = pageShow.Substring(startPoint, endPoint - startPoint); //cut excess rubbish
            }

            Regex regexLinks = new Regex(@"href=""//(.*/)"""); 
            Regex regexDesc = new Regex(@"collection-title-link-inner"">.*\n(.*)"); 

            MatchCollection matchDesc = regexDesc.Matches(pageShow);
            MatchCollection matchLinks = regexLinks.Matches(pageShow);
            int i = 0;
            foreach (Match match in matchDesc)
            {
                String url = matchLinks[i].Groups[1].Value;
                String description = match.Groups[1].Value.Trim(); // Trim excess whitespace cause otherwise itll look like rubbish 
                selectedShowEpisodes.Add(new EpisodesGeneric(description, url));
                i += 2;//Skip every second as theres a href on both the image and the content
            }
        
            //Store the current show name for file naming later
            String selectedShow = showsP7[listBoxContent.SelectedIndex].title;
            //Clean the name for windows
            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
            {
                selectedShow = selectedShow.Replace(c, '-');
            }
            //Update list
            listBoxContent.ItemsSource = selectedShowEpisodes;
            return selectedShow;
        }
        /// <summary>
        /// Gets the name of the current selected show
        /// </summary>
        /// <returns>The selected show's name</returns>
        public override String GetSelectedName()
        {
            return selectedShowEpisodes[listBoxContent.SelectedIndex].Name;
        }
        /// <summary>
        /// Grabs the page, does some stuff (important part ported from p7-hls) and gets the URL
        /// </summary>
        /// <returns>The m3u8 url</returns>
        public override DownloadObject GetDownloadObject() 
        {
            //Get episode page data
            String pageContent;
            String url = selectedShowEpisodes[listBoxContent.SelectedIndex].EpisodeID;
            WebRequest reqShow = HttpWebRequest.Create("https://" + url);
            using (WebResponse resShowUrl = reqShow.GetResponse())
            {
                using (Stream responseStreamUrl = resShowUrl.GetResponseStream())
                {
                    using (StreamReader srShowUrl = new StreamReader(responseStreamUrl, System.Text.Encoding.UTF8))
                    {
                        pageContent = srShowUrl.ReadToEnd();
                    }
                }
            }
            //Get Id from Url
            Regex regRefId = new Regex(@"/([0-9]+)/");
            String refID = regRefId.Matches(url)[0].Groups[1].Value;
            // Get playerkey from page
            Regex regPlayerKey = new Regex(@"rKey"" value=""(.*)""");
            String playerKey = regPlayerKey.Matches(pageContent)[0].Groups[1].Value;
            String jsonUrl = apiUrl + "&playerKey=" + playerKey + "&pubId=" + publisherIdMain + "&refId=" + refID;

            //Get and store the json data   
            using(WebClient wc = new WebClient())
            {
                String showsJson = wc.DownloadString(jsonUrl);
                if(showsJson.Equals("null"))//Bad id
                {
                    showsJson = wc.DownloadString(jsonUrl = apiUrl + "&playerKey=" + playerKey + "&pubId=" + publisherIdAlt + "&refId=" + refID);
                }
                JavaScriptSerializer jss = new JavaScriptSerializer();
                bCoveJson = jss.Deserialize<BCoveJson>(showsJson);
            }

            //Get highest quality
            int defaultQual = bCoveJson.FLVFullSize;
            String fullLengthURL = bCoveJson.FLVFullLengthURL;
            int oldSize = 0;
            foreach(IOSRendition redition in bCoveJson.IOSRenditions)
            {
                if(oldSize < redition.size)
                {
                    fullLengthURL = redition.defaultURL;   
                }
            }
            return new DownloadObject(fullLengthURL, GetSubtitles(), Country.Aus, DownloadMethod.HLS);
        }
        /// <summary>
        /// Handles Clearing the episode list and reseting it back to the show list
        /// </summary>
        public override void CleanEpisodes() 
        {
            selectedShowEpisodes.Clear();
            listBoxContent.ItemsSource = showsP7;
        }
        /// <summary>
        /// Sets the ListBox to be the episodelist for P7
        /// </summary>
        public override void SetActive()
        {
            listBoxContent.ItemsSource = showsP7;
        }
        /// <summary>
        /// Gets the URL of the subtitles for the selected episode.
        /// </summary>
        /// <returns>The URL for the captions, returns a blank String if no Subtitles exist </returns>
        public override String GetSubtitles()
        {
            if(bCoveJson.captions != null)
            {
                return bCoveJson.captions[0].URL;
            }
            return "";
        }
    }
}