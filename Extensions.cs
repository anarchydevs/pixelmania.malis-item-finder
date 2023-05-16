using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Common.Unmanaged.Interfaces;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using EFDataAccessLibrary.Models;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static MalisItemFinder.InventoryManager;

namespace MalisItemFinder
{
    public static class Extensions
    {
        public static bool BankIsNearby(this LocalPlayer localPlayer, out Dynel bank)
        {
            bank = DynelManager.AllDynels.FirstOrDefault(x => x.Name.Contains("Bank"));

            if (bank != null && Vector3.Distance(DynelManager.LocalPlayer.Position, bank.Position) < 5f)
                return true;

            return false;
        }

        public static bool TryOpenBank(this LocalPlayer localPlayer)
        {
            if (Inventory.Find("Portable Bank Terminal", out Item item))
            {
                item.Use();
            }
            else if (localPlayer.BankIsNearby(out Dynel bankDynel))
            {
                bankDynel.Use();
            }
            else
            {
                return false;
            }

            return true;
        }

        public static void SetAllGfx(this Button button, int gfxId)
        {
            button.SetGfx(ButtonState.Raised, gfxId);
            button.SetGfx(ButtonState.Hover, gfxId);
            button.SetGfx(ButtonState.Pressed, gfxId);
        }

        public static bool Find(this List<Item> items, int lowId, int highId, int ql, out Item item)
        {
            return (item = items.FirstOrDefault((Item x) => x.Id == lowId && x.HighId == highId && x.QualityLevel == ql)) != null;
        }

        public static bool Find(this List<Item> items, Slot slot, out Item item)
        {
            return (item = items.FirstOrDefault((Item x) => x.Id == slot.ItemInfo.LowInstance && x.HighId == slot.ItemInfo.HighInstance && x.QualityLevel == slot.ItemInfo.Ql)) != null;
        }

        public static bool IsContainer(this ContainerId id) => Enum.IsDefined(typeof(ContainerId), id);

        public static Backpack SlotHandleToBackpack(this Identity identity)
        {
            Backpack backpack = Inventory.Backpacks.FirstOrDefault(x => x.Items.Any(y => y.Slot == identity));

            if (backpack == null)
                return null;

            return backpack;
        }

        public static List<int> GetMaxSlots(this ContainerId containerId)
        {
            switch (containerId)
            {
                case ContainerId.WeaponPage:
                    return ContainerRanges.WeaponPage;
                case ContainerId.ArmorPage:
                    return ContainerRanges.ArmorPage;
                case ContainerId.ImplantPage:
                    return ContainerRanges.ImplantPage;
                case ContainerId.SocialPage:
                    return ContainerRanges.SocialPage;
                case ContainerId.Inventory:
                    return ContainerRanges.Inventory;
                case ContainerId.Bank:
                    return ContainerRanges.Bank;
                default:
                    return ContainerRanges.Default;
            }
        }

        public static Slot FindContainer(this CharacterInventory charInv, int instance)
        {
            foreach (var container in charInv.ItemContainers)
            {
                foreach (var slot in container.Slots)
                {
                    if (slot.ItemInfo.Instance == instance)
                    {
                        return slot;
                    }
                }
            }

            return null;
        }
    }
}