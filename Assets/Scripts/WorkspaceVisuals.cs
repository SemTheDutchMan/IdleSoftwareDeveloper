using UnityEngine;
using UnityEngine.UI;

namespace CodeClicker
{
    public sealed class WorkspaceVisuals : MonoBehaviour
    {
        [SerializeField] private RectTransform coffeeMachine;
        [SerializeField] private Texture2D coffeeMachinePicture;

        private bool coffeeMachineShown;

        public void Configure(RectTransform machine, Texture2D picture)
        {
            coffeeMachine = machine;
            coffeeMachinePicture = picture;
        }

        public void ShowCoffeeMachine()
        {
            if (coffeeMachineShown)
            {
                return;
            }

            FindCoffeeMachine();

            if (coffeeMachine == null)
            {
                coffeeMachine = CreateCoffeeMachine();
            }

            if (coffeeMachine == null)
            {
                return;
            }

            coffeeMachineShown = true;
            coffeeMachine.gameObject.SetActive(true);
            coffeeMachine.SetAsLastSibling();
        }

        public void ResetVisuals()
        {
            coffeeMachineShown = false;
            FindCoffeeMachine();

            Image computerPicture = GetComponent<Image>();
            if (computerPicture != null)
            {
                computerPicture.enabled = true;
            }

            if (coffeeMachine != null)
            {
                coffeeMachine.gameObject.SetActive(false);
            }
        }

        private void FindCoffeeMachine()
        {
            if (coffeeMachine != null)
            {
                return;
            }

            coffeeMachine = transform.Find("CoffeeMachine") as RectTransform;
        }

        private RectTransform CreateCoffeeMachine()
        {
            if (coffeeMachinePicture == null)
            {
                return null;
            }

            GameObject machineObject = new GameObject(
                "CoffeeMachine", typeof(RectTransform), typeof(RawImage));
            machineObject.transform.SetParent(transform, false);

            RectTransform rect = machineObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.14f, 0.30f);
            rect.anchorMax = new Vector2(0.34f, 0.58f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            RawImage image = machineObject.GetComponent<RawImage>();
            image.texture = coffeeMachinePicture;
            image.raycastTarget = false;

            return rect;
        }
    }
}
