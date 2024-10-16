﻿using System;

namespace BenjisHardwiredLogic
{
    class HelperFunctions
    {
        #region HelperFunctions

        public static double degToRad(double deg)
        {
            return ((Math.PI / 180d) * deg);
        }

        public static double radToDeg(double rad)
        {
            return ((180d / Math.PI) * rad);
        }

        public static double scalarProduct(Vector3d vector1, Vector3d vector2)
        {
            return (vector1.x * vector2.x + vector1.y * vector2.y + vector1.z * vector2.z);
        }

        public static double radAngle(Vector3d vector1, Vector3d vector2)
        {
            vector1 = vector1.normalized;
            vector2 = vector2.normalized;

            return (Math.Acos(scalarProduct(vector1, vector2)));
        }
        public static double degAngle(Vector3d vector1, Vector3d vector2)
        {
            vector1 = vector1.normalized;
            vector2 = vector2.normalized;

            return radToDeg(Math.Acos(scalarProduct(vector1, vector2)));
        }
        public static double limitAbs(double one, double two)
        {
            if (one > two)
                return two;
            if ((-1 * one) > two)
                return (-1 * two);
            else
                return one;
        }
        public static double limit(double one, double min, double max)
        {
            if (one < min)
                return min;
            else if (one > max)
                return max;
            else
                return one;
        }
        public static bool changeOfSign(double one, double two)
        {
            if ((one < 0 && two > 0) || (one > 0 && two < 0))
                return true;
            else
                return false;
        }
        public static bool changeOfSign(float one, float two)
        {
            if ((one < 0 && two > 0) || (one > 0 && two < 0))
                return true;
            else
                return false;
        }
        public static bool isInRangeOf(double one, double lowerLimit, double upperLimit)
        {
            if (one >= lowerLimit && one <= upperLimit)
                return true;
            else
                return false;
        }
        public static bool isInBetween(double one, double lowerLimit, double upperLimit)
        {
            if (one > lowerLimit && one < upperLimit)
                return true;
            else
                return false;
        }
        public static bool isInBetweenFactorOf(double one, double limit, double factor)
        {
            double fraction = limit * factor;
            if (one > (limit - factor) && one < (limit + factor))
                return true;
            else
                return false;
        }
        #endregion
    }
}
