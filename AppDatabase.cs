using SQLite;
using LocalBookManager.Models;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LocalBookManager
{
    // Static SQLite database helper class for book data management
    public static class AppDatabase
    {
        // Asynchronous SQLite database connection instance
        private static SQLiteAsyncConnection _db;

        // Initialize database connection and create Book table if it doesn't exist
        public static async Task InitAsync()
        {
            // Skip initialization if connection already exists
            if (_db != null) return;

            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "LocalBooks.db3");
            _db = new SQLiteAsyncConnection(dbPath);
            await _db.CreateTableAsync<Book>();

            System.Diagnostics.Debug.WriteLine($"[Storage Path] : {dbPath}");
        }

        public static async Task<List<Book>> GetBooksAsync()
        {
            await InitAsync();
            return await _db.Table<Book>().ToListAsync();
        }

        // Save or update a book record
        public static async Task SaveBookAsync(Book book)
        {
            await InitAsync();
            if (book.Id != 0)
            {
                await _db.UpdateAsync(book);
            }
            else
            {
                await _db.InsertAsync(book);
            }
        }
        // Delete a specific book record from the database
        public static async Task DeleteBookAsync(Book book)
        {
            await InitAsync();
            await _db.DeleteAsync(book);
        }
    }
}