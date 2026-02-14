using Avalonia.Controls;
using Avalonia.Threading;
using IapDesktop.Application.Avalonia.ViewModels;
using System;

namespace IapDesktop.Application.Avalonia.Views
{
    public partial class ConnectionView : UserControl
    {
        private ConnectionViewModel? viewModel;

        public ConnectionView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Terminal.UserInput += OnTerminalUserInput;
        }

        private void OnTerminalUserInput(object? sender, string input)
        {
            if (viewModel != null)
            {
                _ = viewModel.SendInputAsync(input);
            }
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (viewModel != null)
            {
                viewModel.OutputReceived -= OnOutputReceived;
            }

            viewModel = DataContext as ConnectionViewModel;

            if (viewModel != null)
            {
                viewModel.OutputReceived += OnOutputReceived;
            }
        }

        private void OnOutputReceived(object? sender, string text)
        {
            Terminal.AppendText(text);
        }
    }
}
