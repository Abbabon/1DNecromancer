﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementController : MovingObject
{
    [SerializeField] Projectile _projectilePrefab;

    private bool _actedOnBeat;
    private CameraShakeEffect _cameraShaker = new CameraShakeEffect();

    //Animations
    private bool _isJumping = false;
    private Animator _animator;
    private int spriteIndex = 0;

    private MoveType _lastMovement;

    protected void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    protected override void Start()
    {
        base.Start();
        GameEngine.Instance.AmmoChanged += OnAmmoChange;
        GameEngine.Instance.SetPlayerRespawn(transform.position);
    }

    protected void Update()
    {
        HandleInput();
    }

    // TODO: support more control methods 
    private void HandleInput()
    {
        if (GameEngine.Instance != null && GameEngine.Instance.GameRunning)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                MoveTile(MoveType.Right);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                MoveTile(MoveType.Left);
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                if (GameEngine.Instance.Ammo > 0)
                {
                    ShootProjectile();
                    GameEngine.Instance.LoseAmmo();
                }
            }
        }
    }

    private void MoveTile(MoveType move)
    {
        if (!_actedOnBeat)
        {
            if (!CanMoveInDirection(move))
                return;
            
            var hitOther = TryMove(move);
            if (hitOther == null)
            {
                //play animation:
                _animator.SetTrigger("Jump");
                _isJumping = true;
            }
            else
            {
                if (hitOther.CompareTag("Respawn"))
                {
                    GameEngine.Instance.SetPlayerRespawn(hitOther.transform.position);
                }

                var enemy = hitOther.GetComponent<GenericEnemy>();
                if (enemy != null)
                {
                    if (enemy.IsCollectable)
                    {
                        enemy.KillEnemy();
                        GameEngine.Instance.GainAmmo();
                        //yes, spelling is hawrde
                        _animator.SetTrigger("Spit");
                    }
                    else
                    {
                        StartCoroutine(_cameraShaker.shake());
                        GameEngine.Instance.LoseHealth();
                    }
                }
            }

            _lastMovement = move;

            if (_lastMovement == MoveType.Right && !_facingRight)
            {
                Flip();
            }
            else if (_lastMovement == MoveType.Left && _facingRight)
            {
                Flip();
            }

            _actedOnBeat = true;
        }
    }

    protected override void AfterMove()
    {
        HandleDrowning();
    }

    public void HandleDrowning()
    {
        var floor = GameEngine.Instance.Tilemap.GetTile(GameEngine.Instance.Tilemap.WorldToCell(transform.position) + new Vector3Int(0, -1, 0));
        if (floor != null && (floor.name.Equals("water") || floor.name.Equals("water_alt")))
        {
            Debug.Log("Drowning");
            _animator.SetTrigger("Drown");
            transform.position = GameEngine.Instance.PlayerDrown();
        }
    }
    
    public bool CanMoveInDirection(MoveType move)
    {
        var floor = GameEngine.Instance.Tilemap.GetTile(GameEngine.Instance.Tilemap.WorldToCell(transform.position) + new Vector3Int(1 * (move == MoveType.Right ? 1 : -1), -1, 0));
        return floor != null;
    }

    //'Reset' movement for this beat
    protected override void OnBeat()
    {
        //animation:
        if (!_isJumping)
        {
            _animator.SetTrigger("Breath");
        }

        //movement:
        if (!_actedOnBeat)
        {
            Penalize();
        }

        CoyoteFrames();

        _actedOnBeat = false;
    }

    //animation events
    public void DoneJumping()
    {
        _isJumping = false;
    }

    private void Penalize(bool swallow = true)
    {
        if (GameEngine.Instance.Ammo > 0)
        {
            GameEngine.Instance.LoseAmmo();
            if (swallow)
            {
                _animator.SetTrigger("Swallow");
            }
        }
    }

    private void ShootProjectile()
    {
        if (!_actedOnBeat)
        {
            var lastMoveDirection = _lastMovement == MoveType.Right ? 1 : -1;
            var projectile = Instantiate(_projectilePrefab, transform.position + Vector3.right * lastMoveDirection, Quaternion.identity);
            projectile.SetDirection(_lastMovement);
            _actedOnBeat = true;
            _animator.SetTrigger("Spit");
        }
    }

    private void OnAmmoChange(int ammo)
    {
        _animator.SetBool("HasAmmo", ammo > 0);
    }
}
