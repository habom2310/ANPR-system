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
    public class Preprocessing
    {

        #region preprocessing functions
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input_image"></param>
        /// <returns></returns>
        public static Image<Gray, byte> gaussian_blur(Image<Gray, byte> input_image)
        {
            Image<Gray, byte> output_image = input_image.SmoothGaussian(7);

            return output_image;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input_image"></param>
        /// <returns></returns>
        public static Image<Gray, float> sobel_X(Image<Gray, byte> input_image)
        {
            Image<Gray, float> output_image = input_image.Sobel(1, 0, 3);

            return output_image;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input_image"></param>
        /// <returns></returns>
        public static Image<Gray, byte> adaptive_threshold(Image<Gray, byte> input_image)
        {
            Image<Gray, byte> output_image = input_image.ThresholdAdaptive(new Gray(255), AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 11, new Gray(2));

            return output_image;
        }

        /// <summary>
        /// Implement Binary and then Otsu threshold
        /// </summary>
        /// <param name="input_image"></param>
        /// <returns></returns>
        public static Image<Gray, byte> threshold(Image<Gray, byte> input_image)
        {
            Image<Gray, byte> binary_thresh = new Image<Gray, byte>(input_image.Width, input_image.Height);
            CvInvoke.Threshold(input_image, binary_thresh, 0, 255, ThresholdType.Binary);

            Image<Gray, byte> output_image = new Image<Gray, byte>(input_image.Width, input_image.Height);
            CvInvoke.Threshold(binary_thresh, output_image, 0, 255, ThresholdType.Otsu);

            return output_image;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input_image"></param>
        /// <returns></returns>
        public static Image<Gray, byte> morphology_ex(Image<Gray, byte> input_image, Plate_feature.type_of_plate type)
        {
            Mat structure = new Mat();

            if (type == Plate_feature.type_of_plate.RECT_PLATE)
            {
                structure = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(22, 3), new Point(-1, -1));
            }
            else if (type == Plate_feature.type_of_plate.SQUARE_PLATE)
            {
                structure = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(26, 5), new Point(-1, -1));
            }

            Image<Gray, byte> output_image = input_image.MorphologyEx(MorphOp.Close, structure, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(255));

            return output_image;
        }

        #endregion

    }
}
