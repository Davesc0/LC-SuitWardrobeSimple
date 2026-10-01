using System;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SuitWardrobeSimple
{
    internal sealed class WardrobeMenu : MonoBehaviour
    {
        private const string AllTab = "All";
        private const string FavouritesTab = "Favourites";

        // Laid out for 1920x1080, the canvas scales it to the real screen.
        private const float PanelHeight = 800f;
        private const float Pad = 24f;
        private const float Gap = 14f;
        private const float TabsWidth = 220f;
        private const float ListLeft = Pad + TabsWidth + Gap;
        private const float ListWidth = 860f;
        private const float SideLeft = ListLeft + ListWidth + Pad;
        private const float SideWidth = 468f;
        private const float PanelWidth = SideLeft + SideWidth + Pad;

        private const float ContentTop = 90f;

        private static WardrobeMenu _instance;

        internal static bool IsOpen => _instance != null && _instance._player != null;

        private GameObject _screen;
        private TMP_InputField _search;
        private RectTransform _tabs;
        private RectTransform _grid;
        private ScrollRect _gridScroll;
        private TextMeshProUGUI _count;
        private RawImage _previewImage;
        private TextMeshProUGUI _selectedName;
        private TextMeshProUGUI _selectedSource;
        private Button _wear;
        private TextMeshProUGUI _wearLabel;

        private readonly PreviewCamera _preview = new PreviewCamera();
        private readonly Dictionary<SuitEntry, Image> _tiles = new Dictionary<SuitEntry, Image>();
        private readonly Dictionary<SuitEntry, RawImage> _pictures = new Dictionary<SuitEntry, RawImage>();
        private readonly Dictionary<string, Image> _tabButtons = new Dictionary<string, Image>();
        private readonly Dictionary<string, TextMeshProUGUI> _tabCounts = new Dictionary<string, TextMeshProUGUI>();
        private List<SuitEntry> _suits = new List<SuitEntry>();
        private string _tab = AllTab;
        private SuitEntry _selected;

        private PlayerControllerB _player;

        private readonly Queue<SuitEntry> _toPhotograph = new Queue<SuitEntry>();

        private PlayerControllerB _subject;
        private bool _inBackground;
        private float _nextCalmCheck;
        private float _nextSuitCheck;
        private string _lastWait;
        private SuitEntry _posing;
        private int _posedAtFrame;
        private TextMeshProUGUI _photoProgress;
        private int _photoTotal;

        private bool _wasInSpecialMenu;
        private bool _wasTypingChat;
        private bool _wasMoveDisabled;

        internal static void Open(PlayerControllerB player)
        {
            if (player == null || player != GameNetworkManager.Instance?.localPlayerController || IsOpen)
                return;
            if (player.isPlayerDead || player.inTerminalMenu || player.quickMenuManager.isMenuOpen)
                return;

            // The rack is disabled while thumbnails are taken, this only catches a press
            // that lands right as they start.
            Prepare();
            if (_instance._inBackground)
                return;
            _instance.Show(player);
        }

        internal static void Prepare()
        {
            if (_instance == null)
                _instance = Create();
        }

        internal static void Dispose()
        {
            if (_instance == null)
                return;

            _instance.Hide(wear: false);
            _instance.StopBackground();
            _instance._preview.Dispose();
            Destroy(_instance.gameObject);
            _instance = null;
        }

        private static WardrobeMenu Create()
        {
            var go = new GameObject("SuitWardrobeSimpleMenu");
            WardrobeMenu menu = go.AddComponent<WardrobeMenu>();
            menu.Build();
            return menu;
        }

        private void Show(PlayerControllerB player)
        {
            _player = player;
            _suits = SuitCatalog.Owned();
            TryOn.Begin(player);
            _selected = _suits.FirstOrDefault(suit => suit.Id == player.currentSuitID);

            string remembered = Saved.LastTab;
            _tab = TabNames().Contains(remembered) ? remembered : AllTab;
            _search.SetTextWithoutNotify(string.Empty);

            BuildTabs();
            Refresh();
            ShowSelected();

            _preview.Start(player);
            _previewImage.texture = _preview.Texture;
            _screen.SetActive(true);
            LockPlayer();
            _subject = player;
            QueuePhotos(_suits.Where(suit => !Thumbnails.Has(suit.Name)));
        }

        private void Hide(bool wear)
        {
            if (_player == null)
                return;

            StopPhotos(showSelected: false);
            PlayerControllerB player = _player;
            _player = null;
            _subject = null;

            if (wear && _selected != null && _selected.Id != TryOn.Original)
                TryOn.Wear(player, _selected.Hanger);
            else
                TryOn.Cancel(player);

            Saved.LastTab = _tab;
            _preview.Stop();
            _screen.SetActive(false);
            UnlockPlayer(player);
        }

        private void Update()
        {
            if (_player == null)
            {
                Background();
                return;
            }

            if (_player.isPlayerDead || !_player.isPlayerControlled)
            {
                Hide(wear: false);
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Hide(wear: false);
                return;
            }

            // The chat's Enter handler resets isTypingChat, so the locks are set again every frame.
            HoldLocks();
            TakePhotos();
        }

        private void LateUpdate()
        {
            if (_subject != null)
                _preview.Place();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        // ---- Locking the player -----------------------------------------------------------

        // isTypingChat also stops Escape from opening the pause menu.
        private void LockPlayer()
        {
            _wasInSpecialMenu = _player.inSpecialMenu;
            _wasTypingChat = _player.isTypingChat;
            _wasMoveDisabled = _player.disableMoveInput;
            HoldLocks();

            _player.cursorIcon.enabled = false;
            _player.cursorTip.text = string.Empty;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (HUDManager.Instance != null)
                HUDManager.Instance.SetMouseCursorSprite(HUDManager.Instance.defaultCursorTex);
        }

        private void HoldLocks()
        {
            _player.inSpecialMenu = true;
            _player.isTypingChat = true;
            _player.disableMoveInput = true;
        }

        private void UnlockPlayer(PlayerControllerB player)
        {
            player.inSpecialMenu = _wasInSpecialMenu;
            player.isTypingChat = _wasTypingChat;
            player.disableMoveInput = _wasMoveDisabled;

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // ---- Suit list --------------------------------------------------------------------

        private IEnumerable<string> TabNames()
        {
            yield return AllTab;
            yield return FavouritesTab;

            // Vanilla first, then the packs in ABC order, and unknown suits last.
            IEnumerable<string> sources = _suits.Select(suit => suit.Source).Distinct().ToList();
            if (sources.Contains(SuitSources.Vanilla))
                yield return SuitSources.Vanilla;
            foreach (string source in sources.Where(source => source != SuitSources.Vanilla && source != SuitSources.Other)
                         .OrderBy(source => source, StringComparer.OrdinalIgnoreCase))
                yield return source;
            if (sources.Contains(SuitSources.Other))
                yield return SuitSources.Other;
        }

        private void BuildTabs()
        {
            foreach (Transform child in _tabs)
                Destroy(child.gameObject);
            _tabButtons.Clear();
            _tabCounts.Clear();

            foreach (string tab in TabNames())
            {
                string name = tab;
                Button button = Ui.Button(_tabs, "Tab " + name, null, 0f, Ui.Raised, () => SelectTab(name));
                button.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;

                TextMeshProUGUI label = Ui.Label(button.transform, "Name", name, 20f);
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(12f, 0f);
                label.rectTransform.offsetMax = new Vector2(-46f, 0f);

                TextMeshProUGUI count = Ui.Label(button.transform, "Count", string.Empty, 18f, TextAlignmentOptions.MidlineRight);
                count.color = Ui.Muted;
                count.rectTransform.anchorMin = new Vector2(1f, 0f);
                count.rectTransform.anchorMax = new Vector2(1f, 1f);
                count.rectTransform.pivot = new Vector2(1f, 0.5f);
                count.rectTransform.anchoredPosition = new Vector2(-10f, 0f);
                count.rectTransform.sizeDelta = new Vector2(40f, 0f);

                _tabButtons[name] = (Image)button.targetGraphic;
                _tabCounts[name] = count;
            }
            CountTabs();
            PaintTabs();
        }

        private void CountTabs()
        {
            foreach (KeyValuePair<string, TextMeshProUGUI> entry in _tabCounts)
            {
                string tab = entry.Key;
                int count = tab == AllTab ? _suits.Count
                    : tab == FavouritesTab ? _suits.Count(suit => Saved.IsFavourite(suit.Name))
                    : _suits.Count(suit => suit.Source == tab);
                entry.Value.text = count.ToString();
            }
        }

        private void SelectTab(string tab)
        {
            _tab = tab;
            PaintTabs();
            Refresh();
        }

        private void PaintTabs()
        {
            foreach (KeyValuePair<string, Image> entry in _tabButtons)
                entry.Value.color = entry.Key == _tab ? Ui.AccentDark : Ui.Raised;
        }

        private IEnumerable<SuitEntry> Visible()
        {
            string query = _search.text.Trim();
            return _suits.Where(suit =>
                (_tab == AllTab
                 || (_tab == FavouritesTab && Saved.IsFavourite(suit.Name))
                 || suit.Source == _tab)
                && (query.Length == 0 || suit.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private void Refresh()
        {
            foreach (Transform child in _grid)
                Destroy(child.gameObject);
            _tiles.Clear();
            _pictures.Clear();

            List<SuitEntry> visible = Visible().ToList();
            foreach (SuitEntry suit in visible)
                _tiles[suit] = AddTile(suit);

            _count.text = visible.Count == 1 ? "1 suit" : $"{visible.Count} suits";
            if (visible.Count == 0)
                _count.text = _tab == FavouritesTab && _search.text.Length == 0
                    ? "No favourites yet: star a suit to keep it here."
                    : "No suits match.";

            _gridScroll.verticalNormalizedPosition = 1f;
            PaintTiles();
        }

        private Image AddTile(SuitEntry suit)
        {
            Button tile = Ui.Button(_grid, "Suit " + suit.Name, null, 0f, Ui.Raised, () => Select(suit));
            Transform root = tile.transform;

            RawImage thumbnail = Ui.Rect("Thumbnail", root).gameObject.AddComponent<RawImage>();
            thumbnail.raycastTarget = false;
            ShowPicture(thumbnail, suit);
            _pictures[suit] = thumbnail;
            RectTransform picture = thumbnail.rectTransform;
            picture.anchorMin = picture.anchorMax = new Vector2(0f, 0.5f);
            picture.pivot = new Vector2(0f, 0.5f);
            picture.anchoredPosition = new Vector2(8f, 0f);
            picture.sizeDelta = new Vector2(58f, 58f);

            TextMeshProUGUI name = Ui.Label(root, "Name", suit.Name, 22f);
            name.rectTransform.anchorMin = new Vector2(0f, 0.45f);
            name.rectTransform.anchorMax = new Vector2(1f, 1f);
            name.rectTransform.offsetMin = new Vector2(76f, 0f);
            name.rectTransform.offsetMax = new Vector2(-44f, -6f);

            TextMeshProUGUI note = Ui.Label(root, "Source", suit.Id == TryOn.Original ? "Wearing" : suit.Source, 16f);
            note.color = suit.Id == TryOn.Original ? Ui.Accent : Ui.Muted;
            note.rectTransform.anchorMin = new Vector2(0f, 0f);
            note.rectTransform.anchorMax = new Vector2(1f, 0.45f);
            note.rectTransform.offsetMin = new Vector2(76f, 6f);
            note.rectTransform.offsetMax = new Vector2(-44f, 0f);

            // The game's font has no star glyph, so it's a plain asterisk.
            Button star = Ui.Button(root, "Favourite", "*", 30f, Ui.Inset, null);
            RectTransform starRect = (RectTransform)star.transform;
            starRect.anchorMin = starRect.anchorMax = new Vector2(1f, 0.5f);
            starRect.pivot = new Vector2(1f, 0.5f);
            starRect.anchoredPosition = new Vector2(-8f, 0f);
            starRect.sizeDelta = new Vector2(32f, 32f);
            TextMeshProUGUI starLabel = star.GetComponentInChildren<TextMeshProUGUI>();
            PaintStar(starLabel, suit);
            star.onClick.AddListener(() => ToggleFavourite(suit, starLabel));

            return (Image)tile.targetGraphic;
        }

        private static void PaintStar(TextMeshProUGUI label, SuitEntry suit)
        {
            label.color = Saved.IsFavourite(suit.Name) ? Ui.Accent : new Color(0.35f, 0.32f, 0.3f, 1f);
        }

        private void ToggleFavourite(SuitEntry suit, TextMeshProUGUI starLabel)
        {
            Saved.ToggleFavourite(suit.Name);
            CountTabs();
            if (_tab == FavouritesTab)
                Refresh();
            else
                PaintStar(starLabel, suit);
        }

        private void PaintTiles()
        {
            foreach (KeyValuePair<SuitEntry, Image> entry in _tiles)
                entry.Value.color = entry.Key == _selected ? Ui.AccentDark : Ui.Raised;
        }

        // ---- Selected suit ----------------------------------------------------------------

        private void Select(SuitEntry suit)
        {
            // Picking a suit wins over the thumbnails, the rest are taken next time.
            StopPhotos(showSelected: false);
            _selected = suit;
            TryOn.Show(_player, suit.Id);
            PaintTiles();
            ShowSelected();
        }

        private void ShowSelected()
        {
            bool wearing = _selected == null || _selected.Id == TryOn.Original;
            _selectedName.text = _selected?.Name ?? string.Empty;
            _selectedSource.text = _selected?.Source ?? string.Empty;
            _wear.interactable = !wearing;
            _wearLabel.text = wearing ? "Wearing" : "Wear";
        }

        // ---- Thumbnails -------------------------------------------------------------------

        // Frames to wait: exposure settling, then MRAPI swapping the model.
        private const int WarmUpFrames = 12;
        private const int PoseFrames = 6;

        private static void ShowPicture(RawImage image, SuitEntry suit)
        {
            Texture2D picture = Thumbnails.Get(suit.Name);
            image.texture = picture;
            image.color = picture != null ? Color.white : Ui.Inset;
        }

        private void QueuePhotos(IEnumerable<SuitEntry> suits)
        {
            _toPhotograph.Clear();
            foreach (SuitEntry suit in suits)
                _toPhotograph.Enqueue(suit);

            _photoTotal = _toPhotograph.Count;
            _posing = null;
            UpdatePhotoProgress();
        }

        private bool TakePhotos()
        {
            if (_posing == null)
            {
                if (_toPhotograph.Count == 0)
                    return false;
                if (_preview.FramesDrawn < WarmUpFrames)
                    return true;

                _posing = _toPhotograph.Dequeue();
                _preview.ResetView();
                TryOn.Show(_subject, _posing.Id);
                _posedAtFrame = _preview.FramesDrawn;
                return true;
            }

            if (_preview.FramesDrawn - _posedAtFrame < PoseFrames)
                return true;

            Thumbnails.Store(_posing.Name, _preview.Snapshot(Thumbnails.Size));
            if (_pictures.TryGetValue(_posing, out RawImage image))
                ShowPicture(image, _posing);

            _posing = null;
            UpdatePhotoProgress();
            if (_inBackground)
                ShowBackgroundProgress();
            if (_toPhotograph.Count > 0)
                return true;

            if (_player != null)
                StopPhotos(showSelected: true);
            return false;
        }

        private void StopPhotos(bool showSelected)
        {
            _toPhotograph.Clear();
            _posing = null;
            UpdatePhotoProgress();

            if (showSelected && _player != null)
                TryOn.Show(_player, _selected?.Id ?? TryOn.Original);
        }

        // ---- Thumbnails in the background -------------------------------------------------

        private const float CalmInterval = 1f;
        private const float SuitInterval = 5f;

        private void Background()
        {
            if (_inBackground)
            {
                string busy = WaitReason(_subject);
                if (busy != null)
                {
                    Plugin.mlg.LogInfo($"Wardrobe pictures paused: {busy}. The rest are taken later.");
                    StopBackground();
                }
                else if (!TakePhotos())
                {
                    Plugin.mlg.LogInfo("Wardrobe pictures done.");
                    StopBackground();
                }
                return;
            }

            if (Time.time < _nextCalmCheck)
                return;
            _nextCalmCheck = Time.time + CalmInterval;

            PlayerControllerB player = GameNetworkManager.Instance?.localPlayerController;
            string wait = WaitReason(player);
            if (wait != _lastWait && wait != null)
                Plugin.mlg.LogDebug($"Wardrobe pictures waiting: {wait}.");
            _lastWait = wait;
            if (wait != null || Time.time < _nextSuitCheck)
                return;
            _nextSuitCheck = Time.time + SuitInterval;

            List<SuitEntry> missing = SuitCatalog.Owned().Where(suit => !Thumbnails.Has(suit.Name)).ToList();
            if (missing.Count == 0)
                return;

            Plugin.mlg.LogInfo($"Taking pictures of {missing.Count} suit(s) for the wardrobe.");
            _inBackground = true;
            _subject = player;
            TryOn.Begin(player);
            _preview.HideOwnArms = true;
            _preview.Start(player);
            QueuePhotos(missing);
            ShowBackgroundProgress();
        }

        private void ShowBackgroundProgress()
        {
            int done = _photoTotal - _toPhotograph.Count - (_posing != null ? 1 : 0);
            RackTrigger.SetBusy($"Preparing wardrobe... {done}/{_photoTotal}");
        }

        private static string WaitReason(PlayerControllerB player)
        {
            StartOfRound round = StartOfRound.Instance;
            if (player == null || round == null)
                return "no local player yet";
            if (!player.isPlayerControlled || player.isPlayerDead)
                return "player not in control";
            if (player.inTerminalMenu || player.inSpecialInteractAnimation)
                return "player busy";
            if (!round.inShipPhase || round.travellingToNewLevel || round.shipIsLeaving)
                return "not in orbit";
            return null;
        }

        private void StopBackground()
        {
            if (!_inBackground)
                return;

            _inBackground = false;
            StopPhotos(showSelected: false);
            TryOn.Cancel(_subject);
            _preview.Stop();
            _preview.HideOwnArms = false;
            _subject = null;
            RackTrigger.SetBusy(null);
        }

        private void UpdatePhotoProgress()
        {
            int left = _toPhotograph.Count + (_posing != null ? 1 : 0);
            _photoProgress.text = left == 0
                ? string.Empty
                : $"Taking pictures {_photoTotal - left + 1} / {_photoTotal}\nclick a suit to skip";
        }

        // ---- Building the menu ------------------------------------------------------------

        private void Build()
        {
            DontDestroyOnLoad(gameObject);

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            // Catches clicks outside the panel so they don't reach the game.
            Image backdrop = Ui.Image(transform, "Backdrop", Ui.Backdrop);
            Ui.Fill(backdrop.rectTransform);
            _screen = backdrop.gameObject;

            Image panel = Ui.Image(_screen.transform, "Panel", Ui.Panel);
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            Image stripe = Ui.Image(panel.transform, "Stripe", Ui.Accent);
            Ui.Box(stripe.rectTransform, 0f, 0f, PanelWidth, 4f);

            BuildList(panel.transform);
            BuildSide(panel.transform);

            _screen.SetActive(false);
        }

        private void BuildList(Transform panel)
        {
            TextMeshProUGUI title = Ui.Label(panel, "Title", "WARDROBE", 44f);
            title.color = Ui.Accent;
            Ui.Box(title.rectTransform, Pad, 20f, 360f, 56f);

            _search = Ui.InputField(panel, "Search", "Search suits...", 24f);
            Ui.Box((RectTransform)_search.transform, ListLeft + ListWidth - 360f, 26f, 360f, 46f);
            _search.onValueChanged.AddListener(_ => Refresh());

            // Tabs go in a column, a row ran off the screen with many suit packs.
            _tabs = Ui.Scroll(panel, "Tabs", vertical: true, out ScrollRect tabScroll);
            Ui.Column((RectTransform)tabScroll.transform, Pad, ContentTop, TabsWidth, Pad);
            var tabLayout = _tabs.gameObject.AddComponent<VerticalLayoutGroup>();
            tabLayout.spacing = 6f;
            tabLayout.padding = new RectOffset(0, 0, 0, 8);
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = true;
            tabLayout.childForceExpandHeight = false;

            _grid = Ui.Scroll(panel, "Suits", vertical: true, out _gridScroll);
            Ui.Column((RectTransform)_gridScroll.transform, ListLeft, ContentTop, ListWidth, Pad + 32f);
            var grid = _grid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(278f, 76f);
            grid.spacing = new Vector2(8f, 8f);
            grid.padding = new RectOffset(5, 5, 0, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;

            _count = Ui.Label(panel, "Count", string.Empty, 18f);
            _count.color = Ui.Muted;
            _count.rectTransform.anchorMin = _count.rectTransform.anchorMax = new Vector2(0f, 0f);
            _count.rectTransform.pivot = new Vector2(0f, 0f);
            _count.rectTransform.anchoredPosition = new Vector2(ListLeft, Pad - 4f);
            _count.rectTransform.sizeDelta = new Vector2(ListWidth, 28f);
        }

        private void BuildSide(Transform panel)
        {
            Image frame = Ui.Image(panel, "Preview", Ui.Inset);
            Ui.Box(frame.rectTransform, SideLeft, 24f, SideWidth, 560f);

            _previewImage = Ui.Rect("View", frame.transform).gameObject.AddComponent<RawImage>();
            Ui.Fill(_previewImage.rectTransform, 4f);
            var aspect = _previewImage.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = PreviewCamera.Aspect;
            _previewImage.gameObject.AddComponent<PreviewDrag>().Camera = _preview;

            TextMeshProUGUI hint = Ui.Label(frame.transform, "Hint", "Drag to turn, scroll to zoom", 16f, TextAlignmentOptions.Center);
            hint.color = Ui.Muted;
            hint.rectTransform.anchorMin = new Vector2(0f, 0f);
            hint.rectTransform.anchorMax = new Vector2(1f, 0f);
            hint.rectTransform.pivot = new Vector2(0.5f, 0f);
            hint.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            hint.rectTransform.sizeDelta = new Vector2(0f, 24f);

            _photoProgress = Ui.Label(frame.transform, "Photos", string.Empty, 18f, TextAlignmentOptions.Center);
            _photoProgress.color = Ui.Accent;
            _photoProgress.rectTransform.anchorMin = new Vector2(0f, 1f);
            _photoProgress.rectTransform.anchorMax = new Vector2(1f, 1f);
            _photoProgress.rectTransform.pivot = new Vector2(0.5f, 1f);
            _photoProgress.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            _photoProgress.rectTransform.sizeDelta = new Vector2(0f, 48f);

            _selectedName = Ui.Label(panel, "Selected", string.Empty, 32f);
            Ui.Box(_selectedName.rectTransform, SideLeft, 596f, SideWidth, 42f);
            _selectedSource = Ui.Label(panel, "SelectedSource", string.Empty, 20f);
            _selectedSource.color = Ui.Muted;
            Ui.Box(_selectedSource.rectTransform, SideLeft, 638f, SideWidth, 28f);

            _wear = Ui.Button(panel, "Wear", "Wear", 30f, Ui.AccentDark, () => Hide(wear: true));
            _wearLabel = _wear.GetComponentInChildren<TextMeshProUGUI>();
            Ui.Box((RectTransform)_wear.transform, SideLeft, PanelHeight - Pad - 64f, SideWidth - 180f, 64f);

            Button close = Ui.Button(panel, "Close", "Close", 26f, Ui.Raised, () => Hide(wear: false));
            Ui.Box((RectTransform)close.transform, SideLeft + SideWidth - 168f, PanelHeight - Pad - 64f, 168f, 64f);
        }
    }

    internal sealed class PreviewDrag : MonoBehaviour, IDragHandler, IScrollHandler
    {
        internal PreviewCamera Camera;

        public void OnDrag(PointerEventData eventData)
        {
            if (Camera != null)
                Camera.Yaw += eventData.delta.x * 0.5f;
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (Camera != null && eventData.scrollDelta.y != 0f)
                Camera.Zoom(Mathf.Sign(eventData.scrollDelta.y));
        }
    }
}
