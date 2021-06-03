using FFMpegCore;
using FFMpegCore.Pipes;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SMOLS2000
{
    class OpenFile
    {

        private string _filePath = "";
        private string _fileName = "";
        private double _totalTimeMiliseconds = 0;
        private int _sampleRate = 0;
        private short _numberOfChannels = 0;
        private ulong _totalSamplesNumber = 0;
        private long _startSecondBuffered = -1;
        private long _endSecondBuffered = -1;
        
        private byte[] _bufferedSamples;                   //buffer for 20s-timed samples; it goes [0-n], where n depdends on sampling frequency (time is const = 20s)
        private const short _bufferSizeSeconds = 20;



        public OpenFile(MainWindow mainWindow)
        {
           
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "Audio files (*.wav, *.flac, *.mp3, *.m4a, *.ogg)|*.wav; *.flac; *.mp3; *.m4a; *.ogg|All files (*.*)|*.*";


            if (openFile.ShowDialog() == true)
            {
                _filePath = openFile.FileName;
                if(_filePath != "")
                {
                    _fileName = _filePath.Substring(_filePath.LastIndexOf('\\') + 1);
                    mainWindow.openFileGrid.Visibility = Visibility.Hidden;
                    mainWindow.appMainCanvas.Visibility = Visibility.Visible;
                }
                
            }

            //ensure try/catch structure is here (like a big IF)!!!
            var mediaInfo = FFProbe.Analyse(_filePath);


            _totalTimeMiliseconds = mediaInfo.Duration.TotalMilliseconds;
            _sampleRate = mediaInfo.PrimaryAudioStream.SampleRateHz;
            _totalSamplesNumber = (ulong)(_sampleRate / (double)1000 * _totalTimeMiliseconds);
            _numberOfChannels = (short)mediaInfo.PrimaryAudioStream.Channels;

    

            var firstMemoryStream = Task.Run(() => readAudioChunk(0, _bufferSizeSeconds)).GetAwaiter().GetResult();
            

            if (firstMemoryStream.Length == (2 * _numberOfChannels * _bufferSizeSeconds * _sampleRate))
            {
                _bufferedSamples = firstMemoryStream.ToArray();
                _startSecondBuffered = 0;
                _endSecondBuffered = _bufferSizeSeconds;

            }
            else if(firstMemoryStream.Length > 0)
            {
                _bufferedSamples = firstMemoryStream.ToArray();
                _startSecondBuffered = 0;
                _endSecondBuffered = _bufferedSamples.Length / 2 / _numberOfChannels / _sampleRate;
                //we got the file, but it's suspisiously short; an additional action *may* be required; perhaps a warning for user??
            }
            else
            {
                //error reading file; Do something, the app won't work!!
            }

        }


        public short getSampleValue(long sampleNumber, short channel)
        {
          
            long startSecondBuffered = _startSecondBuffered;
            long endSecondBuffered = _endSecondBuffered;
            

            bool successfullyRead = false;


            //now working, but should be refined - what if a wrong number of sample is called?
            while (!successfullyRead)
            {
                if ((sampleNumber / _sampleRate >=startSecondBuffered) && (sampleNumber / _sampleRate < endSecondBuffered))
                {

                    short sample = (short)((int)(_bufferedSamples[sampleNumber * 2 * _numberOfChannels + 2*channel - 2*_numberOfChannels*_sampleRate * startSecondBuffered] << 8) + ((int)_bufferedSamples[sampleNumber * 2 * _numberOfChannels + 2*channel - 2*_numberOfChannels*_sampleRate * startSecondBuffered + 1]) - 32768);
                    int aaa = _bufferedSamples[sampleNumber * 2 * _numberOfChannels + channel - 2 * _numberOfChannels * _sampleRate * startSecondBuffered] << 8;
                    int bbb = _bufferedSamples[sampleNumber * 2 * _numberOfChannels + channel - 2 * _numberOfChannels * _sampleRate * startSecondBuffered + 1];

                    successfullyRead = true;                //is it useful?
                    return sample;

                }
                else
                {
                    long startTime = ((long)(sampleNumber / _sampleRate)/_bufferSizeSeconds)*_bufferSizeSeconds;
                    var memoryStream = Task.Run(() => readAudioChunk(startTime, startTime+_bufferSizeSeconds)).GetAwaiter().GetResult();
                    _bufferedSamples = memoryStream.ToArray();
                    startSecondBuffered = _startSecondBuffered = startTime;
                    endSecondBuffered = _endSecondBuffered = startTime + _bufferedSamples.Length / 2 / _numberOfChannels/_sampleRate;               //this line can be responsible for cutting the end of file (< 1s); To be fixed
                }
            }
        


            return 0;
        }


        private async Task<MemoryStream> readAudioChunk(long secondStart, long secondEnd)
        {
            
            var memoryStream = new MemoryStream();

            string time = "-ss " + secondStart + " -to " + secondEnd;

            await FFMpegArguments
                .FromFileInput(_filePath, true, options => options.WithCustomArgument(time))
                .OutputToPipe(new StreamPipeSink(memoryStream), options => options.ForceFormat("u16be"))
                .ProcessAsynchronously();


            return memoryStream;
        }


        public string getFileName()
        {
            return _fileName;
        }

    }
}
