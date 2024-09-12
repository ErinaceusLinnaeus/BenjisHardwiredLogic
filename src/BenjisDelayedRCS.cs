using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace BenjisHardwiredLogic
{
    public class BenjisDelayedRCS : PartModule //ModuleRCSFX
    {
        #region Fields

        //Keeping track of what coroutine is running at the moment
        [KSPField(isPersistant = true, guiActive = false)]
        private int activeCoroutine = 0;

        //Saving UniversalTime into launchTime when the Vessel gets launched
        [KSPField(isPersistant = true, guiActive = false)]
        private double launchTime = 0;

        //Catch the slider dragging "bug"
        [KSPField(isPersistant = true, guiActive = false)]
        private bool negChangeHappened = false;

        //Headline name for the GUI
        [KSPField(isPersistant = true, guiActive = false)]
        private const string PAWRCSGroupName = "Benji's Delayed RCS";

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
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Circuits are:", groupName = PAWRCSGroupName, groupDisplayName = PAWRCSGroupName),
            UI_Toggle(disabledText = StringDisconnected, enabledText = StringConnected)]
        private bool modInUse = false;

        //Toggle the mode between post launch delay and a pre Apside delay
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Delaymode", groupName = PAWRCSGroupName, groupDisplayName = PAWRCSGroupName),
            UI_ChooseOption(options = new string[2] { "Post Launch", "Pre Apside" })]
        private string delayMode = "Post Launch";

        //Specify the delay in seconds in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Delay [s]", guiFormat = "F1", groupName = PAWRCSGroupName, groupDisplayName = PAWRCSGroupName),
        UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 59.9f, incrementLarge = 10f, incrementSmall = 1f, incrementSlide = 0.1f, sigFigs = 1)]
        private float delaySeconds = 0;

        //Specify the delay in minutes in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Delay [min]", guiFormat = "F1", groupName = PAWRCSGroupName, groupDisplayName = PAWRCSGroupName),
        UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 30f, incrementLarge = 5f, incrementSmall = 1f, incrementSlide = 1f, sigFigs = 1)]
        private float delayMinutes = 0;

        //Seconds and Minutes (*60) added
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Total Delay", guiUnits = "sec", guiFormat = "F1", groupName = PAWRCSGroupName, groupDisplayName = PAWRCSGroupName)]
        private float totalDelay = 0;

        //The PAW fields in Flight
        //Shows if RCS is active
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Circuits are", groupName = PAWRCSGroupName, groupDisplayName = PAWRCSGroupName)]
        private string PAWmodInUse = "inactive";
        //Shows the time until RCS gets activated in seconds, one decimal
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Seconds until Activation", guiUnits = "s", guiFormat = "F1", groupName = PAWRCSGroupName, groupDisplayName = PAWRCSGroupName)]
        private double PAWtimeToActivate = 0;
        //Shows what delay mode this RCS is in
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Delaymode", groupName = PAWRCSGroupName, groupDisplayName = PAWRCSGroupName)]
        private string PAWdelayMode = "Post Launch";


        //Shown in the Editor and in Flight
        //A button to enable or disable if a message for this event will be shown
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Event Messaging:", groupName = PAWRCSGroupName, groupDisplayName = PAWRCSGroupName),
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

        public void OnDestroy()
        {
            GameEvents.onEditorShipModified.Remove(updateEditorPAW);
            GameEvents.onLaunch.Remove(isLaunched);
            GameEvents.onPartDie.Remove(isDead);
        }

        public void OnPartDie()
        {
            GameEvents.onEditorShipModified.Remove(updateEditorPAW);
            GameEvents.onLaunch.Remove(isLaunched);
            GameEvents.onPartDie.Remove(isDead);
        }

        //Resume the last active coroutine, makes (quick-)saving useable
        private void isLoading()
        {
            if (activeCoroutine == 1)
                StartCoroutine(coroutinePostLaunch());
            else if (activeCoroutine == 3)
                StartCoroutine(coroutinePreApsideWait());
            else if (activeCoroutine == 4)
                StartCoroutine(coroutinePreApside());

            if (eventMessagingWanted)
                StartCoroutine(coroutinePrintMessage());
        }

        private async void initMod()
        {
            //Wait a bit to avoid the splashed bug, where the vesel can enter/stay in SPLASHED situation if something is done too early (before first physics tick)
            await Task.Delay(250);

            if (activeCoroutine != 0)
                isLoading();

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

                    GameEvents.onLaunch.Add(isLaunched);
                    GameEvents.onPartDie.Add(isDead);
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
            activeCoroutine = 1;
            for (; ; )
            {
                //Calculate how long until the engine ignites
                PAWtimeToActivate = (launchTime + totalDelay) - Planetarium.GetUniversalTime();

                if (PAWtimeToActivate <= 0)
                {
                    activateRCS();
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
            activeCoroutine = 2;
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
            activeCoroutine = 3;
            for (; ; )
            {
                //Calculate how long until the engine ignites
                PAWtimeToActivate = vessel.orbit.timeToAp - totalDelay;

                if (PAWtimeToActivate <= 0)
                {
                    activateRCS();
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
                    if (nextMessageStep == 0 && PAWtimeToActivate <= 10 && PAWtimeToActivate > 5)
                    {
                        ScreenMessages.PostScreenMessage("Activating RCS in 10 seconds.", 4.5f, ScreenMessageStyle.UPPER_CENTER);
                        nextMessageStep++;
                    }
                    else if (nextMessageStep == 1 && PAWtimeToActivate <= 5 && PAWtimeToActivate > 2)
                    {
                        ScreenMessages.PostScreenMessage("Activating RCS in 5 seconds.", 2.5f, ScreenMessageStyle.UPPER_CENTER);
                        nextMessageStep++;
                    }
                    else if (nextMessageStep == 2 && PAWtimeToActivate <= 2 && PAWtimeToActivate > 0)
                    {
                        ScreenMessages.PostScreenMessage("Activating RCS in 2 seconds.", 1.5f, ScreenMessageStyle.UPPER_CENTER);
                        nextMessageStep++;
                    }
                    else if (nextMessageStep == 3 && PAWtimeToActivate <= 0)
                    {
                        ScreenMessages.PostScreenMessage("Activating RCS.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        nextMessageStep++;
                        yield break;
                    }
                }
                yield return new WaitForSeconds(0.2f);
            }
        }

        //Activate RCS
        private void activateRCS()
        {
            part.force_activate();
            //Hide the timeToDecouple once the stage is decoupled
            Fields[nameof(PAWtimeToActivate)].guiActive = false;

        }

        private void endMod()
        {
            if (modInUse)
            {
                modInUse = false;
                PAWmodInUse = StringDisconnected;
                //Disable all text for inFlight Information
                Fields[nameof(PAWtimeToActivate)].guiActive = false;
                Fields[nameof(PAWdelayMode)].guiActive = false;

                //Update the size of the PAW
                MonoUtilities.RefreshPartContextWindow(part);

                activeCoroutine = 0;
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
}