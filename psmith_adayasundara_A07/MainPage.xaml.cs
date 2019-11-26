using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace psmith_adayasundara_A07
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        List<Bitmap> PictureList = new List<Bitmap>();
        private TransformGroup transforms;
        private MatrixTransform previousTransform;
        private CompositeTransform deltaTransform;
        private bool forceManipulationsToEnd;

        //Opening file variables
        string m_fileToken;
        StorageItemAccessList m_futureAccess = StorageApplicationPermissions.FutureAccessList;
        uint m_displayWidthNonScaled;
        uint m_displayHeightNonScaled;
        double m_scaleFactor;

        //Blocks
        BitmapImage[,] images = new BitmapImage[4, 4];
        BitmapImage[,] winImage = new BitmapImage[4, 4];

        public MainPage()
        {
            this.InitializeComponent();
            InitManipulationTransforms();

            // Register for the various manipulation events that will occur on the shape
            manipulateMe.ManipulationStarted += new ManipulationStartedEventHandler(ManipulateMe_ManipulationStarted);
            manipulateMe.ManipulationDelta += new ManipulationDeltaEventHandler(ManipulateMe_ManipulationDelta);
            manipulateMe.ManipulationCompleted += new ManipulationCompletedEventHandler(ManipulateMe_ManipulationCompleted);
            manipulateMe.ManipulationInertiaStarting += new ManipulationInertiaStartingEventHandler(ManipulateMe_ManipulationInertiaStarting);

            manipulateMe.ManipulationMode =
                ManipulationModes.TranslateX |
                ManipulationModes.TranslateY |
                ManipulationModes.Rotate |
                ManipulationModes.TranslateInertia |
                ManipulationModes.RotateInertia;
        }

        private void InitManipulationTransforms()
        {
            transforms = new TransformGroup();
            previousTransform = new MatrixTransform() { Matrix = Matrix.Identity };
            deltaTransform = new CompositeTransform();

            transforms.Children.Add(previousTransform);
            transforms.Children.Add(deltaTransform);

            // Set the render transform on the rect
            manipulateMe.RenderTransform = transforms;
        }

        // When a manipulation begins, change the color of the object to reflect
        // that a manipulation is in progress
        void ManipulateMe_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            forceManipulationsToEnd = false;
            manipulateMe.Background = new SolidColorBrush(Windows.UI.Colors.DeepSkyBlue);
        }

        // Process the change resulting from a manipulation
        void ManipulateMe_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // If the reset button has been pressed, mark the manipulation as completed
            if (forceManipulationsToEnd)
            {
                e.Complete();
                return;
            }

            previousTransform.Matrix = transforms.Value;

            // Get center point for rotation
            Windows.Foundation.Point center = previousTransform.TransformPoint(new Windows.Foundation.Point(e.Position.X, e.Position.Y));
            //Point center = previousTransform.TransformPoint(new Point((int)e.Position.X,(int)e.Position.Y));
            deltaTransform.CenterX = center.X;
            deltaTransform.CenterY = center.Y;

            // Look at the Delta property of the ManipulationDeltaRoutedEventArgs to retrieve
            // the rotation, scale, X, and Y changes
            deltaTransform.Rotation = e.Delta.Rotation;
            deltaTransform.TranslateX = e.Delta.Translation.X;
            deltaTransform.TranslateY = e.Delta.Translation.Y;
        }

        // When a manipulation that's a result of inertia begins, change the color of the
        // the object to reflect that inertia has taken over
        void ManipulateMe_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
        {
            manipulateMe.Background = new SolidColorBrush(Windows.UI.Colors.RoyalBlue);
        }

        // When a manipulation has finished, reset the color of the object
        void ManipulateMe_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            manipulateMe.Background = new SolidColorBrush(Windows.UI.Colors.LightGray);
        }

        //Open an image
        private async void btnImage_Click(object sender, RoutedEventArgs e)
        {

            FileOpenPicker picker = new FileOpenPicker();
            Helpers.FillDecoderExtensions(picker.FileTypeFilter);
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

            var ifile = await picker.PickSingleFileAsync();
            if (ifile != null)
            {
                await LoadFileAsync(ifile);
            }

            BitmapImage image = new BitmapImage(new Uri(ifile.Path));
        }

        /// <summary>
        /// Load an image from a file and display some basic imaging properties.
        /// </summary>
        /// <param name="file">The image to load.</param>
        private async Task LoadFileAsync(StorageFile file)
        {
            try
            {
                await DisplayImageFileAsync(file);
            }
            catch (Exception err)
            {
                ResetSessionState();
            }
        }
        /// <summary>
        /// This function is called when the user clicks the Open button,
        /// and when they save (to refresh the image state).
        /// </summary>
        private async Task DisplayImageFileAsync(StorageFile file)
        {

            // Request persisted access permissions to the file the user selected.
            // This allows the app to directly load the file in the future without relying on a
            // broker such as the file picker.
            m_fileToken = m_futureAccess.Add(file);

            // Display the image in the UI.
            BitmapImage src = new BitmapImage();
            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
            {
                await src.SetSourceAsync(stream);
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                PlaceImage(decoder);
            }

            PreviewImage.Source = src;
            AutomationProperties.SetName(PreviewImage, file.Name);

            // Use BitmapDecoder to attempt to read EXIF orientation and image dimensions.
            await GetImageInformationAsync(file);

        }

        private async void PlaceImage(BitmapDecoder decoder)
        {
            var windowHeight = rectangle1.ActualHeight;
            var windowWidth = rectangle1.ActualWidth;
            var imageHeight = decoder.PixelHeight / 4;
            var imageWidth = decoder.PixelHeight / 4;
            var blocks = new BitmapImage[4, 4];
            for (int i = 0; i < 4; i++)
            {
                for(int j =0; j<4; j++)
                {
                    if( i== 3 && j==3)
                    {
                        images[i, j] = null;
                        continue;
                    }
                    InMemoryRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream(); //Loading images to encoder
                    BitmapEncoder encoder = await BitmapEncoder.CreateForTranscodingAsync(randomAccessStream, decoder); //Bitmap encoder initiate
                    BitmapBounds bounds = new BitmapBounds(); //Used to transform the encoder to specify which part of the image will be used
                    bounds.Height = imageHeight;
                    bounds.Width = imageWidth;
                    bounds.X = 0 + imageWidth * (uint)i;
                    bounds.Y = 0 + imageHeight * (uint)j;
                    encoder.BitmapTransform.Bounds = bounds; //To tell the encoder how to the cutting will be done
                    try
                    {
                        await encoder.FlushAsync(); //Actual cutting
                    }
                    catch(Exception ex)
                    {
                        string s = ex.ToString();
                    }

                    BitmapImage bitImage = new BitmapImage(); //AnImage to be displayed
                    bitImage.SetSource(randomAccessStream);
                    blocks[i, j] = bitImage; //To save the Bitmap Image
                    images[i, j] = blocks[i, j]; //Source of the images to be stored
                }
            }
            for (int i = 0; i < 4; i++)
            {
                for(int j = 0; j<4;j++)
                { 
                    winImage[i, j] = images[i, j];
                }
            }

            //Randomize the blocks to insert into grid
            //Column 0
            img00.Source = blocks[0, 0];
            img01.Source = blocks[0, 1];
            img02.Source = blocks[0, 2];
            img03.Source = blocks[0, 3];

            //Column 1
            img10.Source = blocks[1, 0];
            img11.Source = blocks[1, 1];
            img12.Source = blocks[1, 2];
            img13.Source = blocks[1, 3];

            //Column 2
            img20.Source = blocks[2, 0];
            img21.Source = blocks[2, 1];
            img22.Source = blocks[2, 2];
            img23.Source = blocks[2, 3];

            //Column 3
            img30.Source = blocks[3, 0];
            img31.Source = blocks[3, 1];
            img32.Source = blocks[3, 2];
            img33.Source = blocks[3, 3];

        }

        /// <summary>
        /// Asynchronously attempts to get the oriented dimensions and EXIF orientation from the image file.
        /// Sets member variables instead of returning a value with the Task.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private async Task GetImageInformationAsync(StorageFile file)
        {
            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                // The orientedPixelWidth and Height members provide the image dimensions
                // reflecting any EXIF orientation.
                m_displayHeightNonScaled = decoder.OrientedPixelHeight;
                m_displayWidthNonScaled = decoder.OrientedPixelWidth;
            }
        }

        /// <summary>
        /// Clears all of the state that is stored in memory and in the UI.
        /// </summary>
        private void ResetSessionState()
        {
            m_fileToken = null;
            m_displayHeightNonScaled = 0;
            m_displayWidthNonScaled = 0;
            m_scaleFactor = 1;

            PreviewImage.Source = null;
            //m_transform.CenterX = ImageViewbox.Width / 2;
            //m_transform.CenterY = ImageViewbox.Height / 2;
            //ImageViewbox.RenderTransform = m_transform;
        }


    }
}
