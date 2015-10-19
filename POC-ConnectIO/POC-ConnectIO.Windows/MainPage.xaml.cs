﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.UI.Xaml.Shapes;
using Windows.UI.Input;
using Windows.UI;

using ConnectIO.lib;
using Windows.Networking.Connectivity;
using POC_ConnectIO.lib;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.Media.Capture;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Media.MediaProperties;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.Storage.Pickers;
using System.Xml.Linq;


// Pour en savoir plus sur le modèle d'élément Page vierge, consultez la page http://go.microsoft.com/fwlink/?LinkId=234238

namespace POC_ConnectIO
{
  
    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        public static CanvasInteraction canvasInteraction;

        public static Point lastPoint;
        public static Boolean isDrawing;
        public static ConnectIO.ConnectIO cio;

        public void PhotoTaken()
        {

        }

        public MainPage()
        {

            Application.Current.DebugSettings.EnableFrameRateCounter = false;

            this.InitializeComponent();
            canvasInteraction = new CanvasInteraction(Canvas);

            isDrawing = false;

            // Config the connect.io instance
            cio = ConnectIOImp.create()
                .connectTo("bastienbaret.com:8080")
                .withUser("TestApi_Windows");

            cio.on("clear", async (o) =>
            {

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {

                    canvasInteraction.Clear();

                });

            });

                cio.on("image", async (o) =>
            {

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Event e = (Event)o;
                    String str = e.get<String>("img");
                    Byte[] imgBytes = Convert.FromBase64String(str.Split(',')[1]);
                    Size size = new Size(e.get<double>("w"), e.get<double>("h"));
                    Point position = new Point(e.get<double>("x"), e.get<double>("y"));

                    if(size.Width > 0 && size.Height > 0)
                    {
                        canvasInteraction.DrawImage(position, size, imgBytes);
                    }       

                });

            });

            cio.on("line", async (o) =>
            {

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Event e = (Event)o;
                    Point fromPoint = new Point(e.get<double>("fromX") * Canvas.Width, e.get<double>("fromY") * Canvas.Height);
                    Point toPoint = new Point(e.get<double>("toX") * Canvas.Width, e.get<double>("toY") * Canvas.Height);
                    canvasInteraction.DrawLine(fromPoint, toPoint, e.get<String>("color"));
                });

            });

            cio.onUserInGroupChanged(async (o) =>
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                 {

                     List<String> e = (List<String>)o;
                     String users = "Utilisateurs : ";
                     foreach (String user in e)
                     {
                         users += user + ", ";
                     }
                     TE_Users.Text = users.Substring(0, users.Length - 2);

                 });

            });

            // Connect to the server and join the "abcd" room
            cio.connect((Object o) => {
                //debug.Text = "e";
                cio.joinGroup("abcd");
            });



        }

        public void pointerPressed(object sender, PointerRoutedEventArgs e)
        {
            isDrawing = true;
            lastPoint = e.GetCurrentPoint(Canvas).Position;

        }

        public void pointerMoved(object sender, PointerRoutedEventArgs e)
        {
            // If we are drawing and we had the focus
            if(e.Pointer.IsInContact && isDrawing)
            {

                // Gte the current point
                Point currentPoint = e.GetCurrentPoint(Canvas).Position;

                // Draw line
                canvasInteraction.DrawLine(lastPoint, currentPoint, Colors.Red);

                // Send line object
                Object lineObj = new
                {
                    fromX = lastPoint.X / Canvas.Width,
                    fromY = lastPoint.Y / Canvas.Height,
                    toX = currentPoint.X / Canvas.Width,
                    toY = currentPoint.Y / Canvas.Height,
                    color = "#FF0000"
                };

                cio.send("line", lineObj, false);


                // Update last point
                lastPoint = currentPoint;
            }
        }

        public void pointerReleased(object sender, PointerRoutedEventArgs p)
        {
            isDrawing = false;
        }

        public void pointerExited(object sender, PointerRoutedEventArgs p)
        {
            isDrawing = false;
        }

        private void appSizeChanged(object sender, SizeChangedEventArgs e)
        {
            //MessageDialog dlg = new MessageDialog(Window.Current.Bounds.Height.ToString()); dlg.ShowAsync();
            Canvas.SetTop(Canvas, 50);

            Canvas.Height = e.NewSize.Height - 50;
            Canvas.Width = e.NewSize.Width;
        }

        private void ButtonClearClicked(object sender, RoutedEventArgs e)
        {
            cio.send("clear", null, true);
        }

        private async void ButtonPhotoClicked(object sender, RoutedEventArgs e)
        {

            /*var l_camera = new CameraCaptureUI();
            l_camera.PhotoSettings.MaxResolution = CameraCaptureUIMaxPhotoResolution.HighestAvailable;
            l_camera.PhotoSettings.CroppedAspectRatio = new Size(3, 4);
            var photo = l_camera.CaptureFileAsync(CameraCaptureUIMode.Photo);
            photo.Completed();*/

            var _MediaCapture = new MediaCapture();
            await _MediaCapture.InitializeAsync();
            //Cap1.Source = _MediaCapture;
            //await _MediaCapture.StartPreviewAsync();

            var _Name = Guid.NewGuid().ToString();
            var _Opt = CreationCollisionOption.ReplaceExisting;
            var _File = await ApplicationData.Current.LocalFolder
                .CreateFileAsync(_Name, _Opt);

            var _ImageFormat = ImageEncodingProperties.CreatePng();
            await _MediaCapture
                .CapturePhotoToStorageFileAsync(_ImageFormat, _File);
            var _BitmapImage = new BitmapImage(new Uri(_File.Path));

            //_BitmapImage.PixelHeight = 500;
            //_BitmapImage.PixelHeight = 500;

            canvasInteraction.DrawImage(new Point(0.1, 0.1), new Size(0.1, 0.1), _BitmapImage);



            /*var fo = new FileOpenPicker();
            fo.FileTypeFilter.Add(".png");

            var file = await fo.PickSingleFileAsync();        
            var fStream = await file.OpenAsync(FileAccessMode.Read);

            var reader = new DataReader(fStream.GetInputStreamAt(0));
            var bytes = new byte[fStream.Size];
            await reader.LoadAsync((uint)fStream.Size);
            reader.ReadBytes(bytes);

            canvasInteraction.DrawImage(new Point(0.5, 0.5), new Size(0.5, 0.5), bytes);*/

            //Img1.Source = _BitmapImage;


        }
    }

}
