using TMPro;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

// Gives a type box the caret as soon as it is on screen, so a prompt can be typed into straight away.
internal sealed class TimbnFocusOnStart : MonoBehaviour
{
    public TMP_InputField? Field { get; set; }

    private void Start()
    {
        if (Field == null)
            return;

        Field.Select();
        Field.ActivateInputField();
        Field.caretPosition = Field.text.Length;
    }
}
