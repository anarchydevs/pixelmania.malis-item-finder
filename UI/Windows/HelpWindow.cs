using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Common.Helpers;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Common.Unmanaged.Interfaces;
using AOSharp.Core;
using AOSharp.Core.UI;
using SmokeLounge.AOtomation.Messaging.GameData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MalisItemFinder
{
    public class HelpWindow : AOSharpWindow
    {
        public HelpWindow(string name, string path, WindowStyle windowStyle = WindowStyle.Popup, WindowFlags flags = WindowFlags.AutoScale | WindowFlags.NoFade) : base(name, path, windowStyle, flags)
        { 
        }

        protected override void OnWindowCreating()
        {
            if (Window.FindView("Text", out TextView textView))
            {
                textView.Text = $"\n\n" +
                $"- /mif - Toggles UI\n" +
                $"- /mifdelete 'name' - Deletes char from db\n" +
                $"- /miffixroots - Fixes container roots\n" +
                $"- /mifrefresh - Refreshes character list\n" +
                $"- /mifpreview - Toggles icons (performance)\n" +
                $"- /miflimit 'num1 num2' - Total results limit\n" +
                $"  num1 - preview off, num2 - preview on\n\n" +
                $"- Items update automatically\n" +
                $"- Click headers to order by name / loc / etc.\n\n" +
                $"- Search examples:\n" +
                $"    - mig of t rev\n" +
                $"    - | ql:10-15 | id:2000+ | loc:backpack\n" +
                $"    - che imp , ql:99- , id:200+ , loc:bank\n\n" +
                $"- For bugs / glitches / requests:\n " +
                $"  Discord:  Pixelmania#0349\n\n" +
                $"                          ~ Made with AOSharp SDK";

            }

            if (Window.FindView("Logo", out BitmapView _logo))
            {
                _logo.SetBitmap(TextureId.HelpBackground);
            }

            if (Window.FindView("Close", out Button _closeHelp))
            {
                _closeHelp.SetAllGfx(TextureId.CloseButton);
                _closeHelp.Clicked = CloseHelpClick;
            }

            if (Window.FindView("Search", out Button search))
            {
                search.SetAllGfx(TextureId.SearchButton);
            }

            if (Window.FindView("Scan", out Button scan))
            {
                scan.SetAllGfx(TextureId.ScanButton);
            }
        }

        private void CloseHelpClick(object sender, ButtonBase e)
        {
            Midi.Play("Click");
            Main.ToggleHelpWindow();
        }
    }
}