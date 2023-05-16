using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using EFDataAccessLibrary.Models;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using System.Diagnostics;
using SmokeLounge.AOtomation.Messaging.GameData;
using System.Runtime.InteropServices;

namespace MalisItemFinder
{
    public class Main : AOPluginEntry
    {
        public static string PluginDir;
        public static MainWindow Window;
        internal static InventoryManager InventoryManager;
        internal static ItemFinder ItemFinder;
        internal static ItemScanner ItemScanner;
        public Backpack bag;

        public override void Run(string pluginDir)
        {
            Chat.WriteLine("- Mali's Item Finder -", ChatColor.Gold);

            PluginDir = pluginDir;

            AppDomain.CurrentDomain.AssemblyResolve += AssemblyHelper.ResolveAssemblyOnCurrentDomain;

            Window = new MainWindow("MalisItemFinder", $"{PluginDir}\\UI\\Windows\\MainWindow.xml");
            Window.Show();

            ItemScanner = new ItemScanner();
            InventoryManager = new InventoryManager();


            Game.OnUpdate += Window.OnUpdate;

            Chat.RegisterCommand("loadinv", (string command, string[] param, ChatWindow chatWindow) =>
            {
               InventoryManager.RegisterInventoryAsync(false);
            });

            Chat.RegisterCommand("regbank", (string command, string[] param, ChatWindow chatWindow) =>
            {
                // InventoryManager.RegisterBank();
            });

            Chat.RegisterCommand("moveitems", (string command, string[] param, ChatWindow chatWindow) =>
            {
                foreach (Item item in Inventory.Items)
                    item.MoveToContainer(new Identity(IdentityType.Bank, DynelManager.LocalPlayer.Identity.Instance));
                //Inventory.Find(287437, out Item bpItem1);
                //Inventory.Find(287438, out Item bpItem2);

                //bag = Inventory.Backpacks.FirstOrDefault(x => x.Identity == bpItem1.UniqueIdentity);
                //Backpack targetBag = Inventory.Backpacks.FirstOrDefault(x => x.Identity == bpItem2.UniqueIdentity);

                //if (bag == null)
                //{
                //    return;
                //}
                //if (targetBag == null)
                //{
                //    return;
                //}

                //if (bag.Items.Count == 0)
                //{
                //    foreach (var item in targetBag.Items)
                //        item.MoveToContainer(bag);
                //}
                //else
                //{
                //    foreach (var item in bag.Items)
                //        item.MoveToContainer(targetBag);
                //}
            });
        }

        public override void Teardown()
        {
            Window.Dispose();
        }
    }
}