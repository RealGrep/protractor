//Original author: Enigma
//Development has been continued by: Addle
//Distributed according to GNU General Public License version 3, available at http://www.gnu.org/copyleft/gpl.html. All other rights reserved.
//no warrantees of any kind are made with distribution, including but not limited to warranty of merchantability and warranty for a particular purpose.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Reflection;
using KSP.UI.Screens;



namespace Protractor {

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    //[KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class Protractor : MonoBehaviour
    {
        private GameObject approach_obj;
        private Dictionary<string, Color> bodycolorlist = new Dictionary<string, Color>();

        private ProtractorData pdata;
        private ProtractorCalcs pcalcs;

        private CelestialBody
            drawApproachToBody = null,
            focusbody = null,
            lastknownmainbody;
        protected Rect
            manualwindowPos,
            settingswindowPos,
            windowPos;
        private Vector2 scrollposition;
        private bool
            psitotime = false,
            thetatotime = false,
            dvtotime = false,
            adjustejectangle = false,
            showmanual = true,
            showsettings = false,
            isInitialized = false,
            isGUIInitialized = false,
            loaded = false,
            showplanets = true,
            showadvanced = false,
            showdv = false,
            trackdv = false,
            showmoons = true;
        private GUIStyle
            boldstyle,
            datastyle,
            databox,
            datatitle,
            dataclose,
            dataintercept,
            tooltipstyle;
            //iconstyle;
        private LineRenderer approach;
        private PlanetariumCamera cam;
        private double
            totaldv = 0,
            trackeddv = 0;

        public float t_lastUpdate = 0.0f;

        // Sample strings for GUI fields for font metrics
        private string[] colheaders = new string[6] { "", "θ", "Ψ", "Δv", "Closest", "Moon Ω" };
        private string[] colsamples = new string[6] { "XXXXXXXX", "Xy XXXd 00:00:00XX", "00:00:00XX", "0000.0 m/sXX", "000.00 XXXX", "Moon Ω" };
        private int[] colwidths = new int[6] { 70, 120, 63, 71, 100, 71 };

        private string
            psi_time,
            bodytip,
            phase_angle_time,
            linetip,
            dv_time;
        private string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        // The Id of the currently selected GUI skin
        public int skinId = 0;

        public enum SkinType { Default, KSP, Compact }
        public static GUISkin defaultSkin;
        public static GUISkin compactSkin;

        public static readonly float updateInterval_def = 0.2f;
        public float updateInterval = updateInterval_def;
        public string updateIntervalString = "0.20  ";

        public static readonly double planetAlarmMargin_def = 60 * 60;
        public double planetAlarmMargin = planetAlarmMargin_def;
        public string planetAlarmMargin_str = "3600.00";

        public static readonly double moonAlarmMargin_def = 60 * 5;
        public double moonAlarmMargin = moonAlarmMargin_def;
        public string moonAlarmMargin_str = "300.00";

        // Main GUI visibility
        public static bool isVisible = true;

        // Button for Toolbar
        private IButton button = null;

        // Button for AppLauncher
        public ApplicationLauncherButton appButton = null;


        // Initializes lists of bodies, planets, and parameters
        private void initialize()
        {
            if (isInitialized)
            {
                return;
            }
            loadsettings();
 
            pdata = new ProtractorData();
            pcalcs = new ProtractorCalcs(pdata);

            lastknownmainbody = FlightGlobals.fetch.activeVessel.mainBody;

            isInitialized = true;

            bodycolorlist.Add("Kerbin", Utils.hextorgb("a3ede4"));
            bodycolorlist.Add("Moho", Utils.hextorgb("c46a4b"));
            bodycolorlist.Add("Eve", Utils.hextorgb("d3adff"));
            bodycolorlist.Add("Duna", Utils.hextorgb("edb4a6"));
            bodycolorlist.Add("Jool", Utils.hextorgb("8cf068"));
            bodycolorlist.Add("Vall", Utils.hextorgb("969ebf"));
            bodycolorlist.Add("Laythe", Utils.hextorgb("90caeb"));
            bodycolorlist.Add("Tylo", Utils.hextorgb("fedede"));
            bodycolorlist.Add("Bop", Utils.hextorgb("b8a58b"));
            bodycolorlist.Add("Ike", Utils.hextorgb("aeb5ca"));
            bodycolorlist.Add("Gilly", Utils.hextorgb("b8a58b"));
            bodycolorlist.Add("Mun", Utils.hextorgb("aeb5ca"));
            bodycolorlist.Add("Minmus", Utils.hextorgb("a68db8"));
            bodycolorlist.Add("Eeloo", Utils.hextorgb("929292"));
            bodycolorlist.Add("Dres", Utils.hextorgb("917552"));
            bodycolorlist.Add("Pol", Utils.hextorgb("929d6d"));

            if (!KACWrapper.InitKACWrapper())
            {
                Debug.Log("Protractor: KAC integration initialized.");
            }
            Debug.Log("-------------Protractor Initialized-------------");
        }

        private void initGUI()
        {
            boldstyle = new GUIStyle(GUI.skin.label);
            boldstyle.normal.textColor = Color.yellow;
            boldstyle.fontStyle = FontStyle.Bold;
            boldstyle.alignment = TextAnchor.LowerCenter;

            datastyle = new GUIStyle(GUI.skin.label);
            datastyle.alignment = TextAnchor.MiddleLeft;
            datastyle.fontStyle = FontStyle.Normal;

            tooltipstyle = new GUIStyle(GUI.skin.box);
            tooltipstyle.normal.background = GUI.skin.window.normal.background;
            tooltipstyle.fontStyle = FontStyle.Bold;
            tooltipstyle.normal.textColor = Color.yellow;

            databox = new GUIStyle(GUI.skin.box);
            databox.margin.top = databox.margin.bottom = -5;
            databox.border.top = databox.border.bottom = 0;
            databox.wordWrap = false;

            datatitle = new GUIStyle(GUI.skin.label);
            datatitle.alignment = TextAnchor.MiddleLeft;
            datatitle.fontStyle = FontStyle.Bold;

            dataclose = new GUIStyle(GUI.skin.label);
            dataclose.alignment = TextAnchor.MiddleLeft;
            dataclose.fontStyle = FontStyle.Bold;

            dataintercept = new GUIStyle(GUI.skin.label);
            dataintercept.alignment = TextAnchor.MiddleLeft;
            dataintercept.fontStyle = FontStyle.BoldAndItalic;

            // Figure out the width of the fields in the GUI with a little font metrics
            // from sample strings that should be as wide as the field can be.
            for (int i = 1; i < colheaders.Length; ++i)
            {
                colwidths[i] = Mathf.CeilToInt(datastyle.CalcSize(new GUIContent(colsamples[i])).x);
            }

            isGUIInitialized = true;
        }

        void OnGUIAppLauncherReady()
        {
            if( !this.appButton )
            {
                this.appButton = ApplicationLauncher.Instance.AddModApplication(
                    delegate() {
                        isVisible = true;
                    },
                    delegate() {
                        isVisible = false;
                    },
                    null,
                    null,
                    null,
                    null,
                    ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW,
                    (Texture)GameDatabase.Instance.GetTexture("Protractor/icon", false));
            }
        }

        public void drawGUI()
        {
            Vessel vessel = FlightGlobals.fetch.activeVessel;

            if (!isInitialized)
            {
                initialize();
            }
            if (!isGUIInitialized)
            {
                LoadSkin((SkinType)skinId);
                initGUI();
            }
            // OnDestroy gets called for us when another ship with Protractor exits physics range.
            // So we constantly need to recheck and put it back if it's been zapped.
            CreateToolbarButton();

            if (vessel == FlightGlobals.ActiveVessel)
            {
                GUI.skin = null;
                LoadSkin((SkinType)skinId);

                if (isVisible)
                {
                    if (showmanual)
                    {
                        manualwindowPos = GUILayout.Window(555, manualwindowPos, manualGUI,
                            "Protractor v." + version, GUILayout.Width(400), GUILayout.Height(500));
                    }
                    if (showsettings)
                    {
                        settingswindowPos = GUILayout.Window(557, settingswindowPos, settingsGUI,
                            "Settings", GUILayout.Width(250), GUILayout.Height(100));
                    }

                    windowPos = GUILayout.Window(556, windowPos, mainGUI, "Protractor v." + version, GUILayout.Width(1), GUILayout.Height(1)); //367
                }
                else
                {
                    approach.enabled = false;
                }
            }
        }

        void Start()
        {
            if (!isInitialized)
            {
                initialize();
            }
            approach_obj = new GameObject("Line");
            loadsettings();
            if ((windowPos.x == 0) && (windowPos.y == 0))//windowPos is used to position the GUI window, lets set it in the center of the screen
            {
                windowPos = new Rect(Screen.width / 2, Screen.height / 2, 10, 10);
            }

            approach_obj.layer = 9;
            cam = (PlanetariumCamera)GameObject.FindObjectOfType(typeof(PlanetariumCamera));

            approach = approach_obj.AddComponent<LineRenderer>();
            approach.transform.parent = null;
            approach.enabled = false;
            approach.SetColors(Color.green, Color.green);
            approach.useWorldSpace = true;
            approach.SetVertexCount(2);
            approach.SetWidth(10, 10);  //was 15, 5

            approach.material = ((MapView)GameObject.FindObjectOfType(typeof(MapView))).orbitLinesMaterial;

            if (ToolbarManager.ToolbarAvailable)
            {
                Debug.Log("Protractor: Blizzy's toolbar present");
                CreateToolbarButton();
            }
            else
            {
                Debug.Log("Protractor: Blizzy's toolbar NOT present");
                //loadicons();
                if (appButton == null)
                {
                    if (ApplicationLauncher.Ready)
                    {
                        OnGUIAppLauncherReady();
                    } else {
                        GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
                    }
                }
            }
            //vessel.OnFlyByWire += new FlightInputCallback(fly);
        }

        /// <summary>
        /// Called by Unity to draw the GUI - can be called many times per frame.
        /// </summary>
        public void OnGUI () {
            drawGUI( );
        }

        private void CreateToolbarButton()
        {
            if (ToolbarManager.ToolbarAvailable)
            {
                if (button == null)
                {
                    button = ToolbarManager.Instance.add("Protractor", "protractorButton");
                    button.TexturePath = "Protractor/icon";
                    button.ToolTip = "Toggle Protractor UI";
                    button.Visibility = new GameScenesVisibility(GameScenes.FLIGHT);
                    button.OnClick += (e) => {
                        isVisible = !isVisible;
                    };
                }
            } else {
                if (appButton == null)
                {
                    if (ApplicationLauncher.Ready)
                    {
                        OnGUIAppLauncherReady();
                    } else {
                        GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
                    }
                }
            }
        }

        // If using Blizzy78's Toolbar, the button *must* be destroyed OnDestroy
        public void OnDestroy()
        {
            savesettings();
            if (button != null)
            {
                button.Destroy();
                button = null;
            }
            if (appButton != null && !ToolbarManager.ToolbarAvailable)
            {
                GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
                ApplicationLauncher.Instance.RemoveModApplication(appButton);
            }
        }

        public void Update()
        {
            /*
            if (isVisible)
            {
                protractoricon = protractoriconON;
            }
            else
            {
                protractoricon = protractoriconOFF;
            }
            */
        }



        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                return;
            }


