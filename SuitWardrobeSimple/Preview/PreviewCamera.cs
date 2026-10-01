using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace SuitWardrobeSimple
{
    // Renders only layers 23 and 17 (where MRAPI puts the local model), not the ship.
    internal sealed class PreviewCamera
    {
        private const int ModelLayer = 23;
        private const int NoPostModelLayer = 17;

        private const int Width = 512;
        private const int Height = 704;

        internal const float Aspect = (float)Width / Height;

        private const float FieldOfView = 40f;

        private const float Headroom = 1.12f;

        private const float MinZoom = 0.45f;
        private const float MaxZoom = 1.6f;

        private GameObject _rig;
        private Camera _camera;
        private Light _light;
        private RenderTexture _texture;
        private PlayerControllerB _player;

        private SkinnedMeshRenderer _body;
        private int _bodyLayer;
        private ShadowCastingMode _bodyShadows;
        private bool _bodyMoved;

        private const int ArmsLayer = 4;

        internal bool HideOwnArms;

        private bool _armsHidden;
        private bool _armsWereShown;
        private int _gameplayMask;

        internal float Yaw;

        private float _zoom = 1f;

        internal RenderTexture Texture => _texture;

        internal int FramesDrawn { get; private set; }

        internal void Start(PlayerControllerB player)
        {
            _player = player;
            ResetView();
            if (_rig == null)
                Build(player);

            Place();
            FramesDrawn = 0;
            _rig.SetActive(true);
            RenderPipelineManager.beginCameraRendering += BeforeRender;
            RenderPipelineManager.endCameraRendering += AfterRender;
        }

        internal void Stop()
        {
            RenderPipelineManager.beginCameraRendering -= BeforeRender;
            RenderPipelineManager.endCameraRendering -= AfterRender;
            TakeDown();
            ShowArms();
            if (_rig != null)
                _rig.SetActive(false);
            _player = null;
        }

        internal void Dispose()
        {
            Stop();
            if (_rig != null)
                Object.Destroy(_rig);
            if (_texture != null)
                _texture.Release();
            _rig = null;
            _texture = null;
        }

        internal void ResetView()
        {
            Yaw = 0f;
            _zoom = 1f;
        }

        internal void Zoom(float steps)
        {
            _zoom = Mathf.Clamp(_zoom - steps * 0.1f, MinZoom, MaxZoom);
        }

        internal void Place()
        {
            if (_rig == null || _player == null)
                return;

            float eyes = Mathf.Max(1f, _player.gameplayCamera.transform.position.y - _player.transform.position.y);
            float height = eyes + 0.25f;
            float distance = height * Headroom * 0.5f / Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad) * _zoom;

            Vector3 target = _player.transform.position + Vector3.up * (height * 0.5f);
            Quaternion turn = Quaternion.Euler(0f, _player.transform.eulerAngles.y + Yaw, 0f);
            _rig.transform.position = target + turn * Vector3.forward * distance + Vector3.up * (height * 0.08f);
            _rig.transform.LookAt(target);
        }

        internal Texture2D Snapshot(int size)
        {
            const float square = (float)Width / Height;
            RenderTexture small = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(_texture, small, new Vector2(1f, square), new Vector2(0f, 1f - square));

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = small;
            var picture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "SuitWardrobeSimpleThumbnail" };
            picture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            picture.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(small);
            return picture;
        }

        private void Build(PlayerControllerB player)
        {
            _texture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { name = "SuitWardrobeSimplePreview" };
            _texture.Create();

            _rig = new GameObject("SuitWardrobeSimplePreviewCamera");
            _rig.SetActive(false);
            Object.DontDestroyOnLoad(_rig);

            _camera = _rig.AddComponent<Camera>();
            _camera.targetTexture = _texture;
            _camera.cullingMask = (1 << ModelLayer) | (1 << NoPostModelLayer);
            _camera.fieldOfView = FieldOfView;
            _camera.nearClipPlane = 0.05f;
            _camera.farClipPlane = 15f;
            _camera.depth = -10f;

            var data = _rig.AddComponent<HDAdditionalCameraData>();
            HDAdditionalCameraData gameplay = player.gameplayCamera.GetComponent<HDAdditionalCameraData>();
            if (gameplay != null)
                gameplay.CopyTo(data);

            data.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            data.backgroundColorHDR = new Color(0.035f, 0.035f, 0.04f, 1f);

            // No fog, it would wash out the player.
            data.customRenderingSettings = true;
            data.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.AtmosphericScattering] = true;
            data.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.AtmosphericScattering, false);
            data.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.Volumetrics] = true;
            data.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.Volumetrics, false);

            var lightObject = new GameObject("SuitWardrobeSimplePreviewLight") { layer = ModelLayer };
            lightObject.transform.SetParent(_rig.transform, false);
            lightObject.transform.localPosition = new Vector3(0.6f, 0.8f, 0f);
            HDAdditionalLightData light = lightObject.AddHDLight(HDLightTypeAndShape.Point);
            light.SetIntensity(1500f, LightUnit.Lumen);
            light.SetRange(12f);
            light.affectsVolumetric = false;
            light.EnableShadows(false);
            _light = lightObject.GetComponent<Light>();
            _light.enabled = false;
        }

        private void BeforeRender(ScriptableRenderContext context, Camera camera)
        {
            if (camera == _camera)
            {
                SetUp();
                return;
            }

            TakeDown();
            if (HideOwnArms && _player != null && camera == _player.gameplayCamera)
                HideArms();
        }

        private void AfterRender(ScriptableRenderContext context, Camera camera)
        {
            if (camera == _camera)
            {
                FramesDrawn++;
                TakeDown();
            }
            else if (_player != null && camera == _player.gameplayCamera)
            {
                ShowArms();
            }
        }

        // MRAPI puts the arms layer back on the camera every frame, so it's removed per render.
        private void HideArms()
        {
            if (_armsHidden)
                return;

            Camera gameplay = _player.gameplayCamera;
            _gameplayMask = gameplay.cullingMask;
            gameplay.cullingMask &= ~(1 << ArmsLayer);

            _armsWereShown = _player.thisPlayerModelArms != null && _player.thisPlayerModelArms.enabled;
            if (_armsWereShown)
                _player.thisPlayerModelArms.enabled = false;
            _armsHidden = true;
        }

        private void ShowArms()
        {
            if (!_armsHidden)
                return;

            if (_player != null)
            {
                _player.gameplayCamera.cullingMask = _gameplayMask;
                if (_armsWereShown && _player.thisPlayerModelArms != null)
                    _player.thisPlayerModelArms.enabled = true;
            }
            _armsHidden = false;
        }

        private void SetUp()
        {
            if (_player == null)
                return;

            if (_light != null)
                _light.enabled = true;

            if (_bodyMoved)
                return;

            _body = _player.thisPlayerModel;
            if (_body == null || _body.gameObject.layer == ModelLayer || _body.gameObject.layer == NoPostModelLayer)
            {
                // ModelReplacementAPI already put it on a layer this camera sees.
                _body = null;
                return;
            }

            _bodyLayer = _body.gameObject.layer;
            _bodyShadows = _body.shadowCastingMode;
            _body.gameObject.layer = ModelLayer;
            _body.shadowCastingMode = ShadowCastingMode.On;
            _bodyMoved = true;
        }

        private void TakeDown()
        {
            if (_light != null)
                _light.enabled = false;

            if (!_bodyMoved)
                return;

            if (_body != null)
            {
                _body.gameObject.layer = _bodyLayer;
                _body.shadowCastingMode = _bodyShadows;
            }
            _body = null;
            _bodyMoved = false;
        }
    }
}
