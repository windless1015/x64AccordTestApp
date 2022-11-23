using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Accord.Controls;
using Accord.Video;
using Accord.Video.DirectShow;
using Accord.Video.FFMPEG;
using System.Threading;


/*
 Webcam Name:	Microsoft® LifeCam HD-3000
Quality Rating:	1103
Built-in Microphone:	audioinput#0
Built-in Speaker:	
Frame rate:	30 FPS
Stream Type:	video
Image Mode:	rgb
Webcam MegaPixels:	0.92 MP
Webcam Resolution:	1280×720
Video Standard:	HD
Aspect Ratio:	1.78
PNG File Size:	1.39 MB
JPEG File Size:	725.38 kB
Bitrate:	21.25 MB/s
Number of Colors:	216789
Average RGB Color:	
 
Lightness:	53.92%
Luminosity:	53.85%
Brightness:	53.73%
Hue:	23°
Saturation:	5.53%

 */

namespace x64AccordTestApp
{
    public enum VideoType
    {
        UNKNOWN = -1,
        LOCAL_DEVICE = 0,
        MJPEG = 1,
        VIDEO_FILE = 2
    }
    public partial class VideoPlayer : VideoSourcePlayer
    {
        int curVideoSourceType = 0; //0表示无效, 1表示usb, 2表示牙周
        public bool isStopped = false; //是否已经停止
        public bool isRecording = false; //是否在录制
        public bool isPlaying = false; //是否正在播放
        private VideoFileWriter aviWriter = null;
        private DateTime? _firstFrameTime = null;
        private Size frameSize; //帧的size
        private VideoType vt;
        private string inputString = null;
        private string curRecordFileName = null;
        Size lastClientSize;
        public VideoPlayer()
        {
            InitializeComponent();
            this.NewFrame += new NewFrameHandler(this.videoSourcePlayer_NewFrame);
            this.KeepAspectRatio = true; //保持视频的比例
        }

        //打开本地的设备, MJPEG, 本地文件夹
        public bool PlayVideo(string inputStr)
        {
            if (inputStr == "")
                return false;
            inputString = inputStr;
            //vt = CheckVideoType(inputStr);
            vt = VideoType.LOCAL_DEVICE;
            if (vt == VideoType.UNKNOWN)
                return false;
            else if (vt == VideoType.LOCAL_DEVICE)
            {
                OpenLocalDevice(inputStr);
                //还要判断是是哪个设备
                if (inputStr == "BV USB Camera")
                {
                    frameSize = new Size(400, 400);
                    curVideoSourceType = 2; //牙周
                }
                else if (inputStr == "BV Dental Camera")
                {
                    curVideoSourceType = 1; //usb
                    frameSize = new Size(640, 360);
                }
                else if (inputStr == "Microsoft® LifeCam HD-3000") //https://webcamtests.com/
                {
                    frameSize = new Size(640, 360);
                }
                isPlaying = true;
            }
            else if (vt == VideoType.MJPEG)
            {
                curVideoSourceType = 0;
                OpenMJPEGDevice(inputStr);
                frameSize = new Size(640, 480);
                isPlaying = true;
            }
            else if (vt == VideoType.VIDEO_FILE)
            {
                curVideoSourceType = 0;
                OpenVideoFile(inputStr);
            }
            return true;
        }

        public void StartRecord(string aviSavedFile)
        {
            curRecordFileName = aviSavedFile;
            _firstFrameTime = null;
            aviWriter = new VideoFileWriter();
            //最后一个参数是码率, bitrate,可以调节来控制视频质量
            aviWriter.Open(aviSavedFile, frameSize.Width, frameSize.Height, 25, VideoCodec.H264, 2000 * 1000);
            isRecording = true;
        }

        public void FinishRecord(out string fileName)
        {
            isRecording = false;
            //这里需要做一个延时
            Thread.Sleep(100);

            aviWriter.Close();
            aviWriter.Dispose();
            fileName = curRecordFileName;
            curRecordFileName = null;
        }

        public Bitmap TakeSnapshot(string file, bool isNeedReturenSnapshot)
        {
            Bitmap singleFrame = this.GetCurrentVideoFrame();
            if (singleFrame == null)
            {
                MessageBox.Show("请检查摄像头是否连接正常!");
                return null;
            }
            if(file != "")
                singleFrame.Save(file, System.Drawing.Imaging.ImageFormat.Jpeg);
            if (isNeedReturenSnapshot)
                return singleFrame;
            else
                return null;
        }

        public void PauseVideo()
        {
            this.SignalToStop();
        }

        public void ReStartVideo()
        {
            this.Start();
        }


