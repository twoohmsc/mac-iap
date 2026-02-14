using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IapDesktop.Application.Avalonia
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ViewModels.MainViewModel();
        }

        public void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
