namespace CG.Luxa4Slack.Tray.Views
{
  using System;
  using System.Windows;
  using CG.Luxa4Slack.Tray.ViewModels;
  using GalaSoft.MvvmLight;
  using GalaSoft.MvvmLight.Messaging;

  public partial class PreferencesWindow : Window
  {
    private readonly IMessenger messenger;

    public PreferencesWindow(PreferencesViewModel viewModel, IMessenger messenger)
    {
      this.messenger = messenger;

      this.InitializeComponent();
      this.DataContext = viewModel;

      messenger.Register<CloseWindowMessage>(this.DataContext, this.CloseWindow);
    }

    protected override void OnClosed(EventArgs e)
    {
      messenger.Unregister<CloseWindowMessage>(this.DataContext, this.CloseWindow);
      (this.DataContext as ICleanup)?.Cleanup();
      base.OnClosed(e);
    }

    private void CloseWindow(CloseWindowMessage _)
    {
      this.Close();
    }
  }
}
