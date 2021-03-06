﻿using static Wallace.UWP.Helpers.Tools.UWPStates;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Core;
using Windows.Media.Core;
using Windows.Storage.Streams;
using Douban.UWP.Core.Models;
using Douban.UWP.Core.Models.FMModels;
using Wallace.UWP.Helpers;
using Douban.UWP.Core.Tools;
using System.Collections.ObjectModel;
using Douban.UWP.NET.Resources;

namespace Douban.UWP.NET.Tools {
    public class DoubanMusicService : ViewModelBase {

        public DoubanMusicService(MusicServiceType service_type = MusicServiceType.SongList) {
            ServiceType = service_type;
            this.Player.Source = service_type == MusicServiceType.MHz ? this.MHzChannelList : this.SongPlayList;
            this.RegisterListEventHandlers();
            this.SetSongListPlayerStyleIfNeed(service_type);
            this.Player.Volume = AppResources.Volumn;
        }

        private void SetSongListPlayerStyleIfNeed(MusicServiceType service_type) {
            Player.MediaEnded += OnMediaEnded;
            if (service_type == MusicServiceType.MHz)
                return;
            var player_type = SettingsHelper.ReadSettingsValue(SettingsSelect.BackPlayType);
            PlayType = player_type is string ?
                EnumHelper.Parse<MusicServicePlayType>(player_type as string) :
                MusicServicePlayType.StreamPlay;
            this.InitPlayStyle(PlayType);
        }

        private void OnMediaEnded(MediaPlayer sender, object args) {
            if (this.SingletonPlay && SongPlayList.Items.Count == 1) {
                this.SongListMoveTo(0);
                this.PlayAnyway();
            }
        }

        private void RegisterListEventHandlers() {
            MHzChannelList.CurrentItemChanged += OnCurrentItemChangedAsync;
            SongPlayList.CurrentItemChanged += OnCurrentItemChangedAsync;
        }

        private void OnCurrentItemChangedAsync(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args) {
            var newItem = args.NewItem;
            if (newItem == null)
                return;
            ChangeSelectItem();
            if (newItem.Source.CustomProperties["Message"] is MusicBoardParameter para)
                AppResources.MusicIsCurrent = para;
            if (ServiceType == MusicServiceType.MHz)
                ActionForMHz?.Invoke();
            CacheItems.Add(newItem);
            if (CacheItems.Count <= CacheMax || CacheItems.ToList().Exists(i => i.Source.CustomProperties["SHA256"] as string == newItem.Source.CustomProperties["SHA256"] as string))
                return;
            CacheItems[0].Source.Reset();
            CacheItems.RemoveAt(0);
        }

        private void ChangeSelectItem() {
            if(_current!=null)
                _current.IsSelect = false;
            _current = CurrentItem;
            _current.IsSelect = true;
        }

        private bool ResetPlayer() {
            try {
                Player.Pause();
                Player.Source = null;
                Player.MediaEnded -= OnMediaEnded;
                return true;
            } catch { return false; }
        }

        public bool ChangeServiceChoice(MusicServiceType type) {
            if (type == ServiceType)
                return true;
            var succeed = this.ResetPlayer();
            if (succeed) {
                this.ServiceType = type;
                this.Player.Source = PlaybackList;
                this.Player.IsLoopingEnabled = false;
                this.SetSongListPlayerStyleIfNeed(type);
            }
            return succeed;
        }

        public void PlayAnyway() {
            Player.Play();
        }

        public void PauseAnyway() {
            Player.Pause();
        }

        public void BindingOnMediaEnded(Windows.Foundation.TypedEventHandler<MediaPlayer,object> callback, bool to_bind) {
            if (to_bind)
                Player.MediaEnded += callback;
            else
                Player.MediaEnded -= callback;
        }

        public bool ChangeVolumnTo(double num) {
            if (num > 1 || num < 0)
                return false;
            Player.Volume = num;
            return true;
        }

        public double GetVolumn() {
            return Player.Volume;
        }

        #region Service Methods Selector

        public void MoveNextAnyway() {
            if (ServiceType == MusicServiceType.SongList)
                SongListMoveNext();
            else
                MHzListMoveNext();
        }

