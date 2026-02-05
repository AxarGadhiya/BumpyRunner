using System;
using UnityEngine;


public class PlayerInput : MonoBehaviour
{
    // Jump Action - invoked when jump input is pressed (keyboard or mobile)
    public event Action OnJumpInput;

    private bool isMobile;

    // Input values
    public float Horizontal { get; private set; }
    public float Vertical { get; private set; }
    public bool JumpHeld { get; private set; } // Used for animator and movement logic
    public bool JumpReleased { get; private set; }
    public bool DivePressed { get; private set; }

    // Mobile jump held state tracking
    private bool mobileJumpHeld; // Track if mobile jump button is held

    [SerializeField] FixedJoystick movementJoyStick;
    [SerializeField] FixedTouchField fixedTouchLookField;



    //[SerializeField] CameraLook cameraLook;


    private void Awake()
    {
        //isMobile = SystemInfo.deviceType != DeviceType.Desktop;

        if (Application.platform == RuntimePlatform.Android)
        {
            isMobile = true;
            // Code specific to Android
            Debug.Log("Running on Android.");
        }
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            isMobile = true;
            // Code specific to iOS
            Debug.Log("Running on iOS.");
        }
        else if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
        {
            isMobile = false;
            // Code specific to Windows
            Debug.Log("Running on Windows.");
        }

#if UNITY_EDITOR
        isMobile = true;
#endif
        dragged = false;
        //if (isMobile)
        //{
        //    GameObject joystickObj = GameObject.FindGameObjectWithTag("JoyStick");
        //    if (joystickObj != null)
        //    {
        //        fixedJoystick = joystickObj.GetComponent<FixedJoystick>();
        //    }
        //}
    }

    private void Update()
    {
        //cameraLook.LookAxis = fixedTouchLookField.TouchDist;
        ReadInput();
    }

    public bool dragged;

    private void ReadInput()
    {
        // Reset one-frame buttons
        JumpReleased = false;

        if (isMobile)
        {
            if (movementJoyStick != null)
            {
                dragged = movementJoyStick.Dragged;
                Horizontal = Mathf.Clamp(movementJoyStick.Horizontal, -1f, 1f);
                Vertical = Mathf.Clamp(movementJoyStick.Vertical, -1f, 1f);

                // Deadzone (prevents drift)
                float deadZone = 0.15f;
                if (new Vector2(Horizontal, Vertical).magnitude < deadZone)
                {
                    Horizontal = 0f;
                    Vertical = 0f;

                  
                }
                //else
                //{
                //    Debug.Log($"---------Hori{Horizontal}------Verti{Vertical}----------Input NOT In dead zone--------------------");
                //}
            }
            else
            {
                Horizontal = 0f;
                Vertical = 0f;

              //  Debug.Log("-------------------------JoyStick nulll--------------------");
            }

            // Mobile jump held from UI events
            JumpHeld = mobileJumpHeld;

            // Dive is one-frame trigger (button sets it true)
            // After reading it, reset it so it doesn't stay stuck
            bool diveThisFrame = DivePressed;
            DivePressed = false;
            DivePressed = diveThisFrame;
        }
        else
        {
            Horizontal = Input.GetAxisRaw("Horizontal");
            Vertical = Input.GetAxisRaw("Vertical");

            JumpHeld = Input.GetButton("Jump");
            JumpReleased = Input.GetButtonUp("Jump");

            if (Input.GetButtonDown("Jump"))
                OnJumpInput?.Invoke();

            DivePressed = Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.LeftShift);
        }
    }


  
    //public Vector3 GetInputVector(Transform cameraTransform)
    //{
    //    Vector3 input = new Vector3(Horizontal, 0f, Vertical).normalized;
    //    return Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f) * input;
    //}

    // Mobile jump method (called from UI button/FixedTouchField OnPressedDown)
    public void OnMobileJump()
    {
        mobileJumpHeld = true; // Set mobile held state
        JumpHeld = true;
        
        // Invoke jump action for mobile input
        OnJumpInput?.Invoke();
    }

    // Mobile jump release method (called from UI button on release)
    public void OnMobileJumpRelease()
    {
        Debug.Log("<color=cyan> On Mobile Released</color>");
        mobileJumpHeld = false; // Clear mobile held state
        JumpHeld = false;
        JumpReleased = true;
    }

    // Mobile dive method (called from UI button)
    public void OnMobileDive()
    {
        DivePressed = true;
    }
}


//private void ReadInput()
//{
//    if (isMobile)
//    {
//        Horizontal = movementJoyStick != null ? movementJoyStick.Horizontal * 2f : 0f;
//        Vertical = movementJoyStick != null ? movementJoyStick.Vertical * 2f : 0f;
//    }
//    else
//    {
//        Horizontal = Input.GetAxisRaw("Horizontal");
//        Vertical = Input.GetAxisRaw("Vertical");
//    }

//    // Jump input - keyboard
//    bool jumpInput = isMobile ? false : Input.GetButton("Jump");

//    // For mobile, preserve the mobileJumpHeld state; for keyboard, use input
//    if (isMobile)
//    {
//        JumpHeld = mobileJumpHeld; // Keep mobile jump held state
//    }
//    else
//    {
//        JumpHeld = jumpInput; // Use keyboard input
//    }

//    // Invoke jump action when keyboard jump is pressed
//    if (Input.GetButtonDown("Jump"))
//    {
//        OnJumpInput?.Invoke();
//    }
//    JumpReleased = Input.GetButtonUp("Jump");

//    // Dive input
//    DivePressed = Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.LeftShift);
//}
