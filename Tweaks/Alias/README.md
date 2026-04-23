# Alias

Remap any key combo to send a different key combo system-wide.

## Usage

1. Open the **Actions** tab and click **Add alias**.
2. Give the alias a name and choose the key combo to send.
3. Bind a trigger hotkey using the hotkey box on the new alias card.

When the trigger fires, Alias releases the trigger's modifier keys and injects the target combo — so the receiving app sees a clean input with no leftover modifiers.

## Example

| Trigger | Sends | Effect |
|---|---|---|
| Ctrl+Shift+W | Alt+F4 | Close the foreground window |
| Ctrl+Alt+T | Win+R | Open Run dialog |

## Notes

- Aliases with no trigger or no target are silently ignored.
- If a trigger conflicts with another registered hotkey, a warning is logged and the alias will not fire.
