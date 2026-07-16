using System;
using System.Collections.Generic;
using NativeVoteManagerMS.Shared;
using NativeVoteManagerMS.Shared.Types;
using Sharp.Shared.Objects;
using Wuling.Abstract.Tianshi.Menu;
using Wuling.Abstract.Tianshi.Registry;

namespace MapChooserSharpMS.Modules.MapVote.Services;

/// <summary>
/// MCS-owned vote menu (Wuling world-HUD rendering), passed to NVM as a custom
/// <see cref="IMenuCompat"/>. Curates the flat vote-content list into pages:
///
/// Page 1 — vote description + the abstain ("No Vote") option.
/// Page 2+ — Extend / Don't Change pinned on top (never shuffled), then the
/// map candidates (shuffled per player when RandomShuffle is on).
///
/// The page break is forced by padding page 1 with empty disabled items:
/// Wuling's renderer skips empty-content items visually, but they still
/// occupy page slots.
/// </summary>
internal sealed class McsVoteMenuCompat(IMenu menu, IRegistry registry) : IMenuCompat
{
    private MultiChoiceVoteOptions _voteOptions = null!;
    private readonly Dictionary<int, IMenuInstance> _menuCaches = new();

    public Action<IGameClient, VoteContent> OnChoice { get; set; } = null!;

    public void OpenMenu(IGameClient target)
    {
        var player = registry.GetPlayer(target);
        if (player is null)
            return;

        if (_menuCaches.TryGetValue(target.Slot, out var existing) && !existing.IsClosed)
        {
            if (menu.GetActiveMenu(player) == existing)
                return;

            existing.DisplayToPlayer(player);
            return;
        }

        var instance = BuildMenu(target);
        _menuCaches[target.Slot] = instance;
        instance.DisplayToPlayer(player);
    }

    private IMenuInstance BuildMenu(IGameClient target)
    {
        var instance = menu.CreateMenu();
        instance.Title = _voteOptions.Title.Resolve();

        VoteContent? noVote = null;
        var pinned = new List<VoteContent>();
        var maps = new List<VoteContent>();

        foreach (var content in _voteOptions.VoteContents)
        {
            switch (content.InternalName)
            {
                case MapVoteConstants.NoVoteInternalName:
                    noVote = content;
                    break;

                case MapVoteConstants.ExtendMapInternalName:
                case MapVoteConstants.DontChangeMapInternalName:
                    pinned.Add(content);
                    break;

                default:
                    maps.Add(content);
                    break;
            }
        }

        if (_voteOptions.RandomShuffle)
            ShuffleInPlace(maps);

        // Page 1: description + abstain.
        instance.AddItem(MenuItemStyleFlags.Disabled, _voteOptions.Description.Resolve());
        if (noVote is not null)
            AddVoteItem(instance, target, noVote);

        int itemsPerPage = Math.Max(1, menu.MaxItemsPerPage);
        while (instance.ItemCount % itemsPerPage != 0)
            instance.AddItem(MenuItemStyleFlags.Disabled, string.Empty);

        // Page 2+: pinned specials first, then the (possibly shuffled) maps.
        foreach (var content in pinned)
            AddVoteItem(instance, target, content);
        foreach (var content in maps)
            AddVoteItem(instance, target, content);

        return instance;
    }

    private void AddVoteItem(IMenuInstance instance, IGameClient target, VoteContent content)
    {
        instance.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, content.VisibleName.Resolve(),
            (_, _, _, _) =>
            {
                OnChoice(target, content);
            });
    }

    private static void ShuffleInPlace(List<VoteContent> contents)
    {
        for (int i = contents.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (contents[i], contents[j]) = (contents[j], contents[i]);
        }
    }

    public void CloseMenu(IGameClient target)
    {
        if (!_menuCaches.TryGetValue(target.Slot, out var instance))
            return;

        if (!instance.IsClosed)
            instance.Close();

        _menuCaches.Remove(target.Slot);
    }

    public void SetVoteOptions(MultiChoiceVoteOptions options)
    {
        _voteOptions = options;
    }

    public void Cleanup()
    {
        foreach (var instance in _menuCaches.Values)
        {
            if (!instance.IsClosed)
                instance.Close();
        }

        _menuCaches.Clear();
    }
}
