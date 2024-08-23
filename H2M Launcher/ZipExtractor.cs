using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace H2M_Launcher
{
    public class ZipExtractor
    {
        public static async Task ExtractZipToFolderAsync(string zipFilePath, string extractionFolder)
        {
            // Ensure the extraction folder exists
            Directory.CreateDirectory(extractionFolder);

            // Run the extraction logic on a background thread
            await Task.Run(() =>
            {
                using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        // Create the full path for the entry
                        string filePath = Path.Combine(extractionFolder, entry.FullName);

                        // Ensure the directory exists
                        string directoryPath = Path.GetDirectoryName(filePath);
                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }

                        // Extract the file if it's not a directory
                        if (!string.IsNullOrEmpty(entry.Name))
                        {
                            entry.ExtractToFile(filePath, overwrite: true);
                        }
                    }
                }
            });
        }

        public static async Task ProcessZipAsync(string zipFilePath, string extractionFolder)
        {
            try
            {
                // Extract the ZIP file
                await ExtractZipToFolderAsync(zipFilePath, extractionFolder);
                Console.WriteLine("Processing complete.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}
