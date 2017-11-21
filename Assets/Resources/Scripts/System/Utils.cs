using UnityEngine;

public static class GangerEngineUtils
{
    private static int _platformLayer = 8;
    private static int _objectSolid = 8;
    private static int _oneWayplatformLayer = 9;
    private static Ray[] _rays = new Ray[5];

    public static bool OnGround(float x, float y, Rect boxObject)
    {
        return PlaceMeeting(0, 1, boxObject, _objectSolid) > 0 ||
            (PlaceMeeting(0, 1, boxObject, _oneWayplatformLayer) <= 0 &&
            PlaceMeeting(0, 0, boxObject, _oneWayplatformLayer) > 0);
    }

    public static float PlaceMeeting(float dirX, float dirY, Rect boxObject,
        int layerDestination, float distance = 0.09f)
    {
        #region bottom_rays
        _rays[0] = new Ray(new Vector3(boxObject.x, boxObject.y, 0),
            new Vector3(dirX, -dirY, 0));

        _rays[1] = new Ray(new Vector3(boxObject.x + boxObject.width / 2,
            boxObject.y, 0),
            new Vector3(dirX, -dirY, 0));

        _rays[2] = new Ray(new Vector3(boxObject.x + boxObject.width,
            boxObject.y, 0),
            new Vector3(dirX, -dirY, 0));
        #endregion

        _rays[3] = new Ray(new Vector3(boxObject.x,
            boxObject.y + boxObject.height / 2, 0),
            new Vector3(dirX, -dirY, 0));

        _rays[4] = new Ray(new Vector3(boxObject.x + boxObject.width,
            boxObject.y + boxObject.height / 2, 0),
            new Vector3(dirX, -dirY, 0));

        Debug.DrawRay(_rays[0].origin, _rays[0].direction, Color.green);
        Debug.DrawRay(_rays[1].origin, _rays[1].direction, Color.green);
        Debug.DrawRay(_rays[2].origin, _rays[2].direction, Color.green);
        Debug.DrawRay(_rays[3].origin, _rays[2].direction, Color.green);
        Debug.DrawRay(_rays[4].origin, _rays[3].direction, Color.green);

        RaycastHit[] hits = new RaycastHit[5];

        for (int i = 0; i < _rays.Length; i++)
        {
            if (Physics.Raycast(_rays[i], out hits[i], distance))
            {
                if (hits[i].collider.gameObject.layer == layerDestination)
                {
                    //return true;
                    return hits[i].distance;
                }
            }
        }

        //return false;
        return 0f;
    }

    public static float Approach(float start, float end, float shift)
    {
        if (start < end)
        {
            return Mathf.Min(start + shift, end);
        }
        else
        {
            return Mathf.Max(start - shift, end);
        }
    }

}
