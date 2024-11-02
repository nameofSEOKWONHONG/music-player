using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
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

        private readonly IAudioManager _audioManager;
        public MainViewModel(IAudioManager audioManager)
        {
            this.Url = "https://www.youtube.com/watch?v=kPa7bsKwL-c";
            _audioManager = audioManager;
            DownloadCommand = new Command(OnDownloadClicked);
        }

        private async void OnDownloadClicked()
        {
            await Task.Run(async () =>
            {
                var youtube = new YoutubeClient();

                // You can specify either the video URL or its ID
                var videoUrl = this.Url;
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoUrl);
                var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                var targetPath = Path.Combine(FileSystem.AppDataDirectory, $"{Guid.NewGuid()}.mp3");
                await youtube.Videos.Streams.DownloadAsync(streamInfo, targetPath);

                var player = _audioManager.CreatePlayer(targetPath);
                player.Play();
            });

        }
    }
}