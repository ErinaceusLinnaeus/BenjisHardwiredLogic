using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace BenjisHardwiredLogic
{
    public class BenjisFairingSeparator : PartModule //ProceduralFairingDecoupler
    {
        #region Fields

        //Keeping track of what coroutine is running at the moment
        [KSPField(isPersistant = true, guiActive = false)]
        private int activeCoroutine = 0;

        //Catch the slider dragging "bug"
        [KSPField(isPersistant = false, guiActive = false)]
        private bool negChangeHappened = false;

        //Headline name for the GUI
        [KSPField(isPersistant = false, guiActive = false)]
        private const string PAWFairingGroupName = "Benji's Fairing Separator";

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
        //A button to enable or disable the function
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Circuits are:", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName),
            UI_Toggle(disabledText = StringDisconnected, enabledText = StringConnected)]
        private bool modInUse = false;

        //Specify the Height in kilometers in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Altitude [km]", guiFormat = "F0", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName),
        UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 200f, incrementLarge = 10f, incrementSmall = 1f, incrementSlide = 1f, sigFigs = 0)] //140km - that's where the atmosphere ends
        private float altitudeToSeparate = 75;

        //A button to enable or disable if a message for this event will be shown
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Jettison", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName),
            UI_ChooseOption(options = new string[2] { "Payload", "Interstage" })]
        private string PAWfairing = "Payload";


        //The PAW fields in Flight
        //Shows if the fairing is active
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Circuits are", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName)]
        private string PAWmodInUse;

        //Shows the Height in kilometers at which the fairing gets separated
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Altitude to Jettison", guiUnits = "km", guiFormat = "F0", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName)]
        private float PAWaltitudeToJettison = 0;

        //Shown in the Editor and in Flight
        //A button to enable or disable if a message for this event will be shown
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Event Messaging:", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName),
            UI_Toggle(disabledText = StringInactive, enabledText = StringActive)]
        private bool eventMessagingWanted = true;

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
                //Set the visible PAW variable 
                if (modInUse)
                {
                    PAWmodInUse = StringConnected;
                    //Set the text for inFlight Information
                    PAWaltitudeToJettison = altitudeToSeparate;

                    GameEvents.onLaunch.Add(isLaunched);
                    GameEvents.onPartDie.Add(isDead);
                }
                else
                {
                    PAWmodInUse = StringDisconnected;
                    //Disable all text for inFlight Information
                    Fields[nameof(PAWaltitudeToJettison)].guiActive = false;
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
                Fields[nameof(altitudeToSeparate)].guiActiveEditor = true;
                Fields[nameof(PAWfairing)].guiActiveEditor = true;
                Fields[nameof(eventMessagingWanted)].guiActiveEditor = true;
            }
            else
            {
                //If this gui or any other is visible now, then we seem to change this in the next steps
                if (Fields[nameof(altitudeToSeparate)].guiActiveEditor)
                    negChangeHappened = true;

                Fields[nameof(altitudeToSeparate)].guiActiveEditor = false;
                Fields[nameof(PAWfairing)].guiActiveEditor = false;
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
            StartCoroutine(coroutinePostLaunch());
        }

        //Gets called every .1 seconds and checks if the desired height is reached
        IEnumerator coroutinePostLaunch()
        {
            activeCoroutine = 1;
            for (; ; )
            {
                //Are we high enough to separate...
                if (vessel.orbit.altitude >= (PAWaltitudeToJettison * 1000f))
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
                    yield break;
                }
                yield return new WaitForSeconds(.1f);
            }
        }

        private void endMod()
        {
            modInUse = false;
            PAWmodInUse = StringDisconnected;
            //Disable all text for inFlight Information
            Fields[nameof(PAWaltitudeToJettison)].guiActive = false;
            Fields[nameof(eventMessagingWanted)].guiActive = false;

            //Update the size of the PAW
            MonoUtilities.RefreshPartContextWindow(part);

            activeCoroutine = 0;
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
