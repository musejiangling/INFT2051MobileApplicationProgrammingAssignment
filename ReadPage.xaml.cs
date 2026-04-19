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

        public ReadPage(Book book)
        {
            InitializeComponent();
            _currentBook = book;
            BookTitleLabel.Text = _currentBook.Title;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ApplySettings();

            // Defer content loading until after the layout pass completes,
            // so ContentLabel.Height and Width are valid measurements.
            Dispatcher.Dispatch(async () =>
            {
                await Task.Delay(50); // wait one layout frame
                if (_pages.Count == 0)
                    LoadBookContent();
            });
        }

        private void ApplySettings()
        {
            double fontSize = Preferences.Default.Get("ReadPage_FontSize", 21.0);
            ContentLabel.FontSize = fontSize;

            string fontFamily = Preferences.Default.Get("ReadPage_FontFamily", "System");

            // Font in mobile phone repiar
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (string.IsNullOrEmpty(fontFamily) || fontFamily == "System")
                {
                    ContentLabel.FontFamily = null; 
                }
                else
                {
                    ContentLabel.FontFamily = fontFamily; 
                }

                ContentLabel.InvalidateMeasure();
            });

            string bgColorHex = Preferences.Default.Get("ReadPage_BgColor", "#FFFFFF");
            if (string.IsNullOrEmpty(bgColorHex) || !Color.TryParse(bgColorHex, out Color bgColor))
            {
                bgColorHex = "#FFFFFF";
                bgColor = Colors.White;
                Preferences.Default.Set("ReadPage_BgColor", "#FFFFFF");
            }

            this.BackgroundColor = bgColor;
            RootGrid.BackgroundColor = bgColor;

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

            // Re-paginate if font size changed after initial load
            if (_pages.Count > 0)
            {
                _pages.Clear();
                LoadBookContent();
            }
        }

        private void LoadBookContent()
        {
            string fullText = "";

            try
            {
                if (_currentBook.FileType.Equals(".txt", StringComparison.OrdinalIgnoreCase)
                    && File.Exists(_currentBook.FilePath))
                {
                    fullText = File.ReadAllText(_currentBook.FilePath);
                }
                else
                {
                    fullText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
                               "Quisque faucibus ex sapien vitae pellentesque sem placerat.\n\n" +
                               "(epub/docx parsing not available — showing demo text.)\n\n" +
                               string.Concat(System.Linq.Enumerable.Repeat(
                                   "This is a sample paragraph repeating to simulate a long book. ", 200));
                }

                PaginateByHeight(fullText);

                _currentPageIndex = _currentBook.CurrentPageIndex;
                if (_currentPageIndex >= _pages.Count)
                    _currentPageIndex = Math.Max(0, _pages.Count - 1);

                UpdatePageDisplay();
            }
            catch (Exception ex)
            {
                ContentLabel.Text = "Failed to load book content: " + ex.Message;
            }
        }

        // ── Dynamic pagination ────────────────────────────────────────────
        // Uses the Label's actual rendered height and font size to estimate
        // how many lines fit per page, then groups lines into pages.
        private void PaginateByHeight(string fullText)
        {
            _pages.Clear();

            double containerHeight = ContentLabel.Height;
            double fontSize = ContentLabel.FontSize;
            double labelWidth = ContentLabel.Width > 0 ? ContentLabel.Width : 360;

            // Line height estimate: 1.5× font size matches MAUI default spacing
            double lineHeight = fontSize * 1.5;
            int linesPerPage = Math.Max(1, (int)(containerHeight / lineHeight));

            // Average character width estimate: 0.55× font size
            int charsPerLine = Math.Max(1, (int)(labelWidth / (fontSize * 0.55)));

            // Step 1: word-wrap every paragraph into individual display lines
            var lines = new List<string>();
            foreach (var paragraph in fullText.Split('\n'))
            {
                if (paragraph.Length == 0)
                {
                    lines.Add(""); // preserve blank lines between paragraphs
                    continue;
                }

                for (int i = 0; i < paragraph.Length; i += charsPerLine)
                {
                    int len = Math.Min(charsPerLine, paragraph.Length - i);
                    lines.Add(paragraph.Substring(i, len));
                }
            }

            // Step 2: group lines into fixed-height pages
            for (int i = 0; i < lines.Count; i += linesPerPage)
            {
                int count = Math.Min(linesPerPage, lines.Count - i);
                _pages.Add(string.Join("\n", lines.GetRange(i, count)));
            }

            if (_pages.Count == 0)
                _pages.Add(fullText);
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