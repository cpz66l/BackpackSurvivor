using UnityEngine;

namespace BS.Presentation
{
    /// <summary>Only covers menu lettering/buttons while an existing modal is visible.</summary>
    public sealed class MainMenuOverlayPresentation : MonoBehaviour
    {
        [SerializeField] GameObject[] modals;
        [SerializeField] CanvasGroup[] surfaces;
        bool hidden;
        public void Configure(GameObject[] panels, CanvasGroup[] groups) { modals = panels; surfaces = groups; }
        void LateUpdate()
        {
            bool visible = false;
            if (modals != null) foreach (var modal in modals) if (modal && modal.activeInHierarchy) { visible = true; break; }
            if (visible == hidden) return;
            hidden = visible; Apply();
        }
        void OnDisable() { hidden = false; Apply(); }
        void Apply()
        {
            if (surfaces == null) return;
            foreach (var surface in surfaces)
            {
                if (!surface) continue;
                surface.alpha = hidden ? 0 : 1;
                surface.interactable = surface.blocksRaycasts = !hidden;
            }
        }
    }
}
