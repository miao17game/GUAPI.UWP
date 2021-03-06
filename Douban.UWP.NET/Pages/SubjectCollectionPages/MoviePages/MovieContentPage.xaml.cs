﻿using static Wallace.UWP.Helpers.Tools.UWPStates;
using static Douban.UWP.NET.Resources.AppResources;

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
using Douban.UWP.Core.Models;
using HtmlAgilityPack;
using Douban.UWP.Core.Tools;
using Douban.UWP.NET.Tools;
using Douban.UWP.NET.Models;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Web.Http;
using Windows.Storage.Streams;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using System.Text.RegularExpressions;
using System.Text;
using Wallace.UWP.Helpers;

namespace Douban.UWP.NET.Pages.SubjectCollectionPages.MoviePages {

    public sealed partial class MovieContentPage : BaseContentPage {
        public MovieContentPage() {
            this.InitializeComponent();
        }

        #region Events

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            if (!(e.Parameter is NavigateParameter))
                return;
            var args = e.Parameter as NavigateParameter;
            if (isFromInfoClick = args.IsFromInfoClick)
                GlobalHelpers.SetChildPageMargin(this, matchNumber: VisibleWidth, isDivideScreen: IsDivideScreen);
            frameType = args.FrameType;
            movie_id_received = args.SpecialParameter as string;
            SetWebViewSourceAsync(currentUri = args.ToUri);
        }

        private void BaseHamburgerButton_Click(object sender, RoutedEventArgs e) {
            PageSlideOutStart(VisibleWidth > FormatNumber ? false : true);
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e) {
            model = this.DataContext as MovieContentVM;
        }

        private void FullContentBtn_Click(object sender, RoutedEventArgs e) {
            NavigateToBase?.Invoke(
                null,
                new NavigateParameter { FrameType = FrameType.UpContent, ToUri = currentUri, Title = GetUIString("LinkContent") },
                GetFrameInstance(FrameType.UpContent),
                GetPageType(NavigateType.Undefined));
        }

        private void WishBtn_Click(object sender, RoutedEventArgs e) {
            ReportHelper.ReportAttentionAsync(GetUIString("StillInDeveloping"));
        }

        private void CollectBtn_Click(object sender, RoutedEventArgs e) {
            ReportHelper.ReportAttentionAsync(GetUIString("StillInDeveloping"));
        }

        private void ImageGridView_ItemClick(object sender, ItemClickEventArgs e) {
            var path = e.ClickedItem as string;
            if (path == null)
                return;
            ShowImageInScreen(new Uri(path));
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e) {
            GlobalHelpers.SetChildPageMargin(this, matchNumber: VisibleWidth, isDivideScreen: IsDivideScreen);
            BackImage.Height = grid.ActualHeight;
        }

        private async void ImageSaveButton_ClickAsync(object sender, RoutedEventArgs e) {
            SoftwareBitmap sb = await DownloadImageAsync((sender as Button).CommandParameter as string);
            if (sb != null) {
                SoftwareBitmapSource source = new SoftwareBitmapSource();
                await source.SetBitmapAsync(sb);
                var name = await WriteToFileAsync(sb);
                new ToastSmooth(GetUIString("SaveImageSuccess")).Show();
            }
        }

        private void ImageControlButton_Click(object sender, RoutedEventArgs e) {
            ImagePopup.IsOpen = false;
        }

        private void ImagePopup_SizeChanged(object sender, SizeChangedEventArgs e) {
            ImagePopupBorder.Width = (sender as Popup).ActualWidth;
            ImagePopupBorder.Height = (sender as Popup).ActualHeight;
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e) {
            var item = e.ClickedItem as MovieContentQuestion;
            if (item == null)
                return;
            NavigateToBase?.Invoke(null,
                new NavigateParameter {
                    ToUri = new Uri(item.PathUrl),
                    FrameType =
                    frameType == FrameType.Content ?
                    FrameType.UpContent :
                    FrameType.UserInfos,
                    Title = item.Title
                },
                GetFrameInstance(frameType == FrameType.Content ?
                FrameType.UpContent :
                FrameType.UserInfos),
                GetPageType(NavigateType.MovieContentQuestion));
        }

        private void CommentListView_ItemClick(object sender, ItemClickEventArgs e) {

        }

        private void RecommandGridView_ItemClick(object sender, ItemClickEventArgs e) {
            var item = e.ClickedItem as MovieContentRecommand;
            if (item == null)
                return;
            NavigateToBase?.Invoke(null,
                new NavigateParameter {
                    ToUri = new Uri(item.PathUrl),
                    FrameType =
                    frameType == FrameType.Content ?
                    FrameType.UpContent :
                    FrameType.UserInfos,
                    Title = item.Title,
                    IsNative = true
                },
                GetFrameInstance(frameType == FrameType.Content ?
                FrameType.UpContent :
                FrameType.UserInfos),
                GetPageType(NavigateType.MovieContent));
        }

