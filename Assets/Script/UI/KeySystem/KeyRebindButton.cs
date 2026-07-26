using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class KeyRebindButton : MonoBehaviour
{
    [SerializeField]
    private GameKeyAction action;

    [SerializeField]
    private TMP_Text keyText;

    [SerializeField]
    private TMP_Text hintText;

    [SerializeField]
    private Image actionIcon;

    private KeyIconLibrary iconLibrary;

    private bool waitingForKey;

    private static KeyRebindButton activeButton;

    private void OnEnable()
    {
        RefreshText();
    }

    private void Update()
    {
        if (!waitingForKey)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null &&
            keyboard.escapeKey.wasPressedThisFrame)
        {
            CancelRebind();
            return;
        }

        if (action == GameKeyAction.BuildMode)
        {
            Mouse mouse = Mouse.current;

            if (mouse != null &&
                mouse.rightButton.wasPressedThisFrame)
            {
                GameKeySettings.SetRightMouse(action);
                CompleteRebind();
                return;
            }
        }

        if (keyboard == null)
        {
            return;
        }

        foreach (var keyControl in
                 keyboard.allKeys)
        {
            if (keyControl == null ||
                !keyControl.wasPressedThisFrame)
            {
                continue;
            }

            Key newKey = keyControl.keyCode;

            if (GameKeySettings.IsUsedByOtherAction(
                    action,
                    newKey))
            {
                if (hintText != null)
                {
                    hintText.text =
                        "This key is already in use.";
                }

                return;
            }

            GameKeySettings.Set(action, newKey);
            CompleteRebind();
            return;
        }
    }

    public void StartRebind()
    {
        if (activeButton != null &&
            activeButton != this)
        {
            activeButton.CancelRebind();
        }

        activeButton = this;
        waitingForKey = true;

        if (keyText != null)
        {
            keyText.text = "...";
        }

        if (hintText != null)
        {
            hintText.text = action == GameKeyAction.BuildMode
                ? "Press a key or right mouse button. Press Esc to cancel."
                : "Press a key. Press Esc to cancel.";
        }
    }

    public void CancelRebind()
    {
        waitingForKey = false;

        if (activeButton == this)
        {
            activeButton = null;
        }

        if (hintText != null)
        {
            hintText.text = "";
        }

        RefreshText();
    }

    public void RefreshText()
    {
        if (keyText != null)
        {
            keyText.text =
                GameKeySettings.GetDisplayName(action);
        }

        if (iconLibrary == null)
        {
            iconLibrary =
                GetComponentInParent<KeyIconLibrary>();
        }

        if (actionIcon != null &&
            iconLibrary != null)
        {
            actionIcon.sprite =
                iconLibrary.GetIcon(action);
        }
    }

    private void CompleteRebind()
    {
        waitingForKey = false;
        activeButton = null;

        if (hintText != null)
        {
            hintText.text = "";
        }

        RefreshText();
    }

    public void ResetAllKeys()
    {
        GameKeySettings.ResetAll();

        KeyRebindButton[] buttons =
            FindObjectsOfType<KeyRebindButton>();

        foreach (KeyRebindButton button in buttons)
        {
            button.RefreshText();
        }

        if (hintText != null)
        {
            hintText.text =
                "Controls reset to defaults.";
        }
    }

    private void OnDisable()
    {
        if (waitingForKey)
        {
            CancelRebind();
        }
    }
}
