using System.Linq;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Chat;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared.Chat;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.ContentPack;
using Robust.Shared.Input;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client._Stories.CustomPhrases;

public sealed class CustomPhrasesUIController : UIController, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public static readonly BoundKeyFunction OpenMenuFunction = "StoriesOpenCustomPhrases";

    public const int MaxPhrases = 200;
    public const int MaxTagLength = 32;

    private static readonly ResPath ConfigPath = new("/custom_phrases.yml");
    private static readonly TimeSpan SendCooldown = TimeSpan.FromSeconds(0.5);

    private readonly List<CustomPhrase> _phrases = new();
    private bool _loaded;
    private TimeSpan _nextSend;
    private CustomPhrasesWindow? _window;
    private ISawmill _sawmill = default!;

    public event Action? PhrasesChanged;

    private MenuButton? PhrasesButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.CustomPhrasesButton;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("custom_phrases");
    }

    public IReadOnlyList<CustomPhrase> Phrases
    {
        get
        {
            EnsureLoaded();
            return _phrases;
        }
    }

    public void OnStateExited(GameplayState state)
    {
        _window?.Close();
    }

    public void LoadButton()
    {
        if (PhrasesButton == null)
            return;

        PhrasesButton.OnPressed += OnButtonPressed;
    }

    public void UnloadButton()
    {
        if (PhrasesButton == null)
            return;

        PhrasesButton.OnPressed -= OnButtonPressed;
    }

    private void OnButtonPressed(BaseButton.ButtonEventArgs args)
    {
        ToggleWindow();
    }

    public void ToggleWindow()
    {
        if (_window != null)
        {
            _window.Close();
            return;
        }

        _window = new CustomPhrasesWindow(this);
        _window.OnClose += OnWindowClosed;
        _window.OpenCentered();

        if (PhrasesButton != null)
            PhrasesButton.Pressed = true;
    }

    private void OnWindowClosed()
    {
        _window = null;

        if (PhrasesButton != null)
            PhrasesButton.Pressed = false;
    }

    public void Send(CustomPhrase phrase)
    {
        if (_timing.RealTime < _nextSend)
            return;

        _nextSend = _timing.RealTime + SendCooldown;
        UIManager.GetUIController<ChatUIController>().SendText(phrase.Text, ChatSelectChannel.Local);
    }

    public bool TryAdd(string text, List<string> tags)
    {
        EnsureLoaded();

        if (_phrases.Count >= MaxPhrases)
            return false;

        _phrases.Add(new CustomPhrase { Text = text, Tags = tags });
        OnChanged();
        return true;
    }

    public void Update(CustomPhrase phrase, string text, List<string> tags)
    {
        phrase.Text = text;
        phrase.Tags = tags;
        OnChanged();
    }

    public void Remove(CustomPhrase phrase)
    {
        if (_phrases.Remove(phrase))
            OnChanged();
    }

    public static List<string> ParseTags(string input)
    {
        var tags = input.Split(new[] { ' ', ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        return NormalizeTags(tags);
    }

    public static string FormatTags(IEnumerable<string> tags)
    {
        return string.Join(" ", tags.Select(t => $"#{t}"));
    }

    private static List<string> NormalizeTags(IEnumerable<string> tags)
    {
        return tags
            .Select(t => t.Trim().TrimStart('#').ToLowerInvariant())
            .Where(t => t.Length > 0 && t.Length <= MaxTagLength)
            .Distinct()
            .ToList();
    }

    private void OnChanged()
    {
        Save();
        PhrasesChanged?.Invoke();
    }

    private void EnsureLoaded()
    {
        if (_loaded)
            return;

        _loaded = true;

        if (!_res.UserData.Exists(ConfigPath))
            return;

        try
        {
            using var reader = _res.UserData.OpenText(ConfigPath);
            var documents = DataNodeParser.ParseYamlStream(reader);

            if (documents.FirstOrDefault()?.Root is not MappingDataNode root ||
                !root.TryGet("phrases", out var phrasesNode))
            {
                return;
            }

            var phrases = _serialization.Read<List<CustomPhrase>>(phrasesNode, notNullableOverride: true);
            foreach (var phrase in phrases)
            {
                if (string.IsNullOrWhiteSpace(phrase.Text) || _phrases.Count >= MaxPhrases)
                    continue;

                phrase.Tags = NormalizeTags(phrase.Tags);
                _phrases.Add(phrase);
            }
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to load custom phrases: {e}");
        }
    }

    private void Save()
    {
        try
        {
            var mapping = new MappingDataNode();
            mapping.Add("version", new ValueDataNode("1"));
            mapping.Add("phrases", _serialization.WriteValue(_phrases, notNullableOverride: true));

            using var writer = _res.UserData.OpenWriteText(ConfigPath);
            var stream = new YamlStream { new YamlDocument(mapping.ToYaml()) };
            stream.Save(writer, false);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to save custom phrases: {e}");
        }
    }
}
