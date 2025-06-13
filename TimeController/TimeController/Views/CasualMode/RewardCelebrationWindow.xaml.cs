using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace TimeController.Views.CasualMode
{
    public partial class RewardCelebrationWindow : Window
    {
        private readonly Random _rand = new Random();
        private System.Windows.Media.MediaPlayer _mediaPlayer;

        public RewardCelebrationWindow()
        {
            InitializeComponent();

            // 初始化媒体播放器
            _mediaPlayer = new System.Windows.Media.MediaPlayer();

            // 加载音效文件
            try
            {
                var uri = new Uri("pack://application:,,,/TimeController;component/Resources/fireworks.mp3", UriKind.Absolute);
                _mediaPlayer.Open(uri);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"无法加载音效文件: {ex.Message}");
            }
        }

        private void ClaimRewardButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PlayFireworksAndSound();

            // 5秒后自动关闭窗口
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                this.Close();
            };
            timer.Start();
        }

        private void PlayFireworksAndSound()
        {
            // 播放音效（循环播放）
            try
            {
                _mediaPlayer.Play();
                _mediaPlayer.MediaEnded += (s, e) =>
                {
                    _mediaPlayer.Position = TimeSpan.Zero;
                    _mediaPlayer.Play();
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"播放音效失败: {ex.Message}");
            }

            // 创建烟花粒子
            FullScreenFireworksCanvas.Children.Clear();
            for (int i = 0; i < 150; i++)
            {
                CreateFireworkParticle();
            }
        }

        private void CreateFireworkParticle()
        {
            // 随机起始位置（屏幕中心）
            double centerX = ActualWidth / 2;
            double centerY = ActualHeight / 2;

            // 创建更加醒目的烟花粒子
            var particle = new Ellipse
            {
                Width = _rand.Next(10, 25),  // 增大粒子尺寸
                Height = _rand.Next(10, 25), // 增大粒子尺寸
                Fill = new RadialGradientBrush(
                    GetVibrantColor(),       // 使用鲜艳的颜色
                    Colors.Transparent)
                {
                    RadiusX = 0.8,
                    RadiusY = 0.8,
                    GradientStops =
                    {
                        new GradientStop(Colors.White, 0.0),
                        new GradientStop(GetVibrantColor(), 0.5),
                        new GradientStop(Colors.Transparent, 1.0)
                    }
                },
                Opacity = 1.0
            };

            // 添加发光效果
            particle.Effect = new DropShadowEffect
            {
                Color = Colors.White,
                BlurRadius = 20,
                ShadowDepth = 0,
                Opacity = 0.8
            };

            Canvas.SetLeft(particle, centerX);
            Canvas.SetTop(particle, centerY);
            FullScreenFireworksCanvas.Children.Add(particle);

            // 创建动画：爆炸效果
            double distance = 600 + _rand.Next(0, 200); // 增加爆炸距离
            double angle = _rand.NextDouble() * Math.PI * 2; // 随机角度

            // 计算终点位置
            double endX = centerX + Math.Cos(angle) * distance;
            double endY = centerY + Math.Sin(angle) * distance;

            // 创建动画
            var animX = new DoubleAnimation(endX, TimeSpan.FromSeconds(1.8));
            var animY = new DoubleAnimation(endY, TimeSpan.FromSeconds(1.8));
            var fade = new DoubleAnimation(0, TimeSpan.FromSeconds(1.8));

            // 添加弹跳效果
            animX.EasingFunction = new BounceEase { Bounces = 1, Bounciness = 2 };
            animY.EasingFunction = new BounceEase { Bounces = 1, Bounciness = 2 };

            // 添加随机延迟，使爆炸更自然
            double delay = _rand.NextDouble() * 0.5;
            animX.BeginTime = TimeSpan.FromSeconds(delay);
            animY.BeginTime = TimeSpan.FromSeconds(delay);
            fade.BeginTime = TimeSpan.FromSeconds(delay);

            // 启动动画
            particle.BeginAnimation(Canvas.LeftProperty, animX);
            particle.BeginAnimation(Canvas.TopProperty, animY);
            particle.BeginAnimation(OpacityProperty, fade);
        }

        // 生成鲜艳的颜色
        private Color GetVibrantColor()
        {
            // 鲜艳的颜色列表：红、橙、黄、绿、蓝、紫
            Color[] vibrantColors =
            {
                Colors.Red,
                Colors.Orange,
                Colors.Yellow,
                Colors.LimeGreen,
                Colors.Cyan,
                Colors.Blue,
                Colors.Magenta
            };

            return vibrantColors[_rand.Next(vibrantColors.Length)];
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // 关闭时停止音乐
            _mediaPlayer.Stop();
            _mediaPlayer.Close();
        }
    }
}