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
    [SerializeField] private bool canHeadBob = true;
    [SerializeField] private bool willSlideonSlope = true;
    [SerializeField] private bool canZoom = true;

    [Header("Controls")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode zoomKey = KeyCode.Mouse1;

    [SerializeField] private KeyCode fireKey = KeyCode.Mouse0;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;

    [SerializeField] private float slopeSpeed = 8f;

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

    [Header("Head Bob Settings")]
    [SerializeField] private float walkBobSpeed = 14f;
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float sprintBobSpeed = 18f;
    [SerializeField] private float sprintBobAmount = 0.025f;
    [SerializeField] private float crouchBobSpeed = 8f;
    [SerializeField] private float crouchBobAmount = 0.01f;

    private float defaultPosY;
    private float timer;
     
    // SLIDING SETTINGS

    private Vector3 hitNormal;
    private bool isSliding {
        get {
            if(characterController.isGrounded && Physics.Raycast(transform.position, Vector3.down, out RaycastHit slopeHit, 2f)){

                hitNormal = slopeHit.normal;
                return Vector3.Angle(hitNormal, Vector3.up) > characterController.slopeLimit;

            }
                
            else {
                return false;
            }
        }
    }

    [Header("Zoom Settings")]

    [SerializeField] private float zoomFOV = 60f;
    [SerializeField] private float defaultFOV = 90f;
    [SerializeField] private float timeToZoom = 0.1f;

    private Coroutine zoomRoutine;

    [Header("ADS Settings")]
    [SerializeField] private Transform pistolTransform;
    [SerializeField] private Vector3 adsPosition;
    [SerializeField] private Vector3 hipPosition;

    [Header("Weapon Settings")]

    [SerializeField] private int damage = 10;

    private Coroutine adsRoutine;


    private Camera playerCamera;
    private CharacterController characterController;

    private Vector3 moveDirection;
    private Vector2 currentInput;

    private float rotationX = 0;

    private Transform firePoint;

    void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();
        pistolTransform = GameObject.Find("Pistol").transform;
        firePoint = GameObject.Find("firePoint").transform;
        defaultPosY = playerCamera.transform.localPosition.y;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        adsPosition = pistolTransform.localPosition + new Vector3(-.35f, 0.115f, 0.05f);
        hipPosition = pistolTransform.localPosition;
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

            if(canHeadBob)
                HandleHeadBob();

            if(canZoom)
                HandleZoom();
                HandleADS();
            
            if(Input.GetKeyDown(fireKey))
                Fire();

            ApplyFinalMovement();
            
        }
    }

    private void Fire()
    {
        RaycastHit hit;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); 
        ray = new Ray(firePoint.position, firePoint.forward);
        if (Physics.Raycast(firePoint.position, firePoint.forward, out hit))
        {
            Debug.DrawRay (firePoint.transform.position, transform.forward, Color.red, 5);
            
        }

        Target target = hit.transform.GetComponent<Target>();
        if (target != null)
        {
            target.TakeDamage(damage);
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

        if(willSlideonSlope && isSliding)
        {
            moveDirection += new Vector3(hitNormal.x, -hitNormal.y, hitNormal.z) * slopeSpeed;
        }
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


    private void HandleHeadBob(){
        if(!characterController.isGrounded)
            return;

        if(Mathf.Abs(moveDirection.x) > 0.1f || Mathf.Abs(moveDirection.z) > 0.1f)
        {
            timer += Time.deltaTime * (isCrouching ? crouchBobSpeed : isSprinting ? sprintBobSpeed : walkBobSpeed);

            playerCamera.transform.localPosition = new Vector3(playerCamera.transform.localPosition.x, defaultPosY + Mathf.Sin(timer) * (isCrouching ? crouchBobAmount : isSprinting ? sprintBobAmount : walkBobAmount), playerCamera.transform.localPosition.z);
        }
        else
        {
            timer = 0;
            playerCamera.transform.localPosition = new Vector3(playerCamera.transform.localPosition.x, Mathf.Lerp(playerCamera.transform.localPosition.y, defaultPosY, Time.deltaTime * 11), playerCamera.transform.localPosition.z);
        }

    }

    private void HandleZoom(){
        if(Input.GetKeyDown(zoomKey))
        {
            if(zoomRoutine != null)
                StopCoroutine(zoomRoutine);
                zoomRoutine = null;

            zoomRoutine = StartCoroutine(ToggleZoom(true));
        }

        if(Input.GetKeyUp(zoomKey))
        {
            if(zoomRoutine != null)
                StopCoroutine(zoomRoutine);
                zoomRoutine = null;

            zoomRoutine = StartCoroutine(ToggleZoom(false));
        }
    }

    private void HandleADS()
{
    if (Input.GetKeyDown(zoomKey))
    {
        if (adsRoutine != null)
            StopCoroutine(adsRoutine);
        adsRoutine = StartCoroutine(ToggleADS(true));
    }

    if (Input.GetKeyUp(zoomKey))
    {
        if (adsRoutine != null)
            StopCoroutine(adsRoutine);
        adsRoutine = StartCoroutine(ToggleADS(false));
    }
}


    // COROUTINES

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

    private IEnumerator ToggleZoom(bool isEnter)
    {
        float targetFOV = isEnter ? zoomFOV : defaultFOV;
        float startFOV = playerCamera.fieldOfView;
        float timeElapsed = 0;

        while(timeElapsed < timeToZoom)
        {
            playerCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, timeElapsed / timeToZoom);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        playerCamera.fieldOfView = targetFOV;
        zoomRoutine = null;
    }



    private IEnumerator ToggleADS(bool isEnter)
{
    Vector3 targetPosition = isEnter ? adsPosition : hipPosition;
    Vector3 startPosition = pistolTransform.localPosition;
    float timeElapsed = 0;

    while (timeElapsed < timeToZoom)
    {
        pistolTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, timeElapsed / timeToZoom);
        timeElapsed += Time.deltaTime;
        yield return null;
    }

    pistolTransform.localPosition = targetPosition;
    adsRoutine = null;
}
}