        private void ReviewsListView_ItemClick(object sender, ItemClickEventArgs e) {
            var item = e.ClickedItem as MovieContentReview;
            if (item == null)
                return;
            NavigateToBase?.Invoke(null,
                new NavigateParameter {
                    ToUri = new Uri(item.PathUrl),
                    FrameType =
                    frameType == FrameType.Content ?
                    FrameType.UpContent :
                    FrameType.UserInfos,
                    Title = item.Title
                },
                GetFrameInstance(frameType == FrameType.Content ?
                FrameType.UpContent :
                FrameType.UserInfos),
                GetPageType(NavigateType.MovieContentQuestion));
        }

        #endregion

        #region Methods

        protected override void InitPageState() {
            base.InitPageState();
            GlobalHelpers.DivideWindowRange(this, DivideNumber, isDivideScreen: IsDivideScreen);
        }

        public override void DoWorkWhenAnimationCompleted() {
            if (isFromInfoClick) {
                (this.Parent as Frame).Content = null;
                return;
            }
            if (VisibleWidth > FormatNumber && IsDivideScreen && MainMetroFrame.Content == null)
                MainMetroFrame.Navigate(typeof(MetroPage));
            GetFrameInstance(frameType).Content = null;
        }

        /// <summary>
        /// Update XAML views by url content.
        /// </summary>
        /// <param name="uri">this path url</param>
        private async void SetWebViewSourceAsync(Uri uri) {
            try {
                var result = htmlReturn = await DoubanWebProcess.GetMDoubanResponseAsync(
                    path: uri.ToString(),
                    host: "m.douban.com",
                    reffer: "https://m.douban.com/");
                var doc = new HtmlDocument();
                doc.LoadHtml(result);
                var root = doc.DocumentNode;
                if (model == null)
                    return;

                var model_state_succeed = SetDefaultModelState(root);
                var description_succeed = SetDescription(root.GetSectionNodeContentByClass("subject-intro"));
                var imagelist_succeed = SetImageList(root.GetSectionNodeContentByClass("subject-pics"));
                var question_succeed = SetQuestionList(root.GetSectionNodeContentByClass("subject-question"));
                var rec_succeed = SetRecommandsList(root.GetSectionNodeContentByClass("subject-rec"));
                var reviews_succeed = SetReviewsList(root.GetSectionNodeContentByClass("subject-reviews"));

                var interests_succeed = await SetInterestsAsync(uri);

            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine("Build native movie content page wrong : global wrong.");
                System.Diagnostics.Debug.WriteLine(e.Message);
            } finally { IncrementalLoadingBorder.SetVisibility(false); }
        }

