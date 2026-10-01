using System.Collections.Generic;
using BepInEx.Bootstrap;
using UnityEngine;

namespace SuitWardrobeSimple
{
    internal static class RackTrigger
    {
        private const string OpenTip = "Open wardrobe : [E]";

        private static InteractTrigger _trigger;

        internal static void SetBusy(string status)
        {
            if (_trigger == null)
                return;

            _trigger.interactable = status == null;
            _trigger.disabledHoverTip = status ?? string.Empty;
        }

        internal static void Attach(StartOfRound round)
        {
            Transform anchor = round.rightmostSuitPosition;
            if (anchor == null)
            {
                Plugin.mlg.LogWarning("No suit rack found; the wardrobe cannot be opened this lobby.");
                return;
            }

            var spot = new GameObject("SuitWardrobeSimpleTrigger");
            spot.SetActive(false);
            spot.transform.SetParent(anchor, false);
            spot.transform.localPosition = Vector3.zero;
            spot.transform.localRotation = Quaternion.identity;
            spot.tag = "InteractTrigger";
            spot.layer = LayerMask.NameToLayer("InteractableObject");

            // Trigger only, so players and items can still pass through.
            var box = spot.AddComponent<BoxCollider>();
            box.isTrigger = true;
            spot.AddComponent<RackFit>();

            var trigger = spot.AddComponent<InteractTrigger>();
            trigger.interactable = true;
            trigger.oneHandedItemAllowed = true;
            trigger.twoHandedItemAllowed = true;
            trigger.holdInteraction = false;
            trigger.interactCooldown = false;
            trigger.onInteract = new InteractEvent();
            trigger.onInteractEarly = new InteractEvent();
            trigger.onCancelAnimation = new InteractEvent();
            trigger.onStopInteract = new InteractEvent();
            trigger.holdingInteractEvent = new InteractEventFloat();
            trigger.hoverTip = OpenTip;
            trigger.disabledHoverTip = string.Empty;
            trigger.hoverIcon = trigger.disabledHoverIcon = HoverIcon();
            trigger.onInteract.AddListener(player => WardrobeMenu.Open(player));
            _trigger = trigger;

            spot.SetActive(true);
        }

        private static Sprite HoverIcon()
        {
            Terminal terminal = Object.FindObjectOfType<Terminal>();
            InteractTrigger trigger = terminal != null ? terminal.GetComponent<InteractTrigger>() : null;
            return trigger != null ? trigger.hoverIcon : null;
        }
    }

    // Suit hitboxes are turned off so looking at a suit hits the wardrobe box instead.
    internal sealed class RackFit : MonoBehaviour
    {
        private const float RailLength = 2.07f;

        private const string RailMesh = "NurbsPath.002";

        // Keeps the last shown suit from clipping into the end post.
        private const float EndClearance = 0.15f;

        private const float Margin = 0.05f;

        private const float SuitThickness = 0.12f;

        private const float SuitWidth = 0.3f;
        private const float Interval = 2f;

        private static RackFit _instance;
        private static readonly bool Paged = Chainloader.PluginInfos.ContainsKey("TooManySuits");

        private const string PageLabel = "TooManySuitsPageLabel";
        private const float ButtonGap = 0.02f;

        private BoxCollider _box;
        private Renderer _rail;
        private bool _railWarned;
        private GameObject _pageLabel;
        private bool _pageLabelHidden;

        private const string Boots = "ScavengerModelSuitParts";
        private GameObject _boots;
        private bool _bootsHidden;
        private float _nextFit;

        private readonly HashSet<Renderer> _hidden = new HashSet<Renderer>();

        private void Awake()
        {
            _box = GetComponent<BoxCollider>();
        }

        private void OnEnable()
        {
            _instance = this;
            Settings.HidePageButtons.SettingChanged += OnSettingChanged;
            Settings.ShowBoots.SettingChanged += OnSettingChanged;
        }

        private void OnDisable()
        {
            Settings.HidePageButtons.SettingChanged -= OnSettingChanged;
            Settings.ShowBoots.SettingChanged -= OnSettingChanged;
            if (_instance == this)
                _instance = null;
        }

        private void OnSettingChanged(object sender, System.EventArgs e) => Refresh();

        // The box is refit next frame, AutoParentToShip moves the suits in LateUpdate.
        internal static void Refresh()
        {
            if (_instance == null)
                return;

            _instance.Quiet();
            _instance.Dress();
            _instance._nextFit = 0f;
        }

        private void Update()
        {
            if (Time.time < _nextFit)
                return;

            _nextFit = Time.time + Interval;
            Quiet();
            Dress();
            Fit();
        }

        private void Quiet()
        {
            _hidden.RemoveWhere(mesh => mesh == null);
            foreach (UnlockableSuit suit in Hangers())
            {
                foreach (Collider collider in suit.GetComponentsInChildren<Collider>())
                    collider.enabled = false;

                InteractTrigger trigger = suit.GetComponent<InteractTrigger>();
                if (trigger != null)
                {
                    trigger.enabled = false;
                    trigger.interactable = false;
                }
            }
        }

