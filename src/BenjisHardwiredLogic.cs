namespace BenjisHardwiredLogic
{
    using System;
    public class BenjisAutoAscent : PartModule
    {
        #region Fields

        //Did the launch happen?
        [KSPField(isPersistant = true, guiActive = false)]
        private bool vesselLaunched = false;

        //Saving UniversalTime into launchTime when the Vessel getÞs launched
        [KSPField(isPersistant = true, guiActive = false)]
        private double launchTime = 0;

        //Is this rocket guided?
        [KSPField(isPersistant = true, guiActive = false)]
        private bool guidingAscent = false;

        //IKeep track of if we started steering
        [KSPField(isPersistant = true, guiActive = false)]
        private bool steeringLocked = false;

        //To keep track of the stage we're in during the ascent guidance
        //0 = calculatingTWR
        //1 = calculateAzimuth
        //2 = initialPitch
        //3 = followingPrograge
        //4 = followingAzimuth
        //5 = hittingPerigee
        //6 = hittingApogee
        [KSPField(isPersistant = true, guiActive = false)]
        private int guidanceState = 0;

        //The azimuth we're heading at
        [KSPField(isPersistant = true, guiActive = false)]
        private double azimuth = 0;

        //Save the direction we need to pitch and yaw to, depending on the azimuth and how the rocket is oriented on the pad
        //x: pitch
        //y: yaw
        [KSPField(isPersistant = true, guiActive = false)]
        UnityEngine.Vector2d headingToAzimuth;

        //The initial TWR, will be calculated at launch
        [KSPField(isPersistant = true, guiActive = false)]
        private double initialTWR = 0;
        bool initialCalculationsDone = false;

        //Creating an orbitalFrame, saving the initial state...just like a gimbal
        Vector3d orbitalPrograde;
        Vector3d orbitalRadial;
        UnityEngine.Quaternion orbitalFrame;
        Vector3d orbitalNormal;

        //Headline name for the GUI
        [KSPField(isPersistant = true, guiActive = false)]
        private const string PAWAscentGroupName = "Benji's Auto Ascent";

        //Text, if functionality is disabled/enabled
        [KSPField(isPersistant = true, guiActive = false)]
        private const string PAWTextDisabled = "disconnected";

        [KSPField(isPersistant = true, guiActive = false)]
        private const string PAWTextEnabled = "connected";

        //Text, if event messaging is disabled/enabled
        [KSPField(isPersistant = true, guiActive = false)]
        private const string MessagingDisabled = "inactive";

        [KSPField(isPersistant = true, guiActive = false)]
        private const string MessagingEnabled = "active";

        //A button to enable or disable the function
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Circuits are:", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_Toggle(disabledText = PAWTextDisabled, enabledText = PAWTextEnabled)]
        private bool modInUse = false;

        //Specify the perigee you wanna end up at - Earth's SOI ends at 900.000km, but let's be sensible
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Perigee [km]", guiFormat = "F0", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 499999.9f, incrementLarge = 50f, incrementSmall = 10f, incrementSlide = 1f, sigFigs = 1)]
        private float desiredPerigee = 0;

        //Specify the apogee you wanna end up at
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Apogee [km]", guiFormat = "F0", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 500000.0f, incrementLarge = 50f, incrementSmall = 10f, incrementSlide = 1f, sigFigs = 1)]
        private float desiredApogee = 0;

        //Specify the inclination you wanna end up at
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Inclination [°]", guiFormat = "F1", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 359.9f, incrementLarge = 5f, incrementSmall = 1f, incrementSlide = 0.1f, sigFigs = 1)]
        private float desiredInclination = 0;

        //Specify how agressively the gravity turn should be pointed down from the flight vector 
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Gravity Turn [°]", guiFormat = "F1", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 5f, incrementLarge = 1f, incrementSmall = 0.1f, incrementSlide = 0.1f, sigFigs = 1)]
        private float gravityTurnAngle = 3;

        //Specify if we should follow the azimuth to the north or the south
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Launchdirection", guiFormat = "F1", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_ChooseOption(options = new string[4] { "SE", "NE", "NW", "SW" })]
        private string desiredDirectionToLaunch = "SE";

        //Shows if the decoupler is active
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Circuits are", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName)]
        private string PAWmodInUse;

        //Shows the perigee we try to end up at
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Orbit", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName)]
        private string desiredOrbit;

        //A button to enable or disable if a message for this event will be shown
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Event Messaging:", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_Toggle(disabledText = MessagingDisabled, enabledText = MessagingEnabled)]
        private bool eventMessaging = true;

        int testyCount = 5;
        int testyTime = 0;

        double pitchAngleActual = 0;
        double pitchAngleDesired = 0;
        
        #endregion

        #region steering delegates

        void lazyFlight(FlightCtrlState state)
        {
            state.pitch = 0;
            state.yaw = 0;
            state.roll = 0;
        }
        void initiateGravityTurn(FlightCtrlState state)
        {
            //Ship's pointing that way
            Vector3d shipForward = vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.forward;
            pitchAngleActual = HelperFunctions.degAngle(orbitalRadial, shipForward);
            double leanfactor = (pitchAngleDesired - pitchAngleActual) / 100d;

            state.pitch = (float)((double)headingToAzimuth.x * leanfactor);
            state.yaw = (float)((double)headingToAzimuth.y * leanfactor);
            state.roll = 0;
        }
        void rollManeuver(FlightCtrlState state)
        {
            //Ship's pointing that way
            Vector3d shipLeft = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.left).normalized;
            double rollAngle = HelperFunctions.degAngle(shipLeft, orbitalRadial);
            //ScreenMessages.PostScreenMessage("left.rad. : " + (HelperFunctions.degAngle(shipLeft, orbitalRadial)), 0.1f, ScreenMessageStyle.UPPER_LEFT);
            //ScreenMessages.PostScreenMessage("roll : " + (float)((rollAngle - 90d) / 20d), 0.1f, ScreenMessageStyle.UPPER_CENTER);

            state.pitch = 0;
            state.yaw = 0;
            state.roll = (float)HelperFunctions.limitAbs(((rollAngle - 90d) / 20d), 0.05);
        }
        void enforcingGravityTurn(FlightCtrlState state)
        {
            //Ship's pointing that way
            Vector3d shipLeft = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.left).normalized;
            double rollAngle = HelperFunctions.degAngle(shipLeft, orbitalRadial);

            //Ship's flying that way (airflow)
            Vector3d flightDirectionAirflow = vessel.srf_velocity.normalized;
            //Ship's pointing that way
            Vector3d shipUp = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.up).normalized;
            double pitchAngle = HelperFunctions.degAngle(shipUp, flightDirectionAirflow);

            Vector3d shipForward = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.forward).normalized;

            //ScreenMessages.PostScreenMessage("forw.air. : " + ((int)HelperFunctions.degAngle(shipForward, flightDirectionAirflow)), 0.1f, ScreenMessageStyle.UPPER_LEFT);
            //ScreenMessages.PostScreenMessage("left.air. : " + ((int)HelperFunctions.degAngle(shipLeft, flightDirectionAirflow)), 0.1f, ScreenMessageStyle.UPPER_CENTER);
            //ScreenMessages.PostScreenMessage("up.air.: " + ((int)HelperFunctions.degAngle(shipUp, flightDirectionAirflow)), 0.1f, ScreenMessageStyle.UPPER_RIGHT);
            //ScreenMessages.PostScreenMessage("pitch.: " + -1 * (float)((pitchAngle - 90d + gravityTurnAngle) / 30d), 0.1f, ScreenMessageStyle.UPPER_CENTER);

            ScreenMessages.PostScreenMessage("forw.air. : " + ((int)HelperFunctions.degAngle(shipForward, flightDirectionAirflow)), 0.1f, ScreenMessageStyle.UPPER_LEFT);
            ScreenMessages.PostScreenMessage("left.air. : " + ((int)HelperFunctions.degAngle(shipLeft, flightDirectionAirflow)), 0.1f, ScreenMessageStyle.UPPER_CENTER);
            ScreenMessages.PostScreenMessage("up.air.: " + ((int)HelperFunctions.degAngle(shipUp, flightDirectionAirflow)), 0.1f, ScreenMessageStyle.UPPER_RIGHT);
            //ScreenMessages.PostScreenMessage("pitch.: " + -1 * (float)((pitchAngle - 90d + gravityTurnAngle) / 30d), 0.1f, ScreenMessageStyle.UPPER_CENTER);


            state.pitch = -1 * (float)((pitchAngle - 90d + gravityTurnAngle) / 30d);
            state.yaw = 0;
            state.roll = (float)HelperFunctions.limitAbs(((rollAngle - 90d) / 20d), 0.05);
        }
        void turningTowardsOrbitalPrograde(FlightCtrlState state)
        {
            //Ship's pointing that way
            Vector3d shipLeft = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.left).normalized;
            double rollAngle = HelperFunctions.degAngle(shipLeft, orbitalRadial);

            //Ship's flying that way (orbital)
            Vector3d flightDirectionOrbital = vessel.obt_velocity.normalized;
            //Ship's pointing that way
            Vector3d shipUp = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.up).normalized;
            double pitchAngle = HelperFunctions.degAngle(shipUp, flightDirectionOrbital);

            double yawAngle = HelperFunctions.degAngle(shipLeft, flightDirectionOrbital);

            Vector3d shipForward = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.forward).normalized;

            ScreenMessages.PostScreenMessage("forw.orb. : " + ((int)HelperFunctions.degAngle(shipForward, flightDirectionOrbital)), 0.1f, ScreenMessageStyle.UPPER_LEFT);
            ScreenMessages.PostScreenMessage("left.orb. : " + ((int)HelperFunctions.degAngle(shipLeft, flightDirectionOrbital)), 0.1f, ScreenMessageStyle.UPPER_CENTER);
            ScreenMessages.PostScreenMessage("up.orb.: " + ((int)HelperFunctions.degAngle(shipUp, flightDirectionOrbital)), 0.1f, ScreenMessageStyle.UPPER_RIGHT);
            //ScreenMessages.PostScreenMessage("pitch.: " + -1 * (float)((pitchAngle - 90d + gravityTurnAngle) / 30d), 0.1f, ScreenMessageStyle.UPPER_CENTER);


            state.pitch = -1 * (float)HelperFunctions.limitAbs(((pitchAngle - 90d) / 30d), 0.05);
            state.yaw = (float)HelperFunctions.limitAbs(((yawAngle - 90d) / 20d), 0.05);
            state.roll = (float)HelperFunctions.limitAbs(((rollAngle - 90d) / 20d), 0.05);
        }
        void correctingInclination(FlightCtrlState state)
        {
            //Ship's pointing that way
            Vector3d shipLeft = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.left).normalized;
            double rollAngle = HelperFunctions.degAngle(shipLeft, orbitalRadial);

            //Ship's flying that way (orbital)
            Vector3d flightDirectionOrbital = vessel.obt_velocity.normalized;
            //Ship's pointing that way
            Vector3d shipUp = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.up).normalized;
            double pitchAngle = HelperFunctions.degAngle(shipUp, flightDirectionOrbital);

            //ScreenMessages.PostScreenMessage("up.orb.: " + ((int)HelperFunctions.degAngle(shipUp, flightDirectionOrbital)), 0.1f, ScreenMessageStyle.UPPER_RIGHT);
            //ScreenMessages.PostScreenMessage("pitch.: " + -1 * (float)((pitchAngle - 90d) / 30d), 0.1f, ScreenMessageStyle.UPPER_CENTER);
            ScreenMessages.PostScreenMessage("desired inc - vessel.inc: " + (float)HelperFunctions.limitAbs((desiredInclination - vessel.orbit.inclination), 0.05), 0.1f, ScreenMessageStyle.UPPER_CENTER);

            state.pitch = -1 * (float)HelperFunctions.limitAbs(((pitchAngle - 90d) / 30d), 0.1);
            state.yaw = (float)HelperFunctions.limitAbs((desiredInclination - vessel.orbit.inclination), 0.3);
            state.roll = (float)HelperFunctions.limitAbs(((rollAngle - 90d) / 20d), 0.2);
        }
        void hittingPerigee(FlightCtrlState state)
        {
            state.pitch = 0;
            state.yaw = 0;
            state.roll = 0;
        }
        void hittingApogee(FlightCtrlState state)
        {
            state.pitch = 0;
            state.yaw = 0;
            state.roll = 0;
        }

        #endregion

        #region Overrides

        //This happens once
        public override void OnStart(StartState state)
        {
            //enum of Situations - https://kerbalspaceprogram.com/api/class_vessel.html
            if (vessel.situation == Vessel.Situations.PRELAUNCH)
            {
                //In case the user input is messed up...
                if (desiredApogee < desiredPerigee)
                {
                    //...switch Peri an Apo
                    float temp = desiredPerigee;
                    desiredPerigee = desiredApogee;
                    desiredApogee = temp;
                }

                //Build the desired final orbit into a string to show in flight
                desiredOrbit = (int)desiredPerigee + "x" + (int)desiredApogee + "@" + desiredInclination + "°";

                //Set the visible PAW variable 
                if (modInUse)
                    PAWmodInUse = PAWTextEnabled;
                else
                    PAWmodInUse = PAWTextDisabled;
            }

            //TESTSECTION
            //testyTime = (int)Planetarium.GetUniversalTime();

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
                        //Set the launch time
                        launchTime = Planetarium.GetUniversalTime();
                        //Start the Ascent
                        guidingAscent = true;

                        //Creating an orbitalFrame, saving the initial state...just like a gimbal
                        orbitalPrograde = vessel.obt_velocity.normalized;
                        orbitalRadial = (vessel.CoMD - vessel.mainBody.position).normalized;
                        //Not used right now, but to have it complete:
                        orbitalFrame = UnityEngine.QuaternionD.LookRotation(orbitalPrograde, orbitalRadial);
                        orbitalNormal = orbitalFrame * Vector3d.left;
                    }
                }
                //Check if the ascent started... if the Vessel is launched
                else if (guidingAscent)
                {
                    /*
                    //TESTSECTION
                    //ScreenMessages.PostScreenMessage("(int)Planetarium.GetUniversalTime() - testyTime % testyCount == 0 ?", 3.0f, ScreenMessageStyle.KERBAL_EVA);
                    //ScreenMessages.PostScreenMessage((int)Planetarium.GetUniversalTime() + " - " + testyTime + " % " + testyCount + " = " + ((int)Planetarium.GetUniversalTime() - testyTime) % testyCount, 3.0f, ScreenMessageStyle.LOWER_CENTER);
                                        
                    guidanceState = 99;
                    if (((((int)Planetarium.GetUniversalTime() - testyTime) % testyCount) == 0) && guidanceState == 99)
                    {
                        testyTime = (int)Planetarium.GetUniversalTime();

                        //Ship's pointing that way
                        Vector3d shipForward = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.forward).normalized;
                        Vector3d shipLeft = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.left).normalized;
                        Vector3d shipUp = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.up).normalized;
                        //Ship's flying that way (airflow)
                        Vector3d flightDirectionAirflow = vessel.srf_velocity.normalized;
                        //Ship's flying that way (orbital)
                        Vector3d flightDirectionOrbital = vessel.obt_velocity.normalized;
                        
                        ScreenMessages.PostScreenMessage("angle airflow-forward: " + ((int)HelperFunctions.degAngle(shipForward, flightDirectionAirflow)), 5.0f, ScreenMessageStyle.UPPER_LEFT);
                        ScreenMessages.PostScreenMessage("yaw: " + (Math.Round((-1 * (90 - HelperFunctions.degAngle(shipLeft, flightDirectionAirflow)) / 90), 3)), 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        ScreenMessages.PostScreenMessage("pitch: " + (Math.Round(( +1 * (90 - HelperFunctions.degAngle(shipUp, flightDirectionAirflow)) / 90), 3)), 5.0f, ScreenMessageStyle.UPPER_RIGHT);
                        
                        ScreenMessages.PostScreenMessage("angle orbital-forward: " + (int)radToDeg(Math.Acos(shipForward.x * flightDirectionOrbital.x + shipForward.y * flightDirectionOrbital.y + shipForward.z * flightDirectionOrbital.z)), 5.0f, ScreenMessageStyle.UPPER_LEFT);
                        ScreenMessages.PostScreenMessage("angle orbital-left: " + (int)radToDeg(Math.Acos(shipLeft.x * flightDirectionOrbital.x + shipLeft.y * flightDirectionOrbital.y + shipLeft.z * flightDirectionOrbital.z)), 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        ScreenMessages.PostScreenMessage("angle orbital-up: " + (int)radToDeg(Math.Acos(shipUp.x * flightDirectionOrbital.x + shipUp.y * flightDirectionOrbital.y + shipUp.z * flightDirectionOrbital.z)), 5.0f, ScreenMessageStyle.UPPER_RIGHT);
                        
                        ScreenMessages.PostScreenMessage("angle airflow: " + Vector3d.Angle(shipForward, flightDirectionAirflow), 0.1f, ScreenMessageStyle.UPPER_LEFT);
                        ScreenMessages.PostScreenMessage("angle orbital: " + Vector3d.Angle(shipForward, flightDirectionOrbital), 0.1f, ScreenMessageStyle.UPPER_RIGHT);
                        
                    }*/

                    //enums to keep track of the stage we're in during the ascent guidance
                    //0 = initial calculations
                    //1 = initiate Gravity Turn
                    //3 = correcting inclination
                    //4 = hitting Perigee
                    //5 = hitting Apogee

                    //initial calculations
                    if (guidanceState == 0)
                    {

                        if (!steeringLocked)
                        {
                            vessel.OnFlyByWire += lazyFlight;
                            steeringLocked = true;
                        }


                        //acurate value* (in stock) 0.3 seconds after lift-off
                        //acurate value* (in RO) 0.5 seconds after lift-off
                        //*vessel.specificAcceleration seems to take "some time (stated in the API)" to calculate the correct numbers
                        //let's grab it some short time after 0.49 seconds after lift-off
                        if (!initialCalculationsDone && launchTime + 0.49f <= Planetarium.GetUniversalTime())
                        {
                            //Only do this once
                            initialCalculationsDone = true;

                            //Smaller inclination then our launch site's latitude is not possible.
                            //Choose the smallest possible, which is the latitude
                            if (desiredInclination < vessel.latitude)
                                azimuth = vessel.latitude;
                            else
                                azimuth = HelperFunctions.radToDeg(Math.Acos((Math.Cos(HelperFunctions.degToRad(desiredInclination)) / Math.Cos(HelperFunctions.degToRad(vessel.latitude)))));

                            //Does the user want messages?
                            if (eventMessaging)
                            {
                                ScreenMessages.PostScreenMessage("Auto Ascent[" + guidanceState + "] : Calculated Azimuth = " + azimuth, 3.0f, ScreenMessageStyle.UPPER_RIGHT);
                            }

                            //Rotate the ships coordinates, so we match the desired launch direction and get the correct pitch and yaw settings
                            if (desiredDirectionToLaunch == "SE")
                            {
                                Vector3d shipLeft = vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, -(float)azimuth, 0) * Vector3d.left;
                                Vector3d shipUp = vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, -(float)azimuth, 0) * Vector3d.up;
                                headingToAzimuth.x = 1 * HelperFunctions.scalarProduct(orbitalPrograde, shipUp);
                                headingToAzimuth.y = -1 * HelperFunctions.scalarProduct(orbitalPrograde, shipLeft);
                            }
                            else if (desiredDirectionToLaunch == "NE")
                            {
                                Vector3d shipLeft = vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, (float)azimuth, 0) * Vector3d.left;
                                Vector3d shipUp = vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, (float)azimuth, 0) * Vector3d.up;
                                headingToAzimuth.x = 1 * HelperFunctions.scalarProduct(orbitalPrograde, shipUp);
                                headingToAzimuth.y = -1 * HelperFunctions.scalarProduct(orbitalPrograde, shipLeft);
                            }
                            else if (desiredDirectionToLaunch == "NW")
                            {
                                Vector3d shipLeft = vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, -(float)azimuth, 0) * Vector3d.left;
                                Vector3d shipUp = vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, -(float)azimuth, 0) * Vector3d.up;
                                headingToAzimuth.x = -1 * HelperFunctions.scalarProduct(orbitalPrograde, shipUp);
                                headingToAzimuth.y = 1 * HelperFunctions.scalarProduct(orbitalPrograde, shipLeft);
                            }
                            else if (desiredDirectionToLaunch == "SW")
                            {
                                Vector3d shipLeft = vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, (float)azimuth, 0) * Vector3d.left;
                                Vector3d shipUp = vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, (float)azimuth, 0) * Vector3d.up;
                                headingToAzimuth.x = -1 * HelperFunctions.scalarProduct(orbitalPrograde, shipUp);
                                headingToAzimuth.y = 1 * HelperFunctions.scalarProduct(orbitalPrograde, shipLeft);
                            }

                            ScreenMessages.PostScreenMessage("headingToAzimuth.x: " + headingToAzimuth.x, 3.0f, ScreenMessageStyle.UPPER_LEFT);
                            ScreenMessages.PostScreenMessage("headingToAzimuth.y: " + headingToAzimuth.y, 3.0f, ScreenMessageStyle.UPPER_CENTER);

                            //some calculations
                            initialTWR = vessel.specificAcceleration / 9.80665f;
                            pitchAngleDesired = (5 * initialTWR - 3);

                            //Does the user want messages?
                            if (eventMessaging)
                            {
                                ScreenMessages.PostScreenMessage("Auto Ascent[" + guidanceState + "] : Calculated initial TWR = " + initialTWR + " -> initial angle : " + pitchAngleDesired, 3.0f, ScreenMessageStyle.UPPER_RIGHT);
                            }
                        }

                        //ready to initiate Gravity Turn at 50m/s
                        if (vessel.verticalSpeed >= 50)
                        {
                            //next step in guidance
                            guidanceState += 1;
                            vessel.OnFlyByWire -= lazyFlight;
                            steeringLocked = false;
                        }
                    }
                    //initite Gravity Turn
                    else if (guidanceState == 1)
                    {
                        //Does the user want messages?
                        if (eventMessaging)
                        {
                            ScreenMessages.PostScreenMessage("Auto Ascent[" + guidanceState + "] : initiate Gravity Turn", 3.0f, ScreenMessageStyle.UPPER_RIGHT);
                        }

                        //Calculating the angle between the rocket-pointing-direction and the initially pitching angle = Acos(scalar(orbitRadial*shipForward) / magnitude(orbitRRadial) * magnitude(shipForward))
                        //magnitudes are 1, 'cause vectors given are normalized
                        //Vector3d shipForward = vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.forward;
                        //pitchAngleActual = HelperFunctions.degAngle(orbitalRadial, shipForward);

                        if (!steeringLocked)
                        {
                            vessel.OnFlyByWire += initiateGravityTurn;
                            steeringLocked = true;
                        }

                        //We reached our targeted initial pitch angle
                        if (pitchAngleDesired <= pitchAngleActual)
                        {
                            //next step in guidance
                            guidanceState += 1;
                            vessel.OnFlyByWire -= initiateGravityTurn;
                            steeringLocked = false;
                        }
                    }
                    //roll the horizon to the bottom, so that it's easier to modify the inclination (yaw) and the (time to) apsis (pitch) 
                    else if (guidanceState == 2)
                    {
                        //Does the user want messages?
                        if (eventMessaging)
                        {
                            ScreenMessages.PostScreenMessage("Auto Ascent[" + guidanceState + "] : Roll Maneuver", 3.0f, ScreenMessageStyle.UPPER_RIGHT);
                        }

                        if (!steeringLocked)
                        {
                            vessel.OnFlyByWire += rollManeuver;
                            steeringLocked = true;
                        }

                        //we're past MaxQ
                        if (vessel.altitude >= 15000)
                        {
                            //next step in guidance
                            guidanceState += 1;
                            vessel.OnFlyByWire -= rollManeuver;
                            steeringLocked = false;
                        }

                        /*
                        //Ship's pointing that way
                        Vector3d shipForward = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.forward).normalized;
                        Vector3d shipLeft = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.left).normalized;
                        Vector3d shipUp = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.up).normalized;
                        //Ship's flying that way (airflow)
                        Vector3d flightDirectionAirflow = vessel.srf_velocity.normalized;
                        //Ship's flying that way (orbital)
                        Vector3d flightDirectionOrbital = vessel.obt_velocity.normalized;

                        //ScreenMessages.PostScreenMessage("angle airflow-forward: " + ((int)HelperFunctions.degAngle(shipForward, flightDirectionAirflow)), 0.1f, ScreenMessageStyle.UPPER_LEFT);
                        //ScreenMessages.PostScreenMessage("yaw: " + (Math.Round((-1 * (90 - HelperFunctions.degAngle(shipLeft, flightDirectionAirflow)) / 90), 3)), 0.1f, ScreenMessageStyle.UPPER_CENTER);
                        //ScreenMessages.PostScreenMessage("pitch: " + (Math.Round((+1 * (90 - HelperFunctions.degAngle(shipUp, flightDirectionAirflow)) / 90), 3)), 0.1f, ScreenMessageStyle.UPPER_RIGHT);
                        
                        ScreenMessages.PostScreenMessage("left.pro. : " + ((int)HelperFunctions.degAngle(shipLeft, orbitalPrograde)), 0.1f, ScreenMessageStyle.UPPER_LEFT);
                        ScreenMessages.PostScreenMessage("left.rad. : " + ((int)HelperFunctions.degAngle(shipLeft, orbitalRadial)), 0.1f, ScreenMessageStyle.UPPER_CENTER);
                        ScreenMessages.PostScreenMessage("left.norm.: " + ((int)HelperFunctions.degAngle(shipLeft, orbitalNormal)), 0.1f, ScreenMessageStyle.UPPER_RIGHT);

                        //ScreenMessages.PostScreenMessage("angle orbital-forward: " + (int)HelperFunctions.radToDeg(Math.Acos(shipForward.x * flightDirectionOrbital.x + shipForward.y * flightDirectionOrbital.y + shipForward.z * flightDirectionOrbital.z)), 0.1f, ScreenMessageStyle.UPPER_LEFT);
                        //ScreenMessages.PostScreenMessage("angle orbital-left: " + (int)HelperFunctions.radToDeg(Math.Acos(shipLeft.x * flightDirectionOrbital.x + shipLeft.y * flightDirectionOrbital.y + shipLeft.z * flightDirectionOrbital.z)), 0.1f, ScreenMessageStyle.UPPER_CENTER);
                        //ScreenMessages.PostScreenMessage("angle orbital-up: " + (int)HelperFunctions.radToDeg(Math.Acos(shipUp.x * flightDirectionOrbital.x + shipUp.y * flightDirectionOrbital.y + shipUp.z * flightDirectionOrbital.z)), 0.1f, ScreenMessageStyle.UPPER_RIGHT);

                        //ScreenMessages.PostScreenMessage("angle airflow: " + Vector3d.Angle(shipForward, flightDirectionAirflow), 0.1f, ScreenMessageStyle.UPPER_LEFT);
                        //ScreenMessages.PostScreenMessage("angle orbital: " + Vector3d.Angle(shipForward, flightDirectionOrbital), 0.1f, ScreenMessageStyle.UPPER_RIGHT);
                        */


                    }
                    //
                    //enforcing the gravity turn
                    else if (guidanceState == 3)
                    {
                        //Does the user want messages?
                        if (eventMessaging)
                        {
                            ScreenMessages.PostScreenMessage("Auto Ascent[" + guidanceState + "] : enforcing Gravity Turn by " + gravityTurnAngle + "°", 3.0f, ScreenMessageStyle.UPPER_RIGHT);
                        }

                        if (!steeringLocked)
                        {
                            vessel.OnFlyByWire += enforcingGravityTurn;
                            steeringLocked = true;
                        }

                        //we're at 1% atmospheric density
                        if (vessel.altitude >= 33000)
                        {
                            //next step in guidance
                            guidanceState += 1;
                            vessel.OnFlyByWire -= enforcingGravityTurn;
                            steeringLocked = false;
                        }
                    }
                    //
                    //correct the inclination
                    else if (guidanceState == 4)
                    {
                        //Does the user want messages?
                        if (eventMessaging)
                        {
                            ScreenMessages.PostScreenMessage("Auto Ascent[" + guidanceState + "] : turning towards orbital prograde", 3.0f, ScreenMessageStyle.UPPER_RIGHT);
                        }

                        if (!steeringLocked)
                        {
                            vessel.OnFlyByWire += turningTowardsOrbitalPrograde;
                            steeringLocked = true;
                        }

                        //We reached our target pitch angle
                        if (vessel.orbit.ApA >= (desiredPerigee * 1000))
                        {
                            //next step in guidance
                            guidanceState += 1;
                            vessel.OnFlyByWire -= turningTowardsOrbitalPrograde;
                            steeringLocked = false;
                        }
                    }
                    //ScreenMessages.PostScreenMessage("Apo? " + vessel.orbit.ApA, 0.1f, ScreenMessageStyle.UPPER_CENTER);
                    //ScreenMessages.PostScreenMessage("Per? " + vessel.orbit.PeA, 0.1f, ScreenMessageStyle.LOWER_CENTER);
                    //find out what way we are pointing, get the angle to what we should be pointing (azimuth)
                    //calculate and add corrections to the steering
                    else if (guidanceState == 5)//&& vessel.altitude >= 33000)
                    {
                        //Ship's pointing that way
                        Vector3d shipLeft = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.left).normalized;
                        Vector3d shipUp = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.up).normalized;
                        Vector3d shipForward = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.forward).normalized;

                        //Ship's flying that way (airflow)
                        Vector3d flightDirectionAirflow = vessel.srf_velocity.normalized;
                        //Ship's flying that way (orbital)
                        Vector3d flightDirectionOrbital = vessel.obt_velocity.normalized;


                        //ScreenMessages.PostScreenMessage("forw.air. : " + ((int)HelperFunctions.degAngle(shipForward, flightDirectionAirflow)), 0.1f, ScreenMessageStyle.UPPER_LEFT);
                        //ScreenMessages.PostScreenMessage("left.air. : " + ((int)HelperFunctions.degAngle(shipLeft, flightDirectionAirflow)), 0.1f, ScreenMessageStyle.UPPER_CENTER);
                        //ScreenMessages.PostScreenMessage("up.air.: " + ((int)HelperFunctions.degAngle(shipUp, flightDirectionAirflow)), 0.1f, ScreenMessageStyle.UPPER_RIGHT);
                        //ScreenMessages.PostScreenMessage("pitch.: " + -1 * (float)((pitchAngle - 90d + gravityTurnAngle) / 30d), 0.1f, ScreenMessageStyle.UPPER_CENTER);

                        ScreenMessages.PostScreenMessage("forw.orb. : " + ((int)HelperFunctions.degAngle(shipForward, flightDirectionOrbital)), 0.1f, ScreenMessageStyle.UPPER_LEFT);
                        ScreenMessages.PostScreenMessage("left.orb. : " + ((int)HelperFunctions.degAngle(shipLeft, flightDirectionOrbital)), 0.1f, ScreenMessageStyle.UPPER_CENTER);
                        ScreenMessages.PostScreenMessage("up.orb.: " + ((int)HelperFunctions.degAngle(shipUp, flightDirectionOrbital)), 0.1f, ScreenMessageStyle.UPPER_RIGHT);
                        //Does the user want messages?
                        if (eventMessaging)
                        {
                            ScreenMessages.PostScreenMessage("Auto Ascent[" + guidanceState + "] : GIVING CONTROL BACK TEST", 3.0f, ScreenMessageStyle.UPPER_RIGHT);
                        }

                    }/*
                    //Try to keep the perigee 30 seconds away and finetune the inclination
                    else if (guidanceState == 5)//&& vessel.altitude >= 33000)
                    {
                        //Does the user want messages?
                        if (eventMessaging)
                        {
                            ScreenMessages.PostScreenMessage("Auto Ascent[" + guidanceState + "] : Aiming for Perigee & finetuning inclination", 3.0f, ScreenMessageStyle.UPPER_RIGHT);
                        }

                        //Ship's pointing that way
                        Vector3d shipForward = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.forward).normalized;
                        Vector3d shipLeft = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.left).normalized;
                        Vector3d shipUp = (vessel.GetTransform().rotation * UnityEngine.Quaternion.Euler(-90, 0, 0) * Vector3d.up).normalized;
                        //Ship's flying that way (orbital)
                        Vector3d flightDirectionOrbital = vessel.obt_velocity.normalized;


                        //direct steering command
                        void continuedPitchYawSteering(FlightCtrlState s)
                        {
                            s.pitch = (float)(Math.Round((+1 * (90 - HelperFunctions.degAngle(shipUp, flightDirectionOrbital)) / 90), 3));
                            s.yaw = (float)(Math.Round((-1 * (90 - HelperFunctions.degAngle(shipLeft, flightDirectionOrbital)) / 90), 3));
                            s.roll = 0;

                            //ScreenMessages.PostScreenMessage("angle orbital-forward: " + ((int)HelperFunctions.degAngle(shipForward, flightDirectionOrbital)), 5.0f, ScreenMessageStyle.UPPER_LEFT);
                            //ScreenMessages.PostScreenMessage("yaw: " + s.yaw, 0.1f, ScreenMessageStyle.UPPER_CENTER);
                            //ScreenMessages.PostScreenMessage("pitch: " + s.pitch, 0.1f, ScreenMessageStyle.UPPER_RIGHT);
                        }
                        vessel.OnFlyByWire += continuedPitchYawSteering;

                        if (vessel.altitude >= 50000)
                            //next step in guidance
                            guidanceState += 1;
                    }*/
                }
            }
        }

        #endregion
    }

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

        //Text, if functionality is disabled/enabled
        [KSPField(isPersistant = true, guiActive = false)]
        private const string PAWTextDisabled = "disconnected";

        [KSPField(isPersistant = true, guiActive = false)]
        private const string PAWTextEnabled = "connected";

        //Text, if event messaging is disabled/enabled
        [KSPField(isPersistant = true, guiActive = false)]
        private const string MessagingDisabled = "inactive";

        [KSPField(isPersistant = true, guiActive = false)]
        private const string MessagingEnabled = "active";

        //A button to enable or disable the function
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Circuits are:", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName),
            UI_Toggle(disabledText = PAWTextDisabled, enabledText = PAWTextEnabled)]
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

        //Shows if the decoupler is active
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Circuits are", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName)]
        private string PAWmodInUse;

        //Shows the time until the decoupler is activated in seconds, one decimal
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Seconds until Decouple", guiFormat = "F1", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName)]
        private double timeToDecouple = 0;

        //A button to enable or disable if a message for this event will be shown
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Event Messaging:", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName),
            UI_Toggle(disabledText = MessagingDisabled, enabledText = MessagingEnabled)]
        private bool eventMessaging = true;

        //A button to enable or disable if a message for this event will be shown
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Decoupling", groupName = PAWDecouplerGroupName, groupDisplayName = PAWDecouplerGroupName),
            UI_ChooseOption(options = new string[5] { "1st stage", "2nd stage", "3rd stage", "Booster", "Payload" })]
        private string eventMessage = "1st stage";

        //A small variable to manage the onScreen Messages
        private char nextMessageStep = (char)0;

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
                if (modInUse)
                    PAWmodInUse = PAWTextEnabled;
                else
                    PAWmodInUse = PAWTextDisabled;
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

                    //Does the user want messages?
                    if (eventMessaging)
                    {
                        //Time to announce the upcoming decouple event
                        if (nextMessageStep == 0 && timeToDecouple <= 10)
                        {
                            ScreenMessages.PostScreenMessage("Decoupling " + eventMessage + " in 10 seconds.", 4.5f, ScreenMessageStyle.UPPER_CENTER);
                            nextMessageStep++;
                        }
                        else if (nextMessageStep == 1 && timeToDecouple <= 5)
                        {
                            ScreenMessages.PostScreenMessage("Decoupling " + eventMessage + " in  5 seconds.", 2.5f, ScreenMessageStyle.UPPER_CENTER);
                            nextMessageStep++;
                        }
                        else if (nextMessageStep == 2 && timeToDecouple <= 2)
                        {
                            ScreenMessages.PostScreenMessage("Decoupling " + eventMessage + " in  2 seconds.", 1.5f, ScreenMessageStyle.UPPER_CENTER);
                            nextMessageStep++;
                        }
                    }

                    //If it's time to decouple...
                    if (timeToDecouple <= 0)
                    {
                        //Does the user want messages?
                        if (eventMessaging)
                        {
                            //Showing the actual decouple message
                            ScreenMessages.PostScreenMessage("Decoupling " + eventMessage, 3f, ScreenMessageStyle.UPPER_CENTER);
                        }

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

        //Text, if functionality is disabled/enabled
        [KSPField(isPersistant = true, guiActive = false)]
        private const string PAWTextDisabled = "disconnected";

        [KSPField(isPersistant = true, guiActive = false)]
        private const string PAWTextEnabled = "connected";

        //Text, if event messaging is disabled/enabled
        [KSPField(isPersistant = true, guiActive = false)]
        private const string MessagingDisabled = "inactive";

        [KSPField(isPersistant = true, guiActive = false)]
        private const string MessagingEnabled = "active";

        //A button to enable or disable the function
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Circuits are:", groupName = PAWIgnitorGroupName, groupDisplayName = PAWIgnitorGroupName),
            UI_Toggle(disabledText = PAWTextDisabled, enabledText = PAWTextEnabled)]
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
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Circuits are", groupName = PAWIgnitorGroupName, groupDisplayName = PAWIgnitorGroupName)]
        private string PAWmodInUse;

        //Shows the time until the engine is activated in seconds, one decimal
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Seconds until Ignition", guiFormat = "F1", groupName = PAWIgnitorGroupName, groupDisplayName = PAWIgnitorGroupName)]
        private double timeToIgnite = 0;

        //A button to enable or disable if a message for this event will be shown
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Event Messaging:", groupName = PAWIgnitorGroupName, groupDisplayName = PAWIgnitorGroupName),
            UI_Toggle(disabledText = MessagingDisabled, enabledText = MessagingEnabled)]
        private bool eventMessaging = true;

        //A button to enable or disable if a message for this event will be shown
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Igniting", groupName = PAWIgnitorGroupName, groupDisplayName = PAWIgnitorGroupName),
            UI_ChooseOption(options = new string[4] { "1st stage", "2nd stage", "3rd stage", "Booster" })]
        private string eventMessage = "1st stage";

        //A small variable to manage the onScreen Messages
        private char nextMessageStep = (char)0;

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
                if (modInUse)
                    PAWmodInUse = PAWTextEnabled;
                else
                    PAWmodInUse = PAWTextDisabled;
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

                    //Does the user want messages?
                    if (eventMessaging)
                    {
                        //Time to announce the upcoming ignition event
                        if (nextMessageStep == 0 && timeToIgnite <= 10)
                        {
                            ScreenMessages.PostScreenMessage("Igniting " + eventMessage + " in 10 seconds.", 4.5f, ScreenMessageStyle.UPPER_CENTER);
                            nextMessageStep++;
                        }
                        else if (nextMessageStep == 1 && timeToIgnite <= 5)
                        {
                            ScreenMessages.PostScreenMessage("Igniting " + eventMessage + " in  5 seconds.", 2.5f, ScreenMessageStyle.UPPER_CENTER);
                            nextMessageStep++;
                        }
                        else if (nextMessageStep == 2 && timeToIgnite <= 2)
                        {
                            ScreenMessages.PostScreenMessage("Igniting " + eventMessage + " in  2 seconds.", 1.5f, ScreenMessageStyle.UPPER_CENTER);
                            nextMessageStep++;
                        }
                    }

                    //If it's time to decouple...
                    if (timeToIgnite <= 0)
                    {
                        //Does the user want messages?
                        if (eventMessaging)
                        {
                            //Showing the actual ignition message
                            ScreenMessages.PostScreenMessage("Igniting " + eventMessage, 3f, ScreenMessageStyle.UPPER_LEFT);
                        }

                        //Stop the countdown
                        countingDown = false;
                        //...do it already
                        part.force_activate();
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

            //Text, if functionality is disabled/enabled
            [KSPField(isPersistant = true, guiActive = false)]
            private const string PAWTextDisabled = "disconnected";

            [KSPField(isPersistant = true, guiActive = false)]
            private const string PAWTextEnabled = "connected";

            //Text, if event messaging is disabled/enabled
            [KSPField(isPersistant = true, guiActive = false)]
            private const string MessagingDisabled = "inactive";

            [KSPField(isPersistant = true, guiActive = false)]
            private const string MessagingEnabled = "active";

            //A button to enable or disable the function
            [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Circuits are:", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName),
                UI_Toggle(disabledText = PAWTextDisabled, enabledText = PAWTextEnabled)]
            private bool modInUse = false;

            //Specify the Height in kilometers in the Editor
            [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Height [km]", guiFormat = "F0", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 140f, incrementLarge = 10f, incrementSmall = 1f, incrementSlide = 1f, sigFigs = 0)] //140km - that's where the atmosphere ends
            private float editorHeightToSeparate = 0;

            //Shows if the fairing is active
            [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Circuits are", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName)]
            private string PAWmodInUse;

            //Shows the Height in kilometers at which the fairing gets separated
            [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Height [km] to Separate", guiFormat = "F0", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName)]
            private float flightHeightToSeparate = 0;

            //A button to enable or disable if a message for this event will be shown
            [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Event Messaging:", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName),
                UI_Toggle(disabledText = MessagingDisabled, enabledText = MessagingEnabled)]
            private bool eventMessaging = true;

            //A button to enable or disable if a message for this event will be shown
            [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Jettison", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName),
                UI_ChooseOption(options = new string[3] { "Payload", "3rd stage", "2nd stage" })]
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
                        PAWmodInUse = PAWTextEnabled;
                    else
                        PAWmodInUse = PAWTextDisabled;
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
                            if (eventMessaging)
                            {
                                //Showing the jettison message
                                ScreenMessages.PostScreenMessage("Jettison " + eventMessage + " fairing.", 3f, ScreenMessageStyle.UPPER_CENTER);
                            }

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

            //Text, if functionality is disabled/enabled
            [KSPField(isPersistant = true, guiActive = false)]
            private const string PAWTextDisabled = "disconnected";

            [KSPField(isPersistant = true, guiActive = false)]
            private const string PAWTextEnabled = "connected";

            //Text, if event messaging is disabled/enabled
            [KSPField(isPersistant = true, guiActive = false)]
            private const string MessagingDisabled = "inactive";

            [KSPField(isPersistant = true, guiActive = false)]
            private const string MessagingEnabled = "active";

            //A button to enable or disable the function
            [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Circuits are:", groupName = PAWFairingGroupName, groupDisplayName = PAWFairingGroupName),
                UI_Toggle(disabledText = PAWTextDisabled, enabledText = PAWTextEnabled)]
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
                UI_Toggle(disabledText = MessagingDisabled, enabledText = MessagingEnabled)]
            private bool eventMessaging = true;

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
                        PAWmodInUse = PAWTextEnabled;
                    else
                        PAWmodInUse = PAWTextDisabled;
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
                            if (eventMessaging)
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
}