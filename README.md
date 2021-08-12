# CSGO-AutoAccept
Auto match accept for CSGO with home assistant push notifications.

## Features
- Hotkey for enabling auto accept: Ctrl + PgUp.
- Config file with home assistant auth token and ip.

## Todo
- [ ] Hotkey setted in config
- [ ] Add another notifications service

## Config Template
```
{
  "enabled": true,
  "hass": {
    "enabled": true,
    "authkey": "auth token",
    "ip": "http://localhost:8123",
    "notifyservice": "mobile_app_j710f"
  },
  "telegram": {
    "enabled": true,
    "bottoken": "telegram bot token (numbers:characters)",
    "chatid": "chat id (https://api.telegram.org/bot<token>)/getUpdates)"
  }
}
```