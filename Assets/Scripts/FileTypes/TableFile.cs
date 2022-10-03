using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using System;
public class TableFile : MonoBehaviour
{
    public class TableAxis {
        public string name;
        public float[] as_float;
        public string[] as_string;
        public bool isNumeric = false;
        public float min;
        public float max;
        public List<string> uniqueStrings = new List<string>();
    }

    public ParallelCoordinatePlot parallelCoordinatePlotPrefab;
    public ParallelCoordinatePlot myPlot;
    public List<TableAxis> axes = new List<TableAxis>();
    public string path;
    public Text t;
    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }

    public void destroy() {
        if (myPlot != null) {
            myPlot.destroy();
			Destroy(myPlot.gameObject);
        }
    }

    public void updateContent() {
        string s = "";
        if (Path.GetExtension(path) == ".csv") {
            string[] lines = File.ReadAllLines(path);
            int numRows = lines.Length;
            //read the column names
            string[] names = lines[0].Split(',');
            int numCols = names.Length;
            axes.Clear();
            for(int i = 0; i < numCols; i++) {
                TableAxis ta = new TableAxis();
                ta.name = names[i].Trim();
                ta.as_float = new float[numRows-1];
                ta.as_string = new string[numRows-1];
                axes.Add(ta);
            }
            for(int r = 1; r < numRows; r++) {
                string[] cols = lines[r].Split(',');
                for (int c = 0; c < numCols; c++) {
                    TableAxis ta = axes[c];
                    ta.as_string[r-1] = cols[c].Trim();
                }
            }

            //now try to convert the axes to float
            for (int c = 0; c < numCols; c++)
            {
                try {
                    float minValue = float.MaxValue;
                    float maxValue = float.MinValue;
                    for (int r = 0; r < axes[c].as_string.Length; r++) {
                        axes[c].as_float[r] = float.Parse(axes[c].as_string[r]);
                        if(axes[c].as_float[r] < minValue) {
                            minValue = axes[c].as_float[r];
                        }
                        if(axes[c].as_float[r] > maxValue) {
                            maxValue = axes[c].as_float[r];
                        }
                    }
                    axes[c].isNumeric = true;
                    axes[c].min = minValue;
                    axes[c].max = maxValue;
                   
                } catch (Exception) {
                    axes[c].isNumeric = false;
                }

                foreach (string a in axes[c].as_string)
                {
                    if (!axes[c].uniqueStrings.Contains(a)) {
                        axes[c].uniqueStrings.Add(a);
                    }
                }
            }

            if (myPlot != null) {
                myPlot.destroy();
				Destroy(myPlot.gameObject);
            }

            myPlot = Instantiate(parallelCoordinatePlotPrefab, transform.position + transform.right, transform.rotation, transform);
            myPlot.assignAxes(axes);
            bool first = true;
            for(int i = 0; i < numRows; i++) { 
                if (!first) {
                    s += "\n";
                } 
                foreach (TableAxis ta in axes) {
                    string z = "";
                    if (first) {
                        z = ta.name;
                    } else {
                        z = ta.as_string[i - 1];
                    }
                    if (z.Length < 20) {
                        s = s + z.PadRight(20) + " ";
                    } else {
                        s = s + z.Substring(0, 17) + "... ";
                    }
                }
                first = false;

            }
            t.text = s;
            

        }
    }
}
