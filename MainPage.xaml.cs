using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.AI.MachineLearning.Preview;
using Windows.Storage;
using Windows.Media;
using Windows.Graphics.Imaging;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using System.Diagnostics;
using Windows.UI.Core;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Media.Imaging;

/// <summary>
/// How to add additional onnx model to this sample application.
/// 1) Copy new onnx model to "Assets" subfolder.
/// 2) Add model to "Project" under Assets folder by selecting "Add existing item"; navigate to new onnx model and add.
///    Change properties "Build-Action" to "Content"  and  "Copy to Output Directory" to "Copy if Newer"
/// 3) Add to list of supported models by adding entry to 'onnxFileNames' inializer below.
///    When you add name of onnx model file name you also neeed to specify the number of lables the model contains.
/// </summary>

namespace SampleOnnxEvaluationApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Stopwatch _stopwatch = new Stopwatch();
        private OnnxModel _model = null;
        private string _ourOnnxFileName = "CheesecakeDonutsFries.onnx";
        private int _ourOnnxNumLables = 3;
        private List<Tuple<string, int>> onnxFileNames = new List<Tuple<string,int>>()
          {
            new Tuple<string,int>("CheesecakeDonutsFries.onnx" , 3 ),
            new Tuple<string,int>("Plankton.onnx", 4 )
          };

        public sealed class OnnxModelInput
        {
            public VideoFrame data { get; set; }
        }

        public sealed class OnnxModelOutput
        {
            public IList<string> classLabel { get; set; }
            public IDictionary<string, float> loss { get; set; }
            public OnnxModelOutput(int numLables)
            {
                this.classLabel = new List<string>();

                // For dictionary(map) fields onnx needs the variable to be pre-allocatd such that the 
                // length is equal to the number of labels defined in the model. The names are not
                // required to match what is in the model.
                this.loss = new Dictionary<string, float>();
                for (int x = 0; x < numLables; ++x)
                    this.loss.Add("Label_" + x.ToString(), 0.0f);
            }
        }

        public sealed class OnnxModel
        {
            private LearningModelPreview learningModel = null;

            public static async Task<OnnxModel> CreateOnnxModel(StorageFile file)
            {
                LearningModelPreview learningModel = null;

                try
                {
                    learningModel = await LearningModelPreview.LoadModelFromStorageFileAsync(file);
                }
                catch (Exception e)
                {
                    var exceptionStr = e.ToString();
                    System.Console.WriteLine(exceptionStr);
                    throw e;
                }
                OnnxModel model = new OnnxModel();
                learningModel.InferencingOptions.PreferredDeviceKind = LearningModelDeviceKindPreview.LearningDeviceGpu;
                learningModel.InferencingOptions.ReclaimMemoryAfterEvaluation = true;
                model.learningModel = learningModel;
                return model;
            }

            public async Task<OnnxModelOutput> EvaluateAsync(OnnxModelInput input, int numLabels)
            {
                int zed = numLabels;
                OnnxModelOutput output = new OnnxModelOutput(zed);
                LearningModelBindingPreview binding = new LearningModelBindingPreview(learningModel);
                binding.Bind("data", input.data);
                binding.Bind("classLabel", output.classLabel);
                binding.Bind("loss", output.loss);
                LearningModelEvaluationResultPreview evalResult = await learningModel.EvaluateAsync(binding, string.Empty);
                return output;
            }
        }

        public MainPage()
        {
            this.InitializeComponent();
            foreach (var x in onnxFileNames)
            {
                this.ActiveModel.Items.Add(x.Item1);
            }
            this.ActiveModel.SelectedIndex = 0;
            _ourOnnxFileName = onnxFileNames[0].Item1;
            _ourOnnxNumLables = onnxFileNames[0].Item2;
        }

        private async Task LoadModelAsync()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => StatusBlock.Text = $"Loading {_ourOnnxFileName} ... patience ");

            try
            {
                _stopwatch = Stopwatch.StartNew();

                var modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/{_ourOnnxFileName}"));
                _model = await OnnxModel.CreateOnnxModel(modelFile);

                _stopwatch.Stop();
                Debug.WriteLine($"Loaded {_ourOnnxFileName}: Elapsed time: {_stopwatch.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"error: {ex.Message}");
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => StatusBlock.Text = $"error: {ex.Message}");
                _model = null;
            }
        }

        private async void ButtonRun_Click(object sender, RoutedEventArgs e)
        {
            ButtonRun.IsEnabled = false;
            UIPreviewImage.Source = null;
            try
            {
                if (_model == null)
                {
                    // Load the model
                    await Task.Run(async () => await LoadModelAsync());
                }

                // Trigger file picker to select an image file
                FileOpenPicker fileOpenPicker = new FileOpenPicker();
                fileOpenPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                fileOpenPicker.FileTypeFilter.Add(".bmp");
                fileOpenPicker.FileTypeFilter.Add(".jpg");
                fileOpenPicker.FileTypeFilter.Add(".png");
                fileOpenPicker.ViewMode = PickerViewMode.Thumbnail;
                StorageFile selectedStorageFile = await fileOpenPicker.PickSingleFileAsync();

                SoftwareBitmap softwareBitmap;
                using (IRandomAccessStream stream = await selectedStorageFile.OpenAsync(FileAccessMode.Read))
                {
                    // Create the decoder from the stream 
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                    // Get the SoftwareBitmap representation of the file in BGRA8 format
                    softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                    softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }

                // Display the image
                SoftwareBitmapSource imageSource = new SoftwareBitmapSource();
                await imageSource.SetBitmapAsync(softwareBitmap);
                UIPreviewImage.Source = imageSource;

                // Encapsulate the image in the WinML image type (VideoFrame) to be bound and evaluated
                VideoFrame inputImage = VideoFrame.CreateWithSoftwareBitmap(softwareBitmap);

                await Task.Run(async () =>
                {
                    // Evaluate the image
                    await EvaluateVideoFrameAsync(inputImage);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"error: {ex.Message}");
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => StatusBlock.Text = $"error: {ex.Message}");
            }
            finally
            {
                ButtonRun.IsEnabled = true;
            }
        }

        private async Task EvaluateVideoFrameAsync(VideoFrame frame)
        {
            if (frame != null)
            {
                try
                {
                    _stopwatch.Restart();
                    OnnxModelInput inputData = new OnnxModelInput();
                    inputData.data = frame;
                    var results = await _model.EvaluateAsync(inputData, _ourOnnxNumLables);
                    var loss = results.loss.ToList().OrderBy(x=>-(x.Value));
                    var labels = results.classLabel;
                    _stopwatch.Stop();

                    var lossStr = string.Join(",  ", loss.Select(l => l.Key + " " + (l.Value * 100.0f).ToString("#0.00") + "%"));
                    string message = $"Evaluation took {_stopwatch.ElapsedMilliseconds}ms to execute, Predictions: {lossStr}.";
                    Debug.WriteLine(message);
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => StatusBlock.Text = message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"error: {ex.Message}");
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => StatusBlock.Text = $"error: {ex.Message}");
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => ButtonRun.IsEnabled = true);
            }
        }

        private void ActiveModel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIdx = ActiveModel.SelectedIndex;
            if  ((selectedIdx >= 0)  &&  (selectedIdx < onnxFileNames.Count))
            {
                _ourOnnxFileName = onnxFileNames[selectedIdx].Item1;
                _ourOnnxNumLables = onnxFileNames[selectedIdx].Item2;
                _model = null;  // Will force reloading of model.
            }
        }
    }
}
