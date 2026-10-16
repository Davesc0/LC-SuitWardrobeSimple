using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.UI;

namespace SuitWardrobeSimple
{
    internal sealed partial class WardrobeMenu
    {
        private const int Columns = 3;
        private const float TileHeight = 76f;
        private const float TileGap = 8f;

        private const float RepeatDelay = 0.4f;
        private const float RepeatRate = 0.12f;
        private const float TurnSpeed = 150f;
        private const float ZoomSpeed = 6f;

        private List<SuitEntry> _visible = new List<SuitEntry>();
        private readonly Dictionary<SuitEntry, TextMeshProUGUI> _stars = new Dictionary<SuitEntry, TextMeshProUGUI>();
        private TextMeshProUGUI _hint;
        private TextMeshProUGUI _tabHint;
        private TextMeshProUGUI _favouriteHint;
        private Image[] _tabIcons;
        private Image _favouriteIcon;
        private Image _wearIcon;
        private Image _closeIcon;

        private const float HintIcon = 34f;
        private const float ButtonIcon = 38f;

        private bool _usingGamepad;
        private int _openedFrame;
        private Vector2Int _held;
        private float _nextRepeat;

        private void StartInput()
        {
            _openedFrame = Time.frameCount;
            _held = Vector2Int.zero;
            _usingGamepad = StartOfRound.Instance != null && StartOfRound.Instance.localPlayerUsingController;
            ShowHints();
        }

        private bool HandleGamepad()
        {
            Gamepad pad = Gamepad.current;
            WatchDevice(pad);

            // The press that opened the rack still counts as pressed on this frame.
            if (pad == null || Time.frameCount == _openedFrame)
                return false;

            if (pad.buttonEast.wasPressedThisFrame || pad.startButton.wasPressedThisFrame)
            {
                Hide(wear: false);
                return true;
            }
            if (pad.buttonSouth.wasPressedThisFrame && _wear.interactable)
            {
                Hide(wear: true);
                return true;
            }

            if (pad.buttonWest.wasPressedThisFrame && _selected != null && _stars.TryGetValue(_selected, out TextMeshProUGUI star))
                ToggleFavourite(_selected, star);
            if (pad.leftShoulder.wasPressedThisFrame)
                StepTab(-1);
            if (pad.rightShoulder.wasPressedThisFrame)
                StepTab(1);

            Vector2Int step = Direction(pad);
            if (step != Vector2Int.zero)
                MoveSelection(step);

            Vector2 look = pad.rightStick.ReadValue();
            if (Mathf.Abs(look.x) > 0.2f)
                _preview.Yaw += look.x * TurnSpeed * Time.unscaledDeltaTime;
            if (Mathf.Abs(look.y) > 0.2f)
                _preview.Zoom(look.y * ZoomSpeed * Time.unscaledDeltaTime);
            return false;
        }

