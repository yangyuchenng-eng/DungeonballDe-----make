using UnityEngine;

public class SpawnedEnemyLink : MonoBehaviour
{
    private RoomEncounter owner;
    private bool reportedDead = false;

    public void Bind(RoomEncounter encounter)
    {
        owner = encounter;
    }

    void OnDisable()
    {
     
        ReportDeadOnce();
    }

    void OnDestroy()
    {
        ReportDeadOnce();
    }

    private void ReportDeadOnce()
    {
        if (reportedDead) return;
        reportedDead = true;

        if (owner != null)
            owner.NotifyEnemyDied(this);
    }
}
