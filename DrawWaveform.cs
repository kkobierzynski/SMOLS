using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;



namespace SMOLS2000
{
    
    /// <summary>
    /// A prototype of the class allowing to draw a waveform basing on the table of samples.
    /// </summary>
    class DrawWaveform
    {
        private static WriteableBitmap writeableBitmap;

        /// <summary>
        /// Method draws a test waveform in accordance with the class description.
        /// </summary>
        /// <param name="mainWindow">Current instance of mainWindow class</param>
        public DrawWaveform(MainWindow mainWindow)
        {
            int maxWidth = 752;
            int maxHeight = 100;

            writeableBitmap = new WriteableBitmap(
                maxWidth,
                maxHeight,
                96,                     //dpi - X axis
                96,                     //dpi - Y axis
                PixelFormats.Bgr32,     //natively supported format
                null);                  //bitmap palette

            byte[] GreenColorData = { 115, 137, 47, 50 };   // B G R
            byte[] BlackColorData = { 0, 0, 0, 50 };        // B G R
            byte[] WhiteColorData = { 255, 255, 255, 50 };  // B G R


            for (int x = 0; x < maxWidth; x++)                      //change background to white
            {

                for (int y = 0; y < maxHeight; y++)
                {
                    if (y != maxHeight / 2)     //axis line remains black
                    {
                        Int32Rect rect = new Int32Rect(
                            x,
                            y,
                            1,
                            1);

                        writeableBitmap.WritePixels(rect, WhiteColorData, 4, 0);
                    }

                }
            }

            int nrSamples = 100000;     //just for a test; later - the information about number of samples should be taken from sound file

            double[] sine = new double[nrSamples];
            short[] tab = new short[nrSamples];


            for (int i = 0; i < nrSamples; i++)
            {
                sine[i] = Math.Sin(i);              //just for a test; sine waveform - values <-1, 1>
                tab[i] = (short)(32767 * sine[i]);  //just for a test; simulation of sine waveform - values <-32767, 32767>      later - change to real table taken from sound file

            }

            int step = (int)Math.Floor((double)nrSamples / maxWidth);   //indicates which samples should be considered to be drawn on the bitmap

            for (int x = 0; x < maxWidth; x++)
            {
                for (int y = 0; y < maxHeight / 2; y++)                 //upper part of the waveform - above X axis
                {

                    if (y > tab[0 + x * step] * (maxHeight / 2) / 32767 + maxHeight / 2)     //maxHeight / 2 means X axis
                    {
                        Int32Rect rect = new Int32Rect(
                                x,
                                y,
                                1,
                                1);

                        writeableBitmap.WritePixels(rect, GreenColorData, 4, 0);
                    }
                }

                for (int y = maxHeight / 2; y < maxHeight; y++)         //lower part of the waveform - below X axis
                {

                    if (y < tab[0 + x * step] * (maxHeight / 2) / 32767 + maxHeight / 2)
                    {
                        Int32Rect rect = new Int32Rect(
                                x,
                                y,
                                1,
                                1);

                        writeableBitmap.WritePixels(rect, GreenColorData, 4, 0);
                    }
                }


            }

            mainWindow.waveform.Source = writeableBitmap;   //show ready bitmap

        }

    }
}
