﻿namespace BenjisHardwiredLogic
{
    using Expansions.Missions.Editor;
    using Smooth.Algebraics;
    using Smooth.Collections;
    using Smooth.Compare;
    using System;
    using System.Collections;
    using System.Text.RegularExpressions;
    using System.Threading;
    using UnityEngine;
    using static FinePrint.ContractDefs;

    public class BenjisDelayedDecoupler : PartModule//Module*Decouple*
    {
        #region Fields

        //Saving UniversalTime into launchTime when the Vessel gets launched
        [KSPField(isPersistant = true, guiActive = false)]
        private double launchTime = 0;

        //Catch the slider dragging "bug"
        [KSPField(isPersistant = true, guiActive = false)]
        private bool negChangeHappened = false;

        //Headline name for the GUI
        [KSPField(isPersistant = true, guiActive = false)]
        private const string PAWDecouplerGroupName = "Benji's Delayed Decoupler";

        //Text, if functionality is disabled/enabled
        [KSPField(isPersistant = true, guiActive = false)]
        private const string StringDisconnected = "disconnected";

        [KSPField(isPersistant = true, guiActive = false)]
        private const string StringConnected = "connected";

        //Text, if event messaging is disabled/enabled
        [KSPField(isPersistant = true, guiActive = false)]
        private const string StringInactive = "inactive";

        [KSPField(isPersistant = true, guiActive = false)]
        private const string StringActive = "active";

        //The PAW fields in the editor
        //A button to enable or disable the module
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Circuits are:", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName),
            UI_Toggle(disabledText = StringDisconnected, enabledText = StringConnected)]
        private bool modInUse = false;

        //Toggle the mode between post launch delay and a pre Apside delay
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Delaymode", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName),
            UI_ChooseOption(options = new string[2] { "Post Launch", "Pre Apside" })]
        private string delayMode = "Post Launch";

        //Specify the delay in seconds in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Delay [s]", guiFormat = "F1", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName),
        UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 59.9f, incrementLarge = 10f, incrementSmall = 1f, incrementSlide = 0.1f, sigFigs = 1)]
        private float delaySeconds = 0;

        //Specify the delay in minutes in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Delay [min]", guiFormat = "F1", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName),
        UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 30f, incrementLarge = 5f, incrementSmall = 1f, incrementSlide = 1f, sigFigs = 1)]
        private float delayMinutes = 0;

        //Seconds and Minutes (*60) added
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Total Delay", guiUnits = "sec", guiFormat = "F1", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName)]
        private float totalDelay = 0;

        //Name what the stage that will be decoupled will be called during event messaging
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Decouple", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName),
            UI_ChooseOption(options = new string[12] { "1st Stage", "2nd Stage", "3rd Stage", "4th Stage", "Booster", "Apogee Kick Stage" , "Payload", "Separation-Motor", "Spin-Motor", "Ullage-Motor", "Spin-/Ullage-Motor", "Apogee Kick Stage" })]
        private string stage = "1st Stage";

        //The PAW fields in Flight
        //Shows if the decoupler is active
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Circuits are", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName)]
        private string PAWmodInUse;
        //Shows the time until the decoupler decouples in seconds, one decimal
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Seconds until Decouple", guiUnits = "s", guiFormat = "F1", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName)]
        private double PAWtimeToDecouple = 0;
        //Shows what delay mode this decoupler is in
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Delaymode", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName)]
        private string PAWdelayMode;
        //Shows what type of stage will be decouple by this decoupler
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Decouple", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName)]
        private string PAWstage;

        //Shown in the Editor and in Flight
        //A button to enable or disable if a message for this event will be shown
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Event Messaging:", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName),
            UI_Toggle(disabledText = StringInactive, enabledText = StringActive)]
        private bool eventMessagingWanted = true;

        //A small variable to manage the onScreen Messages
        private char nextMessageStep = (char)0;

        #endregion

        #region Overrides

        //This happens once in both EDITOR and FLIGHT
        public override void OnStart(StartState state)
        {
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
                initMod();

            if (HighLogic.LoadedScene == GameScenes.EDITOR)
                initEditor();

            //Need to call that, in case other mods do stuff here
            base.OnStart(state);
        }

        //Initialize all the fields when in FLIGHT
        private void initMod()
        {
            //Wait a bit to avoid the splashed bug, where the vesel can enter/stay in SPLASHED situation if something is done too early (before first physics tick)
            Thread.Sleep(200);

            //Now to check if we are on the launch pad
            if (vessel.situation == Vessel.Situations.PRELAUNCH)
            {
                //Add up the two parts of the overall delay and show me the numbers
                PAWtimeToDecouple = totalDelay = delaySeconds + (delayMinutes * 60f);

                //Set the visible PAW variable 
                if (modInUse)
                {
                    PAWmodInUse = StringConnected;
                    //Set the text for inFlight Information
                    PAWdelayMode = delayMode;
                    PAWstage = stage;
                }
                else
                {
                    PAWmodInUse = StringDisconnected;
                    //Disable all text for inFlight Information
                    Fields[nameof(PAWtimeToDecouple)].guiActive = false;
                    Fields[nameof(PAWdelayMode)].guiActive = false;
                    Fields[nameof(PAWstage)].guiActive = false;
                }

            }
        }

        //Initialize all the fields when in EDITOR
        private void initEditor()
        {
            //refresh the PAW to its new size
            //We need to do this once, in case the mod is active in a saved ship
            updateEditorPAW(null);

            GameEvents.onEditorShipModified.Add(updateEditorPAW);
        }

        //Tweak what fields are shown in the editor
        private void updateEditorPAW(ShipConstruct ship)
        {
            if (modInUse)
            {
                totalDelay = delaySeconds + (delayMinutes * 60f);

                Fields[nameof(delaySeconds)].guiActiveEditor = true;
                Fields[nameof(delayMinutes)].guiActiveEditor = true;
                Fields[nameof(totalDelay)].guiActiveEditor = true;

                Fields[nameof(delayMode)].guiActiveEditor = true;

                Fields[nameof(eventMessagingWanted)].guiActiveEditor = true;
            }
            else
            {
                //If this gui or any other is visible now, then we seem to change this in the next steps
                if (Fields[nameof(delaySeconds)].guiActiveEditor)
                    negChangeHappened = true;

                Fields[nameof(delaySeconds)].guiActiveEditor = false;
                Fields[nameof(delayMinutes)].guiActiveEditor = false;
                Fields[nameof(totalDelay)].guiActiveEditor = false;
                Fields[nameof(delayMode)].guiActiveEditor = false;
                Fields[nameof(eventMessagingWanted)].guiActiveEditor = false;
            }

            //Only hop in hear if change happened in this mod. Else we break the sliders every time we call for a PAW refresh
            if (negChangeHappened)
            {
                negChangeHappened = false;
                //refresh the PAW to its new size
                MonoUtilities.RefreshPartContextWindow(part);
            }
        }

        //Gets called by the GameEvent when the rocket is launched
        private void isLaunched(EventReport report)
        {
            //Set the launch time
            launchTime = Planetarium.GetUniversalTime();

            if (delayMode == "Post Launch")
            {
                StartCoroutine(coroutinePostLaunch());
            }
            else if (delayMode == "Pre Apside")
            {
                StartCoroutine(coroutinePreApsideWait());
            }
            if (eventMessagingWanted)
                StartCoroutine(coroutinePrintMessage());

        }

        //Gets called every .1 seconds and counts down to 0 after launch
        IEnumerator coroutinePostLaunch()
        {
            for (; ; )
            {
                //Calculate how long until the engine ignites
                PAWtimeToDecouple = (launchTime + totalDelay) - Planetarium.GetUniversalTime();

                if (PAWtimeToDecouple <= 0)
                {
                    decoupleStage();
                    endMod();
                    StopCoroutine(coroutinePostLaunch());
                    yield break;
                }

                yield return new WaitForSeconds(.1f);
            }
        }
        
        //Gets called every 5 seconds to check if the vessel is suborbital, then starts the countdown
        IEnumerator coroutinePreApsideWait()
        {
            for (; ; )
            {
                if (vessel.situation == Vessel.Situations.SUB_ORBITAL)
                {
                    StartCoroutine(coroutinePreApside());
                    StopCoroutine(coroutinePreApsideWait());
                    yield break;
                }

                yield return new WaitForSeconds(5.0f);
            }
        }
        
        //Gets called every .1 seconds and counts down to 0 leading up to the next Apside
        IEnumerator coroutinePreApside()
        {
            for (; ; )
            {
                //Calculate how long until the engine ignites
                PAWtimeToDecouple = vessel.orbit.timeToAp - totalDelay;

                if (PAWtimeToDecouple <= 0)
                {
                    decoupleStage();
                    yield break;
                }

                yield return new WaitForSeconds(0.1f);
            }
        }
        
        //This function will write all the messages on the screen
        IEnumerator coroutinePrintMessage()
        {
            for (; ; )
            {
                //Now to check if we are not on the launch pad
                if (vessel.situation != Vessel.Situations.PRELAUNCH)
                {
                    //Time to announce the upcoming ignition event
                    if (nextMessageStep == 0 && PAWtimeToDecouple <= 10)
                    {
                        ScreenMessages.PostScreenMessage("Decoupling " + stage + " in 10 seconds.", 4.5f, ScreenMessageStyle.UPPER_CENTER);
                        nextMessageStep++;
                    }
                    else if (nextMessageStep == 1 && PAWtimeToDecouple <= 5)
                    {
                        ScreenMessages.PostScreenMessage("Decoupling " + stage + " in 5 seconds.", 2.5f, ScreenMessageStyle.UPPER_CENTER);
                        nextMessageStep++;
                    }
                    else if (nextMessageStep == 2 && PAWtimeToDecouple <= 2)
                    {
                        ScreenMessages.PostScreenMessage("Decoupling " + stage + " in 2 seconds.", 1.5f, ScreenMessageStyle.UPPER_CENTER);
                        nextMessageStep++;
                    }
                    else if (nextMessageStep == 3 && PAWtimeToDecouple <= 0)
                    {
                        ScreenMessages.PostScreenMessage("Decoupling " + stage, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        nextMessageStep++;
                        yield break;
                    }
                }
                yield return new WaitForSeconds(0.2f);
            }
        }

        //Decouples the stage
        private void decoupleStage()
        {
            part.decouple();
            //Hide the timeToDecouple once the stage is decoupled
            Fields[nameof(PAWtimeToDecouple)].guiActive = false;

        }

        //Hide all the fields
        private void endMod()
        {
            if (modInUse)
            {
                modInUse = false;
                PAWmodInUse = StringDisconnected;
                //Disable all text for inFlight Information
                Fields[nameof(PAWtimeToDecouple)].guiActive = false;
                Fields[nameof(PAWdelayMode)].guiActive = false;
                Fields[nameof(PAWstage)].guiActive = false;

                //Update the size of the PAW
                MonoUtilities.RefreshPartContextWindow(part);
            }
        }

        //Gets called when the part explodes etc.
        private void isDead(Part part)
        {
            //Stopping all the coroutines that might be running
            StopCoroutine(coroutinePostLaunch());
            StopCoroutine(coroutinePreApsideWait());
            StopCoroutine(coroutinePreApside());
            StopCoroutine(coroutinePrintMessage());
        }
        #endregion
    }

    public class BenjisDelayedIgniter : PartModule//ModuleEngines*
    {
        #region Fields

        //Saving UniversalTime into launchTime when the Vessel get's launched
        [KSPField(isPersistant = true, guiActive = false)]
        private double launchTime = 0;

        //Check the derivative of the eccentricity for circularizing
        [KSPField(isPersistant = true, guiActive = false)]
        private bool eccRising = false;
        [KSPField(isPersistant = true, guiActive = false)]
        private double tempEcc = 0.0;

        //Catch the slider dragging "bug"
        [KSPField(isPersistant = true, guiActive = false)]
        private bool negChangeHappened = false;

        //Headline name for the GUI
        [KSPField(isPersistant = true, guiActive = false)]
        private const string PAWIgniterGroupName = "Benji's Delayed Igniter";

        //Text, if functionality is disabled/enabled
        [KSPField(isPersistant = true, guiActive = false)]
        private const string StringDisconnected = "disconnected";

        [KSPField(isPersistant = true, guiActive = false)]
        private const string StringConnected = "connected";

        //Text, if event messaging is disabled/enabled
        [KSPField(isPersistant = true, guiActive = false)]
        private const string StringInactive = "inactive";

        [KSPField(isPersistant = true, guiActive = false)]
        private const string StringActive = "active";

        //The PAW fields in the editor
        //A button to enable or disable the module
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Circuits are:", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName),
            UI_Toggle(disabledText = StringDisconnected, enabledText = StringConnected)]
        private bool modInUse = false;

        //Toggle the mode between post launch delay and a pre Apside delay
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Delaymode", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName),
            UI_ChooseOption(options = new string[2] { "Post Launch", "Pre Apside" })]
        private string delayMode = "Post Launch";

        //A button to enable the Cut-Off at Apogee
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Cut-Off @ Apogee:", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName),
            UI_Toggle(disabledText = StringInactive, enabledText = StringActive)]
        private bool cutAtApogee = false;

        //Set which initial apogee the stage should aim for
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Apogee [km]", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 70f, maxValue = 450000f, incrementLarge = 1000f, incrementSmall = 100f, incrementSlide = 10f, sigFigs = 0)]
        private float targetApogee = 450;

        //Specify the delay in seconds in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Delay [sec]", guiFormat = "F1", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 59.9f, incrementLarge = 10f, incrementSmall = 1f, incrementSlide = 0.1f, sigFigs = 1)]
        private float delaySeconds = 0;

        //Specify the delay in minutes in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Delay [min]", guiFormat = "F1", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 30f, incrementLarge = 5f, incrementSmall = 1f, incrementSlide = 1f, sigFigs = 1)]
        private float delayMinutes = 0;

        //Seconds and Minutes (*60) added
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Total Delay", guiUnits = "sec", guiFormat = "F1", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName)]
        private float totalDelay = 0;

        //Name what the engine will be called during event messaging
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Engine", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName),
            UI_ChooseOption(options = new string[10] { "1st Stage", "2nd Stage", "3rd Stage", "4th Stage", "Booster", "Separation-Motor", "Spin-Motor", "Ullage-Motor", "Spin-/Ullage-Motor", "Apogee Kick Stage" })]
        private string engineType = "1st Stage";

        //Specify how to treat the kick stage
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Kick Stage Mode", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName),
            UI_ChooseOption(options = new string[3] { "Burn-Out", "Cut-Off", "Circularize" })]
        private string apKickMode = "Burn-Out";

        //Set which apside the kick stage should aim for
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Apside [km]", guiFormat = "F1", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 70f, maxValue = 450000f, incrementLarge = 1000f, incrementSmall = 100f, incrementSlide = 10f, sigFigs = 0)]
        private float targetApside = 0;

        //The PAW fields in Flight
        //Shows if the igniter is active
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Circuits are", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName)]
        private string PAWmodInUse;
        //Shows the time until the engine is activated in seconds, one decimal
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Seconds until Ignition", guiUnits = "sec", guiFormat = "F1", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName)]
        private double PAWtimeToIgnite = 0;
        //Shows what delay mode this engine is in
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Delaymode", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName)]
        private string PAWdelayMode;
        //Shows if the stage will cut at Apogee
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Cut @ Apogee", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName)]
        private string PAWcutAtApogee;
        //Shows what Apogee we aim for
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Target Apogee", guiUnits = "km", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName)]
        private string PAWtargetApogee;
        //Shows what type of engine this is
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Engine", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName)]
        private string PAWengine;
        //Shows what type of mode this Kick Stage operates in
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Kick Stage Mode", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName)]
        private string PAWkickMode;
        //Shows what Apside we aim for
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Target Apside", guiUnits = "km", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName)]
        private string PAWtargetApside;

        //Shown in the Editor and in Flight
        //A button to enable or disable if a message for this event will be shown
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Event Messaging:", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName),
            UI_Toggle(disabledText = StringInactive, enabledText = StringActive)]
        private bool eventMessagingWanted = true;

        //A small variable to manage the onScreen Messages
        private char nextMessageStep = (char)0;

        #endregion

        #region Overrides

        //This happens once in both EDITOR and FLIGHT
        public override void OnStart(StartState state)
        {
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
                initMod();

            if (HighLogic.LoadedScene == GameScenes.EDITOR)
                initEditor();

            //Need to call that, in case other mods do stuff here
            base.OnStart(state);
        }

        //Initialize all the fields when in FLIGHT
        private void initMod()
        {
            //Wait a bit to avoid the splashed bug, where the vesel can enter/stay in SPLASHED situation if something is done too early (before first physics tick)
            Thread.Sleep(200);

            //Now to check if we are on the launch pad
            if (vessel.situation == Vessel.Situations.PRELAUNCH)
            {
                //Add up the two parts of the overall delay and show me the numbers
                PAWtimeToIgnite = totalDelay = delaySeconds + (delayMinutes * 60f);

                //Set the visible PAW variable 
                if (modInUse)
                {
                    PAWmodInUse = StringConnected;
                    //Set the text for inFlight Information
                    PAWdelayMode = delayMode;
                    if (delayMode == "Post Launch")
                    {
                        if (cutAtApogee)
                        {
                            PAWcutAtApogee = StringActive;
                            PAWtargetApogee = string.Format("{0:N0}", targetApogee);
                        }
                        else
                        {
                            Fields[nameof(PAWcutAtApogee)].guiActive = false;
                            Fields[nameof(PAWtargetApogee)].guiActive = false;
                        }
                    }
                    else if (delayMode == "Pre Apside")
                    {
                        Fields[nameof(PAWcutAtApogee)].guiActive = false;
                        Fields[nameof(PAWtargetApogee)].guiActive = false;
                    }

                    PAWengine = engineType;
                    //Show the kick mode if it is a kick stage
                    if (engineType == "Apogee Kick Stage")
                        PAWkickMode = apKickMode;
                    //or hide it
                    else
                        Fields[nameof(PAWkickMode)].guiActive = false;
                    //Show the targeted Apside if in Cut-Off mode
                    if (apKickMode == "Cut-Off")
                        PAWtargetApside = string.Format("{0:N0}", targetApside);
                    //else hide it
                    else
                        Fields[nameof(PAWtargetApside)].guiActive = false;

                    GameEvents.onLaunch.Add(isLaunched);
                    GameEvents.onPartDie.Add(isDead);
                }
                else
                {
                    PAWmodInUse = StringDisconnected;
                    //Disable all text for inFlight Information
                    Fields[nameof(PAWtimeToIgnite)].guiActive = false;
                    Fields[nameof(PAWdelayMode)].guiActive = false;
                    Fields[nameof(PAWengine)].guiActive = false;
                    Fields[nameof(PAWkickMode)].guiActive = false;
                    Fields[nameof(PAWtargetApside)].guiActive = false;
                }
            }
        }

        //Initialize all the fields when in EDITOR
        private void initEditor()
        {
            //refresh the PAW to its new size
            //We need to do this once, in case the mod is active in a saved ship
            updateEditorPAW(null);

            GameEvents.onEditorShipModified.Add(updateEditorPAW);
        }

        //Tweak what fields are shown in the editor
        private void updateEditorPAW(ShipConstruct ship)
        {
            if (modInUse)
            {
                totalDelay = delaySeconds + (delayMinutes * 60f);

                Fields[nameof(delaySeconds)].guiActiveEditor = true;
                Fields[nameof(delayMinutes)].guiActiveEditor = true;
                Fields[nameof(totalDelay)].guiActiveEditor = true;

                Fields[nameof(delayMode)].guiActiveEditor = true;

                if (delayMode == "Post Launch")
                {
                    Fields[nameof(cutAtApogee)].guiActiveEditor = true;
                    if (cutAtApogee)
                    {
                        Fields[nameof(targetApogee)].guiActiveEditor = true;
                    }
                    else
                    {
                        //If this gui is visible now, then we seem to change this in the next steps
                        if (Fields[nameof(targetApogee)].guiActiveEditor)
                            negChangeHappened = true;

                        Fields[nameof(targetApogee)].guiActiveEditor = false;
                    }
                }
                else
                {
                    //If this gui is visible now, then we seem to change this in the next steps
                    if (Fields[nameof(targetApogee)].guiActiveEditor)
                        negChangeHappened = true;

                    Fields[nameof(cutAtApogee)].guiActiveEditor = false;
                    Fields[nameof(targetApogee)].guiActiveEditor = false;
                }
                Fields[nameof(engineType)].guiActiveEditor = true;
                //Show the Kick Stage Mode if the engine will be a kick stage
                if (engineType == "Apogee Kick Stage")
                {
                    Fields[nameof(apKickMode)].guiActiveEditor = true;

                    //Show the targeted Apside if in Cut-Off mode
                    if (apKickMode == "Cut-Off")
                    {
                        Fields[nameof(targetApside)].guiActiveEditor = true;
                    }
                    //else hide it
                    else
                    {
                        //If this gui is visible now, then we seem to change this in the next steps
                        if (Fields[nameof(targetApside)].guiActiveEditor)
                            negChangeHappened = true;

                        Fields[nameof(targetApside)].guiActiveEditor = false;
                    }
                }
                else
                {
                    Fields[nameof(apKickMode)].guiActiveEditor = false;
                    Fields[nameof(targetApside)].guiActiveEditor = false;
                }
                Fields[nameof(eventMessagingWanted)].guiActiveEditor = true;
            }
            else
            {
                //If this gui or any other is visible now, then we seem to change this in the next steps
                if (Fields[nameof(delaySeconds)].guiActiveEditor)
                    negChangeHappened = true;

                Fields[nameof(delaySeconds)].guiActiveEditor = false;
                Fields[nameof(delayMinutes)].guiActiveEditor = false;
                Fields[nameof(totalDelay)].guiActiveEditor = false;
                Fields[nameof(delayMode)].guiActiveEditor = false;
                Fields[nameof(cutAtApogee)].guiActiveEditor = false;
                Fields[nameof(targetApogee)].guiActiveEditor = false;
                Fields[nameof(engineType)].guiActiveEditor = false;
                Fields[nameof(apKickMode)].guiActiveEditor = false;
                Fields[nameof(targetApside)].guiActiveEditor = false;
                Fields[nameof(eventMessagingWanted)].guiActiveEditor = false;
            }

            //Only hop in hear if change happened in this mod. Else we break the sliders every time we call for a PAW refresh
            if (negChangeHappened)
            {
                negChangeHappened = false;
                //refresh the PAW to its new size
                MonoUtilities.RefreshPartContextWindow(part);
            }
        }

        //Gets called by the GameEvent when the rocket is launched
        private void isLaunched(EventReport report)
        {
            //Set the launch time
            launchTime = Planetarium.GetUniversalTime();

            if (delayMode == "Post Launch")
            {
                StartCoroutine(coroutinePostLaunch());
            }
            else if (delayMode == "Pre Apside")
            {
                StartCoroutine(coroutinePreApsideWait());
            }
            if (eventMessagingWanted)
                StartCoroutine(coroutinePrintMessage());

        }

        //Gets called every .1 seconds and counts down to 0 after launch
        IEnumerator coroutinePostLaunch()
        {
            for (;;)
            {
                //Calculate how long until the engine ignites
                PAWtimeToIgnite = (launchTime + totalDelay) - Planetarium.GetUniversalTime();

                if (PAWtimeToIgnite <= 0)
                {
                    igniteEngine();
                    endMod();
                    StopCoroutine(coroutinePostLaunch());
                    yield break;
                }

                yield return new WaitForSeconds(.1f);
            }
        }

        //Gets called every 5 seconds to check if the vessel is suborbital, then starts the countdown
        IEnumerator coroutinePreApsideWait()
        {
            for (;;)
            {
                if (vessel.situation == Vessel.Situations.SUB_ORBITAL)
                {
                    StartCoroutine(coroutinePreApside());
                    StopCoroutine(coroutinePreApsideWait());
                    yield break;
                }

                yield return new WaitForSeconds(5.0f);
            }
        }

        //Gets called every .1 seconds and counts down to 0 leading up to the next Apside
        IEnumerator coroutinePreApside()
        {
            for (;;)
            {
                //Calculate how long until the engine ignites
                PAWtimeToIgnite = vessel.orbit.timeToAp - totalDelay;

                if (PAWtimeToIgnite <= 0)
                {
                    //If this engine is a Kick Stage that needs to be cut off at a specific Apside, we need to check the current Apsides and the target-Apside, to see if we need to cut at Peri or Ago
                    if (engineType == "Apogee Kick Stage")
                    {
                        if (apKickMode == "Cut-Off")
                        {
                            //targetApside will be the new Apo
                            if ((vessel.orbit.ApA / 1000) <= targetApside)
                            {
                                StartCoroutine(coroutinePreApsideCutAtApogee());
                                StopCoroutine(coroutinePreApside());
                            }
                            //targetApside will (still) be the Peri
                            else
                            {
                                StartCoroutine(coroutinePreApsideCutAtPerigee());
                                StopCoroutine(coroutinePreApside());
                            }
                        }
                        //If this engine is a Kick Stage that tries to circularize, we need to check the current Apsides in mind, because checks like this...
                        //...vessel.orbit.PeA >= vessel.orbit.ApA...
                        //...vessel.orbit.PeA == vessel.orbit.ApA...
                        //...will of course not work. Dummy me.
                        else if (apKickMode == "Circularize")
                        {
                            //Keep the current eccentricity in mind
                            tempEcc = vessel.orbit.eccentricity;
                            StartCoroutine(coroutinePreApsideCircularize());
                            StopCoroutine(coroutinePreApside());
                        }
                        else
                        {
                            StopCoroutine(coroutinePreApside());
                        }
                    }

                    igniteEngine();

                    yield break;
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        //Cut the engine when the targetApside matches with vessel Apogee
        IEnumerator coroutinePreApsideCutAtApogee()
        {
            for (;;)
            {
                if ((vessel.orbit.ApA / 1000) >= targetApside)
                {
                    //...cut the engine
                    FlightInputHandler.state.mainThrottle = 0;
                    endMod();
                    //Does the user want messages?
                    if (eventMessagingWanted)
                    {
                        //Showing the engine cutt-off message
                        ScreenMessages.PostScreenMessage("Cutting " + engineType + " at an Apogee of " + (int)(vessel.orbit.ApA / 1000) + " km. (Target: " + targetApside + " km)", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    yield break;
                }
                yield return new WaitForSeconds(0.1f);
            }
        }

        //Cut the engine when the targetApside matches with vessel Perigee
        IEnumerator coroutinePreApsideCutAtPerigee()
        {
            for (;;)
            {
                if ((vessel.orbit.PeA / 1000) >= targetApside)
                {
                    //...cut the engine
                    FlightInputHandler.state.mainThrottle = 0;
                    endMod();
                    //Does the user want messages?
                    if (eventMessagingWanted)
                    {
                        //Showing the engine cutt-off message
                        ScreenMessages.PostScreenMessage("Cutting " + engineType + " at an Perigee of " + (int)(vessel.orbit.PeA / 1000) + " km. (Target: " + targetApside + " km)", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    yield break;
                }
                yield return new WaitForSeconds(0.1f);
            }
        }


        //Cut the engine when the orbit is as circular as possible
        IEnumerator coroutinePreApsideCircularize()
        {
            for (;;)
            {
                //Once the Kick Stage is burning the eccentricity will fall,
                //but once the orbit was circular and the burn continues the eccentricity will rise again
                if (vessel.orbit.eccentricity > tempEcc)
                {
                    eccRising = true;
                }
                else
                {
                    tempEcc = vessel.orbit.eccentricity;
                }

                if (eccRising)
                {
                    //...cut the engine
                    FlightInputHandler.state.mainThrottle = 0;
                    endMod();

                    //Does the user want messages?
                    if (eventMessagingWanted)
                    {
                        //Showing the engine cutt-off message
                        ScreenMessages.PostScreenMessage("Cutting " + engineType + " at " + (int)(vessel.orbit.PeA / 1000) + "x" + (int)(vessel.orbit.ApA / 1000) + ".", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    yield break;
                }
                yield return new WaitForSeconds(0.1f);
            }
        }
        //This function will write all the messages on the screen

        IEnumerator coroutinePrintMessage()
        {
            for (; ; )
            {
                //Now to check if we are on the launch pad
                if (vessel.situation != Vessel.Situations.PRELAUNCH)
                {
                    //Time to announce the upcoming ignition event
                    if (nextMessageStep == 0 && PAWtimeToIgnite <= 10)
                    {
                        ScreenMessages.PostScreenMessage("Igniting " + engineType + " in 10 seconds.", 4.5f, ScreenMessageStyle.UPPER_CENTER);
                        nextMessageStep++;
                    }
                    else if (nextMessageStep == 1 && PAWtimeToIgnite <= 5)
                    {
                        ScreenMessages.PostScreenMessage("Igniting " + engineType + " in 5 seconds.", 2.5f, ScreenMessageStyle.UPPER_CENTER);
                        nextMessageStep++;
                    }
                    else if (nextMessageStep == 2 && PAWtimeToIgnite <= 2)
                    {
                        ScreenMessages.PostScreenMessage("Igniting " + engineType + " in 2 seconds.", 1.5f, ScreenMessageStyle.UPPER_CENTER);
                        nextMessageStep++;
                    }
                    else if (nextMessageStep == 3 && PAWtimeToIgnite <= 0)
                    {
                        ScreenMessages.PostScreenMessage("Igniting " + engineType, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        nextMessageStep++;
                        yield break;
                    }
                }
                yield return new WaitForSeconds(0.2f);
            }
        }

        //Ignite the engine
        private void igniteEngine()
        {
            //Starts the engine
            part.force_activate();
            //Hide the timeToIgnition once the engine burns
            Fields[nameof(PAWtimeToIgnite)].guiActive = false;

        }

        //Hide all the fields
        private void endMod()
        {
            if (modInUse)
            {
                modInUse = false;
                PAWmodInUse = StringDisconnected;
                //Disable all text for inFlight Information
                Fields[nameof(PAWtimeToIgnite)].guiActive = false;
                Fields[nameof(PAWdelayMode)].guiActive = false;
                Fields[nameof(PAWcutAtApogee)].guiActive = false;
                Fields[nameof(PAWtargetApogee)].guiActive = false;
                Fields[nameof(PAWengine)].guiActive = false;
                Fields[nameof(PAWkickMode)].guiActive = false;
                Fields[nameof(PAWtargetApside)].guiActive = false;
                Fields[nameof(eventMessagingWanted)].guiActive = false;

                //Update the size of the PAW
                MonoUtilities.RefreshPartContextWindow(part);
            }
        }

        //Gets called when the part explodes etc.
        private void isDead(Part part)
        {
            //Stopping all the coroutines that might be running
            StopCoroutine(coroutinePostLaunch());
            StopCoroutine(coroutinePreApsideWait());
            StopCoroutine(coroutinePreApside());
            StopCoroutine(coroutinePreApsideCircularize());
            StopCoroutine(coroutinePreApsideCutAtApogee());
            StopCoroutine(coroutinePreApsideCutAtPerigee());
            StopCoroutine(coroutinePrintMessage());
        }
        #endregion
    }

    public class BenjisDelayedRCS : PartModule//ModuleRCS*
    {
        #region Fields

        //Saving UniversalTime into launchTime when the Vessel get's launched
        [KSPField(isPersistant = true, guiActive = false)]
        private double launchTime = 0;

        //Did the countdown start?
        [KSPField(isPersistant = true, guiActive = false)]
        private bool activated = false;

        //Headline name for the GUI
        [KSPField(isPersistant = true, guiActive = false)]
        private const string PAWIgniterGroupName = "Benji's Delayed RCS";

        //Text, if functionality is disabled/enabled
        [KSPField(isPersistant = true, guiActive = false)]
        private const string StringDisconnected = "disconnected";

        [KSPField(isPersistant = true, guiActive = false)]
        private const string StringConnected = "connected";

        //Text, if event messaging is disabled/enabled
        [KSPField(isPersistant = true, guiActive = false)]
        private const string StringInactive = "inactive";

        [KSPField(isPersistant = true, guiActive = false)]
        private const string StringActive = "active";

        //The PAW fields in the editor
        //A button to enable or disable the module
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Circuits are:", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName),
            UI_Toggle(disabledText = StringDisconnected, enabledText = StringConnected)]
        private bool modInUse = false;

        //Toggle the mode between post launch delay and a pre Apside delay
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Delaymode", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName),
            UI_ChooseOption(options = new string[2] { "Post Launch", "Pre Apside" })]
        private string delayMode = "Post Launch";

        //Specify the delay in seconds in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Delay [s]", guiFormat = "F1", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName),
        UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 59.9f, incrementLarge = 10f, incrementSmall = 1f, incrementSlide = 0.1f, sigFigs = 1)]
        private float delaySeconds = 0;

        //Specify the delay in minutes in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Delay [min]", guiFormat = "F1", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName),
        UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 30f, incrementLarge = 5f, incrementSmall = 1f, incrementSlide = 1f, sigFigs = 1)]
        private float delayMinutes = 0;

        //Seconds and Minutes (*60) added
        [KSPField(isPersistant = true, guiActive = false)]
        private float totalDelay = 0;

        //A button to enable or disable if a message for this event will be shown
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Event Messaging:", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName),
            UI_Toggle(disabledText = StringInactive, enabledText = StringActive)]
        private bool eventMessagingWanted = true;

        //The PAW fields in Flight
        //Shows if RCS is active
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Circuits are", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName)]
        private string PAWmodInUse;
        //Shows the time until RCS gets activated in seconds, one decimal
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Seconds until Activation", guiUnits = "s", guiFormat = "F1", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName)]
        private double PAWtimeToActivate = 0;
        //Shows what delay mode this RCS is in
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Delaymode", groupName = PAWIgniterGroupName, groupDisplayName = PAWIgniterGroupName)]
        private string PAWdelayMode;

        //A small variable to manage the onScreen Messages
        private char nextMessageStep = (char)0;

        #endregion

        #region Overrides

        private void initMod()
        {
            //Wait a bit to avoid the splashed bug, where the vesel can enter/stay in SPLASHED situation if something is done too early (before first physics tick)
            Thread.Sleep(200);
            //Now to check if we are on the launch pad
            if (vessel.situation == Vessel.Situations.PRELAUNCH)
            {
                //Add up the two parts of the overall delay and show me the numbers
                PAWtimeToActivate = totalDelay = delaySeconds + (delayMinutes * 60f);

                //Set the visible PAW variable 
                if (modInUse)
                {
                    PAWmodInUse = StringConnected;
                    //Set the text for inFlight Information
                    PAWdelayMode = delayMode;
                }
                else
                {
                    PAWmodInUse = StringDisconnected;
                    //Disable all text for inFlight Information
                    Fields[nameof(PAWtimeToActivate)].guiActive = false;
                    Fields[nameof(PAWdelayMode)].guiActive = false;
                }

            }
        }

        private void endMod()
        {
            modInUse = false;
            PAWmodInUse = StringDisconnected;
            //Disable all text for inFlight Information
            Fields[nameof(PAWtimeToActivate)].guiActive = false;
            Fields[nameof(PAWdelayMode)].guiActive = false;
        }

        //This happens once
        public override void OnStart(StartState state)
        {
            initMod();
            //Need to call that, in case other mods do stuff here
            base.OnStart(state);
        }

        //This happens every visual frame
        public override void OnUpdate()
        {
            //Only do crazy stuff when inFlight
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                if (modInUse)
                {
                    if (!activated)
                    {
                        //Check if the Vessel is still attached to the launch clamps
                        if (launchTime == 0 && vessel.missionTime > 0)
                        {
                            //Set the launch time
                            launchTime = Planetarium.GetUniversalTime();
                        }

                        if (delayMode == "Post Launch")
                        {
                            //Calculate how long until RCS is activated
                            PAWtimeToActivate = (launchTime + totalDelay) - Planetarium.GetUniversalTime();
                        }
                        else if (delayMode == "Pre Apside")
                        {
                            if (vessel.situation == Vessel.Situations.SUB_ORBITAL)
                            {
                                //Calculate how long until RCS is activated
                                PAWtimeToActivate = vessel.orbit.timeToAp - totalDelay;
                            }
                        }

                        //Does the user want messages?
                        if (eventMessagingWanted)
                        {
                            //Time to announce the upcoming activation event
                            if (nextMessageStep == 0 && PAWtimeToActivate <= 10)
                            {
                                ScreenMessages.PostScreenMessage("Activating RCS in 10 seconds.", 4.5f, ScreenMessageStyle.UPPER_CENTER);
                                nextMessageStep++;
                            }
                            else if (nextMessageStep == 1 && PAWtimeToActivate <= 5)
                            {
                                ScreenMessages.PostScreenMessage("Activating RCS in 5 seconds.", 2.5f, ScreenMessageStyle.UPPER_CENTER);
                                nextMessageStep++;
                            }
                            else if (nextMessageStep == 2 && PAWtimeToActivate <= 2)
                            {
                                ScreenMessages.PostScreenMessage("Activating RCS in 2 seconds.", 1.5f, ScreenMessageStyle.UPPER_CENTER);
                                nextMessageStep++;
                            }
                        }

                        //If it's time to activate...
                        if (PAWtimeToActivate <= 0)
                        {
                            //...do it already
                            part.force_activate();
                            activated = true;
                            //Hide the timeToActivation
                            Fields[nameof(PAWtimeToActivate)].guiActive = false;
                            //Does the user want messages?
                            if (eventMessagingWanted)
                            {
                                //Showing the actual activation message
                                ScreenMessages.PostScreenMessage("Activating RCS.", 3f, ScreenMessageStyle.UPPER_CENTER);
                            }
                        }
                    }
                    //if RCS is activated
                    else
                    {
                        PAWtimeToActivate = 0;
                    }
                }
            }
            //As far as I know I don't need to do this, because I didn't do it before and nothing exploded.
            //But maybe it's a good idea to start calling it (for other mods?)
            //Need to call that, in case other mods do stuff here
            base.OnUpdate();
        }

        #endregion
    }

    public class BenjisFairingSeparator : PartModule//ProceduralFairingDecoupler
    {
        #region Fields

        //Headline name for the GUI
        [KSPField(isPersistant = true, guiActive = false)]
        private const string PAWFairingGroupName = "Benji's Fairing Separator";

        //Text, if functionality is disabled/enabled
        [KSPField(isPersistant = true, guiActive = false)]
        private const string StringDisconnected = "disconnected";

        [KSPField(isPersistant = true, guiActive = false)]
        private const string StringConnected = "connected";

        //Text, if event messaging is disabled/enabled
        [KSPField(isPersistant = true, guiActive = false)]
        private const string StringInactive = "inactive";

        [KSPField(isPersistant = true, guiActive = false)]
        private const string StringActive = "active";


        //The PAW fields in the editor
        //A button to enable or disable the function
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Circuits are:", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName),
            UI_Toggle(disabledText = StringDisconnected, enabledText = StringConnected)]
        private bool modInUse = false;

        //Specify the Height in kilometers in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Height [km]", guiFormat = "F0", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName),
        UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 200f, incrementLarge = 10f, incrementSmall = 1f, incrementSlide = 1f, sigFigs = 0)] //140km - that's where the atmosphere ends
        private float PAWeditorHeightToSeparate = 60;

        //A button to enable or disable if a message for this event will be shown
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Event Messaging:", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName),
            UI_Toggle(disabledText = StringInactive, enabledText = StringActive)]
        private bool eventMessagingWanted = false;

        //A button to enable or disable if a message for this event will be shown
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Jettison", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName),
            UI_ChooseOption(options = new string[2] { "Payload", "Interstage" })]
        private string PAWfairing = "Payload";


        //The PAW fields in Flight
        //Shows if the fairing is active
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Circuits are", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName)]
        private string PAWmodInUse;

        //Shows the Height in kilometers at which the fairing gets separated
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Height to Separate", guiUnits = "km", guiFormat = "F0", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName)]
        private float PAWflightHeightToSeparate = 0;

        #endregion


        #region Overrides

        private void initMod()
        {
            //Wait a bit to avoid the splashed bug, where the vesel can enter/stay in SPLASHED situation if something is done too early (before first physics tick)
            Thread.Sleep(200);
            //Now to check if we are on the launch pad
            if (vessel.situation == Vessel.Situations.PRELAUNCH)
            {
                //enum of Situations - https://kerbalspaceprogram.com/api/class_vessel.html
                if (vessel.situation == Vessel.Situations.PRELAUNCH)
                {
                    //Show me the numbers
                    PAWflightHeightToSeparate = PAWeditorHeightToSeparate;

                    //Set the visible PAW variable 
                    if (modInUse)
                        PAWmodInUse = StringConnected;
                    else
                    {
                        PAWmodInUse = StringDisconnected;
                        Fields[nameof(PAWflightHeightToSeparate)].guiActive = false;
                    }
                }
            }
        }
        private void endMod()
        {
            modInUse = false;
            PAWmodInUse = StringDisconnected;
            //Disable all text for inFlight Information
            Fields[nameof(PAWflightHeightToSeparate)].guiActive = false;
        }

        //This happens once
        public override void OnStart(StartState state)
        {
            initMod();
            //Need to call that, in case other mods do stuff here
            base.OnStart(state);
        }

        //This happens every visual frame
        public override void OnUpdate()
        {
            //Check if the Vessel is still attached to the launch clamps
            if (modInUse && vessel.missionTime > 0)
            {
                //Are we high enough to separate...
                if (vessel.orbit.altitude >= (PAWflightHeightToSeparate * 1000f))
                {
                    //Does the user want messages?
                    if (eventMessagingWanted)
                    {
                        //Showing the jettison message
                        ScreenMessages.PostScreenMessage("Jettisoning " + PAWfairing + "-fairing.", 3f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    
                    //...do it already
                    part.decouple();
                    endMod();
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

        //Text, if functionality is disabled/enabled
        [KSPField(isPersistant = true, guiActive = false)]
        private const string StringDisconnected = "disconnected";

        [KSPField(isPersistant = true, guiActive = false)]
        private const string StringConnected = "connected";

        //Text, if event messaging is disabled/enabled
        [KSPField(isPersistant = true, guiActive = false)]
        private const string StringInactive = "inactive";

        [KSPField(isPersistant = true, guiActive = false)]
        private const string StringActive = "active";

        //A button to enable or disable the function
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Circuits are:", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName),
            UI_Toggle(disabledText = StringDisconnected, enabledText = StringConnected)]
        private bool modInUse = false;

        //Specify the Height in kilometers in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Height [km]", guiFormat = "F0", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName),
        UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 140f, incrementLarge = 10f, incrementSmall = 1f, incrementSlide = 1f, sigFigs = 0)] //140km - that's where the atmosphere ends
        private float editorHeightToSeparate = 0;

        //Shows if the fairing is active
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Circuits are", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName)]
        private string PAWmodInUse;

        //Shows the Height in kilometers at which the fairing gets separated
        [KSPField(isPersistant = true, guiActiveEditor = false,  guiActive = true, guiName = "Height [km] to Separate", guiFormat = "F0", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName)]
        private float flightHeightToSeparate = 0;

        //A button to enable or disable if a message for this event will be shown
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Event Messaging:", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName),
            UI_Toggle(disabledText = StringInactive, enabledText = StringActive)]
        private bool eventMessagingWanted = true;

        //A button to enable or disable if a message for this event will be shown
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Jettison", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName),
            UI_ChooseOption(options = new string[3] {"Payload", "3rd stage", "2nd stage"})]
        private string eventMessage = "Payload";

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
                if (modInUse)
                    PAWmodInUse = StringConnected;
                else
                    PAWmodInUse = StringDisconnected;
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
                    if (vessel.missionTime > 0)
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
                    if (vessel.orbit.altitude >= (flightHeightToSeparate * 1000f))
                    {
                        //Does the user want messages?
                        if (eventMessagingWanted)
                        {
                        //Showing the Jettison message
                        ScreenMessages.PostScreenMessage("Jettison " + eventMessage + " fairing.", 3f, ScreenMessageStyle.UPPER_CENTER);
                        }

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