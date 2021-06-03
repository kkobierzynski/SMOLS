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
        static int sample_rate = 48000;                                             // WARTOSĆ TYMCZASOWA, W PRZYSZŁOŚCI ZOSTANIE OTRZTYMANA WARTOŚĆ ZMIENNA
        int num_samples = 3;
        int num_arrays = 2;                                                         //WARTOSĆ TYMCZASOWA, ZMIENIĆ NA LIST.SIZE ALBO ZMIENNĄ DOSTARCZONĄ OD MICHAŁA
        int[,] data = new int[2, 2, 3] { { { 1, 2, 3 }, { 4, 5, 6 } },             //WARTOŚĆ TYMCZASOWA TESTOWA
                                       { { 7, 8, 9 }, { 10, 11, 12 } } };
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
        
        public CutSilence(MainWindow mainWindow)
        {
            double buffer_slider = mainWindow.A_r_time_slider.Value;                                  //Value from slider Attack/Release time
            double attack_buffer = Math.Round(sample_rate * (0.05 + buffer_slider * (5 / 7.0)));      //Determines the number of samples to be checked when a sample below the threshold is detected. Prevents low value samples from being cut from a non-silent signal
            int release_buffer = (int)Math.Round(sample_rate * (0.02 + buffer_slider * (2 / 7.0)));
            double treshold_slider = mainWindow.Threshold_slider.Value;                               // w przyszłośći ogarnąć poprawne zakresy w zależności od wartości otrzymanych danych
            int threshold = 36 + (int)Math.Round(treshold_slider);                                      // w przyszłośći ogarnąć poprawne zakresy w zależności od wartości otrzymanych danych
            double attack_smooth_silencing = attack_buffer - Math.Round(sample_rate * 0.01);
            double release_smooth_silencing = Math.Round(sample_rate * 0.01);

                for (int i = 0; i < num_samples; i++)
                {
                    if (i == progress_counter * (num_samples / 200))                      //part of the program responsible for progress bar loading.
                    {
                        mainWindow.progress_bar.Value = progress_counter;
                        progress_counter = progress_counter++;
                    }
                    
                    if (data[i, 0] < threshold && data[i, 1] < threshold && checking_threshold)     //First Part - searching for a sample whose value is below the threshold
                    {
                        checking_threshold = false;
                        silence_verification = true;
                    }
                    else                            //Save samples if the value is above the threshold
                    {
                        saving_samples(i);
                    }

                    if (silence_verification)               //Second part - checking if silence is detected
                    {
                        if (buffer_counter > attack_smooth_silencing && buffer_counter < attack_buffer)   //Part of the program responsible for smooth silencing of samples at the end of buffer. The silencing time is 10ms
                        {
                            buffer_counter++;
                            smooth_silencing_counter++;
                            samples_first_channel.Add((int)(data[i, 0] * ((attack_buffer - attack_smooth_silencing - smooth_silencing_counter) / (attack_buffer - attack_smooth_silencing))));   //adding samples with progressively smaller values ​​to achieve smooth muting
                            samples_second_channel.Add((int)(data[i, 1] * ((attack_buffer - attack_smooth_silencing - smooth_silencing_counter) / (attack_buffer - attack_smooth_silencing))));
                        }
                        else
                        {
                            saving_samples(i);
                            buffer_counter++;
                        }

                        if (data[i, 0] > threshold && data[i, 1] > threshold)     //If a sample with a value exceeding the threshold is found, go back to the first part
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
                        buffer_first_channel.Add(data[i, 0]);                //Adding sample values ​​to the buffer that is responsible for release time
                        buffer_second_channel.Add(data[i, 1]);
                        
                        if (release_counter < release_buffer)                   //Defining size of buffer
                        {
                            release_counter++;
                        }
                        else
                        {
                            buffer_first_channel.RemoveRange(0, 1);             //Removing the oldest samples
                            buffer_second_channel.RemoveRange(0, 1);
                        }

                        if (data[i, 0] > threshold || data[i, 1] > threshold)     //The sample value has exceeded the threshold //uwazac bo czasami nie zdazy sie zapelnic cały bufor
                        {
                            for (int k = 0; k < release_smooth_silencing; k++)
                            {
                                buffer_first_channel[k] = (int)(buffer_first_channel[k] * (k / release_smooth_silencing));      //adding samples with progressively larger values ​​to achieve smooth transition
                                buffer_second_channel[k] = (int)(buffer_second_channel[k] * (k / release_smooth_silencing));
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

        private void saving_samples(int i)
        {
            samples_first_channel.Add(data[i,0]);
            samples_second_channel.Add(data[i, 1]);
        }

    }
}
