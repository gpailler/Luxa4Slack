namespace CG.Luxa4Slack.Tray.Views
{
  using System;

  public class PreferencesWindowController
  {
    private readonly Func<PreferencesWindow> preferencesWindowFactory;
    private PreferencesWindow? preferencesWindow;

    public PreferencesWindowController(Func<PreferencesWindow> preferencesWindowFactory)
    {
      this.preferencesWindowFactory = preferencesWindowFactory;
    }

    public event Action<bool>? OpenedChanged;

    public void ShowDialog()
    {
      if (this.preferencesWindow == null)
      {
        this.OpenedChanged?.Invoke(true);
        this.preferencesWindow = this.preferencesWindowFactory();
        this.preferencesWindow.ShowDialog();
        this.preferencesWindow = null;
        this.OpenedChanged?.Invoke(false);
      }
    }
  }
}
