using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HeartPC
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<SensorData> CpuData { get; set; }
        public ObservableCollection<SensorData> RamData { get; set; }
        public ObservableCollection<SensorData> MotherboardData { get; set; }
        public ObservableCollection<SensorData> HardDriveData { get; set; }
        public ObservableCollection<SensorData> GpuData { get; set; }
        public ObservableCollection<SensorData> SystemData { get; set; }
        public ObservableCollection<string> RamDevices { get; set; }
        public ObservableCollection<string> HardDriveDevices { get; set; }
        public ObservableCollection<string> GpuDevices { get; set; }

        private readonly string logFilePath = "test_log.txt";
        private readonly TimeSpan expectedTestDuration = TimeSpan.FromSeconds(30); // норма для времени прохождения теста

        public MainWindow()
        {
            InitializeComponent();
            InitializeDataCollections();
            LoadSystemInfo();

            DataContext = this;
            LoadLogs();
        }

        private void InitializeDataCollections()
        {
            CpuData = new ObservableCollection<SensorData>();
            RamData = new ObservableCollection<SensorData>();
            MotherboardData = new ObservableCollection<SensorData>();
            HardDriveData = new ObservableCollection<SensorData>();
            GpuData = new ObservableCollection<SensorData>();
            SystemData = new ObservableCollection<SensorData>();
            RamDevices = new ObservableCollection<string>();
            HardDriveDevices = new ObservableCollection<string>();
            GpuDevices = new ObservableCollection<string>();

            cpuDataGrid.ItemsSource = CpuData;
            ramDataGrid.ItemsSource = RamData;
            motherboardDataGrid.ItemsSource = MotherboardData;
            hardDriveDataGrid.ItemsSource = HardDriveData;
            gpuDataGrid.ItemsSource = GpuData;
            systemDataGrid.ItemsSource = SystemData;
        }

        private async void LoadSystemInfo()
        {
            await Task.Run(() =>
            {
                LoadComponentInfo("Win32_Processor", CpuData, "CPU", cpuDataGrid);
                LoadComponentInfo("Win32_PhysicalMemory", RamData, "RAM", ramDataGrid, RamDevices);
                LoadComponentInfo("Win32_BaseBoard", MotherboardData, "Motherboard", motherboardDataGrid);
                LoadComponentInfo("Win32_DiskDrive", HardDriveData, "Hard Drive", hardDriveDataGrid, HardDriveDevices);
                LoadComponentInfo("Win32_VideoController", GpuData, "GPU", gpuDataGrid, GpuDevices);
                LoadComponentInfo("Win32_OperatingSystem", SystemData, "System", systemDataGrid);
            });
        }

        private void LoadComponentInfo(string query, ObservableCollection<SensorData> dataCollection, string componentName, DataGrid dataGrid, ObservableCollection<string> devices = null)
        {
            try
            {
                var searcher = new ManagementObjectSearcher($"select * from {query}");
                foreach (ManagementObject obj in searcher.Get())
                {
                    Dispatcher.Invoke(() =>
                    {
                        dataCollection.Clear();
                        switch (componentName)
                        {
                            case "CPU":
                                AddCpuData(obj, dataCollection);
                                break;
                            case "RAM":
                                AddRamData(obj, dataCollection);
                                break;
                            case "Motherboard":
                                AddMotherboardData(obj, dataCollection);
                                break;
                            case "Hard Drive":
                                AddHardDriveData(obj, dataCollection);
                                break;
                            case "GPU":
                                AddGpuData(obj, dataCollection);
                                break;
                            case "System":
                                AddSystemData(obj, dataCollection);
                                break;
                        }
                        if (devices != null)
                        {
                            devices.Add(obj["Name"]?.ToString());
                        }
                    });
                }
            }
            catch (ManagementException ex)
            {
                LogError($"Error loading {componentName} info: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                LogError($"Access error loading {componentName} info: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogError($"Unexpected error loading {componentName} info: {ex.Message}");
            }
        }

        private void LogError(string message)
        {
            string logMessage = $"{DateTime.Now}: {message}\n";
            File.AppendAllText("error_log.txt", logMessage);
        }

        private void AddCpuData(ManagementObject obj, ObservableCollection<SensorData> dataCollection)
        {
            var properties = new[] { "Role", "Name", "Caption", "Manufacturer", "SocketDesignation", "MaxClockSpeed", "NumberOfCores", "NumberOfLogicalProcessors", "L2CacheSize", "L3CacheSize", "Status" };
            foreach (var property in properties)
            {
                if (obj.Properties[property] != null && obj.Properties[property].Value != null)
                {
                    string value = obj[property].ToString();
                    if (property == "MaxClockSpeed")
                    {
                        value += " GHz";
                    }
                    else if (property == "L2CacheSize" || property == "L3CacheSize")
                    {
                        value += " KB";
                    }
                    dataCollection.Add(new SensorData { SensorName = property, SensorValue = value });
                }
                else
                {
                    LogError($"Property {property} not found in CPU data.");
                }
            }
        }

        private void AddRamData(ManagementObject obj, ObservableCollection<SensorData> dataCollection)
        {
            var properties = new[] { "Name", "Manufacturer", "BankLabel", "DeviceLocator", "PartNumber", "Capacity", "Attributes", "ConfiguredClockSpeed", "ConfiguredVoltage", "SerialNumber" };
            foreach (var property in properties)
            {
                if (obj.Properties[property]?.Value != null)
                {
                    string value = obj[property]?.ToString();
                    if (property == "Capacity" && ulong.TryParse(value, out ulong capacityInBytes))
                    {
                        double capacityInGB = capacityInBytes / (1024 * 1024 * 1024);
                        value = $"{capacityInGB:F2} GB";
                    }
                    dataCollection.Add(new SensorData { SensorName = property, SensorValue = value });
                }
                else
                {
                    LogError($"Property {property} not found in RAM data.");
                }
            }
        }

        private void AddMotherboardData(ManagementObject obj, ObservableCollection<SensorData> dataCollection)
        {
            var properties = new[] { "Name", "Product", "Manufacturer", "SerialNumber", "Version", "Status" };
            foreach (var property in properties)
            {
                if (obj.Properties[property]?.Value != null)
                {
                    dataCollection.Add(new SensorData { SensorName = property, SensorValue = obj[property]?.ToString() });
                }
                else
                {
                    LogError($"Property {property} not found in Motherboard data.");
                }
            }
        }

        private void AddHardDriveData(ManagementObject obj, ObservableCollection<SensorData> dataCollection)
        {
            var properties = new[] { "Model", "Description", "InterfaceType", "Size", "FirmwareRevision", "Signature", "TotalCylinders", "Status" };
            foreach (var property in properties)
            {
                if (property == "Size" && obj.Properties[property]?.Value != null)
                {
                    if (ulong.TryParse(obj[property]?.ToString(), out ulong sizeInBytes))
                    {
                        double sizeInGB = sizeInBytes / (1024 * 1024 * 1024);
                        dataCollection.Add(new SensorData { SensorName = property, SensorValue = $"{sizeInGB:F2} GB" });
                    }
                }
                else if (obj.Properties[property]?.Value != null)
                {
                    dataCollection.Add(new SensorData { SensorName = property, SensorValue = obj[property]?.ToString() });
                }
                else
                {
                    LogError($"Property {property} not found in Hard Drive data.");
                }
            }
        }

        private void AddGpuData(ManagementObject obj, ObservableCollection<SensorData> dataCollection)
        {
            var properties = new[] { "VideoProcessor", "AdapterCompatibility", "AdapterDACType", "AdapterRAM", "DriverVersion", "DriverDate", "InfSection", "Status" };
            foreach (var property in properties)
            {
                if (property == "AdapterRAM" && obj.Properties[property]?.Value != null)
                {
                    if (ulong.TryParse(obj[property]?.ToString(), out ulong ramInBytes))
                    {
                        double ramInGB = ramInBytes / (1024 * 1024 * 1024);
                        dataCollection.Add(new SensorData { SensorName = property, SensorValue = $"{ramInGB:F2} GB" });
                    }
                }
                else if (obj.Properties[property]?.Value != null)
                {
                    dataCollection.Add(new SensorData { SensorName = property, SensorValue = obj[property]?.ToString() });
                }
                else
                {
                    LogError($"Property {property} not found in GPU data.");
                }
            }
        }

        private void AddSystemData(ManagementObject obj, ObservableCollection<SensorData> dataCollection)
        {
            var properties = new[] { "Caption", "Manufacturer", "CSName", "OSArchitecture", "FreePhysicalMemory", "FreeVirtualMemory", "NumberOfUsers", "RegisteredUser", "SerialNumber", "InstallDate", "SystemDirectory", "Version", "Status" };
            foreach (var property in properties)
            {
                if ((property == "FreePhysicalMemory" || property == "FreeVirtualMemory") && obj.Properties[property]?.Value != null)
                {
                    if (ulong.TryParse(obj[property]?.ToString(), out ulong memoryInKB))
                    {
                        double memoryInGB = memoryInKB / (1024 * 1024);
                        dataCollection.Add(new SensorData { SensorName = property, SensorValue = $"{memoryInGB:F2} GB" });
                    }
                }
                else if (obj.Properties[property]?.Value != null)
                {
                    dataCollection.Add(new SensorData { SensorName = property, SensorValue = obj[property]?.ToString() });
                }
                else
                {
                    LogError($"Property {property} not found in System data.");
                }
            }
        }

        private void ExportComponentsData_Click(object sender, RoutedEventArgs e)
        {
            string exportPath = "components_data.txt";
            string componentsInfo = GetAllComponentsData();
            File.WriteAllText(exportPath, componentsInfo);
            MessageBox.Show($"Components data exported to {exportPath}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);

            // Open the exported components data file
            Process.Start(new ProcessStartInfo
            {
                FileName = exportPath,
                UseShellExecute = true
            });
        }

        private string GetAllComponentsData()
        {
            string componentsInfo = "";

            componentsInfo += GetComponentInfo("Win32_Processor", "CPU");
            componentsInfo += GetComponentInfo("Win32_PhysicalMemory", "RAM");
            componentsInfo += GetComponentInfo("Win32_BaseBoard", "Motherboard");
            componentsInfo += GetComponentInfo("Win32_DiskDrive", "Hard Drive");
            componentsInfo += GetComponentInfo("Win32_VideoController", "GPU");
            componentsInfo += GetComponentInfo("Win32_OperatingSystem", "System");

            return componentsInfo;
        }

        private string GetComponentInfo(string query, string componentName)
        {
            var searcher = new ManagementObjectSearcher($"select * from {query}");
            string info = "";
            foreach (ManagementObject obj in searcher.Get())
            {
                info += $"{componentName}:\n";
                foreach (var property in obj.Properties)
                {
                    info += $"{property.Name}: {property.Value}\n";
                }
                info += "\n";
            }
            return info;
        }

        private void Log(string message)
        {
            string logMessage = $"{DateTime.Now}: {message}\n";
            File.AppendAllText(logFilePath, logMessage);
            LoadLogs();
        }

        private void LoadLogs()
        {
            if (File.Exists(logFilePath))
            {
                LogTextBox.Text = File.ReadAllText(logFilePath);
            }
        }

        private void ClearLogs_Click(object sender, RoutedEventArgs e)
        {
            File.WriteAllText(logFilePath, string.Empty);
            LogTextBox.Clear();
        }

        private void ExportLogs_Click(object sender, RoutedEventArgs e)
        {
            string exportPath = "exported_logs.txt";
            File.Copy(logFilePath, exportPath, overwrite: true);
            MessageBox.Show($"Logs exported to {exportPath}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);

            // Open the exported log file
            Process.Start(new ProcessStartInfo
            {
                FileName = exportPath,
                UseShellExecute = true
            });
        }

        private async void RunCpuTest_Click(object sender, RoutedEventArgs e)
        {
            await RunCpuTest();
        }

        private async void RunRamTest_Click(object sender, RoutedEventArgs e)
        {
            await RunRamTest();
        }

        private async void RunDiskWriteTest_Click(object sender, RoutedEventArgs e)
        {
            await RunDiskWriteTest();
        }

        private async void RunDiskReadTest_Click(object sender, RoutedEventArgs e)
        {
            await RunDiskReadTest();
        }

        private async void RunGpuTest_Click(object sender, RoutedEventArgs e)
        {
            await RunGpuTest();
        }

        private void CheckSystemErrors_Click(object sender, RoutedEventArgs e)
        {
            CheckSystemErrors();
        }

        public async Task RunCpuTest()
        {
            Log("Starting CPU stress test...");
            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                await Task.Run(() =>
                {
                    double result = 0;
                    for (int i = 0; i < 150000000; i++)
                    {
                        result += Math.Sqrt(i) * Math.Sin(i) * Math.Cos(i);
                    }
                });

                stopwatch.Stop();
                TimeSpan elapsedTime = stopwatch.Elapsed;

                Log($"CPU stress test completed in {elapsedTime.TotalSeconds:F2} seconds.");
                if (elapsedTime > expectedTestDuration)
                {
                    Log("Test took longer than expected. Your CPU might be under heavy load or not performing optimally.");
                }
                else
                {
                    Log("Test completed successfully within the expected time.");
                }
            }
            catch (Exception ex)
            {
                Log("CPU stress test failed: " + ex.Message);
            }
        }

        public async Task RunRamTest()
        {
            Log("Starting RAM stress test...");
            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                await Task.Run(() =>
                {
                    List<byte[]> data = new List<byte[]>();
                    for (int i = 0; i < 1500; i++)
                    {
                        data.Add(new byte[1024 * 1024]); // 1MB
                    }
                    data.Clear();
                });

                stopwatch.Stop();
                TimeSpan elapsedTime = stopwatch.Elapsed;

                Log($"RAM stress test completed in {elapsedTime.TotalSeconds:F2} seconds.");
                if (elapsedTime > expectedTestDuration)
                {
                    Log("Test took longer than expected. Your RAM might be under heavy load or not performing optimally.");
                }
                else
                {
                    Log("Test completed successfully within the expected time.");
                }
            }
            catch (OutOfMemoryException ex)
            {
                Log("RAM stress test failed: Out of memory. " + ex.Message);
            }
            catch (Exception ex)
            {
                Log("RAM stress test failed: " + ex.Message);
            }
        }

        public async Task RunDiskWriteTest()
        {
            Log("Starting Disk Write stress test...");
            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                await Task.Run(() =>
                {
                    string tempFile = Path.GetTempFileName();
                    using (FileStream fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                    {
                        byte[] data = new byte[1024 * 1024]; // 1MB
                        for (int i = 0; i < 2800; i++)
                        {
                            fs.Write(data, 0, data.Length);
                        }
                    }
                    File.Delete(tempFile);
                });

                stopwatch.Stop();
                TimeSpan elapsedTime = stopwatch.Elapsed;

                Log($"Disk Write stress test completed in {elapsedTime.TotalSeconds:F2} seconds.");
                if (elapsedTime > expectedTestDuration)
                {
                    Log("Test took longer than expected. Your disk might be under heavy load or not performing optimally.");
                }
                else
                {
                    Log("Test completed successfully within the expected time.");
                }
            }
            catch (Exception ex)
            {
                Log("Disk Write stress test failed: " + ex.Message);
            }
        }

        public async Task RunDiskReadTest()
        {
            Log("Starting Disk Read stress test...");
            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                await Task.Run(() =>
                {
                    string tempFile = Path.GetTempFileName();
                    using (FileStream fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                    {
                        byte[] data = new byte[1024 * 1024]; // 1MB
                        fs.Write(data, 0, data.Length);
                    }
                    using (FileStream fs = new FileStream(tempFile, FileMode.Open, FileAccess.Read))
                    {
                        byte[] buffer = new byte[1024 * 1024]; // 1MB
                        for (int i = 0; i < 200000; i++)
                        {
                            fs.Read(buffer, 0, buffer.Length);
                        }
                    }
                    File.Delete(tempFile);
                });

                stopwatch.Stop();
                TimeSpan elapsedTime = stopwatch.Elapsed;

                Log($"Disk Read stress test completed in {elapsedTime.TotalSeconds:F2} seconds.");
                if (elapsedTime > expectedTestDuration)
                {
                    Log("Test took longer than expected. Your disk might be under heavy load or not performing optimally.");
                }
                else
                {
                    Log("Test completed successfully within the expected time.");
                }
            }
            catch (Exception ex)
            {
                Log("Disk Read stress test failed: " + ex.Message);
            }
        }

        public async Task RunGpuTest()
        {
            Log("Starting GPU stress test...");
            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                await Task.Run(() =>
                {
                    for (int i = 0; i < 150000000; i++)
                    {
                        // Моделируем загрузку графического процессора с помощью простых математических операций
                        double x = Math.Sin(i);
                        double y = Math.Cos(i);
                        double z = x * y;
                    }
                });

                stopwatch.Stop();
                TimeSpan elapsedTime = stopwatch.Elapsed;

                Log($"GPU stress test completed in {elapsedTime.TotalSeconds:F2} seconds.");
                if (elapsedTime > expectedTestDuration)
                {
                    Log("Test took longer than expected. Your GPU might be under heavy load or not performing optimally.");
                }
                else
                {
                    Log("Test completed successfully within the expected time.");
                }
            }
            catch (Exception ex)
            {
                Log("GPU stress test failed: " + ex.Message);
            }
        }

        public void CheckSystemErrors()
        {
            Log("Checking system for errors...");
            try
            {
                using (var searcher = new ManagementObjectSearcher("select * from Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string lastBootUpTime = obj["LastBootUpTime"].ToString();
                        Log("Last Boot Up Time: " + lastBootUpTime);
                    }
                }

                using (var searcher = new ManagementObjectSearcher("select * from Win32_NTLogEvent where Type = 'Error'"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string message = obj["Message"].ToString();
                        string timeGenerated = obj["TimeGenerated"].ToString();
                        Log("Error: " + message + " at " + timeGenerated);
                    }
                }

                Log("System error check completed.");
            }
            catch (Exception ex)
            {
                Log("System error check failed: " + ex.Message);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            // Открываем URL в браузере по умолчанию
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Открываем URL в браузере по умолчанию
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Nedi962/Tab-on-me",
                UseShellExecute = true
            });
        }

        public class SensorData
        {
            public string SensorName { get; set; }
            public string SensorValue { get; set; }
        }
    }
}
