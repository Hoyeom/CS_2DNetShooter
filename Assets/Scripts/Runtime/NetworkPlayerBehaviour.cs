using System;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Runtime
{
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerInput))]
    public class NetworkPlayerBehaviour : NetworkBehaviour, IAttackAble
    {
        [SerializeField] private Rigidbody2D rigid;
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private LayerMask groundLayer;

        [Header("Status")] [SyncVar] [SerializeField]
        private uint maxHealth = 10;

        [SyncVar] [SerializeField] private uint curHealth;
        [SyncVar] [SerializeField] private float startSpeed = 6;
        [SyncVar] [SerializeField] private float maxSpeed = 3;


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

        public float StartSpeed => startSpeed;
        public float MaxSpeed => maxSpeed;

        #endregion

        #region SERVER

        public override void OnStartServer()
        {
            curHealth = maxHealth;
        }

        [ServerCallback]
        private void Update()
        {
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
            rigid.AddForce(Vector2.right * _inputAxis * StartSpeed);

            rigid.velocity = new Vector2(Mathf.Clamp(rigid.velocity.x, -MaxSpeed, MaxSpeed), rigid.velocity.y);

            if (_inputAxis != 0)
            {
                return;
            }


            Vector2 velocity = rigid.velocity;

            rigid.velocity = Vector2.Lerp(velocity, new Vector2(0, velocity.y), 0.3f);
        }

        private void Jump()
        {
            if(!GetIsGround()) { return; }

            rigid.velocity = new Vector2(rigid.velocity.x, 0);
            
            rigid.AddForce(Vector2.up * 5, ForceMode2D.Impulse);
           
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
    }
}