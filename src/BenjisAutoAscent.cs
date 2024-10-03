using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BenjisHardwiredLogic
{
    internal class BenjisAutoAscent : PartModule //ModuleSAS
    {

        #region Fields

        //Headline name for the GUI
        [KSPField(isPersistant = false, guiActive = false)]
        private const string PAWAscentGroupName = "Benji's Auto Ascent";

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
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Circuits are:", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_Toggle(disabledText = StringDisconnected, enabledText = StringConnected)]
        private bool modInUse = false;

        //Specify when the rocket should reach the desired ballistic angle in seconds in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Guided Flight [sec]", guiFormat = "F1", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 59.9f, incrementLarge = 10f, incrementSmall = 1f, incrementSlide = 0.1f, sigFigs = 1)]
        private float guidedFlightSeconds = 0;

        //Specify when the rocket should reach the desired ballistic angle in minutes in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "Guided Flight [min]", guiFormat = "F1", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 5f, incrementLarge = 5f, incrementSmall = 1f, incrementSlide = 1f, sigFigs = 1)]
        private float guidedFlightMinutes = 0;

        //Seconds and Minutes (*60) added
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Total Guided Flight", guiUnits = "sec", guiFormat = "F1", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName)]
        private float totalGuidedFlight = 0;

        //Specify the angle you wanna end up at
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "DeltaV [m/s]", guiFormat = "F1", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 8000.0f, incrementLarge = 1000f, incrementSmall = 100f, incrementSlide = 1f, sigFigs = 1)]
        private float deltaV = 5000;

        //A button to enable or disable the gimbal spin at the end of the guided flight
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Gimbal Spin:", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_Toggle(disabledText = StringInactive, enabledText = StringActive)]
        private bool gimbalSpin = true;

        //Specify how long before the gimbal spin should start in the Editor
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Gimbal Spin Pre Delay [sec]", guiFormat = "F1", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 0f, maxValue = 5.0f, incrementLarge = 1f, incrementSmall = 0.1f, incrementSlide = 0.1f, sigFigs = 1)]
        private float gimbalSpinPreSeconds = 2.5f;

        //The PAW fields in Flight
        //Shows if the decoupler is active
        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "Circuits are", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName)]
        private string PAWmodInUse = "inactive";

        //Shown in the Editor and in Flight
        //A button to enable or disable if a message for this event will be shown
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Estimated Downrange", guiUnits = "km", guiFormat = "F1", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName)]
        private double PAWestimatedDownrange = 0;

        #endregion


        /*
         * 
        Schleife schreiben, um die weiteste downrange zu finden.
        verschiedene Winkel ausprobieren (25 - 45°)



        double calculateRange(double dV, double angle)
        {
            const double g = 9.81; // Acceleration due to gravity in m/s²
            double radians = angle * (M_PI / 180.0); // Convert angle to radians
            double range = (pow(dV, 2) * sin(2 * radians)) / g; // Calculate range
            return range;
        }




        Flight path angle (FPA) is the direct variable that determines how far you go, what apogee you reach is an effect of that.

        The equation for calculating the Minimum Energy launch angle is
        FPA = 14.325*(π - (Downrange Distance / Radius of Earth))

        This results in the following values for the downrange contracts:
        3000 km = 38.26°
        5000 km = 33.76°
        7500 km = 28.14°

        Flight Path Angle refers to the angle of your velocity vector to the local horizontal.
        It is not necessarily the number you plug into MechJeb, it is the angle you want your velocity vector to be at when your final stage burns out.
        Depending on your design, your pitch angle may be slightly higher than this. You want to mess with your MechJeb ascent settings to reach this value.
        I personally start with MJ settings of 30° and 30% and adjust from there.



        */
    }
}
