using AOSharp.Common.GameData;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MalisItemFinder
{
    public class TableView
    {
        private View Root;
        public ItemScrollListView ItemScrollList;
        public HeaderView Header;

        public TableView(View tableViewRoot)
        {
            Root = View.CreateFromXml($"{Main.PluginDir}\\UI\\Views\\TableView.xml");

            if (Root.FindChild("HeaderRoot", out View headerRoot))
                Header = new HeaderView(headerRoot);

            if (Root.FindChild("ScrollListRoot", out View scrollListRoot))
                ItemScrollList = new ItemScrollListView(scrollListRoot);

            tableViewRoot.AddChild(Root, true);
        }

        public void OnUpdate()
        {
            ItemScrollList.OnUpdate();

            if (Header.TriggerRefresh())
                ItemScrollList.Refresh();
        }


        public void Dispose()
        {
            ItemScrollList.Dispose();
        }
    }
}