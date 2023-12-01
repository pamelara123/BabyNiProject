using System;
using System.IO;

public class FileWatcher
{
    private string sourceDirectory = @"C:\Users\User\Desktop\BabyNiProject\Parser";
    private string outputDirectory = @"C:\Users\User\Desktop\BabyNiProject\Output";

    public void Start()
    {
        // Create output directory if it doesn't exist
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        // Create a FileSystemWatcher to monitor the source directory
        FileSystemWatcher watcher = new FileSystemWatcher();
        watcher.Path = sourceDirectory;

        // Watch for changes to text files only
        watcher.Filter = "*.txt";

        // Set event handlers
        watcher.Created += OnFileCreated;

        // Start monitoring
        watcher.EnableRaisingEvents = true;

        // Handle existing files on startup
        HandleExistingFiles();

        Console.WriteLine("FileWatcher is running. Press any key to exit.");
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        // Get the source and destination paths
        string sourceFile = e.FullPath;
        string destinationFile = Path.Combine(outputDirectory, Path.ChangeExtension(Path.GetFileName(e.Name), "csv"));

        // Check if the destination file already exists
        if (File.Exists(destinationFile))
        {
            // If it exists, delete the duplicate file
            File.Delete(sourceFile);
            Console.WriteLine($"Duplicate file '{e.Name}' detected and deleted.");
        }
        else
        {
            ParserFile parserFile = new ParserFile();
            parserFile.ConvertToCsv(sourceDirectory, outputDirectory);

            Console.WriteLine($"File '{e.Name}' moved to the 'Output' folder and converted to CSV.");
        }
    }

    private void HandleExistingFiles()
    {
        // Handle existing files in the source directory on startup
        foreach (var sourceFilePath in Directory.EnumerateFiles(sourceDirectory, "*.txt"))
        {
            string destinationFile = Path.Combine(outputDirectory, Path.ChangeExtension(Path.GetFileName(sourceFilePath), "csv"));

            // Check if the destination file already exists
            if (!File.Exists(destinationFile))
            {
                // If it doesn't exist, move the file to the destination folder
                File.Move(sourceFilePath, destinationFile);
                ParserFile parserFile = new ParserFile();
                parserFile.ConvertToCsv(sourceDirectory, destinationFile);

                Console.WriteLine($"File '{Path.GetFileName(sourceFilePath)}' moved to the 'Output' folder and converted to CSV.");
            }
        }
    }
}
