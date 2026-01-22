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
        [SerializeField] private float animationBlendSpeed = 8f; 

        [Header("Ground Check")]
        [SerializeField] private float groundCheckDistance = 0.2f;
        [SerializeField] private LayerMask groundMask;

        [Header("Camera Settings")]
        [SerializeField] private CinemachineCamera freeLookCamera;
        [Tooltip("Multiplier for the camera radius when crouching (e.g., 0.6 = 60% of original distance)")]
        [SerializeField] private float crouchZoomMultiplier = 0.6f;
        [SerializeField] private float cameraZoomSpeed = 5f;

        [Header("Animator Settings")]
        [SerializeField] private string speedParameterName = "Speed";
        [SerializeField] private string isCrouchingParameterName = "IsCrouching";
        [SerializeField] private string isJumpingParameterName = "IsJumping";
        [SerializeField] private string isGroundedParameterName = "IsGrounded";
        [SerializeField] private string attackParameterName = "Attack";

        // Components
        private CharacterController _characterController;
        private Animator _animator;
        private InputSystem_Actions _inputActions;
        private CinemachineOrbitalFollow _orbitalFollow;

        // State
        private Vector2 _moveInput;
        private Vector3 _velocity;
        private bool _isGrounded;
        private bool _isSprinting;
        private bool _isCrouching;
        
        // This variable stores the speed we send to the animator
        private float _animationBlend; 

        // Camera State
        private float _originalTopRadius;
        private float _originalMiddleRadius;
        private float _originalBottomRadius;
        private float _currentZoomFactor = 1f;

        // Animator Parameter hashes
        private int _speedHash;
        private int _isCrouchingHash;
        private int _isJumpingHash;
        private int _isGroundedHash;
        private int _attackHash;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _animator = GetComponent<Animator>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();

            _inputActions = new InputSystem_Actions();

            AssignAnimationIDs();
            InitializeCamera();
        }

        private void InitializeCamera()
        {
            if (freeLookCamera != null)
            {
                // Get the OrbitalFollow component which holds the rig data
                _orbitalFollow = freeLookCamera.GetComponent<CinemachineOrbitalFollow>();

                if (_orbitalFollow != null)
                {
                   // Store the initial radius of Top, Middle, and Bottom rigs
                   _originalTopRadius = _orbitalFollow.Orbits.Top.Radius;
                   _originalMiddleRadius = _orbitalFollow.Orbits.Center.Radius;
                   _originalBottomRadius = _orbitalFollow.Orbits.Bottom.Radius;
                }
                else
                {
                     Debug.LogWarning("PlayerMovement: CinemachineCamera does not have a CinemachineOrbitalFollow component!");
                }
            }
            else
            {
                Debug.LogWarning("PlayerMovement: No Cinemachine Camera assigned!");
            }
        }

        private void AssignAnimationIDs()
        {
            _speedHash = Animator.StringToHash(speedParameterName);
            _isCrouchingHash = Animator.StringToHash(isCrouchingParameterName);
            _isJumpingHash = Animator.StringToHash(isJumpingParameterName);
            _isGroundedHash = Animator.StringToHash(isGroundedParameterName);
            _attackHash = Animator.StringToHash(attackParameterName);
        }

        [Header("Interaction")]
        [SerializeField] private Transform itemHolder; // Where the item goes
        [SerializeField] private float pickupRange = 2f;
        [SerializeField] private LayerMask pickupLayer;
        [SerializeField] private Vector3 holdPositionOffset = Vector3.zero;
        [SerializeField] private Vector3 holdRotationOffset = Vector3.zero;
        [SerializeField] private GameObject projectilePrefab; // Prefab to spawn

        private GameObject _heldItem;

        // ... existing code ...

        // (inside Update)

            // SHOOTING: Only if holding an item
            if (_heldItem != null && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Play "Shooting" from the beginning (time 0) to allow spamming
                _animator.Play("Shooting", 0, 0f);

                // Spawn Projectile
                if (projectilePrefab != null)
                {
                    // Find FirePoint
                    Transform firePoint = _heldItem.transform.Find("FirePoint");
                    if (firePoint == null) firePoint = _heldItem.transform; // Fallback to item center

                    Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
                }
            }

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

            // SHOOTING: Only if holding an item
            if (_heldItem != null && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Play "Shooting" from the beginning (time 0) to allow spamming
                // -1 means "Base Layer" (or standard layer resolution), 0f is normalized time.
                _animator.Play("Shooting", 0, 0f);

                // Spawn Projectile
                if (projectilePrefab != null)
                {
                    // Find FirePoint
                    Transform firePoint = _heldItem.transform.Find("FirePoint");
                    if (firePoint == null) firePoint = _heldItem.transform; // Fallback to item center

                    Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
                }
            }

            CheckGroundStatus();
            HandleMovement();
            HandleGravity();
            HandleCameraZoom();
            UpdateAnimator(); // Update animator last
        }

        private void HandleCameraZoom()
        {
            if (_orbitalFollow == null) return;

            // Determine target multiplier (1.0 = Normal, 0.6 = Crouched)
            float targetMultiplier = _isCrouching ? crouchZoomMultiplier : 1f;

            // Smoothly lerp the current factor towards the target
            _currentZoomFactor = Mathf.Lerp(_currentZoomFactor, targetMultiplier, Time.deltaTime * cameraZoomSpeed);

            // Apply the factor to all 3 orbits (Top, Middle, Bottom)
            var orbits = _orbitalFollow.Orbits;
            orbits.Top.Radius = _originalTopRadius * _currentZoomFactor;
            orbits.Center.Radius = _originalMiddleRadius * _currentZoomFactor;
            orbits.Bottom.Radius = _originalBottomRadius * _currentZoomFactor;
            _orbitalFollow.Orbits = orbits;
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
            Vector3 moveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;

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

            if (moveDirection.magnitude >= 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                
                // Use targetSpeed for physics (instant response)
                _characterController.Move(moveDirection * targetSpeed * Time.deltaTime);
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

            // FIX: Send the smoothed Input speed to the animator
            _animator.SetFloat(_speedHash, _animationBlend);
            
            _animator.SetBool(_isCrouchingHash, _isCrouching);
            _animator.SetBool(_isGroundedHash, _isGrounded);
            _animator.SetBool(_isJumpingHash, !_isGrounded); 
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
            if (context.performed && _isGrounded)
            {
                 _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                 // Using Trigger is safer for jumps than Bool
                 _animator.SetTrigger("Jump"); 
            }
        }

        private void OnAttack(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                Debug.Log("Player Attacked!");
                if (_animator != null)
                {
                    _animator.SetTrigger(_attackHash);
                }
            }
        }

        #endregion
    }
}