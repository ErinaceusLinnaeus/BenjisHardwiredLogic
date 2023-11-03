﻿using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using VehiclePhysics;

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


        //TEMPTEMPTEMPTEMPTEMP//
        [KSPField(isPersistant = true, guiActive = true)]
        double rollPlaneVector_shipLeft;
        [KSPField(isPersistant = true, guiActive = true)]
        double rollPlaneVector_shipBelly;
        [KSPField(isPersistant = true, guiActive = true)]
        double VECVECsteering;


        //Colors
        Color orange = new Color(1.0f, 0.64f, 0.0f);
        Color lightblue = new Color(0.67f, 0.84f, 0.9f);

        //Angles between orbital-radial at launchsite and the airstream at T=-1
        //[KSPField(isPersistant = true, guiActive = false)]
        //private double Tm1AngleOrbitalAirstream = 0;
        //Angles between orbital-radial at launchsite and the airstream at t=0
        [KSPField(isPersistant = true, guiActive = false)]
        private double T0AngleOrbitalAirstream = 0;

        //Angles between orbital-radial at launchsite and the rocket's (vacuum) prograde at T=-1
        //[KSPField(isPersistant = true, guiActive = false)]
        //private double tm1AngleOrbitalVacPro = 0;
        //Angles between orbital-radial at launchsite and the rocket's (vacuum) prograde at t=0
        [KSPField(isPersistant = true, guiActive = false)]
        private double T0AngleOrbitalVacPro = 0;

        //Angles between orbital-radial at launchsite and the targeted ascent at T=-1
        //[KSPField(isPersistant = true, guiActive = false)]
        //private double Tm1AngleOrbitalTarget = 0;
        //Angles between orbital-radial at launchsite and the targeted ascent at t=0
        [KSPField(isPersistant = true, guiActive = false)]
        private double T0AngleOrbitalTarget = 0;

        //Vectors we need
        [KSPField(isPersistant = true, guiActive = false)]
        Vector3d shipLeft;
        Vector3d shipBelly;
        Vector3d rollPlaneVector;

        //steeringModes
        //  0 : Roll
        //  1 : Gravity Turn
        int steeringMode;

        //Saving UniversalTime into launchTime when the Vessel getÞs launched
        [KSPField(isPersistant = true, guiActive = false)]
        private double launchData_Time = 0;

        //The azimuth we're heading at
        [KSPField(isPersistant = true, guiActive = true)]
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

        //Creating an launchsiteFrame, saving the initial state...just like a gimbal
        [KSPField(isPersistant = true, guiActive = false)]
        private Vector3d launchsitePrograde;
        [KSPField(isPersistant = true, guiActive = false)]
        private Vector3d launchsiteRadial;
        [KSPField(isPersistant = true, guiActive = false)]
        private UnityEngine.Quaternion launchsiteFrame;
        [KSPField(isPersistant = true, guiActive = false)]
        private Vector3d launchsiteNormal;

        [KSPField(isPersistant = true, guiActive = false)]
        private double launchData_SiteLat;
        [KSPField(isPersistant = true, guiActive = false)]
        private double launchData_SiteLong;

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
        private DirectionTarget uncorrecteddirectionAscentGuidance = new DirectionTarget("uncorrecteddirectionAscentGuidance");
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
        private void steeringCommandGravityTurn(FlightCtrlState state)
        {
            //state.pitch = (float)HelperFunctions.limit((((vessel.angularVelocityD.x + (sumOfAngulars.x + pointToTurn.x) * steerStrengthFactor)) / TimeWarp.CurrentRate), -1, 1);
            //state.roll = (float)HelperFunctions.limit((((vessel.angularVelocityD.y + (sumOfAngulars.y + pointToTurn.y) * steerStrengthFactor)) / TimeWarp.CurrentRate), -1, 1);
            state.roll = (float)HelperFunctions.limit((((vessel.angularVelocityD.y + (desiredHeading.y - HelperFunctions.degAngle(rollPlaneVector, shipLeft) + pointToTurn.y) * steerStrengthFactor)) / TimeWarp.CurrentRate), -0.05, 0.05);
            //state.yaw = (float)HelperFunctions.limit((((vessel.angularVelocityD.z + (sumOfAngulars.z + pointToTurn.z) * steerStrengthFactor)) / TimeWarp.CurrentRate), -1, 1);

        }
        private void steeringCommandRoll(FlightCtrlState state)
        {
            state.pitch = (float)HelperFunctions.limit((((vessel.angularVelocityD.x + (sumOfAngulars.x + pointToTurn.x) * steerStrengthFactor)) / TimeWarp.CurrentRate), -1, 1);
            state.roll = (float)HelperFunctions.limit((((vessel.angularVelocityD.y + (desiredHeading.y - HelperFunctions.degAngle(rollPlaneVector, shipLeft) + pointToTurn.y) * steerStrengthFactor)) / TimeWarp.CurrentRate), -0.05, 0.05);
            state.yaw = (float)HelperFunctions.limit((((vessel.angularVelocityD.z + (sumOfAngulars.z + pointToTurn.z) * steerStrengthFactor)) / TimeWarp.CurrentRate), -1, 1);

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

            //Creating an launchsiteFrame, saving the initial state...just like a gimbal
            launchsitePrograde = vessel.obt_velocity.normalized;
            launchsiteRadial = (vessel.CoMD - vessel.mainBody.position).normalized;
            //Not used right now, but to have it complete:
            launchsiteFrame = UnityEngine.QuaternionD.LookRotation(launchsitePrograde, launchsiteRadial);
            launchsiteNormal = launchsiteFrame * Vector3d.left;
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
                //launching towards north
                if (launchData_SiteLat < 0)
                {
                    desiredHeading.y = azimuth;
                    desiredHeading.z = (90 - azimuth);
                }
                //launching towards south
                else
                {
                    desiredHeading.y = azimuth;
                    desiredHeading.z = (90 + azimuth);
                }
            }
            //And turning it the other direction if launching retrograde
            else
            {
                //launching towards north
                if (launchData_SiteLat < 0)
                {
                    desiredHeading.y = 180 - azimuth;
                    desiredHeading.z = (270 - azimuth);
                }
                //launching towards south
                else
                {
                    desiredHeading.y = 180 - azimuth;
                    desiredHeading.z = (270 + azimuth);
                }
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
            //StartCoroutine(coroutineCalculateAutoAscent());
            StartCoroutine(coroutineUpdateVectors());
            StartCoroutine(coroutineShowVectors());
            StartCoroutine(coroutineSteeringCheck());

            steeringMode = 0;

        }

        //This corourine will check which steering ist active and switch to the next when needed
        private IEnumerator coroutineSteeringCheck()
        {
            activeCoroutine = 1;
            for (; ; )
            {
                if (steeringMode == 0)
                {
                    vessel.OnFlyByWire += steeringCommandRoll;
                    steeringMode = 1;
                }
                if (steeringMode == 1 && vessel.srfSpeed > 100) //THE GRAVITY TURN KICKS-IN A BIT VIOLENTLY
                {
                    StartCoroutine(coroutineCalculateAutoAscent());
                    vessel.OnFlyByWire -= steeringCommandRoll;
                    vessel.OnFlyByWire += steeringCommandGravityTurn;
                }
                yield return new WaitForSeconds(.1f);
            }
        }

        //This coroutine will update all the vectors in use
        private IEnumerator coroutineUpdateVectors()
        {
            activeCoroutine = 1;
            for (; ; )
            {
                /*
                shipLeft = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.left).normalized;
                shipBelly = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(90, 0, 0) * Vector3d.up).normalized;

                Vector3d rollPlane = Vector3d.Cross(shipLeft, shipBelly);
                rollPlaneVector = Vector3.ProjectOnPlane(launchsiteNormal, rollPlane);
                */
                shipLeft = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.left).normalized;
                shipBelly = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(90, 0, 0) * Vector3d.up).normalized;

                //LaunchSite sits north of the equator -> launching south
                if (launchData_SiteLat >= 0)
                    shipLeft = - shipLeft;

                Vector3d rollPlane = Vector3d.Cross(shipLeft, shipBelly);
                rollPlaneVector = Vector3.ProjectOnPlane(launchsiteNormal, rollPlane);

                yield return new WaitForSeconds(.025f);
            }
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

                /*
                Tm1AngleOrbitalTarget = T0AngleOrbitalTarget;
                Tm1AngleOrbitalAirstream = T0AngleOrbitalAirstream;
                tm1AngleOrbitalVacPro = T0AngleOrbitalVacPro;
                */
                T0AngleOrbitalTarget = HelperFunctions.degAngle(directionAscentGuidance.direction, launchsiteRadial);
                T0AngleOrbitalAirstream = HelperFunctions.degAngle(vessel.srf_velocity, launchsiteRadial);
                T0AngleOrbitalVacPro = HelperFunctions.degAngle(vessel.obt_velocity, launchsiteRadial);
                

                yield return new WaitForSeconds(.1f);
            }
        }
        
        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        private IEnumerator coroutineShowVectors()
        {
            activeCoroutine = 1;
            for (; ; )
            {
                //orbital directions at the launchsite

                //DebugLines.draw(vessel, "launchsitePrograde", launchsitePrograde, Color.yellow);
                //DebugLines.draw(vessel, "launchsiteNormal", launchsiteNormal, Color.cyan);
                //DebugLines.draw(vessel, "launchsiteRadial", launchsiteRadial, Color.magenta);

                DebugLines.draw(vessel, "shipLeft", shipLeft, Color.gray);
                DebugLines.draw(vessel, "shipBelly", shipBelly, Color.white);

                DebugLines.draw(vessel, "rollPlaneVector", rollPlaneVector, Color.cyan);

                rollPlaneVector_shipLeft = HelperFunctions.degAngle(rollPlaneVector, shipLeft);
                rollPlaneVector_shipBelly = HelperFunctions.degAngle(rollPlaneVector, shipBelly);
                VECVECsteering = (desiredHeading.y - HelperFunctions.degAngle(rollPlaneVector, shipLeft));

                ScreenMessages.PostScreenMessage("rollPlaneVector-shipLeft: " + HelperFunctions.degAngle(rollPlaneVector, shipLeft), 0.4f, ScreenMessageStyle.UPPER_CENTER);
                ScreenMessages.PostScreenMessage("rollPlaneVector-shipBelly: " + HelperFunctions.degAngle(rollPlaneVector, shipBelly), 0.4f, ScreenMessageStyle.UPPER_LEFT);
                ScreenMessages.PostScreenMessage("steering: " + (desiredHeading.y - HelperFunctions.degAngle(rollPlaneVector, shipLeft)), 0.4f, ScreenMessageStyle.UPPER_RIGHT);
                
                yield return new WaitForSeconds(.05f);
            }
        }
        

        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        private IEnumerator coroutineCalculateAutoAscent()
        {
            activeCoroutine = 1;
            for (; ; )
            {

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

                //The graph on desmos: https://www.desmos.com/calculator/acq2muuqyq


                //Where the vessel would be pointing without corrective steering
                uncorrecteddirectionAscentGuidance.Update(vessel, (90 - desiredHeading.x), desiredHeading.z, true);
                DebugLines.draw(vessel, "uncorrectedTarget", uncorrecteddirectionAscentGuidance.direction, Color.blue);

                if (vessel.srfSpeed > 100) //100 UP FOR DEBATE WHEN SWITCHING TO RO
                {
                    if (correctiveSteering)
                    {
                        //ScreenMessages.PostScreenMessage("azimuth: " + azimuth, 0.4f, ScreenMessageStyle.UPPER_RIGHT);

                        //ScreenMessages.PostScreenMessage("dyn-pressure: " + vessel.dynamicPressurekPa, 0.4f, ScreenMessageStyle.UPPER_LEFT);
                        double maxSteerCorrection = Math.Pow(1.1, (45 - vessel.dynamicPressurekPa)) - 12;
                        //The graph on desmos: https://www.desmos.com/calculator/ylfkjhhf8i
                        //MaxQ (in stock) usually between 15 and 20

                        Vector3 directionToFlyTo;

                        double correctiveAngle = 0;

                        if (vessel.dynamicPressurekPa > 2) //2 UP FOR DEBATE WHEN SWITCHING TO RO
                        {
                            directionToFlyTo = vessel.srf_velocity;
                            maxSteerCorrection = HelperFunctions.limitAbs(maxSteerCorrection, HelperFunctions.degAngle(directionAscentGuidance.direction, directionToFlyTo));// vessel.srf_velocity));
                            correctiveAngle = HelperFunctions.limit(T0AngleOrbitalTarget - T0AngleOrbitalAirstream, -maxSteerCorrection, maxSteerCorrection);
                        }
                        else
                        {
                            //Slowly transition from the rocket's airstream velocity vector to its orbital prograde vector, while dynamic pressure falls from 2 to 0
                            if (vessel.dynamicPressurekPa <= 2 && vessel.dynamicPressurekPa > 1)
                            {
                                directionToFlyTo = vessel.srf_velocity + vessel.obt_velocity * ( -(vessel.dynamicPressurekPa - 2));
                            }
                            if (vessel.dynamicPressurekPa <= 1 && vessel.dynamicPressurekPa > 0)
                            {
                                directionToFlyTo = vessel.obt_velocity + (vessel.srf_velocity / vessel.dynamicPressurekPa);
                            }
                            else
                            {
                                directionToFlyTo = vessel.obt_velocity;
                            }
                            maxSteerCorrection = HelperFunctions.limitAbs(maxSteerCorrection, HelperFunctions.degAngle(directionAscentGuidance.direction, directionToFlyTo));// vessel.obt_velocity));
                            correctiveAngle = HelperFunctions.limit(T0AngleOrbitalTarget + T0AngleOrbitalVacPro, -maxSteerCorrection, maxSteerCorrection);
                        }

                        //https://www.kerbalspaceprogram.com/ksp/api/class_direction_target.html
                        //vessel, pitch, heading, true means degree
                        directionAscentGuidance.Update(vessel, (90 - desiredHeading.x - correctiveAngle), desiredHeading.z, true);
                    }
                    else
                    {
                        //https://www.kerbalspaceprogram.com/ksp/api/class_direction_target.html
                        //vessel, pitch, heading, true means degree
                        directionAscentGuidance.Update(vessel, (90 - desiredHeading.x), desiredHeading.z, true);
                    }
                }
                else
                {
                    directionAscentGuidance.Update(vessel, (90 - desiredHeading.x), desiredHeading.z, true);
                }

                //Draw some Debuglines
                //Color orange = new Color(1.0f, 0.64f, 0.0f);
                //Color lightblue = new Color(0.67f, 0.84f, 0.9f);

                //vessel directions
                DebugLines.draw(vessel, "AirPrograde", vessel.srf_velocity, orange);
                DebugLines.draw(vessel, "VacuumPrograde", vessel.obt_velocity, Color.red);
                DebugLines.draw(vessel, "Target", directionAscentGuidance.direction, lightblue);

                
                //ScreenMessages.PostScreenMessage("Airstream - Target: " + HelperFunctions.degAngle(directionAscentGuidance.direction, vessel.srf_velocity), 0.4f, ScreenMessageStyle.UPPER_LEFT);
                //ScreenMessages.PostScreenMessage("VacProgra - Target: " + HelperFunctions.degAngle(directionAscentGuidance.direction, vessel.obt_velocity), 0.4f, ScreenMessageStyle.UPPER_LEFT);

                //ScreenMessages.PostScreenMessage("Airstream - Radial: " + HelperFunctions.degAngle(vessel.srf_velocity, launchsiteRadial), 0.4f, ScreenMessageStyle.UPPER_CENTER);
                //ScreenMessages.PostScreenMessage("Target - Radial: " + HelperFunctions.degAngle(directionAscentGuidance.direction, launchsiteRadial), 0.4f, ScreenMessageStyle.UPPER_RIGHT);


                //orbital directions at the launchsite

                //DebugLines.draw(vessel, "launchsitePrograde", launchsitePrograde, Color.yellow);
                //DebugLines.draw(vessel, "launchsiteNormal", launchsiteNormal, Color.cyan);
                //DebugLines.draw(vessel, "launchsiteRadial", launchsiteRadial, Color.magenta);

                yield return new WaitForSeconds(.05f);
            }
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
