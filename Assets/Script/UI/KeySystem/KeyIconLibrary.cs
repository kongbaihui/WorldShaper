using UnityEngine;
using UnityEngine.InputSystem;

public class KeyIconLibrary : MonoBehaviour
{
    [SerializeField]
    private Sprite[] keyboardIcons;

    [SerializeField]
    private Sprite keyboardFallbackIcon;

    [SerializeField]
    private Sprite rightMouseIcon;

    public Sprite GetIcon(GameKeyAction action)
    {
        if (GameKeySettings.UsesRightMouse(action))
        {
            return rightMouseIcon;
        }

        string iconName = GetIconName(
            GameKeySettings.Get(action));

        if (keyboardIcons != null)
        {
            foreach (Sprite icon in keyboardIcons)
            {
                if (icon != null &&
                    icon.name == iconName)
                {
                    return icon;
                }
            }
        }

        return keyboardFallbackIcon;
    }

    private static string GetIconName(Key key)
    {
        string keyName = key.ToString();

        if (key >= Key.A && key <= Key.Z)
        {
            return keyName.ToLowerInvariant();
        }

        if (key >= Key.Digit0 && key <= Key.Digit9)
        {
            return keyName.Substring(5);
        }

        if (key >= Key.F1 && key <= Key.F12)
        {
            return keyName.ToLowerInvariant();
        }

        switch (key)
        {
            case Key.Space:
                return "space";
            case Key.LeftArrow:
                return "arrow-left";
            case Key.RightArrow:
                return "arrow-right";
            case Key.UpArrow:
                return "arrow-up";
            case Key.DownArrow:
                return "arrow-down";
            case Key.LeftShift:
            case Key.RightShift:
                return "shift";
            case Key.LeftCtrl:
            case Key.RightCtrl:
                return "ctrl";
            case Key.LeftAlt:
            case Key.RightAlt:
                return "alt";
            case Key.Escape:
                return "esc";
            case Key.Enter:
            case Key.NumpadEnter:
                return "enter";
            case Key.Backspace:
                return "backspace";
            case Key.Tab:
                return "tab";
            case Key.CapsLock:
                return "caps";
            case Key.Delete:
                return "del";
            case Key.Insert:
                return "ins";
            case Key.Home:
                return "home";
            case Key.End:
                return "end";
            case Key.PageUp:
                return "pgup";
            case Key.PageDown:
                return "pgdn";
            case Key.Backquote:
                return "tilde";
            case Key.Minus:
                return "hyphen";
            case Key.Equals:
                return "equals";
            case Key.LeftBracket:
                return "bracket-open";
            case Key.RightBracket:
                return "bracket-close";
            case Key.Backslash:
                return "backward-slash";
            case Key.Semicolon:
                return "semi-colon";
            case Key.Quote:
                return "quote";
            case Key.Comma:
                return "comma";
            case Key.Period:
                return "dot";
            case Key.Slash:
                return "forward-slash";
            default:
                return "";
        }
    }
}