        private void HideIfPastRail(UnlockableSuit suit, float railEnd)
        {
            bool past = transform.InverseTransformPoint(suit.transform.position).z > railEnd - EndClearance;
            foreach (Renderer mesh in suit.GetComponentsInChildren<Renderer>(true))
            {
                if (past)
                {
                    if (mesh.enabled)
                    {
                        mesh.enabled = false;
                        _hidden.Add(mesh);
                    }
                }
                else if (_hidden.Remove(mesh))
                {
                    mesh.enabled = true;
                }
            }
        }

        private void Fit()
        {
            // Start with the rail, so an empty rack still opens the wardrobe.
            (float railStart, float railEnd) = RailSpan();
            var min = new Vector3(-0.15f, -0.1f, railStart);
            var max = new Vector3(0.15f, 0.3f, railEnd);

            foreach (UnlockableSuit suit in Hangers())
            {
                if (!Paged)
                    HideIfPastRail(suit, railEnd);

                // Skip hidden suits (past the rail or on another page),
                // they'd stretch the box into the wall.
                Renderer mesh = suit.suitRenderer;
                if (mesh == null || !mesh.enabled || mesh.bounds.size == Vector3.zero)
                    continue;

                // Position comes from the hanger, the mesh bounds are only used for the height.
                // The bounds include the arms, which would make the box too wide.
                Vector3 hanger = transform.InverseTransformPoint(suit.transform.position);
                float bottom = hanger.y;
                float top = hanger.y;
                foreach (Vector3 corner in Corners(mesh.bounds))
                {
                    float height = transform.InverseTransformPoint(corner).y;
                    bottom = Mathf.Min(bottom, height);
                    top = Mathf.Max(top, height);
                }

                min = Vector3.Min(min, new Vector3(hanger.x - SuitWidth, bottom, hanger.z - SuitThickness));
                max = Vector3.Max(max, new Vector3(hanger.x + SuitWidth, top, hanger.z + SuitThickness));
            }

            min -= Vector3.one * Margin;
            max += Vector3.one * Margin;
            if (Paged && !Settings.HidePageButtons.Value)
                max.y = Mathf.Min(max.y, BelowPageButtons());
            _box.center = (min + max) * 0.5f;
            _box.size = max - min;
        }

        private (float start, float end) RailSpan()
        {
            if (_rail == null)
            {
                Transform rack = transform.parent != null ? transform.parent.parent : null;
                Transform rail = rack != null ? rack.Find(RailMesh) : null;
                _rail = rail != null ? rail.GetComponent<Renderer>() : null;
            }
            if (_rail == null)
            {
                if (!_railWarned)
                {
                    Plugin.mlg.LogWarning($"Suit rack rail ({RailMesh}) not found; assuming the vanilla length.");
                    _railWarned = true;
                }
                return (0f, RailLength);
            }

            float start = float.MaxValue;
            float end = float.MinValue;
            foreach (Vector3 corner in Corners(_rail.bounds))
            {
                float along = transform.InverseTransformPoint(corner).z;
                start = Mathf.Min(start, along);
                end = Mathf.Max(end, along);
            }
            return (start, end);
        }

        private void Dress()
        {
            if (Paged)
            {
                if (_pageLabel == null)
                    _pageLabel = GameObject.Find(PageLabel);
                _pageLabelHidden = Show(_pageLabel, !Settings.HidePageButtons.Value, _pageLabelHidden);
            }

            if (_boots == null)
                _boots = GameObject.Find(Boots);
            _bootsHidden = Show(_boots, Settings.ShowBoots.Value, _bootsHidden);
        }

        // Only renderers and colliders, TooManySuits' own updates run on the page label.
        private static bool Show(GameObject thing, bool show, bool hidden)
        {
            if (thing == null || (show && !hidden))
                return false;

            foreach (Renderer mesh in thing.GetComponentsInChildren<Renderer>(true))
                mesh.enabled = show;
            foreach (Collider collider in thing.GetComponentsInChildren<Collider>(true))
                collider.enabled = show;
            return !show;
        }

        // The box has to end below the TooManySuits arrows or they can't be clicked.
        private float BelowPageButtons()
        {
            if (_pageLabel == null)
                _pageLabel = GameObject.Find(PageLabel);
            if (_pageLabel == null)
                return float.MaxValue;

            float bottom = float.MaxValue;
            foreach (BoxCollider button in _pageLabel.GetComponentsInChildren<BoxCollider>(true))
            {
                Vector3 half = button.size * 0.5f;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = button.center + Vector3.Scale(half, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    float height = transform.InverseTransformPoint(button.transform.TransformPoint(corner)).y;
                    bottom = Mathf.Min(bottom, height);
                }
            }
            return bottom - ButtonGap;
        }

        private static IEnumerable<UnlockableSuit> Hangers()
        {
            foreach (UnlockableSuit suit in FindObjectsOfType<UnlockableSuit>())
            {
                if (suit != null && suit.IsSpawned)
                    yield return suit;
            }
        }

        private static IEnumerable<Vector3> Corners(Bounds bounds)
        {
            Vector3 c = bounds.center;
            Vector3 e = bounds.extents;
            for (int i = 0; i < 8; i++)
                yield return c + Vector3.Scale(e, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
        }
    }
}
