namespace BenjisHardwiredLogic
{
    public class BenjisDelayedDecoupler : PartModule
    {
        #region Fields

        //Saving UniversalTime into launchTime when the Vessel getÞs launched
        [KSPField(isPersistant = true, guiActive = false)]
        private double launchTime = 0;

        //Did the launch happen?
        [KSPField(isPersistant = true, guiActive = false)]
        private bool vesselLaunched = false;

        //Did the countdown start?
        [KSPField(isPersistant = true, guiActive = false)]
        private bool countingDown = false;

        //Headline name for the GUI
        [KSPField(isPersistant = true, guiActive = false)]
        private const string PAWDecouplerGroupName = "Benji's Delayed Decoupler";

        //Text, if functionality is disabled
        [KSPField(isPersistant = true, guiActive = false)]
        private const string PAWDecouplerDisabled = "disconnected";

        //Text, if functionality is enabled
        [KSPField(isPersistant = true, guiActive = false)]
        private const string PAWDecouplerEnabled = "connected";

        //A button to enable or disable the function
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Circuits are:", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName),
            UI_Toggle(disabledText = PAWDecouplerDisabled, enabledText = PAWDecouplerEnabled)]
        private bool modInUse = false;

        //Specify the delay in seconds in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Delay [sec]", guiFormat = "F1", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 59.9f, incrementLarge = 10f, incrementSmall = 1f, incrementSlide = 0.1f, sigFigs = 1)]
        private float delaySeconds = 0;

        //Specify the delay in minutes in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Delay [min]", guiFormat = "F1", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 30f, incrementLarge = 5f, incrementSmall = 1f, incrementSlide = 1f, sigFigs = 1)]
        private float delayMinutes = 0;

        //Seconds and Minutes (*60) added
        [KSPField(isPersistant = true, guiActive = false)]
        private float totalDelay = 0;

        //Shows if the ignitor is active
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Decoupler active?", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName)]
        private bool PAWmodInUse = false;

        //Shows the time until the decoupler is activated in seconds, one decimal
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Seconds until Decouple", guiFormat = "F1", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName)]
        private double timeToDecouple = 0;

        #endregion


        #region Overrides

        //This happens once
        public override void OnStart(StartState state)
        {
            //enum of Situations - https://kerbalspaceprogram.com/api/class_vessel.html
            if (vessel.situation == Vessel.Situations.PRELAUNCH)
                {
                //Add up the two parts of the overall delay and show me the numbers
                timeToDecouple = totalDelay = delaySeconds + (delayMinutes * 60f);

                //Set the visible PAW variable 
                PAWmodInUse = modInUse;
            }
            //Need to call that, in case other mods do stuff here
            base.OnStart(state);
        }

        //This happens every frame
        public override void OnUpdate()
        {
            if (modInUse)
            {
                //Check if the Vessel is still attached to the launch clamps
                if (!vesselLaunched)
                {
                    //Once launched the mission time adds up
                    if (part.localRoot.vessel.missionTime > 0)
                    {
                        //Make sure not to jump in here again
                        vesselLaunched = true;
                        //Set the launch time
                        launchTime = Planetarium.GetUniversalTime();
                        //Start the Countdown
                        countingDown = true;
                    }
                }
                //Check if the countdown started... if the Vessel is launched
                else if (countingDown)
                {
                    //Calculate how long until the decoupler decouples
                    timeToDecouple = (launchTime + totalDelay) - Planetarium.GetUniversalTime();

                    //If it's time to decouple...
                    if (timeToDecouple <= 0)
                    {
                        //Stop the countdown
                        countingDown = false;
                        //...do it already
                        part.decouple();
                    }
                }
            }
        }

