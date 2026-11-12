using System;
using UnityEngine;

namespace PerspectiveCamera
{
    internal class PerspectiveCameraMouse : MonoBehaviour
    {
        public KeyCode MouseOrbitButton;


        public bool ScreenEdgeMoveBreaksFollow;
        public int ScreenEdgeBorderWidth;
        public float MoveSpeed;

        public bool AllowPan;
        public bool PanBreaksFollow;
        public float PanSpeedVar;
        private const float MapViewPanFactor = 0.4f;
        private const float PanFactor = 0.5f;
        public float PanSpeed;

        public bool AllowRotate;

        public bool AllowTilt;
        public float TiltSpeedVar;
        public float TiltSpeed;

        public bool AllowZoom;

        public string RotateInputAxis = "Mouse X";
        public string TiltInputAxis = "Mouse Y";
        public string ZoomInputAxis = "Mouse ScrollWheel";
        public KeyCode PanKey1 = KeyCode.LeftShift;
        public KeyCode PanKey2 = KeyCode.RightShift;

        //

        private PerspectiveCamera _camera;

        //

        public void Reset()
        {
            MouseOrbitButton = KeyCode.Mouse2;


            ScreenEdgeMoveBreaksFollow = true;
            ScreenEdgeBorderWidth = 4;
            MoveSpeed = 10f;

            AllowPan = true;
            PanBreaksFollow = true;
            PanSpeedVar = 50f;
            PanKey1 = KeyCode.LeftShift;
            PanKey2 = KeyCode.RightShift;

            AllowRotate = true;

            AllowTilt = true;
            TiltSpeedVar = 200f;

            AllowZoom = true;
            

            RotateInputAxis = "Mouse X";
            TiltInputAxis = "Mouse Y";
            ZoomInputAxis = "Mouse ScrollWheel";
        }

        protected void Start()
        {
            _camera = gameObject.GetComponent<PerspectiveCamera>();
        }

        // A drag with the orbit button that starts over a window belongs to the window, such as a PiP Cam view, so the
        // camera neither orbits nor holds the cursor for it. Worked out once a frame, for both of its readers.
        private static int _orbitCheckedFrame = -1;
        private static bool _orbitStartedOverWindow;

        internal static bool OrbitStartedOverWindow
        {
            get
            {
                if (_orbitCheckedFrame != Time.frameCount)
                {
                    _orbitCheckedFrame = Time.frameCount;
                    if (Input.GetKeyDown(KeyCode.Mouse2))
                        _orbitStartedOverWindow = UIUtility.isMouseOverUIElement();
                    else if (!Input.GetKey(KeyCode.Mouse2))
                        _orbitStartedOverWindow = false;
                }
                return _orbitStartedOverWindow;
            }
        }

        protected void Update()
        {
            // Without a camera there is nothing to control, and the first lines below would throw on every frame.
            if (_camera == null)
                return;

            try
            {
                float num = 0.02f;
                float distanceMultiplier = _camera.Distance * 5 / _camera.MaxDistance + 0.1f;
                float distanceModifier = Mathf.Sqrt(_camera.Distance / _camera.MaxDistance);


                MoveSpeed = 20f;
                var rotateSpeed = PerspectiveCameraSettings.Instance.RotateSpeed;
                var zoomSpeed = PerspectiveCameraSettings.Instance.ZoomSpeed;
                TiltSpeed = TiltSpeedVar;
                PanSpeed = PanSpeedVar;

                if (!UIUtility.isMouseOverUIElement())
                {
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        // Shift with the wheel changes how fast the keys move the camera instead of zooming. It is the
                        // same value as the Move Speed slider, kept within the slider's range.
                        float notches = Input.mouseScrollDelta.y;
                        if (notches != 0f)
                        {
                            PerspectiveCameraSettings settings = PerspectiveCameraSettings.Instance;
                            settings.MoveSpeed = Mathf.Clamp(settings.MoveSpeed * Mathf.Pow(1.15f, notches), 1f, 40f);
                            PlayerPrefs.SetFloat("MoveSpeed", settings.MoveSpeed);
                        }
                    }
                    else if (AllowZoom)
                    {
                        var scroll = -Input.GetAxisRaw(ZoomInputAxis);
                        _camera.Distance -= scroll * zoomSpeed * num * distanceMultiplier;
                    }
                }

                if (Input.GetKey(MouseOrbitButton) && !OrbitStartedOverWindow)
                {
                    // In the map view there is no orbit, so the button drags the map without Shift.
                    if (AllowPan && (_camera.IsMapView || Input.GetKey(PanKey1) || Input.GetKey(PanKey2)))
                    {
                        // Slower than the original mod's pan, and slower still looking straight down, where the same
                        // mouse movement covers more of the screen.
                        var zoomModifier = _camera.ZoomSpeedModifier * (_camera.IsMapView ? MapViewPanFactor : PanFactor);
                        var panX = -1 * Input.GetAxisRaw("Mouse X") * PanSpeed * num * zoomModifier;
                        var panZ = -1 * Input.GetAxisRaw("Mouse Y") * PanSpeed * num * zoomModifier;

                        _camera.AddToPosition(panX, 0, panZ);

                        if (PanBreaksFollow && (Mathf.Abs(panX) > 0.001f || Mathf.Abs(panZ) > 0.001f))
                        {
                            _camera.EndFollow();
                        }
                    }
                    else
                    {
                        if (AllowTilt)
                        {
                            var tilt = Input.GetAxisRaw(TiltInputAxis);
                            _camera.Tilt -= tilt * TiltSpeed * num;
                        }

                        if (AllowRotate)
                        {
                            var rot = Input.GetAxisRaw(RotateInputAxis);
                            _camera.Rotation += rot * rotateSpeed * num;
                        }
                    }
                }

                if (Settings.Instance.controlsEdgeScrolling && (!_camera.IsFollowing || ScreenEdgeMoveBreaksFollow))
                {
                    var hasMovement = false;

                    if (Input.mousePosition.y > (Screen.height - ScreenEdgeBorderWidth))
                    {
                        hasMovement = true;
                        _camera.AddToPosition(0, 0, MoveSpeed * num * distanceModifier);
                    }
                    else if (Input.mousePosition.y < ScreenEdgeBorderWidth)
                    {
                        hasMovement = true;
                        _camera.AddToPosition(0, 0, -1 * MoveSpeed * num * distanceModifier);
                    }

                    if (Input.mousePosition.x > (Screen.width - ScreenEdgeBorderWidth))
                    {
                        hasMovement = true;
                        _camera.AddToPosition(MoveSpeed * num * distanceModifier, 0, 0);
                    }
                    else if (Input.mousePosition.x < ScreenEdgeBorderWidth)
                    {
                        hasMovement = true;
                        _camera.AddToPosition(-1 * MoveSpeed * num * distanceModifier, 0, 0);
                    }

                    if (hasMovement && _camera.IsFollowing && ScreenEdgeMoveBreaksFollow)
                    {
                        _camera.EndFollow();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}