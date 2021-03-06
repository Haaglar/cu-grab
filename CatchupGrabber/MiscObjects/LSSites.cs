﻿using CatchupGrabber.MiscObjects.EnumValues;

namespace CatchupGrabber.MiscObjects.LiveStreamSites
{
    class LSSites
    {
        public string Name { get; set; }
        public string URL { get; set; }
        public bool RequiresInitialRequest { get; set; }
        public DownloadMethod Method { get; set; } 
        public override string ToString()
        {
            return Name;
        }
    }
}