        /// <summary>
        /// Fetch the interests list by movie-id and show them. Regex the uri to get the movie-id.
        /// </summary>
        /// <param name="uri">this path url</param>
        /// <returns>succeed or not</returns>
        private async Task<bool> SetInterestsAsync(Uri uri) {

            if (movie_id_received != null) {
                movie_id = movie_id_received;
            } else {
                movie_id = new Regex(@"subject/(?<movie_id>.+)").Match(uri.ToString()).Groups["movie_id"].Value;
                if (movie_id == "")
                    return false;
            }

            var interests_json = await DoubanWebProcess.GetMDoubanResponseAsync(
                path: $"{"https://"}m.douban.com/rexxar/api/v2/movie/{movie_id}/interests?count={7}&order_by=hot&start={0}&for_mobile=1",
                host: "m.douban.com",
                reffer: $"{"https://"}m.douban.com/movie/subject/{movie_id}/?refer=home");

            try {
                var collection = JsonHelper.FromJson<MovieContentInterestsCollection>(interests_json);
                model.CommentsList = collection.Interests;
                return true;
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine("Build native movie content page wrong : interests wrong.");
                System.Diagnostics.Debug.WriteLine(e.Message);
                return false;
            }
        }

        /// <summary>
        /// Init default state of the model this page.
        /// </summary>
        /// <param name="root">html root node</param>
        /// <returns>succeed or not</returns>
        private bool SetDefaultModelState(HtmlNode root) {
            try {
                model.Title = root.GetNodeFormat("h1", "class", "title")?.InnerText;
                model.Cover = root.GetNodeFormat("img", "class", "cover").Attributes["data-src"].Value;
                model.Rating = Convert.ToDouble(root.GetNodeFormat("meta", "itemprop", "ratingValue").Attributes["content"].Value);
                model.CommentersCount = root.GetNodeFormat("meta", "itemprop", "reviewCount").Attributes["content"].Value;
                model.Meta = SetMeta(root);
                return true;
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine("Build native movie content page wrong : set default model wrong.");
                System.Diagnostics.Debug.WriteLine(e.Message);
                return false;
            }
        }

        /// <summary>
        /// Read the description of the movie in correct format.
        /// </summary>
        /// <param name="descrip_group">description node</param>
        /// <returns>succeed or not</returns>
        private bool SetDescription(HtmlNode descrip_group) {
            try{
                if (descrip_group != null) {
                    model.Intro = descrip_group.GetNodeFormat("div", "class", "bd", false).SelectSingleNode("p").InnerText.Replace("。", "。\n");
                    return true;
                }
                return false;
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine("Build native movie content page wrong : description wrong.");
                System.Diagnostics.Debug.WriteLine(e.Message);
                return false;
            }
        }

        /// <summary>
        /// Read the questions-list of the movie in correct format.
        /// </summary>
        /// <param name="questions">questions list node</param>
        /// <returns>succeed or not</returns>
        private bool SetQuestionList(HtmlNode questions) {
            try {
                if (questions != null) {
                    var new_questions_list = new List<MovieContentQuestion>();
                    var ques_nodes = questions.GetNodeFormat("ul", "class", "list", false)?.SelectNodes("li");
                    ques_nodes?.ToList()?.ForEach(i => {
                        var act = i.SelectSingleNode("a");
                        if (act?.SelectSingleNode("h3") != null)
                            new_questions_list.Add(new MovieContentQuestion {
                                UrlPart = act?.Attributes["href"]?.Value,
                                Title = act?.SelectSingleNode("h3")?.InnerText,
                                Count = act?.GetNodeFormat("div", "class", "info", false)?.InnerText
                            });
                    });
                    model.Questions = new_questions_list;
                    return true;
                }
                return false;
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine("Build native movie content page wrong : question list wrong.");
                System.Diagnostics.Debug.WriteLine(e.Message);
                return false;
            }
        }

        /// <summary>
        /// Read the reviews-list of the movie in correct format.
        /// </summary>
        /// <param name="reviews">reviews list node</param>
        /// <returns>succeed or not</returns>
        private bool SetReviewsList(HtmlNode reviews) {
            try {
                if (reviews != null) {
                    var new_questions_list = new List<MovieContentReview>();
                    var ques_nodes = reviews.GetNodeFormat("div", "class", "bd", false).GetNodeFormat("ul", "class", "list", false)?.SelectNodes("li");
                    ques_nodes?.ToList()?.ForEach(i => {
                        var act = i.SelectSingleNode("a");
                        var wp = act.SelectSingleNode("div");
                        if (act?.SelectSingleNode("h3") != null)
                            new_questions_list.Add(new MovieContentReview {
                                UrlPart = act?.Attributes["href"]?.Value,
                                Title = act?.SelectSingleNode("h3")?.InnerText,
                                UserName = wp?.GetNodeFormat("span", "class", "username", false)?.InnerText,
                                Rating = Convert.ToDouble(wp?.GetNodeFormat("span", "class", "rating-stars", false)?.Attributes["data-rating"].Value) / 10,
                                UsefulCount = act?.GetNodeFormat("div", "class", "info", false)?.InnerText.Replace(" ", "").Substring(1),
                                Abstract = act?.GetNodeFormat("p", "class", "abstract", false)?.InnerText.Replace(" ", "").Substring(1),
                            });
                    });
                    model.Reviews = new_questions_list;
                    return true;
                }
                return false;
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine("Build native movie content page wrong : review list wrong.");
                System.Diagnostics.Debug.WriteLine(e.Message);
                return false;
            }
        }

        /// <summary>
        /// Read the rec-list of the movie in correct format.
        /// </summary>
        /// <param name="rec_coll">recommand list node</param>
        /// <returns>succeed or not</returns>
        private bool SetRecommandsList(HtmlNode rec_coll) {
            try {
                if (rec_coll != null) {
                    var new_questions_list = new List<MovieContentRecommand>();
                    var ques_nodes = rec_coll.GetNodeFormat("div", "class", "bd", false)?.SelectSingleNode("ul")?.SelectNodes("li");
                    ques_nodes?.ToList()?.ForEach(i => {
                        var act = i.SelectSingleNode("a");
                        if (act != null && act.GetNodeFormat("div", "class", "wp", false) != null) {
                            var wp = act.GetNodeFormat("div", "class", "wp", false);
                            if (wp?.SelectSingleNode("h3") != null)
                                new_questions_list.Add(new MovieContentRecommand {
                                    UrlPart = act?.Attributes["href"]?.Value,
                                    Title = wp?.SelectSingleNode("h3")?.InnerText,
                                    Cover = wp?.SelectSingleNode("img")?.Attributes["data-src"]?.Value
                                });
                        }
                    });
                    model.Recommands = new_questions_list;
                    return true;
                }
                return false;
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine("Build native movie content page wrong : recommand list wrong.");
                System.Diagnostics.Debug.WriteLine(e.Message);
                return false;
            }
        }

        /// <summary>
        /// Read the image-list of the movie in correct format.
        /// </summary>
        /// <param name="img_group">image node</param>
        /// <returns>succeed or not</returns>
        private bool SetImageList(HtmlNode img_group) {
            try {
                if (img_group != null) {
                    var new_list = new List<string>();
                    var lists = img_group
                        .GetNodeFormat("div", "class", "bd photo-list", false)
                        .GetNodeFormat("ul", "class", "wx-preview", false)
                        .SelectNodes("li[@class='pic']");
                    lists.ToList().ForEach(i => new_list.Add(i.SelectSingleNode("a").SelectSingleNode("img").Attributes["data-src"].Value));
                    model.ImageList = new_list;
                    return true;
                }
                return false;
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine("Build native movie content page wrong : images list wrong.");
                System.Diagnostics.Debug.WriteLine(e.Message);
                return false;
            }
        }

        /// <summary>
        /// Read the meta of the movie in correct format.
        /// </summary>
        /// <param name="root">html root node</param>
        /// <returns>string format meta</returns>
        private string SetMeta(HtmlNode root) {
            var builder = new StringBuilder();
            try {
                var meta = root.GetNodeFormat("p", "class", "meta").InnerText.Replace(" ", "").Replace(@"\n", "");
                meta = meta.Substring(1, meta.Length - 2);
                var time = new Regex(@"(?<time>[0-9]{4}-[0-9]{2}-[0-9]{2}.+)").Match(meta).Groups["time"].Value;
                var meta_coll = meta.Replace(time, "").Split('/').ToList();
                meta_coll.RemoveAt(meta_coll.Count - 1);
                var duration = meta_coll[0];
                meta_coll.RemoveAt(0);
                var director_index = meta_coll.FindIndex(i => i.Contains("导演"));
                var director = default(string);
                var feel_coll = default(List<string>);
                if (director_index >= 0) {
                    director = meta_coll[director_index];
                    meta_coll.RemoveAt(director_index);
                    feel_coll = meta_coll.Take(director_index).ToList();
                    meta_coll.RemoveRange(0, director_index);
                }
                var actors = string.Join(",", meta_coll);
                var feels = string.Join(",", feel_coll);

                if (actors.Length >= 120)
                    actors = actors.Substring(0, 120) + "...";

                return builder
                    .AppendLine(duration)
                    .AppendLine(feels)
                    .AppendLine()
                    .AppendLine(director)
                    .AppendLine(actors)
                    .AppendLine()
                    .AppendLine(time)
                    .ToString();

            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine("Build native movie content page wrong : set meta area wrong.");
                System.Diagnostics.Debug.WriteLine(e.Message);
                return builder.ToString();
            }
        }

        #region Show Images

        public void ShowImageInScreen(Uri imageSource) {
            ImageSaveButton.CommandParameter = imageSource.ToString();
            ImageScreen.Source = new BitmapImage(imageSource);
            ImagePopup.IsOpen = true;
        }

        private async Task<SoftwareBitmap> DownloadImageAsync(string url) {
            try {
                HttpClient hc = new HttpClient();
                HttpResponseMessage resp = await hc.GetAsync(new Uri(url));
                resp.EnsureSuccessStatusCode();
                IInputStream inputStream = await resp.Content.ReadAsInputStreamAsync();
                IRandomAccessStream memStream = new InMemoryRandomAccessStream();
                await RandomAccessStream.CopyAsync(inputStream, memStream);
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(memStream);
                SoftwareBitmap softBmp = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                return softBmp;
            } catch (Exception) {
                return null;
            }
        }

        public async Task<string> WriteToFileAsync(SoftwareBitmap softwareBitmap) {
            string fileName = "BEANSPROUTUWP" + "-" +
                Guid.NewGuid().ToString() + "-" +
                DateTime.Now.ToString("yyyy-MM--dd-hh-mm-ss") + ".jpg";

            if (softwareBitmap != null) {
                // save image file to cache
                StorageFile file = await (
                    await KnownFolders.PicturesLibrary.CreateFolderAsync("BeansproutUWP", CreationCollisionOption.OpenIfExists))
                    .CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite)) {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                    encoder.SetSoftwareBitmap(softwareBitmap);
                    await encoder.FlushAsync();
                }
            }

            return fileName;
        }

        #endregion

        #endregion

        #region Properties
        string movie_id;
        string htmlReturn;
        bool isFromInfoClick;
        string movie_id_received;
        FrameType frameType;
        MovieContentVM model;
        #endregion

    }
}
