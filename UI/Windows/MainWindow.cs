using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.Misc;
using AOSharp.Core.UI;
using EFDataAccessLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MalisItemFinder
{
    public class MainWindow : AOSharpWindow
    {
        private ItemScrollListView _itemScrollList;
        private SearchView _searchView;
        private HeaderView _headerView;

        public MainWindow(string name, string path, WindowStyle windowStyle = WindowStyle.Popup, WindowFlags flags = WindowFlags.AutoScale | WindowFlags.NoFade) : base(name, path, windowStyle, flags)
        {
            Utils.LoadCustomTextures($"{Main.PluginDir}\\UI\\Textures\\", 1525831);
        }

        protected override void OnWindowCreating()
        {
            try
            {
                if (Window.FindView("Background", out BitmapView background))
                    background.SetBitmap(TextureId.MainBackground);

                if (Window.FindView("HeaderRoot", out View headerRoot))
                    _headerView = new HeaderView(headerRoot);

                if (Window.FindView("ScrollListRoot", out View scrollListRoot))
                    _itemScrollList = new ItemScrollListView(scrollListRoot);

                if (Window.FindView("SearchRoot", out View searchRoot))
                    _searchView = new SearchView(searchRoot, _itemScrollList);

                if (Window.FindView("Scan", out Button scan))
                {
                    scan.Clicked = ScanClick;
                    scan.SetAllGfx(TextureId.ScanButton);
                }

                if (Window.FindView("Search", out Button search))
                {
                    search.Clicked = SearchClick;
                    search.SetAllGfx(TextureId.SearchButton);
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine(ex.Message);
            }
        }

        private void SearchClick(object sender, ButtonBase e)
        {
            Chat.WriteLine("Search clicked!");

            if (_itemScrollList.SelectedItem == null)
            {
                Chat.WriteLine("You must select a valid item first!");
                return;
            }

            Main.ItemFinder = new ItemFinder(_itemScrollList.SelectedItem);
            _itemScrollList.RefreshEntryColors();
        }

        private void ScanClick(object sender, ButtonBase e)
        {
            Main.ItemScanner.Scan();
        }

        public void OnUpdate(object sender, float e)
        {
            SearchViewUpdate();
            HeaderViewUpdate();
            _itemScrollList.OnUpdate(sender, e);
        }

        private void HeaderViewUpdate()
        {
            if (!Main.InventoryManager.SearchInProgress)
                return;

            if (!_headerView.FilterModeUpdate())
                return;

            _itemScrollList.Refresh(Main.InventoryManager.SearchResults, _headerView.Current);
        }

        private void SearchViewUpdate()
        {
            if (!_searchView.OnTextUpdate(out List<string> keywords))
                return;

            _itemScrollList.QueueSearch(keywords);
        }


        public HeaderButton GetCurrentHeader() => _headerView.Current;

        public void Dispose()
        {
        }

        internal static class TextureId
        {
            internal const int MainBackground = 1525831;
            internal const int ItemEntryBackground = 1525832;
            internal const int ScanButton = 1525833;
            internal const int SearchButton = 1525834;
        }

        internal static class Colors
        {
            internal const int DarkGrey = 0x2A2A2A;
            internal const int LightGrey = 0x2E2E2E;
            internal const int Green = 0x537747;
        }
    }
}