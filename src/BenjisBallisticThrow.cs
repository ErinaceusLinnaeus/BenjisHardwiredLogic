using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


namespace BenjisHardwiredLogic
{
    internal class BenjisBallisticThrow : PartModule //ModuleSAS
    {

        #region Fields

        //The Marker the rocket tries to follow
        [KSPField(isPersistant = false, guiActive = false)]
        private DirectionTarget directionGuidance = new DirectionTarget("directionGuidance");

        //Saving UniversalTime into launchTime when the Vessel gets launched
        [KSPField(isPersistant = true, guiActive = false)]
        private double launchTime = 0;

        //Saving UniversalTime into launchTime when the Vessel gets launched
        [KSPField(isPersistant = true, guiActive = true)]
        private double missionTime = 0;

        //Keeping track of what coroutine is running at the moment
        [KSPField(isPersistant = true, guiActive = false)]
        private int activeCoroutine = 0;

        //Catch the slider dragging "bug"
        [KSPField(isPersistant = false, guiActive = false)]
        private bool negChangeHappened = false;

        //Headline name for the GUI
        [KSPField(isPersistant = false, guiActive = false)]
        private const string PAWAscentGroupName = "Benji's Ballistic Throw";

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

        [KSPField(isPersistant = false, guiActive = true)]
        private double desiredPitch = 0;

        //The PAW fields in the editor
        //A button to enable or disable the module
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Circuits are:", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_Toggle(disabledText = StringDisconnected, enabledText = StringConnected)]
        private bool modInUse = false;

        //Specify when the rocket should reach the desired ballistic angle in seconds in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Guided Flight [sec]", guiFormat = "F1", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 59.9f, incrementLarge = 10f, incrementSmall = 1f, incrementSlide = 0.1f, sigFigs = 1)]
        private float guidedFlightSeconds = 0;

        //Specify when the rocket should reach the desired ballistic angle in minutes in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Guided Flight [min]", guiFormat = "F1", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 5f, incrementLarge = 5f, incrementSmall = 1f, incrementSlide = 1f, sigFigs = 1)]
        private float guidedFlightMinutes = 0;

        //Seconds and Minutes (*60) added
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Total Guided Flight", guiUnits = "sec", guiFormat = "F1", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName)]
        private float totalGuidedFlight = 0;

        //A button to enable or disable the gimbal spin at the end of the guided flight
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Gimbal Spin:", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_Toggle(disabledText = StringInactive, enabledText = StringActive)]
        private bool gimbalSpin = true;

        //Specify how long before the gimbal spin should start in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Gimbal Spin Pre Delay [sec]", guiFormat = "F1", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 5.0f, incrementLarge = 1f, incrementSmall = 0.1f, incrementSlide = 0.1f, sigFigs = 1)]
        private float gimbalSpinPreSeconds = 2.5f;

