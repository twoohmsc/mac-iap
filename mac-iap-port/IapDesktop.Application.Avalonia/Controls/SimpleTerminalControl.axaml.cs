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
        private readonly StringBuilder inputBuffer = new StringBuilder();
        
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
            // Simple logic: append to buffer and display locally?
            // Or rely on remote echo?
            // Usually remote echo is best. If we echo locally + remote echo, we get double chars.
            // Let's assume remote echo. So we send chars immediately?
            // Or line buffering?
            // If line buffering, we handle locally.
            // Let's implement line buffering for simplicity first.
            
            if (!string.IsNullOrEmpty(e.Text))
            {
                foreach (char c in e.Text)
                {
                    // Handle backspace handled in KeyDown?
                    // TextInput usually gives printable chars.
                    if (c >= 32)
                    {
                        inputBuffer.Append(c);
                        AppendText(c.ToString()); // Local echo for line editing visual
                    }
                }
            }
        }

        private void OutputEditor_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var input = inputBuffer.ToString();
                inputBuffer.Clear();
                AppendText("\n"); // Local echo newline
                
                UserInput?.Invoke(this, input + "\n");
                e.Handled = true;
            }
            else if (e.Key == Key.Back)
            {
                if (inputBuffer.Length > 0)
                {
                    inputBuffer.Length--;
                    // Remove last char from document
                    if (OutputEditor.Document.TextLength > 0)
                    {
                        OutputEditor.Document.Remove(OutputEditor.Document.TextLength - 1, 1);
                    }
                }
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
                inputBuffer.Clear();
            });
        }
    }
}