        public void MovePreviousAnyway() {
            if (ServiceType == MusicServiceType.SongList)
                SongListMovePrevious();
            else
                MHzListMovePrevious();
        }

        public void MoveToAnyway(MHzSongBase song) {
            if (ServiceType == MusicServiceType.SongList)
                SongListMoveTo(song);
            else
                MHzListMoveTo(song);
        }

        public async Task<bool> InsertItemAsync(MHzSongBase song, int index = -1) {
            if (ServiceType == MusicServiceType.SongList)
                return await InsertMusicItemAsync(song, index);
            else
                return await InsertMHzItemAsync(song, index);
        }

        public int InsertItem(MediaPlaybackItem musicItem, int index = -1) {
            if (ServiceType == MusicServiceType.SongList)
                return InsertMusicItem(musicItem, index);
            else
                return InsertMHzItem(musicItem, index);
        }

        public bool IsExistItem(MediaPlaybackItem musicItem) {
            if (ServiceType == MusicServiceType.SongList)
                return IsExistPlayItem(musicItem);
            else
                return IsExistMHzItem(musicItem);
        }

        public bool IsExistItem(MHzSongBase song) {
            if (ServiceType == MusicServiceType.SongList)
                return IsExistPlayItem(song);
            else
                return IsExistMHzItem(song);
        }

        public MediaPlaybackItem SelectPlayBackItemBySHA256(string sha256) {
            if (ServiceType == MusicServiceType.SongList)
                return SelectItemBySHA256(sha256);
            else
                return SelectMHzItemBySHA256(sha256);
        }

        public MHzSongBase SelectListItemBySHA256(string sha256) {
            if (ServiceType == MusicServiceType.SongList)
                return SelectSongItemBySHA256(sha256);
            else
                return SelectMHzSongItemBySHA256(sha256);
        }

        public bool IsCurrent(MHzSongBase song) {
            if (ServiceType == MusicServiceType.SongList)
                return IsCurrentItem(song);
            else
                return IsCurrentMHzItem(song);
        }

        public bool IsCurrent(MediaPlaybackItem musicItem) {
            if (ServiceType == MusicServiceType.SongList)
                return IsCurrentItem(musicItem);
            else
                return IsCurrentMHzItem(musicItem);
        }

        public bool RemoveItem(MHzSongBase song) {
            if (ServiceType == MusicServiceType.SongList)
                return RemovePlaybackItem(song);
            else
                return RemoveMHzPlaybackItem(song);
        }

        public bool RemoveItem(MediaPlaybackItem item) {
            if (ServiceType == MusicServiceType.SongList)
                return RemovePlaybackItem(item);
            else
                return RemoveMHzPlaybackItem(item);
        }

        public int FindItemIndex(MediaPlaybackItem musicItem) {
            if (ServiceType == MusicServiceType.SongList)
                return FindPlayItemIndex(musicItem);
            else
                return FindMHzItemIndex(musicItem);
        }

        #endregion

        #region SongList Methods

        #region Control

        public void SongListMoveTo(int index = -1) {
            if (index < 0) {
                if (SongPlayList.CurrentItem == SongPlayList.Items[currentInsert])
                    return;
                else
                    SongPlayList.MoveTo((uint)currentInsert);
            } else
                SongPlayList.MoveTo((uint)index);
            PlayAnyway();
        }

        public void SongListMoveTo(MHzSongBase song) {
            var item = SelectItemBySHA256(song.SHA256);
            if (item == null)
                return;
            SongListMoveTo(item);
        }

        public void SongListMoveTo(MediaPlaybackItem item) {
            if (SongPlayList.CurrentItem == item)
                return;
            var index = SongPlayList.Items.ToList().FindIndex(i => i == item);
            SongPlayList.MoveTo(index < 0 ? (uint)currentInsert : (uint)index);
            PlayAnyway();
        }

        private void SongListMoveNext() {
            SongPlayList.MoveNext();
            PlayAnyway();
        }

        private void SongListMovePrevious() {
            SongPlayList.MovePrevious();
            PlayAnyway();
        }

