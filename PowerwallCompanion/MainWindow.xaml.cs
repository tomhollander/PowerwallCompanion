using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using WinRT.Interop;
using static System.Runtime.InteropServices.JavaScript.JSType;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PowerwallCompanion
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        AppWindow m_AppWindow;

        public MainWindow()
        {
            this.InitializeComponent();

            m_AppWindow = GetAppWindowForCurrentWindow();

            // Check to see if customization is supported.
            // Currently only supported on Windows 11.
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                var titleBar = m_AppWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = true;
                AppTitleBar.Loaded += AppTitleBar_Loaded;
                AppTitleBar.SizeChanged += AppTitleBar_SizeChanged;


                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.ButtonBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(0, 0x17, 0x17, 0x17);
                titleBar.ButtonInactiveForegroundColor = Colors.DarkGray;
                titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(0, 0x17, 0x17, 0x17);
                titleBar.ButtonHoverBackgroundColor = Colors.Gray;

                BackButton.Click += OnBackClicked;
                BackButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Title bar customization using these APIs is currently
                // supported only on Windows 11. In other cases, hide
                // the custom title bar element.
                // AppTitleBar.Visibility = Visibility.Collapsed;
                // TODO Show alternative UI for any functionality in
                // the title bar, such as the back button, if used
            }

            RestoreLastWindowSize();
            this.SizeChanged += MainWindow_SizeChanged;

        }

        private void RestoreLastWindowSize()
        {
            try
            {
                m_AppWindow.Resize(new SizeInt32(Settings.WindowWidth, Settings.WindowHeight));
                Debug.WriteLine($"Restoring window size to {Settings.WindowWidth}x{Settings.WindowHeight}");

                var presenter = m_AppWindow.Presenter as OverlappedPresenter;
                if (Settings.WindowState == "Maximized")
                {
                    presenter.Maximize();
                }
            }
            catch (Exception ex)
            {
                Telemetry.TrackException(ex);
            }
        }

        private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args)
        {
            try
            {
                // Do not use args.Size, as it is not accurate for the app window.
                Settings.WindowWidth = m_AppWindow.Size.Width;
                Settings.WindowHeight = m_AppWindow.Size.Height;

                Debug.WriteLine($"Saving window size: {m_AppWindow.Size.Width}x{m_AppWindow.Size.Height}");

                var presenter = m_AppWindow.Presenter as OverlappedPresenter;
                Settings.WindowState = presenter.State.ToString();
            }
            catch (Exception ex)
            {
                Telemetry.TrackException(ex);
            }

        }

        public Button BackButton => AppTitleBarBackButton;

        private void AppTitleBar_Loaded(object sender, RoutedEventArgs e)
        {
            SetTitleBar(AppTitleBar);
            PageFrame.Navigate(typeof(MainPage));
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                SetDragRegionForCustomTitleBar(m_AppWindow);
            }
        }

        private void OnBackClicked(object sender, RoutedEventArgs e)
        {
            if (PageFrame.CanGoBack)
            {
                PageFrame.GoBack();
            }
        }

        private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (AppWindowTitleBar.IsCustomizationSupported()
                && m_AppWindow.TitleBar.ExtendsContentIntoTitleBar)
            {
                // Update drag region if the size of the title bar changes.
                SetDragRegionForCustomTitleBar(m_AppWindow);
            }
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        private void SetDragRegionForCustomTitleBar(AppWindow appWindow)
        {
            if (AppWindowTitleBar.IsCustomizationSupported()
                && appWindow.TitleBar.ExtendsContentIntoTitleBar)
            {
                double scaleAdjustment = GetScaleAdjustment();

                RightPaddingColumn.Width = new GridLength(appWindow.TitleBar.RightInset / scaleAdjustment);
                LeftPaddingColumn.Width = new GridLength(appWindow.TitleBar.LeftInset / scaleAdjustment);

                List<Windows.Graphics.RectInt32> dragRectsList = new List<Windows.Graphics.RectInt32>();
                Windows.Graphics.RectInt32 dragRectL;
                dragRectL.X = (int)((LeftPaddingColumn.ActualWidth + IconColumn.ActualWidth) * scaleAdjustment);
                dragRectL.Y = 0;
                dragRectL.Height = (int)((AppTitleBar.ActualHeight) * scaleAdjustment);
                dragRectL.Width = (int)((TitleColumn.ActualWidth
                                        + DragColumn.ActualWidth) * scaleAdjustment);
                dragRectsList.Add(dragRectL);

                Windows.Graphics.RectInt32[] dragRects = dragRectsList.ToArray();
                appWindow.TitleBar.SetDragRectangles(dragRects);
            }
        }

        [DllImport("Shcore.dll", SetLastError = true)]
        internal static extern int GetDpiForMonitor(IntPtr hmonitor, Monitor_DPI_Type dpiType, out uint dpiX, out uint dpiY);

        internal enum Monitor_DPI_Type : int
        {
            MDT_Effective_DPI = 0,
            MDT_Angular_DPI = 1,
            MDT_Raw_DPI = 2,
            MDT_Default = MDT_Effective_DPI
        }

        private double GetScaleAdjustment()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            DisplayArea displayArea = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Primary);
            IntPtr hMonitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);

            // Get DPI.
            int result = GetDpiForMonitor(hMonitor, Monitor_DPI_Type.MDT_Default, out uint dpiX, out uint _);
            if (result != 0)
            {
                throw new Exception("Could not get DPI for monitor.");
            }

            uint scaleFactorPercent = (uint)(((long)dpiX * 100 + (96 >> 1)) / 96);
            return scaleFactorPercent / 100.0;
        }
    }
}
