using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.Misc;
using AOSharp.Core.UI;
using EFDataAccessLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace MalisItemFinder
{
    public class InventoryManager
    {
        private SqliteContext _db;
        private ItemTracker _itemTracker;
        public bool SearchInProgress = false;
        public List<Slot> SearchResults = new List<Slot>();

        private ContainerProcessor _containerProcessor = new ContainerProcessor(); 

        public InventoryManager()
        {
            Chat.WriteLine("Loading database..");
            Task.Run(() => { LoadDb(); });
        }

        private void LoadDb()
        {
            string dataFolderPath = Path.Combine($"{Main.PluginDir}", "Data");
            Directory.CreateDirectory(dataFolderPath);
            string dbPath = Path.Combine(dataFolderPath, "Inventories.db");

            _db = new SqliteContext(dbPath);

            if (File.Exists(dbPath))
            {
                Chat.WriteLine("Database loaded");
            }
            else
            {
                _db.Database.EnsureCreated();
                _db.Inventories.AddRange(new List<CharacterInventory>());
                DbSaveAsync();

                Chat.WriteLine("Db not found. Loading default.");
            }

            if (!_db.Inventories.Any(x => x.CharName == DynelManager.LocalPlayer.Name))
            {
                _db.Inventories.Add(new CharacterInventory { CharName = DynelManager.LocalPlayer.Name });
                DbSaveAsync();
            }

            _itemTracker = new ItemTracker(this);

            Network.N3MessageSent += _itemTracker.OnN3MessageSent;
            Network.N3MessageReceived += _itemTracker.OnN3MessageReceived;
            Inventory.ContainerOpened += OnContainerOpenedAsync;
        }

        internal Task<int> DbSaveAsync() => _db.SaveChangesAsync();

        internal void RegisterContainer(ContainerId containerId)
        {
            try
            {
                switch (containerId)
                {
                    case ContainerId.Inventory:
                        RegisterInventoryAsync(false);
                        break;
                    case ContainerId.Bank:
                        RegisterBankAsync();
                        break;
                    default:
                        RegisterInventoryContainerAsync(containerId);
                        break;
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine(ex.Message);
            }
        }

        internal void RemoveMissingBagsAsync() => _containerProcessor.AddChange(() => RemoveMissingBags());

        private void RemoveMissingBags()
        {
            var localPlayerInv = _db.Inventories
                .Include(x => x.ItemContainers)
                .ThenInclude(x => x.Slots)
                .ThenInclude(x => x.ItemInfo)
                .FirstOrDefault(x => x.CharName == DynelManager.LocalPlayer.Name);

            var containers = localPlayerInv.ItemContainers.Where(x => !Enum.IsDefined(typeof(ContainerId), x.ContainerInstance) && Inventory.Backpacks.Any(c => c.Identity.Instance != (int)x.ContainerInstance));

            foreach (var container in containers)
            {
                _db.ItemInfos.RemoveRange(container.Slots.Select(x => x.ItemInfo));
                _db.Slots.RemoveRange(container.Slots);
                _db.ItemContainers.Remove(container);
            }

            RegisterInventory(false);
        }

        internal void OnContainerOpenedAsync(object sender, Container container)
        {
            try
            {
                switch (container.Identity.Type)
                {
                    case IdentityType.Bank:
                        RegisterBankAsync();
                        break;
                    default:
                        RegisterInventoryContainerAsync((ContainerId)container.Identity.Instance);
                        break;
                }

                Chat.WriteLine($"{container.Identity} updated.");
            }
            catch (Exception ex)
            {
                Chat.WriteLine(ex.Message);
            }
        }

        public void RegisterInventoryContainerAsync(ContainerId containerId) => _containerProcessor.AddChange(() => RegisterInventoryContainer(containerId));

        public void RegisterBankAsync() => _containerProcessor.AddChange(() => RegisterBank());

        public void RegisterInventoryAsync(bool openBags = true) => _containerProcessor.AddChange(() => RegisterInventory(openBags));

        private void RegisterInventory(bool openBags = true)
        {
            Dictionary<IdentityType, List<Item>> itemDictionary = new Dictionary<IdentityType, List<Item>>
            {
                { IdentityType.Inventory, Inventory.Items.Where(x=>x.Slot.Type == IdentityType.Inventory).ToList()} ,
                { IdentityType.WeaponPage, Inventory.Items.Where(x=>x.Slot.Type == IdentityType.WeaponPage).ToList()} ,
                { IdentityType.ArmorPage, Inventory.Items.Where(x=>x.Slot.Type == IdentityType.ArmorPage).ToList()} ,
                { IdentityType.ImplantPage, Inventory.Items.Where(x=>x.Slot.Type == IdentityType.ImplantPage).ToList()} ,
                { IdentityType.SocialPage, Inventory.Items.Where(x=>x.Slot.Type == IdentityType.SocialPage).ToList()} ,
            };

            foreach (var items in itemDictionary)
            {
                ContainerId containerId = (ContainerId)items.Key;
                ItemContainer itemContainer = TryGetItemContainer(containerId);

                RegisterItems(itemContainer, items.Value, containerId);
                if (openBags)
                {
                    foreach (var item in items.Value.Where(x => x.UniqueIdentity.Type == IdentityType.Container))
                    {
                        item.Use();
                        item.Use();
                    }
                }
            }
        }

        private void RegisterInventoryContainer(ContainerId containerId)
        {
            ItemContainer itemContainer = TryGetItemContainer(containerId);
            RegisterItems(itemContainer, Inventory.Backpacks.FirstOrDefault(x => x.Identity == new Identity(IdentityType.Container, (int)containerId)).Items.ToList(), containerId);

            if (itemContainer.Root != ContainerId.Inventory)
                itemContainer.Root = ContainerId.Inventory;
        }

        private void RegisterBank()
        {
            ItemContainer itemContainer = TryGetItemContainer(ContainerId.Bank);
            RegisterItems(itemContainer, Inventory.Bank.Items, ContainerId.Bank);
        }

        private void RegisterItems(ItemContainer container, List<Item> itemList, ContainerId containerId)
        {
            Dictionary<int, ItemComparer> comparerDict = new Dictionary<int, ItemComparer>();
            List<Slot> slots = container.Slots;

            var slotsRange = containerId.GetMaxSlots();

            for (int i = slotsRange.First(); i < slotsRange.Last() + 1; i++)
            {
                comparerDict.Add(i, new ItemComparer
                {
                    Item = itemList.FirstOrDefault(x => Utils.ShiftSlot(x.Slot.Instance) == i),
                    Slot = slots.FirstOrDefault(x => x.SlotInstance == i),
                });              
            }

            foreach (ItemComparer comparer in comparerDict.Values)
            {
                Slot slot = comparer.Slot;
                Item item = comparer.Item;

                if (slot == null)
                {
                    if (item != null)
                        AddSlot(item, container, containerId);

                    continue;
                }

                if (item == null)
                {
                    RemoveItem(slot);
                    continue;
                }

                if (!Utils.ItemEquals(item, slot.ItemInfo))
                {
                    UpdateItem(slot, item);
                }
            }

            //foreach (ItemComparer comparer in comparerDict.Values)
            //{
            //    Slot slot = comparer.Slot;
            //    Item item = comparer.Item;

            //    if (slot != null)
            //    {
            //        if (item != null)
            //        {
            //            if (Utils.ItemEquals(item, slot.ItemInfo))
            //            {
            //                //  Chat.WriteLine($"Item is same. Skipping: {item.Name} - {item.Slot}");
            //                continue;
            //            }
            //            else
            //            {
            //                UpdateItem(slot, item);
            //            }
            //        }
            //        else
            //        {
            //            RemoveItem(slot);
            //            // Chat.WriteLine($"Removing: {query.ItemInfo.Name} - {query.SlotInstance}");
            //        }
            //    }
            //    else
            //    {

            //        if (item == null)
            //        {
            //            continue;
            //        }
            //        else
            //        {
            //            AddSlot(item, container, containerId);
            //            // Chat.WriteLine($"Adding: {item.Name} - {item.Slot}");
            //        }
            //    }
            //}

        }

        private void AddSlot(Item item, ItemContainer containerRef, ContainerId containerId)
        {
            Slot slot = new Slot
            {
                SlotInstance = Utils.ShiftSlot(item.Slot.Instance),
                ItemContainer = containerRef,
            };

            _db.Slots.Add(slot);

            ItemInfo itemInfo = new ItemInfo
            {
                Name = Utils.GetItemName(item.Id, item.HighId, item.QualityLevel),
                LowInstance = item.Id,
                HighInstance = item.HighId,
                Ql = item.QualityLevel,
                Type = (int)item.UniqueIdentity.Type,
                Instance = item.UniqueIdentity.Instance,
                Slot = slot
            };

            ItemContainerRootUpdate(item);

            _db.ItemInfos.Add(itemInfo);
        }

        private void RemoveItem(Slot slot)
        {
            _db.Slots.Remove(slot);
        }

        private void UpdateItem(Slot slot, Item item)
        {
            var newSlot = _db.Slots.Find(slot.Id).ItemInfo;
            newSlot.LowInstance = item.Id;
            newSlot.HighInstance = item.HighId;
            newSlot.Ql = item.QualityLevel;
            newSlot.Type = (int)item.UniqueIdentity.Type;
            newSlot.Instance = item.UniqueIdentity.Instance;
            newSlot.Name = Utils.GetItemName(item.Id, item.HighId, item.QualityLevel);

            ItemContainerRootUpdate(item);
        }

        private void ItemContainerRootUpdate(Item item)
        {
            if (item.UniqueIdentity.Type != IdentityType.Container)
                return;

            var container = _db.ItemContainers.FirstOrDefault(x => x.ContainerInstance == (ContainerId)item.UniqueIdentity.Instance);

            if (container != null)
                container.Root = item.Slot.Type == IdentityType.BankByRef ? ContainerId.Bank : (ContainerId)item.Slot.Type;
        }

        private ItemContainer TryGetItemContainer(ContainerId containerId)
        {
            var localPlayerInv = _db.Inventories
                .Include(x => x.ItemContainers)
                .ThenInclude(x => x.Slots)
                .ThenInclude(x => x.ItemInfo)
                .FirstOrDefault(x => x.CharName == DynelManager.LocalPlayer.Name);

            var container = localPlayerInv.ItemContainers.FirstOrDefault(x => x.ContainerInstance == containerId);
         
            if (container != null)
                return container;

            container = new ItemContainer
            {
                ContainerInstance = containerId,
                CharacterInventory = localPlayerInv,
            };

            switch (containerId)
            {
                case ContainerId.Bank:
                case ContainerId.Inventory:
                case ContainerId.ArmorPage:
                case ContainerId.SocialPage:
                case ContainerId.WeaponPage:
                case ContainerId.ImplantPage:
                    container.Root = containerId;
                    break;
            }

            _db.ItemContainers.Add(container);

            return container;
        }

        internal void ItemLookup(IEnumerable<string> searchTerms, int maxElements = int.MaxValue)
        {
            SearchInProgress = true;
            Task.Run(() => ItemLookupAsync(searchTerms, maxElements));
        }

        private void ItemLookupAsync(IEnumerable<string> searchTerms, int maxElements = int.MaxValue)
        {
            var db = _db.Inventories
                .Include(b => b.ItemContainers)
                .ThenInclude(c => c.Slots)
                .ThenInclude(c => c.ItemInfo)
                .ToList()
                .SelectMany(x => x.ItemContainers)
                .SelectMany(x => x.Slots)
                .Where(s => searchTerms.All(term => s.ItemInfo.Name.ToLower().Contains(term)));

            switch (Main.Window.GetCurrentHeader().Mode)
            {
                case (OrderMode.Name):
                    db = db.OrderBy(x => x.ItemInfo.Name);
                    break;
                case (OrderMode.Id):
                    db = db.OrderBy(x => x.ItemInfo.LowInstance);
                    break;
                case (OrderMode.Ql):
                    db = db.OrderBy(x => x.ItemInfo.Ql);
                    break;
                case (OrderMode.Location):
                    db = db.OrderBy(x => x.ItemInfo.Slot.ItemContainer.Root.ToString());
                    break;
                case (OrderMode.Character):
                    db = db.OrderBy(x => x.ItemInfo.Slot.ItemContainer.CharacterInventory.CharName);
                    break;
            }

            SearchResults = db.Take(maxElements).ToList();
            SearchInProgress = false;
        }

        public class ItemComparer
        {
            public Slot Slot;
            public Item Item;
        }
    }
}
