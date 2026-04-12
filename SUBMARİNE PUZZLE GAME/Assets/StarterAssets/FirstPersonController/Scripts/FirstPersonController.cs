using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using PurrNet;
using Cinemachine;
using FMODUnity;

namespace StarterAssets
{
	[RequireComponent(typeof(CharacterController))]

	public class FirstPersonController : NetworkBehaviour
	{
		[Header("Player")]
		public static FirstPersonController Local { get; private set; }
		public float MoveSpeed = 4.0f;
		public float SprintSpeed = 6.0f;
		public float RotationSpeed = 1.0f;
		public float SpeedChangeRate = 10.0f;

		[Space(10)]
		public float JumpHeight = 1.2f;
		public float Gravity = -15.0f;

		[Space(10)]
		public float JumpTimeout = 0.1f;
		public float FallTimeout = 0.15f;

		[Space(10)]
		private Vector3 _initialSpawnPosition;

		[Header("Player Grounded")]
		public bool Grounded = true;
		public bool canJump = true;
		public float GroundedOffset = -0.14f;
		public float GroundedRadius = 0.5f;
		public LayerMask GroundLayers;

		[Header("Cinemachine")]
		[SerializeField] private CinemachineVirtualCamera _cinemachineVirtualCamera;
		public GameObject CinemachineCameraTarget;
		public float TopClamp = 90.0f;
		public float BottomClamp = -90.0f;

		[Header("Audio - Footsteps")]
		[SerializeField] private AudioEventChannelSO _sfxChannel;
		[SerializeField] private EventReference _footstepSound;
		[SerializeField] private float _baseStepDistance = 2.0f; // Yürüme için adım mesafesi
		[SerializeField] private float _sprintStepDistance = 2.5f; // Koşma için adım mesafesi

		private Vector3 _lastStepPosition;

		// cinemachine
		private float _cinemachineTargetPitch;

		// player
		private float _speed;
		private float _rotationVelocity;
		private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;

		// timeout deltatime
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;


#if ENABLE_INPUT_SYSTEM
		private PlayerInput _playerInput;
#endif
		private CharacterController _controller;
		private StarterAssetsInputs _input;
		private GameObject _mainCamera;

		private const float _threshold = 0.01f;

		private bool IsCurrentDeviceMouse
		{
			get
			{
#if ENABLE_INPUT_SYSTEM
				return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
			}
		}

		private void Awake()
		{
			if (_mainCamera == null)
			{
				_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
			}
		}

		protected override void OnSpawned()
		{
			base.OnSpawned();
			enabled = isOwner;
			_cinemachineVirtualCamera.gameObject.SetActive(isOwner);
			_initialSpawnPosition = transform.position;
			if (isOwner)
			{
				Local = this;
			}
		}

		protected override void OnDestroy()
		{
			if (Local == this)
			{
				Local = null;
			}
		}


		private void Start()
		{
			_controller = GetComponent<CharacterController>();
			_input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
			_playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

			// reset our timeouts on start
			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;

			_lastStepPosition = transform.position;
		}

		private void Update()
		{

			GroundedCheck();
			Move();
			JumpAndGravity();
		}

		private void LateUpdate()
		{
			CameraRotation();
		}

		public void UnstuckPlayer()
		{
			if (!isOwner) return;

			Debug.Log("Oyuncu kurtarılıyor, başlangıç noktasına ışınlanıyor...");

			_controller.enabled = false;
			transform.position = _initialSpawnPosition;
			_controller.enabled = true;
		}

		private void GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}

		private void CameraRotation()
		{
			// if there is an input
			if (_input.look.sqrMagnitude >= _threshold)
			{
				//Don't multiply mouse input by Time.deltaTime
				float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

				_cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
				_rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

				// clamp our pitch rotation
				_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

				// Update Cinemachine camera target pitch
				CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

				// rotate the player left and right
				transform.Rotate(Vector3.up * _rotationVelocity);
			}
		}

		private void Move()
		{
			// set target speed based on move speed, sprint speed and if sprint is pressed
			float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

			// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon
			// note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is no input, set the target speed to 0
			if (_input.move == Vector2.zero) targetSpeed = 0.0f;

			// a reference to the players current horizontal velocity
			float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

			float speedOffset = 0.1f;
			float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

			// accelerate or decelerate to target speed
			if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
			{
				// creates curved result rather than a linear one giving a more organic speed change
				// note T in Lerp is clamped, so we don't need to clamp our speed
				_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

				// round speed to 3 decimal places
				_speed = Mathf.Round(_speed * 1000f) / 1000f;
			}
			else
			{
				_speed = targetSpeed;
			}

			// normalise input direction
			Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

			// note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is a move input rotate player when the player is moving
			if (_input.move != Vector2.zero)
			{
				// move
				inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
			}

			// move the player
			_controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);


			// Eğer oyuncu yerdeyse ve hareket ediyorsa (hızı sıfır değilse)
			if (Grounded && _speed > 0.1f)
			{
				// Şu anki hıza göre (koşma/yürüme) hedef adım mesafesini belirle
				float currentStepDistance = _input.sprint ? _sprintStepDistance : _baseStepDistance;

				// Son adım atılan yerden yeterince uzaklaşıldıysa yeni ses çıkar
				if (Vector3.Distance(transform.position, _lastStepPosition) > currentStepDistance)
				{
					PlayFootstepSound();
					_lastStepPosition = transform.position;
				}
			}
			else
			{
				// Havada veya dururken son adım pozisyonunu güncelle ki yere inince hemen ses çıkmasın
				_lastStepPosition = transform.position;
			}
		}
		[ObserversRpc(runLocally: true)]
		private void PlayFootstepSound()
		{
			// Veri paketini oluştur
			AudioEventPayload payload = new AudioEventPayload(
				_footstepSound,
				transform.position
			);

			// Telsizden (Channel) anons geç
			if (_sfxChannel != null)
			{
				_sfxChannel.RaiseEvent(payload);
			}
			else
			{
				Debug.LogWarning("[Audio] SFX_EventChannel atanmamış!");
			}
		}

		private void JumpAndGravity()
		{
			if (Grounded)
			{
				// reset the fall timeout timer
				_fallTimeoutDelta = FallTimeout;

				// stop our velocity dropping infinitely when grounded
				if (_verticalVelocity < 0.0f)
				{
					_verticalVelocity = -2f;
				}
				// Jump
				if (_input.jump && _jumpTimeoutDelta <= 0.0f && canJump)
				{
					// the square root of H * -2 * G = how much velocity needed to reach desired height
					_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
				}

				// jump timeout
				if (_jumpTimeoutDelta >= 0.0f)
				{
					_jumpTimeoutDelta -= Time.deltaTime;
				}
			}
			else
			{
				// reset the jump timeout timer
				_jumpTimeoutDelta = JumpTimeout;

				// fall timeout
				if (_fallTimeoutDelta >= 0.0f)
				{
					_fallTimeoutDelta -= Time.deltaTime;
				}

				// if we are not grounded, do not jump
				_input.jump = false;
			}

			// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
			if (_verticalVelocity < _terminalVelocity)
			{
				_verticalVelocity += Gravity * Time.deltaTime;
			}
		}

		private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle > 360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}

		private void OnDrawGizmosSelected()
		{
			Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
			Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

			if (Grounded) Gizmos.color = transparentGreen;
			else Gizmos.color = transparentRed;

			// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
			Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
		}
	}
}