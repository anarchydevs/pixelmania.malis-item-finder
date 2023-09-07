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
using static MalisItemFinder.Database;

namespace MalisItemFinder
{
    public static class Extensions
    {
        private static void ApplyOrder<TKey>(this List<Slot> items, Func<Slot, TKey> keySelector, Direction direction) where TKey : IComparable
        {
            if (direction == Direction.Ascending)
            {
                items.Sort((a, b) => keySelector(a).CompareTo(keySelector(b)));
            }
            else
            {
                items.Sort((a, b) => keySelector(b).CompareTo(keySelector(a)));
            }
        }

        public static void ApplyCriteria(this List<Slot> matchingItems, Dictionary<FilterCriteria, List<SearchCriteria>> criterias)
        {
            foreach (var filter in criterias)
            {
                switch (filter.Key)
                {
                    case FilterCriteria.Ql:
                        foreach (var qlCriteria in filter.Value)
                            matchingItems.RemoveAll(x => !qlCriteria.MeetsReqs(x.ItemInfo.Ql));
                        break;
                    case FilterCriteria.Id:
                        foreach (var idCriteria in filter.Value)
                            matchingItems.RemoveAll(x => !idCriteria.MeetsReqs(x.ItemInfo.LowInstance));
                        break;
                    case FilterCriteria.Location:
                        foreach (var idCriteria in filter.Value)
                            matchingItems.RemoveAll(x => !idCriteria.MeetsReqs(x.ItemInfo.Slot.ItemContainer.ContainerInstance));
                        break;
                        // Add other cases for different filter criteria as needed
                }
            }
        }

        public static void ApplyOrder(this List<Slot> matchingItems, OrderMode orderMode, Direction direction)
        {
            if (matchingItems.Count() < 1)
                return;

            switch (orderMode)
            {
                case OrderMode.Name:
                    matchingItems.ApplyOrder(item => item.ItemInfo.Name, direction);
                    break;
                case OrderMode.Id:
                    matchingItems.ApplyOrder(item => item.ItemInfo.LowInstance, direction);
                    break;
                case OrderMode.Ql:
                    matchingItems.ApplyOrder(item => item.ItemInfo.Ql, direction);
                    break;
                case OrderMode.Location:
                    matchingItems.ApplyOrder(item => item.ItemInfo.Slot.ItemContainer.ContainerInstance, direction);
                    break;
                case OrderMode.Character:
                    matchingItems.ApplyOrder(item => item.ItemInfo.Slot.ItemContainer.CharacterInventory.CharName, direction);
                    break;
                default:
                    Chat.WriteLine("This shouldn't happen.");
                    break;
            }
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
    
        public static void PeekBags(this IEnumerable<Item> items)
        {
            foreach (var item in items.Where(x=>x.UniqueIdentity.Type == IdentityType.Container))
            {
                item.Use();
                item.Use();
            }
        }

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
                case ContainerId.GMI:
                    return ContainerRanges.GMI;
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