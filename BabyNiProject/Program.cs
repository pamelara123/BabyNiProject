using BabyNiProject;
using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("FileWatcher Application");
        dbConnection connectionn=new dbConnection();
        string connection = connectionn.ConnectionString();

        // Create instances of FileWatcher and ParserFile
        FileWatcher fileWatcher = new FileWatcher();
        fileWatcher.Start();
        FileLoader fileLoader = new FileLoader(connection); // Replace with your actual connection string

        
      //  string sourceDirectory = @"C:\Users\User\Desktop\BabyNiProject\Parser";
        string destinationDirectory = @"C:\Users\User\Desktop\BabyNiProject\Output";

        // Load data to the database
        fileLoader.LoadDataToDatabase(destinationDirectory);

      
        Console.ReadKey();
     

    }
}
