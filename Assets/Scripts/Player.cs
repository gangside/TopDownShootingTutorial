using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(GunController))]
public class Player : LivingEntity
{
    public float moveSpeed = 5;

    public CrossHair crosshair;

    Camera viewCamera;
    PlayerController controller;
    GunController gunController;

    private void Awake() {
        controller = GetComponent<PlayerController>();
        gunController = GetComponent<GunController>();
        viewCamera = Camera.main;
        FindObjectOfType<Spawner>().OnNewWave += OnNewWave;
    }

    protected override void Start()
    {
        base.Start();
    }

    void OnNewWave(int waveNumber) {
        health = startingHealth;
        gunController.EquipGun(waveNumber - 1);
    }

    void Update()
    {

        //이동을 입력받는 곳
        Vector3 moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        Vector3 moveVelocity = moveInput.normalized * moveSpeed;
        controller.Move(moveVelocity);

        //바라보는 방향을 입력받는 곳
        Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.up * gunController.GunHeight);
        float rayDistance;

        if(groundPlane.Raycast(ray, out rayDistance))
        {
            Vector3 point = ray.GetPoint(rayDistance);
            //Debug.DrawLine(ray.origin, point, Color.red);
            controller.LookAt(point);
            crosshair.transform.position = point;
            crosshair.DetectTarget(ray);

            if ((new Vector3(point.x, point.y, point.z) - new Vector3(transform.position.x, transform.position.y, transform.position.z)).sqrMagnitude > 2f){
                gunController.Aim(point);
            }
        }

        //무기 발사를 입력받는 곳
        if (Input.GetMouseButton(0))
        {
            gunController.OnTriggerHold();
        }

        if (Input.GetMouseButtonUp(0)) {
            gunController.OnTriggerRelease();
        }

        if (Input.GetKeyDown(KeyCode.R)){
            gunController.Reload();
        }

        if(transform.position.y < -10) {
            TakeDamage(health);
        }
    }

    public override void Die() {
        AudioManager.instance.PlaySound("Player Death", transform.position);
        base.Die();
    }
}
