using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI.Popups;
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

        //Opening file variables
        string m_fileToken;
        StorageItemAccessList m_futureAccess = StorageApplicationPermissions.FutureAccessList;
        uint m_displayWidthNonScaled;
        uint m_displayHeightNonScaled;
        double m_scaleFactor;

        //Blocks
        BitmapImage[,] images = new BitmapImage[4, 4];
        BitmapImage[,] winImage = new BitmapImage[4, 4];

        //Winning list
        List<BitmapImage> winningList = new List<BitmapImage>();

        //Shuffle List
        List<StackPanel> shuffleList = new List<StackPanel>();
        List<KeyValuePair<int, int>> gridLocation = new List<KeyValuePair<int, int>>() {
                                                    new KeyValuePair<int, int>(0, 0),
                                                    new KeyValuePair<int, int>(0, 1),
                                                    new KeyValuePair<int, int>(0, 2),
                                                    new KeyValuePair<int, int>(0, 3),
                                                    new KeyValuePair<int, int>(1, 0),
                                                    new KeyValuePair<int, int>(1, 1),
                                                    new KeyValuePair<int, int>(1, 2),
                                                    new KeyValuePair<int, int>(1, 3),
                                                    new KeyValuePair<int, int>(2, 0),
                                                    new KeyValuePair<int, int>(2, 1),
                                                    new KeyValuePair<int, int>(2, 2),
                                                    new KeyValuePair<int, int>(2, 3),
                                                    new KeyValuePair<int, int>(3, 0),
                                                    new KeyValuePair<int, int>(3, 1),
                                                    new KeyValuePair<int, int>(3, 2),
                                                    new KeyValuePair<int, int>(3, 3)
                                                };

        //Camera
        private static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");

        //Timer and moves
        Stopwatch timer = new Stopwatch();
        int inMoves = 0;
        public MainPage()
        {
            this.InitializeComponent();
            AddPanelsToList();
        }

        
        private void AddPanelsToList()
        {
            shuffleList.Add(pnl00);
            shuffleList.Add(pnl01);
            shuffleList.Add(pnl02);
            shuffleList.Add(pnl03);

            shuffleList.Add(pnl10);
            shuffleList.Add(pnl11);
            shuffleList.Add(pnl12);
            shuffleList.Add(pnl13);

            shuffleList.Add(pnl20);
            shuffleList.Add(pnl21);
            shuffleList.Add(pnl22);
            shuffleList.Add(pnl23);

            shuffleList.Add(pnl30);
            shuffleList.Add(pnl31);
            shuffleList.Add(pnl32);

        }


        #region Setting up Game
        // -------------- IMAGE MANIPULATION --------------- //
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

        //Convert 
        private async void PlaceImage(BitmapDecoder decoder)
         {
            int i = 0;
            int j = 0;

            var windowHeight = rectangle1.ActualHeight;
            var windowWidth = rectangle1.ActualWidth;
            var imageHeight = decoder.PixelHeight / 4;
            var imageWidth = decoder.PixelHeight / 4;
            var blocks = new BitmapImage[4, 4];

            for (i = 0; i < 4; i++)
            {
                for(j = 0; j < 4; j++)
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

                    BitmapImage bitImage = new BitmapImage(); //An Image to be displayed
                    bitImage.SetSource(randomAccessStream);
                    blocks[i, j] = bitImage; //To save the Bitmap Image
                    images[i, j] = blocks[i, j]; //Source of the images to be stored
                    //PictureList.Add(images[i, j]);
                }
            }
            for (i = 0; i < 4; i++)
            {
                for(j = 0; j < 4; j++)
                { 
                    winImage[i, j] = images[i, j];
                    winningList.Add(winImage[i,j]);
                }
            }
            
            //Assign image to source
            //Column 0
            img00.Source = images[0, 0];
            img01.Source = images[0, 1];
            img02.Source = images[0, 2];
            img03.Source = images[0, 3];

            //Column 1
            img10.Source = images[1, 0];
            img11.Source = images[1, 1];
            img12.Source = images[1, 2];
            img13.Source = images[1, 3];

            //Column 2
            img20.Source = images[2, 0];
            img21.Source = images[2, 1];
            img22.Source = images[2, 2];
            img23.Source = images[2, 3];

            //Column 3
            img30.Source = images[3, 0];
            img31.Source = images[3, 1];
            img32.Source = images[3, 2];
            img33.Source = images[3, 3];


            //Randomize the blocks to insert into grid
            Shuffle();
            timer.Reset();
            lblTimeElapsed.Text = "00:00:00";
            inMoves = 0;
            lblMoves.Text = "0";

        }

        public void Shuffle()
        {
            Random rand = new Random();
            List<int> generatedNumb = new List<int>();
            int k = 0;
            for(k = 0; k < 15; k++)
            {
                int randNum = rand.Next(15);
                if(generatedNumb.Contains(randNum))
                {
                    k--;
                    continue;
                }
                generatedNumb.Add(randNum);
            }
            k = 0;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    
                    if (j == 3 && i == 3)
                    {
                        break;
                    }
                    Grid.SetColumn(shuffleList[generatedNumb[k]], i);
                    Grid.SetRow(shuffleList[generatedNumb[k]], j);
                    k++;
                }
            }
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
        }

        private void panel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ++inMoves;
            lblMoves.Text = "" + inMoves;
            if(inMoves==1)
            {
                timer.Start();
            }
            lblTimeElapsed.Text = timer.Elapsed.ToString().Remove(8);

            StackPanel panel = (StackPanel)sender;
            int blankRow = 0;
            int blankCol = 0;
            int panelRow = 0;
            int panelCol = 0;
            string panelName = null;

            blankRow = Grid.GetRow(blank);
            blankCol = Grid.GetColumn(blank);
            panelRow = Grid.GetRow(panel);
            panelCol = Grid.GetColumn(panel);
            panelName = panel.Name;
            
            if((blankRow == panelRow) && ((panelCol + 1) == blankCol)) 
            {

                Grid.SetColumn(panel, (Grid.GetColumn(panel) + 1));
                Grid.SetColumn(blank, (Grid.GetColumn(blank) - 1));
            }
            else if ((blankRow == panelRow) && ((panelCol - 1) == blankCol))
            {
                Grid.SetColumn(panel, (Grid.GetColumn(panel) - 1));
                Grid.SetColumn(blank, (Grid.GetColumn(blank) + 1));
            }
            else if ((blankRow == (panelRow + 1)) && (panelCol == blankCol))
            {
                Grid.SetRow(panel, (Grid.GetRow(panel) + 1));
                Grid.SetRow(blank, (Grid.GetRow(blank) - 1));
            }
            else if ((blankRow == (panelRow - 1)) && (panelCol == blankCol))
            {
                Grid.SetRow(panel, (Grid.GetRow(panel) - 1));
                Grid.SetRow(blank, (Grid.GetRow(blank) + 1));
            }

            //Check Win State
            if(checkWin())
            {
                winner.Text = "YOU WIN!!";
                timer.Stop();
            }
        }

        private bool checkWin()
        {
            bool checkRow = false;
            bool checkCol = false;
            bool goodColRow = false;

            if((Grid.GetRow(pnl00) == 0) &&
               (Grid.GetRow(pnl01) == 1) &&
               (Grid.GetRow(pnl02) == 2) &&
               (Grid.GetRow(pnl03) == 3) &&

               (Grid.GetRow(pnl10) == 0) &&
               (Grid.GetRow(pnl11) == 1) &&
               (Grid.GetRow(pnl12) == 2) &&
               (Grid.GetRow(pnl13) == 3) &&

               (Grid.GetRow(pnl20) == 0) &&
               (Grid.GetRow(pnl21) == 1) &&
               (Grid.GetRow(pnl22) == 2) &&
               (Grid.GetRow(pnl23) == 3) &&

               (Grid.GetRow(pnl30) == 0) &&
               (Grid.GetRow(pnl31) == 1) &&
               (Grid.GetRow(pnl32) == 2) &&
               (Grid.GetRow(blank) == 3))
            {
                checkRow = true;
            }

            if ((Grid.GetColumn(pnl00) == 0) &&
                (Grid.GetColumn(pnl01) == 0) &&
                (Grid.GetColumn(pnl02) == 0) &&
                (Grid.GetColumn(pnl03) == 0) &&

                (Grid.GetColumn(pnl10) == 1) &&
                (Grid.GetColumn(pnl11) == 1) &&
                (Grid.GetColumn(pnl12) == 1) &&
                (Grid.GetColumn(pnl12) == 1) &&

                (Grid.GetColumn(pnl20) == 2) &&
                (Grid.GetColumn(pnl21) == 2) &&
                (Grid.GetColumn(pnl22) == 2) &&
                (Grid.GetColumn(pnl23) == 2) &&

                (Grid.GetColumn(pnl30) == 3) &&
                (Grid.GetColumn(pnl31) == 3) &&
                (Grid.GetColumn(pnl32) == 3) &&
                (Grid.GetColumn(blank) == 3))
            {
                checkCol = true;
            }
            
            if(checkCol && checkRow)
            {
                goodColRow = true;
            }
            //throw new NotImplementedException();
            return goodColRow;
        }
        #endregion Setting up Game

        #region Taking a picture
        // -------------------------- PHOTO CAMERA FUN -------------------- //
        //Start the camera
        private async void PhotoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CameraCaptureUI captureUI = new CameraCaptureUI();
                StorageFile file = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);

                if (file != null)
                {
                    BitmapImage bitmapCamera = new BitmapImage();
                    using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read))
                    {
                        //bitmapCamera.SetSource(fileStream);
                        await bitmapCamera.SetSourceAsync(fileStream);
                        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
                        PlaceImage(decoder);

                    }
                    PreviewImage.Source = bitmapCamera;
                }
                else
                {
                    var dialog1 = new MessageDialog("Error");
                    dialog1.ShowAsync();
                }

            }
            catch (Exception ex)
            {
                var dialog1 = new MessageDialog("Error");
                dialog1.ShowAsync();

            }
        }
        #endregion Taking a picture

    }
}
