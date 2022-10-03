using System.Collections.Generic;
using UnityEngine;

public class ParallelCoordinatePlot : MonoBehaviour
{
    
    public class Record {
        public int index;
        
        public List<Vector3> points = new List<Vector3>();
        public LineRenderer lr;
        public Record(int index, Transform t, LineRenderer linePrefab) {
            this.index = index;
            lr = Instantiate(linePrefab, t);
            
        }
    }
    public List<TableFile.TableAxis> axes;
    public List<ParallelCoordinatePlotAxis> visualAxes = new List<ParallelCoordinatePlotAxis>();
    public ParallelCoordinatePlotAxis axisPrefab;
    public List<Record> recordLines = new List<Record>();
    public Material[] lineMaterialMap;
    public LineRenderer linePrefab;
    public void destroy() {
        foreach(Record r in recordLines) {
            //VectorLine.Destroy(ref r.vl);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        bool anyMoved = false;
        for(int i = 0; i < visualAxes.Count; i++) {
            anyMoved = visualAxes[i].moved;
            if (anyMoved) {
                break;
            }
        }

        foreach (Record r in recordLines) {
            if (anyMoved) {
                r.points.Clear();
                bool isMasked = false;
                for (int i = 0; i < visualAxes.Count; i++) {
                    isMasked = visualAxes[i].isMasked(r.index);
                }
                if (isMasked) {
                    continue;
                }
                Vector3 lineOffset = transform.forward * r.index * .002f;
                for (int i = 0; i < visualAxes.Count; i++) {
                    if (!visualAxes[i].isNumeric()) {
                        int colorIndex = (int)visualAxes[i].getValueOnAxis(r.index);
                        r.lr.material = lineMaterialMap[colorIndex % lineMaterialMap.Length];
                       
                        continue;
                    }
                }
                for (int i = 0; i < visualAxes.Count; i++) {
                    r.points.Add(lineOffset+visualAxes[i].getPointOnAxis(r.index));
                }
                r.lr.startWidth = .01f;
                r.lr.endWidth = .01f;
                r.lr.positionCount = r.points.Count;
                r.lr.SetPositions(r.points.ToArray());
            }
           
        }
        
        
    }
    public void assignAxes(List<TableFile.TableAxis> axes) {
        this.axes = axes;
        float offset = 0f;
        foreach(TableFile.TableAxis t in axes) {
            ParallelCoordinatePlotAxis pca = Instantiate(axisPrefab, transform.position + transform.right * offset, transform.rotation, transform);
            offset += 1.0f;
            pca.assignAxis(t);
            visualAxes.Add(pca);
        }
        if(axes.Count > 0) {
            int numRecords = axes[0].as_string.Length;
            for(int i = 0; i < numRecords; i++) {
                recordLines.Add(new Record(i,transform,linePrefab));
                
            }
        }
    }
}
