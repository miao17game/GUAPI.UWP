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

    public sealed partial class TVIndexPage : Page {
        public TVIndexPage() {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            InitWhenNavigatedAsync();
        }

        private async void InitWhenNavigatedAsync() {
            var TVDomesticResult = await SetGridViewResourcesAsync("tv_domestic");
            TVDomesticResources.Source = TVDomesticResult != null ? TVDomesticResult.Items : null;
            var TVVarietyShowResult = await SetGridViewResourcesAsync("tv_variety_show");
            TVVarietyShowResources.Source = TVVarietyShowResult != null ? TVVarietyShowResult.Items : null;
            var TVAmericanResult = await SetGridViewResourcesAsync("tv_american");
            TVAmericanResources.Source = TVAmericanResult != null ? TVAmericanResult.Items : null;
            var webResult = await DoubanWebProcess.GetMDoubanResponseAsync("https://m.douban.com/tv/");
            SetWrapPanelResources(webResult);
            SetFilterResources(webResult);
            StopLoadingAnimation();
        }

        private void SetWrapPanelResources(string webResult) {
            if (webResult == null)
                return;
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(webResult);
            var wrapList = new List<ItemGroup<MovieItem>>();
            var lis = doc.DocumentNode
                    .SelectSingleNode("//section[@class='interests']")
                    .SelectSingleNode("div[@class='section-content']")
                    .SelectSingleNode("ul")
                    .SelectNodes("li");
            lis.ToList().ForEach(singleton => {
                var actionL = singleton.SelectSingleNode("a");
                if (actionL != null)
                    wrapList.Add(new ItemGroup<MovieItem> { GroupName = actionL.InnerText, GroupPathUrl = actionL.Attributes["href"].Value, });
            });
            var randomer = new Random();
            wrapList.ForEach(i => {
                var color = GlobalHelpers.GetColorRandom((randomer.Next())%15);
                var button = new Button { Content = i.GroupName, Background = new SolidColorBrush(Colors.Transparent), Foreground = color };
                button.Click += (obj, args) => NavigateToBase?.Invoke(
                    null,
                    new NavigateParameter { ToUri = new Uri(i.GroupPathUrl) , Title = i.GroupName},
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
            var filterList = new List<ItemGroup<MovieItem>>();
            var lis = doc.DocumentNode
                    .SelectSingleNode("//section[@class='types']")
                    .SelectSingleNode("div[@class='section-content']")
                    .SelectSingleNode("ul[@class='type-list']")
                    .SelectNodes("li");
            lis.ToList().ForEach(singleton => {
                var actionL = singleton.SelectSingleNode("a");
                if (actionL != null)
                    filterList.Add(new ItemGroup<MovieItem> { GroupName = actionL.InnerText, GroupPathUrl = "https://m.douban.com" + actionL.Attributes["href"].Value, });
            });
            FilterResources.Source = filterList;
        }

        private async Task<ItemGroup<MovieItem>> SetGridViewResourcesAsync(string groupName) {
            return await FetchMessageFromAPIAsync(
                group: groupName,
                count: 13,
                loc_id: GetLocalUid());
        }

        private static string GetLocalUid() {
            return IsLogined ? LoginStatus.APIUserinfos?.LocationUid : "108288";
        }

        private async Task<ItemGroup<MovieItem>> FetchMessageFromAPIAsync(
            string group,
            string loc_id = "108288",
            uint start = 0,
            uint count = 8,
            int offset = 0) {
            var gmodel = default(ItemGroup<MovieItem>);
            try {
                var minised = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
                var result = await BeansproutRequestHelper.FetchTypeCollectionList(group, loc_id, start, count, minised, SubjectType.TVs, RequestType.SubjectCollection);
                if (result == null) {
                    ReportWhenGoesWrong("WebActionError");
                    return gmodel;
                }
                JObject jo = JObject.Parse(result);
                gmodel = SetGroupResources(jo, gmodel);
                gmodel = SetSingletonResources(jo, gmodel);
            } catch { ReportHelper.ReportAttentionAsync(GetUIString("UnknownError")); }
            IncrementalLoadingBorder.SetVisibility(false);
            return gmodel;
        }

        private ItemGroup<MovieItem> SetGroupResources(JObject jObject, ItemGroup<MovieItem> gModel) {
            var sub_collection = jObject["subject_collection"];
            if (sub_collection == null || !sub_collection.HasValues) {
                ReportWhenGoesWrong("FetchJsonDataError");
                return gModel;
            }
            try {
                gModel = DataProcess.SetGroupItem<MovieItem>(jObject, sub_collection);
            } catch { /* Ignore, item error. */ }
            return gModel;
        }

        private ItemGroup<MovieItem> SetSingletonResources(JObject jObject, ItemGroup<MovieItem> gModel) {
            var feeds = jObject["subject_collection_items"];
            if (feeds == null || !feeds.HasValues) {
                ReportWhenGoesWrong("FetchJsonDataError");
                return gModel;
            }
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
            var keyword = $"subject_collection/{path}/";
            NavigateToBase?.Invoke( // change loc_id to adjust location.
                null,
                new NavigateParameter { ToUri = null, ApiHeadString = keyword, Title = GetUIString("DB_MORE") },
                GetFrameInstance(FrameType.Content),
                GetPageType(NavigateType.TVFilter));
        }

        private void GridView_ItemClick(object sender, ItemClickEventArgs e) {
            var item = e.ClickedItem as MovieItem;
            if (item == null || item.PathUrl == null)
                return;
            NavigateToBase?.Invoke(
                null,
                new NavigateParameter { ToUri = new Uri(item.PathUrl), Title = item.Title },
                GetFrameInstance(FrameType.Content),
                GetPageType(NavigateType.TVContent));
        }

        private void FilterGridView_ItemClick(object sender, ItemClickEventArgs e) {
            var item = e.ClickedItem as ItemGroup<MovieItem>;
            if (item == null || item.GroupPathUrl == null)
                return;
            var keyword = new Regex(@"/tv/(?<key_word>.+)").Match(item.GroupPathUrl).Groups["key_word"].Value;
            if (keyword != "")
                keyword = UriDecoder.EditKeyWordsForTVFilter(keyword, "tv");
            NavigateToBase?.Invoke(
                null,
                new NavigateParameter { ToUri = new Uri(item.GroupPathUrl), ApiHeadString = keyword, Title = item.GroupName },
                GetFrameInstance(FrameType.Content),
                GetPageType(NavigateType.TVFilter));
        }

        private void StackPanel_SizeChanged(object sender, SizeChangedEventArgs e) {
            WrapPanel.Width = (sender as StackPanel).ActualWidth;
        }

        #region Properties

        #endregion
        
    }
}
