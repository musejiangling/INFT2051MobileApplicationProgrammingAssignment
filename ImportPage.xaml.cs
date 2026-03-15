using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using LocalBookManager.Models;

namespace LocalBookManager
{
    public partial class ImportPage : ContentPage
    {
        private MainPage _mainPage;
        private List<Book> _selectedFiles = new List<Book>();

        // Constructor receives the reference passed from the main page
        public ImportPage(MainPage mainPage)
        {
            InitializeComponent();
            _mainPage = mainPage;
            LoadFilesAsync(); // Call the system file picker as soon as the page opens
        }

        private async void LoadFilesAsync()
        {
            try
            {
                // Define supported file types to ensure correct filtering across different operating systems
                var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".txt", ".epub", ".docx" } },
                    { DevicePlatform.macOS, new[] { "txt", "epub", "docx" } },
                    { DevicePlatform.iOS, new[] { "public.plain-text", "org.idpf.epub-container", "org.openxmlformats.wordprocessingml.document" } },
                    { DevicePlatform.Android, new[] { "text/plain", "application/epub+zip", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" } }
                });

                // Call the native file picker, supporting multiple selection
                var results = await FilePicker.Default.PickMultipleAsync(new PickOptions
                {
                    PickerTitle = "Please select book files",
                    FileTypes = customFileType
                });

                if (results != null)
                {
                    foreach (var file in results)
                    {
                        var fileInfo = new FileInfo(file.FullPath);
                        var newBook = new Book
                        {
                            Title = Path.GetFileNameWithoutExtension(file.FileName),
                            FilePath = file.FullPath,
                            FileType = fileInfo.Extension,
                            FileSize = (fileInfo.Length / 1024).ToString() + " KB",
                            ImportDate = DateTime.Now
                        };

                        // Initialize formatted text properties to prevent blank UI or errors
                        newBook.InitializeTitle();

                        _selectedFiles.Add(newBook);
                    }
                    // Display the selected files in the current page's list
                    FileCollectionView.ItemsSource = _selectedFiles;
                }
            }
            catch (Exception)
            {
                // User canceled the selection or a permission exception occurred
                await DisplayAlert("Notice", "File selection canceled or failed.", "OK");
            }
        }

        // Confirm button: Add the selected books to the main page and close the current page
        private async void OnConfirmClicked(object sender, EventArgs e)
        {
            foreach (var book in _selectedFiles)
            {
                // Use the new method provided by the main page; this automatically triggers search/sort logic
                _mainPage.AddBook(book);
            }
            await Navigation.PopModalAsync(); // Close current page, return to main page
        }

        // Back button: Close the current page directly
        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}