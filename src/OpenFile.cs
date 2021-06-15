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
    /// <summary>
    /// Class contains objects representing 1 open audio file.
    /// </summary>
    public class OpenFile
    {
        private string _filePath = "";                          //variable contains a complete file path
        private string _fileName = "";                          //variable contains a file name (with extension)
        private double _totalTimeMiliseconds = 0;
        private int _sampleRate = 0;
        private short _numberOfChannels = 0;
        private ulong _totalSamplesNumber = 0;                  //variable represents total number of all samples within read AV file;
        private long _startSecondBuffered = -1;                 //second of current buffered frame (where buffer begins, this means what we have in memory)
        private long _endSecondBuffered = -1;                   //second of current buffered frame (where buffer ends, this means what we have in memory)
        private double _normalisedNoiseLevel = 0.0004;          //default value;
        private double _normalisedSignalLevel = 0.04;           //default value;

        private byte[] _bufferedSamples;                        //buffer for samples saved in memory; it's size may vary depending on sampling frequency;
        private const short _bufferSizeSeconds = 20;            //time of buffered samples in seconds



        /// <summary>
        /// Constructor for initializing opening file sequence (with "Open" button).
        /// </summary>
        /// <param name="mainWindow">Current instance of mainWindow class</param>
        public OpenFile(MainWindow mainWindow) : this(mainWindow, null) { }

        /// <summary>
        /// Constructor for initializing opening file sequence (using drag/drop function, providing file path).
        /// </summary>
        /// <param name="mainWindow">Current instance of mainWindow class</param>
        /// <param name="filePath">Complete path to the AV file (passed as a string)</param>
        public OpenFile(MainWindow mainWindow, string filePath)
        {
            if (filePath == null)
            {

                OpenFileDialog openFile = new OpenFileDialog();
                openFile.Filter = "Audio files (*.wav, *.flac, *.mp3, *.m4a, *.ogg)|*.wav; *.flac; *.mp3; *.m4a; *.ogg|All files (*.*)|*.*";


                if (openFile.ShowDialog() == true)
                    _filePath = openFile.FileName;

            }
            else
            {
                _filePath = filePath;
            }
            //ensure try/catch structure is here (like a big IF)!!!

            //if file name is empty - no file was opened; we don't want to run anything then
            if (_filePath != "")
            {
                try
                {
                    var mediaInfo = FFProbe.Analyse(_filePath);

                    _totalTimeMiliseconds = mediaInfo.Duration.TotalMilliseconds;
                    _sampleRate = mediaInfo.PrimaryAudioStream.SampleRateHz;
                    _totalSamplesNumber = (ulong)(_sampleRate / (double)1000 * _totalTimeMiliseconds);
                    _numberOfChannels = (short)mediaInfo.PrimaryAudioStream.Channels;

                } catch (Exception e)
                {
                    //#TODO
                    //An error for user - a file is not a valid AV file; choose a different file!
                }

                if (_totalTimeMiliseconds > 10)
                {

                    var firstMemoryStream = Task.Run(() => readAudioChunk(0, _bufferSizeSeconds)).GetAwaiter().GetResult();


                    if (firstMemoryStream.Length == (2 * _numberOfChannels * _bufferSizeSeconds * _sampleRate))
                    {
                        //saving first batch of samples to a local buffer;
                        _bufferedSamples = firstMemoryStream.ToArray();
                        _startSecondBuffered = 0;
                        _endSecondBuffered = _bufferSizeSeconds;

                        //file has been opened - change UI accordingly;
                        _fileName = _filePath.Substring(_filePath.LastIndexOf('\\') + 1);
                        mainWindow.openFileGrid.Visibility = Visibility.Hidden;
                        mainWindow.appMainCanvas.Visibility = Visibility.Visible;

                        audioFileAnalysis();

                    }
                    else if (firstMemoryStream.Length > 0)
                    {
                        //we got the file, but it's suspisiously short; saving first batch of samples to a local buffer;
                        _bufferedSamples = firstMemoryStream.ToArray();
                        _startSecondBuffered = 0;
                        _endSecondBuffered = _bufferedSamples.Length / 2 / _numberOfChannels / _sampleRate;

                        //file has been opened - change UI accordingly;
                        _fileName = _filePath.Substring(_filePath.LastIndexOf('\\') + 1);
                        mainWindow.openFileGrid.Visibility = Visibility.Hidden;
                        mainWindow.appMainCanvas.Visibility = Visibility.Visible;

                        audioFileAnalysis();
                        
                        //#TODO
                        //A warning for user - a file seems to be very short
                    }
                    else
                    {
                        //#TODO
                        //An error for user - a file is empty / couldn't be opened;
                    }

                    
                }
                else
                {
                    //#TODO
                    //An error for user - a file is empty / couldn't be opened;
                }
            }

        }

        /// <summary>
        /// Method returns one sample (signed short) at a given number and channel.
        /// </summary>
        /// <param name="sampleNumber">Number of sample that has to be returned (starting with 0)</param>
        /// <param name="channel">Channel number (starting with 0)</param>
        /// <returns>1 short value, representing sample value (signed 16-bit short)</returns>
        public short getSampleValue(long sampleNumber, short channel)
        {
            if ((ulong)sampleNumber < _totalSamplesNumber)
            {
                long startSecondBuffered = _startSecondBuffered;
                long endSecondBuffered = _endSecondBuffered;


                bool successfullyRead = false;


                //now partially working, but has to be refined - what if a wrong number of sample is called? (and other issues...)
                while (!successfullyRead)
                {
                    if ((sampleNumber / _sampleRate >= startSecondBuffered) && (sampleNumber / _sampleRate < endSecondBuffered))
                    {
                        short sample = 0;


                        //a temporary workaround for compressed files (mp3, ac3, etc...);
                        if (endSecondBuffered - startSecondBuffered < 20)
                        {
                            try
                            {
                                sample = (short)((int)(_bufferedSamples[sampleNumber * 2 * _numberOfChannels + 2 * channel - 2 * _numberOfChannels * _sampleRate * startSecondBuffered] << 8) + ((int)_bufferedSamples[sampleNumber * 2 * _numberOfChannels + 2 * channel - 2 * _numberOfChannels * _sampleRate * startSecondBuffered + 1]) - 32768);
                            }catch(Exception e)
                            {
                                sample = 0;
                            }

                            return sample;
                        }

                        sample = (short)((int)(_bufferedSamples[sampleNumber * 2 * _numberOfChannels + 2 * channel - 2 * _numberOfChannels * _sampleRate * startSecondBuffered] << 8) + ((int)_bufferedSamples[sampleNumber * 2 * _numberOfChannels + 2 * channel - 2 * _numberOfChannels * _sampleRate * startSecondBuffered + 1]) - 32768);

                        successfullyRead = true;                //is it useful?
                        return sample;

                    }
                    else
                    {
                        long startTime = ((long)(sampleNumber / _sampleRate) / _bufferSizeSeconds) * _bufferSizeSeconds;
                        var memoryStream = Task.Run(() => readAudioChunk(startTime, startTime + _bufferSizeSeconds)).GetAwaiter().GetResult();
                        _bufferedSamples = memoryStream.ToArray();
                        startSecondBuffered = _startSecondBuffered = startTime;
                        endSecondBuffered = _endSecondBuffered = startTime + _bufferedSamples.Length / 2 / _numberOfChannels / _sampleRate;         //this line was responsible for cutting the end of file (< 1s); To be refined

                        //a temporary workaround
                        if (endSecondBuffered - startSecondBuffered < 20)
                        {
                            _endSecondBuffered++;
                        }
                    }
                }
            }
            else
            {
                return 0;
            }
        
            return 0;
        }

        /// <summary>
        /// Asynchronous task aimed to read a certain chunk of open file.
        /// </summary>
        /// <param name="secondStart">Starting second of extracted chunk</param>
        /// <param name="secondEnd">Ending second of extracted chunk</param>
        /// <returns>Memory chunk containing retrieved samples</returns>
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

        /// <summary>
        /// Method updating open AV file parameters (noise power, signal power, etc.).
        /// </summary>
        private void audioFileAnalysis()
        {
            //perform analysis only for files longer than 12 seconds; otherwise use default values
            if(_totalTimeMiliseconds >= 12000)
            {
                short measureTime = 2000;                                   //measure time - in ms
                short effectiveMeasureTime = (short)(measureTime - 200);    //eliminate compression bugs (e.g. silence) on the edges of read samples
                short fMinNoise = 20;                                       //min frequency of noise; 20 Hz here
                short numberOfMeasures;

                //if a file is longer than 20 minutes - perform 100 2-seconds measurements. More probably won't be necessary.
                if (_totalTimeMiliseconds >= 1200000)
                    numberOfMeasures = 100;
                else
                    numberOfMeasures = (short)(_totalTimeMiliseconds / 12000);
                

                //measurement algorithm

                long measureStepMs = (long)(_totalTimeMiliseconds / (numberOfMeasures+1));
                List<long> secondsRanges = new List<long>();

                //it's important to set all measurements' times (begin - end).
                for (short i = 0; i < numberOfMeasures; i++)
                    secondsRanges.Add((i+1)*measureStepMs/1000);
                

                long maxFramePower = _sampleRate * (long)effectiveMeasureTime / 1000 * 32767 * 32767;       //maximum possible signal power in one measured frame (2s, 1.8s effectively)
                short noiseFramesAnalysisNumber = (short)(fMinNoise * effectiveMeasureTime / 1000);         //noise detection must not be on a sample basis; f_min = 20 Hz noise here;
                                                                                                            //this value tells us how many sub-frames there are going to be in one "big"
                                                                                                            //1.8s frame (sub-frames are ONLY for noise measurements).

                int noiseFrameDurationInSamples = _sampleRate / fMinNoise;                                  //how many samples are within a 20 Hz wave - a value needed for a loop below;
                long maxNoisePower = maxFramePower / noiseFramesAnalysisNumber;                             //maximum possible noise power in one measured sub-frame

                //these lists store all measured values (the absolute values); Number of elements within lists equals number of measurements (numberOfMeasures)
                List<long> partialFramesPowers = new List<long>();
                List<long> partialNoisePowers = new List<long>();

                //measurements loop; it works numberOfMeasures times, as defined above
                for (short i=0; i < numberOfMeasures; i++)
                {
                    //save read samples to a local array (samples); 
                    var memoryStream = Task.Run(() => readAudioChunk(secondsRanges[i], secondsRanges[i]+ measureTime/1000)).GetAwaiter().GetResult();
                    byte[] samples = new byte[effectiveMeasureTime * 2 * _numberOfChannels * _sampleRate / 1000];
                    Array.Copy(memoryStream.ToArray(), (measureTime- effectiveMeasureTime) * _numberOfChannels * _sampleRate / 1000, samples, 0, effectiveMeasureTime * 2 * _numberOfChannels * _sampleRate / 1000);

                    //create 2D array of samples as (short); The first index is for channel selection, the second index is sample number;
                    List<List<short>> samplesShort = new List<List<short>>();

                    //create list for every channel
                    for (int j=0; j<_numberOfChannels; j++)
                        samplesShort.Add(new List<short>());
                    


                    //after this for loop all samples are in a dynamic 2D array - first dimention is channel, second is sample number;
                    for(int j=0; j < (samples.Length/2/_numberOfChannels); j++)
                    {
                        for(short k=0; k<_numberOfChannels; k++)
                        {
                            short sample = (short)((int)(samples[j * 2 * _numberOfChannels + 2 * k] << 8) + ((int)samples[j * 2 * _numberOfChannels + 2 * k + 1]) - 32768);
                            samplesShort[k].Add(sample);
                        }
                    }

                    List<long> signalPowerValues = new List<long>();            //list contains summed signal power absolute values for every channel independently (within a 1.8s frame)
                    List<long> channelsNoisePowerValues  = new List<long>();    //list contains calculated noise powers (absolute values) for every channel independently (within a 1.8s frame)

                    //create 2D array of possible noise values (absolute values); The first index is for channel selection, the second index is for sub-frame selection;
                    List<List<long>> smallFramesNoisePowerValues = new List<List<long>>();

                    //initialization of initial list values (sums equal 0)
                    for (short j = 0; j < _numberOfChannels; j++)
                    {
                        signalPowerValues.Add(0);
                        channelsNoisePowerValues.Add(0);
                        smallFramesNoisePowerValues.Add(new List<long>());

                        for (short k = 0; k < noiseFramesAnalysisNumber; k++)
                            smallFramesNoisePowerValues[j].Add(0);
                    }

                    //for every noise sub-frame
                    for(short j=0; j< noiseFramesAnalysisNumber; j++)
                    {   //for every sample that exists in a sub-frame
                        for(int k=0; k< noiseFrameDurationInSamples; k++)
                        {   //for every channel independently
                            for(short m=0; m<_numberOfChannels; m++)
                            {   //calculate total signal power within a sub-frame (every channel independently)
                                smallFramesNoisePowerValues[m][j] += samplesShort[m][j * noiseFrameDurationInSamples + k] * samplesShort[m][j * noiseFrameDurationInSamples + k];
                            }
                        }
                    }

                    //for every channel:
                    for (short j = 0; j < _numberOfChannels; j++)
                    {   
                        signalPowerValues[j] = smallFramesNoisePowerValues[j].Sum();            //calculate total power of signal within a 1.8s frame (for every channel independently)
                        channelsNoisePowerValues[j] = smallFramesNoisePowerValues[j].Min();     //select the lowest signal power value found among the analyzed subframes (for each channel independently)
                    }

                    //save the highest power values (absolute values) found among all audio channels
                    //(this solves the issue of silent/empty channels)
                    partialFramesPowers.Add(signalPowerValues.Max());               //save signal power data
                    partialNoisePowers.Add(channelsNoisePowerValues.Max());         //save noise power data

                }
                //MEASUREMETS ARE DONE at this point - process all data below;

                //Remove (most probably) false zero values of noise level
                if(numberOfMeasures > 50)
                {
                    partialNoisePowers.RemoveAt(partialNoisePowers.LastIndexOf(partialNoisePowers.Min()));
                    partialNoisePowers.RemoveAt(partialNoisePowers.LastIndexOf(partialNoisePowers.Min()));
                } else if(numberOfMeasures >= 10)
                {
                    partialNoisePowers.RemoveAt(partialNoisePowers.LastIndexOf(partialNoisePowers.Min()));
                }

                
                List<double> normalisedPartialFramesPowers = new List<double>();            //list that stores all frames signal powers as normalised double values (not absolutes)
                long noisePowerLong = partialNoisePowers.Min();                             //select the real value of noise level (absolute value)

                //convert all signal powers (within frames) and normalise them;
                for (short i=0; i < numberOfMeasures; i++)
                    normalisedPartialFramesPowers.Add(partialFramesPowers[i] * 1.0 / maxFramePower);

                //if noise level seems to be 0 - change it so it is 100x less than signal level
                //(it means SMOLS won't treat single-digit samples' values as a "true"/relevant signal);
                //here inside condition below, value of noise level is set globally
                if (noisePowerLong != 0)
                    _normalisedNoiseLevel = Math.Sqrt(noisePowerLong * 1.0 / maxNoisePower);
                else
                    _normalisedNoiseLevel = Math.Sqrt(normalisedPartialFramesPowers.Sum() / numberOfMeasures)/100;

                _normalisedSignalLevel = Math.Sqrt(normalisedPartialFramesPowers.Sum() / numberOfMeasures);         //value of signal level is now set globally

            }
        }

        /// <summary>
        /// Method returns total time of open AV file as a string.
        /// </summary>
        /// <returns>String containing total AV play time</returns>
        public string getPlayTime()
        {
            int hours = (int)(_totalTimeMiliseconds / 3600000);
            short minutes = (short)((_totalTimeMiliseconds / 60000) - (hours * 60));
            short seconds = (short)((_totalTimeMiliseconds / 1000) - (hours * 3600) - (minutes * 60));

            string hoursString = hours.ToString();
            if (hoursString.Length == 1)
            {
                hoursString = "0" + hoursString;

            }
            else if (hoursString.Length == 0)
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

        /// <summary>
        /// Returns calculated normalised signal level.
        /// </summary>
        /// <returns>Normalised signal level (double min=0, max=1)</returns>
        public double getSignalLevel()
        {
            return _normalisedSignalLevel;
        }

        /// <summary>
        /// Returns calculated normalised noise level.
        /// </summary>
        /// <returns>Normalised noise level (double min=0, max=1)</returns>
        public double getNoiseLevel()
        {
            return _normalisedNoiseLevel;
        }

        /// <summary>
        /// Returns file name string (name, not full path).
        /// </summary>
        /// <returns>Name file string</returns>
        public string getFileName()
        {
            return _fileName;
        }

        /// <summary>
        /// Returns sample rate as an integer in Hz.
        /// </summary>
        /// <returns>Sample rate in Hz</returns>
        public int getSampleRate()
        {
            return _sampleRate;
        }

        /// <summary>
        /// Returns total number of available audio samples as ulong value.
        /// </summary>
        /// <returns>Total samples number (ulong)</returns>
        public ulong getTotalSamplesNumber()
        {
            return _totalSamplesNumber;
        }

        /// <summary>
        /// Returns total number of available audio channels.
        /// </summary>
        /// <returns>Audio channels number</returns>
        public short getNumberOfChannels()
        {
            return _numberOfChannels;
        }


    }
}
