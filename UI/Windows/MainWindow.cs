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
using System.Threading.Tasks;

namespace MalisItemFinder
{
    public class MainWindow : AOSharpWindow
    {
        internal TitleBarView TitleBarView;
        internal SearchView SearchView;
        internal TableView TableView;
        internal bool SearchInProgress;
        internal List<Slot> SearchResults;
        internal int MaxElements = 50;

        public MainWindow(string name, string path, WindowStyle windowStyle = WindowStyle.Popup, WindowFlags flags = WindowFlags.AutoScale | WindowFlags.NoFade) : base(name, path, windowStyle, flags)
        {
            Utils.LoadCustomTextures($"{Main.PluginDir}\\UI\\Textures\\", 1525831);
            SearchResults = new List<Slot>();
        }

        protected override void OnWindowCreating()
        {
            try
            {
                if (Window.FindView("Background", out BitmapView background))
                    background.SetBitmap(TextureId.MainBackground);

                if (Window.FindView("TitleBarRoot", out View titleBar))
                    TitleBarView = new TitleBarView(titleBar);

                if (Window.FindView("SearchRoot", out View searchRoot))
                    SearchView = new SearchView(searchRoot);

                if (Window.FindView("TableRoot", out View tableRoot))
                    TableView = new TableView(tableRoot);
            }
            catch (Exception ex)
            {
                Chat.WriteLine(ex.Message);
            }
        }

        public void OnUpdate(object sender, float e)
        {
            SearchView.OnUpdate();
            TableView.OnUpdate();

            if (SearchView.OnComboBoxChange())
            {
                Refresh();
            }

            if (SearchView.OnSearchChange() || TableView.Header.OnHeaderChange())
            {
                TableView.ItemScrollList.RefreshEntryColors();
                Refresh();
            }
        }

        internal void Refresh()
        {
            if (SearchInProgress || DatabaseProcessor.IsOccupied())
                return;

            string character = SearchView.GetCharacter();
            IEnumerable<string> searchTerms = SearchView.GetKeywords();
            Dictionary<FilterCriteria, List<SearchCriteria>> criterias = SearchView.GetCriterias();
            int maxElements = MaxElements;
            SearchInProgress = true;

            Task.Run(() =>
            {
                SearchResults = Main.Database.ItemLookup(character, searchTerms, criterias, maxElements);
                SearchInProgress = false;
            });
        }

        public HeaderButton GetCurrentHeader() => TableView.Header.Current;

        public void Dispose()
        {
        }
    }

    internal static class TextureId
    {
        internal const int MainBackground = 1525831;
        internal const int ItemEntryBackground = 1525832;
        internal const int ScanButton = 1525833;
        internal const int SearchButton = 1525834;
        internal const int HelpBackground = 1525835;
        internal const int InfoButton = 1525836;
        internal const int CloseButton = 1525837;
    }

    internal static class Colors
    {
        internal const int DarkGrey = 0x2A2A2A;
        internal const int LightGrey = 0x2E2E2E;
        internal const int Green = 0x537747;
    }
}