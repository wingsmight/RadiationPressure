using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class StepPressure : MonoBehaviour
{
    private const string RESULT_FILE_PATH = "Assets/Resources/results.txt";


    [SerializeField] private PhotonGenerator photonGenerator;
    [SerializeField] private SoeAngleSlider angleSlider;
    [SerializeField] private SatelliteArea satelliteArea;


    public void Calculate(float startAngle, float finishAngle, float step)
    {
        ClearFile(RESULT_FILE_PATH);

        StartCoroutine(CalculateRoutine(startAngle, finishAngle, step));
    }

    private IEnumerator CalculateRoutine(float startAngle, float finishAngle, float step)
    {
        for (float angle = startAngle; angle <= finishAngle; angle += step)
        {
            photonGenerator.Clear();

            angleSlider.SetAngle(angle);

            yield return photonGenerator.Throw();

            //yield return new WaitForSeconds(5.0f);

            var resultPressure = PhotonGenerator.radiatoinForce / (satelliteArea.OverallArea / 1.0E+13f * RaycastReflectionPhoton.caughtPhtotonCount);
            WriteResults(RESULT_FILE_PATH, angle + ": " + resultPressure.ToString("F13"));

            yield return new WaitForEndOfFrame();

            print("Wave at " + angle + "° has calculated successfully!");
        }

        photonGenerator.Clear();

        print("Calculation has ended successfully!");
    }
    private void ClearFile(string path)
    {
        using (var stream = new FileStream(path, FileMode.Truncate))
        {
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(string.Empty);
            }
        }

        AssetDatabase.ImportAsset(path);
    }
    private void WriteResults(string path, string text)
    {
        StreamWriter writer = new StreamWriter(path, true);
        writer.WriteLine(text);
        writer.Close();

        AssetDatabase.ImportAsset(path);
    }
}
