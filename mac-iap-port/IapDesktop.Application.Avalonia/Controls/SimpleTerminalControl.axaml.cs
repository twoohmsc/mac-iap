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
        public event EventHandler<(ushort columns, ushort rows)>? TerminalResized;

        public SimpleTerminalControl()
        {
            InitializeComponent();
            
            // Handle output
            OutputEditor.Document = new TextDocument();
            
            // Handle input
            OutputEditor.KeyDown += OutputEditor_KeyDown;
            OutputEditor.TextInput += OutputEditor_TextInput;
            OutputEditor.SizeChanged += OutputEditor_SizeChanged;
        }

        private void OutputEditor_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
             var textView = OutputEditor.TextArea.TextView;
             if (textView != null && textView.DefaultLineHeight > 0 && textView.WideSpaceWidth > 0)
             {
                double w = e.NewSize.Width > 0 ? e.NewSize.Width : OutputEditor.Bounds.Width;
                double h = e.NewSize.Height > 0 ? e.NewSize.Height : OutputEditor.Bounds.Height;

                if (w > 0 && h > 0)
                {
                    int cols = (int)(w / textView.WideSpaceWidth);
                    int rows = (int)(h / textView.DefaultLineHeight);
                    
                    if (cols > 0 && rows > 0)
                    {
                        TerminalResized?.Invoke(this, ((ushort)cols, (ushort)rows));
                    }
                }
             }
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
                case Key.V:
                    if (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta))
                    {
                        HandlePasteAsync();
                        e.Handled = true;
                        return;
                    }
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

        private async void HandlePasteAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard != null)
            {
                try 
                {
                    var text = await topLevel.Clipboard.GetTextAsync();
                    if (!string.IsNullOrEmpty(text))
                    {
                        UserInput?.Invoke(this, text);
                    }
                }
                catch (Exception) 
                { 
                    // Ignore clipboard errors
                }
            }
        }
    }
}
