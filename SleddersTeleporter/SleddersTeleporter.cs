using MelonLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SleddersTeleporterNs
{   
    public class SleddersTeleporter : MelonMod
    {
        //private TeleportMapViewController teleporter;
        private TeleportMapController teleportMap;

        //Game reports player position as X == <east/west>, y == vertical, z == <north/south>
        //We only care about 2-D coordinates, so represent our position as normal (X,Y)
        private string playerXPos = "";
        private string playerYPos = "";

        private string targetXPos = "";
        private string targetYPos = "";

        private bool createTextBoxes;
        private bool teleportPlayer;
        private bool chatOpen;

        private GameObject player = null;
        private ChatController2 chatController = null;
        private string chatOpenVarName = "";

        KeyCode controllerKey;
        KeyCode keyboardKey;        

        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();

            SceneManager.sceneLoaded += this.OnSceneLoaded;                        

            createTextBoxes = false;
            teleportPlayer = false;

            string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgFile = Path.Combine(modPath, "TeleporterControls.cfg");
            bool cfgFound = File.Exists(cfgFile);

            if (!cfgFound)
            {
                MelonLogger.Msg("Creating cfg file in " + cfgFile);
                StreamWriter writer = new StreamWriter(cfgFile);
                writer.WriteLine("keyboard=T");
                writer.WriteLine("controller=JoystickButton8");
                writer.Close();
            }

            MelonLogger.Msg("Reading cfg file from " + cfgFile);
            StreamReader reader = new StreamReader(cfgFile);
            string cfgText = reader.ReadToEnd();
            
            foreach(Match match in Regex.Matches(cfgText, @"keyboard=(.+)")){
                if (match.Success)
                {
                    keyboardKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), match.Groups[1].Value, true);
                    MelonLogger.Msg("Keyboard key: " + keyboardKey.ToString());
                }               
            }
            foreach (Match match in Regex.Matches(cfgText, @"controller=(.+)"))
            {
                if (match.Success)
                {
                    controllerKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), match.Groups[1].Value, true);
                    MelonLogger.Msg("Controller key: " + controllerKey.ToString());
                }                
            }

            MelonLogger.Msg("Teleporter initialized!");
            
        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            //Disable text boxes whenever the scene changes, otherwise they can get stuck
            this.createTextBoxes = false;

            this.player = null;
            MapController mapController = (MapController)GameObject.FindAnyObjectByType<MapController>();
            //Try and figure out if we are in game yet. We should have a map once we're in
            if (mapController != null)
            {
                MelonLogger.Msg("Found MapController object");

                //Construct our objects once we are in a scene with a map
                //this.teleporter = new TeleportMapViewController();
                this.teleportMap = new TeleportMapController();
            }
            this.chatController = (ChatController2)GameObject.FindAnyObjectByType<ChatController2>();

            if (this.chatController != null)
            {
                MelonLogger.Msg("Got chat controller instance!");

                MemberInfo[] fields = typeof(ChatController2).GetMembers(BindingFlags.Public | BindingFlags.GetField | BindingFlags.Instance);
                foreach (FieldInfo pubField in fields)
                {                    
                    MelonLogger.Msg(pubField);
                    chatOpenVarName = pubField.Name.ToString();                    
                }
            }
        }

        public void tryTeleportPlayer(Vector3 position, Quaternion rotation)
        {
            GameObject gameObject = GameObject.FindGameObjectWithTag("Player");
            if (gameObject != null) {
                Transform transform = gameObject.transform;
                Respawnable respawnable = ((transform != null) ? transform.GetComponent<Respawnable>() : null);
                if (respawnable == null)
                {
                    MelonLogger.Msg("Cannot respawn: respawn controller not found.");
                    return;
                }
                Vector3 validRespawnPosition = respawnable.GetValidRespawnPosition(position, rotation);
                respawnable.Respawn(validRespawnPosition, rotation, true);

                GameObject hud = GameObject.FindGameObjectWithTag("Hud");
                if (hud != null)
                {
                    hud.GetComponent<HudController>().Notify("kibjib's teleporter: Player teleported!", Color.white, 1.5f, 1f, false);
                }
            }
            else
            {
                MelonLogger.Msg("No player GameObject found!");
            }

            
        }

        private bool tryMapTeleport(MapViewController mapViewController)
        {
            //We are in the map view
            MelonLogger.Msg("Got mapViewController instance");

            //Cursor position and map size are vector2 members                        
            FieldInfo[] fields = typeof(MapViewController).GetFields(BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);

            foreach (FieldInfo privField in fields)
            {
                if (privField.FieldType == typeof(Vector2))
                {
                    if (privField.Name.ToString() != "mapSize")
                    {
                        MelonLogger.Msg("Found vector2 in mapView controller: " + privField.Name.ToString());
                        //Use reflection to access private member of map to get the cursor position                                                            
                        var field = typeof(MapViewController).GetField(privField.Name.ToString(), BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);

                        Vector2 cursorPosition = (Vector2)field.GetValue(mapViewController);
                        MelonLogger.Msg("Cursor map position: " + cursorPosition.ToString());

                        //Convert the cursor position to the actual map position
                        cursorPosition = teleportMap.mapToWorldPosition(cursorPosition);
                        MelonLogger.Msg("World position: " + cursorPosition.ToString() + "\n");

                        //Teleport the player to the new position         
                        tryTeleportPlayer(new Vector3(cursorPosition.x, 0f, cursorPosition.y), Quaternion.Euler(0f, 0f, 0f));                        
                        
                        return true;
                    }
                }
            }
            MelonLogger.Msg("Failed to find private member for cursor coords. Aborting!!");
            return false;
        }


        private bool tryTextTeleport()
        {
            if (this.teleportPlayer)
            {
                this.teleportPlayer = false;

                if (this.player != null)
                {
                    MelonLogger.Msg("Found player object! Attempting teleport!");
                    MelonLogger.Msg("Current pos: (" + player.transform.position.x + "," + player.transform.position.z + ")");

                    //Y component is don't care, because the game's method will fix that for us
                    Vector3 targetPosition = new Vector3((float)Convert.ToDouble(this.targetXPos), 0f, (float)Convert.ToDouble(this.targetYPos));                    
                    tryTeleportPlayer(targetPosition, Quaternion.Euler(0f, 0f, 0f));
                    MelonLogger.Msg("New pos: (" + player.transform.position.x + "," + player.transform.position.z + ")\n");

                    this.targetXPos = "";
                    this.targetYPos = "";
                    return true;
                }
                else
                {
                    MelonLogger.Msg("No player object found!");
                    return false;
                }
            }
            return false;
        }

        private void updateTextBoxes()
        {
            if (this.createTextBoxes)
            {
                if (this.player != null)
                {
                    //Update the X and Y position for the OnGUI method below so our position constantly updates
                    playerXPos = player.transform.position.x.ToString();
                    playerYPos = player.transform.position.z.ToString();
                }
                else
                {
                    this.createTextBoxes = false;
                    MelonLogger.Msg("No player object found!");
                }
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (chatController != null)
            {
                this.chatOpen = chatController.FFHDACKPPIN;
            }

            if (!this.chatOpen)
            {
                //Player presses keyboard or controller button
                if (Input.GetKeyDown(keyboardKey) || Input.GetKeyDown(controllerKey))
                {
                    //Try and get a handle to a MapViewController instance. This will tell us if we are looking at the map
                    MapViewController mapController = (MapViewController)GameObject.FindAnyObjectByType<MapViewController>();
                    if (mapController != null)
                    {
                        this.tryMapTeleport(mapController);                        
                    }

                    //If we're not looking at the map, then use text boxes
                    else
                    {
                        GameObject gameObject = GameObject.FindGameObjectWithTag("Player");
                        //if (!((gameObject != null) ? gameObject.GetComponent<SnowmobileController>() : null).hasJoinedChallenge)
                        if(gameObject != null)
                        {
                            this.player = gameObject;
                            this.createTextBoxes = !this.createTextBoxes;
                        }
                        else
                        {
                            this.createTextBoxes = false;
                            MelonLogger.Msg("No player object found!");
                        }
                    }
                }

                this.updateTextBoxes();                
            }
            else
            {
                createTextBoxes = false;
            }
        }

        public override void OnGUI()
        {
            base.OnGUI();            

            if (this.createTextBoxes)
            {
                GUI.skin.textField.fontSize = 20;
                GUI.skin.box.fontSize = 20;
                GUI.skin.button.fontSize = 20;                

                int boxWidth = 100;
                int boxHeight = 40;
                int boxXOffset = 10;
                int boxYOffset = 10;       
                
                GUI.Box(new Rect(boxWidth*0 + boxXOffset, boxHeight*0 + boxYOffset, boxWidth*2, boxHeight), "kibjib's Teleporter");
                
                GUI.Box(new Rect(boxWidth*0 + boxXOffset, boxHeight*1 + boxYOffset, boxWidth, boxHeight), "Player X: ");
                GUI.Box(new Rect(boxWidth*1 + boxXOffset, boxHeight*1 + boxYOffset, boxWidth, boxHeight), playerXPos);

                GUI.Box(new Rect(boxWidth*0 + boxXOffset, boxHeight*2 + boxYOffset, boxWidth, boxHeight), "Player Y: ");
                GUI.Box(new Rect(boxWidth*1 + boxYOffset, boxHeight*2 + boxYOffset, boxWidth, boxHeight), playerYPos);                

                //Get user input for desired coordinates
                GUI.Box(new Rect(boxWidth*0 + boxXOffset, boxHeight*3 + boxYOffset, boxWidth, boxHeight), "Target X:");
                targetXPos = GUI.TextField(new Rect(boxWidth*1 + boxXOffset, boxHeight*3 + boxYOffset, boxWidth, boxHeight), targetXPos, 8);

                //Could use a better regex here to only allow a single -
                //Doing a regex replace on any illegal characters is much easier though
                targetXPos = Regex.Replace(targetXPos, @"[^0-9\.-]", "");

                GUI.Box(new Rect(boxWidth*0 + boxXOffset, boxHeight*4 + boxYOffset, boxWidth, boxHeight), "Target Y:");
                targetYPos = GUI.TextField(new Rect(boxWidth * 1 + boxXOffset, boxHeight * 4 + boxYOffset, boxWidth, boxHeight), targetYPos, 8);
                targetYPos = Regex.Replace(targetYPos, @"[^0-9\.-]", "");

                if (GUI.Button(new Rect(boxWidth*0 + boxXOffset, boxHeight*5 + boxYOffset, 2*boxWidth, boxHeight), "Teleport"))
                {
                    MelonLogger.Msg("Attempting text teleport to (" + this.targetXPos + "," + this.targetYPos + ")");
                    this.createTextBoxes = false;
                    this.teleportPlayer = true;
                    this.tryTextTeleport();
                }
            }
        }

        public override void OnApplicationQuit()
        {
            base.OnApplicationQuit();

            SceneManager.sceneLoaded -= this.OnSceneLoaded;

            this.teleportPlayer = false;
            this.createTextBoxes = false;
            this.playerXPos = "";
            this.playerYPos = "";

            MelonLogger.Msg("Teleporter terminated");
        }
    }

    public class TeleportMapController : MapController {

        private Vector2 origin2D = new Vector2();
        private float mapScale = 0f;

        public TeleportMapController()
        {
            base.Awake();

            Terrain[] terrains = Terrain.activeTerrains;

            Vector3 terrainPosition;

            Vector3 origin = new Vector3(0f, 0f, 0f);

            float maxHoriz = 0f;
            float maxVert = 0f;

            float scaleX = 0f;
            float scaleZ = 0f;

            foreach (Terrain terrain in terrains)
            {
                terrainPosition = terrain.GetPosition();

                if (terrainPosition.x <= origin.x && terrainPosition.z <= origin.z)
                {                    
                    origin = terrainPosition;
                    scaleX = origin.x / terrain.terrainData.baseMapResolution;
                    scaleZ = origin.z / terrain.terrainData.baseMapResolution;

                }
                if (terrainPosition.x >= maxHoriz && terrainPosition.z >= maxVert)
                {
                    maxHoriz = terrainPosition.x;
                    maxVert = terrainPosition.z;
                }
            }

            //In the case that the origin is at 0,0 we need to make sure the scale is correct.
            if (scaleX == 0f)
                scaleX = 1f;

            if (scaleZ == 0f)
                scaleZ = 1f;

            this.origin2D = new Vector2(origin.x / scaleX, origin.z / scaleZ);

            maxHoriz = maxHoriz + origin2D.x;

            //If our map scale is equal to our world scale, then we will be one basemap off in each direction
            if (scaleX == 1f)
                maxHoriz += terrains[0].terrainData.baseMapResolution;

            maxVert = maxVert + origin2D.y;
            if (scaleZ == 1f)
                maxVert += terrains[0].terrainData.baseMapResolution;

            this.mapScale = this.mapSize.x / maxHoriz;

            MelonLogger.Msg("Map origin = " + origin2D.x + ", " + origin2D.y);
            MelonLogger.Msg("Map max coords = " + maxHoriz + ", " + maxVert);
            MelonLogger.Msg("Map scale = " + mapScale);
        }
        
        public Vector2 mapToWorldPosition(Vector2 mapPos)
        {            
            mapPos.y = this.mapSize.y - mapPos.y;
            return mapPos / mapScale - origin2D;                                    
        }        
    }          
}
