using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public Transform muzzle;
    public Projectile projectile;
    public float msBetweenShots = 100f;
    public float muzzleVelocity = 35f;

    public Transform shell;
    public Transform shellInjection;
    
    MuzzleFlash muzzleFlash;
    
    float nextShotTime;

    private void Start() {
        muzzleFlash = GetComponent<MuzzleFlash>();
    }

    public void Shoot()
    {
        if(Time.time >= nextShotTime)
        {
            nextShotTime = Time.time + msBetweenShots / 1000;
            Projectile newProjectile = Instantiate(projectile, muzzle.position, muzzle.rotation) as Projectile;
            newProjectile.SetSpeed(muzzleVelocity);
            Instantiate(shell, shellInjection.position, shellInjection.rotation );
            muzzleFlash.Activate();
        }
    }   
}
