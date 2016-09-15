using System;
using System.Collections.Generic;
using System.IO;
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
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;

namespace FaceRecognize
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IFaceServiceClient _faceServiceClient;
        private readonly string _pathImageDir = Environment.CurrentDirectory + @"\..\..\Images\";
        private readonly string[] _images;

        public MainWindow()
        {
            _faceServiceClient = new FaceServiceClient("77192b6052d246a68a97781453e24be5");
            //_images = new[] { @"img9.png", @"img11.png", @"img12.png", @"img24.png", @"img26.png", @"img27.png", @"img32.png", @"img33.png", @"img35.png", @"img36.png" };
            _images = new[] { @"img11.jpeg"};//, @"img11.png", @"img24.png", @"img36.png" };
            InitializeComponent();
        }

        private async Task Training()
        {            
            //var t = _faceServiceClient.GetPersonGroupAsync()
            var idGeneralHero = "abracadabra";
            try
            {
                _faceServiceClient.CreatePersonGroupAsync(idGeneralHero, "General Heroes").Wait();
            }
            catch (Exception ex)
            {
                var str = ex.Message;
            }

            CreatePersonResult person = await _faceServiceClient.CreatePersonAsync(idGeneralHero, "Qwerty");

            foreach (var cur in _images)
            {
                using (Stream s = File.OpenRead(_pathImageDir + cur))
                {
                    // Detect faces in the image and add to Qwerty
                    await _faceServiceClient.AddPersonFaceAsync(idGeneralHero, person.PersonId, s);
                }
            }
            await _faceServiceClient.TrainPersonGroupAsync(idGeneralHero);
            TrainingStatus trainingStatus = null;
            while (true)
            {
                trainingStatus = await _faceServiceClient.GetPersonGroupTrainingStatusAsync(idGeneralHero);
                if (trainingStatus.Status.ToString() != "running")
                {
                    break;
                }

                await Task.Delay(1000);
            }

            Console.WriteLine("END TRAINING");
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            //Training().Wait();
            Title = "Detecting...";
            FaceRectangle[] faceRects = await UploadAndDetectFaces(_pathImageDir + @"img13.jpeg");
            Title = string.Format($"Detection Finished. {faceRects.Length} face(s) detected");

            if (faceRects.Length > 0)
            {
                BitmapImage bitmapSource = new BitmapImage();
                bitmapSource.BeginInit();
                bitmapSource.StreamSource = new MemoryStream(File.ReadAllBytes(_pathImageDir + @"img11.jpeg"));
                bitmapSource.EndInit();
                DrawingVisual visual = new DrawingVisual();
                DrawingContext drawingContext = visual.RenderOpen();
                drawingContext.DrawImage(bitmapSource,
                    new Rect(0, 0, bitmapSource.Width, bitmapSource.Height));
                double dpi = bitmapSource.DpiX;
                double resizeFactor = 96 / dpi;

                foreach (var faceRect in faceRects)
                {
                    drawingContext.DrawRectangle(
                        Brushes.Transparent,
                        new Pen(Brushes.Red, 2),
                        new Rect(
                            faceRect.Left * resizeFactor,
                            faceRect.Top * resizeFactor,
                            faceRect.Width * resizeFactor,
                            faceRect.Height * resizeFactor
                            )
                    );
                }

                drawingContext.Close();
                RenderTargetBitmap faceWithRectBitmap = new RenderTargetBitmap(
                    (int)(bitmapSource.PixelWidth * resizeFactor),
                    (int)(bitmapSource.PixelHeight * resizeFactor),
                    96,
                    96,
                    PixelFormats.Pbgra32);

                faceWithRectBitmap.Render(visual);
                FacePhoto.Source = faceWithRectBitmap;
            }
        }

        private async Task<FaceRectangle[]> UploadAndDetectFaces(string imageFilePath)
        {
            try
            {
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    var faces = await _faceServiceClient.DetectAsync(imageFileStream);
                    var faceRects = faces.Select(face => face.FaceRectangle);
                    return faceRects.ToArray();
                }
            }
            catch (Exception)
            {
                return new FaceRectangle[0];
            }
        }

    }
}
