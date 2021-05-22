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

        public MainWindow()
        {
            InitializeComponent();
        }

        private void openFileButton_Click(object sender, RoutedEventArgs e)
        {
            audiofile = new OpenFile();
            int zmienna = audiofile.testowanko();
            Console.WriteLine(zmienna);
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            cut = new CutSilence(); 
        }
    }
}
