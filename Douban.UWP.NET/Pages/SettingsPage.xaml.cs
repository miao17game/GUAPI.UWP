﻿#region Using
using static Wallace.UWP.Helpers.Tools.UWPStates;
using static Douban.UWP.NET.Resources.AppResources;
using static Douban.UWP.NET.Pages.SettingsPage.InsideResources;

using Edi.UWP.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Douban.UWP.Core.Tools;
using Douban.UWP.NET.Controls;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Douban.UWP.NET.Tools;
using Wallace.UWP.Helpers.Tools;
using Windows.UI;
using Wallace.UWP.Helpers;
using Douban.UWP.NET.Resources;
using System.Reflection;
using Windows.Services.Store;
using Windows.Media.Core;
#endregion

namespace Douban.UWP.NET.Pages {
 
    public sealed partial class SettingsPage : Page {

        #region Constructor
        public SettingsPage() {
            this.InitializeComponent();
            Current = this;
            InitSettingsPageStateAsync();
            this.NavigationCacheMode = NavigationCacheMode.Required;
        }
        #endregion

        #region Events
        protected override void OnNavigatedTo(NavigationEventArgs e) {
            
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var item = (sender as Pivot).SelectedItem as PivotItem;
            if (item.Name == PrivacyPolicy.Name)
                policyRingDB.SetVisibility(true);
        }

        private async void FeedBackBtn_ClickAsync(object sender, RoutedEventArgs e) {
            await ReportErrorAsync(null, "N/A", true);
        }

        private void Switch_Toggled(object sender, RoutedEventArgs e) {
            GetSwitchHandler((sender as ToggleSwitch).Name)
               .Invoke((sender as ToggleSwitch).Name);
        }

        private void LanguageCombox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var newLanguage = GetLanguageTag(
                    GetComboItemInstance(
                        (e.AddedItems.FirstOrDefault() as ComboBoxItem)
                        .Name as string));
            SaveLanguageSettings(Language = newLanguage);
            if (isInitViewOrNot) { isInitViewOrNot = false; return; }
            Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = newLanguage;
            new ToastSmooth(GetUIString("ReStartToChangeLanguage")).Show();
        }

