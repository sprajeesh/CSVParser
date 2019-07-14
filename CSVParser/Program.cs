using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;

namespace CSVParser
{
    class Program
    {
        public static void Main(string[] args)
        {
            string filePath = args[0];

            Console.WriteLine("Validating the selected file {0}", filePath);
            int returnValue = ValidateCSVFile(filePath);

            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string fileType = Path.GetExtension(filePath);
            string directory = Path.GetDirectoryName(filePath);
            
            if (returnValue == 1)
            {
                DataTable dataTable = ParseCSV(filePath);

                if (dataTable.Rows.Count > 0)
                {
                    // Create this as a separate CSV
                    string newCSVFile = directory + "\\" + fileName + "_Updated" + fileType;
                    ExportDataTableToCSV(dataTable, newCSVFile);
                }
            }
            Console.ReadLine();
        }

        public static int ValidateCSVFile(string filePath)
        {
            // Should be CSV file
            string fileExtension = Path.GetExtension(filePath);
            if (fileExtension.ToUpper() != ".CSV")
            {
                Console.WriteLine("Passed file is not a CSV file");
                return -1;
            }

            return 1;
        }

        public static DataTable ParseCSV(string filePath)
        {
            Console.WriteLine("Parsing the CSV file {0}", filePath);

            DataTable csvData = new DataTable();

            try
            {
                using (TextFieldParser csvReader = new TextFieldParser(filePath))
                {
                    csvReader.SetDelimiters(new string[] { ","});
                    csvReader.HasFieldsEnclosedInQuotes = true;

                    // Read the column names
                    string[] csvColNames = csvReader.ReadFields();
                    foreach (string column in csvColNames)
                    {
                        try
                        {
                            DataColumn dataCol = new DataColumn(column);
                            dataCol.AllowDBNull = true;

                            if (column.ToUpper() == "DATE")
                            {
                                dataCol.DataType = typeof(System.DateTime);
                            }                            
                            csvData.Columns.Add(dataCol);                            
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error while passing the column header names");
                            Console.WriteLine(ex.Message);
                            Console.ReadLine();
                        }
                    }

                    // Read the column data
                    while (!csvReader.EndOfData)
                    {
                        try
                        {
                            string[] dataFields = csvReader.ReadFields();

                            for (int i = 0; i < dataFields.Length; i++)
                            {
                                System.Type dataType = csvData.Columns[i].DataType;
                                if (dataType == typeof(System.DateTime))
                                {
                                    DateTime returnDate = ParseDate(dataFields[i], out int result);

                                    if (result == 1)
                                    {
                                        Console.WriteLine("Manually parse successfully done. Changed {0} to {1}", dataFields[i], returnDate.Date);
                                        Console.WriteLine("Day, Month and Year are {0}, {1}, {2}", returnDate.Day, returnDate.Month, returnDate.Year);
                                        returnDate = returnDate.Date;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Manual parse failed");
                                    }
                                    dataFields[i] = returnDate.Date.ToString();
                                }

                                // The description field can have values like "value 1, value 2"
                                // This is a single value
                                string data = dataFields[i];
                                if (data != "" && data.IndexOf(",") > 0)
                                {
                                    Console.WriteLine("The data is {0}", data);

                                    // Enclose this in a double string
                                    dataFields[i] = '"' + data + '"';
                                }
                            }
                            csvData.Rows.Add(dataFields);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error while parsing the Field Data");
                            Console.WriteLine(ex.Message);
                            Console.ReadLine();
                        }
                    }
                    csvReader.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while parsing the CSV file {0}", filePath);
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
            finally
            {
                Console.WriteLine("DataTable created successfully for {0}", filePath);
                Console.WriteLine("Total records {0}", csvData.Rows.Count.ToString());
            }
            return csvData;
        }

        public static DateTime ParseDate(string value, out int result)
        {
            DateTime returnValue = DateTime.Now;
            result = -1;

            try
            {
                // value will be in m/dd/yy format
                int firstSlashPos = value.IndexOf("/");
                int lastSlashPos = value.LastIndexOf("/");

                string month = value.Substring(0, firstSlashPos);
                month = string.Format("{00:00}", month);

                string date = value.Substring(firstSlashPos + 1, (lastSlashPos - firstSlashPos) - 1);
                date = string.Format("{00:00}", date);

                string year = value.Substring(lastSlashPos + 1);
                if (year == "19")
                {
                    year = "2019";
                }
                else if (year == "18")
                {
                    year = "2018";
                }

                // Concatenate the value
                string finalValue = date + "/" + month + "/" + year;

                // Change the string to date value
                bool success = DateTime.TryParse(finalValue, out DateTime outResult);
                if (success == true)
                {
                    returnValue = outResult;
                    result = 1;
                    Console.WriteLine("Successfully parsed {0} to {1}", finalValue, result);
                }
                else
                {
                    Console.WriteLine("Unable to parse {0}", finalValue);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to parse string to datetime");
                Console.WriteLine(ex.Message);
            }
            return returnValue;
        }

        public static void ExportDataTableToCSV(DataTable dataTable, string newCSVFileName)
        {
            StringBuilder builder = new StringBuilder();

            try
            {
                IEnumerable<string> columnNames = dataTable.Columns.Cast<DataColumn>().
                                      Select(column => column.ColumnName);
                builder.AppendLine(string.Join(",", columnNames));

                foreach (DataRow row in dataTable.Rows)
                {
                    IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                    builder.AppendLine(string.Join(",", fields));
                }

                File.WriteAllText(newCSVFileName, builder.ToString());
                Console.WriteLine("Modified contents has been written successfully to the file {0}", newCSVFileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while converting the DataTable to CSV");
                Console.WriteLine(ex.Message);
            }
        }
    }
}
