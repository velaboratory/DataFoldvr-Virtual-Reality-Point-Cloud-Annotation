using UnityEngine;
using Paroxe.PdfRenderer;
public class PDFFile : MonoBehaviour {

    public string path;
    public PDFViewer viewer;
    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }

    public void updateContent() {
        viewer.FilePath = path;
    }
}
