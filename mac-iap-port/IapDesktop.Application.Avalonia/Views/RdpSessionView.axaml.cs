using Avalonia.Controls;
using Avalonia.Interactivity;
using IapDesktop.Application.Avalonia.ViewModels;

namespace IapDesktop.Application.Avalonia.Views
{
    public partial class RdpSessionView : UserControl
    {
        public RdpSessionView()
        {
            InitializeComponent();
        }

        protected override async void OnDataContextChanged(System.EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (DataContext is RdpSessionViewModel vm)
            {
                 // Auto-connect when view is loaded/bound
                 await vm.ConnectAsync();
            }
        }
    }
}
