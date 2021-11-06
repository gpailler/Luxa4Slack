namespace CG.Luxa4Slack.Tray.Views
{
  using System;

  public class PreferencesWindowController
  {
    private readonly Func<PreferencesWindow> _preferencesWindowFactory;
    private PreferencesWindow? _preferencesWindow;

    public PreferencesWindowController(Func<PreferencesWindow> preferencesWindowFactory)
    {
      _preferencesWindowFactory = preferencesWindowFactory;
    }

    public event Action<bool>? OpenedChanged;

    public void ShowDialog()
    {
      if (_preferencesWindow == null)
      {
        OpenedChanged?.Invoke(true);
        _preferencesWindow = _preferencesWindowFactory();
        _preferencesWindow.ShowDialog();
        _preferencesWindow = null;
        OpenedChanged?.Invoke(false);
      }
    }
  }
}
