using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace KerbalSlingshotter
{
    [KSPAddon(KSPAddon.Startup.Flight,false)]
    public class FlightSlingshot : SlingshotCore { }
    [KSPAddon(KSPAddon.Startup.TrackingStation,false)]
    public class TrackingSlingshot : SlingshotCore { }

    public class SlingshotCore : MonoBehaviour
    {
        internal static Texture2D ShipIcon = null;
        internal static Texture2D BodyIcon = null;
        private static Vessel vessel { get { return CurrentVessel(); } }
        protected Rect windowPos = new Rect(50, 100, 300, 200);
        double DesiredTime;
        public static int HoursPerDay { get { return GameSettings.KERBIN_TIME ? 6 : 24; } }
        public static int DaysPerYear { get { return GameSettings.KERBIN_TIME ? 426 : 365; } }
        private static double UT { get { return Planetarium.GetUniversalTime(); } }
        ApplicationLauncherButton button;
        bool WindowVisible = false;
        uint years = 0, days = 0, hours = 0, minutes = 0, seconds = 0;

        void Start()
        {
            DesiredTime = UT;
            ShipIcon = GameDatabase.Instance.GetTexture("Squad/PartList/SimpleIcons/RDicon_commandmodules", false);
            BodyIcon = GameDatabase.Instance.GetTexture("SlingShotter/Textures/body", false);
            RenderingManager.AddToPostDrawQueue(3, new Callback(drawGUI));//start the GUI
            if (HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.FLIGHT)
                CreateButtonIcon();
        }
        void OnDestroy()
        {
            ApplicationLauncher.Instance.RemoveModApplication(button);
        }

        void FixedUpdate()
        {
        }

        static Vessel CurrentVessel()
        {
            if (HighLogic.LoadedScene == GameScenes.FLIGHT && FlightGlobals.ActiveVessel != null)
            {
                return FlightGlobals.ActiveVessel;
            }
            else if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
            {
                SpaceTracking st = (SpaceTracking)FindObjectOfType(typeof(SpaceTracking));
                if (st.mainCamera.target != null && st.mainCamera.target.type == MapObject.MapObjectType.VESSEL)
                {
                    return st.mainCamera.target.vessel;
                }
                else
                {
                    return null;
                }
            }
            return null;
        }


        void setTimeSelection(double selection)
        {
            selection -= UT;
            years = (uint)(selection / DaysPerYear / HoursPerDay / 3600);
            days = (uint)((selection / HoursPerDay / 3600) % DaysPerYear);
            hours = (uint)((selection / 3600) % HoursPerDay);
            minutes = (uint)((selection / 60) % 60);
            seconds = (uint)(selection % 60);
        }

        private void WindowGUI(int windowID)
        {
            GUIStyle mySty = new GUIStyle(GUI.skin.button);
            mySty.normal.textColor = mySty.focused.textColor = Color.white;
            mySty.hover.textColor = mySty.active.textColor = Color.yellow;
            mySty.onNormal.textColor = mySty.onFocused.textColor = mySty.onHover.textColor = mySty.onActive.textColor = Color.green;
            mySty.padding = new RectOffset(8, 8, 8, 8);
            GUILayout.BeginVertical();
            GUILayout.Label("Desired Time:", GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            GUILayout.Label("y", GUILayout.ExpandWidth(false));
            years = uint.Parse(GUILayout.TextField(years.ToString(), GUILayout.Width(40)));
            GUILayout.Label("d", GUILayout.ExpandWidth(false));
            days = uint.Parse(GUILayout.TextField(days.ToString(), GUILayout.Width(40)));
            GUILayout.Label("h", GUILayout.ExpandWidth(false));
            hours = uint.Parse(GUILayout.TextField(hours.ToString(), GUILayout.Width(40)));
            GUILayout.Label("m", GUILayout.ExpandWidth(false));
            minutes = uint.Parse(GUILayout.TextField(minutes.ToString(), GUILayout.Width(40)));
            GUILayout.Label("s", GUILayout.ExpandWidth(false));
            seconds = uint.Parse(GUILayout.TextField(seconds.ToString(), GUILayout.Width(40)));
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Next Node") && vessel.patchedConicSolver.maneuverNodes.Any())
            {
                setTimeSelection(vessel.patchedConicSolver.maneuverNodes.First().UT);
                GUI.changed = false;
            }
            if (GUILayout.Button("Last Node") && vessel.patchedConicSolver.maneuverNodes.Any())
            {
                setTimeSelection(vessel.patchedConicSolver.maneuverNodes.Last().UT);
                GUI.changed = false;
            }
            GUILayout.EndVertical();
            DesiredTime = UT + years * DaysPerYear * HoursPerDay * 3600.0 + 
                days * HoursPerDay * 3600.0 + 
                hours * 3600.0 +
                minutes * 60.0 + seconds;
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }
        private void drawGUI()
        {
            if (WindowVisible)
            {
                if (HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.FLIGHT)
                    DrawIconForAllOrbits();
                GUI.skin = HighLogic.Skin;
                windowPos = GUILayout.Window(1, windowPos, WindowGUI, "SlingShotter | Set Time", GUILayout.MinWidth(300));
            }
        }

        void DrawPatchIcons(Orbit o)
        {
            while (o != null)
            {
                if (o.ContainsUT(DesiredTime))
                    DrawIcon(o.getPositionAtUT(DesiredTime), ShipIcon);
                o = o.nextPatch;
            }
        }

        void DrawNodeOrbits()
        {
            Orbit o = vessel.orbit;
            foreach (ManeuverNode node in vessel.patchedConicSolver.maneuverNodes)
            {
                if (node != null && node.nextPatch != null)
                {
                    DrawPatchIcons(node.nextPatch);
                }
            }
        }

        void DrawBodyOrbits()
        {
            foreach (CelestialBody body in FlightGlobals.Bodies)
            {

                if (body != null && body.orbit != null)
                {
                    DrawIcon(body.getPositionAtUT(DesiredTime), BodyIcon);
                }
            }
        }

        void DrawIconForAllOrbits()
        {
            if (vessel != null)
            {
                DrawPatchIcons(vessel.orbit); // Icons for all patches of actual vessel orbit
                DrawNodeOrbits(); // Icon(s) for wherever we fall in any maneuver node created orbits
            }
            DrawBodyOrbits(); // Icons for all the celestial bodies
        }

        void DrawIcon(Vector3d position,Texture2D icon)
        {
            GUIStyle styleWarpToButton = new GUIStyle();
            styleWarpToButton.fixedWidth = 32;
            styleWarpToButton.fixedHeight = 32;
            styleWarpToButton.normal.background = icon;

            Vector3d screenPosNode = MapView.MapCamera.camera.WorldToScreenPoint(ScaledSpace.LocalToScaledSpace(position));
            Rect rectNodeButton = new Rect((Int32)screenPosNode.x-16, (Int32)(Screen.height - screenPosNode.y)-16, 32, 32);
            GUI.Button(rectNodeButton, "", styleWarpToButton);
        }

        private void CreateButtonIcon()
        {
            button = ApplicationLauncher.Instance.AddModApplication(
                () => WindowVisible = true,
                () => WindowVisible = false,
                null,
                null,
                null,
                null,
                ApplicationLauncher.AppScenes.ALWAYS,
                GameDatabase.Instance.GetTexture("SlingShotter/Textures/icon", false)
                );
        }

    }
}