        private int FindPlayItemIndex(MediaPlaybackItem musicItem) {
            return SongPlayList.Items.ToList().FindIndex(i => i.Source.CustomProperties["SHA256"] as string == musicItem.Source.CustomProperties["SHA256"] as string);
        }

        #endregion

        #region Insert With Index

        private async Task<bool> InsertMusicItemAsync(MHzSongBase song, int index = -1) {
            var find_index = SongList.ToList().FindIndex(i => i.SHA256 == song.SHA256);
            if (find_index != -1) {
                currentInsert = find_index;
                return true;
            }
            var item = await MusicServiceHelper.CreatePlayItemAsync(song);
            var retuenIndex = InsertMusicItem(item, index);
            SongList.Insert(retuenIndex, song);
            RaisePropertyChanged("SongList");
            return retuenIndex == -1 ? false : true;
        }

        private int InsertMusicItem(MediaPlaybackItem musicItem, int index = -1) {
            bool succeed = true;
            if (index < 0)
                SongPlayList.Items.Add(musicItem);
            else if (SongPlayList.Items.Count > index)
                SongPlayList.Items.Insert(index, musicItem);
            else if (SongPlayList.Items.Count == index)
                SongPlayList.Items.Add(musicItem);
            else
                succeed = false;
            return succeed ? currentInsert = (index >= 0 ? index : (SongPlayList.Items.Count - 1)) : -1;
        }

        #endregion

        #region Chech If Exist

        private bool IsExistPlayItem(MediaPlaybackItem musicItem) {
            var find_index = SongPlayList.Items.ToList().FindIndex(i => i.Source.CustomProperties["SHA256"] as string == musicItem.Source.CustomProperties["SHA256"] as string);
            return find_index == -1 ? false : true;
        }

        private bool IsExistPlayItem(MHzSongBase song) {
            var find_index = SongList.ToList().FindIndex(i => i.SHA256 == song.SHA256);
            return find_index == -1 ? false : true;
        }

        #endregion

        #region Select Item

        private MediaPlaybackItem SelectItemBySHA256(string sha256) {
            return SongPlayList.Items.ToList().Find(i => i.Source.CustomProperties["SHA256"] as string == sha256);
        }

        private MHzSongBase SelectSongItemBySHA256(string sha256) {
            return SongList.ToList().Find(i => i.SHA256 == sha256);
        }

        #endregion

        #region Check Is Current Or Not

        private bool IsCurrentItem(MHzSongBase song) {
            var item = SelectItemBySHA256(song.SHA256);
            if (item == null)
                return false;
            return IsCurrentItem(item);
        }

        private bool IsCurrentItem(MediaPlaybackItem musicItem) {
            return SongPlayList.CurrentItem == musicItem;
        }

        #endregion

        #region Remove

        private bool RemovePlaybackItem(MHzSongBase song) {
            var item = SelectItemBySHA256(song.SHA256);
            if (item == null)
                return false;
            if (IsCurrentItem(item))
                return false;
            var succeed = SongPlayList.Items.Remove(item);
            if (!succeed)
                return succeed;
            succeed = SongList.Remove(song);
            RaisePropertyChanged("SongList");
            return succeed;
        }

        private bool RemovePlaybackItem(MediaPlaybackItem item) {
            if (IsCurrentItem(item))
                return false;
            var succeed = SongPlayList.Items.Remove(item);
            if (!succeed)
                return succeed;
            var song = SelectSongItemBySHA256(item.Source.CustomProperties["SHA256"] as string);
            if (song == null)
                return false;
            succeed = SongList.Remove(song);
            RaisePropertyChanged("SongList");
            return succeed;
        }

        #endregion

        #endregion

        #region MHzList Methods

        private void MHzListMoveNext() {
            MHzChannelList.MoveNext();
            PlayAnyway();
        }

        private void MHzListMovePrevious() {
            MHzChannelList.MovePrevious();
            PlayAnyway();
        }

        public void MHzListMoveTo(MHzSongBase song) {
            if (IsCurrentMHzItem(song))
                return;
            var item = SelectMHzItemBySHA256(song.SHA256);
            if (item == null)
                return;
            var index = MHzChannelList.Items.ToList().FindIndex(i => i == item);
            if (index < 0)
                return;
            MHzChannelList.MoveTo((uint)index);
            PlayAnyway();
        }

