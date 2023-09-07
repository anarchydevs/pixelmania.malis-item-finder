using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using EFDataAccessLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MalisItemFinder
{
    public class TitleBarView
    {
        public View Root;

        public TitleBarView(View searchRootView)
        {
            Root = View.CreateFromXml($"{Main.PluginDir}\\UI\\Views\\TitleBarView.xml");

            if (Root.FindChild("Title", out TextView textView))
            {
                textView.Text = "- Mali's Item Finder -";
            }

            if (Root.FindChild("Close", out Button closeButton))
            {
                closeButton.Clicked = CloseClick;
                closeButton.SetAllGfx(TextureId.CloseButton);
            }

            if (Root.FindChild("Info", out Button infoButton))
            {
                infoButton.Clicked = InfoClick;
                infoButton.SetAllGfx(TextureId.InfoButton);
            }

            searchRootView.AddChild(Root, true);
        }

        private void InfoClick(object sender, ButtonBase e)
        {
            Main.ToggleHelpWindow();
            Midi.Play("Click");
        }

        private void CloseClick(object sender, ButtonBase e)
        {
            Main.ToggleMainWindow();
            Midi.Play("Click");
        }
    }
}