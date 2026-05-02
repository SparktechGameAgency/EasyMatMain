using UnityEngine;

public class SwipeInput : MonoBehaviour
{
    private Vector2 startTouchPos;
    private Vector2 endTouchPos;
    private bool isSwiping = false;

    public static SwipeDirection swipeDirection = SwipeDirection.None;

    public enum SwipeDirection
    {
        None, Up, Down, Left, Right
    }

    private void Update()
    {
        swipeDirection = SwipeDirection.None;

        ////#if UNITY_EDITOR || UNITY_STANDALONE
        //        // For testing in Editor with mouse drag
        //        if (Input.GetMouseButtonDown(0))
        //        {
        //            startTouchPos = Input.mousePosition;
        //            isSwiping = true;
        //        }
        //        else if (Input.GetMouseButtonUp(0))
        //        {
        //            endTouchPos = Input.mousePosition;
        //            DetectSwipe();
        //            isSwiping = false;
        //        }
        ////#else
        ////        // For mobile
        ////        if (Input.touchCount > 0)
        ////        {
        ////            Touch touch = Input.GetTouch(0);
        ////            switch (touch.phase)
        ////            {
        ////                case TouchPhase.Began:
        ////                    startTouchPos = touch.position;
        ////                    isSwiping = true;
        ////                    break;

        ////                case TouchPhase.Ended:
        ////                    endTouchPos = touch.position;
        ////                    DetectSwipe();
        ////                    isSwiping = false;
        ////                    break;
        ////            }
        ////        }
        ////#endif

    #if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0))
            {
                startTouchPos = Input.mousePosition;
                isSwiping = true;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                endTouchPos = Input.mousePosition;
                DetectSwipe();
                isSwiping = false;
            }
    #else
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        startTouchPos = touch.position;
                        isSwiping = true;
                        break;
                    case TouchPhase.Ended:
                        endTouchPos = touch.position;
                        DetectSwipe();
                        isSwiping = false;
                        break;
                }
            }
    #endif
    }

    private void DetectSwipe()
    {
        Vector2 swipeDelta = endTouchPos - startTouchPos;

        if (swipeDelta.magnitude < 100f) return; // minimum swipe distance

        float x = Mathf.Abs(swipeDelta.x);
        float y = Mathf.Abs(swipeDelta.y);

        if (x > y)
        {
            swipeDirection = swipeDelta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
        }
        else
        {
            swipeDirection = swipeDelta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
        }
    }
}
