using UnityEngine;

namespace SpeedTutorMainMenuSystem
{
    public class Init_LoadPreferences : MonoBehaviour
    {
        [SerializeField] private bool canUse;
        [SerializeField] private MenuController menuController;

        private void Awake()
        {
            if (canUse && menuController != null)
                menuController.LoadPreferences();
        }
    }
}
