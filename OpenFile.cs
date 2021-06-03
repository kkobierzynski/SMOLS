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
        private double _normalisedNoiseLevel = 0.02;         //default value; to be discussed / measured which value is most common
        private double _normalisedSignalLevel = 0.04;         //default value; to be discussed / measured which value is most common

        private byte[] _bufferedSamples;                   //buffer for 20s-timed samples; it goes [0-n], where n depdends on sampling frequency (time is const = 20s)
        private const short _bufferSizeSeconds = 20;


        //TEMP CODE!!! REMOVE LATER!
        List<short> outputSamples = new List<short>();



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

            audioFileAnalysis();

        }

        //TEMP CODE!!! REMOVE LATER!
        public void saveSampleValue(short sample)
        {
            outputSamples.Add(sample);
        }

        //TEMP CODE!!! REMOVE LATER!
        public void saveFile()
        {
            //var firstMemoryStream = Task.Run(() => saveAudio(outputSamples)).GetAwaiter().GetResult();
            //int aaa = 0;
            StreamWriter filee = new StreamWriter("aaa.txt");

            for(int i=0; i<(int)_totalSamplesNumber; i++)
            {
                filee.WriteLine(outputSamples[i]);
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


        public double getSignalLevel()
        {
            return _normalisedSignalLevel;
        }

        public double getNoiseLevel()
        {
            return _normalisedNoiseLevel;
        }

        public string getFileName()
        {
            return _fileName;
        }

        public int getSampleRate()
        {
            return _sampleRate;
        }

        public ulong getTotalSamplesNumber()
        {
            return _totalSamplesNumber;
        }

        public string getPlayTime()
        {
            int hours = (int)(_totalTimeMiliseconds / 3600000);
            short minutes = (short)((_totalTimeMiliseconds / 60000) - (hours * 60));
            short seconds = (short)((_totalTimeMiliseconds / 1000) - (hours * 3600) - (minutes * 60));

            string hoursString = hours.ToString();
            if(hoursString.Length == 1)
            {
                hoursString = "0" + hoursString;

            } else if(hoursString.Length == 0)
            {
                hoursString = "00";
            }

            string minutesString = minutes.ToString();
            if (minutesString.Length == 1)
            {
                minutesString = "0" + minutesString;
            }
            else if (hoursString.Length == 0)
            {
                minutesString = "00";
            }

            string secondsString = seconds.ToString();
            if (secondsString.Length == 1)
            {
                secondsString = "0" + secondsString;
            }
            else if (hoursString.Length == 0)
            {
                secondsString = "00";
            }

            return hoursString + ":" + minutesString + ":" + secondsString;
        }

        private void audioFileAnalysis()
        {
            //perform analysis only for files longer than 12 seconds; otherwise use default values.
            if(_totalTimeMiliseconds >= 12000)
            {
                short measureTime = 2000;           //measure time - in ms
                short effectiveMeasureTime = (short)(measureTime - 200);     //eliminate compression bugs (e.g. silence) on the edges of read samples

                short fMinNoise = 20;             //min frequency of noise; 20 Hz here

                short numberOfMeasures;
                //if a file is longer than 20 minutes - perform 100x 2s measurements. More won't be necessary.
                if (_totalTimeMiliseconds >= 1200000)
                {
                    numberOfMeasures = 100;
                }
                else
                {
                    numberOfMeasures = (short)(_totalTimeMiliseconds / 12000);
                }

                //measurement algorithm

                long measureStepMs = (long)(_totalTimeMiliseconds / (numberOfMeasures+1));


                List<long> secondsRanges = new List<long>();

                for (short i = 0; i < numberOfMeasures; i++)
                {
                    secondsRanges.Add((i+1)*measureStepMs/1000);
                }

                long maxFramePower = _sampleRate * (long)effectiveMeasureTime / 1000 * 32767 * 32767;
                short noiseFramesAnalysisNumber = (short)(fMinNoise * effectiveMeasureTime / 1000);            //the detection must not be on a sample basis; f_min = 20 Hz noise here
                int noiseFrameDurationInSamples = _sampleRate / fMinNoise;                                    //how many samples are within a 20 Hz wave
                long maxNoisePower = maxFramePower / noiseFramesAnalysisNumber;

                List<long> partialFramesPowers = new List<long>();
                List<long> partialNoisePowers = new List<long>();


                for(short i=0; i < numberOfMeasures; i++)
                {
                    var memoryStream = Task.Run(() => readAudioChunk(secondsRanges[i], secondsRanges[i]+ measureTime/1000)).GetAwaiter().GetResult();

                    byte[] samples = new byte[effectiveMeasureTime * 2 * _numberOfChannels * _sampleRate / 1000];

                    //int aaa = memoryStream.ToArray().Count();
                    //int bbb = (measureTime - effectiveMeasureTime) * _numberOfChannels * _sampleRate / 1000;
                    //int ccc = effectiveMeasureTime * 2 * _numberOfChannels * _sampleRate / 1000;


                    Array.Copy(memoryStream.ToArray(), (measureTime- effectiveMeasureTime) * _numberOfChannels * _sampleRate / 1000, samples, 0, effectiveMeasureTime * 2 * _numberOfChannels * _sampleRate / 1000);

                    List<List<short>> samplesShort = new List<List<short>>();

                    for (int j=0; j<_numberOfChannels; j++)
                    {
                        samplesShort.Add(new List<short>());
                    }


                    //after this for loop we have all samples in a dynamic 2D array - first dimention is channel, second is sample number;
                    for(int j=0; j < (samples.Length/2/_numberOfChannels); j++)
                    {
                        for(short k=0; k<_numberOfChannels; k++)
                        {
                            short sample = (short)((int)(samples[j * 2 * _numberOfChannels + 2 * k] << 8) + ((int)samples[j * 2 * _numberOfChannels + 2 * k + 1]) - 32768);
                            samplesShort[k].Add(sample);
                        }
                    }

                    List<long> signalPowerValues = new List<long>();
                    List<long> channelsNoisePowerValues  = new List<long>();
                    //List<long> noisePowerValues = new List<long>();
                    List<List<long>> smallFramesNoisePowerValues = new List<List<long>>();

                    for (short j = 0; j<_numberOfChannels; j++)
                    {
                        signalPowerValues.Add(0);
                        channelsNoisePowerValues.Add(0);
                        //noisePowerValues.Add(0);
                        smallFramesNoisePowerValues.Add(new List<long>());
                        for (short k=0; k< noiseFramesAnalysisNumber; k++)
                        {
                            smallFramesNoisePowerValues[j].Add(0);
                        }

                    }

                    for(short j=0; j< noiseFramesAnalysisNumber; j++)
                    {
                        for(int k=0; k< noiseFrameDurationInSamples; k++)
                        {
                            for(short m=0; m<_numberOfChannels; m++)
                            {
                                smallFramesNoisePowerValues[m][j] += samplesShort[m][j * noiseFrameDurationInSamples + k] * samplesShort[m][j * noiseFrameDurationInSamples + k];
                            }
                        }
                    }



                    for(short j=0; j<_numberOfChannels; j++)
                    {
                        signalPowerValues[j] = smallFramesNoisePowerValues[j].Sum();
                        channelsNoisePowerValues[j] = smallFramesNoisePowerValues[j].Min();

                    }
                    partialFramesPowers.Add(signalPowerValues.Max());
                    partialNoisePowers.Add(channelsNoisePowerValues.Max());




                }



                List<double> normalisedPartialFramesPowers = new List<double>();
                long noisePowerLong = partialNoisePowers.Min();

                for(short i=0; i < numberOfMeasures; i++)
                {
                    normalisedPartialFramesPowers.Add(partialFramesPowers[i] * 1.0 / maxFramePower);
                }

                _normalisedNoiseLevel = Math.Sqrt(noisePowerLong*1.0 / maxNoisePower);
                _normalisedSignalLevel = Math.Sqrt(normalisedPartialFramesPowers.Sum() / numberOfMeasures);


                //end of measurements, values are set;

            }
        }


    }
}
