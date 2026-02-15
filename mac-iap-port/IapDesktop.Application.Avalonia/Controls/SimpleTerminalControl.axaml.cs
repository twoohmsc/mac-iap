using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Avalonia.Interactivity;

namespace IapDesktop.Application.Avalonia.Controls
{
    public partial class SimpleTerminalControl : UserControl
    {
        public event EventHandler<string>? UserInput;
        public event EventHandler<(ushort columns, ushort rows)>? TerminalResized;

        public SimpleTerminalControl()
        {
             InitializeComponent();
             
             var outputEditor = this.FindControl<AvaloniaEdit.TextEditor>("OutputEditor");
             if (outputEditor != null)
             {
                 outputEditor.Document = new TextDocument();
                 // Use Tunneling for both to capture BEFORE AvaloniaEdit
                 outputEditor.AddHandler(TextInputEvent, OutputEditor_TextInput, RoutingStrategies.Tunnel);
                 outputEditor.AddHandler(KeyDownEvent, OutputEditor_KeyDown, RoutingStrategies.Tunnel);
             }
         }

        private void OutputEditor_TextInput(object? sender, TextInputEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Text))
            {
                UserInput?.Invoke(this, e.Text);
                e.Handled = true;
            }
        }
 
        private void OutputEditor_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Most Linux shells over SSH expect \r which is then translated to \n by the TTY.
                UserInput?.Invoke(this, "\r");
                e.Handled = true;
            }
            else if (e.Key == Key.Back)
            {
                 UserInput?.Invoke(this, "\b");
                 e.Handled = true;
            }
            else if (e.Key == Key.Tab)
            {
                UserInput?.Invoke(this, "\t");
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                UserInput?.Invoke(this, "\x1b[A");
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                UserInput?.Invoke(this, "\x1b[B");
                e.Handled = true;
            }
             else if (e.Key == Key.Right)
            {
                UserInput?.Invoke(this, "\x1b[C");
                e.Handled = true;
            }
             else if (e.Key == Key.Left)
            {
                UserInput?.Invoke(this, "\x1b[D");
                e.Handled = true;
            }
             else if (e.Key == Key.V && e.KeyModifiers == KeyModifiers.Meta) // Cmd+V on Mac
            {
                HandlePaste();
                e.Handled = true;
            }
        }
          
         public void AppendText(string text)
         {
             Dispatcher.UIThread.InvokeAsync(() =>
             {
                 var outputEditor = this.FindControl<AvaloniaEdit.TextEditor>("OutputEditor");
                 if (outputEditor != null)
                 {
                     // Direct append to document
                     outputEditor.Document.Insert(outputEditor.Document.TextLength, text);
                     outputEditor.ScrollToEnd();
                 }
             });
         }
         

         
         public void ProcessOutput(string text)
         {
            AppendText(text);
         }
        
        public void Clear()
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                 var outputEditor = this.FindControl<AvaloniaEdit.TextEditor>("OutputEditor");
                 if (outputEditor != null)
                 {
                     outputEditor.Document.Text = "";
                 }
            });
        }

        private async void HandlePaste()
        {
             var outputEditor = this.FindControl<AvaloniaEdit.TextEditor>("OutputEditor");
             if (outputEditor != null)
             {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.Clipboard != null) 
                {
                    var text = await topLevel.Clipboard.GetTextAsync();
                    if (!string.IsNullOrEmpty(text))
                    {
                        UserInput?.Invoke(this, text);
                    }
                }
             }
        }
    }
}
