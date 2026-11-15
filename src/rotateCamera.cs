using ThunderRoad;
using UnityEngine;

namespace BetterCalibrationUI
{
    class RotateCamera : ThunderBehaviour
    {
        public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update;
        protected override void ManagedUpdate()
        {
            if (false)
            {
                // this.focusTrsf.position = this.targetTrsf.position - this.targetTrsf.forward * this.distance + Vector3.up * this.height;
                this.camera.transform.position = Vector3.Lerp(base.transform.position, this.focusTrsf.position, Time.deltaTime * this.velocity);
                this.camera.transform.LookAt(focusTrsf);
            }
            base.ManagedUpdate();
        }
        private void Start()
        {

        }
        public void Setup(Transform player, Transform camera, float? distanceManual = null)
        {
            Spectator.local.SetViewFree();
            this.focusTrsf = player;
            this.height = 1.0f;
            this.cameraTrsf = camera;
            this.distance = distanceManual ?? this.height * 1.5f;
            this.velocity = 0.1f;
            Spectator.local.transform.position -= this.targetTrsf.forward * this.distance + Vector3.up * this.height;

        }
        private Camera camera;
        public float distance;
        public float height;
        public float velocity = 0.1f;
        public Transform focusTrsf;
        public Transform targetTrsf;
        public Transform cameraTrsf;
        public bool Activate { get; protected set; } = true;
    }
}

