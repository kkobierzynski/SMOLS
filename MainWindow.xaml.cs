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
    /// Interaction logic for the MainWindow.xaml class.
    /// </summary>
    public partial class MainWindow : Window
    {
        private OpenFile audioFile;
        private CutSilence cut;
        private DrawWaveform drawnWaveform;

        /// <summary>
        /// Main window entrance constructor.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            drawnWaveform = new DrawWaveform(this);


            openFileGrid.AllowDrop = true;
            openFileGrid.DragEnter += OpenFileGrid_DragEnter;
            openFileGrid.Drop += OpenFileGrid_Drop;

        }

        /// <summary>
        /// Method used for opening file through drag-drop procedure.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFileGrid_Drop(object sender, DragEventArgs e)
        {
            string[] file = (string[])e.Data.GetData(DataFormats.FileDrop);
            audioFile = new OpenFile(this, file[0]);

        }

        /// <summary>
        /// Method used for a special effect while doing drag-drop procedure.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFileGrid_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Copy;
        }

        /// <summary>
        /// Method for opening AV file through "Open" button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openFileButton_Click(object sender, RoutedEventArgs e)
        {
            audioFile = new OpenFile(this);

        }

        /// <summary>
        /// Method invoking processing and saving procedures (after file is loaded).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            cut = new CutSilence(this, audioFile);
            cut.saving();
        }

        /// <summary>
        /// Method responsible for updating attack/release time coefficient (not implemented yet).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        /// <summary>
        /// Method responsible for updating threshold coefficient (not implemented yet).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Slider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }


        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            
        }

        /// <summary>
        /// Exit button method.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitAppMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
