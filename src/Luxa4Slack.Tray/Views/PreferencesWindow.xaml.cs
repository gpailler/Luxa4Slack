namespace CG.Luxa4Slack.Tray.Views
{
  using System;
  using CG.Luxa4Slack.Tray.ViewModels;
  using GalaSoft.MvvmLight;
  using GalaSoft.MvvmLight.Messaging;

  public partial class PreferencesWindow
  {
    private readonly IMessenger _messenger;

    public PreferencesWindow(PreferencesViewModel viewModel, IMessenger messenger)
    {
      _messenger = messenger;

      InitializeComponent();
      DataContext = viewModel;

      messenger.Register<CloseWindowMessage>(DataContext, CloseWindow);
    }

    protected override void OnClosed(EventArgs e)
    {
      _messenger.Unregister<CloseWindowMessage>(DataContext, CloseWindow);
      (DataContext as ICleanup)?.Cleanup();
      base.OnClosed(e);
    }

    private void CloseWindow(CloseWindowMessage _)
    {
      Close();
    }
  }
}
