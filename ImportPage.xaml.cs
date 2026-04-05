using LocalBookManager.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;

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

        // Open the system file picker and select supported book files
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

        // Save selected books to app storage and database, then return to main page
        private async void OnConfirmClicked(object sender, EventArgs e)
        {
            int successCount = 0; // Track the number of successfully imported books

            foreach (var book in _selectedFiles)
            {
                try
                {
                    // 1. Copy the file to the app's sandbox directory
                    string fileName = Path.GetFileName(book.FilePath);
                    string targetPath = Path.Combine(FileSystem.AppDataDirectory, fileName);

                    // Overwrite the file if it already exists with the same name
                    File.Copy(book.FilePath, targetPath, true);

                    // Update the book's file path to the sandbox path
                    book.FilePath = targetPath;

                    // 2. Save book data to SQLite database
                    await AppDatabase.SaveBookAsync(book);

                    // 3. Update the in-memory list on the main page
                    _mainPage.AddBook(book);

                    successCount++; // Increment count after successful import
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Import Error", $"Failed to import {book.Title}: {ex.Message}", "OK");
                }
            }

            // Trigger local notification if any books were imported successfully
            if (successCount > 0)
            {
                var request = new NotificationRequest
                {
                    NotificationId = 1002,
                    Title = "Book Import Completed",
                    Description = $"Successfully added {successCount} books to your local bookshelf.",
                    BadgeNumber = successCount,
                    Schedule = new NotificationRequestSchedule { NotifyTime = DateTime.Now } // Send notification immediately
                };

                await LocalNotificationCenter.Current.Show(request);
            }

            await Navigation.PopModalAsync();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}