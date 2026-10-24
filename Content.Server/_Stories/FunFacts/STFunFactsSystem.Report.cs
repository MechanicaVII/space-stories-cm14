using Content.Server._RMC14.LinkAccount;
using Content.Server._RMC14.Rules.DistressSignal;
using Content.Server.GameTicking;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._Stories.FunFacts;

public sealed partial class STFunFactsSystem
{
    private const int MaxFactsPerSide = 5;
    private const int MinDeaths = 3;
    private const int MinLarvaeBurst = 3;
    private const int SuffixVariants = 3;

    private static readonly FunFact[] Facts =
    [
        new("kills", s => s.Kills, 5),
        new("damage-taken", s => s.DamageTaken, 400),
        new("damage-dealt", s => s.DamageDealt, 1000),
        new("friendly-damage", s => s.FriendlyDamage, 200),
        new("crit-time", s => (float) s.CritTime.TotalMinutes, 5),
        new("revives", s => s.Revives, 5, Xenos: false),
        new("times-revived", s => s.TimesRevived, 2, Xenos: false),
        new("evolutions", s => s.Evolutions, 3, Humans: false),
        new("shots", s => s.ShotsFired, 300, Xenos: false),
        new("healed", s => s.Healed, 300, Xenos: false),
        new("surgeries", s => s.Surgeries, 3, Xenos: false),
        new("built", s => s.Built, 10, Xenos: false),
        new("dogtags", s => s.Dogtags, 3, Xenos: false),
        new("grenades", s => s.Grenades, 5, Xenos: false),
        new("ignited", s => s.Ignited, 3),
        new("eaten", s => s.Eaten, 5, Xenos: false),
        new("nested", s => s.Nested, 3, Humans: false),
        new("infected", s => s.Infected, 2, Humans: false, AllowExcludedCaste: true),
        new("resin", s => s.ResinBuilt, 30, Humans: false),
        new("weeds", s => s.WeedsPlanted, 20, Humans: false),
        new("acid", s => s.AcidApplied, 5, Humans: false),
        new("xeno-heals", s => s.XenoHeals, 10, Humans: false),
        new("knockdowns", s => s.Knockdowns, 10),
        new("rescues", s => s.Rescues, 2, Xenos: false),
    ];

    private readonly List<FunFactsEntry> _candidates = new();
    private readonly List<FunFactsPlayer> _playerCandidates = new();

    private void InitializeReport()
    {
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndTextAppend,
            after: [typeof(CMDistressSignalRuleSystem)],
            before: [typeof(LinkAccountSystem)]);
    }

    private void OnRoundEndTextAppend(RoundEndTextAppendEvent ev)
    {
        var entries = CollectEntries();
        var lines = new List<string>();
        AddSideFacts(entries, false, lines);
        AddSideFacts(entries, true, lines);

        if (lines.Count == 0)
            return;

        ev.AddLine(Loc.GetString("st-fun-facts-header"));
        foreach (var line in lines)
        {
            ev.AddLine(line);
        }

        ev.AddLine(string.Empty);
    }

    private List<FunFactsEntry> CollectEntries()
    {
        var now = _timing.CurTime;
        var entries = new List<FunFactsEntry>(_departed);
        var query = EntityQueryEnumerator<STFunFactsTrackerComponent>();
        while (query.MoveNext(out var uid, out var tracker))
        {
            if (tracker.Transferred || tracker.Player is not { } player)
                continue;

            if (tracker.CritStartedAt is { } critStarted)
            {
                tracker.Stats.CritTime += now - critStarted;
                tracker.CritStartedAt = now;
            }

            entries.Add(new FunFactsEntry(player, Name(uid), tracker.Gender, tracker.Xeno, tracker.ExcludedCaste, tracker.Dead, tracker.DeathCause, tracker.Stats));
        }

        return entries;
    }

    private void AddSideFacts(List<FunFactsEntry> entries, bool xeno, List<string> lines)
    {
        var generators = new List<Func<string?>>();
        foreach (var fact in Facts)
        {
            if (xeno ? fact.Xenos : fact.Humans)
                generators.Add(() => TryEntityFact(fact, entries, xeno));
        }

        generators.Add(() => TryPlayerFact("deaths", p => p.Deaths, MinDeaths, xeno));
        if (xeno)
            generators.Add(() => TryPlayerFact("larvae", p => p.LarvaeBurst, MinLarvaeBurst, xeno));

        _random.Shuffle(generators);

        var added = 0;
        foreach (var generator in generators)
        {
            if (generator() is not { } line)
                continue;

            lines.Add(line);
            if (++added >= MaxFactsPerSide)
                break;
        }
    }

    private string? TryEntityFact(FunFact fact, List<FunFactsEntry> entries, bool xeno)
    {
        _candidates.Clear();
        foreach (var entry in entries)
        {
            if (entry.Xeno != xeno ||
                entry.ExcludedCaste && !fact.AllowExcludedCaste ||
                fact.Value(entry.Stats) < fact.Min ||
                !IsConnected(entry.Player))
            {
                continue;
            }

            _candidates.Add(entry);
        }

        if (_candidates.Count == 0)
            return null;

        var picked = _random.Pick(_candidates);
        var stats = picked.Stats;
        var accuracy = stats.ShotsFired > 0 ? Math.Min(100, stats.ShotsHit * 100 / stats.ShotsFired) : 0;
        var message = Loc.GetString($"st-fun-fact-{fact.Id}",
            ("name", FormattedMessage.EscapeText(picked.Name)),
            ("gender", picked.Gender),
            ("value", (int) fact.Value(stats)),
            ("accuracy", accuracy));

        var variant = _random.Next(1, SuffixVariants + 1);
        string suffix;
        if (!picked.Dead)
            suffix = Loc.GetString($"st-fun-facts-survived-{variant}", ("gender", picked.Gender));
        else if (picked.DeathCause is { } cause)
            suffix = Loc.GetString($"st-fun-facts-died-to-{variant}", ("gender", picked.Gender), ("cause", FormattedMessage.EscapeText(cause)));
        else
            suffix = Loc.GetString($"st-fun-facts-died-{variant}", ("gender", picked.Gender));

        return $"{message} {suffix}";
    }

    private string? TryPlayerFact(string id, Func<FunFactsPlayer, int> value, int min, bool xeno)
    {
        _playerCandidates.Clear();
        foreach (var (userId, player) in _players)
        {
            if (player.Xeno != xeno ||
                player.Name.Length == 0 ||
                value(player) < min ||
                !IsConnected(userId))
            {
                continue;
            }

            _playerCandidates.Add(player);
        }

        if (_playerCandidates.Count == 0)
            return null;

        var picked = _random.Pick(_playerCandidates);
        return Loc.GetString($"st-fun-fact-{id}",
            ("name", FormattedMessage.EscapeText(picked.Name)),
            ("gender", picked.Gender),
            ("value", value(picked)));
    }

    private sealed record FunFact(
        string Id,
        Func<STFunFactsStats, float> Value,
        float Min,
        bool Humans = true,
        bool Xenos = true,
        bool AllowExcludedCaste = false);
}
