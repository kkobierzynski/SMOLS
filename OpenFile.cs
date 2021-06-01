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

        private List<byte[]> _bufferedSamples = new List<byte[]>();
        //private byte[][] _bufferedSamples;                   //buffer for 10s-timed samples; it goes like [0-2][0-n], where n depdends on sampling frequency (time is const = 20s)
        private short _totalNumberOfBuffers = 3;
        //private short _currentBufferIndex = 0;              //buffers 0, 1, 2 can be used; every 20s; this is only counter to know which buffer is used
        private const short _bufferSizeSeconds = 20;
        //private short noOfBuffersPreloaded = 0;
        private bool _readProcessStarted = false;



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

            //ensure try/catch structure is here (like a big IF)
            var mediaInfo = FFProbe.Analyse(_filePath);


            _totalTimeMiliseconds = mediaInfo.Duration.TotalMilliseconds;
            _sampleRate = mediaInfo.PrimaryAudioStream.SampleRateHz;
            _totalSamplesNumber = (ulong)(_sampleRate / (double)1000 * _totalTimeMiliseconds);



            //Task<MemoryStream> readTask = readAudioChunk(0, 10);
            //readTask.Wait();

            var memoryStream = Task.Run(() => readAudioChunk(0, 10)).GetAwaiter().GetResult();


            long aaa = memoryStream.Length;

            byte[] aaaa = memoryStream.ToArray();

            //int aa = short.max


            int kkk = 0;

        }


        public short getSampleValue(long sampleNumber, short channel)
        {
            long startSecondBuffered = _startSecondBuffered;
            long endSecondBuffered = _endSecondBuffered;
            List<long> midSecondRanges = new List<long>();

            for(short i=0; i<((endSecondBuffered-startSecondBuffered)/ _bufferSizeSeconds); i++)
            {
                midSecondRanges.Add(startSecondBuffered + i* _bufferSizeSeconds);
            }



            for(short i=0; i<midSecondRanges.Count; i++)
            {
                if((sampleNumber/ _sampleRate >= midSecondRanges[i]) && (sampleNumber / _sampleRate < midSecondRanges[i] + _bufferSizeSeconds))
                {
                    short sample = _bufferedSamples[i][sampleNumber * 2 + channel - _sampleRate * midSecondRanges[i]];

                    if((i == midSecondRanges.Count - 1) && _readProcessStarted == false)
                    {
                        //start new read process (probably? what if one was started and didn't finish??)

                        _readProcessStarted = true;
                    }

                    return sample;
                }
            }


            if (_readProcessStarted == false)
            {
                // we're out of scope of buffered audio; begin fresh buffering process just like loading a new file
            }
            else
            {
                //we're waiting for a read process to finish

                while (_readProcessStarted == true) { }

                return getSampleValue(sampleNumber, channel);

            }


            /*
            if(((sampleBlock*10000+sampleNumber)/_sampleRate >= _startSecondBuffered )&& ((sampleBlock * 10000 + sampleNumber)/ _sampleRate < _endSecondBuffered)){
                if (((sampleBlock * 10000 + sampleNumber) / _sampleRate >= (_startSecondBuffered+(_totalNumberOfBuffers-1)* _bufferSizeSeconds)) && ((sampleBlock * 10000 + sampleNumber) / _sampleRate < _endSecondBuffered))
                {
                    //start reading new 20s chunk of audio file, paste it to memory
                    return 0;
                }
                else
                {
                    return 0;
                }
            }
            */


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

        public int testowanko()
        {
            return 0;
        }


    }
}
