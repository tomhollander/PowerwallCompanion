using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PowerwallCompanion.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace PowerwallCompanion
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ChartPage : Page
    {
        public ChartViewModel ViewModel { get; set; }
        public ChartPage()
        {
            this.InitializeComponent();

            this.ViewModel = new ChartViewModel();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                await Task.Delay(10);

                if ((DateTime.Now - ViewModel.DataLastUpdated).TotalMinutes > 10)
                {
                    var t1 = ViewModel.LoadPowerGraphData();
                    var t2 = ViewModel.LoadTodaySelfConsumptionData();
                    await Task.WhenAll(t1, t2);
                }

                //ViewModel.SelfConsumption = ViewModel.SelfConsumptionToday;
                //ViewModel.SelectedSeries = ViewModel.TodaySeries;

                SetAxes();
                periodCombo.SelectedIndex = 0;

                areaChart.Visibility = Visibility.Visible;
            }
            catch(UnauthorizedAccessException)
            {
                progressRing.IsActive = false;
                this.Frame.Navigate(typeof(LoginPage));
            }
            catch (Exception ex)
            {
                progressRing.IsActive = false;
            }
   
            base.OnNavigatedTo(e);
        }

        private void SetAxes()
        {
            foreach (var series in areaChart.Series)
            {
                var s = (AreaSeries)series;
                s.DependentRangeAxis = axis;
                s.IndependentAxis = areaTimeAxis;
            }

            foreach (var series in barChart.Series)
            {
                var s = (ColumnSeries)series;
                s.DependentRangeAxis = columnEnergyAxis;
                s.IndependentAxis = columnTimestampAxis;
            }
        }


        private void areaChart_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (e.OriginalSource is TextBlock)
            {
                var textBlock = (TextBlock)e.OriginalSource;
                object dataContext = textBlock.DataContext;
                if (dataContext != null)
                {
                    var seriesName = (string)dataContext;
                    var series = areaChart.Series.OfType<AreaSeries>().Where(s => (string)s.Title == seriesName).Single();
                    if (series.Visibility == Visibility.Visible)
                    {
                        // Hide it
                        series.Visibility = Visibility.Collapsed;
                        textBlock.Foreground = new SolidColorBrush(Colors.Gray);
                    }
                    else
                    {
                        // Show it
                        series.Visibility = Visibility.Visible;
                        textBlock.ClearValue(TextBlock.ForegroundProperty);
                    }

                }
            }
        }

        private async void periodCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            areaChart.Visibility = Visibility.Collapsed;
            barChart.Visibility = Visibility.Collapsed;
            progressRing.IsActive = true;
            await Task.Delay(10);

            switch (periodCombo.SelectedIndex)
            {
                case 0: // Today
                    {
                        ViewModel.SelfConsumption = ViewModel.SelfConsumptionToday;
                        ViewModel.SelectedSeries = ViewModel.TodaySeries;
                        areaChart.Visibility = Visibility.Visible;

                        break;
                    }
                case 1: // Yesterday
                    {
                        ViewModel.SelfConsumption = ViewModel.SelfConsumptionYesterday;
                        ViewModel.SelectedSeries = ViewModel.YesterdaySeries;
                        areaChart.Visibility = Visibility.Visible;

                        break;
                    }
                case 2: // Week
                    {
                        if (ViewModel.SelfConsumptionWeek == null)
                        {
                            ViewModel.SelfConsumptionWeek = await ViewModel.GetSelfConsumptionData("week");
                        }
                        ViewModel.SelfConsumption = ViewModel.SelfConsumptionWeek;

                        if (ViewModel.EnergyHistoryWeek == null)
                        {
                            ViewModel.EnergyHistoryWeek = await ViewModel.GetEnergyHistoryData("week");
                        }
                        ViewModel.EnergyHistory = ViewModel.EnergyHistoryWeek;
                        barChart.Visibility = Visibility.Visible;
                        break;
                    }
                case 3: // Month
                    {
                        if (ViewModel.SelfConsumptionMonth == null)
                        {
                            ViewModel.SelfConsumptionMonth = await ViewModel.GetSelfConsumptionData("month");
                        }
                        ViewModel.SelfConsumption = ViewModel.SelfConsumptionMonth;

                        if (ViewModel.EnergyHistoryMonth == null)
                        {
                            ViewModel.EnergyHistoryMonth = await ViewModel.GetEnergyHistoryData("month");
                        }
                        ViewModel.EnergyHistory = ViewModel.EnergyHistoryMonth;
                        barChart.Visibility = Visibility.Visible;
                        break;
                    }
                case 4: // Year
                    {
                        if (ViewModel.SelfConsumptionYear == null)
                        {
                            ViewModel.SelfConsumptionYear = await ViewModel.GetSelfConsumptionData("year");
                        }
                        ViewModel.SelfConsumption = ViewModel.SelfConsumptionYear;

                        if (ViewModel.EnergyHistoryYear == null)
                        {
                            ViewModel.EnergyHistoryYear = await ViewModel.GetEnergyHistoryData("year");
                        }
                        ViewModel.EnergyHistory = ViewModel.EnergyHistoryYear;
                        barChart.Visibility = Visibility.Visible;
                        break;
                    }
            }

            progressRing.IsActive = false;
        }

   
        private async void exportButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                savePicker.SuggestedStartLocation =
                    Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                // Dropdown of file types the user can save the file as
                savePicker.FileTypeChoices.Add("Comma Separated Value (CSV) file", new List<string>() { ".csv" });
                // Default file name if the user does not type one in or select a file to replace
                savePicker.SuggestedFileName = String.Format("Powerwall-{0:yyyy-MM-dd}-{1}.csv", DateTime.Now, periodCombo.SelectedValue);

                Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    exportButton.Content = "Saving...";
                    exportButton.IsEnabled = false;

                    if (periodCombo.SelectedIndex <= 1)
                    {
                        await SavePowerInfo(file);
                    }
                    else
                    {
                        await SaveEnergyInfo(file);
                    }


                    exportButton.Content = "Export Data";
                    exportButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                var md = new MessageDialog("Error while saving data: " + ex.Message);
                await md.ShowAsync();
            }
        }

        private async Task SavePowerInfo(StorageFile file)
        {
            //Windows.Storage.CachedFileManager.DeferUpdates(file);
            await Windows.Storage.FileIO.WriteTextAsync(file, "timestamp,solar_power,battery_power,grid_power,load_power\r\n");
            foreach (var record in ViewModel.SelectedSeries)
            {
                if (!record.IsDummy)
                {
                    await Windows.Storage.FileIO.AppendTextAsync(file, $"{record.Timestamp:yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz},{record.SolarPower},{record.BatteryPower},{record.GridPower},{record.LoadPower}\r\n");
                }
            }
            //await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
        }


        private async Task SaveEnergyInfo(StorageFile file)
        {
            //Windows.Storage.CachedFileManager.DeferUpdates(file);
            await Windows.Storage.FileIO.WriteTextAsync(file, "timestamp,battery_energy_output,battery_energy_imported_from_grid,battery_energy_imported_from_solar,consumer_energy_imported_from_battery,consumer_energy_imported_from_grid,consumer_energy_imported_from_solar,battery_energy_exported_to_grid,solar_energy_exported_to_grid,grid_energy_imported,solar_energy_generated\r\n");
            foreach (var record in ViewModel.EnergyHistory)
            {

                await Windows.Storage.FileIO.AppendTextAsync(file, $"{record.Timestamp:yyyy-MM-dd},{record.BatteryEnergyExported},{record.BatteryEnergyImportedFromGrid},{record.BatteryEnergyImportedFromSolar},{record.ConsumerEnergyImportedFromBattery},{record.ConsumerEnergyImportedFromGrid},{record.ConsumerEnergyImportedFromSolar},{record.GridEnergyExportedFromBattery},{record.GridEnergyExportedFromSolar},{record.GridEnergyImported},{record.SolaryEnergyExported}\r\n");
             
            }
            //await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
        }

 
        private void areaChart_LayoutUpdated(object sender, object e)
        {
            System.Diagnostics.Debug.WriteLine("areaChart_LayoutUpdated");
        }

        private void hamburgerMenu_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var frame = (Frame)Window.Current.Content;
            var mainPage = (MainPage)frame.Content;
            mainPage.ToggleMenuPane();
        }

        private void statusLight_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (ViewModel.LastExceptionMessage != null)
            {
                var md = new MessageDialog($"Last error occurred at {ViewModel.LastExceptionDate.ToString("g")}:\r\n{ViewModel.LastExceptionMessage}");
                md.ShowAsync();
            }
        }
    }


}

