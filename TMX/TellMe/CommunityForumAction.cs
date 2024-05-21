﻿using System.Drawing;

namespace Sdl.Community.FileType.TMX.TellMe
{
    class CommunityForumAction : TellMeAction
    {
        private static readonly string[] _helpKeywords = {  "support", "documentation" };
        private static readonly string _actionName = Constants.TellMe_Forum_Name;
        private static readonly string _url = Constants.TellMe_Forum_Url;
        private static readonly Icon _icon = PluginResources.TellMe_Forum;
        private static readonly bool _isAvailable = true;

        public CommunityForumAction() : base(_actionName, _icon, _helpKeywords, _isAvailable, url: _url) { }
    }
}