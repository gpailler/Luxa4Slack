# Luxa4Slack
[![Build status](https://ci.appveyor.com/api/projects/status/jr2u84tj866eferw?svg=true)](https://ci.appveyor.com/project/gpailler/luxa4slack)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/gpailler/luxa4slack/blob/master/LICENSE)

Luxa4Slack is a small Windows app showing your [Slack](https://slack.com/) unread messages/mentions on your [Luxafor](http://luxafor.com/) device.
Your Luxafor device switches to red color if you have unread mention(s) and blue color if you have unread message(s).

## Usage
- Download latest [release](https://github.com/gpailler/Luxa4Slack/releases/latest) and uncompress the binaries in the folder you want.
- Create a Slack token on https://api.slack.com/docs/oauth-test-tokens
- Luxa4Slack exists in two flavors, `Luxa4Slack.Console.exe` or `Luxa4Slack.Tray.exe`
  - If you prefer the command line version, launch: `Luxa4Slack.Console.exe --token=xoxp-[...] --debug`
  - If you prefer the Window tray version, launch `Luxa4Slack.Tray.exe` and fill your token when requested.

## Screenshots
##### Luxa4Slack.Console
![Luxa4Slack.Console](https://cloud.githubusercontent.com/assets/3621529/16187882/6b0c1b44-3705-11e6-92b3-a941c6eba834.png)
##### Luxa4Slack.Tray
![Luxa4Slack.Console](https://cloud.githubusercontent.com/assets/3621529/16181665/f4a4f5fa-36cf-11e6-9a47-b65f9146e5c4.png)

## Throubleshooting
- Luxafor device is sometimes not properly detected and an error message is displayed. Unplug the device, wait few seconds,  plug it again and start Luxa4Slack again.