        // The game stops tracking the device while a menu is open, so it's tracked here.
        private void WatchDevice(Gamepad pad)
        {
            bool gamepad = pad != null
                && (pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame
                    || pad.buttonWest.wasPressedThisFrame || pad.leftShoulder.wasPressedThisFrame
                    || pad.rightShoulder.wasPressedThisFrame || pad.dpad.ReadValue().sqrMagnitude > 0.25f
                    || pad.leftStick.ReadValue().sqrMagnitude > 0.25f || pad.rightStick.ReadValue().sqrMagnitude > 0.25f);
            bool desk = (Mouse.current != null && (Mouse.current.delta.ReadValue().sqrMagnitude > 4f || Mouse.current.leftButton.wasPressedThisFrame))
                || (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame);

            if (gamepad == desk || gamepad == _usingGamepad)
                return;

            _usingGamepad = gamepad;
            if (gamepad && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
            ShowHints();
        }

        // Shows the button's icon while a controller is in use. Without InputUtils there are no
        // icons, and the button's name in brackets is returned to go in front of the text instead.
        private string Prompt(Image icon, string control, string xbox, string playStation)
        {
            Sprite sprite = _usingGamepad && control != null ? PadGlyphs.Get(control) : null;
            icon.sprite = sprite;
            icon.gameObject.SetActive(sprite != null);
            if (!_usingGamepad || control == null || sprite != null)
                return string.Empty;
            return "[" + (Gamepad.current is DualShockGamepad ? playStation : xbox) + "] ";
        }

        // The icon sits at the left of the button and the label moves over to make room.
        private string ButtonPrompt(Image icon, TextMeshProUGUI label, string control, string xbox, string playStation)
        {
            string text = Prompt(icon, control, xbox, playStation);
            bool shown = icon.gameObject.activeSelf;
            RectTransform box = icon.rectTransform;
            box.anchorMin = box.anchorMax = new Vector2(0f, 0.5f);
            box.pivot = new Vector2(0f, 0.5f);
            box.anchoredPosition = new Vector2(14f, 0f);
            box.sizeDelta = new Vector2(ButtonIcon, ButtonIcon);
            label.rectTransform.offsetMin = new Vector2(shown ? ButtonIcon + 18f : 0f, 0f);
            label.rectTransform.offsetMax = new Vector2(shown ? -10f : 0f, 0f);
            return text;
        }

        private void ShowHints()
        {
            _hint.text = _usingGamepad ? "Right stick to turn and zoom" : "Drag to turn, scroll to zoom";

            string tabs = Prompt(_tabIcons[0], "leftShoulder", "LB", "L1") + Prompt(_tabIcons[1], "rightShoulder", "RB", "R1");
            bool tabIcons = _tabIcons[0].gameObject.activeSelf;
            _tabHint.text = _usingGamepad ? tabs + "Tab" : string.Empty;
            _tabHint.rectTransform.anchoredPosition = new Vector2(Pad + (tabIcons ? 2f * (HintIcon + 4f) + 4f : 0f), Pad - 4f);

            // Right-aligned, so the icon goes just left of where the text starts.
            _favouriteHint.text = _usingGamepad ? Prompt(_favouriteIcon, "buttonWest", "X", "Square") + "Favourite" : string.Empty;
            if (!_usingGamepad)
                Prompt(_favouriteIcon, null, null, null);
            float textLeft = ListLeft + ListWidth - _favouriteHint.preferredWidth;
            Ui.Box(_favouriteIcon.rectTransform, textLeft - 6f - HintIcon, PanelHeight - Pad + 4f - 28f, HintIcon, 28f);

            _closeLabel.text = ButtonPrompt(_closeIcon, _closeLabel, "buttonEast", "B", "Circle") + "Close";
            ShowSelected();
        }

        // The button names make the labels longer, so they shrink to fit instead of being cut off.
        private static void FitLabel(TextMeshProUGUI label)
        {
            label.enableAutoSizing = true;
            label.fontSizeMax = label.fontSize;
            label.fontSizeMin = 16f;
            label.overflowMode = TextOverflowModes.Overflow;
        }

        private Vector2Int Direction(Gamepad pad)
        {
            Vector2 stick = pad.leftStick.ReadValue();
            Vector2 dpad = pad.dpad.ReadValue();
            int x = dpad.x > 0.5f || stick.x > 0.6f ? 1 : dpad.x < -0.5f || stick.x < -0.6f ? -1 : 0;
            int y = dpad.y > 0.5f || stick.y > 0.6f ? 1 : dpad.y < -0.5f || stick.y < -0.6f ? -1 : 0;
            var now = new Vector2Int(x, y);

            if (now == Vector2Int.zero || now != _held)
            {
                _held = now;
                _nextRepeat = Time.unscaledTime + RepeatDelay;
                return now;
            }
            if (Time.unscaledTime < _nextRepeat)
                return Vector2Int.zero;

            _nextRepeat = Time.unscaledTime + RepeatRate;
            return now;
        }

        private void MoveSelection(Vector2Int step)
        {
            if (_visible.Count == 0)
                return;

            int index = _visible.IndexOf(_selected);
            int target = index < 0 ? 0 : index + step.x - step.y * Columns;
            if (target < 0)
                return;
            if (target >= _visible.Count)
            {
                int last = _visible.Count - 1;
                if (step.y >= 0 || last / Columns == index / Columns)
                    return;
                target = last;
            }
            if (target == index)
                return;

            Select(_visible[target]);
            ScrollTo(target);
        }

        private void ScrollTo(int index)
        {
            float top = index / Columns * (TileHeight + TileGap);
            float bottom = top + TileHeight + TileGap;
            float view = _gridScroll.viewport.rect.height;

            Vector2 position = _grid.anchoredPosition;
            if (top < position.y)
                position.y = top;
            else if (bottom > position.y + view)
                position.y = bottom - view;
            position.y = Mathf.Clamp(position.y, 0f, Mathf.Max(0f, _grid.rect.height - view));
            _grid.anchoredPosition = position;
        }

        private void StepTab(int direction)
        {
            List<string> tabs = TabNames().ToList();
            int index = (tabs.IndexOf(_tab) + direction + tabs.Count) % tabs.Count;
            SelectTab(tabs[index]);

            ScrollRect scroll = _tabs.GetComponentInParent<ScrollRect>();
            if (scroll != null)
                scroll.verticalNormalizedPosition = tabs.Count > 1 ? 1f - (float)index / (tabs.Count - 1) : 1f;
        }
    }
}
