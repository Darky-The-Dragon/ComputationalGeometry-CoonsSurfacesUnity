using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AppUIController : MonoBehaviour
{
    private const string StatusEditingSurface = "Status: Editing reference surface";
    private const string StatusApproximationReady = "Status: Approximation ready";
    private const string StatusRecomputeRequired = "Status: Surface changed, recomputation required";

    [Header("Scene References")] public ReferenceSurfaceGenerator referenceSurface;

    public CoonsGridGenerator coonsGrid;
    public SurfaceApproximationAnalyzer analyzer;

    [Header("UI - Status")] public TMP_Text statusText;

    [Header("UI - Surface")] public TMP_Dropdown surfaceModeDropdown;

    public Slider heightScaleSlider;
    public TMP_Text heightScaleValueText;
    public Slider noiseScaleSlider;
    public TMP_Text noiseScaleValueText;

    [Header("UI - Grid")] public Slider gridXSlider;

    public TMP_Text gridXValueText;
    public Slider gridZSlider;
    public TMP_Text gridZValueText;
    public Slider patchResolutionSlider;
    public TMP_Text patchResolutionValueText;
    public Slider verticalOffsetSlider;
    public TMP_Text verticalOffsetValueText;

    [Header("UI - Display")] public Toggle showReferenceToggle;

    public Toggle showCoonsGridToggle;

    [Header("UI - Heatmap")] public Toggle heatmapToggle;

    public Slider heatmapMaxErrorSlider;
    public TMP_Text heatmapMaxErrorValueText;

    [Header("UI - Buttons")] public Button randomizeNoiseButton;

    public Button generateApproximationButton;
    public TMP_Text generateApproximationButtonText;

    [Header("UI - Metrics")] public TMP_Text metricsText;

    private bool approximationDirty;

    private bool hasGeneratedApproximation;
    private bool suppressCallbacks;

    private void Start()
    {
        SetupUIFromScene();
        BindEvents();
        ApplyInitialWorkflowState();
        RefreshAll();
    }

    private void OnEnable()
    {
        if (referenceSurface != null)
            referenceSurface.OnSurfaceRegenerated += HandleReferenceSurfaceUpdated;

        if (coonsGrid != null)
            coonsGrid.OnGridRegenerated += HandleGridUpdated;
    }

    private void OnDisable()
    {
        if (referenceSurface != null)
            referenceSurface.OnSurfaceRegenerated -= HandleReferenceSurfaceUpdated;

        if (coonsGrid != null)
            coonsGrid.OnGridRegenerated -= HandleGridUpdated;
    }

    private void HandleReferenceSurfaceUpdated()
    {
        RefreshValueTexts();

        if (hasGeneratedApproximation && !approximationDirty)
            RefreshMetricsText();
    }

    private void HandleGridUpdated()
    {
        if (hasGeneratedApproximation)
            RefreshMetricsText();
    }

    private void SetupUIFromScene()
    {
        if (referenceSurface == null || coonsGrid == null)
            return;

        suppressCallbacks = true;

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

        if (showReferenceToggle != null)
            showReferenceToggle.isOn = true;

        if (showCoonsGridToggle != null)
            showCoonsGridToggle.isOn = false;

        suppressCallbacks = false;

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

        if (showReferenceToggle != null)
            showReferenceToggle.onValueChanged.AddListener(OnShowReferenceChanged);

        if (showCoonsGridToggle != null)
            showCoonsGridToggle.onValueChanged.AddListener(OnShowCoonsGridChanged);

        if (heatmapToggle != null)
            heatmapToggle.onValueChanged.AddListener(OnHeatmapToggleChanged);

        if (heatmapMaxErrorSlider != null)
            heatmapMaxErrorSlider.onValueChanged.AddListener(OnHeatmapMaxErrorChanged);

        if (randomizeNoiseButton != null)
            randomizeNoiseButton.onClick.AddListener(OnRandomizeNoiseClicked);

        if (generateApproximationButton != null)
            generateApproximationButton.onClick.AddListener(OnGenerateApproximationClicked);
    }

    private void RefreshAll()
    {
        RefreshValueTexts();
        RefreshMetricsText();
        RefreshStatusText();
        RefreshGenerateButtonText();
        RefreshInteractivity();
        RefreshVisibility();
    }

    private void ApplyInitialWorkflowState()
    {
        hasGeneratedApproximation = false;
        approximationDirty = false;

        if (coonsGrid != null)
            coonsGrid.gameObject.SetActive(false);

        if (referenceSurface != null)
            referenceSurface.gameObject.SetActive(true);

        if (showReferenceToggle != null)
            showReferenceToggle.isOn = true;

        if (showCoonsGridToggle != null)
            showCoonsGridToggle.isOn = false;

        RefreshAll();
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
        if (metricsText == null)
            return;

        if (!hasGeneratedApproximation || approximationDirty || analyzer == null)
        {
            metricsText.text =
                "Mean Error: Not Computed\n" +
                "Max Error: Not Computed\n" +
                "RMSE: Not Computed\n" +
                "Samples: Not Computed";
            return;
        }

        metricsText.text =
            $"Mean Error: {analyzer.GetMeanError():F4}\n" +
            $"Max Error: {analyzer.GetMaxError():F4}\n" +
            $"RMSE: {analyzer.GetRMSE():F4}\n" +
            $"Samples: {analyzer.GetSampleCount()}";
    }

    private void RefreshStatusText()
    {
        if (statusText == null)
            return;

        if (!hasGeneratedApproximation)
        {
            statusText.text = StatusEditingSurface;
            return;
        }

        statusText.text = approximationDirty
            ? StatusRecomputeRequired
            : StatusApproximationReady;
    }

    private void RefreshGenerateButtonText()
    {
        if (generateApproximationButtonText == null)
            return;

        generateApproximationButtonText.text = hasGeneratedApproximation
            ? "Recompute Approximation"
            : "Generate Approximation";
    }

    private void RefreshInteractivity()
    {
        var approximationReady = hasGeneratedApproximation && !approximationDirty;
        var heatmapControlsEnabled = approximationReady;

        if (showCoonsGridToggle != null)
            showCoonsGridToggle.interactable = hasGeneratedApproximation;

        if (heatmapToggle != null)
            heatmapToggle.interactable = heatmapControlsEnabled;

        if (heatmapMaxErrorSlider != null)
            heatmapMaxErrorSlider.interactable = heatmapControlsEnabled && heatmapToggle != null && heatmapToggle.isOn;
    }

    private void RefreshVisibility()
    {
        if (referenceSurface != null)
        {
            var showReference = showReferenceToggle == null || showReferenceToggle.isOn;
            referenceSurface.gameObject.SetActive(showReference);
        }

        if (coonsGrid != null)
        {
            var showGrid = hasGeneratedApproximation &&
                           showCoonsGridToggle != null &&
                           showCoonsGridToggle.isOn;

            coonsGrid.gameObject.SetActive(showGrid);
        }
    }

    private void MarkApproximationDirty()
    {
        if (!hasGeneratedApproximation)
        {
            RefreshAll();
            return;
        }

        approximationDirty = true;
        RefreshAll();
    }

    private void GenerateApproximation()
    {
        if (referenceSurface == null || coonsGrid == null)
            return;

        coonsGrid.GenerateGrid();

        hasGeneratedApproximation = true;
        approximationDirty = false;

        if (showCoonsGridToggle != null)
            showCoonsGridToggle.isOn = true;

        RefreshAll();
    }

    private void OnGenerateApproximationClicked()
    {
        GenerateApproximation();
    }

    private void OnSurfaceModeChanged(int value)
    {
        if (suppressCallbacks || referenceSurface == null)
            return;

        referenceSurface.surfaceMode = (SurfaceFunctionProvider.SurfaceMode)value;
        referenceSurface.GenerateSurface();

        MarkApproximationDirty();
        RefreshValueTexts();
    }

    private void OnHeightScaleChanged(float value)
    {
        if (suppressCallbacks || referenceSurface == null)
            return;

        referenceSurface.heightScale = value;
        referenceSurface.GenerateSurface();

        MarkApproximationDirty();
        RefreshValueTexts();
    }

    private void OnNoiseScaleChanged(float value)
    {
        if (suppressCallbacks || referenceSurface == null)
            return;

        referenceSurface.noiseScale = value;
        referenceSurface.GenerateSurface();

        MarkApproximationDirty();
        RefreshValueTexts();
    }

    private void OnGridXChanged(float value)
    {
        if (suppressCallbacks || coonsGrid == null)
            return;

        coonsGrid.gridResolutionX = Mathf.RoundToInt(value);
        RefreshValueTexts();

        if (hasGeneratedApproximation && !approximationDirty)
            GenerateApproximation();
    }

    private void OnGridZChanged(float value)
    {
        if (suppressCallbacks || coonsGrid == null)
            return;

        coonsGrid.gridResolutionZ = Mathf.RoundToInt(value);
        RefreshValueTexts();

        if (hasGeneratedApproximation && !approximationDirty)
            GenerateApproximation();
    }

    private void OnPatchResolutionChanged(float value)
    {
        if (suppressCallbacks || coonsGrid == null)
            return;

        coonsGrid.patchResolution = Mathf.RoundToInt(value);
        RefreshValueTexts();

        if (hasGeneratedApproximation && !approximationDirty)
            GenerateApproximation();
    }

    private void OnVerticalOffsetChanged(float value)
    {
        if (suppressCallbacks || coonsGrid == null)
            return;

        coonsGrid.verticalOffset = value;
        RefreshValueTexts();

        if (hasGeneratedApproximation && !approximationDirty)
            GenerateApproximation();
    }

    private void OnShowReferenceChanged(bool value)
    {
        if (suppressCallbacks)
            return;

        RefreshVisibility();
    }

    private void OnShowCoonsGridChanged(bool value)
    {
        if (suppressCallbacks)
            return;

        RefreshVisibility();
    }

    private void OnHeatmapToggleChanged(bool value)
    {
        if (suppressCallbacks || coonsGrid == null)
            return;

        coonsGrid.enableHeatmap = value;

        if (hasGeneratedApproximation && !approximationDirty)
            GenerateApproximation();
        else
            RefreshInteractivity();
    }

    private void OnHeatmapMaxErrorChanged(float value)
    {
        if (suppressCallbacks || coonsGrid == null)
            return;

        coonsGrid.heatmapMaxError = value;
        RefreshValueTexts();

        if (hasGeneratedApproximation && !approximationDirty)
            GenerateApproximation();
    }

    private void OnRandomizeNoiseClicked()
    {
        if (referenceSurface == null)
            return;

        referenceSurface.RandomizeNoiseOffsets();
        MarkApproximationDirty();
        RefreshValueTexts();
    }
}