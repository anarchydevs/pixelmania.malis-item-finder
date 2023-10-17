using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.GMI;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using EFDataAccessLibrary.Models;
using Force.DeepCloner;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MalisItemFinder
{
    public class Database
    {
        internal string Path;

        public Database()
        {
            try
            {
                Chat.WriteLine("Loading database..");
              
                EventHandler.Load();
                DatabaseProcessor.Load();

                Game.TeleportStarted += ItemTracker.OnTeleportStarted;
                EventHandler.OnN3MessageReceived += ItemTracker.OnN3MessageReceived;
                EventHandler.OnContainerOpened += ItemTracker.OnContainerOpened;
                Network.N3MessageSent += ItemTracker.OnN3MessageSent;

                LoadDb();
                RegisterInventory();
                RegisterGMI();
            }
            catch (Exception ex)
            {
                Chat.WriteLine(ex.Message);
                Chat.WriteLine($"Failed to load db. This shouldn't happen.");
            }
        }

        internal Task<int> DbSaveAsync(SqliteContext db) => db.SaveChangesAsync();

        private void LoadDb()
        {
            string dataFolderPath = System.IO.Path.Combine($"{Main.PluginDir}", "Data");
            Directory.CreateDirectory(dataFolderPath);
            Path = System.IO.Path.Combine(dataFolderPath, "Inventories.db");

            using (var db = new SqliteContext(Path))
            {
                if (File.Exists(Path))
                {
                    Chat.WriteLine("Database loaded");
                }
                else
                {
                    db.Database.EnsureCreated();
                    db.Inventories.AddRange(new List<CharacterInventory>());
                    DbSaveAsync(db);

                    Chat.WriteLine("Db not found. Loading default.");
                }

                if (!db.Inventories.Any(x => x.CharName == DynelManager.LocalPlayer.Name))
                {
                    var charInv = new CharacterInventory { CharName = DynelManager.LocalPlayer.Name };
                    db.Inventories.Add(charInv);

                    foreach (ContainerId containerId in Enum.GetValues(typeof(ContainerId)))
                    {
                        if (containerId == ContainerId.None)
                            continue;

                        db.ItemContainers.Add(new ItemContainer
                        {
                            ContainerInstance = containerId,
                            CharacterInventory = charInv,
                            Root = containerId,
                        });
                    }

                    DbSaveAsync(db);
                    Chat.WriteLine($"Registering character '{DynelManager.LocalPlayer.Name}'.");
                }
            }
        }

        internal bool TryDeleteInventory(string charName)
        {
            using (var db = new SqliteContext(Path))
            {
                var inventory = db.Inventories.Where(x => x.CharName == charName).FirstOrDefault();

                if (inventory == null)
                    return false;

                db.Inventories.Remove(db.Inventories.Where(x => x.CharName == charName).FirstOrDefault());
                DbSaveAsync(db);

                return true;
            }
        }

        internal void RegisterContainer(ContainerId containerId)
        {
            try
            {
                switch (containerId)
                {
                    case ContainerId.Inventory:
                        RegisterInventory();
                        break;
                    case ContainerId.Bank:
                        RegisterBank();
                        break;
                    default:
                        RegisterContainerItems(containerId, Inventory.Backpacks.FirstOrDefault(x => x.Identity.Instance == (int)containerId).Items.DeepClone());
                        break;
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine(ex.Message);
                Chat.WriteLine("RegisterContainer");
            }
        }

        internal void RegisterInventory() => DatabaseProcessor.AddChange(new DatabaseAction 
        {
            Name = "RegisterInventory", 
            Action = (db, charInv) => RegisterInventory(db, charInv, CloneInventoryItems()) 
        });

        internal void RegisterBank() => DatabaseProcessor.AddChange(new DatabaseAction 
        { 
            Name = "RegisterBank", 
            Action = (db, charInv) => RegisterItems(db, charInv, ContainerId.Bank, Inventory.Bank.Items.DeepClone()) 
        });

        internal void RegisterContainerItems(ContainerId container, List<Item> items) => DatabaseProcessor.AddChange(new DatabaseAction
        {
            Name = "RegisterContainerItems",
            Action = (db, charInv) => RegisterItems(db, charInv, container, items.DeepClone())
        });

        internal void RemoveMissingBags(ContainerId rootContainerId, List<ContainerId> missingContainerIds) => DatabaseProcessor.AddChange(new DatabaseAction
        {
            Name = "RemoveMissingBag",
            Action = (db, charInv) => RemoveMissingBags(db, charInv, rootContainerId, missingContainerIds)
        });

        internal void FixBackpackRoots(ContainerId rootId) => DatabaseProcessor.AddChange(new DatabaseAction
        {
            Name = "FixBackpackRoots",
            Action = (db, charInv) => FixBackpackRoots(db, charInv, rootId,
                rootId == ContainerId.Inventory ?
                Inventory.Backpacks.Select(x => (ContainerId)x.Identity.Instance) :
                rootId == ContainerId.Bank ?
                Inventory.Bank.Backpacks.Select(x => (ContainerId)x.Identity.Instance) :
                new List<ContainerId>())
        });

        internal void RemoveMissingBags(SqliteContext db, CharacterInventory charInv, ContainerId containerId, List<ContainerId> identities)
        {
            var containers = charInv.ItemContainers.Where(x => x.Root == containerId && identities.Contains(x.ContainerInstance));

            foreach (var container in containers)
            {
                Chat.WriteLine($"Lost ownership of container '{container.ContainerInstance}'. Removing from database.");
                db.Slots.RemoveRange(container.Slots);
                db.ItemContainers.Remove(container);
            }
        }

        internal void RegisterGMI()
        {
            GMI.GetInventory()?.ContinueWith(marketInventory =>
            {
                var result = marketInventory.Result;

                if (result == null)
                    return;

                DatabaseProcessor.AddChange(new DatabaseAction
                {
                    Name = "RegisterGMI",
                    Action = (db, charInv) => RegisterItems(db, charInv, ContainerId.GMI, result.Items.DeepClone())
                });
            });
        }

        private void FixBackpackRoots(SqliteContext db, CharacterInventory charInv, ContainerId rootId, IEnumerable<ContainerId> containerIds)
        {
            foreach (var containerId in containerIds)
            {
                ItemContainer container = charInv.ItemContainers.FirstOrDefault(x => x.ContainerInstance == containerId);

                if (container == null)
                    continue;

                if (container.Root == rootId)
                    continue;

                container.Root = rootId;
            }
        }

        private void RegisterInventory(SqliteContext db, CharacterInventory charInv, Dictionary<IdentityType, List<Item>> items)
        {
            foreach (var item in items)
                RegisterItems(db, charInv, (ContainerId)item.Key, item.Value);
        }

        private void RegisterItems(SqliteContext db, CharacterInventory charInv, ContainerId containerId, IEnumerable<MyMarketInventoryItem> itemList)
        {
            try
            {
                ItemContainer container = charInv.ItemContainers.FirstOrDefault(x => x.ContainerInstance == containerId);
                Dictionary<int, MarketItemComparer> comparerDict = new Dictionary<int, MarketItemComparer>();
                List<Slot> slots = container.Slots;
                var slotsRange = container.ContainerInstance.GetMaxSlots();

                for (int i = slotsRange.First(); i < slotsRange.Last() + 1; i++)
                {
                    comparerDict.Add(i, new MarketItemComparer
                    {
                        Item = itemList.FirstOrDefault(x => Utils.ShiftSlot(x.Slot) == i),
                        Slot = slots.FirstOrDefault(x => x.SlotInstance == i),
                    });
                }

                foreach (MarketItemComparer comparer in comparerDict.Values)
                {
                    Slot slot = comparer.Slot;
                    MyMarketInventoryItem item = comparer.Item;

                    if (slot == null)
                    {
                        if (item != null)
                            AddSlot(db, item, container);

                        continue;
                    }

                    if (slot.Id == 0)
                        continue;

                    if (item == null)
                    {
                        db.Slots.Remove(slot);

                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine(ex.Message);
                Chat.WriteLine("RegisterItems");
            }
        }

        private void RegisterItems(SqliteContext db, CharacterInventory charInv, ContainerId containerId, IEnumerable<Item> itemList)
        {
            try
            {
                ItemContainer containerRoot = charInv.ItemContainers.FirstOrDefault(x => x.ContainerInstance == containerId);
                Dictionary<int, ItemComparer> comparerDict = new Dictionary<int, ItemComparer>();
                List<Slot> slots = containerRoot.Slots;
                var slotsRange = containerRoot.ContainerInstance.GetMaxSlots();

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
                        {
                            AddSlot(db, item, containerRoot);

                            if (item.UniqueIdentity.Type == IdentityType.Container)
                                AddContainer(db, item, containerRoot);
                        }

                        continue;
                    }

                    if (slot.Id == 0)
                        continue;

                    if (item == null)
                    {
                        db.Slots.Remove(slot);
                        continue;
                    }

                    if (!Utils.ItemEquals(item, slot.ItemInfo))
                    {
                        UpdateItem(slot, item);

                        if (item.UniqueIdentity.Type == IdentityType.Container)
                            AddContainer(db, item, containerRoot);
                    }
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine(ex.Message);
                Chat.WriteLine("RegisterItems");
            }
        }

        private void UpdateItem(Slot slot, Item item)
        {
            var itemInfo = slot.ItemInfo;
            itemInfo.LowInstance = item.Id;
            itemInfo.HighInstance = item.HighId;
            itemInfo.Ql = item.QualityLevel;
            itemInfo.Type = (int)item.UniqueIdentity.Type;
            itemInfo.Instance = item.UniqueIdentity.Instance;
            itemInfo.Name = Utils.GetItemName(item.Id, item.HighId, item.QualityLevel);
        }

        private void AddContainer(SqliteContext db, Item item,ItemContainer containerRoot)
        {
            var localPlayerInv = db.Inventories.Where(x => x.CharName == DynelManager.LocalPlayer.Name).FirstOrDefault();
            var container = db.Inventories.Where(x => x.CharName == DynelManager.LocalPlayer.Name).FirstOrDefault().ItemContainers.FirstOrDefault(x => x.ContainerInstance == (ContainerId)item.UniqueIdentity.Instance);

            if (container == null)
            {
                db.ItemContainers.Add(new ItemContainer
                {
                    ContainerInstance = (ContainerId)item.UniqueIdentity.Instance,
                    CharacterInventory = localPlayerInv,
                    Root = containerRoot.ContainerInstance,
                });
            }
            else if (container.Root != containerRoot.Root)
            {
                //Chat.WriteLine(containerRef.Root);
                container.Root = containerRoot.Root;
            }
        }

        private void AddSlot(SqliteContext db, string itemName, int lowId, int highId, int ql, int slotInstance, IdentityType identityType, int identityInstance, ItemContainer containerRef)
        {
            Slot slot = new Slot
            {
                SlotInstance = Utils.ShiftSlot(slotInstance),
                ItemContainer = containerRef,
            };

            db.Slots.Add(slot);

            ItemInfo itemInfo = new ItemInfo
            {
                Name = itemName,
                LowInstance = lowId,
                HighInstance = highId,
                Ql = ql,
                Type = (int)identityType,
                Instance = identityInstance,
                Slot = slot
            };

            db.ItemInfos.Add(itemInfo);
        }

        private void AddSlot(SqliteContext db, Item item, ItemContainer containerRef) => AddSlot(db, 
            Utils.GetItemName(item.Id, item.HighId, item.QualityLevel), 
            item.Id, 
            item.HighId, 
            item.QualityLevel,
            item.Slot.Instance,
            item.UniqueIdentity.Type,
            item.UniqueIdentity.Instance,
            containerRef);

        private void AddSlot(SqliteContext db, MyMarketInventoryItem item, ItemContainer containerRef) => AddSlot(db,
             item.Name,
             257958,
             257958,
             item.TemplateQL,
             item.Slot,
             0,
             0,
             containerRef);

        private Dictionary<IdentityType, List<Item>> CloneInventoryItems()
        {
            var identityTypesToInclude = new[] { IdentityType.Inventory, IdentityType.WeaponPage, IdentityType.ArmorPage, IdentityType.ImplantPage, IdentityType.SocialPage };
            var categorizedItems = new Dictionary<IdentityType, List<Item>>();

            foreach (var identityType in identityTypesToInclude)
                categorizedItems[identityType] = new List<Item>();

            foreach (var item in Inventory.Items)
                if (categorizedItems.TryGetValue(item.Slot.Type, out var itemList))
                    itemList.Add(item.DeepClone());

            return categorizedItems;
        }

        internal List<string> GetAllCharacters()
        {
            using (var db = new SqliteContext(Path))
            {
                return db.Inventories.Select(x => x.CharName).ToList();
            }
        }

        public List<Slot> ItemLookup(string character,IEnumerable<string> searchTerms, Dictionary<FilterCriteria, List<SearchCriteria>> criterias, int maxElements)
        {
            using (var db = new SqliteContext(Path))
            {
                IQueryable<CharacterInventory> charInventory = character == "All" ? db.Inventories : db.Inventories.Where(x => x.CharName == character);
                HeaderButton header = Main.MainWindow.GetCurrentHeader();
                List<Slot> slots = charInventory.Include(b => b.ItemContainers)
                    .ThenInclude(c => c.Slots)
                    .ThenInclude(c => c.ItemInfo)
                    .ToList()
                    .SelectMany(x => x.ItemContainers)
                    .SelectMany(x => x.Slots)
                    .Where(s => searchTerms.All(term => s.ItemInfo.Name.ToLower().Contains(term)))
                    .ToList();

                slots.ApplyCriteria(criterias);
                slots.ApplyOrder(header.Mode, header.Direction);

                return slots.Take(maxElements).ToList();
            }
        }
        public class ItemComparer
        {
            public Slot Slot;
            public Item Item;
        }

        public class MarketItemComparer
        {
            public Slot Slot;
            public MyMarketInventoryItem Item;
        }
    }
}
