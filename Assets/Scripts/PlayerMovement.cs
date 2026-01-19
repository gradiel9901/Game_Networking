using UnityEngine;
using UnityEngine.InputSystem;

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

        // State
        private Vector2 _moveInput;
        private Vector3 _velocity;
        private bool _isGrounded;
        private bool _isSprinting;
        private bool _isCrouching;
        
        // This variable stores the speed we send to the animator
        private float _animationBlend; 

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
        }

        private void AssignAnimationIDs()
        {
            _speedHash = Animator.StringToHash(speedParameterName);
            _isCrouchingHash = Animator.StringToHash(isCrouchingParameterName);
            _isJumpingHash = Animator.StringToHash(isJumpingParameterName);
            _isGroundedHash = Animator.StringToHash(isGroundedParameterName);
            _attackHash = Animator.StringToHash(attackParameterName);
        }

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
            _inputActions.Player.Disable();
        }

        private void Update()
        {
            CheckGroundStatus();
            HandleMovement();
            HandleGravity();
            UpdateAnimator(); // Update animator last
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