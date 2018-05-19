using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KittopiaTech.UI.Framework
{
    // https://forum.unity.com/threads/child-objects-blocking-scrollrect-from-scrolling.311555/#post-2351789
    public class FixScrollRect : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public ScrollRect MainScroll;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (enabled)
            {
                MainScroll.OnBeginDrag(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (enabled)
            {
                MainScroll.OnDrag(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (enabled)
            {
                MainScroll.OnEndDrag(eventData);
            }
        }


        public void OnScroll(PointerEventData data)
        {
            if (enabled)
            {
                MainScroll.OnScroll(data);
            }
        }
    }
}