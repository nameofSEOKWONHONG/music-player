using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using eXtensionSharp;
using Microsoft.Maui.Accessibility;
using Microsoft.Maui.Controls;
using Plugin.Maui.Audio;
using VideoLibrary;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace MauiApp1;

public partial class MainPage : ContentPage
{
    int count = 0;

    public MainPage(IServiceProvider provider)
    {
        InitializeComponent();
        this.BindingContext = provider.GetService<MainViewModel>();
    }

    private async void OnCounterClicked(object sender, EventArgs e)
    {
        count++;

        if (count == 1)
            CounterBtn.Text = $"Clicked {count} time";
        else
            CounterBtn.Text = $"Clicked {count} times";

        SemanticScreenReader.Announce(CounterBtn.Text);
    }
    
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _url;
    
        public string Url
        {
            get => _url;
            set
            {
                if (_url != value)
                {
                    _url = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        public ICommand DownloadCommand { get; set; }
        public ICommand PlayCommand { get; set; }
        public ICommand StopCommand { get; set; }
        public ICommand PauseCommand { get; set; }

        private readonly IAudioManager _audioManager;
        private IAudioPlayer? _audioPlayer;
        public MainViewModel(IAudioManager audioManager)
        {
            this.Url = "https://www.youtube.com/watch?v=kPa7bsKwL-c";
            _audioManager = audioManager;
            DownloadCommand = new Command(OnDownloadClicked);
            PlayCommand = new Command(OnPlayClicked);
            StopCommand = new Command(OnStopClicked);
            PauseCommand = new Command(OnPauseClicked);
        }

        private void OnPauseClicked()
        {
            _audioPlayer.Pause();
        }

        private void OnStopClicked()
        {
            _audioPlayer.Stop();
        }

        private void OnPlayClicked()
        {
            var path = Microsoft.Maui.Storage.Preferences.Get("music", string.Empty);
            if (_audioPlayer.xIsEmpty())
            {
                #if WINDOWS
                var stream = File.OpenRead(path);
                _audioPlayer = _audioManager.CreatePlayer(stream);
                #elif ANDROID
                _audioPlayer = _audioManager.CreatePlayer(path);
                #endif
            }
            
            _audioPlayer.Play();
        }

        private async void OnDownloadClicked()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            var path = Microsoft.Maui.Storage.Preferences.Get("music", string.Empty);
            if (path.xIsEmpty())
            {
                var toast = Toast.Make("downloading", ToastDuration.Short, 14);
                await toast.Show(cancellationTokenSource.Token);
                
                await Task.Factory.StartNew(async () =>
                {
                    var youtube = new YoutubeClient();

                    // You can specify either the video URL or its ID
                    var videoUrl = this.Url;
                    var video = await youtube.Videos.GetAsync(videoUrl);

                    var title = video.Title; // "Collections - Blender 2.80 Fundamentals"

                    var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoUrl);
                    var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                    var targetPath = Path.Combine(FileSystem.AppDataDirectory, $"{title}.mp3");
                    await youtube.Videos.Streams.DownloadAsync(streamInfo, targetPath);
                    Microsoft.Maui.Storage.Preferences.Set("music", targetPath);
                });
                
                toast = Toast.Make("downloaded", ToastDuration.Short, 14);
                await toast.Show(cancellationTokenSource.Token);                
            }

            var toast2 = Toast.Make("already downloaded", ToastDuration.Short, 14);
            await toast2.Show(cancellationTokenSource.Token);             
        }
    }
}