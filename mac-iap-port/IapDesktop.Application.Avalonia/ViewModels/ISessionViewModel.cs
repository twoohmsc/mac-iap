using System;

namespace IapDesktop.Application.Avalonia.ViewModels
{
    public interface ISessionViewModel : IDisposable
    {
        string Title { get; }
    }
}
