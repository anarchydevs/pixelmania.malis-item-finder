using AOSharp.Core;
using AOSharp.Core.UI;
using EFDataAccessLibrary.Models;
using MalisItemFinder;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class SearchCriteria
{
    public bool IsActive = false;
    public string Text;

    public SearchCriteria(string text)
    {
        Text = text;
    }

    public virtual bool MeetsReqs(object item) { return true; }
}

public class RangeCriteria : SearchCriteria
{
    private int _min;
    private int _max;

    public RangeCriteria(string text) : base(text)
    {
        string[] rangeParts = text.Split('-');
        IsActive = rangeParts.Length == 2 && int.TryParse(rangeParts[0], out _min) && int.TryParse(rangeParts[1], out _max);
    }

    public override bool MeetsReqs(object item) => (int)item >= _min && (int)item <= _max;
}

public class EqualCriteria : SearchCriteria
{
    public int Result;

    public EqualCriteria(string text) : base(text)
    {
        IsActive = int.TryParse(text, out Result);
    }

    public override bool MeetsReqs(object item) => (int)item == Result;
}

public class MinusCriteria : SearchCriteria
{
    public int Result;

    public MinusCriteria(string text) : base(text)
    {
        string[] minusParts = text.Split('-');
        IsActive = text.EndsWith("-") && minusParts.Length == 2 && int.TryParse(minusParts[0], out Result);
    }

    public override bool MeetsReqs(object item) => Result >= (int)item;
}

public class PlusCriteria : SearchCriteria
{
    public int Result;

    public PlusCriteria(string text) : base(text)
    {
        string[] plusParts = text.Split('+');
        IsActive = text.EndsWith("+") && plusParts.Length == 2 && int.TryParse(plusParts[0], out Result);
    }

    public override bool MeetsReqs(object item) => Result <= (int)item;
}

public class LocationCriteria : SearchCriteria
{
    public ContainerId Result;

    public LocationCriteria(string text) : base(text)
    {
        if (text.Length == 0 || !Enum.TryParse(text[0] + text.Substring(1), true, out ContainerId container))
        {
            IsActive = false;
            return;
        }

        Result = container;
        IsActive = true;
    }

    public override bool MeetsReqs(object item)
    {
        ContainerId contId = (ContainerId)item;

        switch (contId)
        {
            case ContainerId.WeaponPage:
            case ContainerId.ArmorPage:
            case ContainerId.ImplantPage:
            case ContainerId.SocialPage:
            case ContainerId.Inventory:
            case ContainerId.MailTerminal:
            case ContainerId.Bank:
            case ContainerId.Organization:
            case ContainerId.GMI:
                return Result == contId;
            default:
                return Result == ContainerId.Backpack;
        }
    }
}

public enum FilterCriteria
{
    Name,
    Ql,
    Id,
    Location
}