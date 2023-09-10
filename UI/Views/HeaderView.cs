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
        private bool _headerUpdate = false;

        public HeaderView(View headerRootView)
        {
            Root = headerRootView;

            Current = new HeaderButton();
            _cached = new HeaderButton();

            if (Root.FindChild("Empty1", out Button empty1)) { empty1.SetAllGfx(TextureId.HeaderEmpty1); };
         
            if (Root.FindChild("Name", out Button name)) { SetButtonParams(name, OrderMode.Name,TextureId.HeaderName); }

            if (Root.FindChild("Id", out Button id)) { SetButtonParams(id, OrderMode.Id, TextureId.HeaderId); }

            if (Root.FindChild("Ql", out Button ql)) { SetButtonParams(ql, OrderMode.Ql, TextureId.HeaderQl); }

            if (Root.FindChild("Location", out Button location)) { SetButtonParams(location, OrderMode.Location, TextureId.HeaderLocation); }

            if (Root.FindChild("Character", out Button character)) { SetButtonParams(character, OrderMode.Character, TextureId.HeaderCharacter); }

            if (Root.FindChild("Empty2", out Button empty2)) { empty2.SetAllGfx(TextureId.HeaderEmpty2); };

        }

        public bool TriggerRefresh()
        {
            if (!Main.MainWindow.SearchInProgress)
                return false;

            if (!FilterModeUpdate())
                return false;

            if (Main.MainWindow.SearchResults == null)
                return false;

            return true;
        }

        private void SetButtonParams(Button button, OrderMode mode, int gfxId)
        {
            button.Tag = mode;
            button.SetAllGfx(gfxId);
            button.Clicked = OnButtonClick;
        }

        private void OnButtonClick(object sender, ButtonBase e)
        {
            var tag = (OrderMode)e.Tag;

            if (Current.Mode == tag)
            {
                Current.SwitchDirection();
            }
            else
            {
                Current.Mode = (OrderMode)e.Tag;
                Current.Direction = Direction.Ascending;
            }

            Midi.Play("Click");
            _headerUpdate = true;
        }

        internal bool OnHeaderChange()
        {
            if (!_headerUpdate)
                return false;

            _headerUpdate = false;

            return true;
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
