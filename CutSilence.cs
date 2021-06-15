﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Data;

namespace SMOLS2000
{
    public class CutSilence
    {
        private int buffer_counter = 0;
        private int release_counter = 0;
        private int smooth_silencing_counter = 0;
        private int progress_counter = 0;


        private List<List<short>> _samples = new List<List<short>>();
        private List<List<short>> _buffer = new List<List<short>>();
        private List<short> _temporarySamplesValues = new List<short>();

        private short _numberOfChannels = 0;


        //List<int> samples_first_channel = new List<int>();                          //A list that containing samples from the first channel
        //List<int> samples_second_channel = new List<int>();                         //A list that containing samples from the second channel
        //List<int> buffer_first_channel = new List<int>();
        //List<int> buffer_second_channel = new List<int>();
        private bool checking_threshold = true;                                             //Boolean variables responsible for the operation of states
        private bool silence_verification = false;
        private bool silence_cutting = false;
        private OpenFile audio_open_file;

        public CutSilence(MainWindow mainWindow, OpenFile audio_open)
        {
            audio_open_file = audio_open;
            _numberOfChannels = audio_open_file.getNumberOfChannels();

            //ensure the lists have adequate number of channels - right dimension
            for (short i = 0; i < _numberOfChannels; i++)
            {
                _samples.Add(new List<short>());
                _buffer.Add(new List<short>());
                _temporarySamplesValues.Add(0);
            }


            double buffer_slider = mainWindow.A_r_time_slider.Value;                                  //Value from slider Attack/Release time
            double attack_buffer = Math.Round(audio_open_file.getSampleRate() * (0.05 + buffer_slider * (5 / 7.0)));      //Determines the number of samples to be checked when a sample below the threshold is detected. Prevents low value samples from being cut from a non-silent signal
            int release_buffer = (int)Math.Round(audio_open_file.getSampleRate() * (0.02 + buffer_slider * (2 / 7.0)));
            double treshold_slider = mainWindow.Threshold_slider.Value;
            int threshold = 500 + (int)Math.Round(treshold_slider);                                      //w zależności od wartości otrzymanych danych
            double attack_smooth_silencing = attack_buffer - Math.Round(audio_open_file.getSampleRate() * 0.01);
            double release_smooth_silencing = Math.Round(audio_open_file.getSampleRate() * 0.01);

            for (int i = 0; i < (int)audio_open_file.getTotalSamplesNumber(); i++)
            {
                if (i == progress_counter * (((int)audio_open_file.getTotalSamplesNumber()) / 200))           //part of the program responsible for progress bar loading.
                {
                    //A workaround for working status bar; Call it AS RARELY AS POSSIBLE as this kind of dispatcher hurts application's performance.
                    //I suggest calling this part of code 50 times during whole convertion process; It will be enough to show the progress and it won't hurt the performance as well.
                    mainWindow.Dispatcher.Invoke((Action)(() => { mainWindow.progress_bar.Value += 1; }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                    progress_counter++;
                }

                bool thresholdContition = true;

                for(short j=0; j<_numberOfChannels; j++)
                {
                    _temporarySamplesValues[j] = audio_open_file.getSampleValue(i, j);
                }

                for(short j=0; j<_temporarySamplesValues.Count(); j++)
                {
                    if (_temporarySamplesValues[j] >= threshold)
                        thresholdContition = false;
                }


                if (thresholdContition && checking_threshold)     //First Part - searching for a sample whose value is below the threshold
                {
                    checking_threshold = false;
                    silence_verification = true;
                }
                if (checking_threshold)                  //Save samples if the value is above the threshold
                {
                    saving_samples(i);
                }

                if (silence_verification)               //Second part - checking if silence is detected
                {
                    if (buffer_counter > attack_smooth_silencing && buffer_counter < attack_buffer)   //Part of the program responsible for smooth silencing of samples at the end of buffer. The silencing time is 10ms
                    {
                        buffer_counter++;
                        smooth_silencing_counter++;
                        //samples_first_channel.Add((int)(audio_open_file.getSampleValue(i, 0) * ((attack_buffer - attack_smooth_silencing - smooth_silencing_counter) / (attack_buffer - attack_smooth_silencing))));   //adding samples with progressively smaller values ​​to achieve smooth muting
                        //samples_second_channel.Add((int)(audio_open_file.getSampleValue(i, 0/*1*/) * ((attack_buffer - attack_smooth_silencing - smooth_silencing_counter) / (attack_buffer - attack_smooth_silencing))));

                        for (short j = 0; j < _numberOfChannels; j++)
                        {
                            _samples[j].Add((short)(audio_open_file.getSampleValue(i, j) * ((attack_buffer - attack_smooth_silencing - smooth_silencing_counter) / (attack_buffer - attack_smooth_silencing))));
                        }

                    }
                    else
                    {
                        saving_samples(i);
                        buffer_counter++;
                    }

                    //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                    //I'm not sure about this - now it means all samples with OR condition; previously it was AND condition...                      //lseurttyuu
                    //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                    if (!thresholdContition)     //If a sample with a value exceeding the threshold is found, go back to the first part
                    {
                        checking_threshold = true;
                        silence_verification = false;
                        smooth_silencing_counter = 0;
                        buffer_counter = 0;
                    }
                    if (buffer_counter == attack_buffer && silence_verification)       //If all samples in the buffer were below the threshold go to third part. Silence verification is
                    {                                                                  //needed to rule out situations in which the last attempt in the buffer exceeds the threshold. Eliminate a situation where both ifs are true 
                        silence_cutting = true;
                        silence_verification = false;
                        buffer_counter = 0;
                        smooth_silencing_counter = 0;
                    }

                }
                if (silence_cutting)                                        //Third part - cutting silence from the signal.
                {

                    for(short j=0; j<_numberOfChannels; j++)
                    {
                        _buffer[j].Add(audio_open_file.getSampleValue(i, j));
                    }


                    //buffer_first_channel.Add(audio_open_file.getSampleValue(i, 0));                //Adding sample values ​​to the buffer that is responsible for release time
                    //buffer_second_channel.Add(audio_open_file.getSampleValue(i, 0/*1*/));

                    if (release_counter < release_buffer)                   //Defining size of buffer
                    {
                        release_counter++;
                    }
                    else
                    {
                        for (short j =0; j<_numberOfChannels; j++)
                        {
                            //perhaps use "RemoveAt" (better performance)? If only 1 sample is removed (as I can see?), you can do it.          //lseurrttyuu
                            _buffer[j].RemoveRange(0, 1);
                        }

                        //buffer_first_channel.RemoveRange(0, 1);             //Removing the oldest samples
                        //buffer_second_channel.RemoveRange(0, 1);
                    }

                    if (!thresholdContition)     //The sample value has exceeded the threshold //uwazac bo czasami nie zdazy sie zapelnic cały bufor
                    {
                        if (release_counter == release_buffer)              //checking if the buffer has been completely full
                        {
                            for (int k = 0; k < release_smooth_silencing; k++)
                            {

                                for(short j=0; j<_numberOfChannels; j++)
                                {
                                    _buffer[j][k] = (short)(_buffer[j][k] * (k / release_smooth_silencing));
                                }


                                //buffer_first_channel[k] = (int)(buffer_first_channel[k] * (k / release_smooth_silencing));      //adding samples with progressively larger values ​​to achieve smooth transition
                                //buffer_second_channel[k] = (int)(buffer_second_channel[k] * (k / release_smooth_silencing));
                            }
                        }

                        for (short j = 0; j < _numberOfChannels; j++)
                        {
                            _samples[j].AddRange(_buffer[j]);
                        }



                        //samples_first_channel.AddRange(buffer_first_channel);       //Adding samples that are included in the release time
                        //samples_second_channel.AddRange(buffer_second_channel);
                        release_counter = 0;                                        //Preparing the counter and buffers for re-operation

                        for(short j=0; j<_numberOfChannels; j++)
                        {
                            _buffer[j].Clear();
                        }


                        //buffer_first_channel.Clear();
                        //buffer_second_channel.Clear();
                        silence_cutting = false;
                        checking_threshold = true;
                    }
                }

            }
            //int[] array = samples_first_channel.ToArray();
            //samples_first_channel.Clear();
            //TRZEBA BEDZIE CZYŚCIĆ BUFORY (buffer_first_channel.Clear(); buffer_second_channel.Clear();) NA WSZELKI WYPADEK JAKBY DZIAŁANIE PROGRAMU ZAKOŃCZYŁA SIE W POŁOWIE ORAZ BY PRZYGOTOWAĆ PROGRAM DO CZYSZCZENIA KOLEJNEGO PLIKU!!!!!!!! PRAWDOPODOBNIE NA WYJSCIU FORA
            //BĘDZIE TRZEBA DODAĆ PRZYPISYWANIE LISTY DO DANEJ TABLICY ORAZ WYKONAĆ CZYSZCZENIE LISTY TAK BY KOLEJNE 10000 PRÓBEK ZAPISYWAŁO SIE OD NOWA (OSZCZĘDNOŚC PAMIĘCI)
            //ZASTANOWAIĆ SIĘ CZY NIE BĘDZIE KONIECZNE WRZUCENIE NA KONIEC BOOLEANÓW TAK BY STATUSY DZIAŁAŁY POPRAWNIE NAWET JAK UŻYTKOWNIK NIE WYJDZIE Z PROGRAMU

        }

        private void saving_samples(int i)          //saving samles to the list. Value of the actual sample is taken from class OpenFile.cs
        {

            for(short j=0;j<_numberOfChannels; j++)
            {
                _samples[j].Add(audio_open_file.getSampleValue(i, j));
            }

            //samples_first_channel.Add(audio_open_file.getSampleValue(i, 0));
            //samples_second_channel.Add(audio_open_file.getSampleValue(i, 0/*1*/));
            //audio_open_file.saveSampleValue(audio_open_file.getSampleValue(i, 0));

        }

        //public temporarily
        public void saving()
        {
            SaveFile saveFile = new SaveFile(audio_open_file);


            for (int i = 0; i < _samples[0].Count; i++)
            {
                //audio_open_file.saveSampleValue((short)samples_first_channel[i]);

                for(short j=0; j< _numberOfChannels; j++)
                {
                    saveFile.saveSingleSample(_samples[j][i], j);
                }

                //saveFile.saveSingleSample((short)samples_first_channel[i], 0);
            }
            //audio_open_file.saveFile();
            saveFile.Close();
        }

    }
}