        #endregion
    }

    public class BenjisDelayedIgnitor : PartModule//ModuleEngines
    {
        #region Fields

        //Saving UniversalTime into launchTime when the Vessel getÞs launched
        [KSPField(isPersistant = true, guiActive = false)]
        private double launchTime = 0;

        //Did the launch happen?
        [KSPField(isPersistant = true, guiActive = false)]
        private bool vesselLaunched = false;
        //Did the countdown start?
        [KSPField(isPersistant = true, guiActive = false)]
        private bool countingDown = false;

        //Headline name for the GUI
        [KSPField(isPersistant = true, guiActive = false)]
        private const string PAWIgnitorGroupName = "Benji's Delayed Ignitor";

        //Text, if functionality is disabled
        [KSPField(isPersistant = true, guiActive = false)]
        private const string PAWIgnitorDisabled = "disconnected";

        //Text, if functionality is enabled
        [KSPField(isPersistant = true, guiActive = false)]
        private const string PAWIgnitorEnabled = "connected";

        //A button to enable or disable the function
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Circuits are:", groupName = PAWIgnitorGroupName, groupDisplayName = PAWIgnitorGroupName),
            UI_Toggle(disabledText = PAWIgnitorDisabled, enabledText = PAWIgnitorEnabled)]
        private bool modInUse = false;

        //Specify the delay in seconds in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Delay [s]", guiFormat = "F1", groupName = PAWIgnitorGroupName, groupDisplayName = PAWIgnitorGroupName),
        UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 59.9f, incrementLarge = 10f, incrementSmall = 1f, incrementSlide = 0.1f, sigFigs = 1)]
        private float delaySeconds = 0;

        //Specify the delay in minutes in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Delay [min]", guiFormat = "F1", groupName = PAWIgnitorGroupName, groupDisplayName = PAWIgnitorGroupName),
        UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 30f, incrementLarge = 5f, incrementSmall = 1f, incrementSlide = 1f, sigFigs = 1)]
        private float delayMinutes = 0;

        //Seconds and Minutes (*60) added
        [KSPField(isPersistant = true, guiActive = false)]
        private float totalDelay = 0;

        //Shows if the ignitor is active
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Ignitor active?", groupName = PAWIgnitorGroupName, groupDisplayName = PAWIgnitorGroupName)]
        private bool PAWmodInUse = false;

        //Shows the time until the engine is activated in seconds, one decimal
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Seconds until Ignition", guiFormat = "F1", groupName = PAWIgnitorGroupName, groupDisplayName = PAWIgnitorGroupName)]
        private double timeToIgnite = 0;

        #endregion


        #region Overrides

        //This happens once
        public override void OnStart(StartState state)
        {
            //enum of Situations - https://kerbalspaceprogram.com/api/class_vessel.html
            if (vessel.situation == Vessel.Situations.PRELAUNCH)
            {
                //Add up the two parts of the overall delay and show me the numbers
                timeToIgnite = totalDelay = delaySeconds + (delayMinutes * 60f);

                //Set the visible PAW variable 
                PAWmodInUse = modInUse;
            }
            //Need to call that, in case other mods do stuff here
            base.OnStart(state);
        }

        //This happens every frame
        public override void OnUpdate()
        {
            if (modInUse)
            {
                //Check if the Vessel is still attached to the launch clamps
                if (!vesselLaunched)
                {
                    //Once launched the mission time adds up
                    if (part.localRoot.vessel.missionTime > 0)
                    {
                        //Make sure not to jump in here again
                        vesselLaunched = true;
                        //Set the launch time
                        launchTime = Planetarium.GetUniversalTime();
                        //Start the Countdown
                        countingDown = true;
                    }
                }
                //Check if the countdown started... if the Vessel is launched
                else if (countingDown)
                {
                    //Calculate how long until the decoupler decouples
                    timeToIgnite = (launchTime + totalDelay) - Planetarium.GetUniversalTime();

                    //If it's time to decouple...
                    if (timeToIgnite <= 0)
                    {
                        //Stop the countdown
                        countingDown = false;
                        //...do it already
                        part.force_activate();
                    }
                }
            }
        }

        #endregion

    }

    public class BenjisFairingSeparator : PartModule
    {
        #region Fields

        //Did the launch happen?
        [KSPField(isPersistant = true, guiActive = false)]
        private bool vesselLaunched = false;
        //Do we still climb?
        [KSPField(isPersistant = true, guiActive = false)]
        private bool checkingHeight = false;

        //Headline name for the GUI
        [KSPField(isPersistant = true, guiActive = false)]
        private const string PAWFairingGroupName = "Benji's Fairing Separator";

        //Text, if functionality is disabled
        [KSPField(isPersistant = true, guiActive = false)]
        private const string PAWFairingDisabled = "disconnected";

        //Text, if functionality is enabled
        [KSPField(isPersistant = true, guiActive = false)]
        private const string PAWFairingEnabled = "connected";

        //A button to enable or disable the function
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Circuits are:", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName),
            UI_Toggle(disabledText = PAWFairingDisabled, enabledText = PAWFairingEnabled)]
        private bool modInUse = false;

        //Specify the Height in kilometers in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Height [km]", guiFormat = "F0", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName),
        UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 140f, incrementLarge = 10f, incrementSmall = 1f, incrementSlide = 1f, sigFigs = 0)] //140km - that's where the atmosphere ends
        private float editorHeightToSeparate = 0;

        //Shows if the fairing is active
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Fairing active?", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName)]
        private bool PAWmodInUse = false;

        //Shows the Height in kilometers at which the fairing gets separated
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Height [km] to Separate", guiFormat = "F0", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName)]
        private float flightHeightToSeparate = 0;

        #endregion


        #region Overrides

        //This happens once
        public override void OnStart(StartState state)
        {
            //enum of Situations - https://kerbalspaceprogram.com/api/class_vessel.html
            if (vessel.situation == Vessel.Situations.PRELAUNCH)
            {
                //Show me the numbers
                flightHeightToSeparate = editorHeightToSeparate;

                //Set the visible PAW variable 
                PAWmodInUse = modInUse;
            }
            //Need to call that, in case other mods do stuff here
            base.OnStart(state);
        }

        //This happens every frame
        public override void OnUpdate()
        {
            if (modInUse)
            {
                //Check if the Vessel is still attached to the launch clamps
                if (!vesselLaunched)
                {
                    //Once launched the mission time adds up
                    if (part.localRoot.vessel.missionTime > 0)
                    {
                        //Make sure not to jump in here again
                        vesselLaunched = true;
                        //Start checking the height
                        checkingHeight = true;
                    }
                }
                //Check if the check for height started ... if the Vessel is launched
                else if (checkingHeight)
                {
                    //Are we high enough to separate...
                    if (part.localRoot.vessel.orbit.altitude >= (flightHeightToSeparate * 1000f))
                    {
                        //Stop checking the height
                        checkingHeight = false;
                        //...do it already
                        part.decouple();
                    }
                }

            }
        }

        #endregion

    }

    //HAVN'T FOUND OUT WHAT TO USE TO JETTISON STOCK FAIRINGS
    /*
    public class BenjisStockFairingSeparator : PartModule
    {
        #region Fields

        //Did the launch happen?
        [KSPField(isPersistant = true, guiActive = false)]
        private bool vesselLaunched = false;
        //Do we still climb?
        [KSPField(isPersistant = true, guiActive = false)]
        private bool checkingHeight = false;

        //Headline name for the GUI
        [KSPField(isPersistant = true, guiActive = false)]
        private const string PAWFairingGroupName = "Benji's Fairing Separator";

        //Specify the Height in kilometers in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Height [km]", guiFormat = "F0", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName),
        UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 140f, incrementLarge = 10f, incrementSmall = 1f, incrementSlide = 1f, sigFigs = 0)] //140km - that's where the atmosphere ends
        private float editorHeightToSeparate = 0;

        //Shows if the fairing is active
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Fairing active?", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName)]
        private bool PAWmodInUse = false;

        //Shows the Height in kilometers at which the fairing gets separated
        [KSPField(isPersistant = true, guiActiveEditor = false,  guiActive = true, guiName = "Height [km] to Separate", guiFormat = "F0", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName)]
        private float flightHeightToSeparate = 0;

        #endregion


        #region Overrides

        //This happens once
        public override void OnStart(StartState state)
        {
            //enum of Situations - https://kerbalspaceprogram.com/api/class_vessel.html
            if (vessel.situation == Vessel.Situations.PRELAUNCH)
            {
                //Show me the numbers
                flightHeightToSeparate = editorHeightToSeparate;

                //Set the visible PAW variable 
                PAWmodInUse = modInUse;
            }
            //Need to call that, in case other mods do stuff here
            base.OnStart(state);
        }

        //This happens every frame
        public override void OnUpdate()
        {
            if (modInUse)
            {
                //Check if the Vessel is still attached to the launch clamps
                if (!vesselLaunched)
                {
                    //Once launched the mission time adds up
                    if (part.localRoot.vessel.missionTime > 0)
                    {
                        //Make sure not to jump in here again
                        vesselLaunched = true;
                        //Start checking the height
                        checkingHeight = true;
                    }
                }
                //Check if the check for height started ... if the Vessel is launched
                else if (checkingHeight)
                {
                    //Are we high enough to separate...
                    if (part.localRoot.vessel.orbit.altitude >= (flightHeightToSeparate * 1000f))
                    {
                        //Stop checking the height
                        checkingHeight = false;
                        //...do it already
                        //part.decouple();  //HAVN'T FOUND OUT WHAT TO USE TO JETTISON STOCK FAIRINGS
                        
                    }
                }

            }
        }

        #endregion

    }
    */
}