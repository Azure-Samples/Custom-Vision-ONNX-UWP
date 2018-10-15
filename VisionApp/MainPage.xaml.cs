using System;
using System.Linq;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Media.Capture;
using Windows.ApplicationModel;
using System.Threading.Tasks;
using Windows.System.Display;
using Windows.Graphics.Display;
using Windows.Devices.Enumeration;
using Windows.Media.Capture.Frames;
using System.Threading;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using Windows.Storage;
using Windows.Storage.Streams;

namespace VisionApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        // For capturing video from the camera and displaying a preview
        MediaCapture mediaCapture;
        bool isPreviewing;
        DisplayRequest displayRequest = new DisplayRequest();

        // Classes generated from the ONNX model
        // IMPORTANT: Change to the class names to match the ones defined in the
        //   .cs file generated from your ONNX model
        ModelInput inputData;
        Model myVisionModel;

        // Frame reader for extracting frames from the video
        MediaFrameReader frameReader;
        int processingFlag;

        // Handle property changes
        public event PropertyChangedEventHandler PropertyChanged;

        string score;
        public string Score
        {
            get => this.score;
            set => this.SetProperty(ref this.score, value);
        }

        /// <summary>
        /// Sets <paramref name="propertyName"/> to a <paramref name="value"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="storage"></param>
        /// <param name="value"></param>
        /// <param name="propertyName"></param>
        void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            storage = value;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Handle OnLoad event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Load the model
            await this.LoadModelAsync();

            // Start the camera video preview
            await StartPreviewAsync();
        }

        /// <summary>
        /// Load and create the model from the .onnx file
        /// </summary>
        /// <returns></returns>
        private async Task LoadModelAsync()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => StatusBlock.Text=$"Loading model");
            try
            {
                // Load the .onnx file
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/cat-or-dog.onnx"));
                // Create the model from the file
                // IMPORTANT: Change `Model.CreateModel` to match the class and methods in the
                //   .cs file generated from the ONNX model
                this.myVisionModel = await Model.CreateModel(file);
            }
            catch (Exception ex)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => StatusBlock.Text = $"Error: {ex.Message}");
            }
            
        }

        /// <summary>
        /// Try to retrieve the information for the camera by panel
        /// </summary>
        /// <param name="desiredPanel"></param>
        /// <returns></returns>
        private static async Task<DeviceInformation> FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel desiredPanel)
        {
            // Get available devices for capturing pictures
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            // Get the desired camera by panel
            DeviceInformation desiredDevice = allVideoDevices.FirstOrDefault(
                x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == desiredPanel);

            // If there is no device mounted on the desired panel, return the first device found
            return desiredDevice ?? allVideoDevices.FirstOrDefault();
        }

        /// <summary>
        /// Processes media frames as they arrive
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        async void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            if (Interlocked.CompareExchange(ref this.processingFlag, 1, 0) == 0)
            {
                try
                {
                    using (var frame = sender.TryAcquireLatestFrame())
                    using (var videoFrame = frame.VideoMediaFrame?.GetVideoFrame())
                    {
                        if (videoFrame != null)
                        {
                            // If there is a frame, set it as input to the model
                            this.inputData.data = videoFrame;
                            // Evaluate the input data
                            var evalOutput = await this.myVisionModel.EvaluateAsync(this.inputData);
                            // Do something with the model output
                            await this.ProcessOutputAsync(evalOutput);
                        }
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref this.processingFlag, 0);
                }
            }
        }

        /// <summary>
        /// Process the output returned by the model
        /// </summary>
        /// <param name="evalOutput"></param>
        /// <returns></returns>
        async Task ProcessOutputAsync(ModelOutput evalOutput)
        {
            //Get the tags and score to string and then display
            string score = string.Join("   ", evalOutput.loss);
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    this.Score = score;
                }
            );
        }

        /// <summary>
        /// Start the video preview
        /// </summary>
        /// <returns></returns>
        private async Task StartPreviewAsync()
        {
            try
            {
                // Try to get the rear camera
                var cameraDevice = await FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel.Back);
                var settings = new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice.Id };
                // Setup video capture from the camera
                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync(settings);
                displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;

                // Set up the FrameReader to capture frames from the camera video
                var frameSource = this.mediaCapture.FrameSources.Where(
                    source => source.Value.Info.SourceKind == MediaFrameSourceKind.Color)
                    .First();
                this.frameReader =
                    await this.mediaCapture.CreateFrameReaderAsync(frameSource.Value);
                // Set up handler for frames
                this.frameReader.FrameArrived += OnFrameArrived;
                // Start the FrameReader
                await this.frameReader.StartAsync();
            }
            catch (UnauthorizedAccessException)
            {
                // Display an error if the user denied access to the camera in privacy settings
                ContentDialog unauthorizedMsg = new ContentDialog()
                {
                    Title = "No access",
                    Content = "The app was denied access to the camera",
                    CloseButtonText = "OK"
                };
                await unauthorizedMsg.ShowAsync();
                return;
            }

            try
            {
                // Wire up the video capture to the CaptureElement to display the video preview
                VideoPreview.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();
                isPreviewing = true;
            }
            catch (System.IO.FileLoadException)
            {
                mediaCapture.CaptureDeviceExclusiveControlStatusChanged += MediaCaptureCaptureDeviceExclusiveControlStatusChanged;
            }

        }

        /// <summary>
        /// Handler for exclusive control events for the camera
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void MediaCaptureCaptureDeviceExclusiveControlStatusChanged(MediaCapture sender, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
        {
            if (args.Status == MediaCaptureDeviceExclusiveControlStatus.SharedReadOnlyAvailable)
            {
                ContentDialog accessMsg = new ContentDialog()
                {
                    Title = "No access",
                    Content = "Another app has exclusive access",
                    CloseButtonText = "OK"
                };
            }
            else if (args.Status == MediaCaptureDeviceExclusiveControlStatus.ExclusiveControlAvailable && !isPreviewing)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    await StartPreviewAsync();
                });
            }
        }

        /// <summary>
        /// Clean up the camera access/preview
        /// </summary>
        /// <returns></returns>
        private async Task CleanupCameraAsync()
        {
            if (mediaCapture != null)
            {
                if (isPreviewing)
                {
                    await mediaCapture.StopPreviewAsync();
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    VideoPreview.Source = null;
                    if (displayRequest != null)
                    {
                        displayRequest.RequestRelease();
                    }

                    mediaCapture.Dispose();
                    mediaCapture = null;
                });
            }

        }

        /// <summary>
        /// Handler for application suspend
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ApplicationSuspending(object sender, SuspendingEventArgs e)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                var deferral = e.SuspendingOperation.GetDeferral();
                await CleanupCameraAsync();
                deferral.Complete();
            }
        }

        public MainPage()
        {
            this.InitializeComponent();

            // Initialize the input object
            this.inputData = new ModelInput();
            // Event handlers
            Application.Current.Suspending += ApplicationSuspending;
            this.Loaded += OnLoaded;
        }
    }
}
