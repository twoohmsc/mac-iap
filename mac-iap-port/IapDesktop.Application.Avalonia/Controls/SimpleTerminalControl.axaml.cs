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
             
             var outputEditor = this.FindControl<AvaloniaEdit.TextEditor>("OutputEditor");
             if (outputEditor != null)
             {
                 outputEditor.Document = new TextDocument();
                 outputEditor.TextArea.TextInput += OutputEditor_TextInput;
                 outputEditor.KeyDown += OutputEditor_KeyDown;
                 // Prevent standard pasting to handle it manually or forward it
                 // outputEditor.TextArea.TextPasting ...
             }
         }

         private void OutputEditor_TextInput(object? sender, TextInputEventArgs e)
         {
             if (!string.IsNullOrEmpty(e.Text))
             {
                 UserInput?.Invoke(this, e.Text);
             }
             e.Handled = true;
         }
 
         private void OutputEditor_KeyDown(object? sender, KeyEventArgs e)
         {
             // Map special keys manually if needed
             if (e.Key == Key.Enter)
             {
                 UserInput?.Invoke(this, "\r");
                 e.Handled = true;
             }
             else if (e.Key == Key.Back)
             {
                 UserInput?.Invoke(this, "\b");
                 e.Handled = true;
             }
             // Add more special key handling as needed
         }
          
         public void AppendText(string text)
         {
             Dispatcher.UIThread.InvokeAsync(() =>
             {
                 var outputEditor = this.FindControl<AvaloniaEdit.TextEditor>("OutputEditor");
                 if (outputEditor != null)
                 {
                     // Simple append for now. 
                     // TODO: Implement ANSI parsing here.
                     // For now just strip ANSI or append raw?
                     // Let's just append raw and see, but likely we need to process it.
                     // For correct terminal emulation we need a proper VT100 parser.
                     // But for now, let's just make it output *something*.
                     
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
    }
}