        private void ToastCombox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var newVoice = GetLanguageTag(
                    GetComboItemInstance(
                        (e.AddedItems.FirstOrDefault() as ComboBoxItem)
                        .Name as string));
            SaveToastVoiceSettings(ToastVoice = newVoice);
        }

        private void ListenVoiceBtn_Click(object sender, RoutedEventArgs e) {
            VoiceContentElement.Source = MediaSource.CreateFromUri(new Uri(ToastVoice));
            VoiceContentElement.MediaPlayer.Play();
        }

        /// <summary>
        /// *(Important) Change divide percent value when slider action finished.
        /// </summary>
        /// <param name="sender">slider</param>
        /// <param name="e">args</param>
        private void SplitSizeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e) {
            isNeedSaveSliderValue = false;
            if (isInitSliderValueOrNot) {
                isInitSliderValueOrNot = false;
                return;
            }
            if (timerForSlider == null) {
                timerForSlider = new DispatcherTimer();
                timerForSlider.Tick += (obj, args) => {
                    if (isNeedSaveSliderValue) {
                        timerForSlider.Stop();
                        ChangeSplitViewWidth(SplitSizeSlider.Value);
                        timerForSlider = null;
                    }
                    isNeedSaveSliderValue = true;
                };
                timerForSlider.Interval = new TimeSpan(0, 0, 0, 0, 200);
                timerForSlider.Start();
            }
            if (!isInitSliderValueOrNot)
                SaveSplitPercentSetiings(DivideNumber = e.NewValue / 100);
        }

        private async void CacheClearBtn_ClickAsync(object sender, RoutedEventArgs e) {
            CacheClearBtn.IsEnabled = false;
            ClearRing.IsActive = true;
            await ClearCacheSizeAsync();
            CacheClearBtn.IsEnabled = true;
            ClearRing.IsActive = false;
        }

        private async void SecondTitleBtn_ClickAsync(object sender, RoutedEventArgs e) {
            Windows.UI.StartScreen.SecondaryTile tile = TilesHelper.GenerateSecondaryTile("SecondaryTitle", "Beansprout UWP", Colors.Transparent);
            tile.VisualElements.ShowNameOnSquare150x150Logo =
                tile.VisualElements.ShowNameOnSquare310x310Logo =
                tile.VisualElements.ShowNameOnWide310x150Logo =
                true;
            await tile.RequestCreateAsync();
            try {
                await TilesHelper.GetNewsAsync();
            } catch { /* Ignore */ }
        }

        private void WebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args) {
            
        }

        private void WebView_ContentLoading(WebView sender, WebViewContentLoadingEventArgs args) {
            policyRingDB.SetVisibility(true);
        }

        private void WebView_DOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args) {
            policyRingDB.SetVisibility(false);
        }

        private async void StackPanel_LoadedAsync(object sender, RoutedEventArgs e) {
            var context = StoreContext.GetDefault();
            var fm_succeed = await WindowsStoreHelpers.GetProductInfoAsync(context, id: "9mzf5cp1mf83");
            var group_succeed = await WindowsStoreHelpers.GetProductInfoAsync(context, id: "9pbfqp5g46w6");

            FM_Extensions_Status.Text = fm_succeed ? GetUIString("Extensions_Status_Get") : GetUIString("Extensions_Status_Unget");
            Group_Extensions_Status.Text = group_succeed ? GetUIString("Extensions_Status_Get") : GetUIString("Extensions_Status_Unget");
            GetFMExtensionsBtn.SetVisibility(!fm_succeed);
            GetGroupExtensionsBtn.SetVisibility(!group_succeed);
            if (fm_succeed)
                FM_Extensions_Status.Foreground = Application.Current.Resources["DoubanForeground"] as SolidColorBrush;
            if (group_succeed)
                Group_Extensions_Status.Foreground = Application.Current.Resources["DoubanForeground"] as SolidColorBrush;
        }

        private void GetGroupExtensionsBtn_Click(object sender, RoutedEventArgs e) {
            ReportHelper.ReportAttentionAsync(GetUIString("StillInDeveloping"));
            //var context = StoreContext.GetDefault();
            //var result = await WindowsStoreHelpers.PurchaseAddOnAsync(context, "9pbfqp5g46w6");
            //if (result == PurchasAddOnReturn.Successful) {
            //    GetFMExtensionsBtn.SetVisibility(false);
            //    FM_Extensions_Status.Text = GetUIString("Extensions_Status_Get");
            //    FM_Extensions_Status.Foreground = Application.Current.Resources["DoubanForeground"] as SolidColorBrush;
            //    HasFMExtensions = true;
            //} else if (result == PurchasAddOnReturn.Unknown) {
            //    ReportHelper.ReportAttentionAsync(GetUIString("MS_Server_Boom"));
            //}
        }

        private async void GetFMExtensionsBtn_ClickAsync(object sender, RoutedEventArgs e) {
            var context = StoreContext.GetDefault();
            var result = await WindowsStoreHelpers.PurchaseAddOnAsync(context, "9mzf5cp1mf83");
            if (result == PurchasAddOnReturn.Successful) {
                GetFMExtensionsBtn.SetVisibility(false);
                FM_Extensions_Status.Text = GetUIString("Extensions_Status_Get");
                FM_Extensions_Status.Foreground = Application.Current.Resources["DoubanForeground"] as SolidColorBrush;
                HasFMExtensions = true;
            } else if(result == PurchasAddOnReturn.Unknown) {
                ReportHelper.ReportAttentionAsync(GetUIString("MS_Server_Boom"));
            }
        }

        #endregion

        #region Methods

        private async void InitSettingsPageStateAsync() {
            VersionMessage.Text = GetUIString("VersionMessage") + Utils.GetAppVersion();
            LanguageCombox.SelectedItem = GetComboItemFromTag((string)SettingsHelper.ReadSettingsValue(SettingsSelect.Language) ?? ConstFields.English_US);
            ToastCombox.SelectedItem = GetComboItemFromTag(ToastVoice);
            ThemeSwitch.IsOn = IsGlobalDark;
            NEONSwitch.IsOn = IsProjectNEON;
            ScreenSwitch.IsOn = IsDivideScreen;
            ToastSendSwitch.IsOn = IsToastEnable;
            SplitSizeSlider.Value = 100 * DivideNumber;
            ScreenSwitch.IsEnabled = !IsMobile;
            SplitSizeSlider.IsEnabled = !IsMobile;
            await ShowCacheSizeAsync();
        }

        private void ChangeSplitViewWidth(double value) {
            SetChangesDone(MainUserInfosFrame, value);
            SetChangesDone(MainUpContentFrame, value);
            SetChangesDone(MainContentFrame, value);
            SetMetroChangesDone(value);
        }

        #region Toggle Events

        private void OnThemeSwitchToggled(ToggleSwitch sender) {
            SaveThemeSettings(IsGlobalDark = sender.IsOn);
            AppResources.Current.RequestedTheme = sender.IsOn ? ElementTheme.Dark : ElementTheme.Light;
            if (isInitViewOrNot)
                return;
            StatusBarInit.InitDesktopStatusBar(false, Colors.Black, Color.FromArgb(255, 67, 104, 203), Colors.White, Color.FromArgb(255, 202, 0, 62));
            StatusBarInit.InitMobileStatusBar(false, Colors.Black, Color.FromArgb(255, 67, 104, 203), Colors.White, Color.FromArgb(255, 202, 0, 62));
        }

        private void OnScreenSwitchToggled(ToggleSwitch sender) {
            SaveDivideSettings(IsDivideScreen = sender.IsOn);
            DoWorkWhenScreenSwitchToggled(sender);
        }

        private void OnNEONSwitchToggled(ToggleSwitch sender) {
            SaveNEONSettings(IsProjectNEON = sender.IsOn);
            DoWorkWhenNEONSwitchToggled(sender);
        }

        private void OnToastSendSwitchToggled(ToggleSwitch sender) {
            SaveToastSendSettings(IsToastEnable = sender.IsOn);
        }

        #endregion

        #region This Helper

        private string GetTypeName(object obj) {
            return obj.GetType().GetTypeInfo().Name;
        }

        private string GetParentName(object obi) {
            return obi.GetType().GetTypeInfo().BaseType.Name;
        }

        private void SaveToastVoiceSettings(string newVoice) {
            SettingsHelper.SaveSettingsValue(SettingsSelect.ToastVoice, newVoice);
        }

        private void SaveLanguageSettings(string newLanguage) {
            SettingsHelper.SaveSettingsValue(SettingsSelect.Language, newLanguage);
        }

        private void SaveSplitPercentSetiings(double num) {
            SettingsHelper.SaveSettingsValue(SettingsSelect.SplitViewMode, num);
        }

        private void SaveToastSendSettings(bool isToastEnable) {
            SettingsHelper.SaveSettingsValue(SettingsSelect.IsToastEnable, isToastEnable);
        }

        private void SaveDivideSettings(bool isDivideScreen) {
            SettingsHelper.SaveSettingsValue(SettingsSelect.IsDivideScreen, isDivideScreen);
        }

        private void SaveNEONSettings(bool isNEON) {
            SettingsHelper.SaveSettingsValue(SettingsSelect.IsProjectNEON, isNEON);
        }

        private void SaveThemeSettings(bool isDarkOrNot) {
            SettingsHelper.SaveSettingsValue(SettingsConstants.IsDarkThemeOrNot, isDarkOrNot);
        }

        private void NavigateToMetroPage() {
            MainMetroFrame.Navigate(typeof(MetroPage));
        }

        private void ClearFrameContent(Frame frame) {
            frame.Content = null;
        }

        private void SetMetroChangesDone(double value) {
            if (MainMetroFrame.Content == null)
                return;
            if (GetTypeName(MainMetroFrame.Content) == typeof(MetroPage).Name)
                AdaptForFrameDivide(MainMetroFrame.Content, value / 100, Current.ScreenSwitch.IsOn);
        }
        private void SetMetroChangesDone() {
            if (MainMetroFrame.Content == null)
                return;
            if (GetTypeName(MainMetroFrame.Content) == typeof(MetroPage).Name)
                AdaptForFrameDivide(MainMetroFrame.Content, DivideNumber, IsDivideScreen);
        }

        private void SetNEONChangesMetroDone(object content, bool isOn) {
            if (content == null)
                return;
            if (GetTypeName(content) == typeof(MetroPage).Name)
                (content as MetroPage)?.SetNEONOutside(isOn);
        }

        private void SetNEONChangesListDone(object content, bool isOn) {
            if (content == null)
                return;
            if (GetTypeName(content) == typeof(ListInfosPage).Name)
                (content as ListInfosPage)?.SetNEONOutside(isOn);
        }

        private void SetChangesDone(Frame frame, double value) {
            if (frame.Content == null)
                return;
            if (GetParentName(frame.Content) == typeof(BaseContentPage).Name)
                AdaptForFrameDivide(frame.Content, value / 100, Current.ScreenSwitch.IsOn);
        }

        private void SetChangesDone(Frame frame) {
            if (frame.Content == null)
                return;
            if (GetParentName(frame.Content) == typeof(BaseContentPage).Name)
                AdaptForFrameDivide(frame.Content, DivideNumber, IsDivideScreen);
        }

        private void AdaptForFrameDivide(object content, double divideNum, bool isDivide) {
            GlobalHelpers.DivideWindowRange(
                content as Page,
                divideNum: DivideNumber,
                isDivideScreen: IsDivideScreen);
        }

        private void DoWorkWhenScreenSwitchToggled(ToggleSwitch sender) {
            SetChangesDone(MainUserInfosFrame);
            SetChangesDone(MainUpContentFrame);
            SetChangesDone(MainContentFrame);
            SetMetroChangesDone();
            if (sender.IsOn && VisibleWidth > FormatNumber && MainMetroFrame.Content == null)
                NavigateToMetroPage();
        }

        private void DoWorkWhenNEONSwitchToggled(ToggleSwitch sender) {
            SetNEONChangesMetroDone(MainMetroFrame.Content, sender.IsOn);
            SetNEONChangesListDone(MainLeftPartFrame.Content, sender.IsOn);
        }

        #endregion

        #region Cache Methods

        private async Task ShowCacheSizeAsync() {
            var localCF = ApplicationData.Current.LocalCacheFolder;
            var folders = await localCF.GetFoldersAsync();
            double sizeOfCache = 0.00;
            BasicProperties propertiesOfItem;
            foreach (var item in folders) {
                var files = await item.GetFilesAsync(Windows.Storage.Search.CommonFileQuery.DefaultQuery);
                foreach (var file in files) {
                    propertiesOfItem = await file.GetBasicPropertiesAsync();
                    sizeOfCache += propertiesOfItem.Size;
                }
            }
            var sizeInMb = sizeOfCache / (1024 * 1024);
            CacheSizeTitle.Text = sizeInMb - 0 > 0.00000001 ? sizeInMb.ToString("#.##") + "MB" : "0 MB";
        }

        private async Task ClearCacheSizeAsync() {
            var localCF = ApplicationData.Current.LocalCacheFolder;
            var folders = await localCF.GetFoldersAsync();
            foreach (var item in folders) {
                var files = await item.GetFilesAsync(Windows.Storage.Search.CommonFileQuery.DefaultQuery);
                foreach (var file in files) {
                    await file.DeleteAsync();
                }
            }
            await ShowCacheSizeAsync();
        }

        #endregion

        #region Error Reporter
        /// <summary>
        /// ReportError Method
        /// </summary>
        /// <param name="viewName"></param>
        /// <param name="msg"></param>
        /// <param name="pageSummary"></param>
        /// <param name="includeDeviceInfo"></param>
        /// <returns></returns>
        public static async Task ReportErrorAsync(string msg = null, string pageSummary = "N/A", bool includeDeviceInfo = true) {
            var deviceInfo = new EasClientDeviceInformation();

            string subject = GetUIString("Feedback_Subject");
            string body = $"{GetUIString("Feedback_Body")}：{msg}  " +
                          $"（{GetUIString("Feedback_Version")}：{Utils.GetAppVersion()} ";

            if (includeDeviceInfo) {
                body += $", {GetUIString("Feedback_FriendlyName")}：{deviceInfo.FriendlyName}, " +
                          $"{GetUIString("Feedback_OS")}：{deviceInfo.OperatingSystem}, " +
                          $"SKU：{deviceInfo.SystemSku}, " +
                          $"{GetUIString("Feedback_SPN")}：{deviceInfo.SystemProductName}, " +
                          $"{GetUIString("Feedback_SMF")}：{deviceInfo.SystemManufacturer}, " +
                          $"{GetUIString("Feedback_SFV")}：{deviceInfo.SystemFirmwareVersion}, " +
                          $"{GetUIString("Feedback_SHV")}：{deviceInfo.SystemHardwareVersion}）";
            } else {
                body += ")";
            }

            string to = "miao17game@qq.com";
            await Tasks.OpenEmailComposeAsync(to, subject, body);
        }

        #endregion

        #endregion

        #region Inside resources class
        internal static class InsideResources {

            public static ComboBoxItem GetComboItemInstance(string buttonName) { return comboItemsMaps.ContainsKey(buttonName) ? comboItemsMaps[buttonName] : null; }
            public static string GetLanguageTag(ComboBoxItem button) { return languageSaveTagsMaps.ContainsKey(button) ? languageSaveTagsMaps[button] : null; }
            public static ComboBoxItem GetComboItemFromTag(string tag) { return languageSaveTagsMaps.ContainsValue(tag) ? languageSaveTagsMaps.Where(i => i.Value == tag).FirstOrDefault().Key : null; }
            static private Dictionary<string, ComboBoxItem> comboItemsMaps = new Dictionary<string, ComboBoxItem> {
                { Current.enUSSelect.Name,Current.enUSSelect},
                { Current.zhCNSelect.Name,Current.zhCNSelect},
                { Current.Voice_Yiner.Name,Current.Voice_Yiner},
                { Current.Voice_Haizi.Name,Current.Voice_Haizi},
                { Current.Voice_Yellsedtsr.Name,Current.Voice_Yellsedtsr},
                { Current.Voice_AMeng.Name,Current.Voice_AMeng},
                { Current.Voice_Xiaoxin.Name,Current.Voice_Xiaoxin},
                { Current.Voice_CS_FireInTheHole.Name,Current.Voice_CS_FireInTheHole},
                { Current.Voice_CS_GO.Name,Current.Voice_CS_GO},
        };
            static private Dictionary<ComboBoxItem, string> languageSaveTagsMaps = new Dictionary<ComboBoxItem, string> {
                { Current.enUSSelect,ConstFields.English_US},
                { Current.zhCNSelect,ConstFields.Chinese_CN},
                { Current.Voice_Yiner,@"ms-appx:///Voice/yiner.mp3"},
                { Current.Voice_Haizi,@"ms-appx:///Voice/haizi.mp3"},
                { Current.Voice_Yellsedtsr,@"ms-appx:///Voice/yellsedtsr.mp3"},
                { Current.Voice_AMeng,@"ms-appx:///Voice/DoraAmeng.mp3"},
                { Current.Voice_Xiaoxin,@"ms-appx:///Voice/xiaoxin.mp3"},
                { Current.Voice_CS_FireInTheHole,@"ms-appx:///Voice/fire_in_the_hole.wav"},
                { Current.Voice_CS_GO,@"ms-appx:///Voice/gogogo.wav"},
        };

            public static ToggleSwitch GetSwitchInstance(string str) { return switchSettingsMaps.ContainsKey(str) ? switchSettingsMaps[str] : null; }
            static private Dictionary<string, ToggleSwitch> switchSettingsMaps = new Dictionary<string, ToggleSwitch> {
                { Current.ThemeSwitch.Name,Current.ThemeSwitch},
                { Current.ScreenSwitch.Name,Current.ScreenSwitch},
                { Current.NEONSwitch.Name,Current.NEONSwitch},
                { Current.ToastSendSwitch.Name,Current.ToastSendSwitch},
        };

            public static SwitchEventHandler GetSwitchHandler(string switchName) { return switchHandlerMaps.ContainsKey(switchName) ? switchHandlerMaps[switchName] : null; }
            static private Dictionary<string, SwitchEventHandler> switchHandlerMaps = new Dictionary<string, SwitchEventHandler> {
                { Current.ThemeSwitch.Name, new SwitchEventHandler(instance=> { Current.OnThemeSwitchToggled(GetSwitchInstance(instance)); }) },
                { Current.ScreenSwitch.Name, new SwitchEventHandler(instance=> { Current.OnScreenSwitchToggled(GetSwitchInstance(instance)); }) },
                { Current.NEONSwitch.Name, new SwitchEventHandler(instance=> { Current.OnNEONSwitchToggled(GetSwitchInstance(instance)); }) },
                { Current.ToastSendSwitch.Name, new SwitchEventHandler(instance=> { Current.OnToastSendSwitchToggled(GetSwitchInstance(instance)); }) },
        };

        }
        #endregion

        #region Properties and state
        /// <summary>
        /// If first load this page or not.
        /// </summary>
        private bool isInitViewOrNot = true;
        private bool isInitSliderValueOrNot = true;
        private bool isNeedSaveSliderValue = true;
        private DispatcherTimer timerForSlider;
        public static SettingsPage Current;
        public delegate void SwitchEventHandler(string instance);
        #endregion

    }
}
