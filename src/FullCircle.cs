using JetBrains.Annotations;
using System;
using System.Collections;
using System.Dynamic;
using System.Threading.Tasks;
using UnityEngine;

namespace BenjisHardwiredLogic
{
    public class FullCircle
    {
        [KSPField(isPersistant = true, guiActive = false)]
        static double degAzimuth;
        [KSPField(isPersistant = true, guiActive = false)]
        static double degAzimuth_KSPNavBall;
        /*
        [KSPField(isPersistant = true, guiActive = false)]
        static double degAzimuthNorth;
        [KSPField(isPersistant = true, guiActive = false)]
        static double degAzimuthEast;
        [KSPField(isPersistant = true, guiActive = false)]
        static int segmentAzimuth;
        */

        static int InWhatSegment(double east, double north)
        {
            // SEGMENTS   5
            //            W
            //        ---------
            //       /    |    \
            //      /     |     \
            //     /      |      \
            //    /    1  |   4   \
            //   |        |        |
            //6 N|_________________|S 8
            //   |        |        |
            //   |        |        |
            //    \    2  |   3   /
            //     \      |      /
            //      \     |     /
            //       \    |    /
            //        ---------
            //            E
            //            7

            if (east > 90) // Segement 1 or 4
            {
                if (north > 90) // Segment 4
                {
                    return 4;
                }
                else if (north < 90) // Segment 1
                {
                    return 1;
                }
                else // Segment 5 or 7
                {
                    if (east == 0) // Segment 7
                    {
                        return 7;
                    }
                    else if (east == 180) // Segment 5
                    {
                        return 5;
                    }
                    else
                        return 0;
                }
            }
            else if (east < 90)// Segment 2 or 3
            {
                if (north > 90) // Segment 3
                {
                    return 3;
                }
                else // Segment 2
                {
                    return 2;
                }
            }
            else // Segment 6 or 8
            {
                if (north == 0) // Segment 6
                {
                    return 6;
                }
                else if (north == 180) // Segment 8
                {
                    return 8;
                }
                else
                    return 0;
            }
        }

        public static void SetAzimuth(double latitude, string direction, double inclination)
        {
            //Calculate the "heading" the vessel needs to roll into, to end up at the desired inclination
            //Lower inclinations than the launch-site's are not possible 
            if (inclination <= Math.Abs(latitude))
                degAzimuth = Math.Abs(latitude);
            else
                degAzimuth = HelperFunctions.radToDeg(Math.Acos((Math.Cos(HelperFunctions.degToRad(inclination)) / Math.Cos(HelperFunctions.degToRad(Math.Abs(latitude))))));

            //LaunchSite on the northern hemisphere
            if (latitude >= 0)
            {
                //Prograde ==> launching to SE
                if (direction == "Prograde")
                {
                    //degAzimuthEast = degAzimuth;
                    //degAzimuthNorth = 90 + degAzimuth;
                    //Azimuth in KSP's weird reference...0°: N, 90°: E, 180°:S, 270°:W
                    degAzimuth_KSPNavBall = 90 + degAzimuth;
                }
                //Retrograde ==> launching to SW
                else if (direction == "Retrograde")
                {
                    //degAzimuthEast = 180 - degAzimuth;
                    //degAzimuthNorth = 90 + degAzimuth;
                    //Azimuth in KSP's weird reference...0°: N, 90°: E, 180°:S, 270°:W
                    degAzimuth_KSPNavBall = 270 - degAzimuth;
                }
            }
            //LaunchSite on the southern hemisphere
            else
            {
                //Prograde ==> launching to NE
                if (direction == "Prograde")
                {
                    //degAzimuthEast = degAzimuth;
                    //degAzimuthNorth = 90 - degAzimuth;
                    //Azimuth in KSP's weird reference...0°: N, 90°: E, 180°:S, 270°:W
                    degAzimuth_KSPNavBall = 90 - degAzimuth;
                }
                //Retrograde ==> launching to NW
                else if (direction == "Retrograde")
                {
                    //degAzimuthEast = 180 - degAzimuth;
                    //degAzimuthNorth = 90 - degAzimuth;
                    //Azimuth in KSP's weird reference...0°: N, 90°: E, 180°:S, 270°:W
                    degAzimuth_KSPNavBall = 270 + degAzimuth;
                }
            }
        }

        public static double GetAzimuth()
        {
            return degAzimuth;
        }
        public static double GetAzimuth_KSPNavBall()
        {
            return degAzimuth_KSPNavBall;
        }
/*
        public static double GetAzimuthEast()
        {
            return degAzimuthEast;
        }

        public static double GetAzimuthNorth()
        {
            return degAzimuthNorth;
        }
*/

        public static double GetShip_KSPNavBall(double shipEast, double shipNorth)
        {

            int shipSegment = InWhatSegment(shipEast, shipNorth);
            double shipKSPNavBall = 0;

            if (shipSegment == 1)
            {
                shipKSPNavBall = 360 - shipNorth;
            }
            else if (shipSegment == 2)
            {
                shipKSPNavBall = shipNorth;
            }
            else if (shipSegment == 3 || shipSegment == 4)
            {
                shipKSPNavBall = 90 + shipEast;
            }
            else if (shipSegment == 5)
            {
                shipKSPNavBall = 270;
            }
            else if (shipSegment == 6)
            {
                shipKSPNavBall = 0;
            }
            else if (shipSegment == 7)
            {
                shipKSPNavBall = 90;
            }
            else if (shipSegment == 8)
            {
                shipKSPNavBall = 180;
            }

            return shipKSPNavBall;
        }

        public static double GetDegBetweenAzimuthAndShip(double shipEast, double shipNorth)
        {
            //int shipSegment = InWhatSegment(shipEast, shipNorth);
            double shipKSPNavBall = GetShip_KSPNavBall(shipEast, shipNorth);

            double return_value = degAzimuth_KSPNavBall - shipKSPNavBall;

            if (return_value > 180)
            {
                return_value -= 360;
            }
            else if (return_value < -180)
            {
                return_value += 360;
            }

            return return_value;
        }
    }
}
