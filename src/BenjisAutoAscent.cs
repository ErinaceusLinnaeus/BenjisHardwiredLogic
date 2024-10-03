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
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Ballistic Angle [°]", guiFormat = "F1", groupName = PAWAscentGroupName, groupDisplayName = PAWAscentGroupName),
            UI_FloatEdit(scene = UI_Scene.All, minValue = 40f, maxValue = 50.0f, incrementLarge = 5f, incrementSmall = 1f, incrementSlide = 0.1f, sigFigs = 1)]
        private float desiredBallisitcAngle = 45;

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

        #endregion
    }
}