        private int FindMHzItemIndex(MediaPlaybackItem musicItem) {
            return MHzChannelList.Items.ToList().FindIndex(i => i.Source.CustomProperties["SHA256"] as string == musicItem.Source.CustomProperties["SHA256"] as string);
        }

        #region Insert

        private async Task<bool> InsertMHzItemAsync(MHzSongBase song, int index = -1) {
            var find_index = MHzSongList.ToList().FindIndex(i => i.SHA256 == song.SHA256);
            if (find_index != -1)
                return true;
            var item = await MusicServiceHelper.CreatePlayItemAsync(song);
            var retuenIndex = InsertMHzItem(item, index);
            MHzSongList.Insert(retuenIndex, song);
            RaisePropertyChanged("MHzSongList");
            return retuenIndex == -1 ? false : true;
        }

        private int InsertMHzItem(MediaPlaybackItem musicItem, int index = -1) {
            bool succeed = true;
            if (index < 0)
                MHzChannelList.Items.Add(musicItem);
            else if (MHzChannelList.Items.Count > index)
                MHzChannelList.Items.Insert(index, musicItem);
            else if (MHzChannelList.Items.Count == index)
                MHzChannelList.Items.Add(musicItem);
            else
                succeed = false;
            return succeed ? (index >= 0 ? index : (MHzChannelList.Items.Count - 1)) : -1;
        }

        #endregion

        #region Chech If Exist

        public bool IsExistMHzItem(MediaPlaybackItem musicItem) {
            var find_index = MHzChannelList.Items.ToList().FindIndex(i => i.Source.CustomProperties["SHA256"] as string == musicItem.Source.CustomProperties["SHA256"] as string);
            return find_index == -1 ? false : true;
        }

        public bool IsExistMHzItem(MHzSongBase song) {
            var find_index = MHzSongList.ToList().FindIndex(i => i.SHA256 == song.SHA256);
            return find_index == -1 ? false : true;
        }

        #endregion

        #region Select Item

        private MediaPlaybackItem SelectMHzItemBySHA256(string sha256) {
            return MHzChannelList.Items.ToList().Find(i => i.Source.CustomProperties["SHA256"] as string == sha256);
        }

        private MHzSongBase SelectMHzSongItemBySHA256(string sha256) {
            return MHzSongList.ToList().Find(i => i.SHA256 == sha256);
        }

        #endregion

        #region Check Is Current Or Not

        private bool IsCurrentMHzItem(MHzSongBase song) {
            var item = SelectMHzItemBySHA256(song.SHA256);
            if (item == null)
                return false;
            return IsCurrentMHzItem(item);
        }

        private bool IsCurrentMHzItem(MediaPlaybackItem musicItem) {
            return MHzChannelList.CurrentItem == musicItem;
        }

        #endregion

        #region Remove

        private bool RemoveMHzPlaybackItem(MHzSongBase song) {
            var item = SelectMHzItemBySHA256(song.SHA256);
            if (item == null)
                return false;
            if (IsCurrentMHzItem(item))
                return false;
            var succeed = MHzChannelList.Items.Remove(item);
            if (!succeed)
                return succeed;
            succeed = MHzSongList.Remove(song);
            RaisePropertyChanged("MHzSongList");
            return succeed;
        }

        private bool RemoveMHzPlaybackItem(MediaPlaybackItem item) {
            if (IsCurrentMHzItem(item))
                return false;
            var succeed = MHzChannelList.Items.Remove(item);
            if (!succeed)
                return succeed;
            var song = SelectMHzSongItemBySHA256(item.Source.CustomProperties["SHA256"] as string);
            if (song == null)
                return false;
            succeed = MHzSongList.Remove(song);
            RaisePropertyChanged("MHzSongList");
            return succeed;
        }

        #endregion

        #endregion

        #region

        #region Properties and Fields

        int currentInsert;

        MediaPlayer _player;
        MediaPlayer Player { get { return _player ?? (_player = new MediaPlayer()); } }

        public MediaPlaybackSession Session { get { return Player.PlaybackSession; } }

