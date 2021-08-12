# CSGO-AutoAccept
Auto match accept for CSGO with home assistant push notifications.

## Features
- Hotkey for enabling auto accept: Ctrl + PgUp.
- Config file with home assistant notifications service setup and telegram bot setup.
- Configutable hotkey in config file

## Todo
- [x] Hotkey setted in config
- [ ] Add another notifications service (except telegram)

## Config Template
```
{
  "hass": {
    "enabled": true,
    "authkey": "auth token",
    "ip": "http://localhost:8123",
    "notifyservice": "mobile_app_j710f"
  },
  "telegram": {
    "enabled": true,
    "bottoken": "telegram bot token (numbers:characters)",
    "chatid": "chat id from: (https://api.telegram.org/bot<token>)/getUpdates)"
  },
  "hotkey": {
    "key": "def: PageUp (https://docs.microsoft.com/pl-pl/dotnet/api/system.windows.forms.keys)",
    "modifiers": "def: Control (available: Alt, Control, Shift, Windows, NoRepeat)"
  }
}
```