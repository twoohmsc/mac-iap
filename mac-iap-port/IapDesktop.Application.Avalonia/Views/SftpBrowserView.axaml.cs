using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using IapDesktop.Application.Avalonia.ViewModels;

namespace IapDesktop.Application.Avalonia.Views
{
    public partial class SftpBrowserView : UserControl
    {
        public SftpBrowserView()
        {
            InitializeComponent();
        }

        protected override async void OnDataContextChanged(System.EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (DataContext is SftpBrowserViewModel vm)
            {
                 await vm.ConnectAsync();
            }
        }
        
        // Note: DoubleClick handling usually requires attaching to the DataGrid events in XAML or code-behind.
        // For simplicity, we'll assume the user uses the Open button or we add an event handler here if possible.
        // Avalonia DataGrid doesn't have a direct DoubleClickCommand yet without behaviors.
        // We can add it in the constructor.
    }
}
