namespace CG.Luxa4Slack.Tray
{
  using System.Windows;

  public partial class Preferences : Window
  {
    public Preferences()
    {
      this.InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
  }
}
