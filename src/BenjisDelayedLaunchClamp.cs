using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace BenjisHardwiredLogic
{
    public class BenjisDelayedClamp : PartModule //*LaunchClamp
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
        private const string PAWLaunchClampGroupName = "Benji's Delayed Launch Clamp";

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
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Circuits are:", groupName = PAWLaunchClampGroupName, groupDisplayName = PAWLaunchClampGroupName),
            UI_Toggle(disabledText = StringDisconnected, enabledText = StringConnected)]
        private bool modInUse = false;

        //Specify the delay in seconds in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Delay [s]", guiFormat = "F1", groupName = PAWLaunchClampGroupName, groupDisplayName = PAWLaunchClampGroupName),
        UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 59.9f, incrementLarge = 10f, incrementSmall = 1f, incrementSlide = 0.1f, sigFigs = 1)]
        private float delaySeconds = 0;

        //The PAW fields in Flight
        //Shows if the decoupler is active
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Circuits are", groupName = PAWLaunchClampGroupName, groupDisplayName = PAWLaunchClampGroupName)]
        private string PAWmodInUse = "inactive";
        //Shows the time until the decoupler decouples in seconds, one decimal
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Seconds until release", guiUnits = "s", guiFormat = "F1", groupName = PAWLaunchClampGroupName, groupDisplayName = PAWLaunchClampGroupName)]
        private double PAWtimeToRelease = 0;

        //A small variable to manage the onScreen Messages
        private char nextMessageStep = (char)0;

        #endregion

        #region Overrides

        //This happens once in both EDITOR and FLIGHT
        public override void OnStart(StartState state)
        {
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
                StartCoroutine(initMod());

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

        }

        //Initialize all the fields when in FLIGHT
        /*
        private async void initMod()
        {
            //Wait a bit to avoid the splashed bug, where the vesel can enter/stay in SPLASHED situation if something is done too early (before first physics tick)
            await Task.Delay(250);
        */
        IEnumerator initMod()
        {
            //Wait a bit to avoid the splashed bug, where the vesel can enter/stay in SPLASHED situation if something is done too early (before first physics tick)
            yield return new WaitForSeconds(0.25f);

            if (activeCoroutine != 0)
                isLoading();

            //Now to check if we are on the launch pad
            if (vessel.situation == Vessel.Situations.PRELAUNCH)
            {
                //Add up the two parts of the overall delay and show me the numbers
                PAWtimeToRelease = delaySeconds;

                //Set the visible PAW variable 
                if (modInUse)
                {
                    PAWmodInUse = StringConnected;

                    GameEvents.onLaunch.Add(isLaunched);
                    GameEvents.onPartDie.Add(isDead);
                }
                else
                {
                    PAWmodInUse = StringDisconnected;
                    //Disable all text for inFlight Information
                    Fields[nameof(PAWtimeToRelease)].guiActive = false;
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
                Fields[nameof(delaySeconds)].guiActiveEditor = true;
            }
            else
            {
                //If this gui or any other is visible now, then we seem to change this in the next steps
                if (Fields[nameof(delaySeconds)].guiActiveEditor)
                    negChangeHappened = true;

                Fields[nameof(delaySeconds)].guiActiveEditor = false;
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

            StartCoroutine(coroutinePostLaunch());
        }

        //Gets called every .1 seconds and counts down to 0 after launch
        IEnumerator coroutinePostLaunch()
        {
            activeCoroutine = 1;
            for (; ; )
            {
                //Calculate how long until the engine ignites
                PAWtimeToRelease = (launchTime + delaySeconds) - Planetarium.GetUniversalTime();

                if (PAWtimeToRelease <= 0)
                {
                    releaseClamp();
                    endMod();
                    StopCoroutine(coroutinePostLaunch());
                    yield break;
                }

                yield return new WaitForSeconds(.1f);
            }
        }

        //Decouples the stage
        private void releaseClamp()
        {
            part.decouple();
            //Hide the timeToDecouple once the stage is decoupled
            Fields[nameof(PAWtimeToRelease)].guiActive = false;

        }

        //Hide all the fields
        private void endMod()
        {
            if (modInUse)
            {
                modInUse = false;
                PAWmodInUse = StringDisconnected;
                //Disable all text for inFlight Information
                Fields[nameof(PAWtimeToRelease)].guiActive = false;

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
        }

        #endregion
    }
}
