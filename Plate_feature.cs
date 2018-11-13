using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.Structure;
using System.Drawing;
using Emgu.CV.CvEnum;

namespace ANPR_System
{
    public class Plate_feature
    {
        public enum type_of_plate
        {
            RECT_PLATE,
            SQUARE_PLATE
        }

        /// <summary>
        /// Check if a contour has the features of a plate
        /// </summary>
        /// <param name="type_of_plate">0: rectangle plate, 1: square plate</param>
        /// <param name="area"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static bool ratio_check(type_of_plate type, double area, double width, double height)
        {
            double aspect = 0;
            int area_min = 0;
            int area_max = 0;
            double rmin = 0;
            double rmax = 0;

            if (type == type_of_plate.RECT_PLATE)
            {
                aspect = 4.272727;
                area_min = 3000;
                area_max = 60000;

                rmin = 2.5;
                rmax = 7;

            }
            else if (type == type_of_plate.SQUARE_PLATE)
            {
                aspect = 4.272727;
                area_min = 4000;
                area_max = 30000;

                rmin = 0.5;
                rmax = 1.5;
            }

            double ratio = width / height;

            if (ratio < 1)
            {
                ratio = 1 / ratio;

            }

            if ((area < area_min || area > area_max) || (ratio < rmin || ratio > rmax))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// check the if the detected contour satisfies the white pixels by black pixels condition
        /// </summary>
        /// <param name="plate"></param>
        /// <returns></returns>
        public static bool is_max_white(Image<Gray, byte> plate)
        {
            double avg = plate.GetAverage().Intensity;
            if (avg >= 40)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Check if a contour has the features of a plate
        /// </summary>
        /// <param name="type">0: rectangle plate, 1: square plate</param>
        /// <param name="feature">contains X, Y, Width, Height and angle of the plate</param>
        /// <returns></returns>
        public static bool validate_rotation_and_ratio(type_of_plate type, RotatedRect plate_feature)
        {
            Rectangle r = plate_feature.MinAreaRect();

            double W = r.Width;
            double H = r.Height;
            double angle = plate_feature.Angle;

            if (W > H)
            {
                angle = (-1) * angle;
            }
            else
            {
                angle = 90 + angle;
            }

            if (angle > 15)
            {
                return false;
            }

            if (W == 0 || H == 0)
            {
                return false;
            }

            double area = W * H;

            if (ratio_check(type, area, W, H))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //public static bool character_check(double plate_height, VectorOfPoint contour_of_character)
        //{
        //    double area = CvInvoke.ContourArea(contour_of_character);
        //    Rectangle r = CvInvoke.BoundingRectangle(contour_of_character);

        //    double w = r.Width;
        //    double h = r.Height;

        //    double aspect_ratio = w / h;
        //    double solidity = area / (w * h);
        //    double height_ratio = h / plate_height;

        //    if (aspect_ratio < 1.0 && solidity < 0.15 && height_ratio < 0.95 && height_ratio > 0.5)
        //    {
        //        return true;
        //    }

        //    else
        //    {
        //        return false;
        //    }
        //}


    }



}
