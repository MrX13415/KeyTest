using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using KeyTest.Windows;

namespace KeyTest
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public int KeyCount { get; private set; }
        

        public MainWindow()
        {
            InitializeComponent();

            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            this.Title += $" - v{ver.Major}.{ver.Minor}";

            KeyboardHock.Initialize();
            KeyboardHock.KeyEvent += KeyEvent;
        }

        private void HandleKey(KeyInfo info)
        {
            Run txt = null;
            Run meta = null;
            if (info.Position < 0)
            {
                info.Position = Output.Inlines.Count;

                txt = new Run(info.KeyString);
                meta = new Run("");
                Output.Inlines.Add(txt);          // text
                Output.Inlines.Add(meta);         // meta info
                Output.Inlines.Add(new Run("  ")); // spaceing
            }
            else
            {
                txt = (Run)Output.Inlines.ElementAt(info.Position);
                meta = (Run)txt.NextInline;
            }

            meta.Text = $"{info.Timer.ElapsedMilliseconds}ms";
            meta.FontSize = Output.FontSize * 0.5;

            if (info.KeyDown)
            {
                if (info.Event.IsSysKey) txt.Foreground = new SolidColorBrush(Color.FromRgb(40, 120, 240)); 
                else txt.Foreground = new SolidColorBrush(Color.FromRgb(240, 40, 40));
                meta.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
            }
            if (info.KeyUp)
            {
                txt.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                meta.Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180));
            }

            OutScroll.ScrollToEnd();
        }

        private void KeyEvent(object sender, KeyHockEventArgs e)
        {
            if (CenterInfo.IsVisible) CenterInfo.Visibility = Visibility.Hidden;

            var last = KeyInfo.Set(e);
            if (last.KeyUp) HandleKey(last);

            foreach (var info in KeyInfo.EventList.Values)
            {
                HandleKey(info);
            }

            KeyCount++;
            CountInfo.Content = KeyCount;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            KeyInfo.EventList.Clear();
            Output.Text = "";
            CenterInfo.Visibility = Visibility.Visible;
            KeyCount = 0;
        }
    }

    public class KeyInfo
    {
        public static Dictionary<int, KeyInfo> EventList { get; } = new Dictionary<int, KeyInfo>();

        public KeyHockEventArgs Event { get; private set; }
        public Stopwatch Timer { get; } = new Stopwatch();
        public int Position { get; set; } = -1;


        private KeyInfo(KeyHockEventArgs @event)
        {
            Event = @event;
        }

        public int VirtualKey => Event.VirtualKey;
        public bool KeyDown => Event.KeyDown;
        public bool KeyUp => Event.KeyUp;
        public bool IsUnknow => Event.IsUnknow;

        public string KeyString => Event.Key.ToString();


        public static KeyInfo Set(KeyHockEventArgs @event)
        {
            if (!EventList.TryGetValue(@event.VirtualKey, out var info))
            {
                info = new KeyInfo(@event);
                if (info.KeyDown) EventList.Add(@event.VirtualKey, info);
            }
            else
            {
                info.Event = @event;
            }
           
            if (info.KeyDown) info.Timer.Start();
            if (info.KeyUp)
            {
                info.Timer.Stop();
                EventList.Remove(info.VirtualKey);
            }
            
            return info;
        }

    }
}

