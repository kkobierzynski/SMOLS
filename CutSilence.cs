﻿using System;
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
        int[,,] data = new int[2, 2, 3] { { { 1, 2, 3 }, { 4, 5, 6 } },             //WARTOŚĆ TYMCZASOWA TESTOWA
                                       { { 7, 8, 9 }, { 10, 11, 12 } } };
        int buffer_counter = 0;
        int release_counter = 0;
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
            double attack_buffer = Math.Round(sample_rate * (0.05 + buffer_slider * (5 / 7)));        //Determines the number of samples to be checked when a sample below the threshold is detected. Prevents low value samples from being cut from a non-silent signal
            double release_buffer = Math.Round(sample_rate * (0.02 + buffer_slider * (2 / 7)));
            double treshold_slider = mainWindow.Threshold_slider.Value;                               // w przyszłośći ogarnąć poprawne zakresy w zależności od wartości otrzymanych danych
            double threshold = 36 + Math.Round(treshold_slider);                                      // w przyszłośći ogarnąć poprawne zakresy w zależności od wartości otrzymanych danych


            for (int i = 0; i<num_arrays; i++)
            {
                for (int j = 0; j<num_samples; j++)
                {
                    
                    if (data[i, 0, j] < threshold && data[i, 1, j] < threshold && checking_threshold)     //First Part - searching for a sample whose value is below the threshold
                    {
                        checking_threshold = false;
                        silence_verification = true;
                    }
                    else                            //Save samples if the value is above the threshold
                    {
                        saving_samples(i, j); 
                    }

                    if (silence_verification)               //Second part - checking if silence is detected
                    {
                        saving_samples(i, j);
                        buffer_counter++;

                        if (data[i, 0, j] > threshold && data[i, 1, j] > threshold)     //If a sample with a value exceeding the threshold is found, go back to the first part
                        {
                            checking_threshold = true;
                            silence_verification = false;
                            buffer_counter = 0;
                        }
                        if (buffer_counter == attack_buffer && silence_verification)       //If all samples in the buffer were below the threshold go to third part. Silence verification is
                        {                                                                  //needed to rule out situations in which the last attempt in the buffer exceeds the threshold. Eliminate a situation where both ifs are true 
                            silence_cutting = true;
                            silence_verification = false;
                            buffer_counter = 0;
                        }

                    }
                    if (silence_cutting)                                        //Third part - cutting silence from the signal.
                    {
                        buffer_first_channel.Add(data[i, 0, j]);                //Adding sample values ​​to the buffer that is responsible for release time
                        buffer_second_channel.Add(data[i, 1, j]);
                        
                        if (release_counter < release_buffer)                   //Defining size of buffer
                        {
                            release_counter++;
                        }
                        else
                        {
                            buffer_first_channel.RemoveRange(0, 1);             //Removing the oldest samples
                            buffer_second_channel.RemoveRange(0, 1);
                        }

                        if (data[i, 0, j] > threshold || data[i, 1, j] > threshold)     //The sample value has exceeded the threshold //uwazac bo czasami nie zdazy sie zapelnic cały bufor
                        {
                            // MIESZANIE PRÓBEK BY WYELIMINOWAĆ TRZASKI
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
                //int[] array = samples_first_channel.ToArray(); //ta tablica moze być pusta (ciagle cisza) moze mieć rozmiar wielkości i albo też o wiele większa(wrzucenie release time)
                //samples_first_channel.Clear();
                //TRZEBA BEDZIE CZYŚCIĆ BUFORY (buffer_first_channel.Clear(); buffer_second_channel.Clear();) NA WSZELKI WYPADEK JAKBY DZIAŁANIE PROGRAMU ZAKOŃCZYŁA SIE W POŁOWIE ORAZ BY PRZYGOTOWAĆ PROGRAM DO CZYSZCZENIA KOLEJNEGO PLIKU!!!!!!!! PRAWDOPODOBNIE NA WYJSCIU FORA
                //BĘDZIE TRZEBA DODAĆ PRZYPISYWANIE LISTY DO DANEJ TABLICY ORAZ WYKONAĆ CZYSZCZENIE LISTY TAK BY KOLEJNE 10000 PRÓBEK ZAPISYWAŁO SIE OD NOWA (OSZCZĘDNOŚC PAMIĘCI)
                //ZASTANOWAIĆ SIĘ CZY NIE BĘDZIE KONIECZNE WRZUCENIE NA KONIEC BOOLEANÓW TAK BY STATUSY DZIAŁAŁY POPRAWNIE NAWET JAK UŻYTKOWNIK NIE WYJDZIE Z PROGRAMU
            }

        }

        private void saving_samples(int i, int j)
        {
            samples_first_channel.Add(data[i,0,j]);
            samples_second_channel.Add(data[i, 1, j]);
        }

    }
}
