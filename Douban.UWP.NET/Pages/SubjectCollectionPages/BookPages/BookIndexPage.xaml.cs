﻿using static Wallace.UWP.Helpers.Tools.UWPStates;
using static Douban.UWP.NET.Resources.AppResources;

using Douban.UWP.Core.Models.ListModel;
using Douban.UWP.Core.Tools;
using Douban.UWP.NET.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json.Linq;
using Windows.UI;
using Douban.UWP.Core.Models;
using System.Text.RegularExpressions;

namespace Douban.UWP.NET.Pages {

    public sealed partial class BookIndexPage : Page {
        public BookIndexPage() {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            InitWhenNavigatedAsync();
        }

        private async void InitWhenNavigatedAsync() {
            var BookFictionResult = await SetGridViewResourcesAsync("book_fiction");
            BookFictionResources.Source = BookFictionResult?.Items;
            var BookNonfictionResult = await SetGridViewResourcesAsync("book_nonfiction");
            BookNonfictionResources.Source = BookNonfictionResult?.Items;
            var MPBookResult = await SetGridViewResourcesAsync("market_product_book");
            MPBookResources.Source = MPBookResult?.Items;
            var webResult = await DoubanWebProcess.GetMDoubanResponseAsync("https://m.douban.com/book/");
            SetWrapPanelResources(webResult);
            SetFilterResources(webResult);
            StopLoadingAnimation();
        }

        private void SetWrapPanelResources(string webResult) {
            if (webResult == null)
                return;
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(webResult);
            var wrapList = new List<ItemGroup<BookItem>>();
            var lis = doc.DocumentNode
                    .SelectSingleNode("//section[@class='interests']")
                    .SelectSingleNode("div[@class='section-content']")
                    .SelectSingleNode("ul")
                    .SelectNodes("li");
            lis.ToList().ForEach(singleton => {
                var actionL = singleton.SelectSingleNode("a");
                if (actionL != null)
                    wrapList.Add(new ItemGroup<BookItem> { GroupName = actionL.InnerText, GroupPathUrl = actionL.Attributes["href"].Value, });
            });
            var randomer = new Random();
            wrapList.ForEach(i => {
                var color = GlobalHelpers.GetColorRandom((randomer.Next())%15);
                var button = new Button { Content = i.GroupName, Background = new SolidColorBrush(Colors.Transparent), Foreground = color };
                button.Click += (obj, args) => NavigateToBase?.Invoke(
                    null,
                    new NavigateParameter { ToUri = new Uri(i.GroupPathUrl), Title = i.GroupName },
                    GetFrameInstance(FrameType.Content),
                    GetPageType(NavigateType.DouList));
                WrapPanel.Children.Add(new Border {
                    CornerRadius = new CornerRadius(3),
                    BorderBrush = color,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(3),
                    Child = button
                });
            });
        }

        private void SetFilterResources(string webResult) {
            if (webResult == null)
                return;
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(webResult);
            var filterList = new List<ItemGroup<BookItem>>();
            var lis = doc.DocumentNode
                    .SelectSingleNode("//section[@class='types']")
                    .SelectSingleNode("div[@class='section-content']")
                    .SelectSingleNode("ul[@class='type-list']")
                    .SelectNodes("li");
            lis.ToList().ForEach(singleton => {
                var actionL = singleton.SelectSingleNode("a");
                if (actionL != null)
                    filterList.Add(new ItemGroup<BookItem> { GroupName = actionL.InnerText, GroupPathUrl = "https://m.douban.com" + actionL.Attributes["href"].Value, });
            });
            FilterResources.Source = filterList;
        }

        private async Task<ItemGroup<BookItem>> SetGridViewResourcesAsync(string groupName) {
            return await FetchMessageFromAPIAsync(
                group: groupName,
                count: 13,
                loc_id: GetLocalUid()??"108288");
        }

        private static string GetLocalUid() {
            return IsLogined ? LoginStatus.APIUserinfos?.LocationUid : "108288";
        }

