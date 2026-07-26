namespace BS.GamePlay.Interaction
{
    public interface IInteractable
    {
        string GetPrompt();   // 提示文本，例如 "按 E 拾取 医疗包"
        bool Interact();      // 执行交互
    }
}