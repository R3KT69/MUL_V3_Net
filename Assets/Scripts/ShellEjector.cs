using UnityEngine;

public class ShellEjector : MonoBehaviour
{
    public ParticleSystem casingParticleSystem;

    public void EmitShell(Vector3 position, Quaternion rotation)
    {
        casingParticleSystem.transform.SetPositionAndRotation(position, rotation);
        casingParticleSystem.Emit(1);
    }
}