        private async Task<ItemGroup<BookItem>> FetchMessageFromAPIAsync(
            string group,
            string loc_id = "108288",
            uint start = 0, 
            uint count = 8, 
            int offset = 0) {
            var gmodel = default(ItemGroup<BookItem>);
            try {
                var minised = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
                var result = await BeansproutRequestHelper.FetchTypeCollectionList(group, loc_id, start, count, minised, SubjectType.Books, RequestType.SubjectCollection);
                if (result == null) {
                    ReportWhenGoesWrong("WebActionError");
                    return gmodel;
                }
                JObject jo = JObject.Parse(result);
                gmodel = DataProcess.SetGroupResources(jo, gmodel);
                gmodel = SetSingletonResources(jo, gmodel);
            } catch { ReportHelper.ReportAttentionAsync(GetUIString("FetchJsonDataError")); }
            IncrementalLoadingBorder.SetVisibility(false);
            return gmodel;
        }

        private ItemGroup<BookItem> SetSingletonResources(JObject jObject, ItemGroup<BookItem> gModel) {
            var header = jObject["header"];
            if (header != null && header.HasValues) 
                DataProcess.SetEachSingleton(gModel, header);
            var feeds = jObject["subject_collection_items"];
            if (feeds == null || !feeds.HasValues) 
                return gModel;
            if (feeds.HasValues)
                feeds.Children().ToList().ForEach(singleton => DataProcess.SetEachSingleton(gModel, singleton));
            return gModel;
        }

        private void ReportWhenGoesWrong(string UIString) {
            ReportHelper.ReportAttentionAsync(GetUIString(UIString));
        }

        private void StopLoadingAnimation() {
            IncrementalLoadingBorder.SetVisibility(false);
        }

        private void MoreButton_Click(object sender, RoutedEventArgs e) {
            var path = (sender as Button).CommandParameter as string;
            if (path == null)
                return;
            var is_not_native = path.Contains("https:");
            var keyword = is_not_native ? path : $"subject_collection/{path}/";
            NavigateToBase?.Invoke( // change loc_id to adjust location.
                null,
                new NavigateParameter {
                    ToUri = is_not_native ? new Uri(path + $"?loc_id=108288") : null,
                    ApiHeadString = keyword,
                    Title = GetUIString("DB_BOOK") },
                GetFrameInstance(FrameType.Content),
                GetPageType(is_not_native ? NavigateType.Undefined : NavigateType.BookFilter));
        }

        private void GridView_ItemClick(object sender, ItemClickEventArgs e) {
            var item = e.ClickedItem as BookItem;
            if (item == null || item.PathUrl == null)
                return;
            NavigateToBase?.Invoke(
                null,
                new NavigateParameter { ToUri = new Uri(item.PathUrl), Title = item.Title },
                GetFrameInstance(FrameType.Content),
                GetPageType(NavigateType.BookContent));
        }

        private void FilterGridView_ItemClick(object sender, ItemClickEventArgs e) {
            var item = e.ClickedItem as ItemGroup<BookItem>;
            if (item == null || item.GroupPathUrl == null)
                return;
            var keyword = new Regex(@"/book/(?<key_word>.+)").Match(item.GroupPathUrl).Groups["key_word"].Value;
            if (keyword != "")
                keyword = UriDecoder.EditKeyWordsForFilter(keyword);
            NavigateToBase?.Invoke(
                null,
                new NavigateParameter {
                    ToUri = new Uri(item.GroupPathUrl),
                    ApiHeadString = keyword,
                    Title = item.GroupName
                },
                GetFrameInstance(FrameType.Content),
                GetPageType(NavigateType.BookFilter));
        }

        private void StackPanel_SizeChanged(object sender, SizeChangedEventArgs e) {
            WrapPanel.Width = (sender as StackPanel).ActualWidth;
        }

        #region Properties

        #endregion
        
    }
}
