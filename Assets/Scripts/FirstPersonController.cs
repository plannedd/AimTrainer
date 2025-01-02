using UnityEngine;
using System.IO;
using System.Text;
using System;
using System.Collections;
using System.Collections.Generic;


public class FirstPersonController : MonoBehaviour

{
    public bool canMove { get; private set; } = true;
    public bool isSprinting => canSprint && Input.GetKey(sprintKey);
    public bool shouldJump => characterController.isGrounded && Input.GetKeyDown(jumpKey);
    public bool shouldCrouch => characterController.isGrounded && Input.GetKeyDown(crouchKey) && !duringCrouchAnimation;
    [Header("Functional Options")]
    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;

    [Header("Controls")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;

    [Header("Mouse Look Settings")]
    [SerializeField, Range(1,10)] private float lookSpeedX = 2f;
    [SerializeField, Range(1,10)] private float lookSpeedY = 2f;
    [SerializeField, Range(1,180)] private float upperLookLimit = 80f;
    [SerializeField, Range(1,180)] private float lowerLookLimit = 80f;

    [Header("Jumping Settings")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = 30f;

    [Header("Crouch Settings")]
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float crouchHeight = 0.5f;
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float timeToCrouch = 0.25f;
    [SerializeField] private Vector3 crouchCenter = new Vector3(0, 0.5f, 0);
    [SerializeField] private Vector3 standCenter = new Vector3(0, 0f, 0);
    private bool isCrouching = false;
    private bool duringCrouchAnimation = false;

    private Camera playerCamera;
    private CharacterController characterController;

    private Vector3 moveDirection;
    private Vector2 currentInput;

    private float rotationX = 0;

    void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (canMove)
        {
            HandleMovementInput();
            HandleMouseLookInput();

            if(canJump)
                handleJump();

            if(canCrouch)
                handleCrouch();

            ApplyFinalMovement();
            
        }
    }

    private void HandleMovementInput(){
        currentInput = new Vector2((isCrouching ? crouchSpeed : isSprinting  ? sprintSpeed : walkSpeed) * Input.GetAxis("Horizontal"), (isCrouching ? crouchSpeed : isSprinting  ? sprintSpeed : walkSpeed) * Input.GetAxis("Vertical"));
        float moveDirectionY = moveDirection.y;
        moveDirection = (transform.TransformDirection(Vector3.forward) * currentInput.y) + (transform.TransformDirection(Vector3.right) * currentInput.x);
        moveDirection.y = moveDirectionY;
    
    }

    private void HandleMouseLookInput(){
        rotationX -= Input.GetAxis("Mouse Y") * lookSpeedY;
        rotationX = Mathf.Clamp(rotationX, -upperLookLimit, lowerLookLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeedX, 0);
    }

    private void ApplyFinalMovement(){
        if(!characterController.isGrounded)
            moveDirection.y -= gravity * Time.deltaTime;

        characterController.Move(moveDirection * Time.deltaTime);
    }

    private void handleJump(){
    if (shouldJump)
        moveDirection.y = jumpForce;

    }

    private void handleCrouch(){
        if (shouldCrouch)
        {
            StartCoroutine(CrouchStand());
        }
    }

    private IEnumerator CrouchStand()
    {

        if(isCrouching && Physics.Raycast(playerCamera.transform.position, Vector3.up, 1f))
            yield break;
        
        duringCrouchAnimation = true;
        float timeElapsed = 0;
        float targetHeight = isCrouching ? standHeight : crouchHeight;
        float currentHeight = characterController.height;
        Vector3 targetCenter = isCrouching ? standCenter : crouchCenter;
        Vector3 currentCenter = characterController.center;
        while(timeElapsed < timeToCrouch)
        {
            characterController.height = Mathf.Lerp(currentHeight, targetHeight, timeElapsed / timeToCrouch);
            characterController.center = Vector3.Lerp(currentCenter, targetCenter, timeElapsed / timeToCrouch);
            timeElapsed += Time.deltaTime;
            
            yield return null;
        }
        characterController.height = targetHeight;
        characterController.center = targetCenter;
        isCrouching = !isCrouching;
        duringCrouchAnimation = false;
    }
}

