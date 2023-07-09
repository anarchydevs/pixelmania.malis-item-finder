using AOSharp.Core;
using AOSharp.Core.UI;
using EFDataAccessLibrary.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static MalisItemFinder.MainWindow;

namespace MalisItemFinder
{
    public class ItemScrollListView
    {
        public Slot SelectedItem;

        private List<ItemEntryView> _itemEntries;
        private View _localRoot;
        private View _mainWindowRoot;
        private View _itemEntryRoot;
        private int _precachedElements = 50;
        private int _displayedItems;

        public ItemScrollListView(View mainWindowRoot)
        {
            _mainWindowRoot = mainWindowRoot;
            _localRoot = View.CreateFromXml($"{Main.PluginDir}\\UI\\Views\\ItemScrollListView.xml");

            _localRoot.FindChild("ItemEntryRoot", out _itemEntryRoot);
            _mainWindowRoot.AddChild(_localRoot, true);
            _mainWindowRoot.FitToContents();

            _itemEntries = new List<ItemEntryView>();

            for (int i = 0; i < _precachedElements; i++)
            {
                int color = i % 2 == 0 ? Colors.DarkGrey : Colors.LightGrey;
                _itemEntries.Add(new ItemEntryView((uint)color, this));
            }
        }

        internal void OnUpdate(object sender, float e)
        {
            if (Main.InventoryManager.SearchResults == null)
                return;

            Refresh(Main.InventoryManager.SearchResults, Main.Window.GetCurrentHeader());

            Main.InventoryManager.SearchResults = null;
        }

        internal void RefreshEntryColors()
        {
            foreach (var entry in _itemEntries)
                entry.ResetColor();

            SelectedItem = null;
        }

        internal void QueueSearch(List<string> searchTerms)
        {
            if (Main.InventoryManager.SearchInProgress)
            {
                Chat.WriteLine("Search already in progress.");
                return;
            }

            Main.InventoryManager.ItemLookup(searchTerms, _precachedElements);
        }

        internal void Refresh(List<Slot> matchingItems, HeaderButton headerButton)
        {
            if (matchingItems.Count > 1)
            {
                switch (headerButton.Mode)
                {
                    case OrderMode.Name:
                        matchingItems = ApplyOrder(matchingItems, item => item.ItemInfo.Name, headerButton.Direction).ToList();
                        break;
                    case OrderMode.Id:
                        matchingItems = ApplyOrder(matchingItems, item => item.ItemInfo.Id, headerButton.Direction).ToList();
                        break;
                    case OrderMode.Ql:
                        matchingItems = ApplyOrder(matchingItems, item => item.ItemInfo.Ql, headerButton.Direction).ToList();
                        break;
                    case OrderMode.Location:
                        matchingItems = ApplyOrder(matchingItems, item => item.ItemInfo.Slot.ItemContainer.Root.ToString(), headerButton.Direction).ToList();
                        break;
                    case OrderMode.Character:
                        matchingItems = ApplyOrder(matchingItems, item => item.ItemInfo.Slot.ItemContainer.CharacterInventory.CharName, headerButton.Direction).ToList();
                        break;
                    default:
                        Chat.WriteLine("This shouldn't happen.");
                        break;
                }
            }

            int itemIndex = 0;

            foreach (var matchingItem in matchingItems)
            {
                if (itemIndex >= _displayedItems)
                {
                    var itemEntry = _itemEntries[itemIndex % _precachedElements];
                    _itemEntryRoot.AddChild(itemEntry.Root, true);
                }

                var updatedItemEntry = _itemEntries[itemIndex % _precachedElements];
                updatedItemEntry.Update(matchingItem);
                itemIndex++;
            }

            while (_displayedItems > itemIndex)
            {
                _itemEntries[_displayedItems - 1].RemoveItem();
                _itemEntryRoot.RemoveChild(_itemEntries[_displayedItems - 1].Root);
                _displayedItems--;
            }

            _displayedItems = itemIndex;
            _itemEntryRoot.FitToContents();

            //Chat.WriteLine($"Results: {_displayedItems}");
        }

        private IOrderedEnumerable<Slot> ApplyOrder<TKey>(List<Slot> items, Func<Slot, TKey> keySelector, Direction direction) where TKey : IComparable => direction == Direction.Ascending ? items.OrderBy(item => keySelector(item)) : items.OrderByDescending(item => keySelector(item));
    }
}