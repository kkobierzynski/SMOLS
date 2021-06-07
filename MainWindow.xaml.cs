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
        OpenFile audioFile;
        CutSilence cut;
        DrawWaveform drawnWaveform;

        public MainWindow()
        {
            InitializeComponent();

            drawnWaveform = new DrawWaveform(this);


            openFileGrid.AllowDrop = true;
            openFileGrid.DragEnter += OpenFileGrid_DragEnter;
            openFileGrid.Drop += OpenFileGrid_Drop;




        }

        private void OpenFileGrid_Drop(object sender, DragEventArgs e)
        {
            string[] file = (string[])e.Data.GetData(DataFormats.FileDrop);
            audioFile = new OpenFile(this, file[0]);

        }

        private void OpenFileGrid_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Copy;
        }

        private void openFileButton_Click(object sender, RoutedEventArgs e)
        {
            audioFile = new OpenFile(this);

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            cut = new CutSilence(this, audioFile);
            cut.saving();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void Slider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            
        }

        private void exitAppMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
