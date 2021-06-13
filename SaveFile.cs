using FFMpegCore;
using Microsoft.Win32;
using NAudio.Lame;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SMOLS2000
{
    class SaveFile
    {

        OpenFile _openFile;

        List<List<byte>> _samplesBuffer;                       //1 second buffer for saving samples in chunks (great performance advantage)

        private string _filePath;
        private string _fileExtension;
        private bool _isSaving = false;

        WaveFileWriter _audioWriter;
        LameMP3FileWriter _lameWriter;

        short _flushCounter = 0;


        public SaveFile(OpenFile openFile)
        {
            _openFile = openFile;

            _samplesBuffer = new List<List<byte>>();

            for(short i=0; i<_openFile.getNumberOfChannels(); i++)
            {
                _samplesBuffer.Add(new List<byte>());
            }

            //think about default format(s)...

            SaveFileDialog saveFile = new SaveFileDialog();
            //default selected file type - mp3
            saveFile.DefaultExt = ".mp3";
            saveFile.Title = "Save modified audio file";
            saveFile.Filter = "Audio files (*.wav, *.flac, *.mp3, *.m4a, *.ogg)|*.mp3; *.flac; *.wav; *.m4a; *.ogg|All files (*.*)|*.*";

            //change UI, prepare app to be able to receive another file
            if (saveFile.ShowDialog() == true)
            {
                _filePath = saveFile.FileName;
                _fileExtension = _filePath.Substring(_filePath.LastIndexOf('.') + 1);
            }

        }



        public void saveSingleSample(short value, short channel)
        {
            var sampleBytes = BitConverter.GetBytes(value);

            if(channel < _openFile.getNumberOfChannels())
            {
                _samplesBuffer[channel].Add(sampleBytes[0]);
                _samplesBuffer[channel].Add(sampleBytes[1]);
            }
            else
            {
                throw new InvalidOperationException("A non-existent channel has been called for save action.");
            }


            if(_samplesBuffer[_openFile.getNumberOfChannels()-1].Count >= _openFile.getSampleRate()*2)
            {
                bool buffersAreAligned = true;

                for(short i=0; i<_openFile.getNumberOfChannels(); i++)
                {
                    if(_samplesBuffer[i].Count != _openFile.getSampleRate()*2)
                    {
                        buffersAreAligned = false;
                    }
                }

                if (buffersAreAligned)
                {
                    saveAudioChunk();

                    for (short i = (short)(_openFile.getNumberOfChannels()-1); i >=0; i--)
                    {
                        _samplesBuffer.RemoveAt(i);
                    }

                    for (short i = 0; i < _openFile.getNumberOfChannels(); i++)
                    {
                        _samplesBuffer.Add(new List<byte>());
                    }
                }

            }
            
        }

        private void saveAudioChunk()
        {

            List<byte> samplesStream = new List<byte>();

            short localChannelsToBeSaved;

            //we want > 2 channels only if target format supports it; in this "if" statement include all formats supporting > 2 channels;
            if (_fileExtension.Equals("wav", StringComparison.InvariantCultureIgnoreCase))
            {
                localChannelsToBeSaved = _openFile.getNumberOfChannels();
            }
            else
            {
                localChannelsToBeSaved = 2;
            }




            for (int i = 0; i < _openFile.getSampleRate(); i++)
            {
                for (short j = 0; j < localChannelsToBeSaved; j++)
                {
                    samplesStream.Add(_samplesBuffer[j][2 * i]);
                    samplesStream.Add(_samplesBuffer[j][2 * i + 1]);
                }
            }


            if (_fileExtension.Equals("wav", StringComparison.InvariantCultureIgnoreCase))
            {
                saveWavChunk(samplesStream);
            }else if(_fileExtension.Equals("mp3", StringComparison.InvariantCultureIgnoreCase))
            {
                //write mp3 using LAME library
                saveMp3Chunk(samplesStream, localChannelsToBeSaved);
            }
            else
            {
                //if unknown format - use mp3 as default (optimal file size)
                saveMp3Chunk(samplesStream, localChannelsToBeSaved);
            }


        }


        private void saveWavChunk(List<byte> samplesStream)
        {
           
            if (!_isSaving)
            {
                //initiate saving
                _audioWriter = new WaveFileWriter(_filePath, new WaveFormat(_openFile.getSampleRate(), 16, _openFile.getNumberOfChannels()));
                _isSaving = true;
            }

            _audioWriter.Write(samplesStream.ToArray());

            _flushCounter++;
            if (_flushCounter > 60)
            {
                _flushCounter = 0;
                _audioWriter.Flush();
            }
        }


        private void saveMp3Chunk(List<byte> samplesStream, short channels)
        {

            if (!_isSaving)
            {
                //initiate saving
                
                //resampler is a must here! Sampling rates higher than 48k are rejected automatically; #TODO
                _lameWriter = new LameMP3FileWriter(_filePath, new WaveFormat(_openFile.getSampleRate(), 16, channels), LAMEPreset.V0);

                _isSaving = true;
            }

            _lameWriter.Write(samplesStream.ToArray());

        }


        public void Close()
        {
            
            if(_audioWriter != null)
            {
                _audioWriter.Dispose();
                _audioWriter.Close();
            }else if(_lameWriter != null)
            {
                _lameWriter.Flush();
                _lameWriter.Dispose();
                _lameWriter.Close();
            }

            
            _isSaving = false;
            _flushCounter = 0;

            for (short i = (short)(_openFile.getNumberOfChannels()-1); i >=0; i--)
            {
                _samplesBuffer.RemoveAt(i);
            }

            for (short i = 0; i < _openFile.getNumberOfChannels(); i++)
            {
                _samplesBuffer.Add(new List<byte>());
            }


            MessageBox.Show("An audio file has been saved successfully!", "SUCCESS", MessageBoxButton.OK, MessageBoxImage.Information);

        }

    }
}
