using UnityEngine;
using unityutilities;

public class DynamicLOD : MonoBehaviour
{
	private const float lodMAX = 4;
	private float targetDeltaTime;
	[Range(0, lodMAX)] public float lodMultMin = .1f;
	[Range(0, lodMAX)] public float lodMultMax = lodMAX;
	[Range(0, lodMAX)] public float currentLODMult = 1;
	private const float lodChangeDelta = 1f;
	[ReadOnly] public float smoothSmoothDeltaTime;
	private float smoothSmoothDeltaTimeSmoothFactor = .99f;

	// Start is called before the first frame update
	private void Start()
	{
		targetDeltaTime = 1f / Screen.currentResolution.refreshRate;
		smoothSmoothDeltaTime = Time.smoothDeltaTime;
	}

	// Update is called once per frame
	private void Update()
	{
		smoothSmoothDeltaTime = smoothSmoothDeltaTime * smoothSmoothDeltaTimeSmoothFactor + Time.smoothDeltaTime * (1 - smoothSmoothDeltaTimeSmoothFactor);

		float localDelta = lodChangeDelta;
		if (Mathf.Abs(smoothSmoothDeltaTime - targetDeltaTime) / targetDeltaTime < .1f) localDelta *= 0;
		if (smoothSmoothDeltaTime < targetDeltaTime)
		{
			currentLODMult += localDelta * Time.deltaTime;
		}
		else if (smoothSmoothDeltaTime > targetDeltaTime)
		{
			currentLODMult -= localDelta * Time.deltaTime;
		}

		currentLODMult = Mathf.Clamp(currentLODMult, lodMultMin, lodMultMax);

		QualitySettings.lodBias = currentLODMult;
	}
}