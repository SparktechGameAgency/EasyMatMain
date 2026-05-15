using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Snake : MonoBehaviour
{
    private enum Direction { Left, Right, Up, Down }
    private enum State { Alive, Dead }

    //[SerializeField] private Vector2Int gridSize = new Vector2Int(10, 20); // Adjust to fit with panel

    private State state;

    private bool gameStarted = false;

    public float snakeMoveSpeed = 2f;
    private Vector2Int gridPosotion;

    private Direction gridMoveDirection;
    private float gridMoveTimer;
    private float gridMoveTimerMax;
    private LevelGrid levelGrid;
    private int snakeBodySize;

    public bool canPassWalls = false;   // added new for testing ==================================

    private List<SnakeMovePosition> snakeMovePositionList;

    private List<SnakeBodyPart> snakeBodyPartList;

    //// === Tail Code Start ===
    //private GameObject snakeTailObject;
    //private SpriteRenderer snakeTailRenderer;
    //// === Tail Code End ===

    [SerializeField] private GameOverWindow gameOverWindow;

    public void Setup(LevelGrid levelGrid)
    {
        this.levelGrid = levelGrid;
    }

    private void Awake()
    {
        gridPosotion = new Vector2Int(8, 15);
        gridMoveTimerMax = 1f;
        gridMoveTimer = gridMoveTimerMax;

        gridMoveDirection = Direction.Right;

        snakeMovePositionList = new List<SnakeMovePosition>();
        snakeBodySize = 0;

        snakeBodyPartList = new List<SnakeBodyPart>();
        state = State.Alive;


        //// === Tail Code Start ===
        //snakeTailObject = new GameObject("SnakeTail", typeof(SpriteRenderer));
        //snakeTailRenderer = snakeTailObject.GetComponent<SpriteRenderer>();
        ////snakeTailRenderer.sprite = GameAssets.Instance.snakeTailSprite;
        //snakeTailRenderer.sortingOrder = -100; // draw behind everything else
        //snakeTailObject.transform.position = new Vector3(gridPosotion.x - 1, gridPosotion.y, 0);
        //// === Tail Code End ===
    }

    // Update is called once per frame
    //void Update()
    //{
    //    switch (state)
    //    {
    //        case State.Alive: HandleInput(); HandleGridMovement(); break;
    //        case State.Dead: break;
    //    }

    //}
    void Update()
    {
        switch (state)
        {
            case State.Alive:
                if (!gameStarted)
                {
                    if (Input.GetMouseButtonDown(0) ||
                        (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                    {
                        gameStarted = true;
                    }
                    return;
                }
                HandleInput();
                HandleGridMovement();
                break;

            case State.Dead:
                break;
        }
    }

    // added new for testing ==================================
    public IEnumerator DisableWallPassAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        canPassWalls = false;

        // Tell LevelGrid to recreate the walls
        FindObjectOfType<GameHandler>().levelGrid.CreateWalls();
    }

    public IEnumerator SlowDownFor(float duration)
    {
        float originalSpeed = snakeMoveSpeed;
        snakeMoveSpeed = originalSpeed * 0.5f;
        yield return new WaitForSeconds(duration);
        snakeMoveSpeed = originalSpeed;
    }


    private void HandleInput()
    {
        switch (SwipeInput.swipeDirection)
        {
            case SwipeInput.SwipeDirection.Up:
                if (gridMoveDirection != Direction.Down)
                    gridMoveDirection = Direction.Up;
                break;

            case SwipeInput.SwipeDirection.Down:
                if (gridMoveDirection != Direction.Up)
                    gridMoveDirection = Direction.Down;
                break;

            case SwipeInput.SwipeDirection.Left:
                if (gridMoveDirection != Direction.Right)
                    gridMoveDirection = Direction.Left;
                break;

            case SwipeInput.SwipeDirection.Right:
                if (gridMoveDirection != Direction.Left)
                    gridMoveDirection = Direction.Right;
                break;
        }

        // Keyboard input (optional for Editor)
        if (Input.GetKeyDown(KeyCode.UpArrow) && gridMoveDirection != Direction.Down)
            gridMoveDirection = Direction.Up;
        else if (Input.GetKeyDown(KeyCode.DownArrow) && gridMoveDirection != Direction.Up)
            gridMoveDirection = Direction.Down;
        else if (Input.GetKeyDown(KeyCode.LeftArrow) && gridMoveDirection != Direction.Right)
            gridMoveDirection = Direction.Left;
        else if (Input.GetKeyDown(KeyCode.RightArrow) && gridMoveDirection != Direction.Left)
            gridMoveDirection = Direction.Right;
    }


    private void HandleGridMovement()
    {
        gridMoveTimer += Time.deltaTime * snakeMoveSpeed;
        if (gridMoveTimer >= gridMoveTimerMax)
        {
            gridMoveTimer -= gridMoveTimerMax;

            SnakeMovePosition previousSnakeMovePosition = null;
            if (snakeMovePositionList.Count > 0)
            {
                previousSnakeMovePosition = snakeMovePositionList[0];
            }

            SnakeMovePosition snakeMovePosition = new SnakeMovePosition(previousSnakeMovePosition, gridPosotion, gridMoveDirection);
            snakeMovePositionList.Insert(0, snakeMovePosition);

            Vector2Int gridMoveDirectionVector;
            switch (gridMoveDirection)
            {
                default:
                case Direction.Right: gridMoveDirectionVector = new Vector2Int(+1, 0); break;
                case Direction.Left: gridMoveDirectionVector = new Vector2Int(-1, 0); break;
                case Direction.Up: gridMoveDirectionVector = new Vector2Int(0, +1); break;
                case Direction.Down: gridMoveDirectionVector = new Vector2Int(0, -1); break;
            }

            gridPosotion += gridMoveDirectionVector;

            gridPosotion = levelGrid.ValidateGridPosition(gridPosotion);
            // Keep movement inside the playable grid (panel bounds)
            //gridPosotion.x = Mathf.Clamp(gridPosotion.x, 0, gridSize.x - 1);
            //gridPosotion.y = Mathf.Clamp(gridPosotion.y, 0, gridSize.y - 1);

            bool snakeAteFood = levelGrid.TrySnakeEatFood(gridPosotion);
            if (snakeAteFood)
            {
                // Snake ate food and grow body
                snakeBodySize++;
                CreateSnakeBody();
            }

            if (snakeMovePositionList.Count >= snakeBodySize + 1)
            {
                snakeMovePositionList.RemoveAt(snakeMovePositionList.Count - 1);
            }

            /* foreach (SnakeBodyPart snakeBodyPart in snakeBodyPartList)
             {
                 Vector2Int snakeBodyPartGridPosition = snakeBodyPart.GetGridPosition();
                 if (gridPosotion == snakeBodyPartGridPosition)
                 {
                     // Game Over
                     state = State.Dead;
                     //GameHandler.SnakeDied();
                     if (gameOverWindow != null)
                         gameOverWindow.ShowGameOver();
                     return;
                 }
             }*/

            foreach (SnakeBodyPart snakeBodyPart in snakeBodyPartList)
            {
                Vector2Int snakeBodyPartGridPosition = snakeBodyPart.GetGridPosition();
                if (gridPosotion == snakeBodyPartGridPosition)
                {
                    state = State.Dead;

                    // ? Save score to Firestore
                    if (GameHandler.instance != null)
                        GameHandler.instance.TriggerGameOver();

                    if (gameOverWindow != null)
                        gameOverWindow.ShowGameOver();
                    return;
                }
            }

            transform.position = new Vector3(gridPosotion.x, gridPosotion.y);
            transform.eulerAngles = new Vector3(0, 0, GetAngleFromVector(gridMoveDirectionVector));

            UpdateSnakeBodyParts();


            // === Tail Code Start ===
            //if (snakeBodyPartList.Count > 0)
            //{
            //    // Get the last body part’s grid position
            //    SnakeBodyPart lastBody = snakeBodyPartList[snakeBodyPartList.Count - 1];
            //    Vector2Int tailGridPos = lastBody.GetGridPosition();

            //    // --- NEW CODE START ---
            //    // Offset tail to appear *behind* the last body part based on its move direction
            //    Vector2Int offset = Vector2Int.zero;
            //    switch (lastBody.GetDirection()) // assuming you store movement direction in SnakeBodyPart
            //    {
            //        case Direction.Up: offset = Vector2Int.down; break;
            //        case Direction.Down: offset = Vector2Int.up; break;
            //        case Direction.Left: offset = Vector2Int.right; break;
            //        case Direction.Right: offset = Vector2Int.left; break;
            //    }
            //    tailGridPos += offset;
            //    // --- NEW CODE END ---

            //    // Update tail position
            //    snakeTailObject.transform.position = new Vector3(tailGridPos.x, tailGridPos.y, 0);

            //    //// Rotate tail to face same direction as last body part
            //    //snakeTailObject.transform.rotation = lastBody.GetRotation();
            //    //snakeTailObject.transform.rotation = lastBody.GetTransform().rotation;
            //    float tailAngle = 0f;
            //    switch (lastBody.GetDirection())
            //    {
            //        case Direction.Up: tailAngle = 90f; break;
            //        case Direction.Down: tailAngle = 270f; break;
            //        case Direction.Left: tailAngle = 180f; break;
            //        case Direction.Right: tailAngle = 0f; break;
            //    }
            //    snakeTailObject.transform.rotation = Quaternion.Euler(0, 0, tailAngle);
            //}
            //else
            //{
            //    // If no body yet, tail follows the head
            //    //snakeTailObject.transform.position = new Vector3(gridPosotion.x - 1, gridPosotion.y, 0);
            //    // --- NEW CODE START ---
            //    Vector2Int tailGridPos = gridPosotion;
            //    Vector2Int offset = Vector2Int.zero;
            //    switch (gridMoveDirection)
            //    {
            //        case Direction.Up: offset = Vector2Int.down; break;
            //        case Direction.Down: offset = Vector2Int.up; break;
            //        case Direction.Left: offset = Vector2Int.right; break;
            //        case Direction.Right: offset = Vector2Int.left; break;
            //    }
            //    tailGridPos += offset;
            //    snakeTailObject.transform.position = new Vector3(tailGridPos.x, tailGridPos.y, 0);

            //    float tailAngle = 0f;
            //    switch (gridMoveDirection)
            //    {
            //        case Direction.Up: tailAngle = 90f; break;
            //        case Direction.Down: tailAngle = 270f; break;
            //        case Direction.Left: tailAngle = 180f; break;
            //        case Direction.Right: tailAngle = 0f; break;
            //    }
            //    snakeTailObject.transform.rotation = Quaternion.Euler(0, 0, tailAngle);
            //    // --- NEW CODE END ---
            //}
            // === Tail Code End ===
        }
    }

    private void CreateSnakeBody()
    {
        snakeBodyPartList.Add(new SnakeBodyPart(snakeBodyPartList.Count,
            GameAssets.Instance.snakeBodySprite,
            GameAssets.Instance.snakeBodyCornerSprite));
    }


    private void UpdateSnakeBodyParts()
    {
        for (int i = 0; i < snakeBodyPartList.Count; i++)
        {
            snakeBodyPartList[i].SetSnakeMovePosition(snakeMovePositionList[i]);
        }
    }

    private float GetAngleFromVector(Vector2Int dir)
    {
        float n = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (n < 0) n += 360;
        return n;
    }

    public Vector2Int GetGridPosition()
    {
        return gridPosotion;
    }

    // Return the full list of positions occupied by the snake; Head + Body
    public List<Vector2Int> GetFullSnakeGridPositionList()
    {
        List<Vector2Int> gridPositionList = new List<Vector2Int>() { gridPosotion };
        foreach (SnakeMovePosition snakeMovePosition in snakeMovePositionList)
        {
            gridPositionList.Add(snakeMovePosition.GetGridPosition());
        }
        return gridPositionList;
    }

    // Handles a Single Body Part
    private class SnakeBodyPart
    {
        private SnakeMovePosition snakeMovePosition;
        private Transform transform;
        private Sprite straightSprite;
        private Sprite cornerSprite;

        public SnakeBodyPart(int bodyIndex, Sprite straightSprite, Sprite cornerSprite)
        {
            this.straightSprite = straightSprite;
            this.cornerSprite = cornerSprite;

            GameObject snakeBodyGameObject = new GameObject("SnakeBody", typeof(SpriteRenderer));
            snakeBodyGameObject.GetComponent<SpriteRenderer>().sprite = straightSprite; // default
            snakeBodyGameObject.GetComponent<SpriteRenderer>().sortingOrder = -bodyIndex;
            transform = snakeBodyGameObject.transform;
        }

        public void SetSnakeMovePosition(SnakeMovePosition snakeMovePosition)
        {
            this.snakeMovePosition = snakeMovePosition;
            transform.position = new Vector3(
                snakeMovePosition.GetGridPosition().x,
                snakeMovePosition.GetGridPosition().y,
                0f);

            Direction dir = snakeMovePosition.GetDirection();
            Direction prevDir = snakeMovePosition.GetPreviousDirection();

            SpriteRenderer sr = transform.GetComponent<SpriteRenderer>();
            float angle = 0f;

            if (dir == prevDir)
            {
                // ---- STRAIGHT BODY ----
                sr.sprite = straightSprite;
                switch (dir)
                {
                    case Direction.Up: angle = 90; break;
                    case Direction.Down: angle = 90; break;
                    case Direction.Left: angle = 0; break;
                    case Direction.Right: angle = 0; break;
                }
            }
            else
            {
                // ---- CORNER BODY ----
                sr.sprite = cornerSprite;

                switch (dir)
                {
                    case Direction.Up:
                        switch (prevDir)
                        {
                            case Direction.Left: angle = 90; break;
                            case Direction.Right: angle = 180; break;
                        }
                        break;

                    case Direction.Down:
                        switch (prevDir)
                        {
                            case Direction.Left: angle = 0; break;
                            case Direction.Right: angle = 270; break;
                        }
                        break;

                    case Direction.Left:
                        switch (prevDir)
                        {
                            case Direction.Up: angle = 270; break;
                            case Direction.Down: angle = 180; break;
                        }
                        break;

                    case Direction.Right:
                        switch (prevDir)
                        {
                            case Direction.Up: angle = 0; break;
                            case Direction.Down: angle = 90; break;
                        }
                        break;
                }
            }

            transform.localRotation = Quaternion.Euler(0, 0, angle);
        }

        public Vector2Int GetGridPosition()
        {
            return snakeMovePosition != null ? snakeMovePosition.GetGridPosition() : new Vector2Int(-999, -999);
        }

        // === Tail Code Start ===
        public Transform GetTransform()
        {
            return transform;
        }
        //public Quaternion GetRotation()
        //{
        //    return transform.rotation;
        //}

        public Direction GetDirection()
        {
            return snakeMovePosition != null ? snakeMovePosition.GetDirection() : Direction.Right;
        }

        // === Tail Code End ===
    }

    // Handles one move position from the Snake 
    private class SnakeMovePosition
    {
        private SnakeMovePosition previousSnakeMovePosition;
        private Vector2Int gridPosition;
        private Direction direction;

        public SnakeMovePosition(SnakeMovePosition previousSnakeMovePosition, Vector2Int gridPosition, Direction direction)
        {
            this.previousSnakeMovePosition = previousSnakeMovePosition;
            this.gridPosition = gridPosition;
            this.direction = direction;
        }

        public Vector2Int GetGridPosition()
        {
            return gridPosition;
        }

        public Direction GetDirection()
        {
            return direction;
        }

        public Direction GetPreviousDirection()
        {
            if (previousSnakeMovePosition == null)
            {
                //return Direction.Right;
                return direction;
            }
            else
            {
                return previousSnakeMovePosition.direction;
            }
        }
    }

    public void ForceGameOver()
    {
        state = State.Dead;

        // ? Save score to Firestore
        if (GameHandler.instance != null)
            GameHandler.instance.TriggerGameOver();

        if (gameOverWindow != null)
            gameOverWindow.ShowGameOver();
    }
}
