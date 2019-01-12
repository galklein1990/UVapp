using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace UVapp
{
    public class UVTime
    {
        private double UVMinutesLeft;
        public UVTime(SkinType skinType)
        {
            UVMinutesLeft = skinType.UVMinutesToBurn();
        }

        public void passTimeUnderUV(double minutes, long uviNum)
        {
            UVMinutesLeft -= minutes * uviNum;
        }

        public double getUVMinutesLeft()
        {
            return UVMinutesLeft;
        }
    }

    // The 6 Fitzpatrick skin types
    public enum SkinType
    {
        Fitz1 = 1,  // Palest
        Fitz2 = 2,
        Fitz3 = 3,
        Fitz4 = 4,
        Fitz5 = 5,
        Fitz6 = 6   // Darkest
    }

    public static class SkinTypeMethods
    {
        /* The methods in this class are extension methods to the SkinType enum
         * By specifying SkinType parameter as "this"
         * The methods becomes an extension method and you can perform
         * 
         * userSkinType = SkinType.Fitz1;
         * double uvMinutes = userSkinType.MinutesToBurn(uviNum);
         */

        /*
         * We measure UV doses in UV minutes. UV minutes = minutes * UVI.
         * So 10 minutes spent under UVI 1 are 10 UV minutes but 10 minutes spent under UVI 3 are 30 UV minutes.
         * 
         * Each skin type has a minimal dose of UV Minutes before it burns
         * The actual dose varies between a range
         * The ranges are given in https://www.ncbi.nlm.nih.gov/pmc/articles/PMC3709783/
         * And the values are converted to time using UVI unit = 25mW/m2 which is given in
         * https://web.archive.org/web/20100613192249/http://www.serc.si.edu/labs/photobiology/UVIndex_calculation.aspx
         * And more clearly in 
         * https://en.wikipedia.org/wiki/Ultraviolet_index
         */

        /**
         * Returns the minutes it takes the skin type to burn under uv index uviNum
         */
        public static double MinutesToBurn(this SkinType skinType, long uviNum)
        {
            return skinType.UVMinutesToBurn() / uviNum;
        }

        /** 
         * Returns the UV minutes to burn for each skin type
         * Chosen or averaged from within the skin type's range
         */
        public static double UVMinutesToBurn(this SkinType skinType)
        {
            // 25% into the range from the lower bound
            return (3 * skinType.LowBoundRangeUVMinutes() + skinType.UpBoundRangeUVMinutes()) / 4;
        }
        
        /**
         * Returns the lower bound of minimal dose to burn range
         */
        private static double LowBoundRangeUVMinutes(this SkinType skinType)
        {
            switch (skinType)
            {
                case SkinType.Fitz1:
                    return 100;
                case SkinType.Fitz2:
                    return 500.0 / 3;   // 166 and 2 thirds
                case SkinType.Fitz3:
                    return 200;
                case SkinType.Fitz4:
                    return 800.0 / 3;   // 266 and 2 thirds
                case SkinType.Fitz5:
                    return 400;
                case SkinType.Fitz6:
                    return 600;
                default:
                    return 0;
            }
        }

        /**
         * Returns the upper bound of minimal dose to burn range
         */
        private static double UpBoundRangeUVMinutes(this SkinType skinType)
        {
            switch (skinType)
            {
                case SkinType.Fitz1:
                    return 200;
                case SkinType.Fitz2:
                    return 800.0 / 3;   // 266 and 2 thirds
                case SkinType.Fitz3:
                    return 1000.0 / 3;
                case SkinType.Fitz4:
                    return 400;   
                case SkinType.Fitz5:
                    return 600;
                case SkinType.Fitz6:
                    return 1000;
                default:
                    return 0;
            }
        }
    }


}