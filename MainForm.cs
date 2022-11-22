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
        //视频播放器控件
        VideoPlayer videoPlayer = null;

        public MainForm()
        {
            InitializeComponent();
            InitVideoPlayer();
        }

        private void InitVideoPlayer()
        {
            videoPlayer = new VideoPlayer();
            videoPlayer.Location = new Point(10, 10);
            videoPlayer.Size = new Size(640, 360);
            videoPlayer.Show();
            this.Controls.Add(videoPlayer);
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            //videoPlayer.PlayVideo("HP TrueVision HD Camera");
            videoPlayer.PlayVideo("Microsoft® LifeCam HD-3000");
        }

        private void btnTakePicture_Click(object sender, EventArgs e)
        {

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
                string aviFileName = Environment.CurrentDirectory + "\\" + dateTime + ".avi";
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
    }
}
