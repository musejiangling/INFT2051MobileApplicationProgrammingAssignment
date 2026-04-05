using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LocalBookManager.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Graphics;

namespace LocalBookManager
{
    public partial class ReadPage : ContentPage
    {
        private Book _currentBook;
        private List<string> _pages = new List<string>();
        private int _currentPageIndex = 0;
        private const int CharsPerPage = 800;

        public ReadPage(Book book)
        {
            InitializeComponent();
            _currentBook = book;
            BookTitleLabel.Text = _currentBook.Title;

            LoadBookContent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ApplySettings();
        }

        private void ApplySettings()
        {
            double fontSize = Preferences.Default.Get("ReadPage_FontSize", 21.0);
            ContentLabel.FontSize = fontSize;

            string fontFamily = Preferences.Default.Get("ReadPage_FontFamily", "System");
            ContentLabel.FontFamily = fontFamily == "System" ? null : fontFamily;

            string bgColorHex = Preferences.Default.Get("ReadPage_BgColor", "#FFFFFF");

            // Safety check: if parsing fails or value is empty, force reset to pure white
            if (string.IsNullOrEmpty(bgColorHex) || !Color.TryParse(bgColorHex, out Color bgColor))
            {
                bgColorHex = "#FFFFFF";
                bgColor = Colors.White;
                Preferences.Default.Set("ReadPage_BgColor", "#FFFFFF");
            }

            this.BackgroundColor = bgColor;
            RootGrid.BackgroundColor = bgColor;

            // Text color: white on black background, black on all other backgrounds
            bool isDarkMode = bgColorHex == "#000000";
            Color primaryTextColor = isDarkMode ? Colors.White : Colors.Black;
            Color secondaryTextColor = isDarkMode ? Colors.LightGray : Colors.Gray;

            ContentLabel.TextColor = primaryTextColor;
            BackButton.TextColor = primaryTextColor;
            SettingButton.TextColor = primaryTextColor;
            SettingButton.BackgroundColor = isDarkMode ? Color.FromArgb("#333333") : Colors.White;
            SettingButton.BorderColor = isDarkMode ? Colors.Gray : Colors.Black;

            ChapterLabel.TextColor = secondaryTextColor;
            BookTitleLabel.TextColor = secondaryTextColor;
            PageInfoLabel.TextColor = secondaryTextColor;
        }

        private void LoadBookContent()
        {
            string fullText = "";

            try
            {
                if (_currentBook.FileType.Equals(".txt", StringComparison.OrdinalIgnoreCase) && File.Exists(_currentBook.FilePath))
                {
                    fullText = File.ReadAllText(_currentBook.FilePath);
                }
                else
                {
                    fullText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Quisque faucibus ex sapien vitae pellentesque sem placerat. In id cursus mi pretium tellus duis convallis. Tempus leo eu aenean sed diam urna tempor. Pulvinar vivamus fringilla lacus nec metus bibendum egestas. Iaculis massa nisl malesuada lacinia integer nunc posuere. Ut hendrerit semper vel class aptent taciti sociosqu. Ad litora torquent per conubia nostra inceptos himenaeos.\n\n" +
                               "(Since no docx/epub parsing library is currently imported, this is a simulated document. To demonstrate the page-turning effect, text is intentionally repeated below:)\n\n" +
                               new string('X', 2000);
                }

                _pages.Clear();
                for (int i = 0; i < fullText.Length; i += CharsPerPage)
                {
                    int length = Math.Min(CharsPerPage, fullText.Length - i);
                    _pages.Add(fullText.Substring(i, length));
                }

                _currentPageIndex = _currentBook.CurrentPageIndex;

                if (_currentPageIndex >= _pages.Count)
                {
                    _currentPageIndex = Math.Max(0, _pages.Count - 1);
                }

                UpdatePageDisplay();
            }
            catch (Exception ex)
            {
                ContentLabel.Text = "Failed to load book content: " + ex.Message;
            }
        }

        private void UpdatePageDisplay()
        {
            if (_pages.Count > 0)
            {
                ContentLabel.Text = _pages[_currentPageIndex];
                PageInfoLabel.Text = $"{_currentPageIndex + 1} / {_pages.Count}";
                ChapterLabel.Text = $"P{_currentPageIndex + 1}";
            }
        }

        private async void OnSwipedLeft(object sender, SwipedEventArgs e)
        {
            if (_currentPageIndex < _pages.Count - 1)
            {
                _currentPageIndex++;
                UpdatePageDisplay();

                _currentBook.CurrentPageIndex = _currentPageIndex;
                await AppDatabase.SaveBookAsync(_currentBook);
            }
            else
            {
                await DisplayAlert("Notice", "You've reached the last page.", "OK");
            }
        }

        private async void OnSwipedRight(object sender, SwipedEventArgs e)
        {
            if (_currentPageIndex > 0)
            {
                _currentPageIndex--;
                UpdatePageDisplay();

                _currentBook.CurrentPageIndex = _currentPageIndex;
                await AppDatabase.SaveBookAsync(_currentBook);
            }
            else
            {
                await DisplayAlert("Notice", "You're at the first page.", "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private async void OnSettingClicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new SettingPage());
        }
    }
}
