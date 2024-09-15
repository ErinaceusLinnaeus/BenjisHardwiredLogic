using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace BenjisHardwiredLogic
{
    public class BenjisDelayedDecoupler : PartModule //Module*Decouple*
    {
        #region Fields

        //Keeping track of what coroutine is running at the moment
        [KSPField(isPersistant = true, guiActive = false)]
        private int activeCoroutine = 0;

        //Saving UniversalTime into launchTime when the Vessel gets launched
        [KSPField(isPersistant = true, guiActive = false)]
        private double launchTime = 0;

        //Catch the slider dragging "bug"
        [KSPField(isPersistant = false, guiActive = false)]
        private bool negChangeHappened = false;

        //Headline name for the GUI
        [KSPField(isPersistant = false, guiActive = false)]
        private const string PAWDecouplerGroupName = "Benji's Delayed Decoupler";

        //Text, if functionality is disabled/enabled
        [KSPField(isPersistant = false, guiActive = false)]
        private const string StringDisconnected = "disconnected";

        [KSPField(isPersistant = false, guiActive = false)]
        private const string StringConnected = "connected";

        //Text, if event messaging is disabled/enabled
        [KSPField(isPersistant = false, guiActive = false)]
        private const string StringInactive = "inactive";

        [KSPField(isPersistant = false, guiActive = false)]
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
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Total Delay", guiUnits = "sec", guiFormat = "F1", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName)]
        private float totalDelay = 0;

        //Name what the stage that will be decoupled will be called during event messaging
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Decouple", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName),
            UI_ChooseOption(options = new string[9] { "1st Stage", "2nd Stage", "3rd Stage", "4th Stage", "Booster", "Spin-Motor", "Ullage-Motor", "Apogee Kick Stage", "Payload" })]
        private string stage = "1st Stage";

        //The PAW fields in Flight
        //Shows if the decoupler is active
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Circuits are", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName)]
        private string PAWmodInUse = "inactive";
        //Shows the time until the decoupler decouples in seconds, one decimal
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Seconds until Decouple", guiUnits = "s", guiFormat = "F1", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName)]
        private double PAWtimeToDecouple = 0;
        //Shows what delay mode this decoupler is in
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Delaymode", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName)]
        private string PAWdelayMode = "Post Launch";
        //Shows what type of stage will be decouple by this decoupler
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Decouple", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName)]
        private string PAWstage = "1st Stage";

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
                StartCoroutine(coroutineInitMod());

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

        //Initialize all the fields when in FLIGHT
        /*
        private async void initMod()
        {
            //Wait a bit to avoid the splashed bug, where the vesel can enter/stay in SPLASHED situation if something is done too early (before first physics tick)
            await Task.Delay(250);
        */
        IEnumerator coroutineInitMod()
        {
            //Wait a bit to avoid the splashed bug, where the vesel can enter/stay in SPLASHED situation if something is done too early (before first physics tick)
            yield return new WaitForSeconds(0.25f);

            if (activeCoroutine != 0)
                isLoading();

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

                    GameEvents.onLaunch.Add(isLaunched);
                    GameEvents.onPartDie.Add(isDead);
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

                Fields[nameof(delayMode)].guiActiveEditor = true;
                Fields[nameof(delaySeconds)].guiActiveEditor = true;
                Fields[nameof(delayMinutes)].guiActiveEditor = true;
                Fields[nameof(totalDelay)].guiActiveEditor = true;
                Fields[nameof(stage)].guiActiveEditor = true;
                Fields[nameof(eventMessagingWanted)].guiActiveEditor = true;
            }
            else
            {
                //If this gui or any other is visible now, then we seem to change this in the next steps
                if (Fields[nameof(delaySeconds)].guiActiveEditor)
                    negChangeHappened = true;

                Fields[nameof(delayMode)].guiActiveEditor = false;
                Fields[nameof(delaySeconds)].guiActiveEditor = false;
                Fields[nameof(delayMinutes)].guiActiveEditor = false;
                Fields[nameof(totalDelay)].guiActiveEditor = false;
                Fields[nameof(stage)].guiActiveEditor = false;
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
                    if (nextMessageStep == 0 && PAWtimeToDecouple <= 10 && PAWtimeToDecouple > 5)
                    {
                        ScreenMessages.PostScreenMessage("Decoupling " + stage + " in 10 seconds.", 4.5f, ScreenMessageStyle.UPPER_CENTER);
                        nextMessageStep++;
                    }
                    else if (nextMessageStep == 1 && PAWtimeToDecouple <= 5 && PAWtimeToDecouple > 2)
                    {
                        ScreenMessages.PostScreenMessage("Decoupling " + stage + " in 5 seconds.", 2.5f, ScreenMessageStyle.UPPER_CENTER);
                        nextMessageStep++;
                    }
                    else if (nextMessageStep == 2 && PAWtimeToDecouple <= 2 && PAWtimeToDecouple > 0)
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

                activeCoroutine = 0;
            }
        }

        //Gets called when the part explodes etc.
        private void isDead(Part part)
        {
            //Stopping all the coroutines that might be running
            StopCoroutine(coroutineInitMod());
            StopCoroutine(coroutinePostLaunch());
            StopCoroutine(coroutinePreApsideWait());
            StopCoroutine(coroutinePreApside());
            StopCoroutine(coroutinePrintMessage());
        }

        #endregion
    }
}
