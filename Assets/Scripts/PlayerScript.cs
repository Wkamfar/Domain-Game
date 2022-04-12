using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    //make a sliding angle that if you slide at it, you won't stop sliding
    //implement my own gravity
    //make a player variable manager
    //make combination moves later
    //make it so that maxGroundSpeed slow down is fast, but not instant
    //Can only walk/sprint when height is normal

    //momentum cap only after player hits the ground
    //make wasd move
    //make crouch
    //make jump
    //make sprint
    //make slide
    //make shoot (primary and alternative firing modes)
    //make grapple
    //make bulletslow with deflect
    //make melee
    //make dodge
    //make block
    //make parry
    //make ledge climb
    //add cooldowns and stuff like that later
    //split this into different scripts
    public GameObject player;
    public CapsuleCollider playerCollider;
    public Rigidbody playerRigidbody;
    public GameObject head;
    public Camera mainCamera;
    [SerializeField] float maxXTurn;
    [SerializeField] float minXTurn;
    float xRotation;
    float yRotation;
    [SerializeField] float mouseSensitivity;
    [SerializeField] float walkSpeed;
    public float groundAcceleration;
    public float maxGroundSpeed;
    public float maxWalkingSpeed;
    public float maxSprintingSpeed;
    public float maxCrouchingSpeed;
    public float maxSlidingSpeed;
    public float maxAirSpeed;
    //Check if keys are pressed
    int[] moveKeyPressOrder = new int[4] { 0, 0, 0, 0 }; //0 = forward, 1 = left, 2 = back, 3 = right
    int moveKeysPressed;
    bool pressingForward;
    bool pressingLeft;
    bool pressingRight;
    bool pressingBack;
    bool pressingCrouch;
    bool pressingSprint;
    bool notPressingMove;
    //actual inputs
    bool movingForward;
    bool movingLeft;
    bool movingRight;
    bool movingBackward;
    bool crouching;
    bool sprinting;
    bool walking = true; //for testing purposes only
    bool sliding;
    public GameObject groundChecker; // you can use tranform.GetChild() to get the object, but do it this way now
    public GameObject headChecker;
    bool onGround;
    bool notMoving;
    public float stoppingSpeed;
    float finalVelocity;
    float stoppingCountdown;
    //for sliding
    [SerializeField] float slideAcceleration;
    [SerializeField] float slideDeceleration;
    [SerializeField] float maxSlideDeceleration;
    float lastFrameVelocity;
    float currentCounterSlide;
    Vector3 forward;
    Vector3 left;
    Vector3 right;
    Vector3 back;
    //for slide, shoot out a laser and if the distance is greater than a certain distance, continually boost and don't decelerate
    //heights
    [SerializeField] float crouchingLoweringSpeed; //meter per second 
    [SerializeField] float slidingLoweringSpeed; //m/s
    [SerializeField] float risingSpeed; //m/s
    [SerializeField] float crouchingHeight;
    [SerializeField] float slidingHeight;
    [SerializeField] float normalHeight;
    [SerializeField] float headRatio;
    //slope
    public List<float> slopes = new List<float>();
    public float forwardSlope;
    public float slidingSlope;
    public float fallingSlope;
    //get center point
    public int numOfSlopeCircles;
    public float slopeCircleRadius;
    public int numOfSlopePoints;
    public float maxGroundDistance;
    public float directionAngleRange;
    public GameObject testingSphere;
    private Vector3 previousLocation;
    List<GameObject> slopeVisualizers = new List<GameObject>();

    void Start()
    {
        playerCollider = player.GetComponent<CapsuleCollider>();
        playerRigidbody = player.GetComponent<Rigidbody>();
    }
    void Update()
    {
        CheckInput();
        CalculateSlope();
        GroundCheck();
        GravityCheck();
        HeightController();
        MouseLook();
        VelocityController();
        UpdateMovement();
        if (!sliding)
        {
            CalculateDirection();
            BasicMoves();
        }
        else if (sliding)
        {
            Sliding();
        }
    }
    private void GroundCheck()
    {
        onGround = forwardSlope < fallingSlope;
    }
    private void CalculateSlope()
    {
        if (Vector3.Distance(player.transform.position, previousLocation) > 0)
        {
            float sum = 0;
            float count = 0;
            forwardSlope = 0;
            LayerMask layerMask = LayerMask.GetMask("Ground");
            //delete visualizers //remove visualizers later
            for (int i = 0; i < slopeVisualizers.Count; ++i)
            {
                Destroy(slopeVisualizers[i]);
            }
            slopeVisualizers.Clear();
            List<(Vector3, bool)> slopePoints = new List<(Vector3, bool)>();
            Vector3 groundCheckerPos = new Vector3(groundChecker.transform.position.x, groundChecker.transform.position.y + maxGroundDistance, groundChecker.transform.position.z);
            GameObject newVisualizer = Instantiate(testingSphere, groundCheckerPos, Quaternion.identity); //for testing purposes only (uncomment later)
            slopeVisualizers.Add(newVisualizer);
            slopePoints.Add((groundCheckerPos, true));
            for (int i = 1; i <= numOfSlopeCircles; ++i)
            {
                float r = slopeCircleRadius / numOfSlopeCircles * i;
                for (int j = 0; j < numOfSlopePoints; ++j)
                {
                    float radiant = 2f / numOfSlopePoints * j * Mathf.PI;
                    float x = groundChecker.transform.position.x + Mathf.Sin(radiant) * r;
                    float z = groundChecker.transform.position.z + Mathf.Cos(radiant) * r;
                    newVisualizer = Instantiate(testingSphere, new Vector3(x, groundChecker.transform.position.y + maxGroundDistance, z), Quaternion.identity);
                    slopeVisualizers.Add(newVisualizer);
                    slopePoints.Add((new Vector3(x, groundChecker.transform.position.y + maxGroundDistance, z), true));
                }
            }
            for (int i = 0; i < slopePoints.Count; ++i)
            {
                Vector3 rayDir = new Vector3(slopePoints[i].Item1.x, slopePoints[i].Item1.y - 1, slopePoints[i].Item1.z);
                Ray ray = new Ray(slopePoints[i].Item1, rayDir - slopePoints[i].Item1);
                float maxDistance = maxGroundDistance * 2;
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, maxDistance, layerMask))
                {
                    slopePoints[i] = (hit.point, slopePoints[i].Item2);
                    slopeVisualizers[i].transform.position = hit.point;
                }
                else
                {
                    slopePoints[i] = (slopePoints[i].Item1, false);
                    Destroy(slopeVisualizers[i]);
                }
            }
            float playerDirectionAngle = 0;
            if (!notMoving)
            {
                float playerXSpeed = playerRigidbody.velocity.x;
                float playerZSpeed = playerRigidbody.velocity.z;
                playerDirectionAngle = -(Mathf.Atan(playerZSpeed / playerXSpeed) * 180 / Mathf.PI - 90);
                if (playerXSpeed < 0)
                    playerDirectionAngle += 180;
                bool[] conditions = new bool[4] { playerXSpeed == 0 && playerZSpeed > 0, playerXSpeed > 0 && playerZSpeed == 0, playerXSpeed == 0 && playerZSpeed < 0, playerXSpeed < 0 && playerZSpeed == 0 };
                for (int i = 0; i < conditions.Length; ++i)
                    if (conditions[i])
                        playerDirectionAngle = i * 90;
            }   
            //get the points that can help out
            slopes.Clear();
            for (int i = 0; i < numOfSlopePoints; ++i)
            {
                sum = 0;
                count = 0;
                List<Vector3> points = new List<Vector3>();
                if (slopePoints[0].Item2)
                    points.Add(slopePoints[0].Item1);
                for (int j = 0; j < numOfSlopeCircles; ++j)
                {
                    int index = 1 + i + j * numOfSlopePoints;
                    if (slopePoints[index].Item2)
                        points.Add(slopePoints[index].Item1);      
                }
                for (int j = 0; j < points.Count - 1; ++j)
                {
                    bool isNegative = points[j].y < points[j + 1].y;
                    float xDiff = Mathf.Abs(points[j + 1].x - points[j].x);
                    float yDiff = Mathf.Abs(points[j + 1].y - points[j].y);
                    float zDiff = Mathf.Abs(points[j + 1].z - points[j].z);
                    float baseDist = Mathf.Sqrt(Mathf.Pow(xDiff, 2) + Mathf.Pow(zDiff, 2));
                    if (isNegative)
                        sum -= Mathf.Atan(yDiff / baseDist) * 180 / Mathf.PI;
                    else
                        sum += Mathf.Atan(yDiff / baseDist) * 180 / Mathf.PI;
                    ++count;
                }
                if (count > 0)
                    slopes.Add(sum / count);
                else
                    slopes.Add(0);
            }
            //get the forwardSlope
            float slopeRange = 360 / numOfSlopePoints;
            sum = 0;
            count = 0;
            for (int i = 0; i < slopes.Count; ++i)
            {
                float minAngle = (i * slopeRange - slopeRange / 2) % 360;
                float maxAngle = (i * slopeRange + slopeRange / 2) % 360;
                float minDirectionAngle = (playerDirectionAngle - directionAngleRange / 2f) % 360;
                float maxDirectionAngle = (playerDirectionAngle + directionAngleRange / 2f) % 360;
                if (minAngle >= minDirectionAngle &&  minAngle <= maxDirectionAngle || maxAngle >= minDirectionAngle && maxAngle <= maxDirectionAngle)
                {
                    sum += slopes[i];
                    ++count;
                }   
            }
            if (count > 0)
                forwardSlope = sum / count;
            Collider[] groundCheckHits = Physics.OverlapSphere(groundChecker.transform.position, player.transform.lossyScale.x / 2, layerMask);
            if (groundCheckHits.Length == 0)
                forwardSlope = 90;
                
            previousLocation = player.transform.position;
        }       
    }
    private void CalculateDirection()
    {
        forward = transform.forward;
        right = transform.right;
        back = -transform.forward;
        left = -transform.right;
        bool[] conditions = new bool[4] { pressingForward, pressingRight, pressingBack, pressingLeft };
        Vector3[] directions = new Vector3[4] { forward, right, back, left };
        for (int i = 0; i < conditions.Length; ++i)
        {
            if (conditions[i])
            {
                float sum;
                float count;
                float slopeRange = 360 / numOfSlopePoints;
                sum = 0;
                count = 0;
                for (int j = 0; j < slopes.Count; ++j)
                {
                    float minAngle = (j * slopeRange - slopeRange / 2) % 360;
                    float maxAngle = (j * slopeRange + slopeRange / 2) % 360;
                    float minDirectionAngle = ((player.transform.eulerAngles.y + i * 90) % 360 - directionAngleRange / 2f) % 360;
                    float maxDirectionAngle = ((player.transform.eulerAngles.y + i * 90) % 360 + directionAngleRange / 2f) % 360;
                    if (minAngle >= minDirectionAngle && minAngle <= maxDirectionAngle || maxAngle >= minDirectionAngle && maxAngle <= maxDirectionAngle)
                    {
                        sum += slopes[j];
                        ++count;
                    }
                }
                float tempSlope = 0;
                if (count > 0)
                    tempSlope = sum / count;
                //we need to find the y change
                if (tempSlope == 0)
                    continue;
                if (tempSlope > 0)
                {
                    float hyp = 1 / Mathf.Cos(Mathf.Abs(tempSlope) / 180 * Mathf.PI);
                    float y = Mathf.Sqrt(hyp * hyp - 1);
                    directions[i] -= new Vector3(0, y, 0);
                }
                else
                {
                    float hyp = 1 / Mathf.Cos(Mathf.Abs(tempSlope) / 180 * Mathf.PI);
                    float y = Mathf.Sqrt(hyp * hyp - 1);
                    directions[i] += new Vector3(0, y, 0);
                }
            }


        }
        forward = directions[0];
        right = directions[1];
        back = directions[2];
        left = directions[3];
    }
    private void GravityCheck()
    {
        playerRigidbody.useGravity = !onGround; //do this later (gravity stuff is stupid)
    }
    private void CheckInput()
    {
        //start pressing
        if (Input.GetKeyDown(Controls.forward))
        {
            pressingForward = true;
            UpdateMoveDirection(0);
        }
        if (Input.GetKeyDown(Controls.left))
        {
            pressingLeft = true;
            UpdateMoveDirection(1);
        }   
        if (Input.GetKeyDown(Controls.right))
        {
            pressingRight = true;
            UpdateMoveDirection(3);
        }
        if (Input.GetKeyDown(Controls.backward))
        {
            pressingBack = true;
            UpdateMoveDirection(2);
        }
        //stop pressing
        if (Input.GetKeyUp(Controls.forward))
        {
            pressingForward = false;
            UpdateMoveDirection(0);
        }  
        if (Input.GetKeyUp(Controls.left))
        {
            pressingLeft = false;
            UpdateMoveDirection(1);
        }    
        if (Input.GetKeyUp(Controls.right))
        {
            pressingRight = false;
            UpdateMoveDirection(3);
        } 
        if (Input.GetKeyUp(Controls.backward))
        {
            pressingBack = false;
            UpdateMoveDirection(2);
        }
        notPressingMove = !pressingForward && !pressingLeft && !pressingRight && !pressingBack;
        notMoving = playerRigidbody.velocity.magnitude == 0;

        if (Input.GetKeyDown(Controls.crouch))
        {
            pressingCrouch = true;
            UpdateMovement(1);
        }
        if (Input.GetKeyDown(Controls.sprint))
        {
            pressingSprint = true;
            UpdateMovement(0);
        } 
        if (Input.GetKeyUp(Controls.crouch))
        {
            pressingCrouch = false;
            UpdateMovement(1);
        }  
        if (Input.GetKeyUp(Controls.sprint))
        {
            pressingSprint = false;
            UpdateMovement(0);
        }
    }
    private void UpdateMoveDirection(int key)
    {
        bool[] moveDirections = new bool[4] { pressingForward, pressingLeft, pressingBack, pressingRight }; // I don't need this
        if (moveDirections[key] && moveDirections[(key + 2) % 4])
        {
            moveDirections[(key + 2) % 4] = false;
        }
        else if (!moveDirections[key] && moveDirections[(key + 2) % 4])
        {
            moveDirections[(key + 2) % 4] = true;
        }
        movingForward = moveDirections[0];
        movingLeft = moveDirections[1];
        movingBackward = moveDirections[2];
        movingRight = moveDirections[3];
    }
    private void UpdateMovement() // 0 = sprint, 1 = crouch
    {
        if (onGround)
        {
            if (!sliding)
            {
                sprinting = !pressingCrouch && pressingSprint && movingForward;
                crouching = pressingCrouch && !pressingSprint;
            }
            if (pressingCrouch && pressingSprint && !sliding && playerRigidbody.velocity.magnitude < (maxSprintingSpeed + maxWalkingSpeed) / 2)
            {
                crouching = true;
                sprinting = false;
            }
            if (playerRigidbody.velocity.magnitude <= maxCrouchingSpeed)
            {
                sliding = false;

            }
            walking = !crouching && !sprinting && !sliding;
        }
    }
    private void UpdateMovement(int key) // 0 = sprint, 1 = crouch
    {
        if (onGround) 
        {
            if (!sliding)
            {
                sprinting = !pressingCrouch && pressingSprint && movingForward;
                crouching = pressingCrouch && !pressingSprint;
                if (key == 0 && pressingCrouch)
                    crouching = true;
            }
            if (pressingCrouch && pressingSprint && playerRigidbody.velocity.magnitude >= (maxSprintingSpeed + maxWalkingSpeed) / 2 && !sliding)
            {
                sliding = true;
                currentCounterSlide = 0;
                crouching = false;
                sprinting = false;
            }
            else if (pressingCrouch && pressingSprint && !sliding)
            {
                crouching = true;
                sprinting = false;
            }
            else if ((key == 1 && !pressingCrouch))
            {
                sliding = false;
                sprinting = pressingSprint;
            }
            walking = !crouching && !sprinting && !sliding;
        }
    }
    private void BasicMoves()
    {
        if (movingForward)
        {
            playerRigidbody.AddForce(forward * Time.deltaTime * groundAcceleration);
        }
        if (movingLeft)
        {
            playerRigidbody.AddForce(left * Time.deltaTime * groundAcceleration);
        }
        if (movingRight)
        {
            playerRigidbody.AddForce(right * Time.deltaTime * groundAcceleration);
        }
        if (movingBackward)
        {
            playerRigidbody.AddForce(back * Time.deltaTime * groundAcceleration);
        }     
    }
    private void Sliding()
    {
        currentCounterSlide = currentCounterSlide + slideDeceleration * Time.deltaTime < maxSlideDeceleration ? currentCounterSlide + slideDeceleration * Time.deltaTime : maxSlideDeceleration;
        playerRigidbody.AddForce(forward * Time.deltaTime * slideAcceleration);
        playerRigidbody.AddForce(-forward * Time.deltaTime * currentCounterSlide);
        if (currentCounterSlide > slideAcceleration && Mathf.Round(lastFrameVelocity) < Mathf.Round(playerRigidbody.velocity.magnitude))
        {
            sliding = false;
        }

        lastFrameVelocity = playerRigidbody.velocity.magnitude;
    }
    private void VelocityController() // three different states, in air, on ground, and landed
    {
        if (onGround)
        {
            if (playerRigidbody.velocity.magnitude > maxGroundSpeed)
                playerRigidbody.velocity = Vector3.ClampMagnitude(playerRigidbody.velocity, maxGroundSpeed);
            if (sprinting && playerCollider.height == normalHeight && movingForward)
            {
                if (playerRigidbody.velocity.magnitude > maxSprintingSpeed)
                    playerRigidbody.velocity = Vector3.ClampMagnitude(playerRigidbody.velocity, maxSprintingSpeed);
            }
            else if (crouching || (!sliding && playerCollider.height < normalHeight))
            {
                if (playerRigidbody.velocity.magnitude > maxCrouchingSpeed)
                    playerRigidbody.velocity = Vector3.ClampMagnitude(playerRigidbody.velocity, maxCrouchingSpeed);
            }
            else if (sliding)
            {
                if (playerRigidbody.velocity.magnitude > maxSlidingSpeed)
                    playerRigidbody.velocity = Vector3.ClampMagnitude(playerRigidbody.velocity, maxSlidingSpeed);
            }
            else if (walking && playerCollider.height == normalHeight)
            {
                if (playerRigidbody.velocity.magnitude > maxWalkingSpeed)
                    playerRigidbody.velocity = Vector3.ClampMagnitude(playerRigidbody.velocity, maxWalkingSpeed);
            }
            if (notPressingMove && !notMoving && !sliding)
            {
                stoppingCountdown = stoppingCountdown > Time.deltaTime ? stoppingCountdown - Time.deltaTime : 0;
                playerRigidbody.velocity = Vector3.ClampMagnitude(playerRigidbody.velocity, finalVelocity * (stoppingCountdown / stoppingSpeed));
            }
            else if (!notPressingMove && !sliding)
            {
                stoppingCountdown = stoppingSpeed;
                finalVelocity = playerRigidbody.velocity.magnitude;
            }
        }
    }
    private void HeightController()
    {
        if (onGround)
        {
            float groundHeight = player.transform.position.y - playerCollider.height / 2;
            LayerMask layerMask =~ LayerMask.GetMask("Player");
            Collider[] ceilingColliders = Physics.OverlapSphere(headChecker.transform.position, 0.25f, layerMask);
            bool hasCeiling = ceilingColliders.Length > 0;
            if (crouching)
            {
                float newHeight = playerCollider.height;
                if (playerCollider.height > crouchingHeight)
                    newHeight = playerCollider.height - crouchingLoweringSpeed * Time.deltaTime > crouchingHeight ? playerCollider.height - crouchingLoweringSpeed * Time.deltaTime : crouchingHeight;
                if (playerCollider.height < crouchingHeight && !hasCeiling)
                    newHeight = playerCollider.height + crouchingLoweringSpeed * Time.deltaTime < crouchingHeight ? playerCollider.height + crouchingLoweringSpeed * Time.deltaTime : crouchingHeight;
                playerCollider.height = newHeight;
                player.transform.position = new Vector3(player.transform.position.x, groundHeight + newHeight / 2, player.transform.position.z);
                head.transform.position = new Vector3(head.transform.position.x, groundHeight + newHeight * headRatio, head.transform.position.z);
                headChecker.transform.position = new Vector3(player.transform.position.x, groundHeight + newHeight, player.transform.position.z);
                groundChecker.transform.position = new Vector3(player.transform.position.x, groundHeight, player.transform.position.z);
            }
            else if (sliding)
            {
                float newHeight = playerCollider.height;
                if (playerCollider.height > slidingHeight)
                    newHeight = playerCollider.height - slidingLoweringSpeed * Time.deltaTime > slidingHeight ? playerCollider.height - slidingLoweringSpeed * Time.deltaTime : slidingHeight;
                else if (playerCollider.height < slidingHeight && !hasCeiling)
                    newHeight = playerCollider.height + slidingLoweringSpeed * Time.deltaTime < slidingHeight ? playerCollider.height + slidingLoweringSpeed * Time.deltaTime : slidingHeight;
                playerCollider.height = newHeight;
                player.transform.position = new Vector3(player.transform.position.x, groundHeight + newHeight / 2, player.transform.position.z);
                head.transform.position = new Vector3(head.transform.position.x, groundHeight + newHeight * headRatio, head.transform.position.z);
                headChecker.transform.position = new Vector3(player.transform.position.x, groundHeight + newHeight, player.transform.position.z);
                groundChecker.transform.position = new Vector3(player.transform.position.x, groundHeight, player.transform.position.z);
            }
            else
            {
                float newHeight = playerCollider.height;
                if (playerCollider.height > normalHeight)
                    newHeight = playerCollider.height - risingSpeed * Time.deltaTime > normalHeight ? playerCollider.height - risingSpeed * Time.deltaTime : normalHeight;
                else if (playerCollider.height < normalHeight && !hasCeiling)
                    newHeight = playerCollider.height + risingSpeed * Time.deltaTime < normalHeight ? playerCollider.height + risingSpeed * Time.deltaTime : normalHeight;    
                playerCollider.height = newHeight;
                player.transform.position = new Vector3(player.transform.position.x, groundHeight + newHeight / 2, player.transform.position.z);
                head.transform.position = new Vector3(head.transform.position.x, groundHeight + newHeight * headRatio, head.transform.position.z);
                headChecker.transform.position = new Vector3(player.transform.position.x, groundHeight + newHeight, player.transform.position.z);
                groundChecker.transform.position = new Vector3(player.transform.position.x, groundHeight, player.transform.position.z);
            }
        }
    }
    private void MouseLook()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        xRotation = (xRotation - Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime) % 360;
        xRotation = Mathf.Clamp(xRotation, minXTurn, maxXTurn);
        yRotation = (yRotation + Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime) % 360;
        head.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        player.transform.localRotation = Quaternion.Euler(0, yRotation, 0);
    }
}
