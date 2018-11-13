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
    /// <summary>
    /// This class does the segmentation to a plate image
    /// in order to get single characters in the plate
    /// </summary>
    public class Plate_segmentation
    {

        Image<Gray, byte> plate_after_preprocessing;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="preprocessing_plate">gray plate image after preprocessing (adaptive_threshold_img)</param>
        public Plate_segmentation(Image<Gray,byte> preprocessing_plate)
        {
            plate_after_preprocessing = preprocessing_plate;
        }

        /// <summary>
        /// check whether a contour is a character or not
        /// </summary>
        /// <param name="plate_after_preprocessing"></param>
        /// <param name="suspected_contour">just a single contour</param>
        /// <returns>true: if yes, false: if not</returns>
        private static bool is_contour_a_character(Image<Gray, byte> plate_after_preprocessing, VectorOfPoint suspected_contour)
        {
            Rectangle r = CvInvoke.BoundingRectangle(suspected_contour);

            double c_W = r.Width;
            double c_H = r.Height;

            double i_W = plate_after_preprocessing.Width;
            double i_H = plate_after_preprocessing.Height;

            // ratio_area_contour_over_img
            double ratio_1 = (i_W * i_H)/(c_W * c_H);
            // ratio_contour_over_img
            double ratio_2 = c_H /i_H;

            if ((ratio_1 >= 10 && ratio_1 < 43) && (ratio_2 >= 0.4) && ((c_H/c_W) > 1.2))
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// get all convex hull that satisfy is_contour_a_character condition.
        /// </summary>
        /// <param name="plate_after_preprocessing"></param>
        /// <param name="contours"></param>
        /// <returns></returns>
        private static List<VectorOfPoint> get_convex_hull(Image<Gray, byte> plate_after_preprocessing, VectorOfVectorOfPoint contours)
        {
            List<VectorOfPoint> hulls = new List<VectorOfPoint>();
            for (int i = 0; i < contours.Size; i++)
            {
                if (is_contour_a_character(plate_after_preprocessing, contours[i]))
                {
                    VectorOfPoint hull = new VectorOfPoint();
                    CvInvoke.ConvexHull(contours[i], hull);
                    hulls.Add(hull);
                    //CvInvoke.FillConvexPoly(hull_img, hull, new MCvScalar(255));
                }
            }
            return hulls;
        }

        /// <summary>
        /// re-order the hulls by x-axis. Then crop characters from the original image in that order.
        /// </summary>
        /// <param name="plate_after_preprocessing"></param>
        /// <param name="contours"></param>
        /// <returns></returns>
        private static List<Image<Gray, byte>> ordered_character(Image<Gray, byte> plate_after_preprocessing, List<VectorOfPoint> contours)
        {
            List<Image<Gray, byte>> order_character_image = new List<Image<Gray, byte>>();
            Dictionary<Rectangle, VectorOfPoint> dict_to_order = new Dictionary<Rectangle, VectorOfPoint>();

            for (int i = 0; i < contours.Count; i++)
            {
                Rectangle r = CvInvoke.BoundingRectangle(contours[i]);
                dict_to_order.Add(r, contours[i]);
            }

            var ordered_result = dict_to_order.OrderByDescending(i => i.Key.X);

            List<VectorOfPoint> ordered_contours = new List<VectorOfPoint>();

            foreach (KeyValuePair<Rectangle, VectorOfPoint> kvp in ordered_result)
            {
                ordered_contours.Add(kvp.Value);

                plate_after_preprocessing.ROI = kvp.Key;
                Image<Gray, byte> roi = new Image<Gray,byte>(kvp.Key.Width,kvp.Key.Height);
                plate_after_preprocessing.CopyTo(roi);
                order_character_image.Add(roi);
                plate_after_preprocessing.ROI = Rectangle.Empty;
            }


            return order_character_image;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="plate">plate cropped from original image</param>
        /// <returns>List of images of characters</returns>
        public static List<Image<Gray, byte>> segment_characters_from_plate(Image<Gray, byte> plate)
        {
            List<Image<Gray, byte>> segmented_characters = new List<Image<Gray, byte>>();
            //Image<Gray, byte> blur_plate = Image_utils.gaussian_blur(plate);
            Image<Gray, byte> threshold_plate = Image_utils.adaptive_threshold(plate);
            //Image<Gray, byte> threshold_plate = plate.ThresholdBinary(new Gray(100), new Gray(255));

            //CvInvoke.Imshow("plate", threshold_plate);

            VectorOfVectorOfPoint contours = Image_utils.find_all_contours(threshold_plate);
            
            List<VectorOfPoint> hulls = get_convex_hull(plate, contours);
            Image<Gray, byte> hull_img = new Image<Gray, byte>(plate.Width, plate.Height, new Gray(0));
            
            foreach (VectorOfPoint hull in hulls)
            {
                CvInvoke.FillConvexPoly(hull_img, hull, new MCvScalar(255));
            }

            //CvInvoke.Imshow("hull", hull_img);

            VectorOfVectorOfPoint final_contours = Image_utils.find_all_contours(hull_img);

            if (final_contours == null)
            {
                return segmented_characters;
            }

            Dictionary<Rectangle, VectorOfPoint> dict_to_order = new Dictionary<Rectangle, VectorOfPoint>();

            for (int i = 0; i < final_contours.Size; i++)
            {
                Rectangle r = CvInvoke.BoundingRectangle(final_contours[i]);
                dict_to_order.Add(r, final_contours[i]);
            }

            List<VectorOfPoint> ordered_contours = new List<VectorOfPoint>();
            var ordered_result = dict_to_order.OrderByDescending(i => i.Key.X);

            foreach (KeyValuePair<Rectangle, VectorOfPoint> kvp in ordered_result)
            {
                ordered_contours.Add(kvp.Value);
                Image<Gray, byte> character = plate.Copy(kvp.Key);
                segmented_characters.Add(character);
            }
            //CvInvoke.WaitKey();
            //CvInvoke.DestroyAllWindows();
            return segmented_characters;

        }


        public static List<Image<Gray, byte>> test(Image<Gray, byte> plate_after_preprocessing)
        {
            VectorOfVectorOfPoint contours = Image_utils.find_all_contours(plate_after_preprocessing);
            List<VectorOfPoint> hulls = get_convex_hull(plate_after_preprocessing, contours);
            List<Image<Gray, byte>> order_character_image = ordered_character(plate_after_preprocessing, hulls);
            return order_character_image;
        }



    }
}
