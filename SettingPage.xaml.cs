using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using System;
using System.Threading.Tasks;

namespace LocalBookManager
{
    public partial class SettingPage : ContentPage
    {
        private bool _isInitialized = false;

        public SettingPage()
        {
            InitializeComponent();
            LoadCurrentSettings();
            _isInitialized = true;
        }

        private void LoadCurrentSettings()
        {
            double currentSize = Preferences.Default.Get("ReadPage_FontSize", 21.0);
            FontSizeSlider.Value = currentSize;
            if (FontSizeLabel != null)
            {
                FontSizeLabel.Text = Math.Round(currentSize).ToString();
            }

            ReminderSwitch.IsToggled = Preferences.Default.Get("IsReminderEnabled", false);
            TimeSpan savedTime = TimeSpan.Parse(Preferences.Default.Get("ReminderTime", "20:00:00"));
            ReminderTimePicker.Time = savedTime;
        }

        private void OnFontSizeChanged(object sender, ValueChangedEventArgs e)
        {
            if (!_isInitialized || FontSizeLabel == null) return;

            double newSize = Math.Round(e.NewValue);
            FontSizeLabel.Text = newSize.ToString();
            Preferences.Default.Set("ReadPage_FontSize", newSize);
        }

        private void OnFontFamilyClicked(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                string selectedFont = btn.StyleId;
                Preferences.Default.Set("ReadPage_FontFamily", selectedFont);
                DisplayAlert("Font Updated", $"Font style set to {btn.Text}", "OK");
            }
        }

        private void OnBgColorClicked(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                string hexColor = btn.StyleId;
                Preferences.Default.Set("ReadPage_BgColor", hexColor);
                DisplayAlert("Background Updated", "Background color changed.", "OK");
            }
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private void OnReminderToggled(object sender, ToggledEventArgs e)
        {
            Preferences.Default.Set("IsReminderEnabled", e.Value);
            UpdateDailyNotification();
        }

        private void OnReminderTimeChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == TimePicker.TimeProperty.PropertyName)
            {
                TimeSpan selectedTime = ReminderTimePicker.Time ?? new TimeSpan(20, 0, 0);
                Preferences.Default.Set("ReminderTime", selectedTime.ToString());
                UpdateDailyNotification();
            }
        }

        private async void UpdateDailyNotification()
        {
            if (!_isInitialized) return;

            LocalNotificationCenter.Current.Cancel(1001);

            if (Preferences.Default.Get("IsReminderEnabled", false))
            {
                TimeSpan time = ReminderTimePicker.Time ?? new TimeSpan(20, 0, 0);
                var notifyTime = DateTime.Today.Add(time);

                if (notifyTime < DateTime.Now)
                {
                    notifyTime = notifyTime.AddDays(1);
                }

                var request = new NotificationRequest
                {
                    NotificationId = 1001,
                    Title = "Is reading time!",
                    Description = "Open LocalBookManager and continue your reading journey.",
                    Schedule = new NotificationRequestSchedule
                    {
                        NotifyTime = notifyTime,
                        RepeatType = NotificationRepeat.Daily
                    }
                };

                await LocalNotificationCenter.Current.Show(request);
            }
        }
    }
}
