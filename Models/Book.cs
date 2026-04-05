using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SQLite; 

namespace LocalBookManager.Models
{
    public class Book : INotifyPropertyChanged
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }// SQLite primary key, auto-incremented unique identifier for the book


        public string Title { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public string FileSize { get; set; }
        public DateTime ImportDate { get; set; }

        public int CurrentPageIndex { get; set; } = 0; // Current reading progress (page index), default value is 0

        [Ignore] // Ignored by SQLite (not mapped to database table)
        public string CoverText => string.IsNullOrEmpty(Title) ? "?" : Title.Substring(0, 1).ToUpper();

        private FormattedString _formattedTitle;

        [Ignore] // Ignored by SQLite to prevent database creation errors
        public FormattedString FormattedTitle
        {
            get => _formattedTitle;
            set
            {
                _formattedTitle = value;
                OnPropertyChanged();
            }
        }

        public void InitializeTitle()
        {
            UpdateHighlight(""); 
        }

        public void UpdateHighlight(string keyword)
        {
            var formattedString = new FormattedString();

            // No keyword: use default plain text title
            if (string.IsNullOrEmpty(keyword))
            {
                formattedString.Spans.Add(new Span { Text = Title, TextColor = Colors.Black, FontAttributes = FontAttributes.None });
                FormattedTitle = formattedString;
                return;
            }
            // Find the first case-insensitive match of the keyword
            int index = Title.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);

            if (index < 0)
            {
                formattedString.Spans.Add(new Span { Text = Title, TextColor = Colors.Black, FontAttributes = FontAttributes.None });
                FormattedTitle = formattedString;
                return;
            }

            int currentIndex = 0;
            // Loop through all keyword matches in the title
            while (index != -1)
            {
                // Add non-highlighted text before the keyword match
                if (index > currentIndex)
                {
                    formattedString.Spans.Add(new Span { Text = Title.Substring(currentIndex, index - currentIndex), TextColor = Colors.Black, FontAttributes = FontAttributes.None });
                }

                // Add highlighted keyword
                formattedString.Spans.Add(new Span
                {
                    Text = Title.Substring(index, keyword.Length),
                    BackgroundColor = Colors.Yellow,
                    TextColor = Colors.Red,
                    FontAttributes = FontAttributes.Bold
                });

                currentIndex = index + keyword.Length;
                index = Title.IndexOf(keyword, currentIndex, StringComparison.OrdinalIgnoreCase);
            }

            if (currentIndex < Title.Length)
            {
                formattedString.Spans.Add(new Span { Text = Title.Substring(currentIndex), TextColor = Colors.Black, FontAttributes = FontAttributes.None });
            }

            FormattedTitle = formattedString;
        }
    
        // Counts the number of case-insensitive keyword matches in the book title
        public int GetMatchCount(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return 0;
            return Regex.Matches(Title, Regex.Escape(keyword), RegexOptions.IgnoreCase).Count;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}