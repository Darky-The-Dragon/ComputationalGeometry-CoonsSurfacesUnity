using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AppUIController : MonoBehaviour
{
    [Header("Scene References")]
    public ReferenceSurfaceGenerator referenceSurface;
    public CoonsGridGenerator coonsGrid;
    public SurfaceApproximationAnalyzer analyzer;

    [Header("UI - Surface")]
    public TMP_Dropdown surfaceModeDropdown;
    public Slider heightScaleSlider;
    public TMP_Text heightScaleValueText;
    public Slider noiseScaleSlider;
    public TMP_Text noiseScaleValueText;

    [Header("UI - Grid")]
    public Slider gridXSlider;
    public TMP_Text gridXValueText;
    public Slider gridZSlider;
    public TMP_Text gridZValueText;
    public Slider patchResolutionSlider;
    public TMP_Text patchResolutionValueText;
    public Slider verticalOffsetSlider;
    public TMP_Text verticalOffsetValueText;

    [Header("UI - Heatmap")]
    public Toggle heatmapToggle;
    public Slider heatmapMaxErrorSlider;
    public TMP_Text heatmapMaxErrorValueText;

    [Header("UI - Buttons")]
    public Button randomizeNoiseButton;

    [Header("UI - Metrics")]
    public TMP_Text metricsText;

    private void Start()
    {
        SetupUIFromScene();
        BindEvents();
        RefreshAll();
    }

    private void OnEnable()
    {
        if (referenceSurface != null)
            referenceSurface.OnSurfaceRegenerated += HandleSceneUpdated;

        if (coonsGrid != null)
            coonsGrid.OnGridRegenerated += HandleSceneUpdated;
    }

    private void OnDisable()
    {
        if (referenceSurface != null)
            referenceSurface.OnSurfaceRegenerated -= HandleSceneUpdated;

        if (coonsGrid != null)
            coonsGrid.OnGridRegenerated -= HandleSceneUpdated;
    }

    private void HandleSceneUpdated()
    {
        RefreshMetricsText();
    }

    private void SetupUIFromScene()
    {
        if (referenceSurface == null || coonsGrid == null)
            return;

        if (surfaceModeDropdown != null)
            surfaceModeDropdown.value = (int)referenceSurface.surfaceMode;

        if (heightScaleSlider != null)
            heightScaleSlider.value = referenceSurface.heightScale;

        if (noiseScaleSlider != null)
            noiseScaleSlider.value = referenceSurface.noiseScale;

        if (gridXSlider != null)
            gridXSlider.value = coonsGrid.gridResolutionX;

        if (gridZSlider != null)
            gridZSlider.value = coonsGrid.gridResolutionZ;

        if (patchResolutionSlider != null)
            patchResolutionSlider.value = coonsGrid.patchResolution;

        if (verticalOffsetSlider != null)
            verticalOffsetSlider.value = coonsGrid.verticalOffset;

        if (heatmapToggle != null)
            heatmapToggle.isOn = coonsGrid.enableHeatmap;

        if (heatmapMaxErrorSlider != null)
            heatmapMaxErrorSlider.value = coonsGrid.heatmapMaxError;

        RefreshValueTexts();
    }

    private void BindEvents()
    {
        if (surfaceModeDropdown != null)
            surfaceModeDropdown.onValueChanged.AddListener(OnSurfaceModeChanged);

        if (heightScaleSlider != null)
            heightScaleSlider.onValueChanged.AddListener(OnHeightScaleChanged);

        if (noiseScaleSlider != null)
            noiseScaleSlider.onValueChanged.AddListener(OnNoiseScaleChanged);

        if (gridXSlider != null)
            gridXSlider.onValueChanged.AddListener(OnGridXChanged);

        if (gridZSlider != null)
            gridZSlider.onValueChanged.AddListener(OnGridZChanged);

        if (patchResolutionSlider != null)
            patchResolutionSlider.onValueChanged.AddListener(OnPatchResolutionChanged);

        if (verticalOffsetSlider != null)
            verticalOffsetSlider.onValueChanged.AddListener(OnVerticalOffsetChanged);

        if (heatmapToggle != null)
            heatmapToggle.onValueChanged.AddListener(OnHeatmapToggleChanged);

        if (heatmapMaxErrorSlider != null)
            heatmapMaxErrorSlider.onValueChanged.AddListener(OnHeatmapMaxErrorChanged);

        if (randomizeNoiseButton != null)
            randomizeNoiseButton.onClick.AddListener(OnRandomizeNoiseClicked);
    }

    private void RefreshAll()
    {
        RefreshValueTexts();
        RefreshMetricsText();
    }

    private void RefreshValueTexts()
    {
        if (heightScaleValueText != null && heightScaleSlider != null)
            heightScaleValueText.text = heightScaleSlider.value.ToString("F2");

        if (noiseScaleValueText != null && noiseScaleSlider != null)
            noiseScaleValueText.text = noiseScaleSlider.value.ToString("F3");

        if (gridXValueText != null && gridXSlider != null)
            gridXValueText.text = ((int)gridXSlider.value).ToString();

        if (gridZValueText != null && gridZSlider != null)
            gridZValueText.text = ((int)gridZSlider.value).ToString();

        if (patchResolutionValueText != null && patchResolutionSlider != null)
            patchResolutionValueText.text = ((int)patchResolutionSlider.value).ToString();

        if (verticalOffsetValueText != null && verticalOffsetSlider != null)
            verticalOffsetValueText.text = verticalOffsetSlider.value.ToString("F3");

        if (heatmapMaxErrorValueText != null && heatmapMaxErrorSlider != null)
            heatmapMaxErrorValueText.text = heatmapMaxErrorSlider.value.ToString("F3");
    }

    private void RefreshMetricsText()
    {
        if (metricsText == null || analyzer == null)
            return;

        metricsText.text =
            $"Mean Error: {analyzer.GetMeanError():F4}\n" +
            $"Max Error: {analyzer.GetMaxError():F4}\n" +
            $"RMSE: {analyzer.GetRMSE():F4}\n" +
            $"Samples: {analyzer.GetSampleCount()}";
    }

    private void OnSurfaceModeChanged(int value)
    {
        referenceSurface.surfaceMode = (SurfaceFunctionProvider.SurfaceMode)value;
        referenceSurface.GenerateSurface();
        RefreshValueTexts();
    }

    private void OnHeightScaleChanged(float value)
    {
        referenceSurface.heightScale = value;
        referenceSurface.GenerateSurface();
        RefreshValueTexts();
    }

    private void OnNoiseScaleChanged(float value)
    {
        referenceSurface.noiseScale = value;
        referenceSurface.GenerateSurface();
        RefreshValueTexts();
    }

    private void OnGridXChanged(float value)
    {
        coonsGrid.gridResolutionX = Mathf.RoundToInt(value);
        coonsGrid.GenerateGrid();
        RefreshValueTexts();
    }

    private void OnGridZChanged(float value)
    {
        coonsGrid.gridResolutionZ = Mathf.RoundToInt(value);
        coonsGrid.GenerateGrid();
        RefreshValueTexts();
    }

    private void OnPatchResolutionChanged(float value)
    {
        coonsGrid.patchResolution = Mathf.RoundToInt(value);
        coonsGrid.GenerateGrid();
        RefreshValueTexts();
    }

    private void OnVerticalOffsetChanged(float value)
    {
        coonsGrid.verticalOffset = value;
        coonsGrid.GenerateGrid();
        RefreshValueTexts();
    }

    private void OnHeatmapToggleChanged(bool value)
    {
        coonsGrid.enableHeatmap = value;
        coonsGrid.GenerateGrid();
    }

    private void OnHeatmapMaxErrorChanged(float value)
    {
        coonsGrid.heatmapMaxError = value;
        coonsGrid.GenerateGrid();
        RefreshValueTexts();
    }

    private void OnRandomizeNoiseClicked()
    {
        referenceSurface.RandomizeNoiseOffsets();
        RefreshValueTexts();
    }
}