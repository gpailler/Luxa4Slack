namespace CG.Luxa4Slack.Tray.Views
{
  using System;
  using CG.Luxa4Slack.Tray.ViewModels;
  using Microsoft.Toolkit.Mvvm.Messaging;

  public partial class PreferencesWindow
  {
    private readonly IMessenger _messenger;

    public PreferencesWindow(PreferencesViewModel viewModel, IMessenger messenger)
    {
      _messenger = messenger;

      InitializeComponent();
      DataContext = viewModel;

      messenger.Register<CloseWindowMessage>(this, CloseWindow);
    }

    protected override void OnClosed(EventArgs e)
    {
      _messenger.Unregister<CloseWindowMessage>(this);
      base.OnClosed(e);
    }

    private void CloseWindow(object recipient, CloseWindowMessage message)
    {
      Close();
    }
  }
}
