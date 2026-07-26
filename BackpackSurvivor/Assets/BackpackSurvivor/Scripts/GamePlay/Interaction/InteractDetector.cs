using BS.GamePlay.Combat;
using BS.GamePlay.Player;
using System;
using UnityEngine;

namespace BS.GamePlay.Interaction
{
    public class InteractDetector : MonoBehaviour
    {
        //事件
        public event Action<IInteractable> OnTargetChanged;

        [SerializeField] private float detectionRadius = 2.25f;
        [SerializeField] private LayerMask interactableLayerMask;
        [SerializeField] private float scanInterval = 0.3f;
        [SerializeField] private Collider[] buffer = new Collider[16];

        public IInteractable CurrentTarget { get; private set; }
        

        private IInteractable previousTarget;
        private Health playerH;
        private float timer;

        private InputReader ir;

        private void OnEnable()
        {
            ir.OnInteract += Interact;
        }
        private void OnDisable()
        {
            ir.OnInteract -= Interact;
        }
        private void Awake()
        {
            ir = GetComponent<InputReader>();
            playerH = GetComponent<Health>();
        }
        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= scanInterval)
            {
                timer = 0f;
                Scan();
            }
        }

        private void Scan()
        {
            int count = Physics.OverlapSphereNonAlloc(
            playerH.Position,
            detectionRadius,
            buffer,
            interactableLayerMask);

            IInteractable nearest = null;
            float minSqDist = Mathf.Infinity;

            for (int i = 0; i < count; i++)
            {
                var col = buffer[i];
                if (col == null) continue;

                var interactable = col.GetComponent<IInteractable>();
                if (interactable == null) continue;

                Vector3 offset = col.transform.position - playerH.Position;
                float sqrDist = offset.sqrMagnitude;

                if (sqrDist < minSqDist)
                {
                    minSqDist = sqrDist;
                    nearest = interactable;
                }
            }

            if (nearest != previousTarget) 
            {
                previousTarget = nearest;
                CurrentTarget = nearest;
                OnTargetChanged?.Invoke(nearest);
            }
        }

        private void Interact()
        {
            if (CurrentTarget == null) return;
            CurrentTarget.Interact();
            //交互后立即重置
            previousTarget = null;
            CurrentTarget = null;
            OnTargetChanged?.Invoke(null);   // 提示框立即隐藏
        }
    }
}
