using Google.Solutions.Ssh;
using System;

namespace IapDesktop.Application.Avalonia.Services
{
    public class GuiKeyboardInteractiveHandler : IKeyboardInteractiveHandler
    {
        public string? Prompt(string caption, string instruction, string prompt, bool echo)
        {
            // For now, just log and return null or empty? 
            // In a real GUI, we would show a dialog.
            System.Diagnostics.Debug.WriteLine($"[SSH Prompt] {caption}: {instruction} ({prompt})");
            return null;
        }

        public IPasswordCredential PromptForCredentials(string username)
        {
            throw new NotImplementedException("Password authentication is not yet supported in the GUI.");
        }
    }
}
