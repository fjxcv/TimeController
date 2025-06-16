using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using TimeController.ViewModels;
using System.Media;
using System.Linq;

namespace TimeController.Views.CasualMode
{
    public partial class RewardCelebrationWindow : Window
    {
        private readonly Random _rand = new Random();
        private System.Windows.Media.MediaPlayer _mediaPlayer;
        private bool _isClosing = false;  // 添加标志防止重复关闭
        private readonly CasualModeViewModel _viewModel;

        public RewardCelebrationWindow(CasualModeViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            // 初始化媒体播放器
            _mediaPlayer = new System.Windows.Media.MediaPlayer();
            _mediaPlayer.Volume = 1.0; // 确保音量设置


            // 禁用窗口的关闭按钮
            this.Closing += (s, e) => 
            {
                if (!_isClosing)
                {
                    e.Cancel = true;
                }
            };
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 确保窗口全屏显示
            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.None;
            Topmost = true;

            if (!_viewModel.RewardTasks.Any())
            {
                await _viewModel.LoadRewardsAsync();
            }

            // 播放烟花和音效
            PlayFireworksAndSound();
        }

        private void ClaimRewardButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isClosing) return;  // 如果正在关闭，直接返回
            
            // 禁用按钮防止重复点击
            if (sender is Button button)
            {
                button.IsEnabled = false;
            }

            _isClosing = true;  // 设置关闭标志

            // 停止并清理媒体播放器
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Close();
            }

            // 直接关闭窗口
            this.DialogResult = true;
            this.Close();
        }

        private void RewardComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RewardComboBox.SelectedItem is TimeController.Models.RewardModel selected)
            {
                foreach (var r in _viewModel.RewardTasks)
                {
                    r.IsClaimed = r == selected;
                }
            }
        }

        private void PlayFireworksAndSound()
        {
            // 播放音效
            try
            {
                string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "fireworks.wav");
                var player = new SoundPlayer(filePath);
                player.Load();    // 同步加载
                player.Play();    // 异步播放一次
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
            // 初始位置，屏幕中心
            double centerX = ActualWidth / 2;
            double centerY = ActualHeight / 2;

            // 创建粒子形状
            var particle = new Ellipse
            {
                Width = _rand.Next(10, 25),  // 粒子大小
                Height = _rand.Next(10, 25), // 粒子大小
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

            // 添加粒子效果
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

            // 添加粒子效果
            double distance = 600 + _rand.Next(0, 200); // 粒子爆炸距离
            double angle = _rand.NextDouble() * Math.PI * 2; // 角度

            // 粒子爆炸位置
            double endX = centerX + Math.Cos(angle) * distance;
            double endY = centerY + Math.Sin(angle) * distance;

            // 创建粒子动画
            var animX = new DoubleAnimation(endX, TimeSpan.FromSeconds(1.8));
            var animY = new DoubleAnimation(endY, TimeSpan.FromSeconds(1.8));
            var fade = new DoubleAnimation(0, TimeSpan.FromSeconds(1.8));

            // 粒子效果
            animX.EasingFunction = new BounceEase { Bounces = 1, Bounciness = 2 };
            animY.EasingFunction = new BounceEase { Bounces = 1, Bounciness = 2 };

            // 粒子效果
            double delay = _rand.NextDouble() * 0.5;
            animX.BeginTime = TimeSpan.FromSeconds(delay);
            animY.BeginTime = TimeSpan.FromSeconds(delay);
            fade.BeginTime = TimeSpan.FromSeconds(delay);

            // 粒子效果
            particle.BeginAnimation(Canvas.LeftProperty, animX);
            particle.BeginAnimation(Canvas.TopProperty, animY);
            particle.BeginAnimation(OpacityProperty, fade);
        }

        // 获取鲜艳的颜色
        private Color GetVibrantColor()
        {
            // 鲜艳的颜色数组
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
    }
}