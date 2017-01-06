﻿using static Wallace.UWP.Helpers.Tools.UWPStates;
using static Douban.UWP.NET.Resources.AppResources;

using Douban.UWP.Core.Models;
using Douban.UWP.NET.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Wallace.UWP.Helpers;
using Douban.UWP.NET.Tools;
using Douban.UWP.Core.Tools;
using Newtonsoft.Json.Linq;
using Douban.UWP.Core.Models.LifeStreamModels;
using System.Threading.Tasks;
using System.Reflection;

namespace Douban.UWP.NET.Pages {

    public sealed partial class UserInfoPage : BaseContentPage {
        public UserInfoPage() {
            this.InitializeComponent();
        }

        protected override void InitPageState() {
            base.InitPageState();
            GlobalHelpers.SetChildPageMargin(this, matchNumber: VisibleWidth, isDivideScreen: IsDivideScreen);
            GlobalHelpers.DivideWindowRange(this, DivideNumber, isDivideScreen: IsDivideScreen);
        }

        public override void DoWorkWhenAnimationCompleted() {
            AdaptToClearContentAfterOnBackPressed();
        }

        public override void PageSlideOutStart(bool isToLeft) {
            if (DetailsFrame.Content != null) {
                var pg = DetailsFrame.Content;
                if (pg.GetType().GetTypeInfo().BaseType.Name == typeof(BaseContentPage).Name)
                    (pg as BaseContentPage).PageSlideOutStart(isToLeft);
                else
                    DetailsFrame.Content = null;
                return;
            }
            base.PageSlideOutStart(isToLeft);
        }

        #region Events

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            DoubanLoading.SetVisibility(false);
            UserNameBlock.Text = LoginStatus.UserName;
            LocationBlock.Text = LoginStatus.LocationString;
            DescriptionBlock.Text = LoginStatus.Description;
        }

        private async void RelativePanel_LoadedAsync(object sender, RoutedEventArgs e) {
            UserInfoDetails = this.DetailsFrame;
            UserInfoPopup = this.InnerContentPanel;
            if (LoginStatus.APIUserinfos != null)
                await SetStateByLoginStatusAsync();
        }

        private void BaseHamburgerButton_Click(object sender, RoutedEventArgs e) {
            PageSlideOutStart(VisibleWidth > 800 ? false : true);
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e) {
            GlobalHelpers.SetChildPageMargin(this, matchNumber: VisibleWidth, isDivideScreen: IsDivideScreen);
            AdaptForScreenSize();
        }

        private void TalkButton_Click(object sender, RoutedEventArgs e) {

        }

        private void WatchButton_Click(object sender, RoutedEventArgs e) {

        }

        private void FlowButton_Click(object sender, RoutedEventArgs e) {

        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            RunButtonClick((sender as Button).Name);
            OpenAllComsBtn.SetVisibility(true);
            OpenInnerContent();
        }

        private async void LogoutButton_ClickAsync(object sender, RoutedEventArgs e) {
            var path = "https://www.douban.com/accounts/logout?source=main";
            await DoubanWebProcess.GetDoubanResponseAsync(path);
            SettingsHelper.SaveSettingsValue(SettingsSelect.UserID, "LOGOUT");
            GlobalHelpers.ResetLoginStatus();
            PageSlideOutStart(VisibleWidth > 800 ? false : true);
        }

        private void ContentList_ItemClick(object sender, ItemClickEventArgs e) {
            var iten = e.ClickedItem as LifeStreamItem;
            if (iten == null)
                return;
            Uri.TryCreate(iten.PathUrl, UriKind.RelativeOrAbsolute, out var uri);
            NavigateToBase?.Invoke(
                null, 
                new NavigateParameter { Title = iten.Title, ToUri = uri, IsFromInfoClick = true }, 
                DetailsFrame,
                GetPageType(NavigateType.InfoItemClick));
        }

        private void PopupAllComments_SizeChanged(object sender, SizeChangedEventArgs e) {
            InnerGrid.Width = (sender as Popup).ActualWidth;
            InnerGrid.Height = (sender as Popup).ActualHeight;
        }

        private void CloseAllComsBtn_Click(object sender, RoutedEventArgs e) {
            InnerContentPanel.IsOpen = false;
        }

        private void OpenAllComsBtn_Click(object sender, RoutedEventArgs e) {
            OpenInnerContent();
        }

        private void PopupAllComments_Closed(object sender, object e) {
            OutPopupBorder.Completed += OnOutPopupBorderOut;
            OutPopupBorder.Begin();
        }

