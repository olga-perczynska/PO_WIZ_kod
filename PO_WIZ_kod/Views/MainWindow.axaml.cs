using Avalonia.Controls;
using Avalonia.Interactivity;
using PO_WIZ_kod.Models;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.Data.Sqlite;
using QRCoder;
using Avalonia.Media.Imaging;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;



namespace PO_WIZ_kod.Views
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Sample> samples = new();
        private int sampleCounter;
        private Sample? selectedSample = null;
        private ObservableCollection<Sample> filteredSamples = new();

        public MainWindow()
        {
            InitializeComponent();
            SampleDatabaseService.InitializeDatabase();

            sampleCounter = GetLastSampleCounter();

            var saved = SampleDatabaseService.PobierzWszystkie();
            foreach (var s in saved)
                samples.Add(s);

            SamplesListBox.ItemsSource = samples;
            SamplesListBox.ItemsSource = filteredSamples;
            SamplesListBox.SelectionChanged += OnSampleSelected;
            UpdateFilteredSamples("");
        }

        private int GetLastSampleCounter()
        {
            using var connection = new SqliteConnection($"Data Source=sample.db");
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT MAX(CAST(Id AS INTEGER)) FROM Samples;";

            var result = cmd.ExecuteScalar();
            return (result != DBNull.Value && result != null) ? Convert.ToInt32(result) + 1 : 1;
        }

        private void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            string name = SampleNameBox.Text ?? "";
            string type = (SampleTypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            string dateText = SampleDateBox.Text ?? "";
            string notes = SampleNotesBox.Text ?? "";

            DateTime? parsedDate = null;
            if (DateTime.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                parsedDate = date;

            if (selectedSample != null)
            {
                selectedSample.Name = name;
                selectedSample.Type = type;
                selectedSample.CollectionDate = parsedDate;
                selectedSample.Notes = notes;

                SampleDatabaseService.AktualizujSample(selectedSample);

                SamplesListBox.ItemsSource = null;
                SamplesListBox.ItemsSource = samples;

                selectedSample = null;
            }
            else
            {
                string id = sampleCounter.ToString("D3");
                sampleCounter++;

                var sample = new Sample
                {
                    Id = id,
                    Name = name,
                    Type = type,
                    CollectionDate = parsedDate,
                    Notes = notes
                };

                samples.Add(sample);
                SampleDatabaseService.ZapiszSample(sample);
            }

            ClearForm();
        }

        private void OnSampleSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (SamplesListBox.SelectedItem is Sample sample)
            {
                selectedSample = sample;
                SampleNameBox.Text = sample.Name;
                SampleDateBox.Text = sample.CollectionDate?.ToString("yyyy-MM-dd") ?? "";
                SampleNotesBox.Text = sample.Notes;

                foreach (ComboBoxItem item in SampleTypeBox.Items)
                {
                    if (item.Content?.ToString() == sample.Type)
                    {
                        SampleTypeBox.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void OnGenerateQrClick(object? sender, RoutedEventArgs e)
        {
            if (SamplesListBox.SelectedItem is Sample selectedSample)
            {
                string qrData = $"ID: {selectedSample.Id}\n" +
                                $"Nazwa: {selectedSample.Name}\n" +
                                $"Typ: {selectedSample.Type}\n" +
                                $"Data: {selectedSample.CollectionDate?.ToString("yyyy-MM-dd") ?? ""}\n" +
                                $"Uwagi: {selectedSample.Notes}";

                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrBytes = qrCode.GetGraphic(20);

                using var stream = new MemoryStream(qrBytes);
                QrCodeImage.Source = new Avalonia.Media.Imaging.Bitmap(stream);
            }
        }

        private void ClearForm()
        {
            SampleNameBox.Text = "";
            SampleTypeBox.SelectedItem = null;
            SampleDateBox.Text = "";
            SampleNotesBox.Text = "";
        }

        private void OnSaveQrClick(object? sender, RoutedEventArgs e)
        {
            if (SamplesListBox.SelectedItem is Sample selectedSample)
            {
                string qrData = $"ID: {selectedSample.Id}\n" +
                                $"Nazwa: {selectedSample.Name}\n" +
                                $"Typ: {selectedSample.Type}\n" +
                                $"Data: {selectedSample.CollectionDate?.ToString("yyyy-MM-dd") ?? ""}\n" +
                                $"Uwagi: {selectedSample.Notes}";

                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrBytes = qrCode.GetGraphic(20);

              
                var saveDialog = new SaveFileDialog
                {
                    Title = "Zapisz kod QR jako PNG",
                    Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter { Name = "Plik PNG", Extensions = { "png" } }
            },
                    InitialFileName = $"Sample_{selectedSample.Id}.png"
                };

                saveDialog.ShowAsync(this).ContinueWith(result =>
                {
                    if (!string.IsNullOrEmpty(result.Result))
                    {
                        try
                        {
                            File.WriteAllBytes(result.Result, qrBytes);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Błąd zapisu pliku: {ex.Message}");
                        }
                    }
                });
            }
        }

        private void PrintLabel(Sample sample)
        {
            string qrData = $"ID: {sample.Id}\n" +
                            $"Nazwa: {sample.Name}\n" +
                            $"Typ: {sample.Type}\n" +
                            $"Data: {sample.CollectionDate?.ToString("yyyy-MM-dd") ?? ""}\n" +
                            $"Uwagi: {sample.Notes}";

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrBytes = qrCode.GetGraphic(20); 

            using var ms = new MemoryStream(qrBytes);
            using var qrBitmap = new System.Drawing.Bitmap(ms);

            int width = qrBitmap.Width + 40;  
            int height = qrBitmap.Height + 60; 

            using var labelBitmap = new System.Drawing.Bitmap(width, height);
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(labelBitmap))
            {
                g.Clear(System.Drawing.Color.White);

                int qrX = (width - qrBitmap.Width) / 2;
                int qrY = 10;
                g.DrawImage(qrBitmap, new System.Drawing.Point(qrX, qrY));

                using var font = new System.Drawing.Font("Arial", 14, System.Drawing.FontStyle.Bold);
                var brush = System.Drawing.Brushes.Black;

                string sampleName = sample.Name ?? "";
                var textSize = g.MeasureString(sampleName, font);
                float textX = (width - textSize.Width) / 2;
                float textY = qrY + qrBitmap.Height + 10; 

                g.DrawString(sampleName, font, brush, textX, textY);
            }

            string tempFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Etykieta_{sample.Id}.png");
            labelBitmap.Save(tempFilePath, System.Drawing.Imaging.ImageFormat.Png);

            try
            {
                if (OperatingSystem.IsWindows())
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer", tempFilePath) { UseShellExecute = true });
                }
                else if (OperatingSystem.IsLinux())
                {
                    System.Diagnostics.Process.Start("xdg-open", tempFilePath);
                }
                else if (OperatingSystem.IsMacOS())
                {
                    System.Diagnostics.Process.Start("open", tempFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas otwierania pliku: {ex.Message}");
            }
        }




        private void OnPrintLabelClick(object? sender, RoutedEventArgs e)
        {
            if (SamplesListBox.SelectedItem is Sample sample)
            {
                PrintLabel(sample);
            }
        }
        private void UpdateFilteredSamples(string filter)
        {
            filteredSamples.Clear();
            foreach (var sample in samples)
            {
                if (sample.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                {
                    filteredSamples.Add(sample);
                }
            }
        }
        private void OnSearchKeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            string filter = SearchBox.Text ?? "";
            UpdateFilteredSamples(filter);
        }


    }
}
