using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PerspectiveCamera
{
    public class Main : AbstractMod, IModSettings
    {
        // The identifier stays the fork's, so keys and settings carry over.
        public override string getName() => "Orbit Cam";
        public override string getDescription() => "A free orbiting perspective camera. Continues H-POPS' Perspective Camera; disable that one when using this.";
        public override string getVersionNumber() => "1.1.0";
        public override string getIdentifier() => "xupisco@PerspectiveCamera";
        public override bool isMultiplayerModeCompatible() => true;
        public override bool isRequiredByAllPlayersInMultiplayerMode() => false;

        private CameraHandler _cameraHandler;

        public Main()
        {
            SetupKeyBinding();
        }

        private HarmonyLib.Harmony _harmony;

        public override void onEnabled()
        {
            PerspectiveCameraSettings.Instance.Load();
            EventManager.Instance.OnStartPlayingPark += InstanceOnOnStartPlayingPark;
            _harmony = new HarmonyLib.Harmony(getIdentifier());
            _harmony.PatchAll(typeof(Main).Assembly);
        }

        private void InstanceOnOnStartPlayingPark()
        {
            GameObject cameraHandlerGo = new GameObject(getIdentifier() + " | Camera Handler");
            _cameraHandler = cameraHandlerGo.AddComponent<CameraHandler>();
            cameraHandlerGo.AddComponent<BehindCameraLabels>();


            _cameraHandler.OrigCamera = Camera.main.gameObject;
            _cameraHandler.PerspectiveCamera = Object.Instantiate(_cameraHandler.OrigCamera);
            _cameraHandler.PerspectiveCamera.name = getIdentifier();
            

            _cameraHandler.OrigCamera.SetActive(false);

            GameObject go = new GameObject();
            Camera cam2 = go.AddComponent<Camera>();

            Camera cam = _cameraHandler.PerspectiveCamera.GetComponent<Camera>();

            cam.fieldOfView = cam2.fieldOfView;

            Object.DestroyImmediate(cam.gameObject.GetComponent<CameraController>());
            Camera.main.gameObject.AddComponent<global::PerspectiveCamera.PerspectiveCamera>();
            Camera.main.gameObject.AddComponent<PerspectiveCameraKeys>();
            Camera.main.gameObject.AddComponent<PerspectiveCameraMouse>();
            Object.Destroy(go);

            _cameraHandler.SetCameraActive(_cameraHandler.PerspectiveCamera);
        }

        

        public void onDisabled()
        {
            _harmony?.UnpatchAll(getIdentifier());
            _harmony = null;
            _cameraHandler.SetCameraActive(_cameraHandler.OrigCamera);
            Object.DestroyImmediate(_cameraHandler.PerspectiveCamera);
            Object.DestroyImmediate(_cameraHandler.gameObject);
        }

        private void SetupKeyBinding()
        {
            KeyGroup group = new KeyGroup(getIdentifier());
            group.keyGroupName = getName();

            InputManager.Instance.registerKeyGroup(group);

            //Options
            RegisterKey("switchMode", KeyCode.F10, "Switch camera",
                "Use this key to quickly switch between the default game camera & Orbit Cam");

            //Options
            RegisterKey("CameraTiltUp", KeyCode.R, "Tilt Camera Up");
            //Options
            RegisterKey("CameraTiltDown", KeyCode.F, "Tilt Camera Down");
        }

        private void RegisterKey(string identifier, KeyCode keyCode, string name, string description = "")
        {
            var key = new KeyMapping(getIdentifier() + "/" + identifier, keyCode, KeyCode.None);
            key.keyGroupIdentifier = getIdentifier();
            key.keyName = name;
            key.keyDescription = description;
            InputManager.Instance.registerKeyMapping(key);
        }

        public void onDrawSettingsUI()
        {
            PerspectiveCameraSettingsUi.DrawGui();
        }

        public void onSettingsOpened()
        {
        }

        public void onSettingsClosed()
        {
            PerspectiveCameraSettings.Instance.Save();
        }

    }
}