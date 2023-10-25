using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace BenjisHardwiredLogic
{
    public class BenjisAutoAscent : PartModule //ModuleSAS
    {
        // Curves that describe the launch profile visually
        // ALT < 25km               :   102.5 * x^0.58
        // 25km < ALT < 82.5km      :   -0.000004 * x^2 + 1.2 * x + 10000
        // 82.5km < ALT < end turn  :   -456000 + 47500 * ln(x)

        // x    : ALT
        // f(x) : Downrange
        // ALT <= 76km      :   0.00174397 x^(1.5626)
        // ALT >  76km      :   15400 ℯ^(0.00002063 x)

        // x = ALT
        // f(x) : pitch
        // ALT < 10.075 m             :   (0.014013 * (x*x*x)) - (0.448716 * (x*x)) + (5.730697 * x) + 0.32134
        // 10.075 m < ALT < 97.800 m  :   (0.00003 * (x * x * x)) - (0.008594 * (x * x)) + (1.075113 * x) + 16.851515
        // 80.000 m < ALT < 240.000 m :   -(0.000889 * (x*x)) + (0.456161 * x) + 31.754229

        #region Fields

        //Saving UniversalTime into launchTime when the Vessel getÞs launched
        [KSPField(isPersistant = true, guiActive = false)]
        private double launchData_Time = 0;

        //The azimuth we're heading at
        [KSPField(isPersistant = true, guiActive = false)]
        private double azimuth = 0;

        //Keeping track of the pitch rate
        [KSPField(isPersistant = true, guiActive = false)]
        //private double desiredPitch = 0;
        private Vector3d desiredHeading = new Vector3d(0, 0, 0);
        [KSPField(isPersistant = true, guiActive = false)]
        private Vector3d sumOfAngulars;

        //Keeping track of what coroutine is running at the moment
        [KSPField(isPersistant = true, guiActive = false)]
        private int activeCoroutine = 0;

        //Catch the slider dragging "bug"
        [KSPField(isPersistant = true, guiActive = false)]
        private bool negChangeHappened = false;

        //Headline name for the GUI
        [KSPField(isPersistant = true, guiActive = false)]
        private const string PAWAscentGroupName = "Benji's Auto Ascent";

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

        //Creating an orbitalFrame, saving the initial state...just like a gimbal
        [KSPField(isPersistant = true, guiActive = false)]
        private Vector3d orbitalPrograde;
        [KSPField(isPersistant = true, guiActive = false)]
        private Vector3d orbitalRadial;
        [KSPField(isPersistant = true, guiActive = false)]
        private UnityEngine.Quaternion orbitalFrame;
        [KSPField(isPersistant = true, guiActive = false)]
        private Vector3d orbitalNormal;

        [KSPField(isPersistant = true, guiActive = false)]
        private double launchData_SiteLat;
        [KSPField(isPersistant = true, guiActive = false)]
        private double launchData_SiteLong;
        [KSPField(isPersistant = true, guiActive = false)]
        private double downrangeDistance;

        [KSPField(isPersistant = true, guiActive = false)]
        private Vector3d markVector;
        [KSPField(isPersistant = true, guiActive = false)]
        private double bodysCircumference;

        //Fields to keep track of the vessel's steering dragyness
        //[KSPField(isPersistant = true, guiActive = false)]
        private Vector3d[] steerDragArray = new Vector3d[128];
        //[KSPField(isPersistant = true, guiActive = false)]
        private int steerDragPos = 0;
        [KSPField(isPersistant = true, guiActive = false)]
        private Vector3d steerDragAverage = new Vector3d(0, 0, 0);
        [KSPField(isPersistant = true, guiActive = false)]
        private Vector3d steerDrag = new Vector3d(0, 0, 0);
        [KSPField(isPersistant = true, guiActive = false)]
        private Vector3d lastTicksSteering = new Vector3d(0, 0, 0);
        [KSPField(isPersistant = true, guiActive = false)]
        private Vector3d lastTicksAngularVelocity;// = new Vector3d(0, 0, 0);
        [KSPField(isPersistant = true, guiActive = false)]
        private Vector3d pointToTurn = new Vector3d(0, 0, 0);
        [KSPField(isPersistant = true, guiActive = false)]
        private double steerStrengthFactor = 10;
        [KSPField(isPersistant = true, guiActive = false)]
        private double launchData_Altitude;

        //[KSPField(isPersistant = true, guiActive = false)]
        private DirectionTarget directionAscentGuidance = new DirectionTarget("directionAscentGuidance");
        //[KSPField(isPersistant = true, guiActive = false)]
        //private DirectionTarget directionParallel2Tangent = new DirectionTarget("directionParallel2Tangent");
        //[KSPField(isPersistant = true, guiActive = false)]
        //private DirectionTarget direction90Deg2Tangent = new DirectionTarget("direction90Deg2Tangent");

        //The PAW fields in the editor
        //A button to enable or disable the module
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Circuits are:", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_Toggle(disabledText = StringDisconnected, enabledText = StringConnected)]
        private bool modInUse = false;
        //Specify the inclination you wanna end up at
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Inclination [°]", guiFormat = "F1", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 90.0f, incrementLarge = 5f, incrementSmall = 1f, incrementSlide = 0.1f, sigFigs = 1)]
        private float desiredInclination = 0;
        //Set the orbit pro- or retrograde
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Orbital Direction", guiFormat = "F1", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_ChooseOption(options = new string[2] { "Prograde", "Retrograde" })]
        private string desiredOrbitalDirection = "Prograde";
        //Execute a roll and keep the horizon leveled during ascent
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Roll Maneuver:", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_Toggle(disabledText = StringInactive, enabledText = StringActive)]
        private bool desiredRoll = true;
        //Just follow the ascent guidance or try to match the flight path as exactly as possible
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Corrective Steering:", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_Toggle(disabledText = StringInactive, enabledText = StringActive)]
        private bool correctiveSteering = false;

        //The PAW fields in Flight
        //Shows if the decoupler is active
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Circuits are", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName)]
        private string PAWmodInUse = "inactive";

        //Shown in the Editor and in Flight
        //A button to enable or disable if a message for this event will be shown
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Event Messaging:", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_Toggle(disabledText = StringInactive, enabledText = StringActive)]
        private bool eventMessagingWanted = true;

        //A small variable to manage the onScreen Messages
        private char nextMessageStep = (char)0;

        #endregion

        #region steering delegates

        //This "steering state" doesn't steer anything. It just keeps track of some data to calculate how much angular change can be achived by steering
        private void steeringMeasurements(FlightCtrlState state)
        {
            //Adding up the total angular changes
            sumOfAngulars += vessel.angularVelocityD * TimeWarp.CurrentRate;

            //And keep the sum within -360° and 360°
            if (sumOfAngulars.x > 360)
                sumOfAngulars.x += -360;
            else if (sumOfAngulars.x < -360)
                sumOfAngulars.x += 360;

            if (sumOfAngulars.y > 360)
                sumOfAngulars.y += -360;
            else if (sumOfAngulars.y < -360)
                sumOfAngulars.y += 360;

            if (sumOfAngulars.z > 360)
                sumOfAngulars.z += -360;
            else if (sumOfAngulars.z < -360)
                sumOfAngulars.z += 360;

            //Store the change in angular change into the temporary array
            //Ignore tiny steering values to avaoid infinity
            if (Math.Abs(lastTicksSteering.x) > 0.0001)
                steerDragArray[steerDragPos].x = (steerDragArray[steerDragPos].x + Math.Abs((lastTicksAngularVelocity.x - vessel.angularVelocityD.x) / lastTicksSteering.x)) / 2;
            if (Math.Abs(lastTicksSteering.y) > 0.0001)
                steerDragArray[steerDragPos].y = (steerDragArray[steerDragPos].y + Math.Abs((lastTicksAngularVelocity.y - vessel.angularVelocityD.y) / lastTicksSteering.y)) / 2;
            if (Math.Abs(lastTicksSteering.z) > 0.0001)
                steerDragArray[steerDragPos].z = (steerDragArray[steerDragPos].z + Math.Abs((lastTicksAngularVelocity.z - vessel.angularVelocityD.z) / lastTicksSteering.z)) / 2;
            //Move to next array position
            if (steerDragPos == (steerDragArray.Length - 1))
                steerDragPos = 0;
            else
                steerDragPos++;

            //Keep this tick's angular and steering for next tick's calculation
            lastTicksAngularVelocity = vessel.angularVelocityD;
            lastTicksSteering.x = state.pitch;
            lastTicksSteering.y = state.roll;
            lastTicksSteering.z = state.yaw;
        }
        //All the steering happens here
        private void steeringCommands(FlightCtrlState state)
        {

            //state.pitch = (float)HelperFunctions.limit((((vessel.angularVelocityD.x + (sumOfAngulars.x + pointToTurn.x) * steerStrengthFactor)) / TimeWarp.CurrentRate), -1, 1);
            state.roll = (float)HelperFunctions.limit((((vessel.angularVelocityD.y + (sumOfAngulars.y + pointToTurn.y) * steerStrengthFactor)) / TimeWarp.CurrentRate), -1, 1);
            //state.yaw = (float)HelperFunctions.limit((((vessel.angularVelocityD.z + (sumOfAngulars.z + pointToTurn.z) * steerStrengthFactor)) / TimeWarp.CurrentRate), -1, 1);



        }
        private void steeringGravityTurn(FlightCtrlState state)
        {

            /*
            Vector3d flightDirection, shipUp;

            if (vessel.orbit.altitude < 45000)
                flightDirection = vessel.srf_velocity;
            else
                flightDirection = vessel.obt_velocity;

            shipUp = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(0, 0, 0) * Vector3d.up).normalized;
            */
            //ScreenMessages.PostScreenMessage("AoA: " + HelperFunctions.degAngle(flightDirection, shipUp), 0.1f, ScreenMessageStyle.UPPER_LEFT);


            /*

            if (HelperFunctions.degAngle(orbitalRadial, flightDirection) < desiredPitch)
            //if (sumOfAngularPitch < desiredPitch)
            {
                //state.pitch = - (float)HelperFunctions.limitAbs(((sumOfAngulars.x - desiredPitch) / 2d), 0.5);
                state.pitch = - (float)HelperFunctions.limit((((vessel.angularVelocityD.x + (desiredPitch) / 10)) / TimeWarp.CurrentRate), -1, 1);
                //state.pitch = -0.1f;
            }
            else
            {
                //state.pitch = (float)HelperFunctions.limitAbs(((sumOfAngulars.x - desiredPitch) / 2d), 0.5);
                state.pitch = (float)HelperFunctions.limit((((vessel.angularVelocityD.x + (desiredPitch) / 10)) / TimeWarp.CurrentRate), -1, 1);
                //state.pitch = 0.1f;
            }

            if (HelperFunctions.degAngle(flightDirection, shipUp) >= 3)
            {
                state.pitch *= -1;
            }
            */

            //TEST WITH MUCH SOFTER PITCHING
            //sumOfAngulars.x = -(desiredPitch / 10);
            /*
            double tempPitch = HelperFunctions.limit((((vessel.angularVelocityD.x + ((sumOfAngulars.x - desiredHeading.x) + pointToTurn.x) / 10)) / TimeWarp.CurrentRate), -1, 1);
            state.pitch = (float)(Math.Abs((HelperFunctions.degAngle(flightDirection, shipUp) / 5) - 1) * tempPitch);

            state.roll = (float)HelperFunctions.limit((((vessel.angularVelocityD.y + ((sumOfAngulars.y) + pointToTurn.y) / 10)) / TimeWarp.CurrentRate), -1, 1);
            state.yaw = (float)HelperFunctions.limit((((vessel.angularVelocityD.z + (sumOfAngulars.z) / 10)) / TimeWarp.CurrentRate), -1, 1);
            */

        }

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

        //Resume the last active coroutine, makes (quick-)saving useable
        private void isLoading()
        {
            if (activeCoroutine == 1)
            {
                StartCoroutine(coroutineSteeringMeasurements());
                StartCoroutine(coroutineCalculateAutoAscent());
            }

            if (eventMessagingWanted)
                StartCoroutine(coroutinePrintMessage());
        }

        //Initialize all the fields when in FLIGHT
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

                    GameEvents.onLaunch.Add(isLaunched);
                    GameEvents.onPartDie.Add(isDead);
                }
                else
                {
                    PAWmodInUse = StringDisconnected;
                    //Disable all text for inFlight Information
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
                Fields[nameof(desiredInclination)].guiActiveEditor = true;
                Fields[nameof(desiredOrbitalDirection)].guiActiveEditor = true;
                Fields[nameof(desiredRoll)].guiActiveEditor = true;
                Fields[nameof(correctiveSteering)].guiActiveEditor = true;
                Fields[nameof(eventMessagingWanted)].guiActiveEditor = true;
            }
            else
            {
                //If this gui or any other is visible now, then we seem to change this in the next steps
                if (Fields[nameof(desiredInclination)].guiActiveEditor)
                    negChangeHappened = true;

                Fields[nameof(desiredInclination)].guiActiveEditor = false;
                Fields[nameof(desiredOrbitalDirection)].guiActiveEditor = false;
                Fields[nameof(desiredRoll)].guiActiveEditor = false;
                Fields[nameof(correctiveSteering)].guiActiveEditor = false;
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
            launchData_Altitude = vessel.orbit.altitude;

            //Need to turn on SAS (in stability mode) or else ksp crashes when we start giving steering commands
            vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);
            vessel.Autopilot.Enable(VesselAutopilot.AutopilotMode.StabilityAssist);

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///Not sure if we need all this....
            //Set the launch time
            launchData_Time = Planetarium.GetUniversalTime();

            //Creating an orbitalFrame, saving the initial state...just like a gimbal
            orbitalPrograde = vessel.obt_velocity.normalized;
            orbitalRadial = (vessel.CoMD - vessel.mainBody.position).normalized;
            //Not used right now, but to have it complete:
            orbitalFrame = UnityEngine.QuaternionD.LookRotation(orbitalPrograde, orbitalRadial);
            orbitalNormal = orbitalFrame * Vector3d.left;
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


            //Needed to calculate the angle deviation when downrange distance gets bigger
            //The planets curves away as the vessel travels downrange distance => MORE ANGLE
            launchData_SiteLat = vessel.latitude;
            launchData_SiteLong = vessel.longitude;

            markVector = FlightGlobals.Bodies[1].GetSurfaceNVector(launchData_SiteLat, launchData_SiteLong);
            bodysCircumference = 2 * Math.PI * FlightGlobals.Bodies[1].Radius;


            //Calculate the "heading" the vessel needs to roll into, to end up at the desired inclination
            //Lower inclinations than the launch-site's are not possible 
            if (desiredInclination <= Math.Abs(launchData_SiteLat))
                azimuth = Math.Abs(launchData_SiteLat);
            else
                azimuth = HelperFunctions.radToDeg(Math.Acos((Math.Cos(HelperFunctions.degToRad(desiredInclination)) / Math.Cos(HelperFunctions.degToRad(Math.Abs(vessel.latitude))))));

            //KSP Navball is weird. North is at 0°, but should be 90°. East at 90°but should be 0°. 
            if (desiredOrbitalDirection == "Prograde")
            {
                if (launchData_SiteLat < 0)
                    desiredHeading.y = (90 - azimuth);
                else
                    desiredHeading.y = (90 + azimuth);
            }
            //And turning it the other direction if launching retrograde
            else
            {
                if (launchData_SiteLat < 0)
                    desiredHeading.y = (270 - azimuth);
                else
                    desiredHeading.y = (270 + azimuth);
            }

            //Setting all the elemts of the Drag Array to zero
            for (int i = 0; i < steerDragArray.Length; i++)
            {
                steerDragArray[i].x = 0;
                steerDragArray[i].y = 0;
                steerDragArray[i].z = 0;
            }

            //vessel, pitch, heading, true means degree
            directionAscentGuidance.Update(vessel, 90, 0, false);
            vessel.targetObject = directionAscentGuidance;
            vessel.Autopilot.Enable(VesselAutopilot.AutopilotMode.Target);

            //directionParallel2Tangent.Update(vessel, 90, 0, false);
            //direction90Deg2Tangent.Update(vessel, 0, 0, false);

            StartCoroutine(coroutineSteeringMeasurements());
            StartCoroutine(coroutineCalculateAutoAscent());

            vessel.OnFlyByWire += steeringMeasurements;
            vessel.OnFlyByWire += steeringCommands;

        }

        //This coroutine will use the data collected by steeringMeasurements and calculate the actual "dragyness"/effectiveness of the gimbaling/steering
        private IEnumerator coroutineSteeringMeasurements()
        {
            activeCoroutine = 1;
            for (; ; )
            {
                //Add up all the values in the array...
                for (int i = 0; i < steerDragArray.Length; i++)
                {
                    steerDragAverage.x += steerDragArray[i].x;
                    steerDragAverage.y += steerDragArray[i].y;
                    steerDragAverage.z += steerDragArray[i].z;
                }
                //...and divide it by the amount of elements...
                //...resulting in an average over the last 128 measurements
                steerDragAverage.x /= steerDragArray.Length;
                steerDragAverage.y /= steerDragArray.Length;
                steerDragAverage.z /= steerDragArray.Length;

                //Now do FindObjectsOfTypeAll this again, but leave out any value that differs by more than 10% from the average
                int jX, jY, jZ;
                jX = jY = jZ = steerDragArray.Length;

                for (int i = 0; i < steerDragArray.Length; i++)
                {
                    if (HelperFunctions.isInBetweenFactorOf(steerDragArray[i].x, steerDragAverage.x, 0.1))
                        steerDrag.x += steerDragArray[i].x;
                    else
                        jX--; //Substract the ignored elements...

                    if (HelperFunctions.isInBetweenFactorOf(steerDragArray[i].y, steerDragAverage.y, 0.1))
                        steerDrag.y += steerDragArray[i].y;
                    else
                        jY--; //Substract the ignored elements...

                    if (HelperFunctions.isInBetweenFactorOf(steerDragArray[i].z, steerDragAverage.z, 0.1))
                        steerDrag.z += steerDragArray[i].z;
                    else
                        jZ--; //Substract the ignored elements...
                }
                //..to get a correct final average result
                steerDrag.x /= jX;
                steerDrag.y /= jY;
                steerDrag.z /= jZ;

                yield return new WaitForSeconds(.1f);
            }
        }

        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        private IEnumerator coroutineCalculateAutoAscent()
        {
            activeCoroutine = 1;
            for (; ; )
            {

                //Calculating at what angular difference to 0 the counter steering needs to happen
                pointToTurn.x = 0;
                pointToTurn.y = 0;
                pointToTurn.z = 0;

                if (steerDrag.x != 0)
                {
                    for (double d = Math.Abs(vessel.angularVelocityD.x); d > 0; d -= steerDrag.x)
                    {
                        pointToTurn.x += d;
                    }
                }
                if (steerDrag.y != 0)
                {
                    for (double d = Math.Abs(vessel.angularVelocityD.y); d > 0; d -= steerDrag.y)
                    {
                        pointToTurn.y += d;
                    }
                }
                if (steerDrag.z != 0)
                {
                    for (double d = Math.Abs(vessel.angularVelocityD.z); d > 0; d -= steerDrag.z)
                    {
                        pointToTurn.z += d;
                    }
                }

                //FOR NOW:
                //Multiplying by a factor of 1.5 to account for latency and atmospheric drag
                //Let's see if it needs adjustment at some point
                if (sumOfAngulars.x >= 0)
                    pointToTurn.x *= -1.5;
                else
                    pointToTurn.x *= 1.5;

                if (sumOfAngulars.y >= 0)
                    pointToTurn.y *= -1.5;
                else
                    pointToTurn.y *= 1.5;

                if (sumOfAngulars.z >= 0)
                    pointToTurn.z *= -1.5;
                else
                    pointToTurn.z *= 1.5;

                steerStrengthFactor = HelperFunctions.limit(1000 * Math.Pow(launchData_Altitude - vessel.orbit.altitude, -1), 0.1, 1000);

                //Calculating the desired pitch to follow
                //The gravity curve is made up of three different functions to keep things "simple"
                double x = vessel.orbit.altitude / 1000.0d;

                if (vessel.orbit.altitude <= 10075)
                    desiredHeading.x = (0.014013 * (x * x * x)) - (0.448716 * (x * x)) + (5.730697 * x) + 0.32134;
                else if (vessel.orbit.altitude > 10075 && vessel.orbit.altitude <= 97800)
                    desiredHeading.x = (0.00003 * (x * x * x)) - (0.008594 * (x * x)) + (1.075113 * x) + 16.851515;
                else if (vessel.orbit.altitude > 97800 && vessel.orbit.altitude < 250000)
                    desiredHeading.x = -(0.000889 * (x * x)) + (0.456161 * x) + 31.754229;
                else
                    desiredHeading.x = 90;

                //The graph on desmos: https://www.desmos.com/calculator/xf4focqsec

                /*
                //Calculate the angle we need to add, because the Earth/Kerbin curves "down" as we travel downrange
                Vector3d vesselVector = vessel.CoM - FlightGlobals.Bodies[1].transform.position;
                downrangeDistance = (FlightGlobals.Bodies[1].Radius * HelperFunctions.radAngle(markVector, vesselVector));

                double angleCorrection = ((downrangeDistance / bodysCircumference) * 360);

                //Add it up
                desiredHeading.x = desiredHeading.x + angleCorrection;
                */

                //https://www.kerbalspaceprogram.com/ksp/api/class_direction_target.html
                //vessel, pitch, heading, true means degree
                directionAscentGuidance.Update(vessel, (90 - desiredHeading.x), desiredHeading.y, true);// desiredHeading.y, true);

                if (correctiveSteering)
                {
                    //https://www.kerbalspaceprogram.com/ksp/api/class_direction_target.html
                    //vessel, pitch, heading, true means degree
                    //directionAscentGuidance.Update(vessel, (90 - desiredHeading.x), 0, true);// desiredHeading.y, true);
                }

                DRAW();

                //ScreenMessages.PostScreenMessage("AG - HOR: " + HelperFunctions.degAngle(directionAscentGuidance.GetFwdVector(), orbitalNormal), 0.5f, ScreenMessageStyle.UPPER_LEFT);
                //ScreenMessages.PostScreenMessage("AG - SKY: " + HelperFunctions.degAngle(directionAscentGuidance.GetFwdVector(), orbitalPrograde), 0.5f, ScreenMessageStyle.UPPER_RIGHT);
                yield return new WaitForSeconds(.05f);
            }
        }

        void DRAW()
        {
            ScreenMessages.PostScreenMessage("LineDrawing active", 0.1f, ScreenMessageStyle.UPPER_CENTER);

            Vector3 startingPoint = vessel.CoM;

            /////////////AIR-PROGRADE LINE/////////////////////
            ///
            var obj3 = new GameObject("AirPrograde");
            var airline = obj3.AddComponent<LineRenderer>();
            Destroy(airline, 0.05f);
            obj3.transform.parent = gameObject.transform;
            obj3.transform.localPosition = Vector3.zero;

            airline.sortingLayerName = "OnTop";
            airline.sortingOrder = 5;
            //line.positionCount(1);
            Vector3 endPoint = vessel.CoM + 2 * (vessel.srf_velocity.normalized);
            airline.SetPosition(0, startingPoint);
            airline.SetPosition(1, endPoint);
            airline.startWidth = 0.05f;
            airline.endWidth = 0.01f;
            airline.useWorldSpace = true;

            Material LineMaterial3 = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            airline.material = LineMaterial3;

            Gradient gradient3 = new Gradient();
            Color orange = new Color(1.0f, 0.64f, 0.0f);
            gradient3.SetKeys
                (
                new GradientColorKey[] { new GradientColorKey(orange, 0.0f), new GradientColorKey(orange, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
                );

            airline.colorGradient = gradient3;

            /////////////VAC-PROGRADE LINE/////////////////////

            var obj4 = new GameObject("VacuumPrograde");
            var vacline = obj4.AddComponent<LineRenderer>();
            Destroy(vacline, 0.05f);
            obj4.transform.parent = gameObject.transform;
            obj4.transform.localPosition = Vector3.zero;

            vacline.sortingLayerName = "OnTop";
            vacline.sortingOrder = 5;
            //line.positionCount(1);
            endPoint = vessel.CoM + 2 * (vessel.obt_velocity.normalized);
            vacline.SetPosition(0, startingPoint);
            vacline.SetPosition(1, endPoint);
            vacline.startWidth = 0.05f;
            vacline.endWidth = 0.01f;
            vacline.useWorldSpace = true;

            Material LineMaterial4 = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            vacline.material = LineMaterial4;

            Gradient gradient4 = new Gradient();
            gradient4.SetKeys
                (
                new GradientColorKey[] { new GradientColorKey(Color.red, 0.0f), new GradientColorKey(Color.red, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
                );

            vacline.colorGradient = gradient4;

            /////////////TARGET LINE/////////////////////

            var obj5 = new GameObject("TARGET");
            var tarline = obj5.AddComponent<LineRenderer>();
            Destroy(tarline, 0.05f);
            obj5.transform.parent = gameObject.transform;
            obj5.transform.localPosition = Vector3.zero;

            tarline.sortingLayerName = "OnTop";
            tarline.sortingOrder = 5;
            //line.positionCount(1);
            endPoint = vessel.CoM + 2 * (directionAscentGuidance.direction.normalized);
            tarline.SetPosition(0, startingPoint);
            tarline.SetPosition(1, endPoint);
            tarline.startWidth = 0.05f;
            tarline.endWidth = 0.01f;
            tarline.useWorldSpace = true;

            Material LineMaterial5 = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            tarline.material = LineMaterial5;

            Gradient gradient5 = new Gradient();
            gradient5.SetKeys
                (
                new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
                );

            tarline.colorGradient = gradient5;

            /////////////ORB-PROGRADE LINE/////////////////////
            /*
            var obj0 = new GameObject("orbitalPrograde");
            var orbproline = obj0.AddComponent<LineRenderer>();
            Destroy(orbproline, 0.05f);
            obj0.transform.parent = gameObject.transform;
            obj0.transform.localPosition = Vector3.zero;

            orbproline.sortingLayerName = "OnTop";
            orbproline.sortingOrder = 5;
            //line.positionCount(1);
            endPoint = vessel.CoM + orbitalPrograde;
            orbproline.SetPosition(0, startingPoint);
            orbproline.SetPosition(1, endPoint);
            orbproline.startWidth = 0.05f;
            orbproline.endWidth = 0.01f;
            orbproline.useWorldSpace = true;

            Material LineMaterial0 = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            orbproline.material = LineMaterial0;

            Gradient gradient0 = new Gradient();
            gradient0.SetKeys
                (
                new GradientColorKey[] { new GradientColorKey(Color.yellow, 0.0f), new GradientColorKey(Color.yellow, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
                );

            orbproline.colorGradient = gradient0;

            /////////////ORB-NORMAL LINE/////////////////////

            var obj1 = new GameObject("orbitalNormal");
            var orbnorline = obj1.AddComponent<LineRenderer>();
            Destroy(orbnorline, 0.05f);
            obj1.transform.parent = gameObject.transform;
            obj1.transform.localPosition = Vector3.zero;

            orbnorline.sortingLayerName = "OnTop";
            orbnorline.sortingOrder = 5;
            //line.positionCount(1);
            endPoint = vessel.CoM + orbitalNormal;
            orbnorline.SetPosition(0, startingPoint);
            orbnorline.SetPosition(1, endPoint);
            orbnorline.startWidth = 0.05f;
            orbnorline.endWidth = 0.01f;
            orbnorline.useWorldSpace = true;

            Material LineMaterial1 = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            orbnorline.material = LineMaterial1;

            Gradient gradient1 = new Gradient();
            gradient1.SetKeys
                (
                new GradientColorKey[] { new GradientColorKey(Color.cyan, 0.0f), new GradientColorKey(Color.cyan, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
                );

            orbnorline.colorGradient = gradient1;

            /////////////ORB-TANGENT LINE/////////////////////

            var obj2 = new GameObject("orbitalRadial");
            var orbradline = obj2.AddComponent<LineRenderer>();
            Destroy(orbradline, 0.05f);
            obj1.transform.parent = gameObject.transform;
            obj1.transform.localPosition = Vector3.zero;

            orbradline.sortingLayerName = "OnTop";
            orbradline.sortingOrder = 5;
            //line.positionCount(1);
            endPoint = vessel.CoM + orbitalRadial;
            orbradline.SetPosition(0, startingPoint);
            orbradline.SetPosition(1, endPoint);
            orbradline.startWidth = 0.05f;
            orbradline.endWidth = 0.01f;
            orbradline.useWorldSpace = true;

            Material LineMaterial2 = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            orbradline.material = LineMaterial2;

            Gradient gradient2 = new Gradient();
            gradient2.SetKeys
                (
                new GradientColorKey[] { new GradientColorKey(Color.magenta, 0.0f), new GradientColorKey(Color.magenta, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
                );

            orbradline.colorGradient = gradient2;
            */
        }
        //This function will write all the messages on the screen
        private IEnumerator coroutinePrintMessage()
        {
            for (; ; )
            {
                //Now to check if we are not on the launch pad
                if (vessel.situation != Vessel.Situations.PRELAUNCH)
                {
                    /*
                    //Time to announce the upcoming ignition event
                    if (nextMessageStep == 0 && activeCoroutine == 1)
                    {
                        ScreenMessages.PostScreenMessage("Rolling into correct Azimuth.", 5f, ScreenMessageStyle.UPPER_CENTER);
                        nextMessageStep++;
                    }
                    else if (nextMessageStep == 1 && activeCoroutine >= 2)
                    {
                        ScreenMessages.PostScreenMessage("Following the ascent path.", 10f, ScreenMessageStyle.UPPER_CENTER);
                        nextMessageStep++;
                        yield break;
                    }
                    */
                }

                yield return new WaitForSeconds(1.2f);
            }
        }

        //Hide all the fields
        private void endMod()
        {
            if (modInUse)
            {
                modInUse = false;
                PAWmodInUse = StringDisconnected;

                Fields[nameof(desiredInclination)].guiActiveEditor = false;
                Fields[nameof(desiredOrbitalDirection)].guiActiveEditor = false;
                Fields[nameof(desiredRoll)].guiActiveEditor = false;
                Fields[nameof(correctiveSteering)].guiActiveEditor = false;
                Fields[nameof(eventMessagingWanted)].guiActiveEditor = false;

                //Update the size of the PAW
                MonoUtilities.RefreshPartContextWindow(part);

                activeCoroutine = 0;
            }
        }

        //Gets called when the part explodes etc.
        private void isDead(Part part)
        {
            //Stopping all the coroutines that might be running
            StopCoroutine(coroutineSteeringMeasurements());
            StopCoroutine(coroutineCalculateAutoAscent());
            StopCoroutine(coroutinePrintMessage());
        }

        #endregion
    }
}
