using AOSharp.Core;
using AOSharp.Core.UI;
using EFDataAccessLibrary.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MalisItemFinder
{
    public class ItemScrollListView
    {
        public Slot SelectedItem;
        private List<ItemEntryView> _itemEntries;
        private View _localRoot;
        private View _itemEntryRoot;
        private int _displayedItems;

        public ItemScrollListView(View root)
        {
            _localRoot = root;
            _localRoot.FindChild("ItemEntryRoot", out _itemEntryRoot);
            _itemEntries = new List<ItemEntryView>();

            RecacheItemViews();
        }

        internal void RecacheItemViews()
        {
            Dispose();
            _itemEntries.Clear();

            for (int i = 0; i < Main.MainWindow.MaxElements; i++)
            {
                int color = i % 2 == 0 ? Colors.DarkGrey : Colors.LightGrey;
                _itemEntries.Add(new ItemEntryView((uint)color, this));
            }
        }

        internal void OnUpdate()
        {
            if (Main.MainWindow.SearchResults == null)
                return;

            Refresh();

            Main.MainWindow.SearchResults = null;
        }

        internal void RefreshEntryColors()
        {
            foreach (var entry in _itemEntries)
                entry.ResetColor();

            SelectedItem = null;
        }

        internal void RefreshItemPreviews()
        {
            if (!Main.Settings.ItemPreview)
            {
                int i = 1;
                
                foreach (var itemEntry in _itemEntries)
                {
                    if (i <= _displayedItems)
                        itemEntry.RemoveItem();

                    i++;
                    itemEntry.RemoveSlotView();
                }
            }
            else
            {
                int i = 1;

                foreach (var itemEntry in _itemEntries)
                {
                    itemEntry.AddSlotView();

                    if (i <= _displayedItems)
                        itemEntry.AddItem();
                    i++;
                }
            }

            RecacheItemViews();
            Main.MainWindow.Refresh();
        }

        internal void Refresh()
        {
            var header = Main.MainWindow.TableView.Header.Current;

            if (Main.MainWindow.SearchResults == null)
                return;

            Main.MainWindow.SearchResults.ApplyOrder(header.Mode, header.Direction);
            int itemIndex = 0;

            foreach (var matchingItem in Main.MainWindow.SearchResults)
            {
                if (itemIndex >= _displayedItems)
                {
                    var itemEntry = _itemEntries[itemIndex % Main.MainWindow.MaxElements];
                    _itemEntryRoot.AddChild(itemEntry.Root, true);
                }

                var updatedItemEntry = _itemEntries[itemIndex % Main.MainWindow.MaxElements];
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

            RefreshEntryColors();
        }   
        
        internal void Dispose()
        {
            int i = 1;

            foreach (var item in _itemEntries)
            {
                if (i <= _displayedItems)
                    _itemEntryRoot.RemoveChild(item.Root);

                item.Root.Dispose();

                i++;
            }

            _itemEntryRoot.FitToContents();
            _displayedItems = 0;
        }
    }
}