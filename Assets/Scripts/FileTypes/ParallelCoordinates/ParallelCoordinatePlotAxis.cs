using UnityEngine;

public class ParallelCoordinatePlotAxis : MonoBehaviour
{
    TableFile.TableAxis axis;

    public Transform endLow;
    public Transform endHigh;
    public bool moved = true;
    Vector3 lastLowPos = Vector3.zero;
    Vector3 lastHighPos = Vector3.zero;
    public Vector3 getPointOnAxis(int index) {

        Vector3 between = endHigh.position - endLow.position;
        
        if (axis.isNumeric) {
            float val = axis.as_float[index];
            float range = axis.max - axis.min;
            return endLow.position + between * (val-axis.min) / range;
        } else {
            int val = axis.uniqueStrings.IndexOf(axis.as_string[index]);
            float range = axis.uniqueStrings.Count-1;
            return endLow.position + between * val / range;
        }
    }
    public float getValueOnAxis(int index) {
        if (axis.isNumeric) {
            return axis.as_float[index];
        } else {
           return axis.uniqueStrings.IndexOf(axis.as_string[index]);
        }
    }

    public bool isNumeric() {
        return axis.isNumeric;
    }
    public bool isMasked(int index) {
        return false;
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
       if((lastHighPos != endHigh.position) || (lastLowPos != endLow.position)) {
            moved = true;
            lastHighPos = new Vector3(endHigh.position.x,endHigh.position.y,endHigh.position.z);
            lastLowPos = new Vector3(endLow.position.x, endLow.position.y, endLow.position.z);
        } else {
            moved = false;
        }
        
    }
    public void assignAxis(TableFile.TableAxis t) {
        axis = t;
    }
    
}
