﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MovingObject
{
    private MoveType _direction;
    private Transform _transform;

    protected void Awake()
    {
        _transform = GetComponent<Transform>();
    }

    public void SetDirection(MoveType direction)
    {
        _direction = direction;
        if (_direction == MoveType.Left) Flip();
    }

    protected override void OnBeat()
    {
        var hitOther = TryMove(_direction);
        if (hitOther != null && !hitOther.CompareTag("Respawn"))
        {
            var enemy = hitOther.GetComponent<GenericEnemy>();
            if (enemy != null)
            {
                enemy.KillEnemy();
            }
            Destroy(gameObject);
        }
    }
}
