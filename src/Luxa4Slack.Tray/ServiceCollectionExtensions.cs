﻿namespace CG.Luxa4Slack.Tray
{
  using System;
  using System.Windows;
  using System.Windows.Threading;
  using CG.Luxa4Slack.Tray.ViewModels;
  using CG.Luxa4Slack.Tray.Views;
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.Toolkit.Mvvm.Messaging;

  public static class ServiceCollectionExtensions
  {
    public static void RegisterLuxa4SlackTray(this IServiceCollection serviceCollection)
    {
      serviceCollection.AddScoped<ApplicationStartup>();
      serviceCollection.AddSingleton<ApplicationInfo>();
      serviceCollection.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
      serviceCollection.AddSingleton<Lazy<Dispatcher>>(_ => new Lazy<Dispatcher>(() => Application.Current.Dispatcher));

      serviceCollection.AddTransient<TrayIconViewModel>();
      serviceCollection.AddTransient<TrayIconController>();

      serviceCollection.AddTransient<PreferencesViewModel>();
      serviceCollection.AddTransient<PreferencesWindow>();
      serviceCollection.AddSingleton<PreferencesWindowController>();
      serviceCollection.AddSingleton<Func<PreferencesWindow>>(x => x.GetRequiredService<PreferencesWindow>);
    }
  }
}
