using Accord.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace x64AccordTestApp
{
    public partial class MainForm : Form
    {

        static int count = 0;
        //视频播放器控件
        VideoPlayer videoPlayer = null;
        Panel snapShotPanel = null;
        public MainForm()
        {
            InitializeComponent();
            InitVideoPlayer();
            InitPicturePanel();
        }

        private void InitPicturePanel()
        {
            snapShotPanel = new Panel();
            snapShotPanel.Location = new Point(660, 10);
            snapShotPanel.Size = new Size(640, 360);
            snapShotPanel.BorderStyle = BorderStyle.FixedSingle;
            snapShotPanel.Show();
            this.Controls.Add(snapShotPanel);
        }

        private void InitVideoPlayer()
        {
            videoPlayer = new VideoPlayer();
            videoPlayer.isCaching = true;
            videoPlayer.Location = new Point(10, 10);
            videoPlayer.Size = new Size(640, 360);
            videoPlayer.Show();
            this.Controls.Add(videoPlayer);
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            string cameraStr = EnumerateVideoDevices();
            //list all the camera devices
            videoPlayer.PlayVideo(cameraStr);
        }

        private void btnTakePicture_Click(object sender, EventArgs e)
        {
            Bitmap frame = videoPlayer.TakeSnapshot("", true);
            snapShotPanel.BackgroundImage = frame;
        }

        private void btnRecord_Click(object sender, EventArgs e)
        {
            if (videoPlayer.VideoSource.FramesReceived <= 0 || (!videoPlayer.IsRunning))
            {
                return;
            }

            //正在播放 且 没有在录像
            if (videoPlayer.isPlaying && !videoPlayer.isRecording)
            {
                string dateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
                //string aviFileName = Environment.CurrentDirectory + "\\" + dateTime + ".avi";
                string aviFileName = Environment.CurrentDirectory + "\\" + dateTime + ".mp4";
                videoPlayer.StartRecord(aviFileName);
                btnRecord.Text = "正在录制";
            }
            else
            {
                string recordFileName;
                videoPlayer.FinishRecord(out recordFileName);
                btnRecord.Text = "录制视频";
            }
        }


        /// <summary>
        /// 枚举视频设备
        /// </summary>
        /// <returns></returns>
        public string EnumerateVideoDevices()
        {
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice); // 筛选视频输入设备

            foreach (var videoDevice in videoDevices)
            {
                string FriendlyName = videoDevice.Name; // 设备的友好名称
                string MonikerName = videoDevice.MonikerString; // 设备的唯一标识符，用于区分哪个设备
                return FriendlyName;
            }
            return "";
        }

    }
}
