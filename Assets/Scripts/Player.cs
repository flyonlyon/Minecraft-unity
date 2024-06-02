using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Player : MonoBehaviour {

    private WorldData world;
    private Transform cameraTransform;

    public bool isGrounded;
    public bool isSprinting;

    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float jumpForce = 5f;
    public float mouseSensitivity = 5f;
    public float gravity = -9.8f;

    public float playerWidth = 0.15f;

    private float horizontal;
    private float vertical;
    private float mouseHorizontal;
    private float mouseVertical;
    private Vector3 velocity;

    private float verticalMomentum = 0;
    private bool jumpRequest;

    public Transform breakBlock;
    public Vector3 placeBlockPosition = new Vector3(0f, 0f, 0f);
    public float checkIncrement = 0.675f;
    public float reach = 7f;

    public Toolbar toolbar;

    private void Start() {
        cameraTransform = GameObject.Find("Main Camera").transform;
        world = GameObject.Find("World").GetComponent<WorldData>();

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update() {

        if (Input.GetKeyDown(KeyCode.E)) world.inUI = !world.inUI;
        if (!world.inUI) {
            GetPlayerInputs();
            PlaceOrBreakBlock();
        }
        
    }

    private void FixedUpdate() {

        if (world.inUI) return;

        CalculateVelocity();
        if (jumpRequest) Jump();

        transform.Rotate(mouseHorizontal * mouseSensitivity * Vector3.up);
        cameraTransform.Rotate(-mouseVertical * mouseSensitivity * Vector3.right);
        transform.Translate(velocity, Space.World);
    }


    private void Jump() {
        verticalMomentum = jumpForce;
        isGrounded = false;
        jumpRequest = false;
    }

    private void CalculateVelocity() {
         
        // Affect vertial momentum with gravity
        if (verticalMomentum > gravity) verticalMomentum += gravity * Time.deltaTime;

        // If we're sprinting, use the sprint multiplier
        if (isSprinting) velocity = sprintSpeed * Time.deltaTime * ((transform.forward * vertical) + (transform.right * horizontal));
        else velocity = walkSpeed * Time.deltaTime * ((transform.forward * vertical) + (transform.right * horizontal));

        // Apply vertical momentum
        velocity += Time.fixedDeltaTime * verticalMomentum * Vector3.up;

        if (velocity.x > 0 && right || velocity.x < 0 && left) velocity.x = 0;
        if (velocity.z > 0 && front || velocity.z < 0 && back) velocity.z = 0;

        if (velocity.y < 0) velocity.y = CheckDownSpeed(velocity.y);
        else if (velocity.y > 0) velocity.y = CheckUpSpeed(velocity.y);
    }

    private void GetPlayerInputs() {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        mouseHorizontal = Input.GetAxis("Mouse X");
        mouseVertical = Input.GetAxis("Mouse Y");

        if (Input.GetButtonDown("Sprint")) isSprinting = true;
        if (Input.GetButtonUp("Sprint")) isSprinting = false;

        if (isGrounded && Input.GetButtonDown("Jump")) jumpRequest = true;

        // Handle selected block

        if (breakBlock.gameObject.activeSelf) {
            if (Input.GetMouseButtonDown(0))
                world.GetChunkFromVector3(breakBlock.position).EditVoxel(breakBlock.position, 0);

            else if (Input.GetMouseButtonDown(1)) {
                if (toolbar.slots[toolbar.slotIndex].itemSlot.HasItem) {
                    world.GetChunkFromVector3(placeBlockPosition).EditVoxel(placeBlockPosition,
                                                                        toolbar.slots[toolbar.slotIndex].itemSlot.stack.id);
                    toolbar.slots[toolbar.slotIndex].itemSlot.Take(1);

                }
            }
                
        }
    }

    private void PlaceOrBreakBlock() {
        Vector3 lastPos = new Vector3();
        float step = checkIncrement;

        while (step < reach) {
            Vector3 position = cameraTransform.position + (cameraTransform.forward * step);

            if (world.CheckForVoxel(position)) {
                breakBlock.position = new Vector3(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y), Mathf.FloorToInt(position.z));
                placeBlockPosition = lastPos;

                breakBlock.gameObject.SetActive(true);
                return;
            }

            lastPos = new Vector3(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y), Mathf.FloorToInt(position.z));
            step += checkIncrement;
        }

        breakBlock.gameObject.SetActive(false);
    }

    private float CheckDownSpeed(float downSpeed) {
        if (world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth))) {
            isGrounded = true;
            return 0;
        }
        isGrounded = false;
        return downSpeed;
    }

    private float CheckUpSpeed(float upSpeed) {
        if (world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f, transform.position.z + playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f, transform.position.z + playerWidth)))
            return 0;
        return upSpeed;
    }

    public bool front {
        get {
            if (world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z + playerWidth)) ||
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z + playerWidth)))
                return true;
            return false;
        }
    }

    public bool back {
        get {
            if (world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z - playerWidth)) ||
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z - playerWidth)))
                return true;
            return false;
        }
    }

    public bool right {
        get {
            if (world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y, transform.position.z)) ||
                world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 1f, transform.position.z)))
                return true;
            return false;
        }
    }

    public bool left {
        get {
            if (world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y, transform.position.z)) ||
                world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 1f, transform.position.z)))
                return true;
            return false;
        }
    }
}