            Vessel vessel = FlightGlobals.fetch.activeVessel;
            if( vessel == FlightGlobals.ActiveVessel )
            {
/*
                if( vessel != vessel ) // vessel changed, nuke everything
                {
                    Debug.Log( "PROTRACTOR: vessel changed, nuke everything" );
                    drawApproachToBody = null;
                    pdata.initialize();
                    lastknownmainbody = vessel.mainBody;
                    focusbody = null;
                }
*/
                if( vessel.situation != Vessel.Situations.PRELAUNCH )
                {
                    totaldv += TimeWarp.fixedDeltaTime * pcalcs.thrustAccel();
                    if (trackdv)
                    {
                        trackeddv += TimeWarp.fixedDeltaTime * pcalcs.thrustAccel();
                    }
                }

                // Only recalculate data at fixed intervals (very roughly speaking)
                t_lastUpdate += Time.deltaTime;
                if (t_lastUpdate > updateInterval)
                {
                    t_lastUpdate = 0.0f;
                    // No need to update if not showing the GUI
                    if (isVisible)
                    {
                        pcalcs.update(pdata.celestials);
                    }
                }
            }
        }

        public void mainGUI(int windowID)
        {
            Vessel vessel = FlightGlobals.fetch.activeVessel;
            if (vessel.mainBody != lastknownmainbody)
            {
                drawApproachToBody = null;
                pdata.initialize();
                lastknownmainbody = vessel.mainBody;
                focusbody = null;
            } //resets bodies, lines and collapse

            bodytip = focusbody == null ? "Click to focus" : "Click to unfocus";
            linetip = "Click to toggle approach line";
            phase_angle_time = "Toggle between angle and ESTIMATED time";
            psi_time = "Toggle between angle and ESTIMATED time";
            dv_time = "Toggle between ΔV and ESTIMATED\nburn time at full thrust";

            printheaders();
            if (showplanets)
            {
                printplanetdata();
            }
            if (showmoons)
            {
                printmoondata();
            }
            printvesseldata();
            drawApproach();

            if (GUI.tooltip != "")
            {
                // How many lines, and what's the longest line in the tooltip text?
                int num_lines = 1;
                int max_width = 0;
                int line_width = 0;
                for (int i = 0; i < GUI.tooltip.Length; ++i)
                {
                    if (GUI.tooltip[i] == '\n')
                    {
                        num_lines++;
                        line_width = 0;
                    }
                    else
                    {
                        line_width++;
                        if (line_width > max_width)
                        {
                            max_width = line_width;
                        }
                    }
                }
                int w = 10 * max_width;
                float x = (Event.current.mousePosition.x < windowPos.width / 2) ? Event.current.mousePosition.x + 10 : Event.current.mousePosition.x - 10 - w;
                GUI.Box(new Rect(x, Event.current.mousePosition.y, w, 30*num_lines), GUI.tooltip, tooltipstyle); //resize
            }
            GUI.DragWindow();
        }

        public void settingsGUI(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Update interval (secs): ");

            updateIntervalString = GUILayout.TextField(updateIntervalString, 10);
            try {
                updateInterval = float.Parse(updateIntervalString);
            } catch {
                updateInterval = updateInterval_def;
            }
            if (updateInterval < 0.001f || updateInterval > 10.0f)
            {
                updateInterval = updateInterval_def;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("KAC Alarm Margin (planets): ");
            planetAlarmMargin_str = GUILayout.TextField(planetAlarmMargin_str, 10);
            try {
                planetAlarmMargin = float.Parse(planetAlarmMargin_str);
            } catch {
                planetAlarmMargin = planetAlarmMargin_def;
            }
            if (planetAlarmMargin < 0.0 || planetAlarmMargin > 60*60*ProtractorCalcs.HoursPerDay*5)
            {
                planetAlarmMargin = planetAlarmMargin_def;
            }
            GUILayout.Label("s");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("KAC Alarm Margin (moons): ");
            moonAlarmMargin_str = GUILayout.TextField(moonAlarmMargin_str, 10);
            try {
                moonAlarmMargin = float.Parse(moonAlarmMargin_str);
            } catch {
                moonAlarmMargin = moonAlarmMargin_def;
            }
            if (moonAlarmMargin < 0.0 || moonAlarmMargin > 60*60*ProtractorCalcs.HoursPerDay)
            {
                moonAlarmMargin = moonAlarmMargin_def;
            }
            GUILayout.Label("s");
            GUILayout.EndHorizontal();

            GUILayout.Label("Current skin: " + (SkinType)skinId );
            if (GUI.skin == null || skinId != 1)
            {
                if (GUILayout.Button("KSP skin"))
                {
                    LoadSkin(SkinType.KSP);
                    skinId = 1;
                }
            }
            if (GUI.skin == null || skinId != 0)
            {
                if (GUILayout.Button("Unity Smoke skin"))
                {
                    LoadSkin(SkinType.Default);
                    skinId = 0;
                }
            }
            if (GUI.skin == null || skinId != 2)
            {
                if (GUILayout.Button("Compact skin"))
                {
                    LoadSkin(SkinType.Compact);
                    skinId = 2;
                }
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        public void manualGUI(int windowID)
        {
            GUIStyle wordWrapLabelStyle;
            wordWrapLabelStyle = new GUIStyle();
            wordWrapLabelStyle.wordWrap = true;
            wordWrapLabelStyle.normal.textColor = Color.white;

            scrollposition = GUILayout.BeginScrollView(scrollposition, false, true,
                GUILayout.Width(600), GUILayout.Height(600));
            GUILayout.Label(
                "*****Tips*****\n\n" +
                "- Click on the icon in the bottom left to hide Protractor and its windows\n" +
                "- Click on the number in \"Closest\" column to toggle the closest approach line on the map\n" +
                "- Click on the name of a celestial body in the list to hide other bodies.\n" +
                "  Click on the θ angle or time display to create a KAC alarm, if present. Keep in mind\n" +
                "    the time calculation assumes a circular orbit and may be off by varying degrees.\n" +
                "- Click on θ in the column headers to toggle between displaying an angle and an \n" +
                " approximate time until the next launch window.\n" +
                "- Click on Ψ in the column headers to toggle between displaying and angle and an\n" +
                " approximate time until the next ejection burn.\n" +
                "- Click on Δv in the column headers to toggle between displaying estimated transfer\n" +
                " delta V and an approximate burn time for that delta V in seconds. When engines are\n" +
                " off, uses maximum thrust for current stage. When firing engines, uses the thrust at\n" +
                " current throttle levels.\n" +
                "- When a body is focused and an intercept is detected, your predicted inclination is \n" +
                " displayed below the closest approach.\n\n" +
                "*****Column Key*****\n\n" +
                "θ: Difference in the current angle between bodies and the desired angle between \n" +
                "   them for transfer. Launch your ship when this is 0.\n\n" +
                "Ψ: Point in vessel's current orbit (relative to orbited body's prograde) where you \n" +
                "   should start your ejection burn. Burn when this is 0.\n\n" +
                "Δv: Amount your current velocity needs to be changed to accomplish maneuver.\n\n" +
                "Adjust Ψ: Used to time escape. Toggle to adjust your escape angle based on your\n" +
                "          craft's thrust capabilities.\n\n" +
                "Closest: The closest approach between your craft and the target during one revolution.\n\n" +
                "*****Instructions*****\n\n" +
                "To use this guide, time warp until \"θ\" is 0. IT IS STRONGLY SUGGESTED TO DO \n" +
                "THIS BEFORE LAUNCHING YOUR SHIP. This means the planets are in the right \n" +
                "position relative to each other. \n" +
                "Launch into a low orbit, then time warp until \"Ψ\" is 0. This means your vessel is \n" +
                "in the right place in it's orbit. \n" +
                "For best results, click \"Adjust Ψ\" or start your ejection burn before \n" +
                "the Ψ hits 0 so that it does so when your burn is exactly 2/3 complete. \n" +
                "Burn in direction of vessel's prograde until \"Δv\" is approximately 0.\"\n\n" +
                "This mod assumes your craft is in a 0-inclination, circular orbit. Target is also \n" +
                "assumed to be in 0-inclination, circular orbit. Either a 90° or 270° heading \n" +
                "will work, though launching to 90° is more efficient. \n" +
                "YOU WILL HAVE TO MAKE ADJUSTMENTS TO RENDEZVOUS. THIS MOD ONLY \n" +
                "GETS YOU IN THE NEIGHBORHOOD. To close the gap, try burning at 90° angles \n" +
                "(pro/retro, norm/antinorm, +rad/-rad).\n" +
                "Eventually, you'll know which way to burn to correct an orbit.\n\n" +
                "*****Advanced*****\n\n" +
                "Only works when orbiting a moon. This data is designed to aid in travelling from \n" +
                "a moon, to the moon's planet, and then to another moon. (e.g. Tylo -> Jool -> Kerbin). \n" +
                "Adds \"Moon Ω\" column representing angle from moon to the prograde of the \n" + 
                "planet that moon orbits. \n" +
                "\"Alt\" above represents your target periapsis around the moon's planet where you \n" +
                "should begin your ejection burn. \"Eject from [moon]\" " + "indicates where to \n" +
                "leave your moon's orbit. To use this mode, wait until \"θ\" is 0, \"Moon Ω\" is 0, \n" +
                "and \"Eject from [moon]\" is 0. Burn to create an orbit with an apoapsis at your \n" +
                "current moon and a periapsis at \"Alt\". When you reach periapsis, burn for target \n" +
                "planet.\n",
                wordWrapLabelStyle
            );
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }
      
        public void switchcolor(string key)
        {
            Color col;
            if (bodycolorlist.TryGetValue(key, out col))
            {
                datastyle.normal.textColor = col;
                datatitle.normal.textColor = col;
            }
            else
            {
                datastyle.normal.textColor = Color.white;
                datatitle.normal.textColor = Color.white;
            }
        }

        public void printheaders()
        {
            // Begin column headers
            GUILayout.BeginHorizontal();
            {
                for (int i = 0; i <= 5; i++)
                {
                    if (i == 5 && (!showadvanced || pdata.getorbitbodytype() != ProtractorData.orbitbodytype.moon))
                    {
                        GUILayout.Label(new GUIContent("", ""), boldstyle);
                        continue;
                    }
                    GUILayout.BeginVertical(GUILayout.Width(colwidths[i]));
                    {
                        if (colheaders[i] == "θ")
                        {
                            GUILayout.Label(new GUIContent(colheaders[i], phase_angle_time), boldstyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                            if ((Event.current.type == EventType.repaint) && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && Input.GetMouseButtonDown(0))
                            {
                                thetatotime = !thetatotime;
                            }
                        }
                        else if (colheaders[i] == "Ψ")
                        {
                            GUILayout.Label(new GUIContent(colheaders[i], psi_time), boldstyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                            if ((Event.current.type == EventType.repaint) && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && Input.GetMouseButtonDown(0))
                            {
                                psitotime = !psitotime;
                            }
                        }
                        else if (colheaders[i] == "Δv")
                        {
                            GUILayout.Label(new GUIContent(colheaders[i], dv_time), boldstyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                            if ((Event.current.type == EventType.repaint) && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && Input.GetMouseButtonDown(0))
                            {
                                dvtotime = !dvtotime;
                            }
                            
                        }
                        else
                        {
                            GUILayout.Label(colheaders[i], boldstyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                    }
                    GUILayout.EndVertical();
                }
                // End column headers
                GUILayout.EndHorizontal();
            }
        }

        // margin: Time before 'ut' we wish to trigger
        // ut: Time of event. Time of alarm will be ut - margin.
        public void AddAlarm(CelestialBody origin, CelestialBody destination, double margin, double ut)
        {
            // Add KAC alarm
            if (KACWrapper.APIReady)
            {
                String tmpID = KACWrapper.KAC.CreateAlarm(KACWrapper.KACAPI.AlarmTypeEnum.TransferModelled,
                    String.Format("{0} -> {1}", origin.name, destination.name),
                    ut - margin);

                KACWrapper.KACAPI.KACAlarm alarmNew = KACWrapper.KAC.Alarms.First(a => a.ID == tmpID);
                alarmNew.Notes = "Alarm created by Protractor.";
                alarmNew.AlarmMargin = margin;
                alarmNew.AlarmAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarp;
                alarmNew.XferOriginBodyName = origin.name;
                alarmNew.XferTargetBodyName = destination.name;
            }
        }

        public void printplanetdata()
        {
            foreach (CelestialBody planet in pdata.planets)
            {
                CelestialData planetdata = pdata.celestials[planet.name];
                if (!(planet.Equals(focusbody)) && (focusbody != null))
                {
                    continue; //focus body defined and it isn't this one
                }
                switchcolor(planet.name);
                // Starts a row of planet data
                GUILayout.BeginHorizontal(databox);
                for (int i = 0; i <= 5; i++)
                {
                    GUILayout.BeginVertical(GUILayout.MinWidth(colwidths[i])); //begin data cell
                    switch (i)
                    {
                    //******printing names******
                    case 0:
                        GUILayout.Label(new GUIContent(planetdata.name, bodytip), datatitle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                        if ((Event.current.type == EventType.repaint) && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && Input.GetMouseButtonDown(0))
                        {
                            if (planet.Equals(focusbody))
                            {
                                focusbody = null;
                            }
                            else
                            {
                                focusbody = planet;
                            }
                        }
                        break;
                    //******printing phase angles******
                    case 1:
                        string datastring;
                        if (thetatotime)    //convert to time or leave as angle
                        {
                            datastring = planetdata.theta_time_str;
                        }
                        else
                        {
                            datastring = String.Format("{0:0.00}°", planetdata.theta_angle);
                        }

                        GUI.skin.label.alignment = TextAnchor.MiddleRight;
                        GUILayout.Label(datastring, datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        GUI.skin.label.alignment = TextAnchor.MiddleLeft;

                        if ((Event.current.type == EventType.repaint) && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && Input.GetMouseButtonDown(0))
                        {
                            // Add KAC alarm on click
                            AddAlarm(FlightGlobals.fetch.activeVessel.mainBody, planet, planetAlarmMargin, Planetarium.GetUniversalTime() + planetdata.theta_time);
                        }

                        break;
                    //******printing psi angles******
                    case 2:
                        if (pdata.getorbitbodytype() != ProtractorData.orbitbodytype.planet)
                        {
                            GUILayout.Label("----", datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        } else {
                            double psidata;
                            string psidisplay;

                            if (adjustejectangle)
                            {
                                psidata = planetdata.psi_angle_adjusted;
                            } else {
                                psidata = planetdata.psi_angle;
                            }

                            if (psidata == -1)
                            {
                                psidisplay = "0 TMR";
                            } else if (psitotime) {
                                if (adjustejectangle)
                                {
                                    psidisplay = planetdata.psi_time_adjusted_str;
                                } else {
                                    psidisplay = planetdata.psi_time_str;
                                }
                            } else {
                                psidisplay = String.Format("{0:0.00}°", psidata);
                            }

                            GUILayout.Label(psidisplay, datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        break;
                    //******delta-v******
                    case 3:
                        if (pdata.getorbitbodytype() == ProtractorData.orbitbodytype.moon)
                        {
                            GUILayout.Label("----", datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        else
                        {
                            double dv = planetdata.deltaV;
                            if (!dvtotime)
                            {
                                GUILayout.Label(String.Format("{0:0.0} m/s", dv), datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                            }
                            else
                            {
                                GUILayout.Label(planetdata.deltaV_time.ToString("F1") + "s", datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                            }
                        }
                        break;
                        //******closest approach******
                    case 4:
                        double distance = planetdata.closest_approach;
                        GUIStyle diststyle = datastyle;
                        if (distance <= 5 * planet.sphereOfInfluence && distance >= 0)
                        {
                            diststyle = dataclose;
                        }

                        if (distance <= planet.sphereOfInfluence && distance >= 0)
                        {
                            diststyle = dataintercept;
                        }

                        if (planet.Equals(drawApproachToBody))
                        {
                            GUILayout.Label(new GUIContent("*" + ProtractorCalcs.ToSI(distance) + "m" + "*", linetip), diststyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        else
                        {
                            GUILayout.Label(new GUIContent(ProtractorCalcs.ToSI(distance) + "m", linetip), diststyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }

                        if ((Event.current.type == EventType.repaint) && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && Input.GetMouseButtonDown(0))
                        {
                            if (planet.Equals(drawApproachToBody))
                            {
                                drawApproachToBody = null;
                            }
                            else
                            {
                                drawApproachToBody = planet;
                            }
                        }
                        break;
                    case 5:
                        if (pdata.getorbitbodytype() == ProtractorData.orbitbodytype.moon && showadvanced)
                        {
                            GUILayout.Label(String.Format("{0:0.00}°", planetdata.adv_ejection_angle), datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        } else
                        {
                            GUILayout.Label(new GUIContent("", ""), datastyle);
                        }
                        break;
                    }
                    GUILayout.EndVertical();    //end data cell
                }
                GUILayout.EndHorizontal(); //end planet row
            }
        }


        public void printmoondata()
        {
            foreach (CelestialBody moon in pdata.moons)
            {
                CelestialData moondata = pdata.celestials[moon.name];
                if (!(moon.Equals (focusbody)) && (focusbody != null))
                {
                    continue;
                }
                switchcolor(moon.name);
                GUILayout.BeginHorizontal(databox);    //starts row of moon data

                for (int i = 0; i <= 4; i++)
                {
                    GUILayout.BeginVertical(GUILayout.Width(colwidths[i])); //begin data cell
                    switch (i)
                    {
                    //******printing names******
                    case 0:
                        GUILayout.Label(new GUIContent(moondata.name, bodytip), datatitle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        if ((Event.current.type == EventType.repaint) &&
                             GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) &&
                             Input.GetMouseButtonDown(0))
                        {
                            if (moon.Equals(focusbody))
                            {
                                focusbody = null;
                            }
                            else
                            {
                                focusbody = moon;
                            }
                        }
                        break;
                    //******phase angles******
                    case 1:
                        string datastring;
                        if (thetatotime)    //convert to time or leave as angle
                        {
                            datastring = moondata.theta_time_str;
                        } else
                        {
                            datastring = String.Format("{0:0.00}°", moondata.theta_angle);
                        }

                        GUI.skin.label.alignment = TextAnchor.MiddleRight;
                        GUILayout.Label(datastring, datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        GUI.skin.label.alignment = TextAnchor.MiddleLeft;

                        // Add KAC Alarm on click
                        if ((Event.current.type == EventType.repaint) && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && Input.GetMouseButtonDown(0))
                        {
                            AddAlarm(FlightGlobals.fetch.activeVessel.mainBody, moon, moonAlarmMargin, Planetarium.GetUniversalTime() + moondata.theta_time);
                        }
                        
                        break;
                    //******eject angles******
                    case 2:
                        if (pdata.getorbitbodytype() == ProtractorData.orbitbodytype.planet)
                        {
                            GUILayout.Label("----", datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        } else {
                            double psidata;
                            string psidisplay;

                            if (adjustejectangle)
                            {
                                psidata = moondata.psi_angle_adjusted;
                            } else {
                                psidata = moondata.psi_angle;
                            }

                            if (psidata == -1)
                            {
                                psidisplay = "0 TMR";
                            } else if (psitotime) {
                                if (adjustejectangle)
                                {
                                    psidisplay = moondata.psi_time_adjusted_str;
                                } else
                                {
                                    psidisplay = moondata.psi_time_str;
                                }
                            } else {
                                psidisplay = String.Format("{0:0.00}°", psidata);
                            }

                            GUILayout.Label(psidisplay, datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        break;
                        //******delta V******
                    case 3:
                        double dv = moondata.deltaV;
                        if (!dvtotime)
                        {
                            GUILayout.Label(String.Format("{0:0.0} m/s", dv), datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        else
                        {
                            GUILayout.Label(moondata.deltaV_time.ToString("F1") + "s", datastyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        break;
                        //******closest approach******
                    case 4:
                        double distance = moondata.closest_approach;
                        GUIStyle diststyle = datastyle;

                        if (distance <= 2 * moon.sphereOfInfluence && distance >= 0)
                        {
                            diststyle = dataclose;
                        }
                        if (distance <= moon.sphereOfInfluence && distance >= 0)
                        {
                            diststyle = dataintercept;
                        }

                        if (moon.Equals(drawApproachToBody))
                        {
                            GUILayout.Label(new GUIContent("*" + ProtractorCalcs.ToSI(distance) + "m" + "*", linetip), diststyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                        else
                        {
                            GUILayout.Label(new GUIContent(ProtractorCalcs.ToSI(distance) + "m", linetip), diststyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }

                        if ((Event.current.type == EventType.repaint) && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && Input.GetMouseButtonDown(0))
                        {
                            if (moon.Equals(drawApproachToBody))
                            {
                                drawApproachToBody = null;
                            }
                            else
                            {
                                drawApproachToBody = moon;
                            }
                        }
                        break;
                    }
                    GUILayout.EndVertical();
                }

                GUILayout.EndHorizontal();
            }
        }

        public void printvesseldata()
        {
            Vessel vessel = FlightGlobals.fetch.activeVessel;
            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical(GUILayout.Width(50));
                {
                    adjustejectangle = GUILayout.Toggle(adjustejectangle, "Adjust Ψ", new GUIStyle(GUI.skin.button));
                    GUILayout.EndVertical();
                }

                GUILayout.BeginVertical(GUILayout.Width(50));
                {
                    showplanets = GUILayout.Toggle(showplanets, "Planets", new GUIStyle(GUI.skin.button));
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUILayout.Width(50));
                {
                    showmoons = GUILayout.Toggle(showmoons, "Moons", new GUIStyle(GUI.skin.button));
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUILayout.Width(50));
                {
                    showdv = GUILayout.Toggle(showdv, "Show dV", new GUIStyle(GUI.skin.button));
                }
                GUILayout.EndVertical();

                if (pdata.getorbitbodytype() == ProtractorData.orbitbodytype.moon)
                {
                    GUILayout.BeginVertical(GUILayout.Width(50));
                    {
                        showadvanced = GUILayout.Toggle(showadvanced, "Adv", new GUIStyle(GUI.skin.button));
                    }
                    GUILayout.EndVertical();
                }

                GUILayout.FlexibleSpace();

                if (focusbody != null && pcalcs.getclosestapproach(focusbody) <= focusbody.sphereOfInfluence)
                {
                    Orbit o = pcalcs.getclosestorbit(focusbody);
                    if (o.referenceBody == focusbody)
                    {
                        GUILayout.Label(String.Format("Inc: {0:0.00}°", o.inclination));
                    }
                    else
                    {
                        GUILayout.Label("Inc: ---");
                    }
                }

                GUILayout.FlexibleSpace();

                GUILayout.BeginVertical(GUILayout.Width(10));
                {
                    showsettings = GUILayout.Toggle(showsettings, "Settings", new GUIStyle(GUI.skin.button));
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUILayout.Width(10));
                {
                    showmanual = GUILayout.Toggle(showmanual, "?", new GUIStyle(GUI.skin.button));
                }
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }

            if (pdata.getorbitbodytype() == ProtractorData.orbitbodytype.moon && showadvanced)
            {
                GUILayout.BeginHorizontal(databox);
                {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label(String.Format("Ejection from " + vessel.mainBody.name + ": {0:0.00}°", (pcalcs.MoonAngle() - pcalcs.CurrentEjectAngle(null) + 360) % 360),
                            boldstyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label("Alt above " + vessel.mainBody.orbit.referenceBody.name + ": " +
                            ProtractorCalcs.ToSI(1.05 * vessel.mainBody.orbit.referenceBody.atmosphereDepth) + "m", boldstyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }   //allow advanced menu

            if (showdv)
            {
                int w = 80;
                boldstyle.alignment = TextAnchor.MiddleLeft;
                GUILayout.BeginHorizontal(databox);
                {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(string.Format("Sum Δv: {0:#,#}", totaldv), boldstyle, GUILayout.ExpandWidth(true));
                            trackdv = GUILayout.Toggle(trackdv, "Track", new GUIStyle(GUI.skin.button), GUILayout.Width(w));
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(string.Format("Tracked Δv: {0:#,#}", trackeddv), boldstyle, GUILayout.ExpandWidth(true));
                            if (GUILayout.Button("Reset", new GUIStyle(GUI.skin.button), GUILayout.Width(w)))
                            {
                                trackeddv = 0;
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
                boldstyle.alignment = TextAnchor.LowerCenter;
            }
        }

        public void drawApproach()
        {
            if (drawApproachToBody != null && MapView.MapIsEnabled && pdata.closestApproachTime > 0)
            {
                //approach.enabled = true;
                Orbit closeorbit = pcalcs.getclosestorbit(drawApproachToBody);

                double distance = pcalcs.getclosestapproach(drawApproachToBody);
                // Only draw when not on intercept course already. Was a bug where
                // we would draw a line to nowhere when intercepting. This is better.
                if (distance > drawApproachToBody.sphereOfInfluence)
                {
                    approach.enabled = true;
                    if (closeorbit.referenceBody == drawApproachToBody)
                    {
                        approach.SetPosition(0, ScaledSpace.LocalToScaledSpace(closeorbit.getTruePositionAtUT(pdata.closestApproachTime)));
                    } else
                    {
                        approach.SetPosition(0, ScaledSpace.LocalToScaledSpace(closeorbit.getPositionAtUT(pdata.closestApproachTime)));
                    }

                    approach.SetPosition(1, ScaledSpace.LocalToScaledSpace(drawApproachToBody.orbit.getPositionAtUT(pdata.closestApproachTime)));

                    float scale = (float)(0.004 * cam.Distance);
                    approach.SetWidth(scale, scale);
                }
                else
                {
                    approach.enabled = false;
                }
            }
            else
            {
                approach.enabled = false;
            }

        }
        /*
        public void fly(FlightCtrlState s)
        {
            throttle = s.mainThrottle;
        }
        */

        /*
         * SETTINGS.
         */
        public void savesettings()
        {
            if (!loaded)
            {
                return;
            }
            KSP.IO.PluginConfiguration cfg = KSP.IO.PluginConfiguration.CreateForType<Protractor>();
            cfg["config_version"] = version;
            cfg["mainpos"] = windowPos;
            cfg["manualpos"] = manualwindowPos;
            cfg["settingspos"] = settingswindowPos;
            cfg["showadvanced"] = showadvanced;
            cfg["adjustejectangle"] = adjustejectangle;
            cfg["showmanual"] = showmanual;
            cfg["showsettings"] = showsettings;
            cfg["isvisible"] = isVisible;
            cfg["showplanets"] = showplanets;
            cfg["showmoons"] = showmoons;
            cfg["showadvanced"] = showadvanced;
            cfg["showdv"] = showdv;
            cfg["trackdv"] = trackdv;
            cfg["skinid"] = skinId;
            cfg["updateinterval"] = updateInterval;
            updateIntervalString = updateInterval.ToString("F2");
            cfg["updateinterval"] = updateIntervalString;
            planetAlarmMargin_str = planetAlarmMargin.ToString("F2");
            cfg["planetalarmmargin"] = planetAlarmMargin_str;
            moonAlarmMargin_str = moonAlarmMargin.ToString("F2");
            cfg["moonalarmmargin"] = moonAlarmMargin_str;

            Debug.Log("-------------Saved Protractor Settings-------------");
            cfg.save();
        }

        public void loadsettings()
        {
            Debug.Log("-------------Loading settings...-------------");
            KSP.IO.PluginConfiguration cfg = KSP.IO.PluginConfiguration.CreateForType<Protractor>();
            cfg.load();
            Debug.Log("-------------Settings Opened-------------");
            windowPos = cfg.GetValue<Rect>("mainpos", new Rect(0, 0, 0, 0));
            manualwindowPos = cfg.GetValue<Rect>("manualpos", new Rect(0, 0, 0, 0));
            settingswindowPos = cfg.GetValue<Rect>("settingspos", new Rect(0, 0, 0, 0));
            showadvanced = cfg.GetValue<bool>("showadvanced", true);
            adjustejectangle = cfg.GetValue<bool>("adjustejectangle", false);
            showmanual = cfg.GetValue<bool>("showmanual", false);
            showsettings = cfg.GetValue<bool>("showsettings", false);
            isVisible = cfg.GetValue<bool>("isvisible", true);
            showplanets = cfg.GetValue<bool>("showplanets", true);
            showmoons = cfg.GetValue<bool>("showmoons", true);
            showdv = cfg.GetValue<bool>("showdv", true);
            trackdv = cfg.GetValue<bool>("trackdv", true);

            skinId = cfg.GetValue<int>("skinid", (int)Protractor.SkinType.Default);

            updateIntervalString = cfg.GetValue<string>("updateinterval", "0.20");
            try {
                updateInterval = Single.Parse(updateIntervalString);
            } catch {
                updateInterval = updateInterval_def;
            }

            planetAlarmMargin_str = cfg.GetValue<string>("planetalarmmargin", "3600");
            try {
                planetAlarmMargin = Single.Parse(planetAlarmMargin_str);
            } catch {
                planetAlarmMargin = planetAlarmMargin_def;
            }

            moonAlarmMargin_str = cfg.GetValue<string>("moonalarmmargin", "300");
            try {
                moonAlarmMargin = Single.Parse(moonAlarmMargin_str);
            } catch {
                moonAlarmMargin = moonAlarmMargin_def;
            }

            loaded = true;  //loaded

            Debug.Log("-------------Loaded Protractor Settings-------------");
        }

        // More code from MechJeb2 for skin selection. Yay GPL3 licensing!
        public static void CopyDefaultSkin()
        {
            GUI.skin = null;
            defaultSkin = (GUISkin)GameObject.Instantiate(GUI.skin);
        }

        public static void CopyCompactSkin()
        {
            GUI.skin = null;
            compactSkin = (GUISkin)GameObject.Instantiate(GUI.skin);
            GUI.skin.name = "KSP Compact";
            compactSkin.label.margin = new RectOffset(1, 1, 1, 1);
            compactSkin.label.padding = new RectOffset(0, 0, 2, 2);
            compactSkin.button.margin = new RectOffset(1, 1, 1, 1);
            compactSkin.button.padding = new RectOffset(4, 4, 2, 2);
            compactSkin.toggle.margin = new RectOffset(1, 1, 1, 1);
            compactSkin.toggle.padding = new RectOffset(15, 0, 2, 0);
            compactSkin.textField.margin = new RectOffset(1, 1, 1, 1);
            compactSkin.textField.padding = new RectOffset(2, 2, 2, 2);
            compactSkin.textArea.margin = new RectOffset(1, 1, 1, 1);
            compactSkin.textArea.padding = new RectOffset(2, 2, 2, 2);
            compactSkin.window.margin = new RectOffset(0, 0, 0, 0);
            compactSkin.window.padding = new RectOffset(5, 5, 20, 5);
        }

        public static void LoadSkin(SkinType skinType)
        {
            GUI.skin = null;
            switch (skinType)
            {
            case SkinType.Default:
                if (defaultSkin == null) CopyDefaultSkin();
                GUI.skin = defaultSkin;
                break;
            case SkinType.KSP:
                GUI.skin = AssetBase.GetGUISkin("KSP window 2");
                break;
            case SkinType.Compact:
                if (compactSkin == null) CopyCompactSkin();
                GUI.skin = compactSkin;
                break;
            }

        }

        // from http://wiki.unity3d.com/index.php?title=PopupList
        public static bool List(Rect position, ref bool showList, ref int listEntry,
            GUIContent buttonContent, string[] list, GUIStyle listStyle)
        {
            return List(position, ref showList, ref listEntry, buttonContent, list, "button", "box", listStyle);
        }
        public static bool List(Rect position, ref bool showList, ref int listEntry, GUIContent buttonContent, string[] list,
            GUIStyle buttonStyle, GUIStyle boxStyle, GUIStyle listStyle)
        {
            int controlID = GUIUtility.GetControlID(865645, FocusType.Passive);
            bool done = false;
            switch (Event.current.GetTypeForControl(controlID))
            {
            case EventType.mouseDown:
                if (position.Contains(Event.current.mousePosition))
                {
                    GUIUtility.hotControl = controlID;
                    showList = true;
                }
                break;
            case EventType.mouseUp:
                if (showList)
                {
                    done = true;
                }
                break;
            }
            GUI.Label(position, buttonContent, buttonStyle);
            if (showList)
            {
                Rect listRect = new Rect(position.x, position.y, position.width, list.Length * 20);
                GUI.Box(listRect, "", boxStyle);
                listEntry = GUI.SelectionGrid(listRect, listEntry, list, 1, listStyle);
            }
            if (done)
            {
                showList = false;
            }
            return done;
        }
    } // end of class

} //end of namespace
