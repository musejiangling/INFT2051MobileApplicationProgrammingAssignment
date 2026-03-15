using System;
using System.Collections.Generic;
using System.IO;
using LocalBookManager.Models;
using Microsoft.Maui.Controls;

namespace LocalBookManager
{
    public partial class ReadPage : ContentPage
    {
        private Book _currentBook;
        private List<string> _pages = new List<string>();
        private int _currentPageIndex = 0;
        private const int CharsPerPage = 800;

        // Constructor: receives the book information passed from the main page
        public ReadPage(Book book)
        {
            InitializeComponent();
            _currentBook = book;
            BookTitleLabel.Text = _currentBook.Title;

            LoadBookContent();
        }

        // Load and paginate the book content
        private void LoadBookContent()
        {
            string fullText = "";

            try
            {
                // Temporarily only demonstrating reading .txt files. .epub and .docx require third-party libraries.
                if (_currentBook.FileType.Equals(".txt", StringComparison.OrdinalIgnoreCase) && File.Exists(_currentBook.FilePath))
                {
                    fullText = File.ReadAllText(_currentBook.FilePath);
                }
                else
                {
                    // If not a .txt file, generate a Lorem Ipsum dummy text for UI demonstration
                    fullText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Quisque faucibus ex sapien vitae pellentesque sem placerat. In id cursus mi pretium tellus duis convallis. Tempus leo eu aenean sed diam urna tempor. Pulvinar vivamus fringilla lacus nec metus bibendum egestas. Iaculis massa nisl malesuada lacinia integer nunc posuere. Ut hendrerit semper vel class aptent taciti sociosqu. Ad litora torquent per conubia nostra inceptos himenaeos.\n\n" +
                               "(Since no docx/epub parsing library is currently imported, this is a simulated document. To demonstrate the page-turning effect, text is intentionally repeated below:)\n\n" +
                               new string('X', 2000); // Insert a large number of placeholders to generate multiple pages
                }

                // Simple pagination algorithm: split the long text into chunks by a fixed character count
                _pages.Clear();
                for (int i = 0; i < fullText.Length; i += CharsPerPage)
                {
                    int length = Math.Min(CharsPerPage, fullText.Length - i);
                    _pages.Add(fullText.Substring(i, length));
                }

                // Initialize to show the first page
                _currentPageIndex = 0;
                UpdatePageDisplay();
            }
            catch (Exception ex)
            {
                ContentLabel.Text = "Failed to load book content: " + ex.Message;
            }
        }

        // Update the UI text and page numbers
        private void UpdatePageDisplay()
        {
            if (_pages.Count > 0)
            {
                ContentLabel.Text = _pages[_currentPageIndex];
                PageInfoLabel.Text = $"{_currentPageIndex + 1} / {_pages.Count}";
                ChapterLabel.Text = $"P{_currentPageIndex + 1}";
            }
        }

        // Next page
        private void OnSwipedLeft(object sender, SwipedEventArgs e)
        {
            if (_currentPageIndex < _pages.Count - 1)
            {
                _currentPageIndex++;
                UpdatePageDisplay();
            }
            else
            {
                DisplayAlert("Notice", "This is the last page.", "OK");
            }
        }

        // Previous page
        private void OnSwipedRight(object sender, SwipedEventArgs e)
        {
            if (_currentPageIndex > 0)
            {
                _currentPageIndex--;
                UpdatePageDisplay();
            }
            else
            {
                DisplayAlert("Notice", "This is the first page.", "OK");
            }
        }

        // Back button
        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        // Setting button 
        private async void OnSettingClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Settings", "The settings interface will be developed in the next step!\nIt will support font size and background color adjustments.", "OK");
        }
    }
}