using System;
using System.IO;
using Vertica.Data.VerticaClient;

public class FileLoader
{
    private readonly string connectionString;

    public FileLoader(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public void LoadDataToDatabase(string sourceDirectory)
    {
        try
        {
            // Create tables in the database
            CreateTables();

            // Create additional tables
            CreateAdditionalTables();

            // Load data from CSV files to the database
            LoadDataFromCsvFiles(sourceDirectory);

            // Load data into hourly_aggregation and daily_aggregation
            LoadDataIntoHourlyAggregation();
            LoadDataIntoDailyAggregation();

        //    Console.WriteLine("Data loaded to the database successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    private void CreateTables()
    {
        using (VerticaConnection connection = new VerticaConnection(connectionString))
        {
            connection.Open();

            // Create table queries - Modify these queries based on your actual table structure
            string createTableQuery1 = "CREATE TABLE IF NOT EXISTS Table1 (NETWORK_SID INT, DATETIME_KEY TIMESTAMP, NEID FLOAT, OBJECT VARCHAR(255),TIME TIMESTAMP, \"INTERVAL\" INT, DIRECTION VARCHAR(255), NEALIAS VARCHAR(255), NETYPE VARCHAR(255), RXLEVELBELOWTS1 INT, RXLEVELBELOWTS2 INT, MINRXLEVEL FLOAT, MAXRXLEVEL FLOAT, TXLEVELABOVETS1 INT, MINTXLEVEL FLOAT, MAXTXLEVEL FLOAT, FAILUREDESCRIPTION VARCHAR(255), LINK VARCHAR(255), TID VARCHAR(255), FARENDTID VARCHAR(255), SLOT INT, PORT INT)";
            string createTableQuery2 = "CREATE TABLE IF NOT EXISTS Table2 (NETWORK_SID INT, DATETIME_KEY TIMESTAMP, NODENAME VARCHAR(255), NEID INT, OBJECT VARCHAR(255), TIME TIMESTAMP, \"INTERVAL\" INT, DIRECTION VARCHAR(255), NEALIAS VARCHAR(255), NETYPE VARCHAR(255), RFINPUTPOWER FLOAT, TID VARCHAR(255), FARENDTID VARCHAR(255), SLOT VARCHAR(255), PORT INT)";

            using (VerticaCommand command1 = new VerticaCommand(createTableQuery1, connection))
            {
                command1.ExecuteNonQuery();
            }

            using (VerticaCommand command2 =  new VerticaCommand(createTableQuery2, connection))
            {
                command2.ExecuteNonQuery();
            }
        }
    }

    private void CreateAdditionalTables()
    {
        using (VerticaConnection connection = new VerticaConnection(connectionString))
        {
            connection.Open();

            // Create table queries for hourly_aggregation and daily_aggregation
            string createHourlyAggregationTableQuery = "CREATE TABLE IF NOT EXISTS hourly_aggregation (NETWORK_SID INT, DATETIME_KEY TIMESTAMP, Time TIMESTAMP, NeAlias VARCHAR(255), NeType VARCHAR(255), RSL_INPUT_POWER FLOAT, MaxRxLevel FLOAT, RSL_Deviation FLOAT)";
            string createDailyAggregationTableQuery = "CREATE TABLE IF NOT EXISTS daily_aggregation (NETWORK_SID INT, DATETIME_KEY TIMESTAMP, Time TIMESTAMP, NeAlias VARCHAR(255), NeType VARCHAR(255), RSL_INPUT_POWER FLOAT, MaxRxLevel FLOAT, RSL_Deviation FLOAT)";

            using (VerticaCommand commandHourly = new VerticaCommand(createHourlyAggregationTableQuery, connection))
            {
                commandHourly.ExecuteNonQuery();
            }

            using (VerticaCommand commandDaily = new VerticaCommand(createDailyAggregationTableQuery, connection))
            {
                commandDaily.ExecuteNonQuery();
            }
        }
    }

    private void LoadDataFromCsvFiles(string sourceDirectory)
    {
        // Get all CSV files in the source directory
        string[] csvFiles = Directory.GetFiles(sourceDirectory, "*.csv");

        foreach (string csvFile in csvFiles)
        {
            // Determine the table name based on the file name
            string tableName = GetTableNameFromCsvFile(csvFile);

            // Load data from CSV file to the database
            LoadDataFromCsv(csvFile, tableName);
        }
    }

    private void LoadDataFromCsv(string csvFile, string tableName)
    {
        using (VerticaConnection connection = new VerticaConnection(connectionString))
        {
            connection.Open();

            // Modify the query based on your actual table structure
            string copyCommand = $"COPY {tableName} FROM LOCAL '{csvFile.Replace("\\", "/")}' DELIMITER ',' skip 1 EXCEPTIONS  '{csvFile}_exceptions.txt'";

            using (VerticaCommand command = new VerticaCommand(copyCommand, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }

    private void LoadDataIntoHourlyAggregation()
    {
        using (VerticaConnection connection = new VerticaConnection(connectionString))
        {
            connection.Open();

            // Modify the query based on your actual table structure
            string hourlyAggregationQuery = @"
                INSERT INTO hourly_aggregation (
                    NETWORK_SID, DATETIME_KEY, Time, NeAlias, NeType, RSL_INPUT_POWER, MaxRxLevel, RSL_Deviation
                )
                SELECT 
                    rf.NETWORK_SID INT,
                    rf.DATETIME_KEY TIMESTAMP,
                    date_trunc('hour', rp.""Time"") as ""Time"",
                    rf.NeAlias,
                    rf.NeType,
                    Max(rp.MaxRxLevel) as MaxRxLevel,
                    Max(rf.RFInputPower) as RSL_INPUT_POWER,
                    abs(Max(rp.MaxRxLevel)) - abs(Max(rf.RFInputPower)) as RSL_Deviation
                FROM  
                    Table2 rf
                    INNER JOIN Table1 rp ON rf.NETWORK_SID = rp.NETWORK_SID 
                GROUP BY 
                    rf.NETWORK_SID, rf.DATETIME_KEY, date_trunc('hour', rp.""Time""), rf.NeAlias, rf.NeType;
            ";

            using (VerticaCommand command = new VerticaCommand(hourlyAggregationQuery, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }

    private void LoadDataIntoDailyAggregation()
    {
        using (VerticaConnection connection = new VerticaConnection(connectionString))
        {
            connection.Open();

            // Modify the query based on your actual table structure
            string dailyAggregationQuery = @"
                INSERT INTO daily_aggregation (
                    NETWORK_SID, DATETIME_KEY, Time, NeAlias, NeType, RSL_INPUT_POWER, MaxRxLevel, RSL_Deviation
                )
                SELECT 
                    rf.NETWORK_SID INT,
                    rf.DATETIME_KEY TIMESTAMP,
                    date_trunc('day', rp.""Time"") as ""Time"",
                    rf.NeAlias,
                    rf.NeType,
                    Max(rp.MaxRxLevel) as MaxRxLevel,
                    Max(rf.RFInputPower) as RSL_INPUT_POWER,
                    abs(Max(rp.MaxRxLevel)) - abs(Max(rf.RFInputPower)) as RSL_Deviation
                FROM  
                    Table2 rf
                    INNER JOIN Table1 rp ON rf.NETWORK_SID = rp.NETWORK_SID 
                GROUP BY 
                    rf.NETWORK_SID, rf.DATETIME_KEY, date_trunc('day', rp.""Time""), rf.NeAlias, rf.NeType;
            ";

            using (VerticaCommand command = new VerticaCommand(dailyAggregationQuery, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }

    private string GetTableNameFromCsvFile(string csvFile)
    {
        // Determine the table name based on the CSV file name
        if (csvFile.Contains("SOEM1_TN_RADIO_LINK_POWER"))
        {
            return "Table1";
        }
        else if (csvFile.Contains("SOEM1_TN_RFInputPower"))
        {
            return "Table2";
        }
        else
        {
            throw new InvalidOperationException("Unable to determine the table name from the CSV file.");
        }
    }
}
