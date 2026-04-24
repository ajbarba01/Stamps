# Launch

Run an app or switch to it with a keystroke. If the app is already open, Launch brings it to the foreground. If it isn't, Launch starts it.

## Usage

1. Open the **Actions** tab and click **Add launch**.
2. Give it a name and paste the full path to the exe.
3. Bind a trigger hotkey using the hotkey box on the new card.

Press the hotkey at any time — Launch will focus the running window or start the app if it isn't running.

## Finding an exe path

- **Start menu**: right-click the app → *Open file location* → right-click the shortcut → *Properties* → copy the **Target** field.
- **Task Manager**: with the app open, right-click its process → *Open file location*.

## Example

| Name          | Exe path                                                                 | Trigger    |
| ------------- | ------------------------------------------------------------------------ | ---------- |
| VS Code       | `C:\Users\you\AppData\Local\Programs\Microsoft VS Code\Code.exe`         | Ctrl+Alt+V |
| Spotify       | `C:\Users\you\AppData\Roaming\Spotify\Spotify.exe`                       | Ctrl+Alt+M |

## Notes

- Entries with no exe path or no trigger are silently ignored.
- If the trigger conflicts with another registered hotkey, a warning is logged and the action will not fire.
- If the app is already focused, the hotkey will briefly flash the taskbar button — this is a Windows limitation on foreground window switching.
