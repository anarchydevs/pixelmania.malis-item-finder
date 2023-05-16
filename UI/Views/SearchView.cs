using AOSharp.Common.GameData;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MalisItemFinder
{
    public class SearchView
    {
        public View Root;
        private TextView _searchView;
        private string _cachedText;
        private ItemScrollListView _itemScrollListView;
       
        public SearchView(View searchRootView, ItemScrollListView itemScrollListView)
        {
            _itemScrollListView = itemScrollListView;

            Root = View.CreateFromXml($"{Main.PluginDir}\\UI\\Views\\SearchView.xml");

            if (Root.FindChild("Search", out _searchView)) { }
            _cachedText = _searchView.Text;
            searchRootView.AddChild(Root, true);
        }

        public List<string> GetKeywords() => _searchView.Text.ToLower().Split(' ').ToList();

        public bool OnTextUpdate(out List<string> keywords)
        {
            keywords = null;
            var text = _searchView.Text;

            if (_cachedText == text)
                return false;

            keywords = GetKeywords();

            _itemScrollListView.RefreshEntryColors();
            _cachedText = text;

            return true;
        }
    }
}