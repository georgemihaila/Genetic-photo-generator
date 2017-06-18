using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using Utilities;

namespace Genetic_photo_generator
{
    /// <summary>
    /// Main window code
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Initialization

        public MainWindow()
        {
            InitializeComponent();
            App.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }

        #region Loaded events


        private void resizeCheckBox_Loaded(object sender, RoutedEventArgs e)
        {
            resizeCheckBox.Checked += ResizeCheckBox_Checked;
            resizeCheckBox.Unchecked += ResizeCheckBox_Checked;
        }


        private void stopRadioButton1_Loaded(object sender, RoutedEventArgs e)
        {
            stopRadioButton1.Checked += StopRadioButton1_Checked;
            stopRadioButton1.Unchecked += StopRadioButton1_Checked;
        }


        private void accuracyRadioButton_Loaded(object sender, RoutedEventArgs e)
        {
            accuracyRadioButton.Checked += AccuracyRadioButton_Checked;
            accuracyRadioButton.Unchecked += AccuracyRadioButton_Checked;
        }


        private void sourceImage_Loaded(object sender, RoutedEventArgs e)
        {
            Configuration.SourceBitmap = (Bitmap)Bitmap.FromFile(Configuration.Filename);
            sourceImage.Source = Configuration.SourceBitmap.ToImageSource();
        }

        #endregion


        #endregion

        #region UI

        #region Events

