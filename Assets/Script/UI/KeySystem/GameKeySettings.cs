using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GameKeyAction
{
    MoveLeft,
    MoveRight,
    MoveDown,
    Jump,
    BuildMode,
    SwitchWeapon,
    Laser,
    SelectPlatform,
    SelectWall,
    SelectSpike
}

public static class GameKeySettings
{
    private const string SavePrefix = "GameKey_";
    private const string MousePrefix = "GameKeyMouse_";

    private static readonly Key[] defaultKeys =
    {
        Key.A,
        Key.D,
        Key.S,
        Key.Space,
        Key.L,
        Key.C,
        Key.E,
        Key.Digit1,
        Key.Digit2,
        Key.Digit3
    };

    private static readonly Key[] currentKeys =
        new Key[defaultKeys.Length];

    private static readonly bool[] useRightMouse =
        new bool[defaultKeys.Length];

    private static bool loaded;

    private static void Load()
    {
        if (loaded)
        {
            return;
        }

        for (int i = 0; i < currentKeys.Length; i++)
        {
            GameKeyAction action = (GameKeyAction)i;

            currentKeys[i] = (Key)PlayerPrefs.GetInt(
                SavePrefix + action,
                (int)defaultKeys[i]);

            useRightMouse[i] =
                action == GameKeyAction.BuildMode &&
                PlayerPrefs.GetInt(
                    MousePrefix + action,
                    PlayerPrefs.HasKey(
                        SavePrefix + action)
                        ? 0
                        : 1) == 1;
        }

        loaded = true;
    }

    public static Key Get(GameKeyAction action)
    {
        Load();
        return currentKeys[(int)action];
    }

    public static bool IsPressed(GameKeyAction action)
    {
        Load();

        if (useRightMouse[(int)action])
        {
            Mouse mouse = Mouse.current;

            return mouse != null &&
                   mouse.rightButton.isPressed;
        }

        Keyboard keyboard = Keyboard.current;

        return keyboard != null &&
               keyboard[Get(action)].isPressed;
    }

    public static bool WasPressed(GameKeyAction action)
    {
        Load();

        if (useRightMouse[(int)action])
        {
            Mouse mouse = Mouse.current;

            return mouse != null &&
                   mouse.rightButton.wasPressedThisFrame;
        }

        Keyboard keyboard = Keyboard.current;

        return keyboard != null &&
               keyboard[Get(action)].wasPressedThisFrame;
    }

    public static void Set(
        GameKeyAction action,
        Key key)
    {
        Load();

        currentKeys[(int)action] = key;
        useRightMouse[(int)action] = false;

        PlayerPrefs.SetInt(
            SavePrefix + action,
            (int)key);
        PlayerPrefs.SetInt(MousePrefix + action, 0);

        PlayerPrefs.Save();
    }

    public static void SetRightMouse(GameKeyAction action)
    {
        if (action != GameKeyAction.BuildMode)
        {
            return;
        }

        Load();

        useRightMouse[(int)action] = true;
        PlayerPrefs.SetInt(MousePrefix + action, 1);
        PlayerPrefs.Save();
    }

    public static bool UsesRightMouse(GameKeyAction action)
    {
        Load();
        return useRightMouse[(int)action];
    }

    public static bool IsUsedByOtherAction(
        GameKeyAction currentAction,
        Key key)
    {
        Load();

        foreach (GameKeyAction action in
                 Enum.GetValues(typeof(GameKeyAction)))
        {
            if (action != currentAction &&
                !useRightMouse[(int)action] &&
                Get(action) == key)
            {
                return true;
            }
        }

        return false;
    }

    public static void ResetAll()
    {
        for (int i = 0; i < defaultKeys.Length; i++)
        {
            GameKeyAction action = (GameKeyAction)i;

            currentKeys[i] = defaultKeys[i];
            useRightMouse[i] =
                action == GameKeyAction.BuildMode;
            PlayerPrefs.DeleteKey(SavePrefix + action);
            PlayerPrefs.DeleteKey(MousePrefix + action);
        }

        loaded = true;
        PlayerPrefs.Save();
    }

    public static string GetDisplayName(
        GameKeyAction action)
    {
        if (UsesRightMouse(action))
        {
            return "Right Mouse";
        }

        Key key = Get(action);

        switch (key)
        {
            case Key.Digit1:
                return "1";

            case Key.Digit2:
                return "2";

            case Key.Digit3:
                return "3";

            case Key.Space:
                return "Space";

            case Key.LeftShift:
                return "Left Shift";

            case Key.RightShift:
                return "Right Shift";

            case Key.LeftCtrl:
                return "Left Ctrl";

            case Key.RightCtrl:
                return "Right Ctrl";

            default:
                return key.ToString();
        }
    }
}
