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
using AOSharp.Core.GMI;

namespace MalisItemFinder
{
    public class Main : AOPluginEntry
    {
        internal static string PluginDir;
        internal static Settings Settings;
        internal static MainWindow MainWindow;
        internal static HelpWindow HelpWindow;
        internal static Database Database;
        internal static ItemFinder ItemFinder;
        internal static ItemScanner ItemScanner;

        public override void Run(string pluginDir)
        {
            Chat.WriteLine("- Mali's Item Finder -", ChatColor.Gold);

            PluginDir = pluginDir;

            Settings = new Settings($"{PluginDir}\\JSON\\Settings.json");
            Settings.Load();
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyHelper.ResolveAssemblyOnCurrentDomain;

            ToggleMainWindow();

            if (Settings.ShowTutorial)
            {
                ToggleHelpWindow();
                Settings.ShowTutorial = false;
                Settings.Save();
            }

            ItemScanner = new ItemScanner();
            Database = new Database();

            Chat.RegisterCommand("mif", (string command, string[] param, ChatWindow chatWindow) =>
            {
                ToggleMainWindow();
            });     
            
            Chat.RegisterCommand("mifrefresh", (string command, string[] param, ChatWindow chatWindow) =>
            {
                MainWindow.SearchView.RefreshComboBox();
                Chat.WriteLine("Refreshed character list.");
            });

            Chat.RegisterCommand("mifpreview", (string command, string[] param, ChatWindow chatWindow) =>
            {
                Settings.ItemPreview = !Settings.ItemPreview;
                Settings.Save();
                MainWindow.RefreshMaxElements();
                MainWindow.TableView.RefreshItemPreviews();
                Chat.WriteLine($"Item preview set to: {Settings.ItemPreview}, total result limit changed to: {MainWindow.MaxElements}");
            });

            Chat.RegisterCommand("mifdelete", (string command, string[] param, ChatWindow chatWindow) =>
            {
                if (param.Length == 1 && Database.TryDeleteInventory(param[0]))
                {
                    Chat.WriteLine($"Character Inventory {param[0]} deleted.");
                    MainWindow.Refresh();
                }
                MainWindow.SearchView.RefreshComboBox();
            });

            Chat.RegisterCommand("miffixroots", (string command, string[] param, ChatWindow chatWindow) =>
            {
                Database.FixBackpackRoots(ContainerId.Inventory);

                if (Inventory.Bank.IsOpen)
                    Database.FixBackpackRoots(ContainerId.Bank);

                Chat.WriteLine("Fixing container roots.");
            });

            Chat.RegisterCommand("miflimit", (string command, string[] param, ChatWindow chatWindow) =>
            {
                int lowNum = 0;
                int highNum = 0;

                if (param.Length != 2 || !int.TryParse(param[0], out lowNum) || !int.TryParse(param[1], out highNum))
                {
                    Chat.WriteLine($"Invalid command usage, try /miflimit 50 100");
                    return;
                }

                Settings.PreviewOnMaxElements = lowNum;
                Settings.PreviewOffMaxElements = highNum;
                Settings.Save();

                MainWindow.RefreshMaxElements();
                MainWindow.TableView.ItemScrollList.RecacheItemViews();
                MainWindow.Refresh();

                Chat.WriteLine($"Results limit set to {param[0]} (with preview on) and {param[1]} (with preview off).");
            });
        }

        internal static void ToggleMainWindow()
        {
            try
            {
                if (MainWindow != null)
                {
                    Game.OnUpdate -= MainWindow.OnUpdate;
                    MainWindow.Window.Close();
                    MainWindow = null;
                    Midi.Play("End");
                }
                else
                {
                    MainWindow = new MainWindow("MalisItemFinder", $"{PluginDir}\\UI\\Windows\\MainWindow.xml");
                    MainWindow.Show();
                    Game.OnUpdate += MainWindow.OnUpdate;
                    Midi.Play("Start");
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine(ex.Message);
            }
        }

        internal static void ToggleHelpWindow()
        {
            if (HelpWindow != null)
            {
                HelpWindow.Window.Close();
                HelpWindow = null;
            }
            else
            {
                HelpWindow = new HelpWindow("MalisItemFinderHelp", $"{PluginDir}\\UI\\Windows\\HelpWindow.xml");
                HelpWindow.Show();
            }
        }

        public override void Teardown()
        {
            Midi.TearDown();
            MainWindow.Dispose();
        }
    }
}