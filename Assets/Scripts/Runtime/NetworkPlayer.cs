using System;
using Mirror;
using Runtime;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkPlayer : NetworkBehaviour,IAttackAble
{
    [Header("Component")]
    
    [SerializeField] private Rigidbody2D rigid;
    [SerializeField] private PlayerInput playerInput;
    
    private Vector2 inputVector;
    
    #region Status

    [Header("Status")]
    
    [SerializeField] private uint maxHealth = 10;
    [SyncVar] private uint curHealth;

    [SerializeField] private float speed = 3;
    #endregion
    
    #region Event

    public event Action<uint, uint> OnChangedHealth;
    public event Action OnDead;

    #endregion

    #region Client

    private void Awake()
    {
        playerInput.enabled = false;
    }

    public override void OnStartLocalPlayer()
    {
        playerInput.enabled = true;
    }

    #region INPUT
    
    public void LocalMoveInput(InputAction.CallbackContext context)
    {
        inputVector = context.ReadValue<Vector2>();

        switch (context)
        {
            case {phase:InputActionPhase.Performed}:
                CmdSetInputVector(inputVector);
                break;
            case {phase: InputActionPhase.Canceled}:
                CmdSetInputVector(inputVector);
                break;
        }
    }

    #endregion
    
    private void FixedUpdate()
    {
        Move();
    }
    
    private void Move()
    {
        rigid.MovePosition(rigid.position + inputVector * speed * Time.fixedDeltaTime);
    }
    
    #endregion

    #region Server
    


    [Server]
    public void TakeDamage(uint damage)
    {
        if(curHealth==0) { return; }
        
        curHealth -= damage;
        OnChangedHealth?.Invoke(curHealth, maxHealth);
        
        if (curHealth != 0) { return; }
        Dead();
        OnDead?.Invoke();
    }

    [Command]
    private void CmdSetInputVector(Vector2 input)
    {
        input.Normalize();
        inputVector = input;
    }
    
    #endregion
    
    
    private void Dead()
    {
        GetComponent<SpriteRenderer>().color = Color.red;
    }

}
