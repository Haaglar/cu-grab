﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Web;

namespace cu_grab
{
    class TV3Cat : DownloadAbstract
    {
        private List<ShowsGeneric> showList = new List<ShowsGeneric>();
        private List<EpisodesGeneric> episodeList = new List<EpisodesGeneric>();

        public TV3Cat (ListBox lBoxContent) : base(lBoxContent) { }

        /// <summary>
        /// Fills the listbox with the content on the programes list
        /// </summary>
        public override void FillShowsList()
        {
            String websiteShowList;
            using (WebClient webClient = new WebClient())
            {
                webClient.Encoding = Encoding.UTF8; //Cause its system ansi by defualt and that screws up the text
                websiteShowList = webClient.DownloadString("http://www.ccma.cat/tv3/programes/");
            }
            Regex getContents = new Regex(@"a href=""(.*?)"">(.*?)<", RegexOptions.Singleline);
            //Cut the string from where the show list starts and ends
            //So we can abuse regex
            int beginIndex = websiteShowList.IndexOf(@"div class=""span9""");
            int endIndex = websiteShowList.IndexOf(@"project_id: modul-programesaz") - beginIndex;
            String cut = websiteShowList.Substring(beginIndex, endIndex); 
            MatchCollection entries = getContents.Matches(cut);
            foreach(Match entry in entries)
            {
                showList.Add(new ShowsGeneric(entry.Groups[2].Value.Trim(), entry.Groups[1].Value));
            }
            listBoxContent.ItemsSource = showList;
        }

        public override string ClickDisplayedShow()
        {
            String urlSelectedTmp = showList[listBoxContent.SelectedIndex].url;
            String showEpisodeList;
            //Its a relative url
            if(urlSelectedTmp.StartsWith("/tv3/"))
            {
                String urlFull = @"http://www.ccma.cat" + urlSelectedTmp;
                using (WebClient webClient = new WebClient())
                {
                    webClient.Encoding = Encoding.UTF8; //Cause its system ansi by defualt and that screws up the text
                    showEpisodeList = webClient.DownloadString(urlFull);
                }
                //:) so wrong
                Regex episodeSearch = new Regex(@"<div class=""F-itemContenidorIntern C-destacatVideo"">.*?<a title=""(.*?)"" href=""(.*?)""", RegexOptions.Singleline);
                //Cut it so we got episodes segment only
               // showEpisodeList = showEpisodeList.Substring(showEpisodeList.LastIndexOf("sliderContenidorIntern"));
                MatchCollection episodes = episodeSearch.Matches(showEpisodeList);
                foreach (Match entry in episodes)
                {                                       //Decoding cause of &#039; need to be '
                    episodeList.Add(new EpisodesGeneric(WebUtility.HtmlDecode(entry.Groups[1].Value), entry.Groups[2].Value));
                }
                listBoxContent.ItemsSource = episodeList;
                
            }
            return "";
        }
        public override string GetUrl()
        {
            throw new NotImplementedException();
        }
        public override string GetSelectedName()
        {
            throw new NotImplementedException();
        }
        public override void CleanEpisodes()
        {
            episodeList.Clear();
            SetActive();
        }
        public override void SetActive()
        {
            listBoxContent.ItemsSource = showList;
        }
        public override string GetSubtitles()
        {
            return "";
        }
    }
}
