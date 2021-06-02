using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace SMOLS2000
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        OpenFile audiofile;
        CutSilence cut;
        DrawWaveform drawnWaveform;


        public MainWindow()
        {
            InitializeComponent();

            drawnWaveform = new DrawWaveform(this);

        }

        private void openFileButton_Click(object sender, RoutedEventArgs e)
        {
            audiofile = new OpenFile(this);

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            cut = new CutSilence(this);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void Slider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }
    }
}
