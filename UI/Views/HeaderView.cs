using AOSharp.Common.GameData;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MalisItemFinder
{
    public class HeaderView
    {
        public View Root;
        public HeaderButton Current;
        public HeaderButton _cached;

        public HeaderView(View headerRootView)
        {
            Root = View.CreateFromXml($"{Main.PluginDir}\\UI\\Views\\HeaderView.xml");

            Current = new HeaderButton();
            _cached = new HeaderButton();

            if (Root.FindChild("Name", out Button name)) { SetButtonParams(name, OrderMode.Name); }

            if (Root.FindChild("Id", out Button id)) { SetButtonParams(id, OrderMode.Id); }

            if (Root.FindChild("Ql", out Button ql)) { SetButtonParams(ql, OrderMode.Ql); }

            if (Root.FindChild("Location", out Button location)) { SetButtonParams(location, OrderMode.Location); }

            if (Root.FindChild("Character", out Button character)) { SetButtonParams(character, OrderMode.Character); }

            headerRootView.AddChild(Root, true);
        }

        private void SetButtonParams(Button button, OrderMode mode)
        {
            button.Tag = mode;
            button.Clicked = OnButtonClick;
        }

        private void OnButtonClick(object sender, ButtonBase e)
        {
            var tag = (OrderMode)e.Tag;

            if (Current.Mode == tag)
            {
                Current.SwitchDirection();
                Chat.WriteLine(Current.Direction);
            }
            else
            {
                Current.Mode = (OrderMode)e.Tag;
                Current.Direction = Direction.Ascending;
            }

            Chat.WriteLine("Header Button clicked.");

        }

        public bool FilterModeUpdate()
        {
            if (_cached.Mode == Current.Mode && _cached.Direction == Current.Direction)
                return false;

            _cached.Direction = Current.Direction;
            _cached.Mode = Current.Mode;

            return true;
        }
    }

    public enum OrderMode
    {
        Name,
        Id,
        Ql,
        Location,
        Character
    }

    public enum Direction
    {
        Ascending,
        Descending
    }

    public class HeaderButton
    {
        public OrderMode Mode = OrderMode.Name;
        public Direction Direction = Direction.Ascending;

        public void SwitchDirection() => Direction = Direction == Direction.Ascending ? Direction.Descending : Direction.Ascending;
    }
}