        private void ResizeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Configuration.Resize = (bool)resizeCheckBox.IsChecked;
            widthUpDown.IsEnabled = heightUpDown.IsEnabled = Configuration.Resize;
        }

        private void StopRadioButton1_Checked(object sender, RoutedEventArgs e)
        {
            Configuration.StopAfterNumberOfGenerations = (bool)stopRadioButton1.IsChecked;
            generationSizeUpDown.IsEnabled = generationsUpDown.IsEnabled = Configuration.StopAfterNumberOfGenerations;
        }

        private void AccuracyRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            Configuration.StopAtAccuracy = (bool)accuracyRadioButton.IsChecked;
            accuracyUpDown.IsEnabled = Configuration.StopAtAccuracy;
        }

        BackgroundWorker[] backgroundWorker;
        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            startButton.IsEnabled = false;
            stopButton.IsEnabled = true;

            Configuration.NumberOfGenerations = (int)generationsUpDown.Value;
            Configuration.GenerationSize = (int)generationSizeUpDown.Value;

            if ((bool)threadButton1.IsChecked) Configuration.NumberOfThreads = 1;
            else if ((bool)threadButton2.IsChecked) Configuration.NumberOfThreads = 2;
            else if ((bool)threadButton3.IsChecked) Configuration.NumberOfThreads = 4;
            else if ((bool)threadButton4.IsChecked) Configuration.NumberOfThreads = 8;
            else if ((bool)threadButton5.IsChecked) Configuration.NumberOfThreads = 16;

            Configuration.SkipTop3 = (bool)top3rdCheckBox.IsChecked;
            Configuration.SkipPerfects = (bool)perfectsCheckBox.IsChecked;

            Configuration.PerfectMutationChance = Convert.ToInt32(mutationChanceSlider.Value);

            threadCanvas.Children.Clear();
            threadTB1 = new TextBlock[Configuration.NumberOfThreads];
            threadTB2 = new TextBlock[Configuration.NumberOfThreads];
            threadTB3 = new TextBlock[Configuration.NumberOfThreads];
            threadPB = new ProgressBar[Configuration.NumberOfThreads];
            for (int i = 0; i < Configuration.NumberOfThreads; i++)
            {
                threadTB1[i] = new TextBlock();
                threadTB1[i].Text = "Thread #" + (i + 1);
                threadTB1[i].FontWeight = FontWeights.Bold;
                threadCanvas.Children.Add(threadTB1[i]);
                Canvas.SetTop(threadTB1[i], 10 + i * 57);
                Canvas.SetLeft(threadTB1[i], 10);

                threadPB[i] = new ProgressBar();
                threadPB[i].Height = 10;
                threadPB[i].Width = 100;
                threadPB[i].Minimum = 0;
                threadPB[i].Maximum = (double)generationsUpDown.Value;
                threadCanvas.Children.Add(threadPB[i]);
                Canvas.SetTop(threadPB[i], 31 + i * 57);
                Canvas.SetLeft(threadPB[i], 10);

                threadTB2[i] = new TextBlock();
                threadTB2[i].Text = "0%";
                threadCanvas.Children.Add(threadTB2[i]);
                Canvas.SetTop(threadTB2[i], 31 + i * 57);
                Canvas.SetLeft(threadTB2[i], 115);

                threadTB3[i] = new TextBlock();
                threadTB3[i].Text = "Stamina: 0";
                threadCanvas.Children.Add(threadTB3[i]);
                Canvas.SetTop(threadTB3[i], 46 + i * 57);
                Canvas.SetLeft(threadTB3[i], 10);
            }
            threadCanvas.Height = Configuration.NumberOfThreads * 57 + 46;
            backgroundWorker = new BackgroundWorker[Configuration.NumberOfThreads];
            generatedChunks = new Bitmap[Configuration.NumberOfThreads];
            for (int i = 0; i < Configuration.NumberOfThreads; i++)
            {
                generatedChunks[i] = new Bitmap(Configuration.ImageWidth / Configuration.NumberOfThreads, Configuration.ImageHeight);
                generatedChunks[i].FillWith(System.Drawing.Color.White);
            }
            for (int i = 0; i < backgroundWorker.Length; i++)
            {
                backgroundWorker[i] = new BackgroundWorker();
                backgroundWorker[i].WorkerSupportsCancellation = true;
                backgroundWorker[i].RunWorkerCompleted += MainWindow_RunWorkerCompleted;
                backgroundWorker[i].DoWork += MainWindow_DoWork;
                object arg = (Configuration.SourceBitmap).SplitVertically(i + 1, Configuration.NumberOfThreads);
                (arg as Bitmap).Tag = new int[] { i, Configuration.NumberOfThreads };
                backgroundWorker[i].RunWorkerAsync(arg);
            }
        }

        private void MainWindow_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            startButton.IsEnabled = true;
            stopButton.IsEnabled = false;
        }

        private void MainWindow_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        Random random = new Random();
        readonly Dispatcher MainThreadDispatcher = Dispatcher.CurrentDispatcher;

        private void MainWindow_DoWork(object sender, DoWorkEventArgs e)
        {
            Bitmap bitmapToCompare = e.Argument as Bitmap;
            int chunkNo = (bitmapToCompare.Tag as int[])[0];
            int totalChunks = (bitmapToCompare.Tag as int[])[1];
            int width = bitmapToCompare.Width;
            int height = bitmapToCompare.Height;
            GeneticPhoto[] photo = new GeneticPhoto[Configuration.GenerationSize];
            for (int i = 0; i < photo.Length; i++)
            {
                photo[i] = new GeneticPhoto(width, height);
                photo[i].Generate(random.Next());
            }
            GeneticPhoto last = photo[0];
            for (int generation = 1; (generation <= Configuration.NumberOfGenerations) && !backgroundWorker[chunkNo].CancellationPending; generation++)
            {
                photo.Sort(bitmapToCompare);
                if (last != photo[0])
                {
                    last = photo[0];
                    Bitmap b = photo[0].ToBitmap();
                    RecomposeGeneratedImage(b, chunkNo, totalChunks);
                }
                double stamina = photo[0].GetStaminaRelativeTo(bitmapToCompare);
                MainThreadDispatcher.Invoke(() => //Gold
                {
                    ReportProgress(chunkNo, generation, stamina);
                });
                photo.Evolve(random.Next(), bitmapToCompare, Configuration.SkipTop3, Configuration.SkipPerfects, Configuration.PerfectMutationChance);
                for (int i = photo.Length / 1; i < photo.Length; i++)
                    photo[i].Generate(random.Next());
            }
        }
        

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            stopButton.IsEnabled = false;
            foreach (var x in backgroundWorker)
                x.CancelAsync();
        }

        #endregion

        #region Methods

        private void ReportProgress(int threadNo, int progress, double stamina)
        {
            threadTB2[threadNo].Text = (progress * 100) / Configuration.NumberOfGenerations + "%";
            threadPB[threadNo].Value = progress;
            threadTB3[threadNo].Text = ("Stamina: " + stamina).PadRight(15);
        }

        Bitmap[] generatedChunks;
        private void RecomposeGeneratedImage(Bitmap bmp, int chunkNo, int totalChunks)
        {lock (this)
            {
                Bitmap finalBitmap = new Bitmap(bmp.Width * totalChunks, bmp.Height);
                for (int i = 0; i < generatedChunks.Length; i++)
                {
                    for (int y = 0; y < generatedChunks[i].Height; y++)
                    {
                        for (int x = 0; x < generatedChunks[i].Width; x++)
                        {
                            finalBitmap.SetPixel(i * generatedChunks[i].Width + x, y, generatedChunks[i].GetPixel(x, y));
                        }
                    }
                }

                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        finalBitmap.SetPixel(bmp.Width * chunkNo + x, y, bmp.GetPixel(x, y));
                    }
                }

                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        generatedChunks[chunkNo].SetPixel(x, y, bmp.GetPixel(x, y));
                    }
                }
                MainThreadDispatcher.Invoke(() => //Gold
                {
                    generatedImage.Source = finalBitmap.ToImageSource();
                });
            }
        }

        #endregion

        #endregion

        #region Variables

        TextBlock[] threadTB1;
        TextBlock[] threadTB2;
        TextBlock[] threadTB3;
        ProgressBar[] threadPB;

        #region Local classes

        private static class Configuration
        {
            public static int NumberOfThreads = 2;
            public static bool Resize = true;
            public static int ImageWidth = 200;
            public static int ImageHeight = 200;
            public static bool StopAfterNumberOfGenerations = true;
            public static bool StopAtAccuracy = false;
            public static int DesiredAccuracy = 20;
            public static string Filename = "source.jpg";
            public static int NumberOfGenerations = 10;
            public static int GenerationSize = 20;
            public static bool SkipTop3 = false;
            public static bool SkipPerfects = false;
            public static int PerfectMutationChance = 1;
            public static Bitmap SourceBitmap;
        }

        #endregion

        #endregion

        #region Error handling

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            MessageBox.Show(e.Exception.ToString());
        }

        #endregion

        private void sourceImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string[] files = System.IO.Directory.GetFiles(Environment.CurrentDirectory);
                List<string> photos = new List<string>();
                for (int i = 0; i < files.Length; i++)
                    if (files[i].EndsWith(".jpg")) photos.Add(files[i]);
                    else if (files[i].EndsWith(".png")) photos.Add(files[i]);
                Configuration.SourceBitmap = ((Bitmap)Bitmap.FromFile(photos[random.Next(photos.Count)])).ResizeImage(Convert.ToInt32(widthUpDown.Value), Convert.ToInt32(heightUpDown.Value));
                sourceImage.Source = Configuration.SourceBitmap.ToImageSource();
            }
            catch (Exception)
            {
                throw new NotImplementedException("Wrong path bro.");
            }
        }
    }
}
