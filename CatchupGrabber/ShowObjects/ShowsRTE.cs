﻿namespace CatchupGrabber.Downloader.RTE
{
    public class ShowsRTE
    {
        public string i { get; set; }//Image id
        public int n { get; set; }// Number of episodes
        public string id { get; set; }// Int show id, 
        public string v { get; set; }   //Name
        public override string ToString()
        {
            return v;
        }
    }
}