        IList<MediaPlaybackItem> _cachelist;
        IList<MediaPlaybackItem> CacheItems { get { return _cachelist ?? (_cachelist = new List<MediaPlaybackItem>()); } }

        public bool SingletonPlay { get { return Player.IsLoopingEnabled; } set { Player.IsLoopingEnabled = value; } }

        public MusicServicePlayType PlayType { get; set; }

        public MusicServiceType ServiceType { get; set; }

        public int MHzChannelID { get; set; }

        uint _cacheMax = 5;
        public uint CacheMax {
            get { return _cacheMax; }
            set { _cacheMax = value; }
        }

        public Action ActionForMHz { get; set; }

        #region PlayBackList

        MHzSongBase _current;

        MediaPlaybackList _song_play_list;
        MediaPlaybackList SongPlayList { get { return _song_play_list ?? (_song_play_list = new MediaPlaybackList()); } }

        MediaPlaybackList _mhz_channel_list;
        MediaPlaybackList MHzChannelList { get { return _mhz_channel_list ?? (_mhz_channel_list = new MediaPlaybackList()); } }

        public MediaPlaybackList PlaybackList { get { return ServiceType == MusicServiceType.MHz ? MHzChannelList : SongPlayList; } }

        #endregion

        #endregion

        #region Binding properties

        ObservableCollection<MHzSongBase> _songList;
        ObservableCollection<MHzSongBase> SongList { get { return _songList ?? (_songList = new ObservableCollection<MHzSongBase>()); } }

        ObservableCollection<MHzSongBase> _mhzList;
        ObservableCollection<MHzSongBase> MHzSongList { get { return _mhzList ?? (_mhzList = new ObservableCollection<MHzSongBase>()); } }

        public ObservableCollection<MHzSongBase> CurrentSongList { get { return ServiceType == MusicServiceType.MHz ? MHzSongList : SongList; } }

        public MHzSongBase CurrentItem { get { return FindItemIndex(PlaybackList.CurrentItem) >= 0 ? CurrentSongList[FindItemIndex(PlaybackList.CurrentItem)] : null; } }

        #endregion

        #endregion

    }

    public enum MusicServicePlayType { StreamPlay, AutoRepeat, SingletonPlay, ShufflePlay }

    public enum MusicServiceType { MHz, SongList }

    public static class MusicServiceHelper{

        public static DateTime UTCPoint { get { return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc); } }

        public static void InitServiceStyle(this DoubanMusicService service, MusicServiceType type) {
            switch (type) {
                case MusicServiceType.MHz:
                    service.ChangeServiceChoice(type);
                    break;
                case MusicServiceType.SongList:
                    service.ChangeServiceChoice(type);
                    break;
            }
        }

        #region MHz Extensions



        #endregion

        #region SongList Extensions

        public static void ChangePlayChoice(this DoubanMusicService service, bool shuffle, bool autoRepeat, bool singleton) {
            service.PlaybackList.ShuffleEnabled = shuffle;
            service.PlaybackList.AutoRepeatEnabled = autoRepeat;
            service.SingletonPlay = singleton;
        }

        public static void InitPlayStyle(this DoubanMusicService service, MusicServicePlayType type) {
            switch (type) {
                case MusicServicePlayType.ShufflePlay:
                    service.ChangePlayChoice(true, true, false);
                    break;
                case MusicServicePlayType.AutoRepeat:
                    service.ChangePlayChoice(false, true, false);
                    break;
                case MusicServicePlayType.SingletonPlay:
                    service.ChangePlayChoice(false, false, true);
                    break;
                default:
                    service.ChangePlayChoice(false, false, false);
                    break;
            }
        }

        public static MusicServicePlayType ChangePlayStyle(this DoubanMusicService service) {
            var returns = default(MusicServicePlayType);
            switch (service.PlayType) {
                case MusicServicePlayType.ShufflePlay:
                    service.ChangePlayChoice(false, false, false);
                    returns = service.PlayType = MusicServicePlayType.StreamPlay;
                    break;
                case MusicServicePlayType.AutoRepeat:
                    service.ChangePlayChoice(true, true, false);
                    returns = service.PlayType = MusicServicePlayType.ShufflePlay;
                    break;
                case MusicServicePlayType.SingletonPlay:
                    service.ChangePlayChoice(false, true, false);
                    returns = service.PlayType = MusicServicePlayType.AutoRepeat;
                    break;
                default:
                    service.ChangePlayChoice(false, false, true);
                    returns = service.PlayType = MusicServicePlayType.SingletonPlay;
                    break;
            }
            return returns;
        }

