using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float runSpeed = 10f;
        [SerializeField] private float jumpHeight = 1.5f;
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float rotationSpeed = 10f;
        
        [Tooltip("How fast the animation blends between Idle/Walk/Run")]
        [SerializeField] private float animationBlendSpeed = 7f; 
        [SerializeField] private float transitionDuration = 0.2f;
        [SerializeField] private float attackAnimationSpeed = 1.5f;

        [Header("First Person Settings")]
        [SerializeField] private bool useFirstPerson = false;
        [SerializeField] private Camera firstPersonCamera;
        [SerializeField] private float mouseSensitivity = 15f;
        [SerializeField] private Vector2 verticalLookLimit = new Vector2(-70f, 70f);

        [Header("Ground Check")]
        [SerializeField] private float groundCheckDistance = 0.2f;
        [SerializeField] private LayerMask groundMask;

        [Header("Visual Adjustments")]
        [Tooltip("Assign the child GameObject that holds the visual mesh/animator here.")]
        [SerializeField] private Transform visualModel;
        [Tooltip("Adjust this offset to fix floating or sinking issues (e.g., (0, -0.05, 0)).")]
        [SerializeField] private Vector3 visualOffset = Vector3.zero;

        [Header("Camera Settings")]
        // Settings removed as per request

        // Components
        private CharacterController _characterController;
        private Animator _animator;
        private InputSystem_Actions _inputActions;

        // State
        private Vector2 _moveInput;
        private Vector3 _velocity;
        private bool _isGrounded;
        private bool _isSprinting;
        private bool _isCrouching;
        
        // This variable stores the speed we send to the animator
        private float _animationBlend; 



        // Animation States
        private const string ANIM_IDLE = "Idle";
        private const string ANIM_WALK = "Walking";
        private const string ANIM_RUN = "Running";
        private const string ANIM_CROUCH_IDLE = "Crouched_Idle";
        private const string ANIM_CROUCH_WALK = "Crouched_Walking";
        private const string ANIM_JUMP = "Jumping";
        private const string ANIM_ATTACK = "Attack_Melee";

        private string _currentAnimState;

        // FPS State
        private float _xRotation = 0f;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _animator = GetComponent<Animator>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();

            // Auto-assign visual model if not set, trying to find the object holding the animator
            if (visualModel == null && _animator != null)
            {
                // If animator is ON this object, we can't really offset it freely without moving the root,
                // effectively we only want a child.
                if (_animator.transform != transform)
                {
                    visualModel = _animator.transform;
                }
            }

            _inputActions = new InputSystem_Actions();

            _inputActions = new InputSystem_Actions();
        }



        [Header("Interaction")]
        [SerializeField] private Transform itemHolder; // Where the item goes
        [SerializeField] private float pickupRange = 2f;
        [SerializeField] private LayerMask pickupLayer;
        [SerializeField] private Vector3 holdPositionOffset = Vector3.zero;
        [SerializeField] private Vector3 holdRotationOffset = Vector3.zero;

        private GameObject _heldItem;

        // ... existing code ...



        // ... existing code ...

        private void OnEnable()
        {
            _inputActions.Player.Enable();
            _inputActions.Player.Move.performed += OnMove;
            _inputActions.Player.Move.canceled += OnMove;
            _inputActions.Player.Sprint.performed += OnSprint;
            _inputActions.Player.Sprint.canceled += OnSprint;
            _inputActions.Player.Crouch.performed += OnCrouch;
            _inputActions.Player.Crouch.canceled += OnCrouch;
            _inputActions.Player.Jump.performed += OnJump;
            _inputActions.Player.Attack.performed += OnAttack;
            _inputActions.Player.Interact.performed += OnInteract;
        }

        private void OnDisable()
        {
            _inputActions.Player.Move.performed -= OnMove;
            _inputActions.Player.Move.canceled -= OnMove;
            _inputActions.Player.Sprint.performed -= OnSprint;
            _inputActions.Player.Sprint.canceled -= OnSprint;
            _inputActions.Player.Crouch.performed -= OnCrouch;
            _inputActions.Player.Crouch.canceled -= OnCrouch;
            _inputActions.Player.Jump.performed -= OnJump;
            _inputActions.Player.Attack.performed -= OnAttack;
            _inputActions.Player.Interact.performed -= OnInteract;
            _inputActions.Player.Disable();
        }

        private void Update()
        {
            // Update held item position/rotation every frame to allow Inspector tweaking
            if (_heldItem != null)
            {
                _heldItem.transform.localPosition = holdPositionOffset;
                _heldItem.transform.localRotation = Quaternion.Euler(holdRotationOffset);
            }

            // DEBUG: Hardcoded input using Input System directly (works with "Input System Package (New)")
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                TryPickUpItem();
            }

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                TryPickUpItem();
            }

            CheckGroundStatus();
            
            if (useFirstPerson)
            {
                HandleFirstPersonLook();
            }

            HandleMovement();
            HandleGravity();
            ApplyVisualOffset();
            UpdateAnimator(); // Update animator last
        }

        private void Start()
        {
             if (useFirstPerson)
             {
                 if (firstPersonCamera != null) firstPersonCamera.gameObject.SetActive(true);
                 
                 // Hide cursor
                 Cursor.lockState = CursorLockMode.Locked;
                 Cursor.visible = false;
             }
        }

        private void HandleFirstPersonLook()
        {
            // Read Look input (Delta)
            Vector2 lookInput = _inputActions.Player.Look.ReadValue<Vector2>();
            
            float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
            float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, verticalLookLimit.x, verticalLookLimit.y);

            if (firstPersonCamera != null)
            {
                firstPersonCamera.transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
            }

            // Rotate player body
            transform.Rotate(Vector3.up * mouseX);
        }

        private void ApplyVisualOffset()
        {
            if (visualModel != null)
            {
                visualModel.localPosition = visualOffset;
            }
        }

        private void CheckGroundStatus()
        {
            _isGrounded = _characterController.isGrounded;
            
            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; 
            }
        }

        private void HandleMovement()
        {
            // 0. CHECK FOR LANDING STATE
            // Prevent movement if the player is in the middle of a landing animation.
            var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Land") || stateInfo.IsName("Landing"))
            {
                _moveInput = Vector2.zero; // Zero out input
                _animationBlend = 0f;      // Force animation to stop
                return;                    // Skip movement logic
            }

            // 0.1 CHECK FOR ATTACK STATE
            // Prevent movement if we are attacking
            if (_currentAnimState == ANIM_ATTACK)
            {
                _moveInput = Vector2.zero;
                _animationBlend = 0f;
                return;
            }

            // 1. Determine target speed based on INPUT
            float targetSpeed = _isCrouching ? walkSpeed * 0.5f : (_isSprinting ? runSpeed : walkSpeed);
            if (_moveInput == Vector2.zero) targetSpeed = 0f;

            // 2. CALCULATE BLEND (The Fix)
            // If we are stopping (target is 0), use MoveTowards for a snappy stop.
            if (targetSpeed == 0f)
            {
                // 50f is the deceleration speed. Higher = stops instantly.
                _animationBlend = Mathf.MoveTowards(_animationBlend, 0f, Time.deltaTime * 50f);
            }
            else
            {
                // If we are moving, use Lerp for smooth acceleration
                _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * animationBlendSpeed);
            }

            // 3. Move the character
            Vector3 moveDirection = Vector3.zero;

            if (useFirstPerson)
            {
                // FPS Movement: Direction is relative to player's current rotation (transform.forward/right)
                // because we are rotating the transform with the mouse.
                moveDirection = transform.right * _moveInput.x + transform.forward * _moveInput.y;
            }
            else
            {
                // Third Person Movement: Relative to Camera Main
                moveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;

                if (Camera.main != null)
                {
                    Vector3 camForward = Camera.main.transform.forward;
                    Vector3 camRight = Camera.main.transform.right;
                    camForward.y = 0;
                    camRight.y = 0;
                    camForward.Normalize();
                    camRight.Normalize();
                    moveDirection = (camForward * _moveInput.y + camRight * _moveInput.x).normalized;
                }

                // Handle Rotation only in 3rd Person
                if (moveDirection.magnitude >= 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }

            if (moveDirection.magnitude >= 0.1f || useFirstPerson) // FPS might want to move even with small input
            {
                // Use targetSpeed for physics (instant response)
                _characterController.Move(moveDirection.normalized * targetSpeed * Time.deltaTime);
            }
        }
        
        private void HandleGravity()
        {
            _velocity.y += gravity * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);
        }

        private void UpdateAnimator()
        {
            if (_animator == null) return;

            // Priority: Attack
            // If we are currently in attack state, wait for it to finish
            if (_currentAnimState == ANIM_ATTACK)
            {
                var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                // If the animation hasn't started yet (still Transitioning) or is still playing, return
                if (!stateInfo.IsName(ANIM_ATTACK) || stateInfo.normalizedTime < 1.0f)
                {
                    return;
                }
            }

            // Determine locomotion state
            string newState = ANIM_IDLE;

            if (!_isGrounded)
            {
                newState = ANIM_JUMP;
            }
            else if (_isCrouching)
            {
                // Check if moving
                if (_moveInput != Vector2.zero)
                    newState = ANIM_CROUCH_WALK;
                else
                    newState = ANIM_CROUCH_IDLE;
            }
            else
            {
                // Standing
                if (_moveInput != Vector2.zero)
                {
                    newState = _isSprinting ? ANIM_RUN : ANIM_WALK;
                }
                else
                {
                    newState = ANIM_IDLE;
                }
            }

            ChangeAnimationState(newState);
        }

        private void ChangeAnimationState(string newState)
        {
            if (_currentAnimState == newState) return;

            // Adjust speed: Faster for attacks, normal for everything else
            if (newState == ANIM_ATTACK) 
                _animator.speed = attackAnimationSpeed;
            else 
                _animator.speed = 1f;

            // CrossFade provides smooth transitions
            _animator.CrossFadeInFixedTime(newState, transitionDuration);
            _currentAnimState = newState;
        }

        #region Input Callbacks

        private void OnInteract(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                TryPickUpItem();
            }
        }


        private void TryPickUpItem()
        {
            // If we are already holding an item, maybe drop it? (For now, just return)
            if (_heldItem != null) return;

            // Use OverlapSphere around the player to find items, easier than a forward cast for ground items
            // Center slightly in front and up
            Vector3 checkPos = transform.position + transform.forward * 0.5f + Vector3.up * 0.5f;
            float checkRadius = 1.0f;

            // Debug visualization (Commented out effectively by removal)
            // Debug.DrawWireSphere does not exist in UnityEngine.Debug
            // Debug.DrawRay(checkPos, Vector3.up, Color.yellow, 2.0f); // Simple alternative if needed

            Collider[] hits = Physics.OverlapSphere(checkPos, checkRadius, pickupLayer);
            foreach (var hit in hits)
            {
                 // check for ItemVisuals
                ItemVisuals item = hit.GetComponentInParent<ItemVisuals>();
                if (item != null)
                {
                    Debug.Log($"PlayerMovement: Found item {item.name}");
                    PickUp(item.gameObject);
                    return; // Only pick up one
                }
            }
            
            Debug.Log("PlayerMovement: No pickupable item found nearby.");
        }

        private void PickUp(GameObject itemObj)
        {
            if (itemHolder == null) 
            {
                Debug.LogWarning("PlayerMovement: No ItemHolder assigned!");
                return;
            }

            _heldItem = itemObj;

            // Disable physics/collider
             Collider col = _heldItem.GetComponent<Collider>();
             if (col != null) col.enabled = false;
             Rigidbody rb = _heldItem.GetComponent<Rigidbody>();
             if (rb != null) rb.isKinematic = true;

            // Parent to hand
            _heldItem.transform.SetParent(itemHolder);
            _heldItem.transform.localPosition = holdPositionOffset;
            _heldItem.transform.localRotation = Quaternion.Euler(holdRotationOffset);

            // Disable visuals (spin/sparkle)
            _heldItem.GetComponent<ItemVisuals>()?.OnPickedUp();
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
        }

        private void OnSprint(InputAction.CallbackContext context)
        {
            _isSprinting = context.ReadValueAsButton();
        }

        private void OnCrouch(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                 _isCrouching = context.ReadValueAsButton();
            }
            else if (context.canceled)
            {
                 _isCrouching = false;
            }
        }
        
        private void OnJump(InputAction.CallbackContext context)
        {
            // Cannot jump if we are currently attacking
            if (_currentAnimState == ANIM_ATTACK) return;

            if (context.performed && _isGrounded)
            {
                 _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                 // Direct jump state set, UpdateAnimator will handle "In Air" check starting next frame
                 ChangeAnimationState(ANIM_JUMP);
            }
        }

        private void OnAttack(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                if (_heldItem == null) return;

                Debug.Log("Player Attacked!");
                if (_animator != null)
                {
                    // Force attack state
                    ChangeAnimationState(ANIM_ATTACK);
                    // Reset blending if needed, though direct play overrides it
                }
            }
        }

        #endregion
    }
}