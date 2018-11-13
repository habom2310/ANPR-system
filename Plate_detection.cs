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
    public class Plate_detection
    {
        /// <summary>
        /// check whether the plate is tilted or not, if yes, rotate it.
        /// </summary>
        /// <param name="plate"></param>
        /// <param name="plate_feature"></param>
        /// <returns>rotated image</returns>
        private static Image<Gray, byte> crop_and_rotated_plate(Image<Gray, byte> plate, RotatedRect plate_feature)
        {
            PointF[] boxes = CvInvoke.BoxPoints(plate_feature);

            List<double> Xs = new List<double>();
            List<double> Ys = new List<double>();
            foreach (PointF box in boxes)
            {
                Xs.Add(box.X);
                Ys.Add(box.Y);
            }

            double Xmax = Xs.Max();
            double Ymax = Ys.Max();

            double Xmin = Xs.Min();
            double Ymin = Ys.Min();
            

            Rectangle r = plate_feature.MinAreaRect();
            double X = r.X;
            double Y = r.Y;
            double W = r.Width;
            double H = r.Height;
            double angle = plate_feature.Angle;

            float X_center = (float)(Xmax + Xmin) / 2;
            float Y_center = (float)(Ymax + Ymin) / 2;

            Size patch_size = new Size((int)(Xmax-Xmin),(int)(Ymax-Ymin));


            if (angle < (-45))
            {
                angle = angle + 90;
            }

            if (angle == 0)
            {
                return plate;
            }


            Mat map_matrix = new Mat(new Size(2, 3), DepthType.Cv64F, 1);
            PointF center = new PointF(X_center, Y_center);
            CvInvoke.GetRotationMatrix2D(center, angle, 1.0, map_matrix);

            Image<Gray, byte> cropped = new Image<Gray, byte>(patch_size);
            CvInvoke.GetRectSubPix(plate, patch_size, center, cropped);

            Image<Gray, byte> warp_affine = cropped.WarpAffine(map_matrix, Inter.Linear, Warp.Default, BorderType.Default, new Gray(0));


            int warp_H = 0;
            int warp_W = 0;
            if (warp_affine.Width > warp_affine.Height)
            {
                warp_H = warp_affine.Height;
                warp_W = warp_affine.Width;
            }
            else
            {
                warp_H = warp_affine.Width;
                warp_W = warp_affine.Height;
            }

            Image<Gray, byte> output = new Image<Gray, byte>(warp_W, warp_H);
            CvInvoke.GetRectSubPix(warp_affine, new Size(warp_W, warp_H), center, output);
            //CvInvoke.Imshow("warpaffine", warp_affine);
            //CvInvoke.Imshow("GetRectSubPix", output);
            //CvInvoke.WaitKey();

        
            //CvInvoke.Imshow("plate", output);
            //CvInvoke.WaitKey();
            return warp_affine;

            //return plate;
        }
        /// <summary>
        /// clean the input suspected plate image and decide if that is a possible plate or not by checking ratio condition.
        /// </summary>
        /// <param name="suspected_plate"></param>
        /// <param name="type"></param>
        /// <returns>a tupple that contains:
        /// 1: image of the plate,
        /// 2: the (x,y,w,h) of the actual plate in the image
        /// </returns>
        public static Tuple<Image<Gray, byte>, Rectangle> clean_plate(Image<Gray, byte> suspected_plate, Plate_feature.type_of_plate type)
        {
            //Image<Gray, byte> threshold_plate = Image_utils.adaptive_threshold(suspected_plate);
            Image<Gray, byte> threshold_plate = suspected_plate.ThresholdBinary(new Gray(100), new Gray(255));
            VectorOfVectorOfPoint contours = Image_utils.find_all_contours(threshold_plate);

            Rectangle r = new Rectangle();

            Tuple<Image<Gray, byte>, Rectangle> output = new Tuple<Image<Gray, byte>, Rectangle>(suspected_plate, r);

            if (contours != null)
            {
                double max_area = 0;
                VectorOfPoint max_contour = new VectorOfPoint();

                for (int i = 0; i < contours.Size; i++)
                {
                    double temp = CvInvoke.ContourArea(contours[i]);
                    if (temp > max_area)
                    {
                        max_area = temp;
                        max_contour = contours[i];
                    }
                }

                VectorOfVectorOfPoint c = new VectorOfVectorOfPoint(max_contour);
                CvInvoke.DrawContours(threshold_plate, c, 0, new MCvScalar(0), 2);

                r = CvInvoke.BoundingRectangle(max_contour);

                RotatedRect plate_feature = CvInvoke.MinAreaRect(max_contour);
                Image<Gray, byte> rotated_plate = crop_and_rotated_plate(suspected_plate, plate_feature);

                output = new Tuple<Image<Gray, byte>, Rectangle>(rotated_plate, r);

                return output;
            }
            else
            {
                return output;
            }
        }

        /// <summary>
        /// check whether the possible plate after clean func is a plate or not by finding the characters in it. 
        /// </summary>
        /// <param name="suspected_plate"></param>
        /// <param name="type"></param>
         /// <returns></returns>
        public static Tuple<bool, List<Image<Gray, byte>>> check_plate_has_character(Image<Gray, byte> suspected_plate, Plate_feature.type_of_plate type)
        {
            
            List<Image<Gray, byte>> characters_found_on_plate = Plate_segmentation.segment_characters_from_plate(suspected_plate);
            Tuple<bool, List<Image<Gray, byte>>> output = new Tuple<bool, List<Image<Gray, byte>>>(false, characters_found_on_plate);

            if (characters_found_on_plate.Count > 5)
            {
                output = new Tuple<bool, List<Image<Gray, byte>>>(true, characters_found_on_plate);
                return output;
            }
            else
            {
                return output;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input_img">original image</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<Image<Gray, byte>> find_all_possible_plates(Image<Gray, byte> input_img, Plate_feature.type_of_plate type)
        {
            Image<Gray, byte> blur = Image_utils.gaussian_blur(input_img);
            Image<Gray, byte> sobelx = Image_utils.sobel_X(blur).Convert<Gray, byte>();
            Image<Gray, byte> adaptive = Image_utils.adaptive_threshold(sobelx);
            Image<Gray, byte> morp = Image_utils.morphology_ex(adaptive, Plate_feature.type_of_plate.RECT_PLATE);

            VectorOfVectorOfPoint contours = Image_utils.find_all_contours(morp);

            List<Image<Gray, byte>> approved_plates = new List<Image<Gray, byte>>();

            for (int i = 0; i < contours.Size; i++)
            {
                double area = CvInvoke.ContourArea(contours[i]);

                Rectangle r = CvInvoke.BoundingRectangle(contours[i]);
                RotatedRect rr = CvInvoke.MinAreaRect(contours[i]);

                Image<Gray, byte> suspected_plate = input_img.Copy(r);


                Tuple<Image<Gray, byte>, Rectangle> after_clean_plate_tuple;
                Tuple<bool, List<Image<Gray, byte>>> after_check_plate_has_characters_tuple;

                if (!Plate_feature.ratio_check(type, area, r.Width, r.Height))
                {
                    continue;
                }
                else
                {
                    if (!Plate_feature.validate_rotation_and_ratio(type, rr))
                    {
                        continue;
                    }
                    else
                    {
                        after_clean_plate_tuple = clean_plate(suspected_plate, type);
                        after_check_plate_has_characters_tuple = check_plate_has_character(after_clean_plate_tuple.Item1, type);

                        if (!after_check_plate_has_characters_tuple.Item1)
                        {
                            continue;
                        }
                        else
                        {
                            approved_plates = after_check_plate_has_characters_tuple.Item2;
                        }
                    }
                }
            }
            return approved_plates;
        }
    }
}
