using LocalBookManager.Models;
using Plugin.LocalNotification;
using System.Collections.ObjectModel;

namespace LocalBookManager
{
    public partial class MainPage : ContentPage
    {
        // Separate master data repository and displayed data
        public ObservableCollection<Book> MasterBooks { get; set; } = new ObservableCollection<Book>();
        public ObservableCollection<Book> DisplayedBooks { get; set; } = new ObservableCollection<Book>();

        private bool _isSelectionMode = false;
        private string _lastSearchedText = null; // Guard variable to prevent ghost search triggers

        public MainPage()
        {
            InitializeComponent();
            BookCollectionView.ItemsSource = DisplayedBooks;
        }

        // Safe add method provided for the Import Page
        public void AddBook(Book book)
        {
            MasterBooks.Add(book);
            PerformSearch(MainSearchBar.Text); // Re-execute search filter to ensure correct categorization
        }

        // Triggered in real-time when the search box text changes 
        private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
        {
            // Skip if the text hasn't actually changed (e.g., losing focus)
            if (_lastSearchedText == e.NewTextValue) return;

            _lastSearchedText = e.NewTextValue;
            PerformSearch(e.NewTextValue);
        }

        // Triggered when the user presses "Enter/Search" on the keyboard
        private void OnSearchButtonPressed(object sender, EventArgs e)
        {
            // Iterate through the currently displayed books and reset their formatted text (restore normal font)
            foreach (var book in DisplayedBooks)
            {
                book.UpdateHighlight("");
            }

            // Remove focus from the search bar to automatically close the virtual keyboard on mobile devices
            MainSearchBar.Unfocus();
        }

        // Core logic: Filtering, sorting, and highlighting
        private void PerformSearch(string keyword)
        {
            DisplayedBooks.Clear();

            if (string.IsNullOrWhiteSpace(keyword))
            {
                // No keyword: Show all, clear highlights, restore default order
                foreach (var book in MasterBooks)
                {
                    book.UpdateHighlight("");
                    DisplayedBooks.Add(book);
                }
            }
            else
            {
                // Keyword exists: Only keep books whose titles contain the keyword
                var filtered = MasterBooks
                    .Where(b => b.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // Sort: The more matches (or matching characters), the higher the rank
                var sorted = filtered.OrderByDescending(b => b.GetMatchCount(keyword)).ToList();

                // Update highlights and add to the display list
                foreach (var book in sorted)
                {
                    book.UpdateHighlight(keyword);
                    DisplayedBooks.Add(book);
                }
            }
        }

        private async void OnImportClicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new ImportPage(this));
        }

        private async void OnBookSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isSelectionMode)
            {
                if (e.CurrentSelection.FirstOrDefault() is Book selectedBook)
                {
                    BookCollectionView.SelectedItem = null;
                    // Navigate to the ReadPage and pass the selected book
                    await Navigation.PushModalAsync(new ReadPage(selectedBook));
                }
            }
        }

        private async void OnSelectClicked(object sender, EventArgs e)
        {
            if (!_isSelectionMode)
            {
                _isSelectionMode = true;
                BtnSelect.Text = "🗑️ Delete";
                BtnSelect.TextColor = Colors.Red;
                BtnCancel.IsVisible = true;
                BtnImport.IsVisible = false;

                BookCollectionView.SelectionMode = SelectionMode.Multiple;
                BookCollectionView.SelectedItems?.Clear();
            }
            else
            {
                var selectedBooks = BookCollectionView.SelectedItems?.Cast<Book>().ToList();
                if (selectedBooks == null || !selectedBooks.Any())
                {
                    ExitSelectionMode();
                    return;
                }

                // Build confirmation message listing the selected book titles
                string bookList = string.Join("\n", selectedBooks.Select(b => $"• {b.Title}"));
                string message = selectedBooks.Count == 1
                    ? $"Are you sure you want to delete \"{selectedBooks[0].Title}\"?\nThis action cannot be undone."
                    : $"Are you sure you want to delete these {selectedBooks.Count} books?\n{bookList}\n\nThis action cannot be undone.";

                bool confirmed = await DisplayAlert("Confirm Delete", message, "Delete", "Cancel");
                if (!confirmed) return; // User pressed Cancel — do nothing

                foreach (var book in selectedBooks)
                {
                    // 1. Delete database record
                    await AppDatabase.DeleteBookAsync(book);

                    // 2. Delete local physical file
                    if (File.Exists(book.FilePath))
                        File.Delete(book.FilePath);

                    // 3. Remove from UI lists
                    MasterBooks.Remove(book);
                    DisplayedBooks.Remove(book);
                }

                ExitSelectionMode();
            }
        }

        private void OnCancelClicked(object sender, EventArgs e)
        {
            ExitSelectionMode();
        }

        private void ExitSelectionMode()
        {
            _isSelectionMode = false;
            BtnSelect.Text = "✔️ Select";
            BtnSelect.TextColor = Colors.Black;
            BtnCancel.IsVisible = false;
            BtnImport.IsVisible = true;
            BookCollectionView.SelectionMode = SelectionMode.Single;
            BookCollectionView.SelectedItem = null;

            // Completely clear the search content, returning the page to its default state
            MainSearchBar.Text = string.Empty;
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Dynamically request notification permissions
            if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
            {
                await LocalNotificationCenter.Current.RequestNotificationPermission();
            }


            // Retrieve the latest data from the database
            var dbBooks = await AppDatabase.GetBooksAsync();

            MasterBooks.Clear();
            foreach (var book in dbBooks)
            {
                book.InitializeTitle();
                MasterBooks.Add(book);
            }

            PerformSearch(MainSearchBar.Text);
        }
    }
}