        #endregion

        #region Create Playback Item

        public static async Task<MediaPlaybackItem> CreatePlayItemAsync(MHzSongBase song) {
            var succeed_img = Uri.TryCreate(song.Picture, UriKind.Absolute, out var img_url);
            if (!succeed_img)
                img_url = new Uri("ms-appx:///Assets/star006.png");
            return await CreatePlayItemAsync(
                url: song.Url,
                img: img_url,
                title: song.Title,
                artist: song.Artist,
                albumTitle: song.AlbumTitle,
                albunmArtist: song.SingerShow,
                para: AppResources.MusicIsCurrent = new MusicBoardParameter { AID = song.AID, SID = song.SID, SSID = song.SSID, SHA256 = song.SHA256, Url = song.Url , Song = song, });
        }

        public async static Task<MediaPlaybackItem> CreatePlayItemAsync(Uri uri, Uri img, MusicBoardParameter para, string title, string artist, string albumTitle, string albunmArtist) {
            var storage = await StorageHelper.FetchLocalMusicBySHA256Async(MHzSongBaseHelper.GetIdentity(para));
            var source = default(MediaSource);
            source = storage != null ? MediaSource.CreateFromStorageFile(storage) : MediaSource.CreateFromUri(uri);
            source.CustomProperties["Title"] = title;
            source.CustomProperties["CheckPoint"] = SetCheckPoint(UTCPoint);
            source.CustomProperties["SHA256"] = para.SHA256;
            source.CustomProperties["Message"] = para;
            var item = new MediaPlaybackItem(source);
            var properties = item.GetDisplayProperties();
            properties.Type = Windows.Media.MediaPlaybackType.Music;
            properties.Thumbnail = RandomAccessStreamReference.CreateFromUri(img);
            properties.MusicProperties.Title = title;
            properties.MusicProperties.Artist = artist;
            properties.MusicProperties.AlbumTitle = albumTitle;
            properties.MusicProperties.AlbumArtist = albunmArtist;
            item.ApplyDisplayProperties(properties);
            return item;
        }

        public static long SetCheckPoint(DateTime point) {
            return (long)((DateTime.Now - point).TotalMilliseconds);
        }

        public static async Task<MediaPlaybackItem> CreatePlayItemAsync(string url, string img, MusicBoardParameter para, string title, string artist, string albumTitle, string albunmArtist) {
            return await CreatePlayItemAsync(new Uri(url), new Uri(img), para, title, artist, albumTitle, albunmArtist);
        }

        public static async Task<MediaPlaybackItem> CreatePlayItemAsync(string url, Uri img, MusicBoardParameter para, string title, string artist, string albumTitle, string albunmArtist) {
            return await CreatePlayItemAsync(new Uri(url), img, para, title, artist, albumTitle, albunmArtist);
        }

        public static async Task<MediaPlaybackItem> CreatePlayItemAsync(Uri uri, Uri img, MusicBoardParameter para, string title, string artist) {
            return await CreatePlayItemAsync(uri, img, para, title, artist, "", "");
        }

        public static async Task<MediaPlaybackItem> CreatePlayItemAsync(string url, string img, MusicBoardParameter para, string title, string artist) {
            return await CreatePlayItemAsync(new Uri(url), new Uri(img), para, title, artist);
        }

        public static async Task<MediaPlaybackItem> CreatePlayItemAsync(Uri uri, Uri img, MusicBoardParameter para) {
            return await CreatePlayItemAsync(uri, img, para, GetUIString("UnknownMusicTitle"), GetUIString("UnknownMusicArtist"));
        }

        public static async Task<MediaPlaybackItem> CreatePlayItemAsync(string url, string img, MusicBoardParameter para) {
            return await CreatePlayItemAsync(new Uri(url), new Uri(img), para);
        }

        #endregion

    }

}