        private bool OpenLocalDevice(string deviceName)
        {
            bool isFindDevice = false;
            //得到本地所有的视频设备
            var videoDeviceList = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            //遍历这些设备，寻找名字为BV Camera的设备
            string targetDeviceMonikerString = null;
            foreach (var target in videoDeviceList)
            {
                if (target.Name == deviceName)
                {
                    targetDeviceMonikerString = target.MonikerString;
                    isFindDevice = true;
                    break;
                }
            }
            if (!isFindDevice)
                return false;
            var videoSource = new VideoCaptureDevice(targetDeviceMonikerString);
            OpenVideoSource(videoSource);
            return true;
        }

        private bool OpenMJPEGDevice(string ip)
        {
            MJPEGStream mjpegSource = new MJPEGStream(ip);
            OpenVideoSource(mjpegSource);
            return true;
        }

        private bool OpenVideoFile(string filePath)
        {
            if (!System.IO.File.Exists(filePath)) //文件是否存在
                return false;
            FileVideoSource fileSource = new FileVideoSource(filePath);
            OpenVideoSource(fileSource);
            return true;
        }

        //打开视频
        private void OpenVideoSource(IVideoSource source)
        {
            this.Cursor = Cursors.WaitCursor;
            CloseCurrentVideoSource();

            this.VideoSource = source;
            this.Start();

            this.Cursor = Cursors.Default;
        }

        private void CloseCurrentVideoSource()
        {
            if (this.VideoSource != null)
            {
                this.SignalToStop();

                for (int i = 0; i < 30; i++)
                {
                    if (!this.IsRunning)
                        break;
                    System.Threading.Thread.Sleep(100);
                }

                if (this.IsRunning)
                {
                    this.Stop();
                }

                this.VideoSource = null;
            }
            isPlaying = false;
        }

        private void videoSourcePlayer_NewFrame(object sender, ref Bitmap image)
        {
            if (isRecording)
            {
                DateTime now = DateTime.Now;
                Graphics g = Graphics.FromImage(image);

                SolidBrush brush = new SolidBrush(Color.Red);
                g.DrawString(now.ToString(), this.Font, brush, new PointF(30, 10)); //画当前时间
                //if ((int)(now.Subtract(flickerDTime).TotalSeconds) == 3)
                //{
                //    g.FillRectangle(brush, new Rectangle(10, 10, 10, 10)); // 画闪烁的红色的实心方框
                //    flickerDTime = now;
                //}
                brush.Dispose();
                g.Dispose();

                using (var bitmap = (Bitmap)image.Clone())
                {
                    if (_firstFrameTime != null)
                    {
                        aviWriter.WriteVideoFrame(bitmap);
                    }
                    else
                    {
                        aviWriter.WriteVideoFrame(bitmap);
                        _firstFrameTime = DateTime.Now;
                    }
                }
            }


        }



        // On timer event - gather statistics
        //private void timer_Tick(object sender, EventArgs e)
        //{
        //    IVideoSource videoSource = this.VideoSource;

        //    if (videoSource != null)
        //    {
        //        // get number of frames since the last timer tick
        //        int framesReceived = videoSource.FramesReceived;

        //        if (stopWatch == null)
        //        {
        //            stopWatch = new Stopwatch();
        //            stopWatch.Start();
        //        }
        //        else
        //        {
        //            stopWatch.Stop();

        //            float fps = 1000.0f * framesReceived / stopWatch.ElapsedMilliseconds;
        //            fpsLabel.Text = fps.ToString("F2") + " fps";

        //            stopWatch.Reset();
        //            stopWatch.Start();
        //        }
        //    }
        //}

        private VideoType CheckVideoType(string inputString)
        {
            if (inputString.Contains("SKT") || inputString.Contains("BV") || inputString.Contains("HP"))
                return VideoType.LOCAL_DEVICE;
            else if (inputString.Contains("10.10"))
                return VideoType.MJPEG;
            else if (inputString.Contains(".avi"))
                return VideoType.VIDEO_FILE;
            else
                return VideoType.UNKNOWN;
        }


        //双击播放器区域
        private void VideoPlayer_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (curVideoSourceType != 2) //牙周的时候才会有窗口大小的变化
                return;
            //判断当前是大, 中, 小
            Size curClientSize = this.ClientSize;
            if (curClientSize.Width > 600)
            {
                this.ClientSize = new Size(600, 600);
                this.Location = new Point(60 + 400, 135 + 94);
            }
            else if (curClientSize.Width == 600)
            {
                if (lastClientSize.Width > 600)
                {
                    this.ClientSize = new Size(400, 400);
                    this.Location = new Point(60 + 500, 135 + 194);
                }
                else
                {
                    this.ClientSize = new Size(1400, 788);
                    this.Location = new Point(60, 135);
                }

            }
            else
            {
                this.ClientSize = new Size(600, 600);
                this.Location = new Point(60 + 400, 135 + 94);
            }
            lastClientSize = curClientSize;
        }

    }
}
