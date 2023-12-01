using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class ParserFile
{
    public void ConvertToCsv(string sourceDirectory, string destinationDirectory)
    {

        Console.WriteLine("hellooo");
        try
        {
            // Check if the source directory exists
            if (!Directory.Exists(sourceDirectory))
            {
                Console.WriteLine($"Source directory '{sourceDirectory}' does not exist.");
                return;
            }

            // Check if the destination directory exists, and create it if not
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
                Console.WriteLine(destinationDirectory);
            }

            // Get all .txt files in the source directory
            string[] txtFiles = Directory.GetFiles(sourceDirectory, "*.txt");

            foreach (string txtFile in txtFiles)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(txtFile);

                // Check if the current file's name matches the first specified name
                if (fileNameWithoutExtension == "SOEM1_TN_RADIO_LINK_POWER_20200312_001500")
                {
                    ProcessRadioLinkPowerFile(txtFile, destinationDirectory);
                }
                // Check if the current file's name matches the second specified name
                else if (fileNameWithoutExtension == "SOEM1_TN_RFInputPower_20210121_051500")
                {
                    ProcessRFInputPowerFile(txtFile, destinationDirectory);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    private void ProcessRadioLinkPowerFile(string txtFile, string destinationDirectory)
    {
        string csvFileName = Path.GetFileNameWithoutExtension(txtFile) + ".csv";
        string csvFile = Path.Combine(destinationDirectory, csvFileName);

        string[] lines = File.ReadAllLines(txtFile);

        if (lines.Length > 0)
        {
            string header = lines[0];
            string[] headerFields = header.Split(',');

            Dictionary<string, int> headerIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < headerFields.Length; i++)
            {
                headerIndices[headerFields[i]] = i;
            }

            if (headerIndices.ContainsKey("Object"))
            {
                int objectIndex = headerIndices["Object"];
                int failureIndex = headerIndices["FailureDescription"];

                var dataLines = new List<string>();
                List<string> modifiedHeaderFields = headerFields.ToList();
                modifiedHeaderFields.Insert(0, "NETWORK_SID");
                modifiedHeaderFields.Insert(1, "datetime_key");
                modifiedHeaderFields.Add("Link"); // Add new header "Link"
                modifiedHeaderFields.Add("TID"); // Add new header "TID"
                modifiedHeaderFields.Add("FARENDTID");
                modifiedHeaderFields.Add("Slot"); // Add new header "Slot"
                modifiedHeaderFields.Add("Port"); // Add new header "Port"


                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(txtFile);
                string dateFromFileName = ExtractDateFromFileName(fileNameWithoutExtension);

                for (int i = 1; i < lines.Length; i++)
                {
                    string[] fields = lines[i].Split(',');

                    string networkSID = CalculateNetworkSID(fields, headerIndices);
                    // Check if the "FailureDescription" is not equal to "-"
                    if (fields[failureIndex] != "-")
                    {
                        continue; // Skip the row if "FailureDescription" is not "-"
                    }

                    if (!fields[objectIndex].Equals("Unreachable Bulk FC"))
                    {
                        var filteredFields = fields
                            .Where((_, index) => index != headerIndices["NodeName"] && index != headerIndices["Position"] && index != headerIndices["IdLogNum"])
                            .ToArray();

                        filteredFields = AddNetworkSIDToModifiedFields(networkSID, dateFromFileName, filteredFields);

                        // Call the new "Link" function to process and add the "Link" column
                        string linkValue = ProcessLinkField(fields[objectIndex]);
                        filteredFields = filteredFields.Append(linkValue).ToArray();

                        // Extract the TID from the "Object" column
                        string tidValue = ExtractTIDFromObject(fields[objectIndex]);
                        filteredFields = filteredFields.Append(tidValue).ToArray(); // Add "TID" value

                        string farendtidValue = ExtractFARENDTIDFromObject(fields[objectIndex]);
                        filteredFields = filteredFields.Append(farendtidValue).ToArray(); // Add "FARENDTID" value

                        // Add the "slotport" values using the modified GenerateSlotPortValues function
                        List<Tuple<string, string>> slotPortValues = GenerateSlotPortValues(linkValue);

                        foreach (var slotPortValue in slotPortValues)
                        {
                            // Clone filteredFields to avoid modifying the same instance
                            string[] modifiedFields = filteredFields.ToArray();

                            // Add the slot and port values to separate columns
                            modifiedFields = modifiedFields.Append(slotPortValue.Item1).Append(slotPortValue.Item2).ToArray();

                            dataLines.Add(string.Join(",", modifiedFields));
                        }

                    }
                }

                string csvContent = string.Join(",", modifiedHeaderFields.Where(header => header != "NodeName" && header != "Position" && header != "IdLogNum")) + Environment.NewLine;
                csvContent += string.Join(Environment.NewLine, dataLines);

                File.WriteAllText(csvFile, csvContent);
                Console.WriteLine($"File '{Path.GetFileName(txtFile)}' converted to '{csvFileName}'.");
            }
        }
    }

    private string ExtractDateFromFileName(string fileNameWithoutExtension)
    {
        // Assuming the date is in the format "yyyyMMdd_HHmmss"
        string[] parts = fileNameWithoutExtension.Split('_');
        if (parts.Length >= 2)
        {
            string datePart = parts[parts.Length - 2] + parts[parts.Length-1]; // Get the second-to-last part as the date
            Console.WriteLine(datePart);
            if (DateTime.TryParseExact(datePart, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out DateTime date))
            {
                // Format the date as "dd-MM-yyyy HH:mm:ss"
                return date.ToString("dd/MM/yyyy HH:mm:ss");
            }
        }
        return string.Empty;
    }

    private string CalculateNetworkSID(string[] fields, Dictionary<string, int> headerIndices)
    {
        if (headerIndices.ContainsKey("NEALIAS") && headerIndices.ContainsKey("NETYPE"))
        {
            string neAlias = fields[headerIndices["NEALIAS"]];
            string neType = fields[headerIndices["NETYPE"]];
            string concatenatedValue = neAlias + neType;
            int hashValue = Math.Abs(concatenatedValue.GetHashCode()); // Get the absolute value of the hash code
            return hashValue.ToString();
        }
        return string.Empty;
    }

    private string[] AddNetworkSIDToModifiedFields(string networkSID, string dateFromFileName, string[] fields)
    {
        List<string> modifiedFields = fields.ToList();
        modifiedFields.Insert(0, networkSID);
        modifiedFields.Insert(1, dateFromFileName); // Add the datetime_key
        return modifiedFields.ToArray();
    }

    // New "Link" function to process and add the "Link" column
    private string ProcessLinkField(string objectData)
    {
        string[] objectParts = objectData.Split('_');
        if (objectParts.Length > 1)
        {
            string linkValue = objectParts[0]; // Take the part before the first "_"
            if (linkValue.Contains("."))
            {
                string[] linkParts = linkValue.Split('.');
                if (linkParts.Length == 2)
                {
                    string slot = linkParts[0];
                    string port = linkParts[1];
                    linkValue = $"{slot}/{port}";// 1/6/2/1
                }
            }
            string[] linkValueParts = linkValue.Split('/');
            if (linkValueParts.Length > 1)
            {
                string result = linkValueParts[1] + "/" + linkValueParts[2];
                return result;
            }
            return string.Empty;
        }
        else
        {
            string[] linkParts = objectParts[0].Split('/');
            if (linkParts.Length > 1)
            {
                string slotPortValue = string.Join("/", linkParts.Skip(1)); // Skip the first number
                return slotPortValue;
            }
        }
        return string.Empty;
    }
    // New function to extract TID from the "Object" column
    private string ExtractTIDFromObject(string objectData)
    {
        // Split the "Object" value by "_" and extract the value between the first and last "_"
        string[] objectParts = objectData.Split("_");
        if (objectParts.Length >= 3) // Make sure there are at least 3 parts
        {
            string tidValue = objectParts[2]; // The second part is the TID
            return tidValue;
        }
        return string.Empty; // Handle cases where the format is not as expected
    }

    private string ExtractFARENDTIDFromObject(string objectData)
    {
        // Split the "Object" value by "_" and extract the value between the first and last "_"
        string[] objectParts = objectData.Split("_");
        if (objectParts.Length >= 3) // Make sure there are at least 3 parts
        {
            string farendtidValue = objectParts[4];
            return farendtidValue;
        }
        return string.Empty; // Handle cases where the format is not as expected
    }

    private List<Tuple<string, string>> GenerateSlotPortValues(string linkData)
    {
        List<Tuple<string, string>> slotPortValues = new List<Tuple<string, string>>();

        // Split the linkData using '+' as delimiter
        string[] linkParts = linkData.Split('+');

        foreach (string linkPart in linkParts)
        {
            // Split each linkPart using '/' as delimiter
            string[] slotPortDataParts = linkPart.Split('/');

            if (slotPortDataParts.Length == 2)
            {
                string slot = slotPortDataParts[0];
                string port = slotPortDataParts[1];

                slotPortValues.Add(new Tuple<string, string>(slot, port));
            }
            else if (slotPortDataParts.Length == 1)
            {
                // If only one part, assume it's the slot, and set port to "1"
                string slot = slotPortDataParts[0];
                string port = "1";
                slotPortValues.Add(new Tuple<string, string>(slot, port));
            }
            else
            {
                // Handle cases where the format is not as expected
                // You can add custom logic here for different formats

                // Assuming the format is like "18+19/1"
                string[] slotPortValuesTemp = linkPart.Split('/');

                if (slotPortValuesTemp.Length == 2)
                {
                    string slot = slotPortValuesTemp[0];
                    string port = slotPortValuesTemp[1];
                    slotPortValues.Add(new Tuple<string, string>(slot, port));
                }
                else
                {
                    // Handle other cases if needed
                    slotPortValues.Add(Tuple.Create(string.Empty, string.Empty));
                }
            }
        }

        return slotPortValues;
    }



  
    private void ProcessRFInputPowerFile(string txtFile, string destinationDirectory)
    {
        string csvFileName = Path.GetFileNameWithoutExtension(txtFile) + ".csv";
        string csvFile = Path.Combine(destinationDirectory, csvFileName);

        string[] lines = File.ReadAllLines(txtFile);

        if (lines.Length > 0)
        {
            string header = lines[0];
            string[] headerFields = header.Split(',');

            List<string> modifiedHeaderFields = headerFields
                .Where(col => col.Trim().ToUpper() != "POSITION" &&
                              col.Trim().ToUpper() != "MEANRXLEVEL1M" &&
                              col.Trim().ToUpper() != "IDLOGNUM" &&
                              col.Trim().ToUpper() != "FAILUREDESCRIPTION")
                .ToList();

            Dictionary<string, int> headerIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < headerFields.Length; i++)
            {
                headerIndices[headerFields[i]] = i;
            }

            var dataLines = new List<string>();
            modifiedHeaderFields.Insert(0, "NETWORK_SID");
            modifiedHeaderFields.Insert(1, "datetime_key");
            modifiedHeaderFields.Add("Slot"); // Add new header "Slot"
            modifiedHeaderFields.Add("Port"); // Add new header "Port"

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(txtFile);
            string dateFromFileName = ExtractDateFromFileName(fileNameWithoutExtension);
            DateTime dt = DateTime.ParseExact(dateFromFileName, "dd/MM/yyyy HH:mm:ss", null);

            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(',');

                // Get the index of the "farendtid" column
                int farendtidIndex = headerIndices["farendtid"];

                // Check if the value in the "farendtid" column is equal to "----"
                if (fields[farendtidIndex].Trim() == "----")
                {
                    // Skip this row and continue to the next one
                    continue;
                }

                string networkSID = CalculateNetworkSID(fields, headerIndices);

                var filteredFields = fields
                    .Where((field, index) =>
                        index != headerIndices["POSITION"] &&
                        index != headerIndices["MEANRXLEVEL1M"] &&
                        index != headerIndices["IDLOGNUM"] &&
                        index != headerIndices["FAILUREDESCRIPTION"])
                    .ToList();

                filteredFields.Insert(0, networkSID);
                filteredFields.Insert(1, dt.ToString());

                // Get the index of the "object" column
                int objectIndex = headerIndices["object"];

                // Get the data from the "object" column
                string objectData = fields[objectIndex].Trim();

                // Manipulate the "object" data to get the desired "Slot" value
                string slotValue = GetSlotValue(objectData);

                // Add the "Slot" value to the filtered fields
                filteredFields.Add(slotValue);


                string portValue = GetPortValue(objectData);
                filteredFields.Add(portValue);

                dataLines.Add(string.Join(",", filteredFields));
            }

            string csvContent = string.Join(",", modifiedHeaderFields) + Environment.NewLine;
            csvContent += string.Join(Environment.NewLine, dataLines);

            File.WriteAllText(csvFile, csvContent);
            Console.WriteLine($"File '{Path.GetFileName(txtFile)}' converted to '{csvFileName}'.");
        }
    }

    private string GetSlotValue(string objectData)
    {
        

        // Find the first occurrence of "."
        int indexOfDot = objectData.IndexOf('.');

        if (indexOfDot != -1)
        {
            // Trim the string to the part before the first "."
            objectData = objectData.Substring(0, indexOfDot + 1);

            // Replace every occurrence of "." with "+"
            objectData = objectData.Replace(".", "+");
        }

        return objectData;
    }
    private string GetPortValue(string objectData)
    {
        int indexOfSlash = objectData.IndexOf('/');
        if (indexOfSlash != -1)
        {
            objectData = objectData.Substring(0, indexOfSlash);
        }
        return objectData;
    }



}
