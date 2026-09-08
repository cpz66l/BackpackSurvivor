using BS.GamePlay.Run;
using UnityEngine;

namespace BS.Presentation
{
    /// <summary>Hides only the underlying visual/input surfaces while a run modal is open.</summary>
    public sealed class RunOverlayPresentation : MonoBehaviour
    {
        [SerializeField] GameSession session;
        [SerializeField] CanvasGroup[] surfaces;
        float[] alphas;
        bool[] interactable, raycasts;
        bool covered;

        public void Configure(GameSession game, CanvasGroup[] groups)
        {
            Restore(); session = game; surfaces = groups;
        }
        void OnEnable()
        {
            if (!session) session = FindAnyObjectByType<GameSession>();
            if (session) session.OnStateChanged += OnState;
        }
        void Start() { if (session) OnState(session.State); }
        void OnDisable()
        {
            if (session) session.OnStateChanged -= OnState;
            Restore();
        }
        void OnState(GameState state)
        {
            bool modal = state == GameState.Paused || state == GameState.LevelUpSelecting ||
                         state == GameState.Victory || state == GameState.Defeat;
            if (!modal) { Restore(); return; }
            if (covered || surfaces == null) return;
            alphas = new float[surfaces.Length];
            interactable = new bool[surfaces.Length]; raycasts = new bool[surfaces.Length];
            for (int i = 0; i < surfaces.Length; i++)
            {
                if (!surfaces[i]) continue;
                alphas[i] = surfaces[i].alpha;
                interactable[i] = surfaces[i].interactable; raycasts[i] = surfaces[i].blocksRaycasts;
            }
            covered = true; Hide();
        }
        void LateUpdate() { if (covered) Hide(); }
        void Hide()
        {
            foreach (CanvasGroup surface in surfaces)
            {
                if (!surface) continue;
                surface.alpha = 0; surface.interactable = false; surface.blocksRaycasts = false;
            }
        }
        void Restore()
        {
            if (!covered) return;
            for (int i = 0; i < surfaces.Length; i++)
            {
                if (!surfaces[i]) continue;
                surfaces[i].alpha = alphas[i];
                surfaces[i].interactable = interactable[i]; surfaces[i].blocksRaycasts = raycasts[i];
            }
            covered = false;
        }
    }
}
