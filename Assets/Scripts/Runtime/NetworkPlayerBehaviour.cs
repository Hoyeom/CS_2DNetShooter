using System;
using DG.Tweening;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Runtime
{
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerInput))]
    public class NetworkPlayerBehaviour : NetworkBehaviour, IAttackAble
    {
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private Rigidbody2D rigid;
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private Transform aimRig;
        [SerializeField] private Camera mainCam;
        
        [Header("Status")] [SyncVar] [SerializeField]
        private uint maxHealth;

        [SyncVar] [SerializeField] private uint curHealth;
        [SyncVar] [SerializeField] private float speed;
        [SyncVar] [SerializeField] private float jumpPower;


        [SyncVar] private bool _isGround;
        private float jumpDelay = 0.3f;
        private float jumpTimer = 0.3f;

        #region INPUT_VALUE

        private float _inputAxis;
        private float _inputJump;

        #endregion

        #region EVENT

        public event Action<uint, uint> OnChangedHealth;
        public event Action OnDead;

        #endregion

        #region EDITOR

        private void OnValidate()
        {
            rigid ??= GetComponent<Rigidbody2D>();
            playerInput ??= GetComponent<PlayerInput>();
            playerInput.enabled = false;

            aimRig = transform.Find("Aim Rig").transform;

            rigid.simulated = false;
            
            curHealth = maxHealth = 10;
            speed = 4;
            jumpPower = 8;
        }

        #endregion

        #region PROPERTY

        public uint MaxHealth => maxHealth;

        public uint Health
        {
            get => curHealth;
            set
            {
                if (curHealth == 0)
                {
                    return;
                }

                curHealth = value;

                OnChangedHealth?.Invoke(maxHealth, curHealth);

                if (curHealth != 0)
                {
                    return;
                }

                OnDead?.Invoke();
            }
        }

        public float Speed => speed;
        
        #endregion

        #region SERVER
        
        private void Awake()
        {
            mainCam = Camera.main;
        }

        public override void OnStartServer()
        {
            curHealth = maxHealth;
        }
        
        private void Update()
        {
            if(!isServer) { return; }
            GroundCheck();
        }

        private void GroundCheck()
        {
            _isGround = Physics2D.CircleCast(transform.position, .1f, Vector2.down, 0, groundLayer);
        }

        [Server]
        public void TakeDamage(uint damage)
        {
            Health -= damage;
        }

        #endregion

        #region CLIENT

        public override void OnStartLocalPlayer()
        {
            playerInput.enabled = true;
            rigid.simulated = true;
        }

        [ClientCallback]
        private void FixedUpdate()
        {
            Move();
            Jump();
        }

        #region MOVE

        private void Move()
        {
            rigid.velocity = new Vector2(_inputAxis * Speed, rigid.velocity.y);

            if (_inputAxis != 0) { return; }


            Vector2 velocity = rigid.velocity;

            rigid.velocity = Vector2.Lerp(velocity, new Vector2(0, velocity.y), 0.1f);
        }

        private void Jump()
        {
            if(!GetIsGround()) { return; }

            rigid.velocity = new Vector2(rigid.velocity.x, 0);
            
            rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
           
            jumpTimer = Time.time;
        }

        private bool GetIsGround()
        {
            if(!_isGround) { return false; }

            if (_inputJump == 0) { return false; }
            
            if(jumpTimer + jumpDelay > Time.time) { return false; }

            return true;
        }

        #endregion

        #region INPUT

        public void LocalMoveInput(InputAction.CallbackContext context)
        {
            _inputAxis = context.ReadValue<float>();
        }

        public void JumpInput(InputAction.CallbackContext context)
        {
            _inputJump = context.ReadValue<float>();
        }
        
        public void MouseInput(InputAction.CallbackContext context)
        {
            Vector3 target = mainCam.ScreenToWorldPoint(context.ReadValue<Vector2>());
            
            aimRig.DORotateQuaternion(LookAt2D(target,aimRig),0.1f).Restart();
            
            // aimRig.rotation = LookAt2D(target, aimRig);
        }

        #endregion

        #region DEBUG

        [ClientCallback]
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, .1f);
        }

        #endregion

        #endregion

        private Quaternion LookAt2D(Vector3 target, Transform transform = null)
        {
            Vector3 dir = (target - (transform == null ? this.transform.position : transform.position)).normalized;
            
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            return Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }
}