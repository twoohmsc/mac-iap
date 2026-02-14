using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IapDesktop.Application.Avalonia.Controls
{
    public partial class SimpleTerminalControl : UserControl
    {

        
        public event EventHandler<string>? UserInput;

        public SimpleTerminalControl()
        {
            InitializeComponent();
            
            // Handle output
            OutputEditor.Document = new TextDocument();
            
            // Handle input
            OutputEditor.KeyDown += OutputEditor_KeyDown;
            OutputEditor.TextInput += OutputEditor_TextInput;
        }

        private void OutputEditor_TextInput(object? sender, TextInputEventArgs e)
        {
            // Raw Input Mode:
            // Forward everything to the backend. Do NOT echo locally.
            // The server will echo back characters if appropriate.
            
            if (!string.IsNullOrEmpty(e.Text))
            {
                UserInput?.Invoke(this, e.Text);
            }
            
            // Prevent local insertion
            e.Handled = true;
        }

        private void OutputEditor_KeyDown(object? sender, KeyEventArgs e)
        {
            // Handle special keys that don't trigger TextInput
            string? sequence = null;

            switch (e.Key)
            {
                case Key.Enter:
                    sequence = "\r";
                    break;
                case Key.Back:
                    sequence = "\x7f"; // DEL is usually what backspace sends in terminals
                    break;
                case Key.Tab:
                    sequence = "\t";
                    break;
                case Key.Up:
                    sequence = "\x1b[A";
                    break;
                case Key.Down:
                    sequence = "\x1b[B";
                    break;
                case Key.Right:
                    sequence = "\x1b[C";
                    break;
                case Key.Left:
                    sequence = "\x1b[D";
                    break;
                case Key.Escape:
                    sequence = "\x1b";
                    break;
                case Key.Home:
                     sequence = "\x1b[H";
                     break;
                case Key.End:
                     sequence = "\x1b[F";
                     break;
                case Key.Delete:
                    sequence = "\x1b[3~";
                    break;
            }

            if (sequence != null)
            {
                UserInput?.Invoke(this, sequence);
                e.Handled = true;
            }
        }

        public void AppendText(string text)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Strip ANSI codes logic
                // Simple regex for CSI codes
                string cleaned = Regex.Replace(text, @"\x1B\[[^@-~]*[@-~]", "");
                
                // Also handle simple carriage returns without newlines if needed,
                // but usually \r\n is fine.
                // If backend sends \r only (loading bars), we might want to handle overwrites.
                // For now, simple append.
                
                OutputEditor.Document.Insert(OutputEditor.Document.TextLength, cleaned);
                OutputEditor.ScrollToEnd();
            });
        }
        
        public void Clear()
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                OutputEditor.Document.Text = "";
            });
        }
    }
}
