using BS.GamePlay.Loot;
using BS.GamePlay.Player;
using TMPro;
using UnityEngine;
namespace BS.Presentation
{
    public class ChestDistanceView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI chestDistanceText;
        [SerializeField] private Transform player;
        [SerializeField] private float hideDistance = 3f;

        private void Awake()
        {
            if(player == null)
                player = FindAnyObjectByType<PlayerController>()?.transform;
        }

        private void Update()
        {
            if (player == null || chestDistanceText == null) return;
            if(LootChest.TryGetNearestUnopened(player.position, out LootChest nearestChest))
            {
                float distance = Vector3.Distance(player.position, nearestChest.transform.position);
                if (distance < hideDistance)
                {
                    chestDistanceText.text = $"宝箱附近";
                    return;
                }
                chestDistanceText.text = $"宝箱:{distance:F1}m";
            }
            else
            {
                chestDistanceText.text = "";
            }
        } 
    }
}