        //Specify the angle you wanna end up at
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "DeltaV [m/s]", guiFormat = "I", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0, maxValue = 10000f, incrementLarge = 1000, incrementSmall = 100, incrementSlide = 1, sigFigs = 1)]
        private int deltaV = 5000;

        //The PAW fields in Flight
        //Shows if the decoupler is active
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Circuits are", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName)]
        private string PAWmodInUse = "inactive";

        //Shown in the Editor and in Flight
        //Shows the estimated downrange for the dV given
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Estimated Downrange", guiUnits = "km", guiFormat = "F1", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName)]
        private double PAWestimatedDownrange = 0;

        //Shows the estimated flight path angle (FPA) at the end of the burn
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Estimated FPA", guiUnits = "°", guiFormat = "F1", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName)]
        private double PAWestimatedFPA = 0;

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
                StartCoroutine(coroutineTurn());;
        }

        //Initialize all the fields when in FLIGHT
        IEnumerator coroutineInitMod()
        {
            //Wait a bit to avoid the splashed bug, where the vesel can enter/stay in SPLASHED situation if something is done too early (before first physics tick)
            yield return new WaitForSeconds(0.25f);

            if (activeCoroutine != 0)
                isLoading();

            //Now to check if we are on the launch pad
            if (vessel.situation == Vessel.Situations.PRELAUNCH)
            {

                //Set the visible PAW variable 
                if (modInUse)
                {
                    PAWmodInUse = StringConnected;
                    totalGuidedFlight = guidedFlightSeconds + (guidedFlightMinutes * 60);
                    GameEvents.onLaunch.Add(isLaunched);
                    GameEvents.onPartDie.Add(isDead);
                }
                else
                {
                    PAWmodInUse = StringDisconnected;
                    //Disable all text for inFlight Information
                    Fields[nameof(PAWestimatedDownrange)].guiActive = false;
                    Fields[nameof(PAWestimatedFPA)].guiActive = false;
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
                //Calculate the downrange for the given dV with a 45° angle (flat surface in a vaccuum...physics)
                PAWestimatedDownrange = ((Math.Pow(deltaV, 2) * Math.Sin(2 * (45 * (Math.PI / 180.0)))) / 9.81) / 1000.0f;
                //Correct the angle with the earth's curvature
                PAWestimatedFPA = 14.325 * (Math.PI - (PAWestimatedDownrange / 6371));
                                //14.325 * (π - (Downrange Distance / Radius of Earth))


                Fields[nameof(guidedFlightSeconds)].guiActiveEditor = true;
                Fields[nameof(guidedFlightMinutes)].guiActiveEditor = true;
                Fields[nameof(totalGuidedFlight)].guiActiveEditor = true;
                Fields[nameof(deltaV)].guiActiveEditor = true;
                Fields[nameof(gimbalSpin)].guiActiveEditor = true;
                if (gimbalSpin == true)
                    Fields[nameof(gimbalSpinPreSeconds)].guiActiveEditor = true;
                else
                    Fields[nameof(gimbalSpinPreSeconds)].guiActiveEditor = false;
                Fields[nameof(PAWestimatedDownrange)].guiActiveEditor = true;
                Fields[nameof(PAWestimatedFPA)].guiActiveEditor = true;
            }
            else
            {
                //If this gui or any other is visible now, then we seem to change this in the next steps
                if (Fields[nameof(guidedFlightSeconds)].guiActiveEditor)
                    negChangeHappened = true;

                Fields[nameof(guidedFlightSeconds)].guiActiveEditor = false;
                Fields[nameof(guidedFlightMinutes)].guiActiveEditor = false;
                Fields[nameof(totalGuidedFlight)].guiActiveEditor = false;
                Fields[nameof(deltaV)].guiActiveEditor = false;
                Fields[nameof(gimbalSpin)].guiActiveEditor = false;
                Fields[nameof(gimbalSpinPreSeconds)].guiActiveEditor = false;
                Fields[nameof(PAWestimatedDownrange)].guiActiveEditor = false;
                Fields[nameof(PAWestimatedFPA)].guiActiveEditor = false;
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

            //Lock into the direction Marker
            vessel.targetObject = directionGuidance;
            vessel.Autopilot.Enable(VesselAutopilot.AutopilotMode.Target);

            StartCoroutine(coroutineTurn());
        }

        //Gets called every .1 seconds and starts the turn
        IEnumerator coroutineTurn()
        {
            activeCoroutine = 1;
            for (; ; )
            {
                //ANGLE = Math.Sqrt(Math.Pow(r, 2) - Math.Pow(0.5 * x - r, 2)); for (x < 2 * r);
                //r is the radius of the quarter-circle...the so called turn shape

                //THOUGHTS:
                //  desired end-angle is given by the dV->downrange->angle calculation
                //  I need to match that with the "burn time"
                //  example: burn-time = 60 = x
                //           angle = 30
                //           Above formula needs to be 30 for x = 60
                //  -> I need a loop/formula that finds out what radius I need to have f(60) = 30
                //  ARIA says:
                //      r = (Math.Pow ((angle, 2) + 0.25 * Math.Pow(burn-time, 2)) ) / burn-time;
                // Put r into the function above and x will be the time since lift off, giving the angle the rocket needs to stear to

                //Desmos: https://www.desmos.com/calculator/cduplyp5aq

                if (missionTime < totalGuidedFlight)
                {
                    //Update the direction Marker depending on the flight time
                    missionTime = Planetarium.GetUniversalTime() - launchTime;
                    desiredPitch = Math.Sqrt(Math.Pow(PAWestimatedFPA, 2) - Math.Pow(0.5 * missionTime - PAWestimatedFPA, 2));

                    directionGuidance.Update(vessel, (90 - desiredPitch), 90, true);
                }
                else
                {
                    directionGuidance.Update(vessel, (90 - PAWestimatedFPA), 90, true);
                }

                if ((missionTime + gimbalSpinPreSeconds) > totalGuidedFlight)
                {
                    vessel.OnFlyByWire -= steeringCommand_GimalRoll;
                    vessel.OnFlyByWire += steeringCommand_GimalRoll;
                }

                    yield return new WaitForSeconds(.1f);
            }
        }

        //All the steering happens here
        private void steeringCommand_GimalRoll(FlightCtrlState state)
        {
            
            state.roll = 1.0f;
        }

        //Hide all the fields
        private void endMod()
        {
            if (modInUse)
            {
                modInUse = false;
                PAWmodInUse = StringDisconnected;
                //Disable all text for inFlight Information
                Fields[nameof(PAWestimatedDownrange)].guiActive = false;
                Fields[nameof(PAWestimatedFPA)].guiActive = false;

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
            StopCoroutine(coroutineTurn());
        }

        #endregion

    }
}
