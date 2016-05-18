using Microsoft.Kinect;
using Microsoft.Kinect.Face;
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
using ImaginativeUniversal;


namespace HololensIPDMeasurementTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor _sensor = null;

        private const float InfraredSourceValueMaximum = (float)ushort.MaxValue;

        private const float InfraredSourceScale = 0.75f;

        private const float InfraredOutputValueMinimum = 0.01f;

        private const float InfraredOutputValueMaximum = 1.0f;

        private FrameDescription _infraredFrameDescription = null;

        private BodyFrameReader _bodyReader = null;

        private HighDefinitionFaceFrameSource _faceFrameSource = null;

        private HighDefinitionFaceFrameReader _faceFrameReader = null;

        private CoordinateMapper _coordinateMapper = null;

        private FaceModel _faceModel = null;

        FaceAlignment _faceAlignment = null;

        private Body[] _bodies = null;

        private int _bodyCount;

        private InfraredFrameReader _irReader = null;

        private WriteableBitmap _infraredBitmap = null;

        private bool _isMeasuring = false;

        private List<double> _collectedMeasurements;

        private const string SETTINGS_FILENAME = "settings.txt";

        private DevPortalVM _settingsVM;

        private DevPortalHelper _devicePortalClient;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _sensor = KinectSensor.GetDefault();
            _coordinateMapper = _sensor.CoordinateMapper;
            _collectedMeasurements = new List<double>();

            if (_sensor != null)
            {
                _infraredFrameDescription = _sensor.InfraredFrameSource.FrameDescription;
                _infraredBitmap = new WriteableBitmap(_infraredFrameDescription.Width, _infraredFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray32Float, null);
                camera.Source = _infraredBitmap;

                _bodyReader = _sensor.BodyFrameSource.OpenReader();
                _bodyCount = _sensor.BodyFrameSource.BodyCount;
                _bodies = new Body[_bodyCount];
                _bodyReader.FrameArrived += BodyReader_FrameArrived;

                _faceModel = new FaceModel();
                _faceAlignment = new FaceAlignment();

                _faceFrameSource = new HighDefinitionFaceFrameSource(_sensor);
                _faceFrameReader = _faceFrameSource.OpenReader();
                _faceFrameReader.FrameArrived += FaceReader_FrameArrived;

                _irReader = _sensor.InfraredFrameSource.OpenReader();
                _irReader.FrameArrived += InfraredReader_FrameArrived;

                _sensor.Open();
            }

            _settingsVM = DevPortalVM.LoadContext(SETTINGS_FILENAME);
            DevPortalGrid.DataContext = _settingsVM;
            _devicePortalClient = new DevPortalHelper(_settingsVM);
          
        }


        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _settingsVM.SaveContext(SETTINGS_FILENAME);

            if (_faceFrameReader != null)
            {
                this._faceFrameReader.Dispose();
                this._faceFrameReader = null;
            }

            if (_faceFrameSource != null)
            {
                _faceFrameSource = null;
            }


            if (_bodyReader != null)
            {
                _bodyReader.Dispose();
                _bodyReader = null;
            }

            if (_sensor != null)
            {
                _sensor.Close();
            }
        }

        private void FaceReader_FrameArrived(object sender, HighDefinitionFaceFrameArrivedEventArgs args)
        {
            using (var frame = args.FrameReference.AcquireFrame())
            {
                if (frame != null && frame.IsFaceTracked)
                {
                    if (_faceModel == null || _faceAlignment == null)
                        return;

                    frame.GetAndRefreshFaceAlignmentResult(_faceAlignment);
                    var faceVertices = _faceModel.CalculateVerticesForAlignment(_faceAlignment);

                    var cspLeftEye = GetLeftEye(faceVertices);
                    var cspRightEye = GetRightEye(faceVertices);

                    var ipd = cspLeftEye.Distance(cspRightEye) * 1000;
                    if(_isMeasuring)
                    {
                        _collectedMeasurements.Add(ipd);
                        var avg = _collectedMeasurements.Average();
                        this.myText.Text = avg.ToString("0.000");
                        pbMeasure.Value++;
                        if(pbMeasure.Value == pbMeasure.Maximum)
                        {
                            _isMeasuring = false;
                            finalIPD.Text = avg.ToString("0.000");
                            spTalkToDevicePortal.Visibility = Visibility.Visible;
                        }

                    } else {
                        this.myText.Text = ipd.ToString("0.000");
                    }


                    var pointEyeLeft = _coordinateMapper.MapCameraPointToDepthSpace(cspLeftEye);
                    var pointEyeRight = _coordinateMapper.MapCameraPointToDepthSpace(cspRightEye);
                    var pointCheekLeft = _coordinateMapper.MapCameraPointToDepthSpace(faceVertices[412]);
                    var pointCheekRight = _coordinateMapper.MapCameraPointToDepthSpace(faceVertices[933]);
                    var pointNose = _coordinateMapper.MapCameraPointToDepthSpace(faceVertices[18]);
                    var pointMouth = _coordinateMapper.MapCameraPointToDepthSpace(faceVertices[10]);
                    var pointChin = _coordinateMapper.MapCameraPointToDepthSpace(faceVertices[4]);
                    var pointForehead = _coordinateMapper.MapCameraPointToDepthSpace(faceVertices[28]);


                    Canvas.SetLeft(eyeLeft, pointEyeLeft.X - eyeLeft.Width / 2.0);
                    Canvas.SetTop(eyeLeft, pointEyeLeft.Y - eyeLeft.Height / 2.0);

                    Canvas.SetLeft(eyeRight, pointEyeRight.X - eyeRight.Width / 2.0);
                    Canvas.SetTop(eyeRight, pointEyeRight.Y - eyeRight.Height / 2.0);

                    Canvas.SetLeft(cheekLeft, pointCheekLeft.X - cheekLeft.Width / 2.0);
                    Canvas.SetTop(cheekLeft, pointCheekLeft.Y - cheekLeft.Height / 2.0);

                    Canvas.SetLeft(cheekRight, pointCheekRight.X - cheekRight.Width / 2.0);
                    Canvas.SetTop(cheekRight, pointCheekRight.Y - cheekRight.Height / 2.0);

                    Canvas.SetLeft(nose, pointNose.X - nose.Width / 2.0);
                    Canvas.SetTop(nose, pointNose.Y - nose.Height / 2.0);

                    Canvas.SetLeft(mouth, pointMouth.X - mouth.Width / 2.0);
                    Canvas.SetTop(mouth, pointMouth.Y - mouth.Height / 2.0);

                    Canvas.SetLeft(chin, pointChin.X - chin.Width / 2.0);
                    Canvas.SetTop(chin, pointChin.Y - chin.Height / 2.0);

                    Canvas.SetLeft(forehead, pointForehead.X - forehead.Width / 2.0);
                    Canvas.SetTop(forehead, pointForehead.Y - forehead.Height / 2.0);


                }
            }
        }

        private CameraSpacePoint GetLeftEye(IReadOnlyList<CameraSpacePoint> vertices)
        {
            CameraSpacePoint cameraSpacePoint = vertices[210];
            CameraSpacePoint cameraSpacePoint2 = vertices[469];
            return new CameraSpacePoint
            {
                X = (cameraSpacePoint2.X + cameraSpacePoint.X) / 2f,
                Y = (cameraSpacePoint2.Y + cameraSpacePoint.Y) / 2f,
                Z = (cameraSpacePoint2.Z + cameraSpacePoint.Z) / 2f
            };

        }

        /// <summary>
        /// Returns the center of the Right Eye.
        /// </summary>
        private CameraSpacePoint GetRightEye(IReadOnlyList<CameraSpacePoint> vertices)
        {

            CameraSpacePoint cameraSpacePoint = vertices[843];
            CameraSpacePoint cameraSpacePoint2 = vertices[1117];
            return new CameraSpacePoint
            {
                X = (cameraSpacePoint2.X + cameraSpacePoint.X) / 2f,
                Y = (cameraSpacePoint2.Y + cameraSpacePoint.Y) / 2f,
                Z = (cameraSpacePoint2.Z + cameraSpacePoint.Z) / 2f
            };
        }



        private void InfraredReader_FrameArrived(object sender, InfraredFrameArrivedEventArgs args)
        {
            using (var frame = args.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    using (Microsoft.Kinect.KinectBuffer infraredBuffer = frame.LockImageBuffer())
                    {
                        // verify data and write the new infrared frame data to the display bitmap
                        if (((_infraredFrameDescription.Width * _infraredFrameDescription.Height) == (infraredBuffer.Size / _infraredFrameDescription.BytesPerPixel)) &&
                            (_infraredFrameDescription.Width == _infraredBitmap.PixelWidth) && (_infraredFrameDescription.Height == _infraredBitmap.PixelHeight))
                        {
                            this.ProcessInfraredFrameData(infraredBuffer.UnderlyingBuffer, infraredBuffer.Size);
                        }
                    }
                }
            }
        }

        private unsafe void ProcessInfraredFrameData(IntPtr infraredFrameData, uint infraredFrameDataSize)
        {
            ushort* frameData = (ushort*)infraredFrameData;
            _infraredBitmap.Lock();
            float* backBuffer = (float*)_infraredBitmap.BackBuffer;

            for (int i = 0; i < (int)(infraredFrameDataSize / _infraredFrameDescription.BytesPerPixel); ++i)
            {
                backBuffer[i] = Math.Min(InfraredOutputValueMaximum, (((float)frameData[i] / InfraredSourceValueMaximum * InfraredSourceScale) * (1.0f - InfraredOutputValueMinimum)) + InfraredOutputValueMinimum);
            }


            _infraredBitmap.AddDirtyRect(new Int32Rect(0, 0, _infraredBitmap.PixelWidth, _infraredBitmap.PixelHeight));
            _infraredBitmap.Unlock();
        }

        private void BodyReader_FrameArrived(object sender, BodyFrameArrivedEventArgs args)
        {
            using (var bodyFrame = args.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    bodyFrame.GetAndRefreshBodyData(_bodies);
                    var body = _bodies.Where(b => b.IsTracked).FirstOrDefault();
                    if (!_faceFrameSource.IsTrackingIdValid && body != null)
                    {
                        _faceFrameSource.TrackingId = body.TrackingId;
                    }
                }
            }
        }

        private void MeasureButton_Click(object sender, RoutedEventArgs e)
        {
            spTalkToDevicePortal.Visibility = Visibility.Collapsed;
            _collectedMeasurements.Clear();
            pbMeasure.Value = 0;
            _isMeasuring = true;
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var ipd = double.Parse(finalIPD.Text);
            try
            {
                _settingsVM.Password = password.Password;
                _devicePortalClient.SetIPD(ipd);
                _settingsVM.IPD = ipd;
            }
            catch
            {
                MessageBox.Show("Unable to save IPD setting to the HoloLens. Make sure the HoloLens is powered and IP address is correct.");
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            _settingsVM.SaveContext(SETTINGS_FILENAME);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _settingsVM.Password = password.Password;
            var idp = _devicePortalClient.GetIPDFromDevice();
        }
    }
}