        private void OnOutPopupBorderOut(object sender, object e) {
            OutPopupBorder.Completed -= OnOutPopupBorderOut;
            PopupBackBorder.SetVisibility(false);
        }

        private void Scroll_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e) {
            try {
                if ((sender as ScrollViewer).VerticalOffset <= 300)
                    TitleBackRec.Opacity = ((sender as ScrollViewer).VerticalOffset) / 300;
                else if (TitleBackRec.Opacity < 1)
                    TitleBackRec.Opacity = 1;
            } catch { /* Ignore */ }
        }

        #endregion

        #region Methods

        private async Task SetStateByLoginStatusAsync() {
            var status = LoginStatus.APIUserinfos;
            BroadcastNumber.Text = status.StatusesCount.ToString();
            PhotosNumber.Text = status.PhotoAlbumsCount.ToString();
            DiaryNumber.Text = status.NotesCount.ToString();
            GroupsNumber.Text = status.JoinedGroupCount.ToString();
            BookMovieNumber.Text = status.CollectedSubjectsCount.ToString();
            FollowingNumber.Text = status.FollowingCount.ToString();
            FollowersNumber.Text = status.FollowersCount.ToString();
            GenderBlock.Foreground = status.Gender == "M" ?
                new SolidColorBrush(Windows.UI.Color.FromArgb(255, 69, 90, 172)) :
                new SolidColorBrush(Windows.UI.Color.FromArgb(255, 217, 6, 94));
            HeadUserImage.Fill = new ImageBrush { ImageSource = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(LoginStatus.BigHeadUrl)) };
            if (status.ProfileBannerLarge != null)
                BackgroundImage.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(LoginStatus.APIUserinfos.ProfileBannerLarge));
            await SetListResourcesAsync(status.UserUid);
        }

        #region Adapt Methods

        private void AdaptForHightMobileMode() {
            HeadContainerStack.Children.Remove(U_L_GRID);
            DescriptionGrid.Children.Add(U_L_GRID);
            Grid.SetColumn(BTN_GRID, 1);
            Grid.SetColumnSpan(BTN_GRID, 1);
            U_L_GRID.HorizontalAlignment = HorizontalAlignment.Stretch;
            UserNameBlock.Foreground = IsGlobalDark ? new SolidColorBrush(Windows.UI.Colors.White) : new SolidColorBrush(Windows.UI.Colors.Black);
        }

        private void AdaptForWidePCMode() {
            DescriptionGrid.Children.Remove(U_L_GRID);
            HeadContainerStack.Children.Add(U_L_GRID);
            Grid.SetColumn(BTN_GRID, 0);
            Grid.SetColumnSpan(BTN_GRID, 2);
            U_L_GRID.HorizontalAlignment = HorizontalAlignment.Center;
            UserNameBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
        }

        #endregion

        #region Set Infos List

        private async Task SetListResourcesAsync(string uid) {
            var newList = new List<LifeStreamItem>();
            try {
                var items = JObject.Parse(await APIForFetchLifeStreamAsync(uid))["items"];
                if (items.HasValues) 
                    items.Children().ToList().ForEach(singleton => AddEverySingleton(singleton, newList)); 
                ListResources.Source = newList.OrderByDescending(i => i.TimeForOrder);
            } finally { IncrementalLoadingBorder.SetVisibility(false); }
        }

        private async Task<string> APIForFetchLifeStreamAsync(string uid) {
            return await DoubanWebProcess.GetMDoubanResponseAsync(
                path: string.Format(APIFormat, uid, DateTime.Now.ToString("yyyy-M"), 20),
                host: "m.douban.com",
                reffer: string.Format("https://m.douban.com/people/{0}/", uid));
        }

        private void AddEverySingleton(JToken singleton, List<LifeStreamItem> newList) {
            try {
                var type = InitLifeStreamType(singleton);
                var itemToAdd = InitLifeStreamItem(singleton, type);
                SetSpecialContent(newList, singleton["content"], type, itemToAdd);
            } catch { /* Ignore */}
        }

        #region Set Comment Content

        private void SetSpecialContent(List<LifeStreamItem> newList, JToken content, InfosItemBase.JsonType type, LifeStreamItem itemToAdd) {
            try {
                switch (type) {
                    case InfosItemBase.JsonType.Status:
                        SetStatusContent(content, itemToAdd);
                        break;
                    case InfosItemBase.JsonType.Article:
                        SetArticleContent(content, itemToAdd);
                        break;
                    case InfosItemBase.JsonType.Card:
                        SetCardContent(content, itemToAdd);
                        break;
                    case InfosItemBase.JsonType.Album:
                        SetAlbumContent(content, itemToAdd);
                        break;
                    case InfosItemBase.JsonType.Undefined:
                        break;
                }
            } finally { newList.Add(itemToAdd); }
        }

        private InfosItemBase.JsonType InitLifeStreamType(JToken singleton) {
            return singleton["type"].Value<string>() == "card" ? InfosItemBase.JsonType.Card :
            singleton["type"].Value<string>() == "status" ? InfosItemBase.JsonType.Status :
            singleton["type"].Value<string>() == "album" ? InfosItemBase.JsonType.Album :
            singleton["type"].Value<string>() == "article" ? InfosItemBase.JsonType.Article :
            InfosItemBase.JsonType.Undefined;
        }

        private LifeStreamItem InitLifeStreamItem(JToken singleton, InfosItemBase.JsonType type) {
            double time = default(double);
            try { time = (DateTime.Parse(singleton["time"].Value<string>())- new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds; } catch { time = 0 ; }
            return new LifeStreamItem {
                Type = type,
                LikersCounts = singleton["likers_count"].Value<string>(),
                Time = singleton["time"].Value<string>(),
                Uri = singleton["uri"].Value<string>(),
                PathUrl = singleton["url"].Value<string>(),
                CommentsCounts = singleton["comments_count"].Value<string>(),
                Activity = singleton["activity"].Value<string>(),
                TimeForOrder = time,
            };
        }

        #endregion

        #region Set Special Content

        private void SetCardContent(JToken content, LifeStreamItem item) {
            AddCover(content, item);
            item.Title = content["title"].Value<string>();
            item.Description = content["description"].Value<string>();
            item.Text = content["text"].Value<string>();
        }

        private void SetArticleContent(JToken content, LifeStreamItem item) {
            AddCover(content, item);
            item.Abstract = content["abstract"].Value<string>();
            item.Title = content["title"].Value<string>();
        }

        private void SetStatusContent(JToken content, LifeStreamItem item) {
            item.Text = content["text"].Value<string>();
            item.Images = new List<PictureItemBase>();
            var images = content["images"];
            if (images.HasValues) {
                item.HasImages = true;
                images.Children().ToList().ForEach(each => item.Images.Add(CreatePictureBaseItem(each)));
            } else {
                item.HasImages = false;
                item.Images.Add(CreateNoPictureBase());
            }
        }

        private void SetAlbumContent(JToken content, LifeStreamItem item) {
            item.AlbumList = new List<PictureItem>();
            var photos = content["photos"];
            if (photos.HasValues) {
                item.HasAlbum = true;
                photos.Children().ToList().ForEach(singleton => item.AlbumList.Add(CreatePictureSingleton(item, singleton)));
            } else {
                item.HasAlbum = false;
                item.Images.Add(CreateNoPictureSingleton());
            }
        }

        #endregion

        #region Details

        private void AddCover(JToken content, LifeStreamItem item) {
            item.HasCover = content["cover_url"].Value<string>() != "" ? true : false;
            item.Cover = content["cover_url"].Value<string>() != "" ? new Uri(content["cover_url"].Value<string>()) : new Uri(NoPictureUrl);
        }

        private PictureItemBase CreateNoPictureBase() {
            return new PictureItemBase {
                Normal = new Uri(NoPictureUrl),
                Large = new Uri(NoPictureUrl),
            };
        }

        private PictureItem CreateNoPictureSingleton() {
            return new PictureItem {
                Normal = new Uri(NoPictureUrl),
                Large = new Uri(NoPictureUrl),
                Small = new Uri(NoPictureUrl),
            };
        }

        private PictureItemBase CreatePictureBaseItem(JToken each) {
            var normal = each["normal"];
            var large = each["large"];
            return new PictureItemBase {
                Normal = new Uri((normal != null && normal.HasValues) ? normal["url"].Value<string>() : NoPictureUrl),
                Large = new Uri((large != null && large.HasValues) ? large["url"].Value<string>() : NoPictureUrl),
            };
        }

        private PictureItem CreatePictureSingleton(LifeStreamItem item, JToken singleton) {
            var author = singleton["author"];
            var picItm = InitPictureItem(singleton, singleton["image"]);
            if (author.HasValues)
                picItm.Author = InitAuthorStatus(author, author["loc"]);
            return picItm;
        }

        private AuthorStatus InitAuthorStatus(JToken author, JToken location) {
             return new AuthorStatus {
                Kind = author["kind"].Value<string>(),
                Name = author["name"].Value<string>(),
                Url = author["url"].Value<string>(),
                Gender = author["gender"].Value<string>(),
                Abstract = author["abstract"].Value<string>(),
                Uri = author["uri"].Value<string>(),
                Avatar = author["avatar"].Value<string>(),
                LargeAvatar = author["large_avatar"].Value<string>(),
                Type = author["type"].Value<string>(),
                ID = author["id"].Value<string>(),
                Uid = author["uid"].Value<string>(),
                LocationID = location.HasValues ? location["id"].Value<string>() : null,
                LocationName = location.HasValues ? location["name"].Value<string>() : null,
                LocationUid = location.HasValues ? location["uid"].Value<string>() : null,
            };
        }

        private PictureItem InitPictureItem(JToken singleton, JToken images) {
            return new PictureItem {
                Liked = singleton["liked"].Value<bool>(),
                Description = singleton["description"].Value<string>(),
                LikersCount = singleton["likers_count"].Value<string>(),
                Uri = singleton["uri"].Value<string>(),
                Url = singleton["url"].Value<string>(),
                CreateTime = singleton["create_time"].Value<string>(),
                CommentsCount = singleton["comments_count"].Value<string>(),
                AllowComment = singleton["allow_comment"].Value<bool>(),
                Position = singleton["position"].Value<int>(),
                OwnedUri = singleton["owner_uri"].Value<string>(),
                Type = singleton["type"].Value<string>(),
                Id = singleton["id"].Value<string>(),
                SharingUrl = singleton["sharing_url"].Value<string>(),
                Small = new Uri(images.HasValues && images["small"].HasValues ? images["small"]["url"].Value<string>() : NoPictureUrl),
                Normal = new Uri(images.HasValues && images["normal"].HasValues ? images["normal"]["url"].Value<string>() : NoPictureUrl),
                Large = new Uri(images.HasValues && images["large"].HasValues ? images["large"]["url"].Value<string>() : NoPictureUrl),
            };
        }

        #endregion

        #endregion

        private void AdaptToClearContentAfterOnBackPressed() {
            if (VisibleWidth > 800) {
                if (IsDivideScreen)
                    MainContentFrame.Navigate(typeof(MetroPage));
                else
                    MainContentFrame.Content = null;
            } else
                MainContentFrame.Content = null;
        }

        private void AdaptForScreenSize() {
            if (VisibleWidth > 800) {
                if (DescriptionGrid.Children.Contains(U_L_GRID))
                    AdaptForWidePCMode();
            } else {
                if (HeadContainerStack.Children.Contains(U_L_GRID))
                    AdaptForHightMobileMode();
            }
        }

        public void OpenInnerContent() {
            InnerContentPanel.IsOpen = true;
            PopupBackBorder.SetVisibility(true);
            EnterPopupBorder.Begin();
        }

        public string  CreateAPITargetByUid(string format, string uid) {
            return string.Format(format, uid);
        }

        public void Navigate() {
            ContentFrame.Navigate(typeof(MyStatusPage));
        }

        #endregion

        #region Properties and state

        private enum ContentType { None = 0, String = 1, Image = 2, Gif = 3, Video = 4, Flash = 5, SelfUri = 6 }

        private void RunButtonClick(string name) { if (EventMap.ContainsKey(name)) EventMap[name].Invoke(); }
        private IDictionary<string, Action> eventMap;
        private IDictionary<string, Action> EventMap {
            get {
                return eventMap ?? new Func<IDictionary<string, Action>>(() => {
                    return eventMap = new Dictionary<string, Action> {
                        {BroadcastButton.Name, Navigate},
                    };
                }).Invoke();
            }
        }

        private const string APIFormat = "https://m.douban.com/rexxar/api/v2/user/{0}/lifestream?slice=recent-{1}&hot=false&filter_after=&count={2}&for_mobile=1";
        private const string StatusAPIFormat = "https://m.douban.com/rexxar/api/v2/status/user_timeline/{0}?for_mobile=1";
        private const string NoPictureUrl = "https://www.none-wallace-767fc6vh7653df0jb.com/no_wallace_085sgdfg7447fddds65.jpg";

        #endregion
    }
}
