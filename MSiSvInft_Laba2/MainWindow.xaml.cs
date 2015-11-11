using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace MSiSvInft_Laba2
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void CloseApp_Click(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Multiselect = false,
                Filter = "ADA code (*.ada, *.adb)|*.ada;*.adb|All files (*.*)|*.*",
                FilterIndex = 1,
                InitialDirectory = @"E:\Programs\ADA\1\",
                Title = "Open code"
            };

            if (ofd.ShowDialog(WMain) != true) return;

            TbFilePath.Text = ofd.FileNames[0];

            var sourceStream = new StreamReader(ofd.OpenFile(), Encoding.Default);

            TbFileText.Text = sourceStream.ReadToEnd();
            sourceStream.Close();
        }

        private void Analize_Click(object sender, RoutedEventArgs e)
        {
            var analiz = new Analizator(TbFileText.Text);
            TbResult.Text = analiz.Analiz();
        }
    }
}
