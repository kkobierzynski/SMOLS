using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Data;

namespace SMOLS2000
{
    class CutSilence
    {
        int buffer_counter = 0;
        int release_counter = 0;
        int smooth_silencing_counter = 0;
        int progress_counter = 0;
        List<int> samples_first_channel = new List<int>();                          //A list that containing samples from the first channel
        List<int> samples_second_channel = new List<int>();                         //A list that containing samples from the second channel
        List<int> buffer_first_channel = new List<int>();
        List<int> buffer_second_channel = new List<int>();
        bool checking_threshold = true;                                             //Boolean variables responsible for the operation of states
        bool silence_verification = false;
        bool silence_cutting = false;
        private OpenFile audio_open_file;
        
        public CutSilence(MainWindow mainWindow, OpenFile audio_open)
        {
            audio_open_file = audio_open;
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
                    
                    if (audio_open_file.getSampleValue(i, 0) < threshold && audio_open_file.getSampleValue(i, 0/*1*/) < threshold && checking_threshold)     //First Part - searching for a sample whose value is below the threshold
                    {
                        checking_threshold = false;
                        silence_verification = true;
                    }
                    if(checking_threshold)                  //Save samples if the value is above the threshold
                    {
                        saving_samples(i);
                    }

                    if (silence_verification)               //Second part - checking if silence is detected
                    {
                        if (buffer_counter > attack_smooth_silencing && buffer_counter < attack_buffer)   //Part of the program responsible for smooth silencing of samples at the end of buffer. The silencing time is 10ms
                        {
                            buffer_counter++;
                            smooth_silencing_counter++;
                            samples_first_channel.Add((int)(audio_open_file.getSampleValue(i, 0) * ((attack_buffer - attack_smooth_silencing - smooth_silencing_counter) / (attack_buffer - attack_smooth_silencing))));   //adding samples with progressively smaller values ​​to achieve smooth muting
                            samples_second_channel.Add((int)(audio_open_file.getSampleValue(i, 0/*1*/) * ((attack_buffer - attack_smooth_silencing - smooth_silencing_counter) / (attack_buffer - attack_smooth_silencing))));
                        }
                        else
                        {
                            saving_samples(i);
                            buffer_counter++;
                        }

                        if (audio_open_file.getSampleValue(i, 0) > threshold && audio_open_file.getSampleValue(i, 0/*1*/) > threshold)     //If a sample with a value exceeding the threshold is found, go back to the first part
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
                        buffer_first_channel.Add(audio_open_file.getSampleValue(i, 0));                //Adding sample values ​​to the buffer that is responsible for release time
                        buffer_second_channel.Add(audio_open_file.getSampleValue(i, 0/*1*/));
                        
                        if (release_counter < release_buffer)                   //Defining size of buffer
                        {
                            release_counter++;
                        }
                        else
                        {
                            buffer_first_channel.RemoveRange(0, 1);             //Removing the oldest samples
                            buffer_second_channel.RemoveRange(0, 1);
                        }
                        
                        if (audio_open_file.getSampleValue(i, 0) > threshold || audio_open_file.getSampleValue(i, 0/*1*/) > threshold)     //The sample value has exceeded the threshold //uwazac bo czasami nie zdazy sie zapelnic cały bufor
                        {
                            if (release_counter == release_buffer)              //checking if the buffer has been completely full
                            {
                                for (int k = 0; k < release_smooth_silencing; k++)
                                {
                                    buffer_first_channel[k] = (int)(buffer_first_channel[k] * (k / release_smooth_silencing));      //adding samples with progressively larger values ​​to achieve smooth transition
                                    buffer_second_channel[k] = (int)(buffer_second_channel[k] * (k / release_smooth_silencing));
                                }
                            }
                            samples_first_channel.AddRange(buffer_first_channel);       //Adding samples that are included in the release time
                            samples_second_channel.AddRange(buffer_second_channel);
                            release_counter = 0;                                        //Preparing the counter and buffers for re-operation
                            buffer_first_channel.Clear();
                            buffer_second_channel.Clear();
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
            samples_first_channel.Add(audio_open_file.getSampleValue(i,0));
            samples_second_channel.Add(audio_open_file.getSampleValue(i, 0/*1*/));
            //audio_open_file.saveSampleValue(audio_open_file.getSampleValue(i, 0));

        }

        public void saving()
        {
            SaveFile saveFile = new SaveFile(audio_open_file);

            for(int i =0; i < samples_first_channel.Count; i++)
            {
                //audio_open_file.saveSampleValue((short)samples_first_channel[i]);
                saveFile.saveSingleSample((short)samples_first_channel[i], 0);
            }
            //audio_open_file.saveFile();
            saveFile.Close();
        }

    }
}
