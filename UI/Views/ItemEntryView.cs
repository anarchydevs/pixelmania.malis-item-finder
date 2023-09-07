using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using EFDataAccessLibrary.Models;
using System;
using System.Collections.Generic;

namespace MalisItemFinder
{
    public class ItemEntryView
    {
        public View Root;

        private ItemEntryData _itemEntryData;
        private Slot _itemLookup;
        private Identity _dummyItemId;

        private BitmapView _background;
        private uint _localColor;
        private ItemScrollListView _scrollListView;

        public ItemEntryView(uint color, ItemScrollListView scrollListView)
        {
            Root = View.CreateFromXml($"{Main.PluginDir}\\UI\\Views\\ItemEntryView.xml");
            _localColor = color;
            _scrollListView = scrollListView;

            if (Root.FindChild("ItemSlot", out View itemSlot)) { }
            if (Root.FindChild("Name", out TextView name)) { }
            if (Root.FindChild("Id", out TextView id)) { }
            if (Root.FindChild("Ql", out TextView ql)) { }
            if (Root.FindChild("Location", out TextView location)) { }
            if (Root.FindChild("Character", out TextView character)) { }
            if (Root.FindChild("Button", out Button button)) { button.Clicked = OnEntryClicked; }
            if (Root.FindChild("Background", out  _background)) 
            {
                _background.SetBitmap(TextureId.ItemEntryBackground);
                _background.SetLocalColor(_localColor);
            }

            _itemEntryData = new ItemEntryData(itemSlot, name, id, ql, location, character);
        }

        private void OnEntryClicked(object sender, ButtonBase e)
        {
            _scrollListView.RefreshEntryColors();
            _background.SetLocalColor(Colors.Green);
            _scrollListView.SelectedItem = _itemLookup;

            Midi.Play("Click");
        }

        internal void ResetColor()
        {
            _background.SetLocalColor(_localColor);
        }

        internal void Update(Slot itemInfo)
        {
            _itemLookup = itemInfo;

            ItemContainer rootContainer = _itemLookup.ItemContainer;

            var affix = rootContainer.Root != rootContainer.ContainerInstance ? $" [B]" : $"";
            string itemLocationText = rootContainer.Root + affix;

            _itemEntryData.SetText(
                _itemLookup.ItemInfo.Name, 
                _itemLookup.ItemInfo.LowInstance.ToString(), 
                _itemLookup.ItemInfo.Ql.ToString(), 
                itemLocationText,
                _itemLookup.ItemInfo.Slot.ItemContainer.CharacterInventory.CharName);

            if (!Main.Settings.ItemPreview)
                return;

            if (!DummyItem.CreateDummyItemID(_itemLookup.ItemInfo.LowInstance, _itemLookup.ItemInfo.HighInstance, _itemLookup.ItemInfo.Ql, out _dummyItemId))
                return;

            _itemEntryData.AddItem(_dummyItemId);
        }

        internal void RemoveItem() => _itemEntryData.RemoveItem();
    }

    public class ItemEntryData
    {
        private ItemEntrySlotView _itemSlot;
        private TextView _name;
        private TextView _id;
        private TextView _ql;
        private TextView _location;
        private TextView _character;

        public ItemEntryData(View itemSlot, TextView name, TextView id, TextView ql, TextView location, TextView character)
        {
            _itemSlot = new ItemEntrySlotView(itemSlot);
            _name = name;
            _id = id;
            _ql = ql;
            _location = location;
            _character = character;
        }

        internal void SetText(string name, string id, string ql, string container, string character)
        {
            _name.Text = name;
            _id.Text = id;
            _ql.Text = ql;
            _location.Text = container;
            _character.Text = character;
        }

        internal void AddItem(Identity dummyItemIdentity) => _itemSlot.AddItem(dummyItemIdentity);
        internal void RemoveItem() => _itemSlot.RemoveItem();
    }

    public class ItemEntrySlotView
    {
        protected MultiListView _slotView;
        protected InventoryListViewItem _itemView = null;

        public ItemEntrySlotView(View itemSlotRoot)
        {
            SetupSlotView();
            itemSlotRoot.AddChild(_slotView, true);
            itemSlotRoot.FitToContents();
        }

        public void SetupSlotView()
        {
            _slotView = ItemListViewBase.Create(new Rect(20, 20, 20, 20), 0, 0);
            _slotView.SetGridIconSpacing(new Vector2(6000, 6000));
            _slotView.SetGridIconSize(3);
            _slotView.SetLayoutMode(0);
            _slotView.SetViewCellCounts(Vector2.Zero, Vector2.Zero);
        }

        public bool IsSlotEmpty() => _itemView == null;

        public void RemoveItem()
        {
            if (IsSlotEmpty())
                return;

            _slotView.RemoveItem(_itemView); // We might need to call a dispose here
            _itemView = null;
        }

        public void AddItem(Identity dummyItem)
        {
            RemoveItem();
            _itemView = InventoryListViewItem.Create(0, dummyItem, true);
            _slotView.AddItem(Vector2.Zero, _itemView, true);
        }
    }
}