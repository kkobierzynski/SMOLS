using FFMpegCore;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SMOLS2000
{
    class OpenFile
    {

        private string filePath = "";
        private string fileName = "";
        private double totalTimeMiliseconds = 0;



        public OpenFile(MainWindow mainWindow)
        {
           
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "Audio files (*.wav, *.flac, *.mp3, *.m4a, *.ogg)|*.wav; *.flac; *.mp3; *.m4a; *.ogg|All files (*.*)|*.*";


            if (openFile.ShowDialog() == true)
            {
                filePath = openFile.FileName;
                if(filePath != "")
                {
                    fileName = filePath.Substring(filePath.LastIndexOf('\\') + 1);
                    mainWindow.openFileGrid.Visibility = Visibility.Hidden;
                    mainWindow.appMainCanvas.Visibility = Visibility.Visible;
                }
                
            }

            //ensure try/catch structure is here (like a big IF)
            var mediaInfo = FFProbe.Analyse(filePath);

            totalTimeMiliseconds = mediaInfo.Duration.TotalMilliseconds;




        }

        public string getFileName()
        {
            return fileName;
        }

        public int testowanko()
        {
            return 0;
        }


    }
}
