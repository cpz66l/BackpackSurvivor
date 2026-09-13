#if UNITY_EDITOR
using UnityEngine.EventSystems;
namespace BackpackSurvivor.EditorTools
{
    // Supplies IME composition through the same BaseInput hook read by TMP_InputField.
    public sealed class ImeCompositionAuditInput : BaseInput
    {
        public string composition = "";
        public override string compositionString => composition;
    }
}
#endif
