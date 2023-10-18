using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Common.Unmanaged.Interfaces;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using EFDataAccessLibrary.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MalisItemFinder
{
    public class Utils
    {
        public static void LoadCustomTextures(string path, int startId)
        {
            try
            {
                DirectoryInfo textureDir = new DirectoryInfo(path);

                foreach (var file in textureDir.GetFiles("*.png").OrderBy(x => x.Name))
                    GuiResourceManager.CreateGUITexture(file.Name.Replace(".png", "").Remove(0, 4), startId++, file.FullName);
            }
            catch
            {
            }
        }

        private static Dynel FindNearestBankDynel() => DynelManager.AllDynels
            .Where(x => x.Name.Contains("Bank") && x.Identity.Type == IdentityType.Terminal)
            .OrderBy(x => Vector3.Distance(DynelManager.LocalPlayer.Position, x.Position))
            .FirstOrDefault();

        public static bool BankIsNearby(out Dynel dynel)
        {
            dynel = null;

            if (Inventory.Find("Portable Bank Terminal", out _))
                return true;

            dynel = FindNearestBankDynel();

            if (dynel == null)
            {
                Chat.WriteLine("Could not find bank terminal.");
                return false;
            }

            if (Vector3.Distance(DynelManager.LocalPlayer.Position, dynel.Position) > 3f)
            {
                Chat.WriteLine("Bank is too far away.");
                return false;
            }
 
            return true;
        }

        public static bool TryOpenBank()
        {
            if (!BankIsNearby(out Dynel dynel))
                return false;

            if (Inventory.Find("Portable Bank Terminal", out Item item))
                item.Use();
            else
                dynel.Use();

            return true;
        }

        public static unsafe string GetItemName(int lowId, int highId, int ql)
        {
            Identity none = Identity.None;
            IntPtr pEngine = N3Engine_t.GetInstance();

            if (!DummyItem.CreateDummyItemID(lowId, highId, ql, out Identity dummyItemId))
                throw new Exception($"Failed to create dummy item. LowId: {lowId}\tLowId: {highId}\tLowId: {ql}");

            IntPtr pItem = N3EngineClientAnarchy_t.GetItemByTemplate(pEngine, dummyItemId, ref none);

            if (pItem == IntPtr.Zero)
                throw new Exception($"DummyItem::DummyItem - Unable to locate item. LowId: {lowId}\tLowId: {highId}\tLowId: {ql}");

            return AOSharp.Common.Helpers.Utils.UnsafePointerToString((*(MemStruct*)pItem).Name);
        }

        public static int ShiftSlot(int slot) => slot & 0xFFFF;

        public static bool ItemEquals(Item item, ItemInfo itemInfo)
        {
            bool comparison = item.QualityLevel == itemInfo.Ql && item.Id == itemInfo.LowInstance && item.HighId == itemInfo.HighInstance;
            return !(!comparison || item.UniqueIdentity.Instance != itemInfo.Instance);
        }
    }

    [StructLayout(LayoutKind.Explicit, Pack = 0)]
    internal struct MemStruct
    {
        [FieldOffset(0x14)]
        public Identity Identity;

        [FieldOffset(0x9C)]
        public IntPtr Name;
    }

    public static class ContainerRanges
    {
        public static readonly List<int> WeaponPage = new List<int> { 1, 0xF, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };
        public static readonly List<int> ArmorPage = new List<int> { 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 0x1F };
        public static readonly List<int> ImplantPage = Enumerable.Range(33, 12).ToList();
        public static readonly List<int> SocialPage = new List<int> { 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 0x3F };
        public static readonly List<int> Inventory = Enumerable.Range(64, 94).ToList();
        public static readonly List<int> Bank = Enumerable.Range(0, 102).ToList();
        public static readonly List<int> Default = Enumerable.Range(0, 21).ToList();
        public static readonly List<int> GMI = Enumerable.Range(0, 21).ToList();
    }
}